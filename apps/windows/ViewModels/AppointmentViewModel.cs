using System.Collections.ObjectModel;
using System.Globalization;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class AppointmentViewModel : ViewModelBase
{
    private readonly IAppointmentService _service;
    private readonly IdentityApiService _identity;
    private readonly int _revision;
    private bool _busy, _denied, _creating, _committed, _reconciled;
    private int _page = 1, _sourcePage = 1;
    private string _feedback = "", _sourceCode = "", _time = "09:00", _location = "";
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
        _identity.SessionCleared += (_, _) => ClearSessionData();
        ReloadCommand = new(async _ => await LoadAsync(Page), _ => CanBrowse);
        PreviousCommand = new(async _ => await LoadAsync(Page - 1), _ => CanBrowse && Page > 1);
        NextCommand = new(async _ => await LoadAsync(Page + 1), _ => CanBrowse && Appointments.Count == AppointmentApiService.PageSize);
        NewCommand = new(async _ => await NewAsync(), _ => CanBrowse);
        CancelCommand = new(_ => { _creating = false; ClearDraft(); Refresh(); }, _ => CanEdit);
        FindSourceCommand = new(async _ => await FindSourceAsync(), _ => CanEdit);
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
    public bool CanBrowse => HasAccess && !_busy && _pending is null;
    public bool CanEdit => HasAccess && _creating && !_busy && _pending is null;
    public bool HasPending => HasAccess && _pending is not null;
    public bool CanSave => CanEdit && _source is { Status: "DaDangKy" } && _item is not null &&
        _source.Items.Contains(_item) && TryTime(out _) && Location.Trim().Length is >= 1 and <= 200;
    public int Page => _page;
    public int SourcePage => _sourcePage;
    public bool NoResults => HasAccess && !_busy && Appointments.Count == 0;
    public bool NoDetail => IsViewing && Detail is null;
    public string Feedback { get => _feedback; private set => Set(ref _feedback, value); }
    public string SourceCode { get => _sourceCode; set { if (CanEdit) Set(ref _sourceCode, value); } }
    public string Time { get => _time; set { if (CanEdit) { Set(ref _time, value); Refresh(); } } }
    public DateTime? AppointmentDate { get => _date; set { if (CanEdit) { Set(ref _date, value); Refresh(); } } }
    public string Location { get => _location; set { if (CanEdit) { Set(ref _location, value); Refresh(); } } }
    public AppointmentSource? Source => _source;
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
    public AppointmentRecord? Detail { get => _detail; private set { Set(ref _detail, value); Refresh(); } }
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
        foreach (var name in new[] { nameof(HasAccess), nameof(NoAccess), nameof(IsBusy), nameof(IsCreating), nameof(IsViewing), nameof(CanBrowse), nameof(CanEdit), nameof(HasPending), nameof(CanSave), nameof(Page), nameof(SourcePage), nameof(NoResults), nameof(NoDetail) }) OnPropertyChanged(name);
        foreach (var command in new[] { ReloadCommand, PreviousCommand, NextCommand, NewCommand, CancelCommand, FindSourceCommand, PreviousSourceCommand, NextSourceCommand, SaveCommand, ReconcileCommand, RetryCommand }) command?.NotifyCanExecuteChanged();
    }
    private void ClearDraft()
    {
        _sourceRow = null; _source = null; _item = null; _sourceCode = ""; _time = "09:00"; _date = DateTime.Today.AddDays(1); _location = "";
        Items.Clear(); Sources.Clear(); _sourcePage = 1;
        foreach (var name in new[] { nameof(SelectedSource), nameof(Source), nameof(SelectedItem), nameof(SourceCode), nameof(Time), nameof(AppointmentDate), nameof(Location) }) OnPropertyChanged(name);
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
        Feedback = e is AppointmentApiException ? e.Message : "Không kết nối hoặc đọc được API. Bản nháp được giữ lại; hãy thử lại.";
    }
    public async Task LoadAsync(int page = 1)
    {
        if (!CanBrowse || page < 1) return;
        _busy = true; Feedback = "Đang tải lịch từ API / SQL…"; Refresh();
        try {
            var rows = await _service.ListAsync(page);
            if (!IsCurrent || _denied) return;
            Appointments.Clear(); foreach (var row in rows) Appointments.Add(row);
            _page = page; _selected = null; OnPropertyChanged(nameof(Selected)); Detail = null;
            Feedback = rows.Length == 0 ? "Chưa có lịch trong trang này." : "Lịch hẹn được đọc từ SQL; chưa phải kết luận đủ điều kiện tiêm.";
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
            _sourcePage = page; _sourceRow = null; _source = null; _item = null; Items.Clear();
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
        if (CanEdit && SelectedSource is { } row) await ReadSourceAsync(row.Id);
    }
    private async Task ReadSourceAsync(int id)
    {
        _busy = true; _source = null; _item = null; Items.Clear();
        OnPropertyChanged(nameof(Source)); OnPropertyChanged(nameof(SelectedItem)); Feedback = "Đang đọc dịch vụ đã chốt…"; Refresh();
        try {
            var source = await _service.GetSourceAsync(id);
            if (!IsCurrent || _denied) return;
            _source = source;
            _sourceRow = Sources.FirstOrDefault(x => x.Id == source.Id) ?? new(source.Id, source.Customer, source.Recipient);
            if (!Sources.Contains(_sourceRow)) Sources.Insert(0, _sourceRow);
            _sourceCode = $"DK-{source.Id:0000}";
            foreach (var item in source.Items) Items.Add(item);
            OnPropertyChanged(nameof(Source)); OnPropertyChanged(nameof(SelectedSource)); OnPropertyChanged(nameof(SourceCode));
            Feedback = "Chọn dòng dịch vụ và nhập thời gian tương lai, địa điểm. Không tự tạo các mũi của gói.";
        } catch (Exception e) { Error(e); }
        finally { _busy = false; Refresh(); }
    }
    public async Task SaveAsync()
    {
        if (!CanSave || !TryTime(out var time)) return;
        await PostAsync(new(_source!.Id, _item!.Kind, _item.Id, time, Location.Trim()));
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
            _committed = true; Accept(saved); Feedback = "Đã lưu lịch (HTTP 201). Có thể tải lại để xem từ SQL.";
        } catch (Exception e) {
            if (!IsCurrent) return;
            if (e is AppointmentApiException { Code: "recordedUnreadable" }) _committed = true;
            else if (e is AppointmentApiException { Code: not ("outcomeUnknown" or "appointmentAlreadyExists") }) _pending = null;
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
        _busy = true; _reconciled = false; Feedback = "Đang GET đối chiếu nguồn và thời điểm…"; Refresh();
        try {
            var rows = await _service.ListAsync(1, request);
            if (!IsCurrent || _denied) return;
            var saved = rows.SingleOrDefault(x => AppointmentApiService.Matches(x, request));
            if (saved is not null) { Accept(saved); Feedback = "Đã tìm thấy lịch trong SQL. Không gửi lại POST."; }
            else { _reconciled = true; Feedback = _committed ? "Đã nhận 201, GET chưa thấy lịch. Tiếp tục GET, không gửi lại." : "GET chưa thấy lịch. Có thể thử lại đúng yêu cầu cũ; API vẫn chặn lịch trùng."; }
        } catch (Exception e) { Error(e); }
        finally { _busy = false; Refresh(); }
    }
}
