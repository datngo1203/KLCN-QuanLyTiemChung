using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using TiemChung.Infrastructure;
using TiemChung.Api;

namespace TiemChung.Api;

public static class PromotionEndpoints
{
    private const decimal MaxMoney = 9999999999999999m;

    public static void MapPromotionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/promotions").WithTags("Promotions");
        
        group.AddEndpointFilter(async (invocation, next) =>
        {
            invocation.HttpContext.Response.Headers.CacheControl = "no-store";
            try
            {
                return await next(invocation);
            }
            catch (DbUpdateException)
            {
                throw new IdentityFailure(503, "dependencyUnavailable");
            }
        });

        // 1. GET /api/v1/promotions - Dành cho Windows A10
        group.MapGet("/", async (HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) =>
        {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await auth.RequireAsync(context, false, ct);
            RequirePromotionManager(actor);

            var now = DateTime.UtcNow;
            var rows = await db.Promotions.AsNoTracking()
                .OrderByDescending(p => p.ThoiGianBatDau)
                .ThenByDescending(p => p.MaKhuyenMai)
                .ToArrayAsync(ct);

            var assignedIds = await db.Registrations.AsNoTracking()
                .Where(r => r.MaKhuyenMai != null)
                .Select(r => r.MaKhuyenMai!.Value)
                .Distinct()
                .ToHashSetAsync(ct);

            var items = rows.Select(p => MapDto(p, now, assignedIds.Contains(p.MaKhuyenMai))).ToArray();
            await tx.CommitAsync(ct);
            return Results.Ok(new PromotionListResponse(items));
        }).WithName("ListPromotions").Produces<PromotionListResponse>();

        // 2. GET /api/v1/promotions/valid - Dành cho Website
        group.MapGet("/valid", async (TiemChungDbContext db, CancellationToken ct) =>
        {
            var now = DateTime.UtcNow;
            var rows = await db.Promotions.AsNoTracking()
                .Where(p => p.MucUuDai >= 0 && p.ThoiGianBatDau <= now && p.ThoiGianKetThuc >= now)
                .OrderByDescending(p => p.ThoiGianBatDau)
                .ThenByDescending(p => p.MaKhuyenMai)
                .ToArrayAsync(ct);

            var items = rows.Select(p => MapDto(p, now, false)).ToArray();
            return Results.Ok(new PromotionListResponse(items));
        }).WithName("ListValidPromotions").Produces<PromotionListResponse>();

        // 3. POST /api/v1/promotions - Tạo mới ưu đãi
        group.MapPost("/", async (UpsertPromotionRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) =>
        {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await auth.RequireAsync(context, true, ct);
            RequirePromotionManager(actor);

            var (name, description, startUtc, endUtc, discount) = ValidateRequest(req);

            var entity = new KhuyenMai
            {
                TenKhuyenMai = name,
                NoiDung = description,
                ThoiGianBatDau = startUtc,
                ThoiGianKetThuc = endUtc,
                MucUuDai = discount
            };

            db.Promotions.Add(entity);
            await db.SaveChangesAsync(ct);

            var audit = IdentityTransactions.Audit(db, "promotions.created", context.TraceIdentifier,
                actor.Account.MaTaiKhoan, session: actor.Session.Id);
            audit.ResourceType = "Promotion";
            audit.ResourceId = entity.MaKhuyenMai.ToString(CultureInfo.InvariantCulture);
            await db.SaveChangesAsync(ct);

            var dto = MapDto(entity, DateTime.UtcNow, false);
            await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/promotions/{entity.MaKhuyenMai}", dto);
        }).WithName("CreatePromotion").Produces<PromotionDto>(201);

