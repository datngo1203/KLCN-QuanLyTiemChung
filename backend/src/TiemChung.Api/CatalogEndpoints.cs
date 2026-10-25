using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog").WithTags("Catalog");
        group.AddEndpointFilter(async (invocation, next) => {
            try { return await next(invocation); }
            catch (DbUpdateException e) when (e.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 547 }) {
                throw new IdentityFailure(409, "catalogReferenced");
            }
        });

        group.MapGet("/injection-types", async (string? search, TiemChungDbContext db, CancellationToken ct) => {
            ValidateSearch(search);
            var items = await db.Set<LoaiTiemChung>().AsNoTracking().Where(x => search == null || x.TenLoaiTiemChung.Contains(search))
                .OrderBy(x => x.MaLoaiTiemChung).Select(x => new CatalogTypeDto(x.MaLoaiTiemChung, x.TenLoaiTiemChung, x.MoTa)).ToArrayAsync(ct);
            return Results.Ok(new { source = "database", items });
        });
        group.MapPost("/injection-types", async (CatalogTypeRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, false, ct);
            ValidateText(req.Name, 120); ValidateText(req.Description, 500, false);
            var row = new LoaiTiemChung { TenLoaiTiemChung = req.Name.Trim(), MoTa = req.Description.Trim() };
            db.Add(row); await db.SaveChangesAsync(ct);
            await Audit(db, context, actor, "created", "injection-types", row.MaLoaiTiemChung, ct); await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/catalog/injection-types/{row.MaLoaiTiemChung}", new CatalogTypeDto(row.MaLoaiTiemChung, row.TenLoaiTiemChung, row.MoTa));
        });
        group.MapPut("/injection-types/{id:int}", async (int id, CatalogTypeRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, false, ct);
            ValidateText(req.Name, 120); ValidateText(req.Description, 500, false);
            var row = await db.Set<LoaiTiemChung>().FindAsync([id], ct) ?? throw new IdentityFailure(404, "resourceNotFound");
            row.TenLoaiTiemChung = req.Name.Trim(); row.MoTa = req.Description.Trim();
            await Audit(db, context, actor, "updated", "injection-types", id, ct); await tx.CommitAsync(ct);
            return Results.Ok(new CatalogTypeDto(id, row.TenLoaiTiemChung, row.MoTa));
        });
        group.MapDelete("/injection-types/{id:int}", async (int id, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, false, ct);
            var row = await db.Set<LoaiTiemChung>().FindAsync([id], ct) ?? throw new IdentityFailure(404, "resourceNotFound");
            db.Remove(row); await Audit(db, context, actor, "deleted", "injection-types", id, ct); await tx.CommitAsync(ct);
            return Results.NoContent();
        });

        group.MapGet("/vaccine-types", async (string? search, TiemChungDbContext db, CancellationToken ct) => {
            ValidateSearch(search);
            var items = await db.Set<LoaiVacXin>().AsNoTracking().Where(x => search == null || x.TenLoaiVacXin.Contains(search))
                .OrderBy(x => x.MaLoaiVacXin).Select(x => new CatalogTypeDto(x.MaLoaiVacXin, x.TenLoaiVacXin, x.MoTa)).ToArrayAsync(ct);
            return Results.Ok(new { source = "database", items });
        });
        group.MapPost("/vaccine-types", async (CatalogTypeRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, false, ct);
            ValidateText(req.Name, 120); ValidateText(req.Description, 500, false);
            var row = new LoaiVacXin { TenLoaiVacXin = req.Name.Trim(), MoTa = req.Description.Trim() };
            db.Add(row); await db.SaveChangesAsync(ct);
            await Audit(db, context, actor, "created", "vaccine-types", row.MaLoaiVacXin, ct); await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/catalog/vaccine-types/{row.MaLoaiVacXin}", new CatalogTypeDto(row.MaLoaiVacXin, row.TenLoaiVacXin, row.MoTa));
        });
        group.MapPut("/vaccine-types/{id:int}", async (int id, CatalogTypeRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, false, ct);
            ValidateText(req.Name, 120); ValidateText(req.Description, 500, false);
            var row = await db.Set<LoaiVacXin>().FindAsync([id], ct) ?? throw new IdentityFailure(404, "resourceNotFound");
            row.TenLoaiVacXin = req.Name.Trim(); row.MoTa = req.Description.Trim();
            await Audit(db, context, actor, "updated", "vaccine-types", id, ct); await tx.CommitAsync(ct);
            return Results.Ok(new CatalogTypeDto(id, row.TenLoaiVacXin, row.MoTa));
        });
        group.MapDelete("/vaccine-types/{id:int}", async (int id, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, false, ct);
            var row = await db.Set<LoaiVacXin>().FindAsync([id], ct) ?? throw new IdentityFailure(404, "resourceNotFound");
            db.Remove(row); await Audit(db, context, actor, "deleted", "vaccine-types", id, ct); await tx.CommitAsync(ct);
            return Results.NoContent();
        });

        group.MapGet("/vaccines", async (string? search, int? vaccineTypeId, int? injectionTypeId, decimal? minPrice, decimal? maxPrice,
            TiemChungDbContext db, IConfiguration config, CancellationToken ct) => {
            ValidateSearch(search); ValidateFilters(vaccineTypeId, injectionTypeId, minPrice, maxPrice);
            if (config.GetValue<bool>("Catalog:UseDemoData")) {
                VaccineDto[] samples = [new(1,"Váº¯c xin máº«u A","NhÃ  sáº£n xuáº¥t máº«u","Nguá»“n gá»‘c máº«u",450000), new(2,"Váº¯c xin máº«u B","NhÃ  sáº£n xuáº¥t máº«u","Nguá»“n gá»‘c máº«u",620000)];
                return Results.Ok(new VaccineListResponse("demo", samples.Where(v =>
                    (string.IsNullOrWhiteSpace(search) || v.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) &&
                    (!vaccineTypeId.HasValue || v.VaccineTypeId == vaccineTypeId) && (!injectionTypeId.HasValue || v.InjectionTypeId == injectionTypeId) &&
                    (!minPrice.HasValue || v.Price >= minPrice) && (!maxPrice.HasValue || v.Price <= maxPrice)).ToArray()));
            }
            var query = db.Vaccines.AsNoTracking().Where(v => (search == null || v.TenVacXin.Contains(search)) &&
                (!vaccineTypeId.HasValue || v.MaLoaiVacXin == vaccineTypeId) && (!injectionTypeId.HasValue || v.MaLoaiTiemChung == injectionTypeId) &&
                (!minPrice.HasValue || v.Gia >= minPrice) && (!maxPrice.HasValue || v.Gia <= maxPrice));
            return Results.Ok(new VaccineListResponse("database", await query.OrderBy(v => v.MaVacXin)
                .Select(v => new VaccineDto(v.MaVacXin,v.TenVacXin,v.NhaSanXuat,v.XuatXu,v.Gia,v.MaLoaiVacXin,v.MaLoaiTiemChung)).ToArrayAsync(ct)));
        });
        group.MapPost("/vaccines", async (VaccineRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, true, ct);
            await ValidateVaccine(req, db, ct);
            var row = new VacXin { TrangThai = "Active" }; SetVaccine(row, req); db.Add(row);
            await db.SaveChangesAsync(ct); await Audit(db, context, actor, "created", "vaccines", row.MaVacXin, ct); await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/catalog/vaccines/{row.MaVacXin}", Vaccine(row));
        });
        group.MapPut("/vaccines/{id:int}", async (int id, VaccineRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, true, ct); await ValidateVaccine(req, db, ct);
            var row = await db.Vaccines.FindAsync([id], ct) ?? throw new IdentityFailure(404,"resourceNotFound");
            SetVaccine(row, req); await Audit(db, context, actor, "updated", "vaccines", id, ct); await tx.CommitAsync(ct);
            return Results.Ok(Vaccine(row));
        });
        group.MapDelete("/vaccines/{id:int}", async (int id, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, true, ct);
            var row = await db.Vaccines.FindAsync([id], ct) ?? throw new IdentityFailure(404,"resourceNotFound");
            db.Remove(row); await Audit(db, context, actor, "deleted", "vaccines", id, ct); await tx.CommitAsync(ct);
            return Results.NoContent();
        });
        group.MapGet("/packages", async (string? search, int? vaccineTypeId, int? injectionTypeId, decimal? minPrice, decimal? maxPrice, TiemChungDbContext db, CancellationToken ct) => {
            ValidateSearch(search); ValidateFilters(vaccineTypeId, injectionTypeId, minPrice, maxPrice);
            var rows = await db.Set<GoiVacXin>().AsNoTracking().Where(x => (search == null || x.TenGoi.Contains(search)) &&
                (!minPrice.HasValue || x.GiaGoi >= minPrice) && (!maxPrice.HasValue || x.GiaGoi <= maxPrice) &&
                ((!vaccineTypeId.HasValue && !injectionTypeId.HasValue) || db.Set<GoiVacXin_VacXin>().Any(link => link.MaGoi == x.MaGoi &&
                    db.Vaccines.Any(v => v.MaVacXin == link.MaVacXin && (!vaccineTypeId.HasValue || v.MaLoaiVacXin == vaccineTypeId) &&
                        (!injectionTypeId.HasValue || v.MaLoaiTiemChung == injectionTypeId))))).OrderBy(x => x.MaGoi).ToArrayAsync(ct);
            var ids = rows.Select(x => x.MaGoi).ToArray();
            var links = await (from link in db.Set<GoiVacXin_VacXin>().AsNoTracking()
                join vaccine in db.Vaccines.AsNoTracking() on link.MaVacXin equals vaccine.MaVacXin
                where ids.Contains(link.MaGoi) orderby link.MaVacXin
                select new { link.MaGoi, link.MaVacXin, link.SoLuong, vaccine.TenVacXin }).ToArrayAsync(ct);
            return Results.Ok(new { source = "database", items = rows.Select(x => new PackageDto(x.MaGoi,x.TenGoi,x.GiaGoi,
                links.Where(l => l.MaGoi == x.MaGoi).Select(l => new PackageItemDto(l.MaVacXin,l.SoLuong,l.TenVacXin)).ToArray())) });
        });
        group.MapPost("/packages", async (PackageRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, true, ct); await ValidatePackage(req, db, ct);
            var row = new GoiVacXin { TenGoi = req.Name.Trim(), GiaGoi = req.Price, TrangThai = "Active" }; db.Add(row); await db.SaveChangesAsync(ct);
            SetItems(db, row.MaGoi, req.Items); await Audit(db, context, actor, "created", "packages", row.MaGoi, ct); await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/catalog/packages/{row.MaGoi}", new PackageDto(row.MaGoi,row.TenGoi,row.GiaGoi,req.Items));
        });
        group.MapPut("/packages/{id:int}", async (int id, PackageRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, true, ct); await ValidatePackage(req, db, ct);
            var row = await db.Set<GoiVacXin>().FindAsync([id], ct) ?? throw new IdentityFailure(404,"resourceNotFound");
            row.TenGoi = req.Name.Trim(); row.GiaGoi = req.Price;
            db.RemoveRange(await db.Set<GoiVacXin_VacXin>().Where(x => x.MaGoi == id).ToArrayAsync(ct)); await db.SaveChangesAsync(ct);
            SetItems(db, id, req.Items); await Audit(db, context, actor, "updated", "packages", id, ct); await tx.CommitAsync(ct);
            return Results.Ok(new PackageDto(id,row.TenGoi,row.GiaGoi,req.Items));
        });
        group.MapDelete("/packages/{id:int}", async (int id, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireWrite(context, auth, true, ct);
            var row = await db.Set<GoiVacXin>().FindAsync([id], ct) ?? throw new IdentityFailure(404,"resourceNotFound");
            db.RemoveRange(await db.Set<GoiVacXin_VacXin>().Where(x => x.MaGoi == id).ToArrayAsync(ct));
            db.Remove(row); await Audit(db, context, actor, "deleted", "packages", id, ct); await tx.CommitAsync(ct);
            return Results.NoContent();
        });
        return app;
    }
    private static async Task<IdentityActor> RequireWrite(HttpContext context, IdentityAuthorization auth, bool price, CancellationToken ct)
    {
        var actor = await auth.RequireAsync(context, true, ct);
        if (actor.IsCustomer || !actor.Permissions.Contains("QuanLyDanhMucChuyenMon") || price && !actor.Permissions.Contains("QuanLyDanhMucGia"))
            throw new IdentityFailure(403,"permissionDenied");
        return actor;
    }
    private static void ValidateText(string? value, int max, bool required = true) {
        if (value is null || value.Trim().Length > max || required && string.IsNullOrWhiteSpace(value)) throw new IdentityFailure(400,"validation");
    }
    private static void ValidateSearch(string? search) { if (search?.Length > 150) throw new IdentityFailure(400,"validation"); }
    private static void ValidatePrice(decimal value) {
        if (value < 0 || value > 9999999999999999m || decimal.Truncate(value) != value) throw new IdentityFailure(400,"validation");
    }
    private static void ValidateFilters(int? vaccineTypeId, int? injectionTypeId, decimal? min, decimal? max) {
        if (vaccineTypeId is <= 0 || injectionTypeId is <= 0 || min is < 0 || max is < 0 || min > max) throw new IdentityFailure(400,"validation");
    }
    private static async Task ValidateVaccine(VaccineRequest req, TiemChungDbContext db, CancellationToken ct) {
        ValidateText(req.Name,150); ValidateText(req.Manufacturer,150); ValidateText(req.Origin,100); ValidatePrice(req.Price);
        if (!await db.Set<LoaiVacXin>().AnyAsync(x => x.MaLoaiVacXin == req.VaccineTypeId,ct) ||
            !await db.Set<LoaiTiemChung>().AnyAsync(x => x.MaLoaiTiemChung == req.InjectionTypeId,ct)) throw new IdentityFailure(400,"validation");
    }
    private static void SetVaccine(VacXin row, VaccineRequest req) {
        row.TenVacXin=req.Name.Trim(); row.NhaSanXuat=req.Manufacturer.Trim(); row.XuatXu=req.Origin.Trim();
        row.Gia=req.Price; row.MaLoaiVacXin=req.VaccineTypeId; row.MaLoaiTiemChung=req.InjectionTypeId;
    }
    private static VaccineDto Vaccine(VacXin row) => new(row.MaVacXin,row.TenVacXin,row.NhaSanXuat,row.XuatXu,row.Gia,row.MaLoaiVacXin,row.MaLoaiTiemChung);
    private static async Task ValidatePackage(PackageRequest req, TiemChungDbContext db, CancellationToken ct) {
        ValidateText(req.Name,150); ValidatePrice(req.Price);
        if (req.Items is null || req.Items.Length is < 1 or > 100 || req.Items.Any(x => x is null || x.VaccineId <= 0 || x.Quantity is < 1 or > 100) ||
            req.Items.Select(x => x.VaccineId).Distinct().Count() != req.Items.Length) throw new IdentityFailure(400,"validation");
        var ids = req.Items.Select(x => x.VaccineId).ToArray();
        if (await db.Vaccines.CountAsync(x => ids.Contains(x.MaVacXin),ct) != ids.Length) throw new IdentityFailure(400,"validation");
    }
    private static void SetItems(TiemChungDbContext db, int id, PackageItemDto[] items) => db.AddRange(items.Select(x => new GoiVacXin_VacXin { MaGoi=id, MaVacXin=x.VaccineId, SoLuong=x.Quantity }));
    private static async Task Audit(TiemChungDbContext db, HttpContext context, IdentityActor actor, string action, string type, int id, CancellationToken ct) {
        var audit = IdentityTransactions.Audit(db,"catalog." + action,context.TraceIdentifier,actor.Account.MaTaiKhoan);
        audit.ResourceType=type; audit.ResourceId=id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await db.SaveChangesAsync(ct);
    }
}
