using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class InventoryEndpoints
{
    private static async Task RequireWarehouse(HttpContext context, IdentityAuthorization auth, CancellationToken ct)
    {
        var actor = await auth.RequireAsync(context, false, ct);
        if (actor.IsCustomer || actor.Employee?.ChucDanh != "ThuKho" || !actor.Permissions.Contains("XemKho"))
            throw new IdentityFailure(403, "permissionDenied");
    }

    private static (int Skip, int Size) Paging(int? page, int? pageSize)
    {
        var size = pageSize ?? 25;
        var skip = ((long)(page ?? 1) - 1) * size;
        if (page is <= 0 || size is <= 0 or > 100 || skip > int.MaxValue)
            throw new IdentityFailure(400, "validation");
        return ((int)skip, size);
    }

    public static void MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/inventory").WithTags("Inventory");
        group.MapGet("/lots", async (HttpContext context, IdentityAuthorization auth, TiemChungDbContext db,
            string? search, int? page, int? pageSize, CancellationToken ct) => {
            await RequireWarehouse(context, auth, ct);
            var (skip, size) = Paging(page, pageSize);
            search = search?.Trim();
            if (search?.Length > 100) throw new IdentityFailure(400, "validation");
            var rows = from l in db.Set<LoVacXin>().AsNoTracking()
                       join v in db.Vaccines.AsNoTracking() on l.MaVacXin equals v.MaVacXin
                       where string.IsNullOrEmpty(search) || l.SoLo.Contains(search) || v.TenVacXin.Contains(search)
                       orderby l.HanSuDung, l.SoLo
                       select new InventoryBatchDto(l.SoLo, v.MaVacXin, v.TenVacXin, l.NgaySanXuat,
                           l.HanSuDung, l.SoLuongBanDau, l.SoLuongTon, l.TrangThai);
            var items = await rows.Skip(skip).Take(size + 1).ToArrayAsync(ct);
            var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
            return Results.Ok(new InventoryBatchListResponse(items.Take(size).ToArray(), items.Length > size, today));
        });
        // Query parameter preserves lot numbers containing '/', spaces or other reserved characters.
        group.MapGet("/movements", async (HttpContext context, IdentityAuthorization auth, TiemChungDbContext db,
            string lotNumber, int? page, int? pageSize, CancellationToken ct) => {
            await RequireWarehouse(context, auth, ct);
            var (skip, size) = Paging(page, pageSize);
            if (string.IsNullOrWhiteSpace(lotNumber) || lotNumber.Length > 80)
                throw new IdentityFailure(400, "validation");
            if (!await db.Set<LoVacXin>().AnyAsync(l => l.SoLo == lotNumber, ct))
                throw new IdentityFailure(404, "resourceNotFound");
            var rows = from m in db.Set<BienDongKho>().AsNoTracking()
                       join n in db.Set<NhanVien>().AsNoTracking() on m.MaNhanVien equals n.MaNhanVien
                       where m.SoLo == lotNumber
                       orderby m.ThoiDiem descending, m.MaBienDong descending
                       select new InventoryMovementDto(m.MaBienDong, m.SoLo, m.LoaiBienDong, m.SoLuong,
                           new DateTimeOffset(DateTime.SpecifyKind(m.ThoiDiem, DateTimeKind.Utc)), n.MaNhanVien, n.HoTen);
            var items = await rows.Skip(skip).Take(size + 1).ToArrayAsync(ct);
            return Results.Ok(new InventoryMovementListResponse(items.Take(size).ToArray(), items.Length > size));
        });
    }
}
