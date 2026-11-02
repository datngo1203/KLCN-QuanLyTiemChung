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

public interface IReportService
{
    bool UseSampleData { get; set; }
    Task<MonthlyRevenueReport> GetMonthlyRevenueAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
}
