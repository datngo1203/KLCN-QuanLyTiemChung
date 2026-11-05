using System.Collections.ObjectModel;
using System.Globalization;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class AppointmentViewModel : ViewModelBase
{
    private readonly IAppointmentService _service;
    private readonly IdentityApiService _identity;
    private readonly int _revision;
    private bool _busy, _denied, _creating, _committed, _reconciled, _defaultDatePending;
    private int _page = 1, _sourcePage = 1;
    private int? _draftSourceId;
    private string _feedback = "", _sourceCode = "", _time = "09:00";
    private DateTime? _date = DateTime.Today.AddDays(1);
    private AppointmentSourceOption? _sourceRow;
    private AppointmentSource? _source;
    private AppointmentItem? _item;
    private AppointmentRecord? _selected, _detail;
    private AppointmentRequest? _pending;

    public AppointmentViewModel(IAppointmentService? service = null, IdentityApiService? identity = null)
    {
        _identity = identity ?? IdentityApiService.Current; _revision = _identity.SessionRevision;
        _service = service ?? new AppointmentApiService(_identity);
        Reminder = new ReminderViewModel(identity: _identity);
        Reminder.Changed += Refresh;
        _identity.SessionCleared += (_, _) => ClearSessionData();
        ReloadCommand = new(async _ => await LoadAsync(Page), _ => CanBrowse);
        PreviousCommand = new(async _ => await LoadAsync(Page - 1), _ => CanBrowse && Page > 1);
        NextCommand = new(async _ => await LoadAsync(Page + 1), _ => CanBrowse && Appointments.Count == AppointmentApiService.PageSize);
        NewCommand = new(async _ => await NewAsync(), _ => CanBrowse);
        CancelCommand = new(_ => { _creating = false; ClearDraft(); Refresh(); }, _ => CanEdit);
        FindSourceCommand = new(async _ => await FindSourceAsync(), _ => CanEdit);
        RefreshSourceCommand = new(async _ => await ReadSourceAsync(_draftSourceId!.Value), _ => CanEdit && _draftSourceId.HasValue);
        PreviousSourceCommand = new(async _ => await LoadSourcesAsync(_sourcePage - 1), _ => CanEdit && _sourcePage > 1);
        NextSourceCommand = new(async _ => await LoadSourcesAsync(_sourcePage + 1), _ => CanEdit && Sources.Count == AppointmentApiService.PageSize);
        SaveCommand = new(async _ => await SaveAsync(), _ => CanSave);
        ReconcileCommand = new(async _ => await ReconcileAsync(), _ => HasAccess && !_busy && _pending is not null);
        RetryCommand = new(async _ => await RetryAsync(), _ => HasAccess && !_busy && _pending is not null && _reconciled && !_committed);
    }
    public ObservableCollection<AppointmentRecord> Appointments { get; } = [];
    public ObservableCollection<AppointmentSourceOption> Sources { get; } = [];
    public ObservableCollection<AppointmentItem> Items { get; } = [];
    public RelayCommand ReloadCommand { get; }
    public RelayCommand PreviousCommand { get; }
    public RelayCommand NextCommand { get; }
    public RelayCommand NewCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand FindSourceCommand { get; }
    public RelayCommand RefreshSourceCommand { get; }
    public RelayCommand PreviousSourceCommand { get; }
    public RelayCommand NextSourceCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand ReconcileCommand { get; }
    public RelayCommand RetryCommand { get; }
    private bool IsCurrent => _identity.IsSignedIn && _identity.SessionRevision == _revision;
    public bool HasAccess => IsCurrent && _identity.CanUseAppointments && !_denied;
    public bool NoAccess => !HasAccess;
    public bool IsBusy => _busy;
    public bool IsCreating => _creating && HasAccess;
    public bool IsViewing => !_creating && HasAccess;
    public bool CanBrowse => HasAccess && !_busy && _pending is null && !Reminder.BlocksNavigation;
    public ReminderViewModel Reminder { get; }
    public bool CanEdit => HasAccess && _creating && !_busy && _pending is null;
    public bool HasPending => HasAccess && _pending is not null;
    public bool CanSave => CanEdit && _source?.Source.CanSchedule == true && _item is not null &&
        _source.Items.Contains(_item) && TryTime(out _);
    public int Page => _page;
    public int SourcePage => _sourcePage;
    public bool NoResults => HasAccess && !_busy && Appointments.Count == 0;
    public bool NoDetail => IsViewing && Detail is null;
    public string Feedback { get => _feedback; private set => Set(ref _feedback, value); }
    public string SourceCode { get => _sourceCode; set { if (CanEdit) Set(ref _sourceCode, value); } }
    public string Time { get => _time; set { if (CanEdit) { Set(ref _time, value); Refresh(); } } }
    public DateTime? AppointmentDate
    {
        get => _date;
        set { if (CanEdit && Set(ref _date, value)) { _defaultDatePending = false; Refresh(); } }
    }
    public AppointmentSourceOption? Source => _source?.Source;
    public bool HasSource => _source is not null;
    public AppointmentSourceOption? SelectedSource
    {
        get => _sourceRow;
        set { if (CanEdit && Set(ref _sourceRow, value)) { _source = null; _item = null; Items.Clear(); OnPropertyChanged(nameof(Source)); OnPropertyChanged(nameof(SelectedItem)); Refresh(); } }
    }
    public AppointmentItem? SelectedItem { get => _item; set { if (CanEdit) { Set(ref _item, value); Refresh(); } } }
    public AppointmentRecord? Selected
    {
        get => _selected;
        set { if (CanBrowse && Set(ref _selected, value)) { _creating = false; Detail = null; Refresh(); } }
    }
    public AppointmentRecord? Detail { get => _detail; private set { Set(ref _detail, value); Reminder.Select(value?.Id); Refresh(); } }
    private bool TryTime(out DateTimeOffset time)
    {
        time = default;
        if (_date is null || !DateTime.TryParseExact(_time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return false;
        var local = DateTime.SpecifyKind(_date.Value.Date.Add(parsed.TimeOfDay), DateTimeKind.Unspecified);
        if (TimeZoneInfo.Local.IsInvalidTime(local) || TimeZoneInfo.Local.IsAmbiguousTime(local)) return false;
        time = new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local));
        return time > DateTimeOffset.UtcNow;
    }
    private void Refresh()
    {
        foreach (var name in new[] { nameof(HasAccess), nameof(NoAccess), nameof(IsBusy), nameof(IsCreating), nameof(IsViewing), nameof(CanBrowse), nameof(CanEdit), nameof(HasPending), nameof(HasSource), nameof(CanSave), nameof(Page), nameof(SourcePage), nameof(NoResults), nameof(NoDetail) }) OnPropertyChanged(name);
        foreach (var command in new[] { ReloadCommand, PreviousCommand, NextCommand, NewCommand, CancelCommand, FindSourceCommand, RefreshSourceCommand, PreviousSourceCommand, NextSourceCommand, SaveCommand, ReconcileCommand, RetryCommand }) command?.NotifyCanExecuteChanged();
    }
    private void ClearDraft()
    {
        _sourceRow = null; _source = null; _item = null; _sourceCode = ""; _draftSourceId = null;
        ResetDateTime();
        Items.Clear(); Sources.Clear(); _sourcePage = 1;
        foreach (var name in new[] { nameof(SelectedSource), nameof(Source), nameof(SelectedItem), nameof(SourceCode) }) OnPropertyChanged(name);
    }
    private void ResetDateTime()
    {
        _defaultDatePending = true; _time = "09:00"; _date = DateTime.Today.AddDays(1);
        OnPropertyChanged(nameof(Time)); OnPropertyChanged(nameof(AppointmentDate));
    }
    private void ClearSessionData()
    {
        _creating = false; _pending = null; _committed = false; _reconciled = false;
        _selected = null; OnPropertyChanged(nameof(Selected)); Detail = null; Appointments.Clear(); ClearDraft();
        Feedback = "Phiên đã thay đổi. Đăng nhập và mở lại màn lịch hẹn."; Refresh();
    }
    private void Error(Exception e)
    {
        if (!IsCurrent) return;
        if (e is AppointmentApiException { Code: "invalidSession" or "permissionDenied" }) { _denied = true; ClearSessionData(); }
        Feedback = e is AppointmentApiException ? e.Message : "Không tải được thông tin. Bản nháp được giữ lại. Hãy thử lại.";
    }
    public async Task LoadAsync(int page = 1)
    {
        if (!CanBrowse || page < 1) return;
        _busy = true; Feedback = "Đang tải danh sách lịch hẹn…"; Refresh();
        try {
            var rows = await _service.ListAsync(page);
            if (!IsCurrent || _denied) return;
            Appointments.Clear(); foreach (var row in rows) Appointments.Add(row);
            _page = page; _selected = null; OnPropertyChanged(nameof(Selected)); Detail = null;
            Feedback = rows.Length == 0 ? "Chưa có lịch trong trang này." : "Chọn một lịch để xem chi tiết.";
        } catch (Exception e) { Error(e); }
        finally { _busy = false; Refresh(); }
    }
    public async Task LoadDetailAsync()
    {
        if (!CanBrowse || Selected is not { } selected) return;
        _busy = true; Detail = null; Feedback = "Đang tải chi tiết…"; Refresh();
        try { var row = await _service.GetAsync(selected.Id); if (IsCurrent && !_denied) { Detail = row; Feedback = "Đang xem lịch; thông tin chỉ đọc."; } }
        catch (Exception e) { Error(e); }
        finally { _busy = false; Refresh(); }
    }
    public async Task NewAsync()
    {
        if (!CanBrowse) return;
        _creating = true; _selected = null; OnPropertyChanged(nameof(Selected)); Detail = null; ClearDraft(); Refresh();
        await LoadSourcesAsync(1);
    }
    public async Task LoadSourcesAsync(int page)
    {
        if (!CanEdit || page < 1) return;
        _busy = true; Feedback = "Đang tải đăng ký…"; Refresh();
        try {
            var rows = await _service.GetSourcesAsync(page);
            if (!IsCurrent || _denied) return;
            Sources.Clear(); foreach (var row in rows) Sources.Add(row);
            _sourcePage = page; _sourceRow = null; _source = null; _item = null; _draftSourceId = null; Items.Clear();
            OnPropertyChanged(nameof(SelectedSource)); OnPropertyChanged(nameof(Source)); OnPropertyChanged(nameof(SelectedItem));
            Feedback = rows.Length == 0 ? "Không có đăng ký trong trang này." : "Chọn đăng ký rồi chọn dòng dịch vụ. Ngày giờ theo máy đang sử dụng.";
        } catch (Exception e) { Error(e); }
        finally { _busy = false; Refresh(); }
    }
    public async Task FindSourceAsync()
    {
        if (!CanEdit) return;
        if (string.IsNullOrWhiteSpace(SourceCode)) { await LoadSourcesAsync(1); return; }
        var code = SourceCode.Trim(); if (code.StartsWith("DK-", StringComparison.OrdinalIgnoreCase)) code = code[3..];
        if (!int.TryParse(code, out var id) || id <= 0) { Feedback = "Nhập mã đăng ký nguyên dương, ví dụ DK-0001."; return; }
        await ReadSourceAsync(id);
    }
    public async Task LoadSourceDetailAsync()
    {
        if (CanEdit && SelectedSource is { } row) await ReadSourceAsync(row.RegistrationId);
    }
    private async Task ReadSourceAsync(int id)
    {
        _busy = true;
        if (_draftSourceId != id) {
            _draftSourceId = id; _source = null; _item = null; Items.Clear();
            ResetDateTime();
            OnPropertyChanged(nameof(Source)); OnPropertyChanged(nameof(SelectedItem));
        }
        Feedback = "Đang tải chi tiết đăng ký…"; Refresh();
        try {
            var source = await _service.GetSourceAsync(id);
            if (!IsCurrent || _denied) return;
            _source = source;
            var oldRow = Sources.FirstOrDefault(x => x.RegistrationId == id);
            _sourceRow = source.Source;
            if (oldRow is null) Sources.Insert(0, _sourceRow);
            else Sources[Sources.IndexOf(oldRow)] = _sourceRow;
            _sourceCode = $"DK-{id:0000}";
            var previousItem = _item;
            Items.Clear();
            foreach (var item in source.Items) Items.Add(item);
            _item = source.Items.FirstOrDefault(i => i.Id == previousItem?.Id && i.Kind == previousItem.Kind);
            if (_defaultDatePending && source.Source.PreferredDate is { } preferred && preferred >= DateOnly.FromDateTime(DateTime.Today)) {
                var fallback = _date;
                _date = preferred.ToDateTime(TimeOnly.MinValue);
                if (!TryTime(out _)) _date = fallback;
                OnPropertyChanged(nameof(AppointmentDate));
            }
            _defaultDatePending = false;
            OnPropertyChanged(nameof(Source)); OnPropertyChanged(nameof(SelectedSource)); OnPropertyChanged(nameof(SourceCode)); OnPropertyChanged(nameof(SelectedItem));
            Feedback = source.Source.CanSchedule ? "Chọn dòng dịch vụ và thời gian hẹn." : source.Source.ReadinessLabel + ". Tải lại đăng ký khi có cập nhật.";
        } catch (Exception e) {
            _source = null; OnPropertyChanged(nameof(Source)); Error(e);
        }
        finally { _busy = false; Refresh(); }
    }
    public async Task SaveAsync()
    {
        if (!CanSave || !TryTime(out var time)) return;
        await PostAsync(new(_source!.Source.RegistrationId, _item!.Kind, _item.Id, time));
    }
    public async Task RetryAsync()
    {
        if (!RetryCommand.CanExecute(null)) return;
        await PostAsync(_pending!);
    }
    private async Task PostAsync(AppointmentRequest request)
    {
        _busy = true; _pending = request; _reconciled = false; Feedback = "Đang lưu lịch…"; Refresh();
        try {
            var saved = await _service.CreateAsync(request);
            if (!IsCurrent || _denied) return;
            _committed = true; Accept(saved); Feedback = "Đã lưu lịch hẹn.";
        } catch (Exception e) {
            if (!IsCurrent) return;
            if (e is AppointmentApiException { Code: "recordedUnreadable" }) _committed = true;
            else if (e is AppointmentApiException { Code: not ("outcomeUnknown" or "appointmentAlreadyExists") }) _pending = null;
            if (e is AppointmentApiException { Code: "appointmentPaymentRequired" or "paymentDataConflict" or "appointmentSourceConflict" }) {
                _source = null; OnPropertyChanged(nameof(Source));
            }
            Error(e);
        } finally { _busy = false; Refresh(); }
    }
    private void Accept(AppointmentRecord saved)
    {
        _pending = null; _committed = false; _reconciled = false; _creating = false;
        if (!Appointments.Any(x => x.Id == saved.Id)) Appointments.Insert(0, saved);
        _selected = saved; OnPropertyChanged(nameof(Selected)); Detail = saved; ClearDraft();
    }
    public async Task ReconcileAsync()
    {
        if (_busy || !HasAccess || _pending is not { } request) return;
        _busy = true; _reconciled = false; Feedback = "Đang đối chiếu lịch theo đăng ký và thời điểm…"; Refresh();
        try {
            var rows = await _service.ListAsync(1, request);
            if (!IsCurrent || _denied) return;
            var saved = rows.SingleOrDefault(x => AppointmentApiService.Matches(x, request));
            if (saved is not null) { Accept(saved); Feedback = "Đã tìm thấy lịch đã lưu."; }
            else { _reconciled = true; Feedback = _committed ? "Chưa mở được chi tiết lịch đã lưu. Hãy đối chiếu lại." : "Chưa tìm thấy lịch. Có thể thử lưu lại đúng yêu cầu cũ."; }
        } catch (Exception e) { Error(e); }
        finally { _busy = false; Refresh(); }
    }
}
