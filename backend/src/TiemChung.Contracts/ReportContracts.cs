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

public sealed record VaccineDosesItemDto(
    int VaccineId,
    string VaccineName,
    int DoseCount
);

public sealed record VaccineDosesReportDto(
    DateOnly From,
    DateOnly To,
    int TotalDoses,
    IReadOnlyList<VaccineDosesItemDto> Items
);

public sealed record RegistrationStatusItemDto(
    string Status,
    int RegistrationCount,
    double Percentage
);

public sealed record RegistrationStatusReportDto(
    DateOnly From,
    DateOnly To,
    int TotalRegistrations,
    IReadOnlyList<RegistrationStatusItemDto> Items
);


