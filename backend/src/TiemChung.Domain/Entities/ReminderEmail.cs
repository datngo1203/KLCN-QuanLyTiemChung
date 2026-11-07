namespace TiemChung.Domain.Entities;

public sealed class ReminderEmail
{
    public int Id { get; set; }
    public string Kind { get; set; } = "Manual";
    public int AppointmentId { get; set; }
    public int RegistrationId { get; set; }
    public int CustomerId { get; set; }
    public int RecipientId { get; set; }
    public string ItemKind { get; set; } = "";
    public int RegistrationItemId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Location { get; set; } = "";
    public string RecipientName { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public int RecipientAccountId { get; set; }
    public string RecipientEmail { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public int? CreatedByAccountId { get; set; }
    public int? CreatedByEmployeeId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Status { get; set; } = "ChoGui";
    public int Attempts { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public string? LastErrorCode { get; set; }
    public bool DispatchStarted { get; set; }
}
