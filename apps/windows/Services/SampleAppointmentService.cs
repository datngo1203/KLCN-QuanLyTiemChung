namespace TiemChung.Desktop.Services;

// TODO: Replace with appointment and reminder API contracts when available; this service sends no real message.
public sealed class SampleAppointmentService : IAppointmentService
{
    private readonly List<AppointmentRecord> _appointments =
    [
        new("LH-MAU-01", "Người tiêm mẫu L01", DateTime.Today.AddDays(1).AddHours(9), "Phòng tiêm 1", "Đã xếp lịch",
            [new("SMS", DateTime.Now.AddHours(-2), "Đã gửi")]),
        new("LH-MAU-02", "Người tiêm mẫu L02", DateTime.Today.AddDays(-1).AddHours(10), "Phòng tiêm 2", "Đã xếp lịch",
            [new("Gọi điện", DateTime.Now.AddDays(-2), "Thất bại")]),
        new("LH-MAU-03", "Người tiêm mẫu L03", DateTime.Today.AddDays(2).AddHours(15), "Phòng tiêm 1", "Đã xếp lịch"),
        new("LH-MAU-04", "Người tiêm mẫu L04", DateTime.Today.AddDays(3).AddHours(8), "Phòng tiêm 3", "Đã đến")
    ];

    public bool UseSampleData { get; set; } = true;
    public IReadOnlyList<AppointmentRecord> GetAppointments() => UseSampleData ? _appointments : [];
    public bool CreateAppointment(string recipient, DateTime scheduledAt, string location)
    {
        if (!UseSampleData) return false;
        _appointments.Insert(0, new($"LH-MAU-{_appointments.Count + 1:00}", recipient, scheduledAt, location, "Đã xếp lịch"));
        return true;
    }
    public bool RescheduleAppointment(string id, DateTime scheduledAt, string location)
    {
        var item = _appointments.FirstOrDefault(x => x.Id == id && x.Status == "Đã xếp lịch" && x.ScheduledAt >= DateTime.Now);
        if (!UseSampleData || item is null) return false;
        item.Reschedule(scheduledAt, location);
        return true;
    }
    public bool SendReminder(string id, string channel, DateTime sentAt)
    {
        var item = _appointments.FirstOrDefault(x => x.Id == id && x.Status == "Đã xếp lịch" && x.ScheduledAt >= DateTime.Now);
        if (!UseSampleData || item is null) return false;
        item.AddReminder(new(channel, sentAt, "Đã gửi"));
        return true;
    }
}
