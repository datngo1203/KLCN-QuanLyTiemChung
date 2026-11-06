namespace TiemChung.Domain.Entities;

// Immutable lifecycle payload; delivery fields alone change after the appointment commits.
public sealed class AppointmentNotification
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public string AppointmentVersion { get; set; } = "";
    public string EventType { get; set; } = "";
    public string Channel { get; set; } = "Email";
    public int? RecipientAccountId { get; set; }
    public string RecipientEmail { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public int CreatedByAccountId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Status { get; set; } = "ChoGui";
    public bool DispatchStarted { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public string? LastErrorCode { get; set; }
}
