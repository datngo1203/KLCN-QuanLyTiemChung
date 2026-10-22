namespace TiemChung.Desktop.Services;

// TODO: Replace with a report query API; formulas and scope must be confirmed by the API owner.
public sealed class SampleReportService : IReportService
{
    public bool UseSampleData { get; set; } = true;
    public ReportSnapshot GetReport(DateOnly from, DateOnly to)
    {
        if (!UseSampleData) return ReportSnapshot.Empty;
        return new(
            12_450_000m,
            1.8m,
            [new("Vắc xin mẫu A", 42), new("Vắc xin mẫu B", 36), new("Vắc xin mẫu C", 18)],
            [new("LO-MAU-11", "Vắc xin mẫu A", DateOnly.FromDateTime(DateTime.Today.AddDays(12)), 8),
             new("LO-MAU-24", "Vắc xin mẫu B", DateOnly.FromDateTime(DateTime.Today.AddDays(24)), 14)]);
    }
}
