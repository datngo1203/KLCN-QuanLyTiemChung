using System.Collections.ObjectModel;
using System.Globalization;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class RegistrationViewModel : ViewModelBase
{
    private readonly IRegistrationService _service;
    private readonly IdentityApiService _identity;
    private readonly int _sessionRevision;
    private CustomerRecord? _customer;
    private RecipientRecord? _recipient;
    private RegistrationCatalogItem? _catalogItem;
    private RegistrationSummary? _selectedRegistration;
    private RegistrationDetail? _detail;
    private string _customerSearch = "", _catalogSearch = "", _quantityText = "1", _feedback = "", _catalogNotice = "";
    private bool _isBusy, _isCreating, _denied, _unknownOutcome, _reviewedList, _catalogReady, _catalogReviewRequired, _catalogConfirmed, _draftSaved;
    private int _page = 1, _customerPage = 1;
    private int? _detailId;
    private string _appliedCustomerSearch = "";

    public RegistrationViewModel(IRegistrationService? service = null, IdentityApiService? identity = null)
    {
        _identity = identity ?? IdentityApiService.Current;
        _sessionRevision = _identity.SessionRevision;
        _service = service ?? new RegistrationApiService(_identity);
        _identity.SessionCleared += (_, _) => ClearSessionData();
        ReloadCommand = new RelayCommand(async _ => await LoadAsync(), _ => CanInteract);
        PreviousCommand = new RelayCommand(async _ => await LoadPageAsync(Page - 1), _ => CanInteract && Page > 1);
        NextCommand = new RelayCommand(async _ => await LoadPageAsync(Page + 1), _ => CanInteract && Registrations.Count == RegistrationApiService.PageSize);
        NewCommand = new RelayCommand(async _ => await NewAsync(), _ => CanInteract && (!_unknownOutcome || _reviewedList));
        ListCommand = new RelayCommand(async _ => { IsCreating = false; await LoadAsync(); }, _ => CanInteract);
        ResumeDraftCommand = new RelayCommand(_ => IsCreating = true, _ => CanEditDraft && Customer is not null);
        SearchCustomersCommand = new RelayCommand(async _ => await SearchCustomersAsync(), _ => CanInteract);
        PreviousCustomersCommand = new RelayCommand(async _ => await LoadCustomersAsync(_customerPage - 1), _ => CanInteract && _customerPage > 1);
        NextCustomersCommand = new RelayCommand(async _ => await LoadCustomersAsync(_customerPage + 1), _ => CanInteract && Customers.Count == RegistrationApiService.PageSize);
        LoadRecipientsCommand = new RelayCommand(async _ => await LoadRecipientsAsync(), _ => CanInteract && Customer is not null);
        ReloadCatalogCommand = new RelayCommand(async _ => await RunAsync(ReloadCatalogCoreAsync), _ => CanInteract);
        AddLineCommand = new RelayCommand(_ => AddLine(), _ => CanEditDraft && _catalogReady && CatalogItem is not null && QuantityValid);
        RemoveLineCommand = new RelayCommand(x => { if (x is RegistrationLine line) { Lines.Remove(line); Refresh(); } }, _ => CanEditDraft);
        SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSubmit);
        OpenDetailCommand = new RelayCommand(async _ => { if (SelectedRegistration is { } row) await OpenDetailAsync(row.Id); }, _ => CanInteract && SelectedRegistration is not null);
        RetryDetailCommand = new RelayCommand(async _ => { if (_detailId is { } id) await OpenDetailAsync(id); }, _ => CanInteract && _detailId is not null);
    }

    public ObservableCollection<CustomerRecord> Customers { get; } = [];
    public ObservableCollection<RecipientRecord> Recipients { get; } = [];
    public ObservableCollection<RegistrationCatalogItem> Catalog { get; } = [];
    public ObservableCollection<RegistrationSummary> Registrations { get; } = [];
    public ObservableCollection<RegistrationLine> Lines { get; } = [];
    public RelayCommand ReloadCommand { get; }
    public RelayCommand PreviousCommand { get; }
    public RelayCommand NextCommand { get; }
    public RelayCommand NewCommand { get; }
    public RelayCommand ListCommand { get; }
    public RelayCommand ResumeDraftCommand { get; }
    public RelayCommand SearchCustomersCommand { get; }
    public RelayCommand PreviousCustomersCommand { get; }
    public RelayCommand NextCustomersCommand { get; }
    public RelayCommand LoadRecipientsCommand { get; }
    public RelayCommand ReloadCatalogCommand { get; }
    public RelayCommand AddLineCommand { get; }
    public RelayCommand RemoveLineCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand OpenDetailCommand { get; }
    public RelayCommand RetryDetailCommand { get; }
    private bool IsCurrent => _identity.IsSignedIn && _identity.SessionRevision == _sessionRevision;
    public bool HasAccess => IsCurrent && _identity.CanUseRegistrations && !_denied;
    public bool NoAccess => !HasAccess;
    public bool IsBusy => _isBusy;
    public bool CanInteract => HasAccess && !_isBusy;
    public bool CanEditDraft => CanInteract && !_unknownOutcome && !_draftSaved;
    public bool IsCreating { get => _isCreating; private set { Set(ref _isCreating, value); Refresh(); } }
    public bool ShowList => HasAccess && !IsCreating;
    public bool ShowDraft => HasAccess && IsCreating;
    public bool ShowDetail => HasAccess && Detail is not null;
    public bool HasDetailId => HasAccess && _detailId is not null;
    public int Page => _page;
    public string CustomerPageLabel => $"Trang khách hàng {_customerPage}";
    public string Feedback { get => _feedback; private set => Set(ref _feedback, value); }
    public string CatalogNotice { get => _catalogNotice; private set { Set(ref _catalogNotice, value); OnPropertyChanged(nameof(HasCatalogNotice)); } }
    public bool HasCatalogNotice => CatalogNotice.Length > 0;
    public bool NeedsCatalogConfirmation => _catalogReviewRequired && _catalogReady;
    public bool CatalogConfirmed { get => _catalogConfirmed; set { Set(ref _catalogConfirmed, value); Refresh(); } }
    public bool UnknownOutcome => _unknownOutcome;
    public string NewLabel => _unknownOutcome ? "Đã tra cứu, tạo lại từ đầu" : "Tạo đăng ký / Tạo tiếp";
    public CustomerRecord? Customer { get => _customer; set { if (Set(ref _customer, value)) { Recipient = null; Recipients.Clear(); Refresh(); } } }
    public RecipientRecord? Recipient { get => _recipient; set { Set(ref _recipient, value); Refresh(); } }
    public RegistrationCatalogItem? CatalogItem { get => _catalogItem; set { Set(ref _catalogItem, value); OnPropertyChanged(nameof(SelectedComponents)); Refresh(); } }
    public string SelectedComponents => CatalogItem?.ComponentLabel ?? "";
    public RegistrationSummary? SelectedRegistration { get => _selectedRegistration; set { Set(ref _selectedRegistration, value); Refresh(); } }
    public RegistrationDetail? Detail { get => _detail; private set { Set(ref _detail, value); Refresh(); } }
    public string CustomerSearch { get => _customerSearch; set => Set(ref _customerSearch, value); }
    public string CatalogSearch { get => _catalogSearch; set { Set(ref _catalogSearch, value); OnPropertyChanged(nameof(FilteredCatalog)); } }
    public IEnumerable<RegistrationCatalogItem> FilteredCatalog => Catalog.Where(x => x.Label.Contains(CatalogSearch.Trim(), StringComparison.CurrentCultureIgnoreCase));
    public string QuantityText { get => _quantityText; set { Set(ref _quantityText, value); Refresh(); } }
    public bool QuantityValid => int.TryParse(QuantityText, NumberStyles.None, CultureInfo.InvariantCulture, out var q) && q is >= 1 and <= 100;
    public decimal Subtotal => Lines.Sum(x => x.LineTotal);
    public string FormError => Customer is null ? "Chọn khách hàng." : Recipient is null || Recipient.CustomerId != Customer.Id ? "Chọn người được tiêm thuộc khách hàng." : Lines.Count == 0 ? "Thêm ít nhất một dịch vụ." : Lines.Count > 100 ? "Tối đa 100 dòng dịch vụ." : Lines.Any(x => !x.Available) ? "Xóa dịch vụ không còn trong Catalog." : Subtotal > 9999999999999999m ? "Tổng tiền vượt giới hạn VND." : "";
    public bool CanSubmit => CanEditDraft && IsCreating && _catalogReady && (!_catalogReviewRequired || CatalogConfirmed) && FormError.Length == 0;

    private void Refresh()
    {
        foreach (var name in new[] { nameof(HasAccess), nameof(NoAccess), nameof(IsBusy), nameof(CanInteract), nameof(CanEditDraft), nameof(ShowList), nameof(ShowDraft), nameof(ShowDetail), nameof(HasDetailId), nameof(Page), nameof(CustomerPageLabel), nameof(NeedsCatalogConfirmation), nameof(UnknownOutcome), nameof(NewLabel), nameof(QuantityValid), nameof(Subtotal), nameof(FormError), nameof(CanSubmit) }) OnPropertyChanged(name);
        foreach (var command in new[] { ReloadCommand, PreviousCommand, NextCommand, NewCommand, ListCommand, ResumeDraftCommand, SearchCustomersCommand, PreviousCustomersCommand, NextCustomersCommand, LoadRecipientsCommand, ReloadCatalogCommand, AddLineCommand, RemoveLineCommand, SaveCommand, OpenDetailCommand, RetryDetailCommand }) command?.NotifyCanExecuteChanged();
    }

    private void ResetDraft()
    {
        Customer = null; Recipient = null; Customers.Clear(); Recipients.Clear(); CatalogItem = null;
        CustomerSearch = ""; _appliedCustomerSearch = ""; CatalogSearch = ""; QuantityText = "1";
        Lines.Clear(); _detailId = null; Detail = null; _customerPage = 1; _draftSaved = false;
        _unknownOutcome = false; _reviewedList = false; _catalogReviewRequired = false; CatalogConfirmed = false; CatalogNotice = "";
        Refresh();
    }
    private void ClearSessionData()
    {
        ResetDraft(); Catalog.Clear(); Registrations.Clear(); SelectedRegistration = null;
        _catalogReady = false; IsCreating = false; Feedback = "Phiên đã thay đổi. Vui lòng mở lại màn đăng ký sau khi đăng nhập.";
        Refresh();
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (!CanInteract) return;
        _isBusy = true; Feedback = "Đang tải / xử lý…"; Refresh();
        try { await action(); }
        catch (Exception e) {
            if (IsCurrent) {
                if (e is RegistrationApiException { Code: "invalidSession" or "permissionDenied" }) { _denied = true; ClearSessionData(); }
                Feedback = e is RegistrationApiException ? e.Message : "Không kết nối hoặc đọc được dữ liệu máy chủ. Bản nháp được giữ; hãy thử lại thao tác tải.";
            }
        }
        finally { _isBusy = false; Refresh(); }
    }

    public Task LoadAsync() => LoadPageAsync(_unknownOutcome ? 1 : Page);
    public Task LoadPageAsync(int page) => RunAsync(async () => {
        var rows = await _service.GetRegistrationsAsync(page);
        if (!IsCurrent) return;
        Registrations.Clear(); foreach (var row in rows) Registrations.Add(row);
        SelectedRegistration = null; _page = page;
        if (_unknownOutcome && page == 1) _reviewedList = true;
        Feedback = rows.Length == 0 ? "Trang này chưa có đăng ký. Có thể quay lại trang trước hoặc tạo mới." : "Đã tải danh sách từ API / SQL. Chọn đăng ký rồi mở chi tiết.";
    });

    public Task NewAsync() => RunAsync(async () => {
        if (_unknownOutcome && !_reviewedList) return;
        ResetDraft(); IsCreating = true; _catalogReady = false;
        await ReloadCatalogCoreAsync();
        var customers = await _service.GetCustomersAsync("", 1);
        if (!IsCurrent) return;
        foreach (var c in customers) Customers.Add(c);
        Feedback = "Tìm khách hàng, chọn hồ sơ rồi tải người được tiêm. Giá hiển thị sẽ được API đối chiếu khi lưu.";
    });
    public Task SearchCustomersAsync()
    {
        _appliedCustomerSearch = CustomerSearch;
        return LoadCustomersAsync(1);
    }
    public Task LoadCustomersAsync(int page) => RunAsync(async () => {
        var customers = await _service.GetCustomersAsync(_appliedCustomerSearch, page);
        if (!IsCurrent) return;
        // Searching another page never silently changes the recipient attached to the draft.
        Customer = null; Customers.Clear(); foreach (var c in customers) Customers.Add(c);
        _customerPage = page;
        Feedback = customers.Length == 0 ? "Không có khách hàng ở trang này. Thử từ khóa khác hoặc quay lại." : "Chọn khách hàng và tải người được tiêm.";
    });
    public Task LoadRecipientsAsync() => RunAsync(async () => {
        if (Customer is not { } customer) return;
        Recipient = null; Recipients.Clear();
        var rows = await _service.GetRecipientsAsync(customer.Id);
        if (!IsCurrent || Customer?.Id != customer.Id) return;
        foreach (var row in rows.Where(x => x.CustomerId == customer.Id)) Recipients.Add(row);
        Feedback = rows.Length == 0 ? "Khách hàng chưa có người được tiêm. Tạo hồ sơ tại màn Hồ sơ trước." : "Chọn người được tiêm cho đăng ký này.";
    });

    private async Task ReloadCatalogCoreAsync()
    {
        _catalogReady = false;
        var rows = await _service.GetCatalogAsync();
        if (!IsCurrent) return;
        CatalogItem = null; Catalog.Clear(); foreach (var row in rows) Catalog.Add(row);
        var changes = new List<string>();
        for (var i = 0; i < Lines.Count; i++) {
            var old = Lines[i]; var current = rows.FirstOrDefault(x => x.Kind == old.Kind && x.Id == old.ServiceId);
            if (current is null) { Lines[i] = old with { Available = false }; changes.Add($"{old.Name}: không còn trong danh mục."); continue; }
            var before = string.Join(",", old.Components?.OrderBy(x => x.VaccineId).Select(x => $"{x.VaccineId}:{x.Quantity}") ?? []);
            var after = string.Join(",", current.Components?.OrderBy(x => x.VaccineId).Select(x => $"{x.VaccineId}:{x.Quantity}") ?? []);
            if (old.UnitPrice != current.Price || before != after)
                changes.Add($"{old.Name}: {old.UnitPrice:N0} → {current.Price:N0} đ. Thành phần [{old.ComponentLabel}] → [{current.ComponentLabel}].");
            Lines[i] = old with { Name = current.Name, UnitPrice = current.Price, Components = current.Components, Available = true };
        }
        if (changes.Count > 0 || _catalogReviewRequired) {
            _catalogReviewRequired = true; CatalogConfirmed = false;
            CatalogNotice = changes.Count > 0 ? string.Join("\n", changes) : "Danh mục đã được tải lại. Kiểm tra giá/thành phần từng dòng rồi xác nhận lại.";
        }
        _catalogReady = true; OnPropertyChanged(nameof(FilteredCatalog)); Refresh();
        Feedback = "Đã tải Catalog SQL. Chọn lại dịch vụ nếu cần.";
    }
    private void AddLine()
    {
        if (!CanEditDraft || !_catalogReady || CatalogItem is not { } item || !QuantityValid) return;
        var quantity = int.Parse(QuantityText, CultureInfo.InvariantCulture);
        var old = Lines.FirstOrDefault(x => x.Kind == item.Kind && x.ServiceId == item.Id);
        if (old is null && Lines.Count >= 100) { Feedback = "Tối đa 100 dòng dịch vụ."; return; }
        var line = new RegistrationLine(item.Id, item.Name, item.Kind, quantity, item.Price, item.Components);
        if (old is null) Lines.Add(line); else Lines[Lines.IndexOf(old)] = line;
        Feedback = old is null ? "Đã thêm dịch vụ vào bản nháp." : $"Đã thay số lượng dòng {item.Name} bằng {quantity}; không tạo dòng trùng.";
        Refresh();
    }
    public Task OpenDetailAsync(int id) => RunAsync(async () => {
        _detailId = id; Detail = null; IsCreating = false;
        await LoadDetailCoreAsync(id);
    });
    private async Task LoadDetailCoreAsync(int id)
    {
        var detail = await _service.GetDetailAsync(id);
        if (!IsCurrent) return;
        Detail = detail; Feedback = $"Đã đọc đăng ký #{id} từ API / SQL. Giá là giá đã chốt lúc đăng ký.";
    }
    public Task SaveAsync()
    {
        if (!CanSubmit) return Task.CompletedTask;
        var request = new RegistrationRequest(Customer!.Id, Recipient!.Id, Lines.Select(x =>
            new RegistrationRequestItem(x.Kind, x.ServiceId, x.Quantity, x.UnitPrice,
                x.Components?.Select(c => new RegistrationComponentRequest(c.VaccineId, c.Quantity)).ToArray())).ToArray());
        return RunAsync(async () => {
            int id;
            try { id = await _service.CreateAsync(request); }
            catch (RegistrationApiException e) when (e.Code == "catalogChanged") {
                if (!IsCurrent) return;
                _catalogReviewRequired = true; _catalogReady = false; CatalogConfirmed = false;
                CatalogNotice = "Danh mục đã thay đổi. Tải lại Catalog, xem thay đổi và xác nhận trước khi lưu.";
                await ReloadCatalogCoreAsync();
                Feedback = "Chưa tạo đăng ký. Danh mục đã đổi; cần xác nhận lại giá và thành phần.";
                return;
            }
            catch (RegistrationApiException e) when (e.Code == "outcomeUnknown") {
                if (!IsCurrent) return;
                _unknownOutcome = true; _reviewedList = false; Feedback = e.Message; return;
            }
            if (!IsCurrent) return;
            _draftSaved = true; _detailId = id; Detail = null; IsCreating = false; Refresh();
            // 201 is already committed. A detail failure must only retry this GET.
            try { await LoadDetailCoreAsync(id); }
            catch (RegistrationApiException e) when (e.Code is "invalidSession" or "permissionDenied") { throw; }
            catch { if (IsCurrent) Feedback = $"Đã tạo đăng ký #{id}, nhưng chưa tải được chi tiết. Dùng Tải lại chi tiết; không gửi tạo lần nữa."; }
        });
    }
}
