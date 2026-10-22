namespace TiemChung.Desktop.Services;

public sealed record VaccinationQueueItem(string Id, string Recipient, string Vaccine, string Dose, bool HasIndication, bool IsScreeningEligible, bool IsPaid);
public sealed record VaccineLotOption(string Number, string Vaccine, DateOnly ExpiresOn, int Stock);

public interface IVaccinationService
{
    bool UseSampleData { get; set; }
    IReadOnlyList<VaccinationQueueItem> GetEligibleQueue();
    IReadOnlyList<VaccineLotOption> GetUsableLots(string vaccine);
    bool RecordInjection(string queueId, string lotNumber, string location, DateTime administeredAt);
    bool RecordFollowUp(string queueId, string condition, string severity, string treatment, DateTime recordedAt);
}
