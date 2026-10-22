using TiemChung.Desktop.ViewModels.Samples;

namespace TiemChung.Desktop.Services;

// TODO: Replace the sample implementation when reviewed screening read/write contracts exist.
public interface IScreeningService
{
    bool UseSampleData { get; set; }
    IReadOnlyList<ScreeningQueueSample> GetQueue();
    IReadOnlyList<VaccineCatalogSample> GetVaccineCatalog();
}

public sealed class SampleScreeningService : IScreeningService
{
    public bool UseSampleData { get; set; } = true;
    public IReadOnlyList<ScreeningQueueSample> GetQueue() => UseSampleData ? ScreeningSampleData.CreateQueue() : [];
    public IReadOnlyList<VaccineCatalogSample> GetVaccineCatalog() => UseSampleData ? ScreeningSampleData.CreateVaccineCatalog() : [];
}
