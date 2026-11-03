using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class InventoryReceiptEndpoints
{
    private static async Task<IdentityActor> RequireReceiver(HttpContext context, IdentityAuthorization auth, bool write, CancellationToken ct)
    {
        var actor = await auth.RequireAsync(context, write, ct);
        if (actor.IsCustomer || actor.Employee?.ChucDanh != "ThuKho" ||
            !actor.Permissions.Contains("NhapKho") || !actor.Permissions.Contains("XemKho"))
            throw new IdentityFailure(403, "permissionDenied");
        return actor;
    }
    private static Task<InventoryReceiptDto?> Read(TiemChungDbContext db, string lot, CancellationToken ct) =>
        (from r in db.Set<InventoryReceipt>().AsNoTracking()
         join l in db.Set<LoVacXin>() on r.SoLo equals l.SoLo
         join m in db.Set<BienDongKho>() on r.MovementId equals m.MaBienDong
         join v in db.Vaccines on l.MaVacXin equals v.MaVacXin
         join n in db.Set<NhanVien>() on m.MaNhanVien equals n.MaNhanVien
         where r.SoLo == lot
         select new InventoryReceiptDto(new(l.SoLo, l.MaVacXin, l.NgaySanXuat, l.HanSuDung, l.SoLuongBanDau,
             r.SupplierName, r.DocumentNumber, r.StorageLocation, true), v.TenVacXin, m.MaBienDong,
             n.MaNhanVien, n.HoTen, new DateTimeOffset(DateTime.SpecifyKind(m.ThoiDiem, DateTimeKind.Utc)))).SingleOrDefaultAsync(ct);
    private static bool Text(string? value, int max) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= max;
    public static void MapInventoryReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/inventory").WithTags("Inventory");
        group.MapGet("/receiving-vaccines", async (HttpContext context, IdentityAuthorization auth, TiemChungDbContext db,
            string? search, int? page, CancellationToken ct) => {
            await RequireReceiver(context, auth, false, ct);
            search = search?.Trim();
            if (search?.Length > 100 || page is <= 0 || ((long)(page ?? 1)-1)*25 > int.MaxValue) throw new IdentityFailure(400,"validation");
            var rows = await db.Vaccines.AsNoTracking().Where(v => v.TrangThai == "Active" &&
                (string.IsNullOrEmpty(search) || v.TenVacXin.Contains(search))).OrderBy(v => v.TenVacXin).ThenBy(v => v.MaVacXin)
                .Skip(((page ?? 1)-1)*25).Take(26).Select(v => new InventoryVaccineDto(v.MaVacXin,v.TenVacXin)).ToArrayAsync(ct);
            return Results.Ok(new InventoryVaccineListResponse(rows.Take(25).ToArray(),rows.Length>25));
        });
        // Read-only history uses only X2.1 receipts; legacy/demo lots are deliberately excluded.
        group.MapGet("/receipts/history", async (HttpContext context, IdentityAuthorization auth, TiemChungDbContext db,
            string? search, int? page, CancellationToken ct) => {
            await RequireReceiver(context, auth, false, ct);
            search = search?.Trim();
            var skip = ((long)(page ?? 1) - 1) * 25;
            if (search?.Length > 100 || page is <= 0 || skip > int.MaxValue)
                throw new IdentityFailure(400, "validation");
            var rows = from r in db.Set<InventoryReceipt>().AsNoTracking()
                       join l in db.Set<LoVacXin>().AsNoTracking() on r.SoLo equals l.SoLo
                       join v in db.Vaccines.AsNoTracking() on l.MaVacXin equals v.MaVacXin
                       join m in db.Set<BienDongKho>().AsNoTracking() on r.MovementId equals m.MaBienDong
                       join n in db.Set<NhanVien>().AsNoTracking() on m.MaNhanVien equals n.MaNhanVien
                       where m.SoLo == r.SoLo && m.LoaiBienDong == "NhapKho" && m.MaMuiTiem == null &&
                             m.SoLuong > 0 && m.SoLuong == l.SoLuongBanDau &&
                             (string.IsNullOrEmpty(search) || r.SoLo.Contains(search) ||
                             r.SupplierName.Contains(search) || r.DocumentNumber.Contains(search) ||
                             v.TenVacXin.Contains(search))
                       orderby m.ThoiDiem descending, m.MaBienDong descending
                       select new InventoryReceiptHistoryItemDto(r.SoLo, v.TenVacXin, m.SoLuong,
                           r.SupplierName, r.DocumentNumber, r.StorageLocation, m.MaBienDong,
                           n.MaNhanVien, n.HoTen,
                           new DateTimeOffset(DateTime.SpecifyKind(m.ThoiDiem, DateTimeKind.Utc)));
            var items = await rows.Skip((int)skip).Take(26).ToArrayAsync(ct);
            return Results.Ok(new InventoryReceiptHistoryListResponse(items.Take(25).ToArray(), items.Length > 25));
        });
        group.MapGet("/receipts", async (string lotNumber, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await RequireReceiver(context, auth, false, ct);
            if (!Text(lotNumber,80)) throw new IdentityFailure(400,"validation");
            return Results.Ok(await Read(db,lotNumber.Trim(),ct) ?? throw new IdentityFailure(404,"resourceNotFound"));
        });
        group.MapPost("/receipts", async (ReceiveInventoryRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db,ct);
            var actor = await RequireReceiver(context,auth,true,ct);
            var now = DateTimeOffset.UtcNow;
            var today = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(7)).DateTime);
            if (!Text(req.LotNumber,80) || !Text(req.SupplierName,200) || !Text(req.DocumentNumber,100) || !Text(req.StorageLocation,200) ||
                req.VaccineId<=0 || req.Quantity<=0 || !req.InspectionConfirmed || req.ManufacturedOn==default || req.ExpiresOn==default ||
                req.ManufacturedOn>today || req.ManufacturedOn>req.ExpiresOn || req.ExpiresOn<today)
                throw new IdentityFailure(400,"validation");
            var lot = req.LotNumber.Trim();
            // Lock the global lot key (also for different warehouse accounts); never top up an existing lot.
            if (await db.Set<LoVacXin>().FromSqlInterpolated($"SELECT * FROM LoVacXin WITH (UPDLOCK,HOLDLOCK) WHERE SoLo={lot}").AnyAsync(ct))
                throw new IdentityFailure(409,"inventoryLotExists");
            if (!await db.Vaccines.AnyAsync(v=>v.MaVacXin==req.VaccineId && v.TrangThai=="Active",ct))
                throw new IdentityFailure(400,"inventoryVaccineUnavailable");
            db.Add(new LoVacXin { SoLo=lot, MaVacXin=req.VaccineId, NgaySanXuat=req.ManufacturedOn, HanSuDung=req.ExpiresOn,
                SoLuongBanDau=req.Quantity, SoLuongTon=req.Quantity, TrangThai="Active" });
            var movement = new BienDongKho { SoLo=lot, MaNhanVien=actor.Employee!.MaNhanVien, LoaiBienDong="NhapKho",
                SoLuong=req.Quantity, ThoiDiem=now.UtcDateTime };
            db.Add(movement);
            try {
                await db.SaveChangesAsync(ct);
                db.Add(new InventoryReceipt { SoLo=lot, SupplierName=req.SupplierName.Trim(), DocumentNumber=req.DocumentNumber.Trim(),
                    StorageLocation=req.StorageLocation.Trim(), MovementId=movement.MaBienDong });
                var audit=IdentityTransactions.Audit(db,"inventory.received",context.TraceIdentifier,actor.Account.MaTaiKhoan,session:actor.Session.Id);
                audit.ResourceType="InventoryReceipt"; audit.ResourceId=lot;
                await db.SaveChangesAsync(ct);
            } catch(DbUpdateException e) when(e.InnerException is SqlException sql && sql.Number is 2601 or 2627) {
                throw new IdentityFailure(409,"inventoryLotExists");
            } catch(DbUpdateException) { throw new IdentityFailure(503,"dependencyUnavailable"); }
            var result=await Read(db,lot,ct); await tx.CommitAsync(ct);
            return Results.Created("/api/v1/inventory/receipts?lotNumber="+Uri.EscapeDataString(lot),result);
        });
    }
}
