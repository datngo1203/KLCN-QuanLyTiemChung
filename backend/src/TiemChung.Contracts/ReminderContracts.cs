using System.Text.Json.Serialization;

namespace TiemChung.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateReminderRequest;
public sealed record ReminderDto(int Id, int AppointmentId, string Channel, string Status, string? Content,
    DateTimeOffset RecordedAt, RegistrationProfileDto? RecordedBy);
public sealed record ReminderDetailDto(AppointmentDto Appointment, string? Preview, bool CanRecord, ReminderDto? Reminder);
