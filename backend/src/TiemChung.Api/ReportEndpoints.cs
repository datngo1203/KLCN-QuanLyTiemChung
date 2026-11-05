using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reports");

        group.MapGet("/monthly-revenue", async (
            HttpContext context,
            IdentityAuthorization auth,
            TiemChungDbContext db,
            string? from,
            string? to,
            CancellationToken ct) =>
        {
            var actor = await auth.RequireAsync(context, false, ct);
            actor.RequirePermission("XemBaoCao");

            var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

            DateOnly fromDate;
            if (from is not null)
            {
                if (!TryParseDate(from, false, out fromDate))
                    throw new IdentityFailure(400, "validation");
            }
            else
            {
                var startSixMonthsAgo = new DateOnly(todayVn.Year, todayVn.Month, 1).AddMonths(-5);
                fromDate = startSixMonthsAgo;
            }

            DateOnly toDate;
            if (to is not null)
            {
                if (!TryParseDate(to, true, out toDate))
                    throw new IdentityFailure(400, "validation");
            }
            else
            {
                toDate = todayVn;
            }

            if (fromDate > toDate || toDate > todayVn)
                throw new IdentityFailure(400, "validation");

            var startUtc = new DateTime(fromDate.Year, fromDate.Month, fromDate.Day, 0, 0, 0, DateTimeKind.Utc).AddHours(-7);
            var endExclusiveUtc = new DateTime(toDate.Year, toDate.Month, toDate.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1).AddHours(-7);

            var payments = await db.Set<ThanhToan>().AsNoTracking()
                .Where(t => t.TrangThai == "ThanhCong" && t.ThoiDiem >= startUtc && t.ThoiDiem < endExclusiveUtc)
                .Select(t => new { t.ThoiDiem, t.SoTien })
                .ToListAsync(ct);

            var grouped = payments
                .GroupBy(p =>
                {
                    var vnTime = p.ThoiDiem.AddHours(7);
                    return (Year: vnTime.Year, Month: vnTime.Month);
                })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new MonthlyRevenueItemDto(
                    g.Key.Year,
                    g.Key.Month,
                    $"{g.Key.Month:D2}/{g.Key.Year}",
                    g.Sum(x => x.SoTien),
                    g.Count()
                ))
                .ToList();

            var totalRevenue = payments.Sum(p => p.SoTien);

            return Results.Ok(new MonthlyRevenueReportDto(
                fromDate,
                toDate,
                totalRevenue,
                IsSimulated: true,
                grouped
            ));
        }).WithTags("Reports");

        group.MapGet("/vaccine-doses", async (
            HttpContext context,
            IdentityAuthorization auth,
            TiemChungDbContext db,
            string? from,
            string? to,
            CancellationToken ct) =>
        {
            var actor = await auth.RequireAsync(context, false, ct);
            actor.RequirePermission("XemBaoCao");

            var todayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

            DateOnly fromDate;
            if (from is not null)
            {
                if (!TryParseDate(from, false, out fromDate))
                    throw new IdentityFailure(400, "validation");
            }
            else
            {
                var startSixMonthsAgo = new DateOnly(todayVn.Year, todayVn.Month, 1).AddMonths(-5);
                fromDate = startSixMonthsAgo;
            }

            DateOnly toDate;
            if (to is not null)
            {
                if (!TryParseDate(to, true, out toDate))
                    throw new IdentityFailure(400, "validation");
            }
            else
            {
                toDate = todayVn;
            }

            if (fromDate > toDate || toDate > todayVn)
                throw new IdentityFailure(400, "validation");

            var startUtc = new DateTime(fromDate.Year, fromDate.Month, fromDate.Day, 0, 0, 0, DateTimeKind.Utc).AddHours(-7);
            var endExclusiveUtc = new DateTime(toDate.Year, toDate.Month, toDate.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1).AddHours(-7);

            var query = await (
                from m in db.Set<MuiTiem>().AsNoTracking()
                where m.TrangThai == "DaTiem" && m.ThoiDiemTiem >= startUtc && m.ThoiDiemTiem < endExclusiveUtc
                join l in db.Set<LoVacXin>().AsNoTracking() on m.SoLo equals l.SoLo
                join v in db.Vaccines.AsNoTracking() on l.MaVacXin equals v.MaVacXin
                group m by new { v.MaVacXin, v.TenVacXin } into g
                select new VaccineDosesItemDto(
                    g.Key.MaVacXin,
                    g.Key.TenVacXin,
                    g.Count()
                )
            ).ToListAsync(ct);

            var ordered = query.OrderByDescending(x => x.DoseCount).ThenBy(x => x.VaccineName).ToList();
            var totalDoses = ordered.Sum(x => x.DoseCount);

            return Results.Ok(new VaccineDosesReportDto(
                fromDate,
                toDate,
                totalDoses,
                ordered
            ));
        }).WithTags("Reports");

        group.MapGet("/registration-statuses", async (
            HttpContext context,
            IdentityAuthorization auth,
            TiemChungDbContext db,
            string? from,
            string? to,
            CancellationToken ct
        ) =>
        {
            var actor = await auth.RequireAsync(context, false, ct);
            actor.RequirePermission("XemBaoCao");

            context.Response.Headers.CacheControl = "no-store";

            var nowUtc = DateTime.UtcNow;
            var todayVn = DateOnly.FromDateTime(nowUtc.AddHours(7));

            DateOnly fromDate;
            if (from is not null)
            {
                if (!TryParseDate(from, false, out fromDate))
                    throw new IdentityFailure(400, "validation");
            }
            else
            {
                var sixMonthsAgo = todayVn.AddMonths(-5);
                fromDate = new DateOnly(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);
            }

            DateOnly toDate;
            if (to is not null)
            {
                if (!TryParseDate(to, true, out toDate))
                    throw new IdentityFailure(400, "validation");
            }
            else
            {
                toDate = todayVn;
            }

            if (fromDate > toDate || toDate > todayVn)
                throw new IdentityFailure(400, "validation");

            var startUtc = new DateTime(fromDate.Year, fromDate.Month, fromDate.Day, 0, 0, 0, DateTimeKind.Utc).AddHours(-7);
            var endExclusiveUtc = new DateTime(toDate.Year, toDate.Month, toDate.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1).AddHours(-7);

            var query = await (
                from r in db.Registrations.AsNoTracking()
                where r.NgayDangKy >= startUtc && r.NgayDangKy < endExclusiveUtc
                group r by r.TrangThai into g
                select new
                {
                    Status = g.Key,
                    Count = g.Count()
                }
            ).ToListAsync(ct);

            var totalRegistrations = query.Sum(x => x.Count);
            var ordered = query
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Status)
                .Select(x => new RegistrationStatusItemDto(
                    x.Status,
                    x.Count,
                    totalRegistrations > 0 ? Math.Round((double)x.Count * 100.0 / totalRegistrations, 1) : 0
                ))
                .ToList();

            return Results.Ok(new RegistrationStatusReportDto(
                fromDate,
                toDate,
                totalRegistrations,
                ordered
            ));
        }).WithTags("Reports");
    }

    private static bool TryParseDate(string input, bool isEnd, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(input)) return false;
        var trimmed = input.Trim();

        if (DateOnly.TryParseExact(trimmed, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;

        if (DateTime.TryParseExact(trimmed, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ym))
        {
            date = isEnd
                ? new DateOnly(ym.Year, ym.Month, DateTime.DaysInMonth(ym.Year, ym.Month))
                : new DateOnly(ym.Year, ym.Month, 1);
            return true;
        }

        return false;
    }
}
