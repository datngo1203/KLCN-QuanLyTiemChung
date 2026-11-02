namespace TiemChung.Desktop.Services;

public sealed class SampleReportService : IReportService
{
    public bool UseSampleData { get; set; } = true;

    public Task<MonthlyRevenueReport> GetMonthlyRevenueAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        if (!UseSampleData)
        {
            return Task.FromResult(MonthlyRevenueReport.Empty);
        }

        var items = new List<MonthlyRevenueItem>
        {
            new(2026, 6, "06/2026", 12_500_000m, 18),
            new(2026, 7, "07/2026", 18_200_000m, 26),
            new(2026, 8, "08/2026", 24_600_000m, 35),
            new(2026, 9, "09/2026", 31_000_000m, 44),
            new(2026, 10, "10/2026", 28_400_000m, 40),
            new(2026, 11, "11/2026", 15_800_000m, 22)
        };

        var total = items.Sum(x => x.Revenue);
        var tx = items.Sum(x => x.TransactionCount);
        var result = new MonthlyRevenueReport(
            from ?? new DateOnly(2026, 6, 1),
            to ?? new DateOnly(2026, 11, 2),
            total,
            tx,
            true,
            items
        );

        return Task.FromResult(result);
    }

    public Task<VaccineDosesReport> GetVaccineDosesAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        if (!UseSampleData)
        {
            return Task.FromResult(VaccineDosesReport.Empty);
        }

        var items = new List<VaccineDoseItem>
        {
            new(1, "Vắc xin Cúm mùa (Influvac Tetra)", 18, 37.5),
            new(3, "Vắc xin Phế cầu (Prevenar 13)", 14, 29.2),
            new(2, "Vắc xin Viêm gan B (Engerix-B)", 10, 20.8),
            new(4, "Vắc xin Sởi - Quai bị - Rubella (MMR II)", 6, 12.5)
        };

        var total = items.Sum(x => x.DoseCount);
        var result = new VaccineDosesReport(
            from ?? new DateOnly(2026, 6, 1),
            to ?? new DateOnly(2026, 11, 2),
            total,
            items
        );

        return Task.FromResult(result);
    }

    public Task<RegistrationStatusReport> GetRegistrationStatusesAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        if (!UseSampleData)
        {
            return Task.FromResult(RegistrationStatusReport.Empty);
        }

        var items = new List<RegistrationStatusItem>
        {
            new("DaDangKy", 25, 45.5),
            new("DaThanhToan", 18, 32.7),
            new("DaHuy", 8, 14.5),
            new("HoanTat", 4, 7.3)
        };

        var total = items.Sum(x => x.RegistrationCount);
        var result = new RegistrationStatusReport(
            from ?? new DateOnly(2026, 6, 1),
            to ?? new DateOnly(2026, 11, 2),
            total,
            items
        );

        return Task.FromResult(result);
    }
}
