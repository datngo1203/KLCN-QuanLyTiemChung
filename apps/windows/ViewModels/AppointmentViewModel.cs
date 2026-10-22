using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Threading;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class AppointmentViewModel : ViewModelBase
{
    private readonly IAppointmentService _service;
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(30) };
    private AppointmentRecord? _selected;
    private string _recipient = "";
    private DateTime _appointmentDate = DateTime.Today.AddDays(1);
    private string _time = "09:00";
    private string _location = "Phòng tiêm 1";
    private string _channel = "SMS";
    private string _validation = "";
    private string _feedback = "";

    public AppointmentViewModel(IAppointmentService? service = null)
    {
        _service = service ?? new SampleAppointmentService();
        foreach (var item in _service.GetAppointments()) Appointments.Add(item);
        Appointments.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoAppointments));
        CreateAppointmentCommand = new(_ => Create(), _ => CanCreateAppointment);
        RescheduleCommand = new(_ => Reschedule(), _ => CanReschedule);
        SendReminderCommand = new(_ => SendReminder(), _ => CanSendReminder);
        _clock.Tick += (_, _) => { foreach (var item in Appointments) item.RefreshClock(); RefreshCommands(); };
        _clock.Start();
    }

    public ObservableCollection<AppointmentRecord> Appointments { get; } = [];
    private static readonly string[] SampleRecipients = ["Người tiêm mẫu L01", "Người tiêm mẫu L02", "Người tiêm mẫu L03", "Người tiêm mẫu L04"];
    public IReadOnlyList<string> Recipients => _service.UseSampleData ? SampleRecipients : [];
    public IReadOnlyList<string> Channels { get; } = ["SMS", "Zalo", "Email", "Gọi điện"];
    public RelayCommand CreateAppointmentCommand { get; }
    public RelayCommand RescheduleCommand { get; }
    public RelayCommand SendReminderCommand { get; }
    public AppointmentRecord? Selected { get => _selected; set { if (Set(ref _selected, value)) { if (value is not null) { Recipient = value.Recipient; AppointmentDate = value.ScheduledAt.Date; Time = value.ScheduledAt.ToString("HH:mm"); Location = value.Location; } OnPropertyChanged(nameof(HasSelection)); OnPropertyChanged(nameof(HasNoSelection)); RefreshCommands(); OnPropertyChanged(nameof(HasRecentReminder)); } } }
    public string Recipient { get => _recipient; set { if (Set(ref _recipient, value)) RefreshCommands(); } }
    public DateTime AppointmentDate { get => _appointmentDate; set { if (Set(ref _appointmentDate, value)) RefreshCommands(); } }
    public string Time { get => _time; set { if (Set(ref _time, value)) RefreshCommands(); } }
    public string Location { get => _location; set { if (Set(ref _location, value)) RefreshCommands(); } }
    public string Channel { get => _channel; set { if (Set(ref _channel, value)) RefreshCommands(); } }
    public string Validation { get => _validation; private set => Set(ref _validation, value); }
    public string Feedback { get => _feedback; private set => Set(ref _feedback, value); }
    public bool HasNoAppointments => Appointments.Count == 0;
    public bool HasSelection => Selected is not null;
    public bool HasNoSelection => Selected is null;
    public bool HasRecentReminder => Selected?.HasRecentReminder == true;
    public bool IsSampleEnabled => _service.UseSampleData;
    public bool UseSampleData
    {
        get => _service.UseSampleData;
        set
        {
            if (_service.UseSampleData == value) return;
            _service.UseSampleData = value;
            Appointments.Clear();
            foreach (var item in _service.GetAppointments()) Appointments.Add(item);
            Selected = null;
            Recipient = "";
            AppointmentDate = DateTime.Today;
            Time = "";
            Location = "";
            OnPropertyChanged(nameof(Recipients));
            OnPropertyChanged(nameof(DataSourceLabel));
            OnPropertyChanged(nameof(IsSampleEnabled));
            RefreshCommands();
        }
    }
    public string DataSourceLabel => _service.UseSampleData ? "Dữ liệu mẫu" : "Chưa có nguồn dữ liệu";
    // TODO: Bind permissions from the authenticated shell session when available.
    public bool CanManageAppointments => true;
    public bool CanSendReminders => true;
    public bool CanCreateAppointment => CanManageAppointments && TryGetScheduledAt(out _) && !string.IsNullOrWhiteSpace(Recipient) && !string.IsNullOrWhiteSpace(Location);
    public bool CanReschedule => CanManageAppointments && Selected is { Status: "Đã xếp lịch" } && Selected.ScheduledAt >= DateTime.Now && TryGetScheduledAt(out _) && !string.IsNullOrWhiteSpace(Location);
    public bool CanSendReminder => CanSendReminders && Selected is { Status: "Đã xếp lịch" } && Selected.ScheduledAt >= DateTime.Now;

    public string AppointmentValidation
    {
        get
        {
            if (!DateTime.TryParseExact(Time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTime)) return "Nhập giờ theo định dạng HH:mm.";
            var scheduled = AppointmentDate.Date.Add(parsedTime.TimeOfDay);
            if (parsedTime.TimeOfDay < new TimeSpan(7, 30, 0) || parsedTime.TimeOfDay > new TimeSpan(17, 30, 0)) return "Giờ hẹn phải trong khung 07:30–17:30.";
            if (scheduled <= DateTime.Now) return "Thời gian hẹn phải sau thời điểm hiện tại.";
            return "";
        }
    }

    private bool TryGetScheduledAt(out DateTime scheduledAt)
    {
        scheduledAt = default;
        if (!DateTime.TryParseExact(Time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return false;
        scheduledAt = AppointmentDate.Date.Add(parsed.TimeOfDay);
        return scheduledAt > DateTime.Now && parsed.TimeOfDay >= new TimeSpan(7, 30, 0) && parsed.TimeOfDay <= new TimeSpan(17, 30, 0);
    }
    private void Create()
    {
        if (!CanCreateAppointment || !TryGetScheduledAt(out var scheduledAt)) return;
        if (_service.CreateAppointment(Recipient, scheduledAt, Location.Trim()))
        { Appointments.Clear(); foreach (var item in _service.GetAppointments()) Appointments.Add(item); Feedback = "Đã tạo lịch trên dữ liệu mẫu; chưa ghi lịch thật."; }
        RefreshCommands();
    }
    private void Reschedule()
    {
        if (!CanReschedule || Selected is null || !TryGetScheduledAt(out var scheduledAt)) return;
        if (_service.RescheduleAppointment(Selected.Id, scheduledAt, Location.Trim())) Feedback = "Đã đổi lịch trên dữ liệu mẫu; chưa ghi lịch thật.";
        RefreshCommands();
    }
    private void SendReminder()
    {
        if (!CanSendReminder || Selected is null) return;
        if (_service.SendReminder(Selected.Id, Channel, DateTime.Now)) Feedback = "Đã ghi lịch sử nhắc mẫu; không gửi SMS/Zalo/Email/cuộc gọi thật.";
        OnPropertyChanged(nameof(HasRecentReminder));
        RefreshCommands();
    }
    private void RefreshCommands()
    {
        Validation = AppointmentValidation;
        OnPropertyChanged(nameof(CanCreateAppointment)); OnPropertyChanged(nameof(CanReschedule)); OnPropertyChanged(nameof(CanSendReminder));
        CreateAppointmentCommand?.NotifyCanExecuteChanged(); RescheduleCommand?.NotifyCanExecuteChanged(); SendReminderCommand?.NotifyCanExecuteChanged();
    }
}
