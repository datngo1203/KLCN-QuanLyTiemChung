using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

internal sealed record RegisteredDose(int VaccineId, string Name, int? Registered, int Administered)
{
    public int? Remaining => Registered is { } total ? Math.Max(total - Administered, 0) : null;
    public string? BlockReason => Registered is null ? "doseEntitlementUnavailable"
        : Remaining == 0 ? "doseEntitlementExhausted" : null;
}

internal static class RegisteredDoseEntitlements
{
    internal static async Task<RegisteredDose[]> Read(TiemChungDbContext db, int registrationId, CancellationToken ct)
    {
        var singles = await db.RegistrationVaccineItems.AsNoTracking().Where(i => i.MaDangKy == registrationId).ToArrayAsync(ct);
        var packages = await db.RegistrationPackageItems.AsNoTracking().Where(i => i.MaDangKy == registrationId).ToArrayAsync(ct);
        var packageIds = packages.Select(i => i.MaGoi).ToArray();
        // Referenced package composition is immutable under the existing Catalog contract.
        var components = await db.Set<GoiVacXin_VacXin>().AsNoTracking().Where(i => packageIds.Contains(i.MaGoi)).ToArrayAsync(ct);
        var valid = singles.All(i => i.SoLuong is >= 1 and <= 100) && packages.All(i =>
            i.SoLuongGoi is >= 1 and <= 100 && components.Any(c => c.MaGoi == i.MaGoi) &&
            components.Where(c => c.MaGoi == i.MaGoi).All(c => c.SoLuong is >= 1 and <= 100));
        var quantities = singles.Select(i => (Id: i.MaVacXin, Quantity: i.SoLuong))
            .Concat(from p in packages join c in components on p.MaGoi equals c.MaGoi
                select (Id: c.MaVacXin, Quantity: p.SoLuongGoi * c.SoLuong)).ToArray();
        var ids = quantities.Select(i => i.Id).Distinct().ToArray();
        var vaccines = await db.Vaccines.AsNoTracking().Where(v => ids.Contains(v.MaVacXin)).ToArrayAsync(ct);
        var counts = await (from m in db.Set<MuiTiem>().AsNoTracking()
            join i in db.Set<ChiDinhTiem>() on m.MaChiDinh equals i.MaChiDinh
            join p in db.Set<PhieuKhamSangLoc>() on i.MaPhieuKham equals p.MaPhieuKham
            join r in db.Registrations on p.MaDangKy equals r.MaDangKy
            where r.MaDangKy == registrationId && m.TrangThai == "DaTiem" &&
                p.MaNguoiDuocTiem == r.MaNguoiDuocTiem && m.MaNguoiDuocTiem == r.MaNguoiDuocTiem
            group m by i.MaVacXin into g select new { Id = g.Key, Count = g.Count() }).ToArrayAsync(ct);
        return vaccines.OrderBy(v => v.MaVacXin).Select(v => new RegisteredDose(v.MaVacXin, v.TenVacXin,
            valid ? quantities.Where(i => i.Id == v.MaVacXin).Sum(i => i.Quantity) : null,
            counts.SingleOrDefault(i => i.Id == v.MaVacXin)?.Count ?? 0)).ToArray();
    }

    internal static string? TimeBlock(DateTimeOffset scheduledAt) => scheduledAt > DateTimeOffset.UtcNow ? "appointmentNotStarted" : null;

    internal static void RequireAvailable(RegisteredDose[] doses, int vaccineId)
    {
        var reason = doses.SingleOrDefault(d => d.VaccineId == vaccineId)?.BlockReason;
        if (!doses.Any(d => d.VaccineId == vaccineId)) reason = "doseEntitlementUnavailable";
        if (reason is not null) throw new IdentityFailure(409, reason);
    }
}
