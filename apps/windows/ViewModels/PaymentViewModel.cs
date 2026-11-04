using System.Collections.ObjectModel;
using TiemChung.Contracts;
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
    private PaymentDraft? _draft;
    private RecordPaymentRequest? _reviewedRequest;
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
    public Func<string, bool>? ConfirmDiscard { get; set; }
    public PaymentDraft? Draft { get => _draft; private set { Set(ref _draft, value); Refresh(); } }
    public string ReviewText { get; private set; } = "";
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
            if (_saving || _confirmation is not null || _updatingRow || _selected == value || !MayDiscard()) return;
            _selected = value;
            _selectionVersion++; Draft = null; Detail = null; _detailBusy = false; _reconciled = false; Feedback = "";
            OnPropertyChanged();
            Refresh();
        }
    }
    public bool UnknownOutcome => HasSelection && _unknown.Contains(SelectedRegistration!.Id);
    public bool CanAcknowledgeRisk => CanBrowse && !_detailBusy && UnknownOutcome && _reconciled &&
        Detail is { CanRecordPayment: true } or { CanConfirmPendingPayment: true };
    public bool ShowEditor => Draft is not null && Detail is { CanRecordPayment: true };
    public bool ShowPendingAction => !IsReadOnly && Detail is { IsPending: true, CanConfirmPendingPayment: true };
    public bool CanRecord => ShowEditor && CanWrite;
    public bool CanConfirmPending => ShowPendingAction && CanWrite;
    private bool CanWrite => CanBrowse && !_detailBusy && _identity.CanWritePayments && Detail is { } d &&
        SelectedRegistration?.Id == d.RegistrationId && !_unknown.Contains(d.RegistrationId) &&
        !_committed.Contains(d.RegistrationId) && !_blocked.Contains(d.RegistrationId);
    private bool MayDiscard() => Draft?.IsDirty != true ||
        ConfirmDiscard?.Invoke("Bỏ bản nháp thanh toán chưa lưu?") == true;

    private void Refresh()
    {
        foreach (var name in new[] { nameof(HasAccess), nameof(NoAccess), nameof(IsReadOnly), nameof(IsSaving), nameof(IsLoading),
            nameof(CanBrowse), nameof(CanSearch), nameof(NoResults), nameof(NoSelection), nameof(HasSelection), nameof(Page),
            nameof(UnknownOutcome), nameof(CanAcknowledgeRisk), nameof(ShowEditor), nameof(ShowPendingAction),
            nameof(CanRecord), nameof(CanConfirmPending) }) OnPropertyChanged(name);
        foreach (var command in new[] { SearchCommand, ReloadCommand, PreviousCommand, NextCommand, ReconcileCommand }) command?.NotifyCanExecuteChanged();
    }
    private void ClearSessionData()
    {
        _selectionVersion++; _selected = null; OnPropertyChanged(nameof(SelectedRegistration)); Detail = null; Draft = null;
        Registrations.Clear(); _unknown.Clear(); _committed.Clear(); _blocked.Clear(); _confirmation = null;
        _reviewedRequest = null; ReviewText = "";
        Search = ""; _appliedSearch = ""; _page = 1; _reconciled = false;
        Feedback = "Phiên đã thay đổi. Vui lòng đăng nhập và mở lại màn Thu ngân."; ListFeedback = ""; Refresh();
    }
    private void HandleError(Exception error, bool list = false)
    {
        if (!IsCurrent) return;
        if (error is PaymentApiException { Code: "invalidSession" or "permissionDenied" }) { _denied = true; ClearSessionData(); }
        var message = error is PaymentApiException ? error.Message : "Chưa tải được dữ liệu. Kiểm tra kết nối và thử lại.";
        if (list) ListFeedback = message; else Feedback = message;
    }
    public Task SearchAsync() => CanSearch ? LoadPageAsync(1, Search.Trim()) : Task.CompletedTask;
    public Task LoadPageAsync(int page) => LoadPageAsync(page, _appliedSearch);
    private async Task LoadPageAsync(int page, string search)
    {
        if (!CanBrowse || page < 1 || !MayDiscard()) return;
        _appliedSearch = search;
        _listBusy = true; ListFeedback = "Đang tải đăng ký…";
        Draft = null;
        SelectedRegistration = null; Registrations.Clear(); Refresh();
        try {
            var rows = await _service.SearchAsync(_appliedSearch, page);
            if (!IsCurrent || _denied) return;
            foreach (var row in rows) Registrations.Add(row);
            _page = page; ListFeedback = "";
        }
        catch (Exception e) { HandleError(e, list: true); }
        finally { _listBusy = false; Refresh(); }
    }
    public async Task LoadDetailAsync()
    {
        if (!CanBrowse || _detailBusy || SelectedRegistration is not { } row) return;
        if (!UnknownOutcome) {
            if (!MayDiscard()) return;
            Draft = null;
        }
        var version = _selectionVersion;
        _detailBusy = true; _reconciled = false;
        if (!_committed.Contains(row.Id)) Detail = null;
        Feedback = "Đang tải chi tiết đăng ký…"; Refresh();
        try { await ReadDetailCoreAsync(row.Id, version); }
        catch (Exception e) {
            if (version == _selectionVersion) {
                HandleError(e);
                if (HasAccess && _committed.Contains(row.Id)) Feedback = "Giao dịch đã xác nhận. Vui lòng đối chiếu để tải lại biên nhận.";
            }
        }
        finally { if (version == _selectionVersion) { _detailBusy = false; Refresh(); } }
    }
    private async Task ReadDetailCoreAsync(int id, int version)
    {
        var detail = await _service.GetAsync(id);
        if (!IsCurrent || _denied || version != _selectionVersion || SelectedRegistration?.Id != id) return;
        Detail = detail; _reconciled = true;
        if (detail.HasReceipt) { _committed.Add(id); _unknown.Remove(id); }
        if (detail.IsPending && _blocked.Contains(id)) _blocked.Remove(id);
        Draft = detail.CanRecordPayment && !IsReadOnly && !_committed.Contains(id)
            ? Draft ?? new(detail.RegistrationContact, detail.TotalDue) : null;
        var index = Registrations.IndexOf(SelectedRegistration);
        if (index >= 0) {
            // Update the row without changing selection or starting another GET.
            var updated = SelectedRegistration with { TotalDue = detail.TotalDue, PaidAmount = detail.PaidAmount,
                RemainingAmount = detail.RemainingAmount, PaymentId = detail.Payment?.Id, PaymentState = detail.PaymentState };
            _updatingRow = true;
            try { _selected = updated; Registrations[index] = updated; OnPropertyChanged(nameof(SelectedRegistration)); }
            finally { _updatingRow = false; }
        }
        Feedback = _unknown.Contains(id) ? "Chưa thấy thanh toán thành công. Xác nhận đã đối chiếu để mở lại thao tác."
            : _committed.Contains(id) && !detail.HasReceipt ? "Giao dịch đã xác nhận. Vui lòng đối chiếu để tải lại biên nhận."
            : detail.State;
        Refresh();
    }
    public RegistrationPayment? BeginConfirmation()
    {
        if (CanRecord) {
            _reviewedRequest = Draft!.Prepare();
            if (_reviewedRequest is null) return null;
            var payer = _reviewedRequest.Payer!;
            ReviewText = $"Người thanh toán: {payer.FullName} · {payer.PhoneNumber}\n" + Draft.Review(_reviewedRequest);
        }
        else if (CanConfirmPending && Detail?.Payment is { } pending) {
            ReviewText = $"Người thanh toán: {pending.Payer?.FullName}\nPhương thức: {pending.MethodLabel}\n" +
                $"Mã giao dịch: {pending.Code}\n{pending.Provider} · {pending.MaskedIdentifier}\nTham chiếu: {pending.Reference}";
        }
        else return null;
        _confirmation = Detail; Refresh(); return _confirmation;
    }
    public async Task ConfirmAsync(bool confirmed)
    {
        var snapshot = _confirmation; _confirmation = null;
        var request = _reviewedRequest; _reviewedRequest = null; ReviewText = "";
        if (!confirmed || snapshot is null || !ReferenceEquals(snapshot, Detail) ||
            (snapshot.IsPending ? !CanConfirmPending : !CanRecord || request is null)) { Refresh(); return; }
        var id = snapshot.RegistrationId; var version = _selectionVersion;
        _saving = true; Feedback = "Đang xác nhận thanh toán…"; Refresh();
        try {
            try {
                if (snapshot.IsPending) await _service.ConfirmPendingAsync(id);
                else await _service.RecordAsync(id, request!);
                if (!IsCurrent || _denied || version != _selectionVersion) return;
                _committed.Add(id); Draft = null; Detail = null;
                Feedback = "Đã thanh toán. Đang tải biên nhận…";
            }
            catch (PaymentApiException e) when (e.Code is "paymentAlreadyRecorded" or "paymentNotRequired" or
                "paymentNotPending" or "paymentDataConflict" or "recordedUnreadable") {
                if (!IsCurrent || version != _selectionVersion) return;
                if (e.Code == "recordedUnreadable") { _committed.Add(id); Draft = null; }
                _blocked.Add(id); Detail = null; Feedback = e.Message;
            }
            // A successful POST or known 409 is followed only by GET, never another POST.
            try { await ReadDetailCoreAsync(id, version); }
            catch (Exception e) {
                if (IsCurrent && version == _selectionVersion) {
                    HandleError(e);
                    if (HasAccess) Feedback = _committed.Contains(id)
                        ? "Đã thanh toán. Chưa tải được biên nhận. Vui lòng đối chiếu thanh toán."
                        : "Chưa tải lại được kết quả. Vui lòng đối chiếu trước khi thanh toán lại.";
                }
            }
        }
        catch (Exception e) {
            if (IsCurrent && version == _selectionVersion) {
                if (e is PaymentApiException { Code: "outcomeUnknown" } || e is not PaymentApiException) { _unknown.Add(id); _reconciled = false; }
                HandleError(e);
                if (UnknownOutcome) Feedback = "Chưa xác định kết quả. Vui lòng đối chiếu thanh toán trước khi thao tác lại.";
            }
        }
        finally { _saving = false; Refresh(); }
    }
    public void AcknowledgeRisk()
    {
        if (!CanAcknowledgeRisk) return;
        _unknown.Remove(SelectedRegistration!.Id); _reconciled = false;
        Feedback = "Đã đối chiếu. Có thể thực hiện lại thao tác thanh toán."; Refresh();
    }
}