        // 4. PUT /api/v1/promotions/{id} - Cập nhật ưu đãi
        group.MapPut("/{id:int}", async (int id, UpsertPromotionRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) =>
        {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await auth.RequireAsync(context, true, ct);
            RequirePromotionManager(actor);
            if (id <= 0) throw new IdentityFailure(400, "validation");

            var (name, description, startUtc, endUtc, discount) = ValidateRequest(req);

            var entity = await db.Promotions.SingleOrDefaultAsync(p => p.MaKhuyenMai == id, ct)
                ?? throw new IdentityFailure(404, "resourceNotFound");

            entity.TenKhuyenMai = name;
            entity.NoiDung = description;
            entity.ThoiGianBatDau = startUtc;
            entity.ThoiGianKetThuc = endUtc;
            entity.MucUuDai = discount;

            var audit = IdentityTransactions.Audit(db, "promotions.updated", context.TraceIdentifier,
                actor.Account.MaTaiKhoan, session: actor.Session.Id);
            audit.ResourceType = "Promotion";
            audit.ResourceId = entity.MaKhuyenMai.ToString(CultureInfo.InvariantCulture);
            await db.SaveChangesAsync(ct);

            var isAssigned = await db.Registrations.AsNoTracking().AnyAsync(r => r.MaKhuyenMai == id, ct);
            var dto = MapDto(entity, DateTime.UtcNow, isAssigned);
            await tx.CommitAsync(ct);
            return Results.Ok(dto);
        }).WithName("UpdatePromotion").Produces<PromotionDto>();

        // 5. DELETE /api/v1/promotions/{id} - Xóa ưu đãi
        group.MapDelete("/{id:int}", async (int id, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) =>
        {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await auth.RequireAsync(context, true, ct);
            RequirePromotionManager(actor);
            if (id <= 0) throw new IdentityFailure(400, "validation");

            var entity = await db.Promotions.SingleOrDefaultAsync(p => p.MaKhuyenMai == id, ct)
                ?? throw new IdentityFailure(404, "resourceNotFound");

            var isAssigned = await db.Registrations.AnyAsync(r => r.MaKhuyenMai == id, ct);
            if (isAssigned) throw new IdentityFailure(409, "promotionReferenced");

            db.Promotions.Remove(entity);
            var audit = IdentityTransactions.Audit(db, "promotions.deleted", context.TraceIdentifier,
                actor.Account.MaTaiKhoan, session: actor.Session.Id);
            audit.ResourceType = "Promotion";
            audit.ResourceId = id.ToString(CultureInfo.InvariantCulture);
            await db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
            return Results.NoContent();
        }).WithName("DeletePromotion").Produces(204);
    }

    private static void RequirePromotionManager(IdentityActor actor)
    {
        if (actor.IsCustomer || !actor.Permissions.Contains("QuanLyKhuyenMai"))
            throw new IdentityFailure(403, "permissionDenied");
    }

    private static (string Name, string Description, DateTime StartUtc, DateTime EndUtc, decimal Discount) ValidateRequest(UpsertPromotionRequest req)
    {
        var name = req.Name?.Trim() ?? "";
        var description = req.Description?.Trim() ?? "";
        if (name.Length is < 1 or > 150 || description.Length is < 1 or > 500 ||
            req.DiscountAmount < 0 || req.DiscountAmount > MaxMoney ||
            decimal.Truncate(req.DiscountAmount) != req.DiscountAmount ||
            req.EndsAt <= req.StartsAt)
        {
            throw new IdentityFailure(400, "validation");
        }

        return (name, description, req.StartsAt.UtcDateTime, req.EndsAt.UtcDateTime, req.DiscountAmount);
    }

    private static PromotionDto MapDto(KhuyenMai row, DateTime nowUtc, bool isAssigned)
    {
        var startUtc = DateTime.SpecifyKind(row.ThoiGianBatDau, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(row.ThoiGianKetThuc, DateTimeKind.Utc);
        var isActive = row.MucUuDai >= 0 && startUtc <= nowUtc && endUtc >= nowUtc;
        return new PromotionDto(
            row.MaKhuyenMai,
            row.TenKhuyenMai,
            row.NoiDung,
            new DateTimeOffset(startUtc),
            new DateTimeOffset(endUtc),
            row.MucUuDai,
            isActive,
            isAssigned);
    }
}