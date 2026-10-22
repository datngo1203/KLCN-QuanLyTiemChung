using System.Collections.ObjectModel;
using System.Globalization;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class RegistrationViewModel : ViewModelBase
{
    private readonly IRegistrationService _service;
    private CustomerRecord? _customer;
    private RecipientRecord? _recipient;
    private RegistrationCatalogItem? _catalogItem;
    private string _injectionType = "Tại trung tâm", _timeText = "", _quantityText = "1", _promotion = "Không áp dụng", _feedback = "";
    private DateTime? _plannedDate = DateTime.Today.AddDays(1);
    private bool _isSaving, _showErrors;
    public RegistrationViewModel(IRegistrationService? service = null)
    {
        _service = service ?? new SampleRegistrationService();
        Customers = _service.GetCustomers(); Recipients = _service.GetRecipients(); Catalog = _service.GetCatalog();
        Registrations = new(_service.GetRegistrations()); Lines = [];
        Lines.CollectionChanged += (_, _) => Refresh();
        AddLineCommand = new RelayCommand(_ => AddLine(), _ => CatalogItem is { Stock: > 0 } && QuantityValid);
        RemoveLineCommand = new RelayCommand(x => { if (x is RegistrationLine line) Lines.Remove(line); });
        ThemVaoHangDoiCommand = new RelayCommand(_ => AddToWaitlist(), _ => Customer is not null && Recipient is not null && CatalogItem is { IsPackage: false, Stock: 0 });
        TaoDangKyCommand = new RelayCommand(_ => Save(), _ => CanSubmit);
    }
    public IReadOnlyList<CustomerRecord> Customers { get; }
    public IReadOnlyList<RecipientRecord> Recipients { get; }
    public IReadOnlyList<RegistrationCatalogItem> Catalog { get; }
    public ObservableCollection<RegistrationRecord> Registrations { get; }
    public ObservableCollection<RegistrationLine> Lines { get; }
    public IReadOnlyList<string> InjectionTypes { get; } = ["Tại trung tâm", "Tại nhà"];
    public IReadOnlyList<string> Promotions { get; } = ["Không áp dụng", "Mã ưu đãi mẫu — chờ tính từ API"];
    public RelayCommand AddLineCommand { get; }
    public RelayCommand RemoveLineCommand { get; }
    public RelayCommand ThemVaoHangDoiCommand { get; }
    public RelayCommand TaoDangKyCommand { get; }
    // TODO: Bind permission from authenticated session.
    public bool CanCreateRegistration => true;
    public string DataSourceLabel => _service.UseSampleData ? "Dữ liệu mẫu" : "Chưa có nguồn dữ liệu";
    public CustomerRecord? Customer { get => _customer; set { if (Set(ref _customer, value)) { Recipient = null; Refresh(); } } }
    public IEnumerable<RecipientRecord> AvailableRecipients => Recipients.Where(x => x.CustomerId == Customer?.Id);
    public RecipientRecord? Recipient { get => _recipient; set { if (Set(ref _recipient, value)) Refresh(); } }
    public RegistrationCatalogItem? CatalogItem { get => _catalogItem; set { if (Set(ref _catalogItem, value)) Refresh(); } }
    public string InjectionType { get => _injectionType; set { if (Set(ref _injectionType, value)) Refresh(); } }
    public DateTime? PlannedDate { get => _plannedDate; set { if (Set(ref _plannedDate, value)) Refresh(); } }
    public string TimeText { get => _timeText; set { if (Set(ref _timeText, value)) Refresh(); } }
    public string QuantityText { get => _quantityText; set { if (Set(ref _quantityText, value)) Refresh(); } }
    public string Promotion { get => _promotion; set { if (Set(ref _promotion, value)) Refresh(); } }
    public string Feedback { get => _feedback; private set => Set(ref _feedback, value); }
    public bool ShowErrors { get => _showErrors; set { if (Set(ref _showErrors, value)) Refresh(); } }
    public bool IsOutOfStock => CatalogItem is { IsPackage: false, Stock: 0 };
    public bool QuantityValid => int.TryParse(QuantityText, out var q) && q > 0;
    public bool TimeValid => PlannedDate is DateTime date && TimeSpan.TryParseExact(TimeText, @"hh\:mm", CultureInfo.InvariantCulture, out var time) && date.Date.Add(time) >= DateTime.Now;
    public decimal Subtotal => Lines.Sum(x => x.Total);
    public string SubtotalLabel => Subtotal.ToString("N0") + " đ";
    public string PriceNote => Promotion == "Không áp dụng" ? "Đơn giá được chốt khi tạo đăng ký mẫu." : "Ưu đãi chưa có công thức/contract; tổng tạm tính chưa trừ khuyến mãi.";
    public string FormError => !ShowErrors ? "" : Customer is null ? "Chọn khách hàng." : Recipient is null ? "Chọn người được tiêm thuộc khách hàng." : !InjectionTypes.Contains(InjectionType) ? "Chọn hình thức tiêm." : !TimeValid ? "Thời gian dự kiến phải từ hiện tại trở đi (HH:mm)." : Lines.Count == 0 ? "Thêm ít nhất một vắc xin lẻ hoặc gói." : Lines.Any(x => x.Quantity <= 0 || x.UnitPrice < 0) ? "Số lượng phải > 0 và đơn giá không âm." : "";
    public bool CanSubmit => CanCreateRegistration && !_isSaving && Customer is not null && Recipient is not null && InjectionTypes.Contains(InjectionType) && TimeValid && Lines.Count > 0 && Lines.All(x => x.Quantity > 0 && x.UnitPrice >= 0);
    private void Refresh()
    {
        _showErrors = true;
        OnPropertyChanged(nameof(AvailableRecipients)); OnPropertyChanged(nameof(IsOutOfStock)); OnPropertyChanged(nameof(QuantityValid)); OnPropertyChanged(nameof(TimeValid));
        OnPropertyChanged(nameof(Subtotal)); OnPropertyChanged(nameof(SubtotalLabel)); OnPropertyChanged(nameof(PriceNote)); OnPropertyChanged(nameof(FormError));
        AddLineCommand.NotifyCanExecuteChanged(); ThemVaoHangDoiCommand.NotifyCanExecuteChanged(); TaoDangKyCommand.NotifyCanExecuteChanged();
    }
    private void AddLine()
    {
        if (CatalogItem is null || !QuantityValid || CatalogItem.Stock == 0) return;
        Lines.Add(new(CatalogItem.Id, CatalogItem.Name, CatalogItem.IsPackage, int.Parse(QuantityText, CultureInfo.InvariantCulture), CatalogItem.Price));
        Feedback = "Đã thêm dịch vụ; giá mẫu đã chốt trên dòng.";
    }
    private void AddToWaitlist()
    {
        if (Customer is null || Recipient is null || CatalogItem is null || CatalogItem.Stock != 0) return;
        _service.AddToWaitlist(Customer.Id, Recipient.Id, CatalogItem.Id);
        Feedback = "Đã thêm vào hàng đợi mẫu; chưa giữ hàng tại kho thật.";
    }
    private void Save()
    {
        ShowErrors = true; if (!CanSubmit) return; _isSaving = true; Refresh();
        try
        {
            TimeSpan.TryParseExact(TimeText, @"hh\:mm", CultureInfo.InvariantCulture, out var time);
            var record = _service.SaveRegistration(new(0, Customer!.Id, Recipient!.Id, Customer.Name, Recipient.Name, InjectionType, PlannedDate!.Value.Date.Add(time), Lines.ToArray(), Promotion == "Không áp dụng" ? null : Promotion, Subtotal, "Chờ khám"));
            Registrations.Add(record); Lines.Clear(); Feedback = $"Đã tạo {record.Code} trên dữ liệu mẫu; chưa ghi API.";
        }
        catch (Exception e) { Feedback = e.Message; }
        finally { _isSaving = false; Refresh(); }
    }
}
