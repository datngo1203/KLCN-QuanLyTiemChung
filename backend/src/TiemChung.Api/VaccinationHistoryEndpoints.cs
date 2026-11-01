using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class VaccinationHistoryEndpoints
{
    public static void MapVaccinationHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/vaccinations/history", async (HttpContext context, IdentityAuthorization auth,
            TiemChungDbContext db, int? recipientId, int? page, int? pageSize, CancellationToken ct) => {
            IdentityActor actor;
            try { actor = await auth.RequireAsync(context, false, ct); }
            catch (IdentityFailure failure) when (failure.Code == "emailVerificationRequired") {
                throw new IdentityFailure(403, "permissionDenied");
            }
            if (!actor.IsCustomer) throw new IdentityFailure(403, "permissionDenied");
            if (recipientId is <= 0 || page is <= 0 || pageSize is <= 0 or > 100 ||
                (long)((page ?? 1) - 1) * (pageSize ?? 25) > int.MaxValue)
                throw new IdentityFailure(400, "validation");

            // Validate historical source/ownership, without reapplying today's clinical, payment or lot eligibility.
            var rows = from m in db.Set<MuiTiem>().AsNoTracking()
                join i in db.Set<ChiDinhTiem>() on m.MaChiDinh equals i.MaChiDinh
                join p in db.Set<PhieuKhamSangLoc>() on i.MaPhieuKham equals p.MaPhieuKham
                join a in db.Set<LichHenTiem>() on p.MaLichHen equals a.MaLichHen
                join r in RegistrationEndpoints.Scope(db, actor) on a.MaDangKy equals r.MaDangKy
                join l in db.Set<LoVacXin>() on m.SoLo equals l.SoLo
                where m.TrangThai == "DaTiem" && p.ThoiDiemKham != null &&
                    m.MaNguoiDuocTiem == r.MaNguoiDuocTiem && a.MaNguoiDuocTiem == r.MaNguoiDuocTiem &&
                    p.MaNguoiDuocTiem == r.MaNguoiDuocTiem && p.MaDangKy == r.MaDangKy && l.MaVacXin == i.MaVacXin &&
                    (!recipientId.HasValue || m.MaNguoiDuocTiem == recipientId) &&
                    ((a.MaChiTietVacXin != null && a.MaChiTietGoi == null &&
                        db.RegistrationVaccineItems.Any(s => s.MaChiTietVacXin == a.MaChiTietVacXin && s.MaDangKy == r.MaDangKy && s.MaVacXin == i.MaVacXin)) ||
                     (a.MaChiTietGoi != null && a.MaChiTietVacXin == null &&
                        db.RegistrationPackageItems.Any(s => s.MaChiTietGoi == a.MaChiTietGoi && s.MaDangKy == r.MaDangKy &&
                            db.Set<GoiVacXin_VacXin>().Any(c => c.MaGoi == s.MaGoi && c.MaVacXin == i.MaVacXin && c.SoLuong > 0))))
                select new { Shot = m, Indication = i, Assessment = p };
            var items = await (from row in rows.OrderByDescending(x => x.Shot.ThoiDiemTiem).ThenByDescending(x => x.Shot.MaMuiTiem)
                    .Skip(((page ?? 1) - 1) * (pageSize ?? 25)).Take(pageSize ?? 25)
                join recipient in db.Recipients on row.Shot.MaNguoiDuocTiem equals recipient.MaNguoiDuocTiem
                join vaccine in db.Vaccines on row.Indication.MaVacXin equals vaccine.MaVacXin
                orderby row.Shot.ThoiDiemTiem descending, row.Shot.MaMuiTiem descending
                select new VaccinationHistoryDto(row.Shot.MaMuiTiem, row.Assessment.MaLichHen!.Value, row.Assessment.MaDangKy,
                    new(recipient.MaNguoiDuocTiem, recipient.HoTen), new(vaccine.MaVacXin, vaccine.TenVacXin),
                    row.Shot.SoLo, row.Shot.ViTriTiem,
                    new DateTimeOffset(DateTime.SpecifyKind(row.Shot.ThoiDiemTiem, DateTimeKind.Utc)), row.Shot.TrangThai))
                .ToArrayAsync(ct);
            return Results.Ok(new VaccinationHistoryResponse(items));
        }).WithTags("Vaccinations");
    }
}
