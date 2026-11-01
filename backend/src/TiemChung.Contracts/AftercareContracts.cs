using System.Text.Json.Serialization;

namespace TiemChung.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateFollowUpRequest(string Condition, string Severity, string Treatment);
public sealed record FollowUpDto(int Id, int VaccinationId, string Condition, string Severity, string Treatment,
    RegistrationProfileDto? RecordedBy, DateTimeOffset RecordedAt);
public sealed record FollowUpDetailDto(VaccinationDto Vaccination, FollowUpDto? FollowUp);
public sealed record FollowUpPendingResponse(VaccinationDto[] Items);
