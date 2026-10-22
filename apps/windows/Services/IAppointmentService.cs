using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TiemChung.Desktop.Services;

public sealed record ReminderHistory(string Channel, DateTime SentAt, string Status);

public sealed class AppointmentRecord : INotifyPropertyChanged
{
    private DateTime _scheduledAt;
    private string _location;
    private string _status;

    public AppointmentRecord(string id, string recipient, DateTime scheduledAt, string location, string status, IEnumerable<ReminderHistory>? reminders = null)
    {
        Id = id; Recipient = recipient; _scheduledAt = scheduledAt; _location = location; _status = status;
        if (reminders is not null) foreach (var item in reminders) Reminders.Add(item);
        Reminders.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasRecentReminder));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public string Id { get; }
    public string Recipient { get; }
    public DateTime ScheduledAt { get => _scheduledAt; private set { if (_scheduledAt != value) { _scheduledAt = value; Changed(); } } }
    public string Location { get => _location; private set { if (_location != value) { _location = value; Changed(); } } }
    public string Status { get => _status; private set { if (_status != value) { _status = value; Changed(); } } }
    public ObservableCollection<ReminderHistory> Reminders { get; } = [];
    public string DisplayStatus => Status == "Đã xếp lịch" && ScheduledAt < DateTime.Now ? "Quá hạn" : Status;
    public bool IsOverdue => DisplayStatus == "Quá hạn";
    public bool HasRecentReminder => Reminders.Any(x => x.Status == "Đã gửi" && x.SentAt >= DateTime.Now.AddHours(-24));
    public void Reschedule(DateTime scheduledAt, string location) { ScheduledAt = scheduledAt; Location = location; }
    public void AddReminder(ReminderHistory reminder) { Reminders.Insert(0, reminder); OnPropertyChanged(nameof(HasRecentReminder)); }
    public void RefreshClock() { OnPropertyChanged(nameof(DisplayStatus)); OnPropertyChanged(nameof(IsOverdue)); OnPropertyChanged(nameof(HasRecentReminder)); }
    private void Changed([CallerMemberName] string? name = null) { OnPropertyChanged(name); OnPropertyChanged(nameof(DisplayStatus)); OnPropertyChanged(nameof(IsOverdue)); }
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public interface IAppointmentService
{
    bool UseSampleData { get; set; }
    IReadOnlyList<AppointmentRecord> GetAppointments();
    bool CreateAppointment(string recipient, DateTime scheduledAt, string location);
    bool RescheduleAppointment(string id, DateTime scheduledAt, string location);
    bool SendReminder(string id, string channel, DateTime sentAt);
}
