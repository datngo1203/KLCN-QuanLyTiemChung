using System.Text.Json.Serialization;

namespace TiemChung.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateClinicalAssessmentRequest(string Conclusion, string? Reason, int[] VaccineIds);
public sealed record ClinicalVaccineDto(int Id, string Name);
public sealed record ClinicalAppointmentDto(AppointmentDto Appointment, ClinicalVaccineDto[] AllowedVaccines);
public sealed record ClinicalIndicationDto(int Id, int VaccineId, string VaccineName, string Result, DateTimeOffset RecordedAt);
public sealed record ClinicalAssessmentDto(int Id, int AppointmentId, int RegistrationId, int RecipientId,
    RegistrationProfileDto Doctor, DateTimeOffset RecordedAt, string Conclusion, string? Reason,
    string? VitalSigns, string? MedicalHistory, ClinicalIndicationDto[] Indications);
public sealed record ClinicalAssessmentResponse(ClinicalAssessmentDto? Assessment);
