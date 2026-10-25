using System.Collections.ObjectModel;
using System.Globalization;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class PaymentViewModel : ViewModelBase
{
    private readonly IPaymentService _service;
    private string _search = "", _amountText = "", _method = "Tiền mặt", _type = "Toàn bộ", _status = "Thành công", _feedback = "";
    private PaymentInvoice? _selectedInvoice;
    private PaymentRecord? _selectedPayment;
    private bool _isSaving;
    public PaymentViewModel(IPaymentService? service = null)
    {
        _service = service ?? new SamplePaymentService();
        Invoices = new(_service.GetInvoices()); History = [];
        TimPhieuCommand = new RelayCommand(_ => RefreshSearch());
        XacNhanThanhToanCommand = new RelayCommand(_ => AddPayment(), _ => CanCollect);
        InBienLaiCommand = new RelayCommand(_ => PrintReceipt(), _ => CanPrint);
    }
    public ObservableCollection<PaymentInvoice> Invoices { get; }
    public ObservableCollection<PaymentRecord> History { get; }
    public IReadOnlyList<string> Methods { get; } = ["Tiền mặt", "Chuyển khoản", "Thẻ ngân hàng"];
    public IReadOnlyList<string> Types { get; } = ["Toàn bộ"];
    public IReadOnlyList<string> Statuses { get; } = ["Thành công", "Đang xử lý", "Thất bại"];
    public RelayCommand TimPhieuCommand { get; }
    public RelayCommand XacNhanThanhToanCommand { get; }
    public RelayCommand InBienLaiCommand { get; }
    // TODO: Bind reviewed GhiThanhToan/XemThanhToan permissions from authenticated session.
    public bool CanRecordPayment => true;
    public string DataSourceLabel => _service.UseSampleData ? "Dữ liệu mẫu" : "Chưa có nguồn dữ liệu";
    public string Search { get => _search; set { if (Set(ref _search, value)) RefreshSearch(); } }
    public IEnumerable<PaymentInvoice> FilteredInvoices => Invoices.Where(x => $"{x.Code} {x.RecipientName}".Contains(Search.Trim(), StringComparison.CurrentCultureIgnoreCase));
    public bool NoResults => !FilteredInvoices.Any();
    public PaymentInvoice? SelectedInvoice
    {
        get => _selectedInvoice;
        set
        {
            if (!Set(ref _selectedInvoice, value)) return;
            History.Clear(); if (value is not null) foreach (var row in value.History) History.Add(row);
            SelectedPayment = null; AmountText = ""; Refresh();
        }
    }
    public PaymentRecord? SelectedPayment { get => _selectedPayment; set { if (Set(ref _selectedPayment, value)) InBienLaiCommand.NotifyCanExecuteChanged(); } }
    public string AmountText { get => _amountText; set { if (Set(ref _amountText, value)) Refresh(); } }
    public string Method { get => _method; set { if (Set(ref _method, value)) Refresh(); } }
    public string Type { get => _type; set { if (Set(ref _type, value)) Refresh(); } }
    public string Status { get => _status; set { if (Set(ref _status, value)) Refresh(); } }
    public string Feedback { get => _feedback; private set => Set(ref _feedback, value); }
    public bool HasSelection => SelectedInvoice is not null;
    public bool HasNoSelection => !HasSelection;
    public bool IsPaidInFull => SelectedInvoice?.Remaining == 0;
    public bool CanEnterPayment => HasSelection && !IsPaidInFull && CanRecordPayment;
    public string AmountError => !HasSelection || IsPaidInFull || AmountText.Length == 0 ? "" : !TryAmount(out decimal amount) || amount <= 0 ? "Số tiền phải lớn hơn 0." : amount > SelectedInvoice!.Remaining ? "Số tiền thu vượt số còn lại." : "";
    public bool CanCollect => CanEnterPayment && !_isSaving && TryAmount(out decimal amount) && amount > 0 && amount <= SelectedInvoice!.Remaining && Methods.Contains(Method) && Types.Contains(Type) && Statuses.Contains(Status);
    public bool CanPrint => SelectedPayment?.Status == "Thành công";
    private bool TryAmount(out decimal amount) => decimal.TryParse(AmountText, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out amount) || decimal.TryParse(AmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    private void RefreshSearch() { OnPropertyChanged(nameof(FilteredInvoices)); OnPropertyChanged(nameof(NoResults)); }
    private void Refresh()
    {
        OnPropertyChanged(nameof(HasSelection)); OnPropertyChanged(nameof(IsPaidInFull)); OnPropertyChanged(nameof(CanEnterPayment)); OnPropertyChanged(nameof(AmountError));
        OnPropertyChanged(nameof(HasNoSelection));
        XacNhanThanhToanCommand.NotifyCanExecuteChanged(); InBienLaiCommand.NotifyCanExecuteChanged();
    }
    private void AddPayment()
    {
        if (!CanCollect) return; _isSaving = true; Refresh();
        try
        {
            TryAmount(out var amount);
            var row = _service.AddPayment(SelectedInvoice!.RegistrationId, amount, Method, Type, Status);
            var fresh = _service.GetInvoices().First(x => x.RegistrationId == SelectedInvoice.RegistrationId);
            var index = Invoices.IndexOf(SelectedInvoice); Invoices[index] = fresh; SelectedInvoice = fresh; SelectedPayment = row;
            Feedback = Status == "Thành công" ? "Đã ghi lần thu thành công trên dữ liệu mẫu." : "Đã ghi trạng thái giao dịch mẫu; công nợ không đổi.";
            RefreshSearch();
        }
        catch (Exception e) { Feedback = e.Message; }
        finally { _isSaving = false; Refresh(); }
    }
    private void PrintReceipt()
    {
        if (!CanPrint) return;
        Feedback = $"Biên lai mẫu {SelectedPayment!.Code}: {SelectedPayment.AmountLabel}, {SelectedPayment.Method}, {SelectedPayment.TimeLabel}. Chưa xuất hóa đơn thật.";
    }
}
