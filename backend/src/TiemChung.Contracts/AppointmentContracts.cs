using System.Text.Json.Serialization;

namespace TiemChung.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateAppointmentRequest(int RegistrationId, string ItemKind, int RegistrationItemId,
    string ScheduledAt, string Location);

public sealed record AppointmentDto(int Id, int RegistrationId, RegistrationProfileDto Customer,
    RegistrationProfileDto Recipient, string ItemKind, int RegistrationItemId, int ServiceId, string ServiceName,
    DateTimeOffset ScheduledAt, string Location, string Status);
public sealed record AppointmentListResponse(AppointmentDto[] Items);
