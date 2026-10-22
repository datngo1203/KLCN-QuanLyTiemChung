namespace TiemChung.Desktop.Services;

public sealed record ReportDoseSummary(string Vaccine, int Doses);
public sealed record ReportExpiringLot(string LotNumber, string Vaccine, DateOnly ExpiresOn, int Stock);
public sealed record ReportSnapshot(decimal Revenue, decimal ReactionRate, IReadOnlyList<ReportDoseSummary> Doses, IReadOnlyList<ReportExpiringLot> ExpiringLots)
{
    public bool HasData => Revenue > 0 || ReactionRate > 0 || Doses.Count > 0 || ExpiringLots.Count > 0;
    public static ReportSnapshot Empty { get; } = new(0, 0, [], []);
}

public interface IReportService
{
    bool UseSampleData { get; set; }
    ReportSnapshot GetReport(DateOnly from, DateOnly to);
}
