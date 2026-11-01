using System.Text.Json.Serialization;

namespace TiemChung.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RecordVaccinationRequest(int IndicationId, string LotNumber, string InjectionSite);
public sealed record VaccinationIndicationDto(int Id, int AppointmentId, int RegistrationId,
    RegistrationProfileDto Customer, RegistrationProfileDto Recipient, ClinicalVaccineDto Vaccine,
    DateTimeOffset ScheduledAt, string Location, RegistrationProfileDto Doctor, DateTimeOffset AssessedAt);
public sealed record VaccinationListResponse(VaccinationIndicationDto[] Items);
public sealed record VaccinationLotDto(string Number, DateOnly ManufacturedOn, DateOnly ExpiresOn, int Stock);
public sealed record VaccinationDetailDto(VaccinationIndicationDto Indication, DateOnly FacilityDate, VaccinationLotDto[] Lots);
public sealed record VaccinationDto(int Id, int IndicationId, int AppointmentId, int RegistrationId,
    RegistrationProfileDto Recipient, ClinicalVaccineDto Vaccine, string LotNumber, string InjectionSite,
    RegistrationProfileDto Nurse, DateTimeOffset AdministeredAt, string Status);
public sealed record VaccinationResultResponse(VaccinationDto? Vaccination);
public sealed record VaccinationHistoryDto(int Id, int AppointmentId, int RegistrationId,
    RegistrationProfileDto Recipient, ClinicalVaccineDto Vaccine, string LotNumber, string InjectionSite,
    DateTimeOffset AdministeredAt, string Status);
public sealed record VaccinationHistoryResponse(VaccinationHistoryDto[] Items);
