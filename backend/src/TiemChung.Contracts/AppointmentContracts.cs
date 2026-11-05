using System.Text.Json.Serialization;

namespace TiemChung.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateAppointmentRequest(int RegistrationId, string ItemKind, int RegistrationItemId,
    string ScheduledAt);

public sealed record AppointmentSourceDto(int RegistrationId, string Status, RegistrationProfileDto Customer,
    RegistrationProfileDto Recipient, DateOnly? PreferredDate, string PaymentState, string? ExpectedChannel,
    bool CanSchedule, string ReadinessCode, string Facility);
public sealed record AppointmentSourceListResponse(AppointmentSourceDto[] Items);
public sealed record AppointmentSourceDetailDto(AppointmentSourceDto Source, AppointmentSourceItemDto[] Items);
public sealed record AppointmentSourceItemDto(int Id, string Kind, int ServiceId, string Name, int Quantity);

public sealed record AppointmentDto(int Id, int RegistrationId, RegistrationProfileDto Customer,
    RegistrationProfileDto Recipient, string ItemKind, int RegistrationItemId, int ServiceId, string ServiceName,
    DateTimeOffset ScheduledAt, string Location, string Status);
public sealed record AppointmentListResponse(AppointmentDto[] Items);
