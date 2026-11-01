using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class VaccinationEndpoints
{
    public static DateOnly FacilityDate(DateTimeOffset utc) => DateOnly.FromDateTime(utc.ToOffset(TimeSpan.FromHours(7)).DateTime);

    private static async Task<IdentityActor> RequireNurse(HttpContext context, IdentityAuthorization auth, bool write, CancellationToken ct)
    {
        var actor = await auth.RequireAsync(context, write, ct);
        if (actor.IsCustomer || actor.Employee?.ChucDanh != "DieuDuong") throw new IdentityFailure(403, "permissionDenied");
        return actor;
    }

    internal static IQueryable<ChiDinhTiem> Sources(TiemChungDbContext db) =>
        from i in db.Set<ChiDinhTiem>().AsNoTracking()
        join p in db.Set<PhieuKhamSangLoc>() on i.MaPhieuKham equals p.MaPhieuKham
        join a in ClinicalEndpoints.Sources(db) on p.MaLichHen equals a.MaLichHen
        join r in db.Registrations on a.MaDangKy equals r.MaDangKy
        where p.MaDangKy == r.MaDangKy && p.MaNguoiDuocTiem == a.MaNguoiDuocTiem && p.ThoiDiemKham != null &&
            p.KetLuan == "Đủ điều kiện" && i.KetQuaChiDinh == "DuocTiem" &&
            ((a.MaChiTietVacXin != null && db.RegistrationVaccineItems.Any(s => s.MaChiTietVacXin == a.MaChiTietVacXin && s.MaDangKy == r.MaDangKy && s.MaVacXin == i.MaVacXin)) ||
             (a.MaChiTietGoi != null && db.RegistrationPackageItems.Any(s => s.MaChiTietGoi == a.MaChiTietGoi && s.MaDangKy == r.MaDangKy &&
                db.Set<GoiVacXin_VacXin>().Any(c => c.MaGoi == s.MaGoi && c.MaVacXin == i.MaVacXin && c.SoLuong > 0))))
        select i;

    private static IQueryable<VaccinationIndicationDto> Query(TiemChungDbContext db, IQueryable<ChiDinhTiem> rows) =>
        from i in rows
        join p in db.Set<PhieuKhamSangLoc>() on i.MaPhieuKham equals p.MaPhieuKham
        join a in db.Set<LichHenTiem>() on p.MaLichHen equals a.MaLichHen
        join r in db.Registrations on a.MaDangKy equals r.MaDangKy
        join recipient in db.Recipients on a.MaNguoiDuocTiem equals recipient.MaNguoiDuocTiem
        join customer in db.Customers on r.MaKhachHang equals customer.MaKhachHang
        join v in db.Vaccines on i.MaVacXin equals v.MaVacXin
        join doctor in db.Set<NhanVien>() on p.MaNhanVien equals doctor.MaNhanVien
        select new VaccinationIndicationDto(i.MaChiDinh, a.MaLichHen, r.MaDangKy, new(customer.MaKhachHang, customer.HoTen),
            new(recipient.MaNguoiDuocTiem, recipient.HoTen), new(v.MaVacXin, v.TenVacXin),
            new DateTimeOffset(DateTime.SpecifyKind(a.ThoiGianHen, DateTimeKind.Utc)), a.DiemTiem,
            new(doctor.MaNhanVien, doctor.HoTen), p.ThoiDiemKham.GetValueOrDefault());

    private static async Task<VaccinationIndicationDto> Source(TiemChungDbContext db, ChiDinhTiem indication, PhieuKhamSangLoc assessment, CancellationToken ct)
    {
        if (assessment.KetLuan != "Đủ điều kiện" || indication.KetQuaChiDinh != "DuocTiem")
            throw new IdentityFailure(409, "vaccinationNotIndicated");
        return await Query(db, Sources(db).Where(i => i.MaChiDinh == indication.MaChiDinh)).SingleOrDefaultAsync(ct)
            ?? throw new IdentityFailure(409, "vaccinationSourceConflict");
    }

    private static async Task RequirePayment(TiemChungDbContext db, DangKyDichVu registration, CancellationToken ct)
    {
        var money = (await PaymentEndpoints.Summaries(db, [registration], ct)).Single();
        if (money.TotalDue == 0) throw new IdentityFailure(409, "vaccinationZeroTotalNotSupported");
        if (money.PaymentId is null || money.RemainingAmount != 0) throw new IdentityFailure(409, "vaccinationPaymentRequired");
    }

    private static IQueryable<LoVacXin> UsableLots(TiemChungDbContext db, int vaccineId, DateOnly day) =>
        db.Set<LoVacXin>().Where(l => l.MaVacXin == vaccineId && l.TrangThai == "Active" &&
            l.NgaySanXuat <= day && l.NgaySanXuat <= l.HanSuDung && l.HanSuDung >= day &&
            l.SoLuongBanDau > 0 && l.SoLuongTon > 0 && l.SoLuongTon <= l.SoLuongBanDau);

    private static Task<VaccinationDto?> Result(TiemChungDbContext db, int indicationId, CancellationToken ct) =>
        (from shot in db.Set<MuiTiem>().AsNoTracking()
         join i in db.Set<ChiDinhTiem>() on shot.MaChiDinh equals i.MaChiDinh
         join p in db.Set<PhieuKhamSangLoc>() on i.MaPhieuKham equals p.MaPhieuKham
         join recipient in db.Recipients on shot.MaNguoiDuocTiem equals recipient.MaNguoiDuocTiem
         join vaccine in db.Vaccines on i.MaVacXin equals vaccine.MaVacXin
         join nurse in db.Set<NhanVien>() on shot.MaNhanVien equals nurse.MaNhanVien
         where i.MaChiDinh == indicationId && p.MaLichHen != null
         select new VaccinationDto(shot.MaMuiTiem, i.MaChiDinh, p.MaLichHen.GetValueOrDefault(), p.MaDangKy,
             new(recipient.MaNguoiDuocTiem, recipient.HoTen), new(vaccine.MaVacXin, vaccine.TenVacXin), shot.SoLo,
             shot.ViTriTiem, new(nurse.MaNhanVien, nurse.HoTen),
             new DateTimeOffset(DateTime.SpecifyKind(shot.ThoiDiemTiem, DateTimeKind.Utc)), shot.TrangThai)).SingleOrDefaultAsync(ct);

    public static void MapVaccinationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/vaccinations").WithTags("Vaccinations");
        group.AddEndpointFilter(async (invocation, next) => {
            try { return await next(invocation); }
            catch (DbUpdateException e) when (e.InnerException is SqlException sql && sql.Number is 2601 or 2627 &&
                sql.Message.Contains("IX_MuiTiem_MaChiDinh", StringComparison.Ordinal)) {
                throw new IdentityFailure(409, "vaccinationAlreadyRecorded");
            }
            catch (DbUpdateException) { throw new IdentityFailure(503, "dependencyUnavailable"); }
        });
        group.MapGet("/eligible", async (HttpContext context, IdentityAuthorization auth, TiemChungDbContext db,
            string? search, int? page, int? pageSize, CancellationToken ct) => {
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
            await RequireNurse(context, auth, false, ct);
            search = search?.Trim();
            if (search?.Length > 100 || page is <= 0 || pageSize is <= 0 or > 100 ||
                (long)((page ?? 1)-1)*(pageSize ?? 25) > int.MaxValue) throw new IdentityFailure(400, "validation");
            var code = search?.StartsWith("LH-", StringComparison.OrdinalIgnoreCase) == true ? search[3..] : search;
            int.TryParse(code, out var id);
            var query = Sources(db).Where(i => !db.Set<MuiTiem>().Any(m => m.MaChiDinh == i.MaChiDinh));
            if (!string.IsNullOrEmpty(search)) query = query.Where(i => db.Set<PhieuKhamSangLoc>().Any(p => p.MaPhieuKham == i.MaPhieuKham &&
                (p.MaLichHen == id || db.Recipients.Any(r => r.MaNguoiDuocTiem == p.MaNguoiDuocTiem && r.HoTen.Contains(search)))));
            var items = await Query(db, query.OrderByDescending(i => i.MaChiDinh).Skip(((page ?? 1)-1)*(pageSize ?? 25)).Take(pageSize ?? 25)).ToArrayAsync(ct);
            await tx.CommitAsync(ct);
            return Results.Ok(new VaccinationListResponse(items));
        });
        group.MapGet("/indications/{id:int}", async (int id, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
            await RequireNurse(context, auth, false, ct);
            if (id <= 0) throw new IdentityFailure(400, "validation");
            var indication = await db.Set<ChiDinhTiem>().AsNoTracking().SingleOrDefaultAsync(i => i.MaChiDinh == id, ct)
                ?? throw new IdentityFailure(404, "resourceNotFound");
            if (await db.Set<MuiTiem>().AnyAsync(m => m.MaChiDinh == id, ct)) throw new IdentityFailure(409, "vaccinationAlreadyRecorded");
            var assessment = await db.Set<PhieuKhamSangLoc>().AsNoTracking().SingleAsync(p => p.MaPhieuKham == indication.MaPhieuKham, ct);
            var source = await Source(db, indication, assessment, ct);
            var registration = await db.Registrations.AsNoTracking().SingleAsync(r => r.MaDangKy == source.RegistrationId, ct);
            await RequirePayment(db, registration, ct);
            var day = FacilityDate(DateTimeOffset.UtcNow);
            var lots = await UsableLots(db, indication.MaVacXin, day).AsNoTracking().OrderBy(l => l.SoLo)
                .Select(l => new VaccinationLotDto(l.SoLo, l.NgaySanXuat, l.HanSuDung, l.SoLuongTon)).ToArrayAsync(ct);
            await tx.CommitAsync(ct);
            return Results.Ok(new VaccinationDetailDto(source, day, lots));
        });
        group.MapGet("/indications/{id:int}/result", async (int id, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
            await RequireNurse(context, auth, false, ct);
            if (id <= 0) throw new IdentityFailure(400, "validation");
            if (!await db.Set<ChiDinhTiem>().AnyAsync(i => i.MaChiDinh == id, ct)) throw new IdentityFailure(404, "resourceNotFound");
            var result = await Result(db, id, ct);
            await tx.CommitAsync(ct);
            return Results.Ok(new VaccinationResultResponse(result));
        });
        group.MapPost("", async (RecordVaccinationRequest req, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireNurse(context, auth, true, ct);
            if (req.IndicationId <= 0 || string.IsNullOrWhiteSpace(req.LotNumber) || req.LotNumber.Trim().Length > 80 ||
                string.IsNullOrWhiteSpace(req.InjectionSite) || req.InjectionSite.Trim().Length > 100) throw new IdentityFailure(400, "validation");
            // Lock the indication first: no shared-read -> update-lock conversion between duplicate requests.
            var indication = await db.Set<ChiDinhTiem>().FromSqlInterpolated(
                $"SELECT * FROM ChiDinhTiem WITH (UPDLOCK,HOLDLOCK) WHERE MaChiDinh={req.IndicationId}").SingleOrDefaultAsync(ct)
                ?? throw new IdentityFailure(404, "resourceNotFound");
            if (await db.Set<MuiTiem>().FromSqlInterpolated(
                $"SELECT * FROM MuiTiem WITH (UPDLOCK,HOLDLOCK) WHERE MaChiDinh={indication.MaChiDinh}").AnyAsync(ct))
                throw new IdentityFailure(409, "vaccinationAlreadyRecorded");
            var assessment = await db.Set<PhieuKhamSangLoc>().AsNoTracking().SingleAsync(p => p.MaPhieuKham == indication.MaPhieuKham, ct);
            var registration = await db.Registrations.FromSqlInterpolated(
                $"SELECT * FROM DangKyDichVu WITH (UPDLOCK,HOLDLOCK) WHERE MaDangKy={assessment.MaDangKy}").SingleAsync(ct);
            if (assessment.MaLichHen is not {} appointmentId) throw new IdentityFailure(409, "vaccinationSourceConflict");
            await db.Set<LichHenTiem>().FromSqlInterpolated(
                $"SELECT * FROM LichHenTiem WITH (UPDLOCK,HOLDLOCK) WHERE MaLichHen={appointmentId}").SingleAsync(ct);
            var source = await Source(db, indication, assessment, ct);
            await RequirePayment(db, registration, ct);
            var now = DateTimeOffset.UtcNow;
            var day = FacilityDate(now);
            var number = req.LotNumber.Trim();
            var changed = await UsableLots(db, indication.MaVacXin, day).Where(l => l.SoLo == number)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.SoLuongTon, l => l.SoLuongTon - 1), ct);
            if (changed != 1) throw new IdentityFailure(409, "vaccinationLotUnavailable");
            var lot = await db.Set<LoVacXin>().AsNoTracking().SingleAsync(l => l.SoLo == number, ct);
            var shot = new MuiTiem { MaChiDinh=indication.MaChiDinh, MaNguoiDuocTiem=source.Recipient.Id, SoLo=lot.SoLo,
                MaNhanVien=actor.Employee!.MaNhanVien, ThoiDiemTiem=now.UtcDateTime, ViTriTiem=req.InjectionSite.Trim(), TrangThai="DaTiem" };
            db.Add(shot); await db.SaveChangesAsync(ct);
            db.Add(new BienDongKho { MaMuiTiem=shot.MaMuiTiem, SoLo=shot.SoLo, MaNhanVien=shot.MaNhanVien,
                LoaiBienDong="XuatDoTiem", SoLuong=1, ThoiDiem=shot.ThoiDiemTiem });
            var audit = IdentityTransactions.Audit(db, "vaccinations.recorded", context.TraceIdentifier, actor.Account.MaTaiKhoan, session:actor.Session.Id);
            audit.ResourceType="Vaccination"; audit.ResourceId=shot.MaMuiTiem.ToString(CultureInfo.InvariantCulture);
            await db.SaveChangesAsync(ct);
            var result = await Result(db, indication.MaChiDinh, ct);
            await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/vaccinations/indications/{indication.MaChiDinh}/result", result);
        }).Produces<VaccinationDto>(201);
    }
}
