using System.Collections.ObjectModel;
using System.Windows.Threading;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class VaccinationViewModel : ViewModelBase
{
    private readonly IVaccinationService _service;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private VaccinationQueueItem? _selected;
    private VaccineLotOption? _selectedLot;
    private string _location = "";
    private bool _busy;
    private string _feedback = "";
    private string _condition = "";
    private string _severity = "";
    private string _treatment = "";
    private bool _doctorNotified;
    private bool _earlyReleaseConfirmed;
    private bool _showEarlyReleaseWarning;
    private DateTime? _followUpStartedAt;
    private string _activeQueueId = "";

    public VaccinationViewModel(IVaccinationService? service = null)
    {
        _service = service ?? new SampleVaccinationService();
        foreach (var item in _service.GetEligibleQueue()) Queue.Add(item);
        Queue.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoQueue));
        SelectQueueCommand = new RelayCommand(p => { if (p is VaccinationQueueItem item) Selected = item; });
        ConfirmInjectionCommand = new RelayCommand(_ => ConfirmInjection(), _ => CanConfirmInjection);
        CompleteFollowUpCommand = new RelayCommand(_ => CompleteFollowUp(), _ => CanCompleteFollowUp);
        _timer.Tick += (_, _) =>
        {
            OnPropertyChanged(nameof(RemainingDisplay));
            OnPropertyChanged(nameof(ShowEarlyReleaseConfirmation));
            RefreshCommands();
        };
    }

    public ObservableCollection<VaccinationQueueItem> Queue { get; } = [];
    public ObservableCollection<VaccineLotOption> Lots { get; } = [];
    public IReadOnlyList<string> Locations { get; } = ["Bắp tay trái", "Bắp tay phải", "Đùi trái", "Đùi phải", "Vị trí khác"];
    public IReadOnlyList<string> Conditions { get; } = ["Bình thường", "Sốt nhẹ", "Sưng đỏ tại chỗ", "Khó thở", "Khác"];
    public IReadOnlyList<string> Severities { get; } = ["Không có", "Nhẹ", "Trung bình", "Nặng", "Cấp cứu"];
    public RelayCommand SelectQueueCommand { get; }
    public RelayCommand ConfirmInjectionCommand { get; }
    public RelayCommand CompleteFollowUpCommand { get; }
    public bool UseSampleData
    {
        get => _service.UseSampleData;
        set
        {
            if (_service.UseSampleData == value) return;
            _service.UseSampleData = value;
            Queue.Clear();
            foreach (var item in _service.GetEligibleQueue()) Queue.Add(item);
            Selected = null;
            OnPropertyChanged(nameof(DataSourceLabel));
            OnPropertyChanged(nameof(HasNoQueue));
        }
    }
    public string DataSourceLabel => _service.UseSampleData ? "Dữ liệu mẫu" : "Chưa có nguồn dữ liệu";
    // TODO: Bind permissions from the authenticated shell session when available.
    public bool CanPerformVaccination => true;
    public bool CanRecordFollowUp => true;
    public bool HasNoQueue => Queue.Count == 0;
    public bool IsFollowingUp => _followUpStartedAt is not null;
    public bool IsNotFollowingUp => !IsFollowingUp;
    public VaccinationQueueItem? Selected { get => _selected; set { if (Set(ref _selected, value)) { LoadLots(); RefreshCommands(); } } }
    public VaccineLotOption? SelectedLot { get => _selectedLot; set { if (Set(ref _selectedLot, value)) RefreshCommands(); } }
    public string Location { get => _location; set { if (Set(ref _location, value)) RefreshCommands(); } }
    public bool IsBusy { get => _busy; private set { if (Set(ref _busy, value)) RefreshCommands(); } }
    public string Feedback { get => _feedback; private set => Set(ref _feedback, value); }
    public string Condition { get => _condition; set { if (Set(ref _condition, value)) RefreshCommands(); } }
    public string Severity { get => _severity; set { if (Set(ref _severity, value)) { OnPropertyChanged(nameof(RequiresDoctorAlert)); RefreshCommands(); } } }
    public string Treatment { get => _treatment; set { if (Set(ref _treatment, value)) RefreshCommands(); } }
    public bool DoctorNotified { get => _doctorNotified; set { if (Set(ref _doctorNotified, value)) RefreshCommands(); } }
    public bool EarlyReleaseConfirmed { get => _earlyReleaseConfirmed; set { if (Set(ref _earlyReleaseConfirmed, value)) RefreshCommands(); } }
    public bool ShowEarlyReleaseWarning { get => _showEarlyReleaseWarning; private set { if (Set(ref _showEarlyReleaseWarning, value)) OnPropertyChanged(nameof(ShowEarlyReleaseConfirmation)); } }
    public bool ShowEarlyReleaseConfirmation => ShowEarlyReleaseWarning && Remaining > TimeSpan.Zero;
    public bool RequiresDoctorAlert => Severity is "Nặng" or "Cấp cứu";
    public bool CanConfirmInjection => CanPerformVaccination && !IsBusy && !IsFollowingUp && Selected is { HasIndication: true, IsScreeningEligible: true, IsPaid: true } && SelectedLot is not null && _service.GetUsableLots(Selected.Vaccine).Contains(SelectedLot) && !string.IsNullOrWhiteSpace(Location);
    public bool CanCompleteFollowUp => CanRecordFollowUp && !IsBusy && IsFollowingUp && Condition.Length > 0 && Severity.Length > 0 && Treatment.Trim().Length > 0 && (!RequiresDoctorAlert || DoctorNotified) && (!ShowEarlyReleaseConfirmation || EarlyReleaseConfirmed);
    public DateTime? FollowUpStartedAt { get => _followUpStartedAt; private set { if (Set(ref _followUpStartedAt, value)) { OnPropertyChanged(nameof(IsFollowingUp)); OnPropertyChanged(nameof(IsNotFollowingUp)); OnPropertyChanged(nameof(RemainingDisplay)); OnPropertyChanged(nameof(ShowEarlyReleaseConfirmation)); } } }
    public TimeSpan Remaining
    {
        get
        {
            if (FollowUpStartedAt is null) return TimeSpan.Zero;
            var remaining = TimeSpan.FromMinutes(30) - (DateTime.Now - FollowUpStartedAt.Value);
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }
    public string RemainingDisplay => IsFollowingUp ? $"{Remaining.Minutes:00}:{Remaining.Seconds:00}" : "30:00";

    private void LoadLots()
    {
        Lots.Clear();
        if (Selected is not null)
            foreach (var lot in _service.GetUsableLots(Selected.Vaccine)) Lots.Add(lot);
        SelectedLot = null;
        Location = "";
    }

    private void ConfirmInjection()
    {
        if (!CanConfirmInjection || Selected is null || SelectedLot is null) return;
        IsBusy = true;
        try
        {
            if (!_service.RecordInjection(Selected.Id, SelectedLot.Number, Location, DateTime.Now))
            { Feedback = "Không thể xác nhận lượt tiêm từ nguồn dữ liệu hiện tại."; return; }
            _activeQueueId = Selected.Id;
            Queue.Remove(Selected);
            Selected = null;
            FollowUpStartedAt = DateTime.Now;
            _timer.Start();
            Feedback = "Đã ghi nhận trên dữ liệu mẫu; bắt đầu theo dõi 30 phút.";
        }
        finally { IsBusy = false; }
    }

    private void CompleteFollowUp()
    {
        if (!CanCompleteFollowUp) return;
        if (Remaining > TimeSpan.Zero && !EarlyReleaseConfirmed)
        {
            ShowEarlyReleaseWarning = true;
            OnPropertyChanged(nameof(CanCompleteFollowUp));
            CompleteFollowUpCommand.NotifyCanExecuteChanged();
            return;
        }
        IsBusy = true;
        try
        {
            if (!_service.RecordFollowUp(_activeQueueId, Condition, Severity, Treatment, DateTime.Now))
            { Feedback = "Không thể lưu theo dõi từ nguồn dữ liệu hiện tại."; return; }
            _timer.Stop();
            FollowUpStartedAt = null;
            Feedback = EarlyReleaseConfirmed ? "Đã xác nhận kết thúc theo dõi sớm trên dữ liệu mẫu." : "Đã hoàn tất theo dõi trên dữ liệu mẫu.";
            Condition = Severity = Treatment = "";
            DoctorNotified = EarlyReleaseConfirmed = false;
            ShowEarlyReleaseWarning = false;
        }
        finally { IsBusy = false; }
    }

    private void RefreshCommands()
    {
        OnPropertyChanged(nameof(CanConfirmInjection));
        OnPropertyChanged(nameof(CanCompleteFollowUp));
        ConfirmInjectionCommand?.NotifyCanExecuteChanged();
        CompleteFollowUpCommand?.NotifyCanExecuteChanged();
    }
}
