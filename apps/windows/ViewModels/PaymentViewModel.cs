using System.Collections.ObjectModel;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class PaymentViewModel : ViewModelBase
{
    private readonly IPaymentService _service;
    private readonly IdentityApiService _identity;
    private readonly int _revision;
    // Keep locks per registration when the user changes selection or searches again.
    private readonly HashSet<int> _unknown = [], _committed = [], _blocked = [];
    private PaymentRegistration? _selected;
    private RegistrationPayment? _detail, _confirmation;
    private string _search = "", _appliedSearch = "", _feedback = "", _listFeedback = "";
    private int _page = 1, _selectionVersion;
    private bool _listBusy, _detailBusy, _saving, _denied, _reconciled, _updatingRow;

    public PaymentViewModel(IPaymentService? service = null, IdentityApiService? identity = null)
    {
        _identity = identity ?? IdentityApiService.Current;
        _revision = _identity.SessionRevision;
        _service = service ?? new PaymentApiService(_identity);
        _identity.SessionCleared += (_, _) => ClearSessionData();
        SearchCommand = new RelayCommand(async _ => await SearchAsync(), _ => CanSearch);
        ReloadCommand = new RelayCommand(async _ => await LoadPageAsync(Page), _ => CanBrowse);
        PreviousCommand = new RelayCommand(async _ => await LoadPageAsync(Page - 1), _ => CanBrowse && Page > 1);
        NextCommand = new RelayCommand(async _ => await LoadPageAsync(Page + 1), _ => CanBrowse && Registrations.Count == PaymentApiService.PageSize);
        ReconcileCommand = new RelayCommand(async _ => await LoadDetailAsync(), _ => CanBrowse && SelectedRegistration is not null && !_detailBusy);
    }
    public ObservableCollection<PaymentRegistration> Registrations { get; } = [];
    public RelayCommand SearchCommand { get; }
    public RelayCommand ReloadCommand { get; }
    public RelayCommand PreviousCommand { get; }
    public RelayCommand NextCommand { get; }
    public RelayCommand ReconcileCommand { get; }
    private bool IsCurrent => _identity.IsSignedIn && _identity.SessionRevision == _revision;
    public bool HasAccess => IsCurrent && _identity.CanReadPayments && !_denied;
    public bool NoAccess => !HasAccess;
    public bool IsReadOnly => HasAccess && !_identity.CanWritePayments;
    public bool IsSaving => _saving;
    public bool IsLoading => _listBusy || _detailBusy;
    public bool CanBrowse => HasAccess && !_listBusy && !_saving && _confirmation is null;
    public bool CanSearch => CanBrowse && Search.Trim().Length <= 100;
    public bool NoResults => HasAccess && !_listBusy && Registrations.Count == 0;
    public bool NoSelection => HasAccess && SelectedRegistration is null;
    public bool HasSelection => HasAccess && SelectedRegistration is not null;
    public int Page => _page;
    public string Search { get => _search; set { Set(ref _search, value); Refresh(); } }
    public string Feedback { get => _feedback; private set => Set(ref _feedback, value); }
    public string ListFeedback { get => _listFeedback; private set => Set(ref _listFeedback, value); }
    public RegistrationPayment? Detail { get => _detail; private set { Set(ref _detail, value); Refresh(); } }
    public PaymentRegistration? SelectedRegistration
    {
        get => _selected;
        set {
            if (_saving || _confirmation is not null || _updatingRow || !Set(ref _selected, value)) return;
            _selectionVersion++; Detail = null; _detailBusy = false; _reconciled = false; Feedback = "";
            Refresh();
        }
    }
    public bool UnknownOutcome => HasSelection && _unknown.Contains(SelectedRegistration!.Id);
    public bool CanAcknowledgeRisk => CanBrowse && !_detailBusy && UnknownOutcome && _reconciled && Detail is { Payment: null };
    public bool CanRecord => CanBrowse && !_detailBusy && _identity.CanWritePayments && Detail is {
        CanRecordPayment: true, RequiresPayment: true, TotalDue: > 0, Payment: null
    } d && SelectedRegistration?.Id == d.RegistrationId && !_unknown.Contains(d.RegistrationId)
        && !_committed.Contains(d.RegistrationId) && !_blocked.Contains(d.RegistrationId);

    private void Refresh()
    {
        foreach (var name in new[] { nameof(HasAccess), nameof(NoAccess), nameof(IsReadOnly), nameof(IsSaving), nameof(IsLoading), nameof(CanBrowse), nameof(CanSearch), nameof(NoResults), nameof(NoSelection), nameof(HasSelection), nameof(Page), nameof(UnknownOutcome), nameof(CanAcknowledgeRisk), nameof(CanRecord) }) OnPropertyChanged(name);
        foreach (var command in new[] { SearchCommand, ReloadCommand, PreviousCommand, NextCommand, ReconcileCommand }) command?.NotifyCanExecuteChanged();
    }
    private void ClearSessionData()
    {
        _selectionVersion++; _selected = null; OnPropertyChanged(nameof(SelectedRegistration)); Detail = null;
        Registrations.Clear(); _unknown.Clear(); _committed.Clear(); _blocked.Clear(); _confirmation = null;
        Search = ""; _appliedSearch = ""; _page = 1; _reconciled = false;
        Feedback = "Phiên đã thay đổi. Vui lòng đăng nhập và mở lại màn Thu ngân."; ListFeedback = ""; Refresh();
    }
    private void HandleError(Exception error, bool list = false)
    {
        if (!IsCurrent) return;
        if (error is PaymentApiException { Code: "invalidSession" or "permissionDenied" }) { _denied = true; ClearSessionData(); }
        var message = error is PaymentApiException ? error.Message : "Không kết nối hoặc đọc được API. Hãy thử lại thao tác tải / đối chiếu bằng GET.";
        if (list) ListFeedback = message; else Feedback = message;
    }
    public async Task SearchAsync()
    {
        if (!CanSearch) return;
        _appliedSearch = Search.Trim(); await LoadPageAsync(1);
    }
    public async Task LoadPageAsync(int page)
    {
        if (!CanBrowse || page < 1) return;
        _listBusy = true; ListFeedback = "Đang tải đăng ký từ API / SQL…";
        SelectedRegistration = null; Registrations.Clear(); Refresh();
        try {
            var rows = await _service.SearchAsync(_appliedSearch, page);
            if (!IsCurrent || _denied) return;
            foreach (var row in rows) Registrations.Add(row);
            _page = page; ListFeedback = rows.Length == 0 ? "Không có kết quả trong trang này." : "Giá và số tiền được đọc từ Payment API / SQL.";
        }
        catch (Exception e) { HandleError(e, list: true); }
        finally { _listBusy = false; Refresh(); }
    }
    public async Task LoadDetailAsync()
    {
        if (!CanBrowse || _detailBusy || SelectedRegistration is not { } row) return;
        var version = _selectionVersion;
        _detailBusy = true; _reconciled = false;
        if (!_committed.Contains(row.Id)) Detail = null;
        Feedback = "Đang đọc chi tiết từ API…"; Refresh();
        try { await ReadDetailCoreAsync(row.Id, version); }
        catch (Exception e) {
            if (version == _selectionVersion) {
                HandleError(e);
                if (HasAccess && _committed.Contains(row.Id)) Feedback = "API đã xác nhận giao dịch trước đó; GET tải lại lỗi. Chỉ đối chiếu bằng GET, không ghi lại.";
            }
        }
        finally { if (version == _selectionVersion) { _detailBusy = false; Refresh(); } }
    }
    private async Task ReadDetailCoreAsync(int id, int version)
    {
        var detail = await _service.GetAsync(id);
        if (!IsCurrent || _denied || version != _selectionVersion || SelectedRegistration?.Id != id) return;
        Detail = detail; _reconciled = true;
        if (detail.Payment is not null) { _committed.Add(id); _unknown.Remove(id); }
        var index = Registrations.IndexOf(SelectedRegistration);
        if (index >= 0) {
            // Update the row without changing selection or starting another GET.
            var updated = SelectedRegistration with { TotalDue = detail.TotalDue, PaidAmount = detail.PaidAmount, RemainingAmount = detail.RemainingAmount, PaymentId = detail.Payment?.Id };
            _updatingRow = true;
            try { _selected = updated; Registrations[index] = updated; OnPropertyChanged(nameof(SelectedRegistration)); }
            finally { _updatingRow = false; }
        }
        Feedback = _unknown.Contains(id) ? "GET chưa thấy giao dịch. Kết quả POST trước vẫn chưa chắc chắn; chỉ mở lại sau khi xác nhận rủi ro."
            : _committed.Contains(id) && detail.Payment is null ? "Đã nhận 201 trước đó. GET chưa xác nhận được giao dịch; tiếp tục đối chiếu, không ghi lại."
            : detail.State;
        Refresh();
    }
    public RegistrationPayment? BeginConfirmation()
    {
        if (!CanRecord) return null;
        _confirmation = Detail; Refresh(); return _confirmation;
    }
    public async Task ConfirmAsync(bool confirmed)
    {
        var snapshot = _confirmation; _confirmation = null;
        if (!confirmed || snapshot is null || !ReferenceEquals(snapshot, Detail) || !CanRecord) { Refresh(); return; }
        var id = snapshot.RegistrationId; var version = _selectionVersion;
        _saving = true; Feedback = "Đang ghi nhận thanh toán mô phỏng…"; Refresh();
        try {
            try {
                var saved = await _service.RecordAsync(id);
                if (!IsCurrent || _denied || version != _selectionVersion) return;
                _committed.Add(id); Detail = saved;
                Feedback = "Đã ghi nhận thanh toán mô phỏng (HTTP 201). Đang đọc lại bằng GET…";
            }
            catch (PaymentApiException e) when (e.Code is "paymentAlreadyRecorded" or "paymentNotRequired" or "recordedUnreadable") {
                if (!IsCurrent || version != _selectionVersion) return;
                if (e.Code == "recordedUnreadable") _committed.Add(id);
                _blocked.Add(id); Detail = null; Feedback = e.Message;
            }
            // A successful POST or known 409 is followed only by GET, never another POST.
            try { await ReadDetailCoreAsync(id, version); }
            catch (Exception e) {
                if (IsCurrent && version == _selectionVersion) {
                    HandleError(e);
                    if (HasAccess) Feedback = _committed.Contains(id)
                        ? "Đã ghi nhận (HTTP 201), nhưng GET tải lại lỗi. Chỉ đối chiếu bằng GET, không ghi lại."
                        : "API đã từ chối ghi tiếp; GET tải lại lỗi. Dùng Đối chiếu bằng GET, không POST lại.";
                }
            }
        }
        catch (Exception e) {
            if (IsCurrent && version == _selectionVersion) {
                if (e is PaymentApiException { Code: "outcomeUnknown" } || e is not PaymentApiException) { _unknown.Add(id); _reconciled = false; }
                if (e is PaymentApiException { Code: "paymentDataConflict" }) { _blocked.Add(id); Detail = null; }
                HandleError(e);
                if (UnknownOutcome) Feedback = "Kết quả POST chưa xác định. Đã khóa ghi lại đăng ký này; dùng Đối chiếu bằng GET.";
            }
        }
        finally { _saving = false; Refresh(); }
    }
    public void AcknowledgeRisk()
    {
        if (!CanAcknowledgeRisk) return;
        _unknown.Remove(SelectedRegistration!.Id); _reconciled = false;
        Feedback = "Đã xác nhận rủi ro sau GET đối chiếu. Có thể mở lại dialog ghi nhận; API vẫn kiểm tra giao dịch trùng."; Refresh();
    }
}
