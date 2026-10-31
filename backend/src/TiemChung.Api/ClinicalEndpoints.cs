using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class ClinicalEndpoints
{
    private const string Eligible = "Đủ điều kiện";
    private static async Task<IdentityActor> RequireDoctor(HttpContext context, IdentityAuthorization auth, bool write, CancellationToken ct)
    {
        var actor = await auth.RequireAsync(context, write, ct);
        if (actor.IsCustomer || actor.Employee?.ChucDanh != "BacSi") throw new IdentityFailure(403, "permissionDenied");
        return actor;
    }

    private static IQueryable<LichHenTiem> Sources(TiemChungDbContext db) =>
        from a in db.Set<LichHenTiem>().AsNoTracking()
        join r in db.Registrations on a.MaDangKy equals r.MaDangKy
        where r.TrangThai == "DaDangKy" && a.MaNguoiDuocTiem == r.MaNguoiDuocTiem &&
            db.Recipients.Any(p => p.MaNguoiDuocTiem == r.MaNguoiDuocTiem && p.MaKhachHang == r.MaKhachHang) &&
            ((a.MaChiTietVacXin != null && a.MaChiTietGoi == null && db.RegistrationVaccineItems.Any(s => s.MaDangKy == r.MaDangKy && s.MaChiTietVacXin == a.MaChiTietVacXin)) ||
             (a.MaChiTietGoi != null && a.MaChiTietVacXin == null && db.RegistrationPackageItems.Any(s => s.MaDangKy == r.MaDangKy && s.MaChiTietGoi == a.MaChiTietGoi && db.Set<GoiVacXin_VacXin>().Any(v => v.MaGoi == s.MaGoi))))
        select a;

    private static async Task<ClinicalVaccineDto[]> Vaccines(TiemChungDbContext db, AppointmentDto row, CancellationToken ct)
    {
        var ids = row.ItemKind == "vaccine" ? db.Vaccines.Where(v => v.MaVacXin == row.ServiceId).Select(v => v.MaVacXin)
            : db.Set<GoiVacXin_VacXin>().Where(v => v.MaGoi == row.ServiceId).Select(v => v.MaVacXin);
        return await db.Vaccines.AsNoTracking().Where(v => ids.Contains(v.MaVacXin)).OrderBy(v => v.MaVacXin)
            .Select(v => new ClinicalVaccineDto(v.MaVacXin, v.TenVacXin)).ToArrayAsync(ct);
    }
    private static async Task<AppointmentDto> Appointment(TiemChungDbContext db, int id, CancellationToken ct)
    {
        if (id <= 0) throw new IdentityFailure(400, "validation");
        return await AppointmentEndpoints.Query(db, Sources(db).Where(a => a.MaLichHen == id)).SingleOrDefaultAsync(ct)
            ?? throw new IdentityFailure(404, "resourceNotFound");
    }

    private static async Task<ClinicalAssessmentDto?> Assessment(TiemChungDbContext db, int id, CancellationToken ct)
    {
        var row = await db.Set<PhieuKhamSangLoc>().AsNoTracking().SingleOrDefaultAsync(x => x.MaLichHen == id, ct);
        if (row is null) return null;
        var doctor = await db.Set<NhanVien>().AsNoTracking().Where(x => x.MaNhanVien == row.MaNhanVien)
            .Select(x => new RegistrationProfileDto(x.MaNhanVien, x.HoTen)).SingleAsync(ct);
        var indications = await (from i in db.Set<ChiDinhTiem>().AsNoTracking()
            join v in db.Vaccines on i.MaVacXin equals v.MaVacXin
            where i.MaPhieuKham == row.MaPhieuKham orderby i.MaChiDinh
            select new ClinicalIndicationDto(i.MaChiDinh, v.MaVacXin, v.TenVacXin, i.KetQuaChiDinh,
                new DateTimeOffset(DateTime.SpecifyKind(i.ThoiGian, DateTimeKind.Utc)))).ToArrayAsync(ct);
        return new(row.MaPhieuKham, id, row.MaDangKy, row.MaNguoiDuocTiem, doctor, row.ThoiDiemKham!.Value,
            row.KetLuan, row.LyDoHoan, row.ChiSoSinhTon, row.TienSuBenhLy, indications);
    }

    public static void MapClinicalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/clinical/appointments").WithTags("Clinical");
        group.AddEndpointFilter(async (invocation, next) => {
            try { return await next(invocation); }
            catch (DbUpdateException e) when (e.InnerException is SqlException sql && sql.Number is 2601 or 2627 &&
                sql.Message.Contains("IX_PhieuKhamSangLoc_MaLichHen", StringComparison.Ordinal)) {
                throw new IdentityFailure(409, "assessmentAlreadyRecorded");
            }
            catch (DbUpdateException) { throw new IdentityFailure(503, "dependencyUnavailable"); }
        });
        group.MapGet("", async (HttpContext context, IdentityAuthorization auth, TiemChungDbContext db,
            string? search, int? page, int? pageSize, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            await RequireDoctor(context, auth, false, ct);
            search = search?.Trim();
            if (search?.Length > 100 || page is <= 0 || pageSize is <= 0 or > 100 ||
                (long)((page ?? 1)-1)*(pageSize ?? 25) > int.MaxValue) throw new IdentityFailure(400, "validation");
            var code = search?.StartsWith("LH-", StringComparison.OrdinalIgnoreCase) == true ? search[3..] : search;
            int.TryParse(code, out var id);
            var rows = Sources(db);
            if (!string.IsNullOrEmpty(search)) rows = rows.Where(a => a.MaLichHen == id ||
                db.Recipients.Any(p => p.MaNguoiDuocTiem == a.MaNguoiDuocTiem && p.HoTen.Contains(search)));
            var items = await AppointmentEndpoints.Query(db, rows.OrderByDescending(a => a.MaLichHen)
                .Skip(((page ?? 1)-1)*(pageSize ?? 25)).Take(pageSize ?? 25)).ToArrayAsync(ct);
            await tx.CommitAsync(ct);
            return Results.Ok(new AppointmentListResponse(items));
        });
        group.MapGet("/{id:int}", async (int id, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            await RequireDoctor(context, auth, false, ct);
            var row = await Appointment(db, id, ct);
            var vaccines = await Vaccines(db, row, ct);
            await tx.CommitAsync(ct);
            return Results.Ok(new ClinicalAppointmentDto(row, vaccines));
        });
        group.MapGet("/{id:int}/assessment", async (int id, HttpContext context, IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            await RequireDoctor(context, auth, false, ct);
            await Appointment(db, id, ct);
            var result = await Assessment(db, id, ct);
            await tx.CommitAsync(ct);
            return Results.Ok(new ClinicalAssessmentResponse(result));
        });
        group.MapPost("/{id:int}/assessment", async (int id, CreateClinicalAssessmentRequest req, HttpContext context,
            IdentityAuthorization auth, TiemChungDbContext db, CancellationToken ct) => {
            await using var tx = await IdentityTransactions.BeginAsync(db, ct);
            var actor = await RequireDoctor(context, auth, true, ct);
            if (id <= 0 || req.Conclusion is not (Eligible or "Tạm hoãn" or "Chống chỉ định") || req.Reason?.Trim().Length > 500 ||
                req.VaccineIds is null || req.VaccineIds.Length > 100 || req.VaccineIds.Any(v => v <= 0) ||
                req.VaccineIds.Distinct().Count() != req.VaccineIds.Length ||
                (req.Conclusion == Eligible ? req.VaccineIds.Length == 0 : req.VaccineIds.Length != 0 || string.IsNullOrWhiteSpace(req.Reason)))
                throw new IdentityFailure(400, "validation");
            await db.Set<LichHenTiem>().FromSqlInterpolated($"SELECT * FROM LichHenTiem WITH (UPDLOCK,HOLDLOCK) WHERE MaLichHen={id}").SingleOrDefaultAsync(ct);
            var appointment = await Appointment(db, id, ct);
            if (await db.Set<PhieuKhamSangLoc>().AnyAsync(x => x.MaLichHen == id, ct)) throw new IdentityFailure(409, "assessmentAlreadyRecorded");
            var allowed = await Vaccines(db, appointment, ct);
            if (req.VaccineIds.Any(v => !allowed.Any(a => a.Id == v))) throw new IdentityFailure(400, "validation");
            var now = DateTimeOffset.UtcNow;
            var row = new PhieuKhamSangLoc {
                MaLichHen=id, MaDangKy=appointment.RegistrationId, MaNguoiDuocTiem=appointment.Recipient.Id,
                MaNhanVien=actor.Employee!.MaNhanVien, ThoiDiemKham=now, KetLuan=req.Conclusion,
                LyDoHoan=string.IsNullOrWhiteSpace(req.Reason) ? null : req.Reason.Trim()
            };
            db.Add(row); await db.SaveChangesAsync(ct);
            foreach (var vaccine in req.VaccineIds) db.Add(new ChiDinhTiem {
                MaPhieuKham=row.MaPhieuKham, MaVacXin=vaccine, KetQuaChiDinh="DuocTiem", ThoiGian=now.UtcDateTime, GhiChu=""
            });
            var audit=IdentityTransactions.Audit(db, "clinical.assessed", context.TraceIdentifier, actor.Account.MaTaiKhoan, session:actor.Session.Id);
            audit.ResourceType="ClinicalAssessment"; audit.ResourceId=row.MaPhieuKham.ToString(CultureInfo.InvariantCulture);
            await db.SaveChangesAsync(ct);
            var result=await Assessment(db,id,ct);
            await tx.CommitAsync(ct);
            return Results.Created($"/api/v1/clinical/appointments/{id}/assessment",result);
        }).Produces<ClinicalAssessmentDto>(201);
    }
}
