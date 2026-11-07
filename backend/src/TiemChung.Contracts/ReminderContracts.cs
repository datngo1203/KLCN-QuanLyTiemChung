using System.Text.Json.Serialization;

namespace TiemChung.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateReminderRequest;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RetryReminderEmailRequest(int ExpectedAttempts, int? DeliveryId = null);
public sealed record ReminderDto(int Id, int AppointmentId, string Channel, string Status, string? Content,
    DateTimeOffset RecordedAt, RegistrationProfileDto? RecordedBy);
public sealed record ReminderDetailDto(AppointmentDto Appointment, string? Preview, bool CanRecord, ReminderDto? Reminder);

public sealed record ReminderEmailDto(int Id, int AppointmentId, string Channel, string Status, string Content,
    DateTimeOffset RecordedAt, RegistrationProfileDto? RecordedBy, string MaskedRecipient, string Subject,
    int Attempts, DateTimeOffset? DeliveredAt, DateTimeOffset? NextAttemptAt, string? LastErrorCode, bool CanRetry, string Kind = "Manual");
public sealed record ReminderEmailDetailDto(AppointmentDto Appointment, string? Preview, bool CanRecord,
    ReminderEmailDto? Reminder, string? MaskedRecipient, string? Subject, string? UnavailableReason,
    ReminderEmailDto[]? History = null);
