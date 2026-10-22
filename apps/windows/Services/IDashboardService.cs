using TiemChung.Desktop.ViewModels.Samples;

namespace TiemChung.Desktop.Services;

// TODO: Replace the sample implementation when reviewed dashboard read contracts exist.
public interface IDashboardService
{
    bool UseSampleData { get; set; }
    IReadOnlyList<OverviewWorkItem> GetWorkItems(DateTime day);
    IReadOnlyList<OverviewStage> GetStages();
    IReadOnlyList<OverviewMetric> GetMetrics();
}

public sealed class SampleDashboardService : IDashboardService
{
    public bool UseSampleData { get; set; } = true;
    public IReadOnlyList<OverviewWorkItem> GetWorkItems(DateTime day) => UseSampleData ? OverviewSampleData.CreateWorkItems(day) : [];
    public IReadOnlyList<OverviewStage> GetStages() => UseSampleData ? OverviewSampleData.CreateStages() : [];
    public IReadOnlyList<OverviewMetric> GetMetrics() => UseSampleData ? OverviewSampleData.CreateMetrics() : [];
}
