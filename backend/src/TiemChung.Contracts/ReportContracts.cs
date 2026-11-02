namespace TiemChung.Contracts;

public sealed record MonthlyRevenueItemDto(
    int Year,
    int Month,
    string Label,
    decimal Revenue,
    int TransactionCount
);

public sealed record MonthlyRevenueReportDto(
    DateOnly From,
    DateOnly To,
    decimal TotalRevenue,
    bool IsSimulated,
    IReadOnlyList<MonthlyRevenueItemDto> Items
);

