using TiemChung.Desktop.ViewModels.Samples;

namespace TiemChung.Desktop.Services;

// TODO: Replace the sample implementation when reviewed dashboard read contracts exist.
public interface IDashboardService
{
    bool UseSampleData { get; set; }
    OverviewSnapshot GetSnapshot(DateTime from, DateTime to);
}

public sealed record OverviewSnapshot(
    IReadOnlyList<OverviewWorkItem> WorkItems,
    IReadOnlyList<OverviewStage> Stages,
    IReadOnlyList<OverviewMetric> Metrics);

public sealed class SampleDashboardService : IDashboardService
{
    public bool UseSampleData { get; set; } = true;
    public OverviewSnapshot GetSnapshot(DateTime from, DateTime to) =>
        UseSampleData ? OverviewSampleData.CreateSnapshot(from, to) : OverviewSampleData.CreateEmptySnapshot();
}

/// <summary>
/// Production-runtime dashboard service: returns empty/neutral state.
/// Used when no reviewed dashboard API endpoint exists yet. Sample data must not appear in production shell.
/// </summary>
public sealed class NullDashboardService : IDashboardService
{
    public bool UseSampleData { get; set; } = false;
    public OverviewSnapshot GetSnapshot(DateTime from, DateTime to) => OverviewSampleData.CreateEmptySnapshot();
}
