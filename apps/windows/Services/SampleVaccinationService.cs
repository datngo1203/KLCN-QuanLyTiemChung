namespace TiemChung.Desktop.Services;

// TODO: Replace with the narrow authorized vaccination read/write API after its contract is reviewed.
public sealed class SampleVaccinationService : IVaccinationService
{
    private readonly List<VaccinationQueueItem> _queue =
    [
        new("LT-MAU-001", "Người tiêm mẫu M01", "Vắc xin mẫu A", "Mũi 1", true, true, true),
        new("LT-MAU-002", "Người tiêm mẫu M02", "Vắc xin mẫu B", "Mũi 2", true, true, true)
    ];
    private readonly List<VaccineLotOption> _lots =
    [
        new("LO-MAU-A01", "Vắc xin mẫu A", DateOnly.FromDateTime(DateTime.Today.AddMonths(8)), 18),
        new("LO-MAU-A02", "Vắc xin mẫu A", DateOnly.FromDateTime(DateTime.Today.AddMonths(3)), 0),
        new("LO-MAU-A03", "Vắc xin mẫu A", DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), 12),
        new("LO-MAU-B01", "Vắc xin mẫu B", DateOnly.FromDateTime(DateTime.Today.AddMonths(10)), 7)
    ];

    public bool UseSampleData { get; set; } = true;
    public IReadOnlyList<VaccinationQueueItem> GetEligibleQueue() => UseSampleData
        ? _queue.Where(x => x.HasIndication && x.IsScreeningEligible && x.IsPaid).ToArray() : [];

    public IReadOnlyList<VaccineLotOption> GetUsableLots(string vaccine) => UseSampleData
        ? _lots.Where(x => x.Vaccine == vaccine && x.ExpiresOn >= DateOnly.FromDateTime(DateTime.Today) && x.Stock > 0).ToArray() : [];

    public bool RecordInjection(string queueId, string lotNumber, string location, DateTime administeredAt)
    {
        if (!UseSampleData) return false;
        var item = _queue.FirstOrDefault(x => x.Id == queueId && x.HasIndication && x.IsScreeningEligible && x.IsPaid);
        var lot = _lots.FirstOrDefault(x => x.Number == lotNumber && x.Vaccine == item?.Vaccine && x.Stock > 0 && x.ExpiresOn >= DateOnly.FromDateTime(DateTime.Today));
        if (item is null || lot is null || string.IsNullOrWhiteSpace(location)) return false;
        _lots[_lots.IndexOf(lot)] = lot with { Stock = lot.Stock - 1 };
        _queue.Remove(item);
        return true;
    }

    public bool RecordFollowUp(string queueId, string condition, string severity, string treatment, DateTime recordedAt) => UseSampleData;
}
