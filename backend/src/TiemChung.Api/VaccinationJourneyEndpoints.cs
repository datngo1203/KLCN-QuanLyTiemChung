using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class VaccinationJourneyEndpoints
{
    public static void MapVaccinationJourneyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/vaccination-journeys").WithTags("Vaccinations");
        group.MapGet("", async (HttpContext context, IdentityAuthorization auth, TiemChungDbContext db,
            int? page, int? pageSize, CancellationToken ct) => {
            var actor = await PaymentEndpoints.RequireCustomer(context, auth, false, ct);
            var size = pageSize ?? 25;
            var offset = ((long)(page ?? 1) - 1) * size;
            if (page is <= 0 || size is <= 0 or > 100 || offset > int.MaxValue)
                throw new IdentityFailure(400, "validation");

            var rows = await RegistrationEndpoints.Scope(db, actor)
                .OrderByDescending(r => r.NgayDangKy).ThenByDescending(r => r.MaDangKy)
                .Skip((int)offset).Take(size).ToArrayAsync(ct);
            return Results.Ok(new VaccinationJourneyListResponse(await Project(db, actor, rows, ct)));
        });
        group.MapGet("/{registrationId:int}", async (HttpContext context, IdentityAuthorization auth,
            TiemChungDbContext db, int registrationId, CancellationToken ct) => {
            var actor = await PaymentEndpoints.RequireCustomer(context, auth, false, ct);
            var row = await RegistrationEndpoints.Scope(db, actor).SingleOrDefaultAsync(r => r.MaDangKy == registrationId, ct)
                ?? throw new IdentityFailure(403, "permissionDenied");
            return Results.Ok((await Project(db, actor, [row], ct)).Single());
        });
    }

    private static async Task<VaccinationJourneyDto[]> Project(TiemChungDbContext db, IdentityActor actor,
        DangKyDichVu[] rows, CancellationToken ct)
    {
        if (rows.Length == 0) return [];
        var ids = rows.Select(r => r.MaDangKy).ToArray();
        var registrations = RegistrationEndpoints.Scope(db, actor).Where(r => ids.Contains(r.MaDangKy));
        var appointments = await AppointmentEndpoints.Query(db,
            from a in db.Set<LichHenTiem>()
            join r in registrations on a.MaDangKy equals r.MaDangKy
            where a.MaNguoiDuocTiem == r.MaNguoiDuocTiem &&
                ((a.MaChiTietVacXin != null && a.MaChiTietGoi == null &&
                    db.RegistrationVaccineItems.Any(s => s.MaDangKy == r.MaDangKy && s.MaChiTietVacXin == a.MaChiTietVacXin)) ||
                 (a.MaChiTietGoi != null && a.MaChiTietVacXin == null &&
                    db.RegistrationPackageItems.Any(s => s.MaDangKy == r.MaDangKy && s.MaChiTietGoi == a.MaChiTietGoi)))
            select a).ToArrayAsync(ct);
        var shots = await VaccinationHistoryEndpoints.Query(db, registrations).ToArrayAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var result = new List<VaccinationJourneyDto>();
        foreach (var row in rows)
        {
            var payment = await PaymentEndpoints.Detail(db, row, actor, ct);
            var schedule = appointments.Where(a => a.RegistrationId == row.MaDangKy)
                .OrderBy(a => a.ScheduledAt).ThenBy(a => a.Id).ToArray();
            var doses = await RegisteredDoseEntitlements.Read(db, row.MaDangKy, ct);
            var confirmed = schedule.Where(a => a.Status == "DaXepLich" && a.ScheduledAt > now).ToArray();
            var progress = doses.Select(d => new VaccinationProgressDto(d.VaccineId, d.Name, d.Registered,
                d.Administered, d.Remaining, d.Remaining is > 0 ? confirmed.FirstOrDefault(a =>
                    a.ItemKind == "vaccine" ? a.ServiceId == d.VaccineId :
                    payment.Items.Any(i => i.Kind == "package" && i.Id == a.RegistrationItemId &&
                        i.Components?.Any(c => c.VaccineId == d.VaccineId) == true)) : null)).ToArray();
            result.Add(new(row.MaDangKy, new DateTimeOffset(DateTime.SpecifyKind(row.NgayDangKy, DateTimeKind.Utc)),
                row.ThoiGianDuKien is { } preferred ? DateOnly.FromDateTime(preferred) : null,
                payment, schedule, confirmed.FirstOrDefault(),
                shots.Where(s => s.RegistrationId == row.MaDangKy).ToArray()) { Progress = progress, PreferredAt = row.ThoiGianDuKien, PreferredTimeSpecified = row.CoGioDuKien });
        }
        return result.ToArray();
    }
}
