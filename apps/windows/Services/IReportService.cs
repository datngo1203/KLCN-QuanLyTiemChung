namespace TiemChung.Desktop.Services;

public sealed record MonthlyRevenueItem(
    int Year,
    int Month,
    string Label,
    decimal Revenue,
    int TransactionCount,
    double BarHeight = 0
);

public sealed record MonthlyRevenueReport(
    DateOnly From,
    DateOnly To,
    decimal TotalRevenue,
    int TotalTransactions,
    bool IsSimulated,
    IReadOnlyList<MonthlyRevenueItem> Items
)
{
    public bool HasData => TotalTransactions > 0 || Items.Count > 0;
    public static MonthlyRevenueReport Empty { get; } = new(DateOnly.MinValue, DateOnly.MinValue, 0, 0, true, []);
}

public sealed record VaccineDoseItem(
    int VaccineId,
    string VaccineName,
    int DoseCount,
    double Percentage = 0,
    double BarWidth = 0
)
{
    public string Code => $"VX-{VaccineId:0000}";
    public string PercentageLabel => $"{Percentage:0.0}%";
}

public sealed record VaccineDosesReport(
    DateOnly From,
    DateOnly To,
    int TotalDoses,
    IReadOnlyList<VaccineDoseItem> Items
)
{
    public bool HasData => TotalDoses > 0 || Items.Count > 0;
    public static VaccineDosesReport Empty { get; } = new(DateOnly.MinValue, DateOnly.MinValue, 0, []);
}

public interface IReportService
{
    bool UseSampleData { get; set; }
    Task<MonthlyRevenueReport> GetMonthlyRevenueAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<VaccineDosesReport> GetVaccineDosesAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
}
