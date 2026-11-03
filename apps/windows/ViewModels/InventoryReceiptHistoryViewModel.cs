using System.IO;
using System.Collections.ObjectModel;
using TiemChung.Contracts;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed record InventoryReceiptHistoryRow(InventoryReceiptHistoryItemDto Data)
{
    public string TimeLabel => Data.RecordedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
}

public sealed class InventoryReceiptHistoryViewModel : ViewModelBase
{
    private readonly IdentityApiService _identity;
    private readonly IInventoryReceiptHistoryService _service;
    private readonly int _revision;
    private bool _busy, _denied, _loaded, _more;
    private int _page = 1;
    private string _search = "", _loadedSearch = "", _feedback = "Tìm trong các phiếu nhập X2.1 đã lưu SQL.";
    private InventoryReceiptHistoryRow? _selected;
    private InventoryReceiptDto? _detail;

    public InventoryReceiptHistoryViewModel(IdentityApiService identity, IInventoryReceiptHistoryService? service = null)
    {
        _identity = identity;
        _revision = identity.SessionRevision;
        _service = service ?? new InventoryReceiptHistoryApiService(identity);
        _identity.SessionCleared += (_, _) => ClearOnSessionChange();
        FindCommand = new(async _ => await LoadAsync(), _ => CanBrowse);
        PreviousCommand = new(async _ => await LoadAsync(Page - 1), _ => CanPage && Page > 1);
        NextCommand = new(async _ => await LoadAsync(Page + 1), _ => CanPage && _more);
        DetailCommand = new(async _ => await ReadDetailAsync(), _ => CanBrowse && Selected is not null);
    }
    public ObservableCollection<InventoryReceiptHistoryRow> Rows { get; } = [];
    public RelayCommand FindCommand { get; }
    public RelayCommand PreviousCommand { get; }
    public RelayCommand NextCommand { get; }
    public RelayCommand DetailCommand { get; }
    private bool Current => _identity.IsSignedIn && _identity.SessionRevision == _revision;
    public bool HasAccess => Current && _identity.CanReceiveInventory && !_denied;
    public bool NoAccess => !HasAccess;
    public bool IsBusy => _busy;
    public bool CanBrowse => HasAccess && !_busy;
    private bool CanPage => CanBrowse && _search.Trim() == _loadedSearch;
    public bool NoRows => HasAccess && !_busy && _loaded && Rows.Count == 0;
    public int Page => _page;
    public string SearchText { get => _search; set { if (CanBrowse && Set(ref _search,value)) Refresh(); } }
    public string Feedback { get => _feedback; private set => Set(ref _feedback,value); }
    public InventoryReceiptHistoryRow? Selected
    {
        get => _selected;
        set {
            if (CanBrowse && Set(ref _selected,value)) {
                _detail = null; Refresh();
            }
        }
    }
    public string DetailLabel => _detail is { } x
        ? $"PHIẾU TIẾP NHẬN X2.1 (chỉ đọc)\nSố lô: {x.Received.LotNumber}  |  Vắc xin: {x.VaccineName}\n" +
          $"NSX: {x.Received.ManufacturedOn:dd/MM/yyyy}  |  HSD: {x.Received.ExpiresOn:dd/MM/yyyy}\n" +
          $"Số lượng nhập ban đầu: {x.Received.Quantity}  |  Mã biến động: {x.MovementId}\n" +
          $"Nhà cung cấp: {x.Received.SupplierName}  |  Chứng từ: {x.Received.DocumentNumber}\n" +
          $"Vị trí bảo quản lúc nhập: {x.Received.StorageLocation}\n" +
          $"Người nhập: {x.EmployeeName} (#{x.EmployeeId})  |  Thời điểm: {x.RecordedAt.ToLocalTime():dd/MM/yyyy HH:mm:ss}\n" +
          "Số lượng trên chứng từ là dữ liệu lúc nhập, KHÔNG phải tồn kho hiện tại."
        : "Chọn dòng rồi bấm Xem chi tiết SQL. Đây không phải số lượng tồn hiện tại.";
    private void Refresh()
    {
        foreach (var name in new[]{nameof(HasAccess),nameof(NoAccess),nameof(IsBusy),nameof(CanBrowse),
            nameof(NoRows),nameof(Page),nameof(Selected),nameof(DetailLabel)}) OnPropertyChanged(name);
        foreach (var command in new[]{FindCommand,PreviousCommand,NextCommand,DetailCommand})
            command.NotifyCanExecuteChanged();
    }
    private void ClearOnSessionChange()
    {
        _search = _loadedSearch = ""; _selected = null; _detail = null; _page = 1; _more = _loaded = false;
        Rows.Clear(); OnPropertyChanged(nameof(SearchText));
        Feedback = "Phiên đã đổi. Đăng nhập và mở lại lịch sử tiếp nhận."; Refresh();
    }
    private void ShowError(Exception error)
    {
        if (!Current) return;
        if (error is InventoryApiException { Code: "invalidSession" or "permissionDenied" }) {
            _denied = true; ClearOnSessionChange();
        }
        Feedback = error is InventoryApiException api ? api.Message :
            "Không đọc được API hoặc dữ liệu phản hồi. Kiểm tra kết nối rồi thử lại.";
    }
    public async Task LoadAsync(int page = 1)
    {
        if (!CanBrowse || page < 1) return;
        if (_search.Trim().Length > 100) { Feedback = "Tìm kiếm tối đa 100 ký tự."; return; }
        _busy = true; _loaded = false; _more = false; _selected = null; _detail = null;
        Rows.Clear(); Feedback = "Đang đọc lịch sử tiếp nhận từ SQL…"; Refresh();
        try {
            var response = await _service.HistoryAsync(_search,page);
            if (!Current || _denied) return;
            foreach (var item in response.Items) Rows.Add(new(item));
            _page = page; _loadedSearch = _search.Trim(); _more = response.HasMore; _loaded = true;
            Feedback = Rows.Count == 0 ? "Không có phiếu nhập X2.1 phù hợp." :
                "Đã đọc SQL. Số lượng là số thực nhận lúc nhập; xem tồn hiện tại ở danh sách lô A05.";
        } catch (Exception e) { ShowError(e); }
        finally { _busy = false; Refresh(); }
    }
    public async Task ReadDetailAsync()
    {
        if (!CanBrowse || Selected is not { } row) return;
        _busy = true; _detail = null; Feedback = "Đang đọc lại phiếu theo số lô từ SQL…"; Refresh();
        try {
            var detail = await _service.DetailAsync(row.Data.LotNumber);
            if (!Current || _denied || Selected != row) return;
            var actual = new InventoryReceiptHistoryItemDto(detail.Received.LotNumber,detail.VaccineName,
                detail.Received.Quantity,detail.Received.SupplierName,detail.Received.DocumentNumber,detail.Received.StorageLocation,
                detail.MovementId,detail.EmployeeId,detail.EmployeeName,detail.RecordedAt);
            if (actual != row.Data)
                throw new InvalidDataException("Receipt mismatch");
            _detail = detail; Feedback = "Chi tiết đã đối chiếu theo số lô từ SQL; không có thao tác chỉnh sửa.";
        } catch (Exception e) { ShowError(e); }
        finally { _busy = false; Refresh(); }
    }
}
