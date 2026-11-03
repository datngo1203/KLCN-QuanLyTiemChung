using System.Collections.ObjectModel;
using TiemChung.Contracts;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed record InventoryBatchRow(InventoryBatchDto Data, DateOnly FacilityDate)
{
    public string Availability => Data.ExpiresOn < FacilityDate ? "Hết hạn" : Data.Stock == 0 ? "Hết tồn" : "Còn tồn";
}
public sealed record InventoryMovementRow(InventoryMovementDto Data)
{
    public string TimeLabel => Data.RecordedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
}

public sealed class InventoryViewModel : ViewModelBase
{
    private readonly IInventoryService _service;
    private readonly IdentityApiService _identity;
    private readonly int _revision;
    private bool _busy, _denied, _loaded, _movementsLoaded, _more, _moreMovements;
    private int _page = 1, _movementPage = 1;
    private string _search = "", _feedback = "", _movementFeedback = "Chọn lô rồi bấm Xem biến động.";
    private InventoryBatchRow? _selected;

    public InventoryViewModel(IInventoryService? service = null, IdentityApiService? identity = null)
    {
        _identity = identity ?? IdentityApiService.Current;
        _revision = _identity.SessionRevision;
        _service = service ?? new InventoryApiService(_identity);
        _identity.SessionCleared += (_, _) => ClearSession();
        ReloadCommand = new(async _ => await LoadAsync(), _ => CanBrowse);
        PreviousCommand = new(async _ => await LoadAsync(Page - 1), _ => CanBrowse && Page > 1);
        NextCommand = new(async _ => await LoadAsync(Page + 1), _ => CanBrowse && _more);
        MovementsCommand = new(async _ => await LoadMovementsAsync(), _ => CanBrowse && Selected is not null);
        PreviousMovementsCommand = new(async _ => await LoadMovementsAsync(MovementPage - 1), _ => CanBrowse && Selected is not null && MovementPage > 1);
        NextMovementsCommand = new(async _ => await LoadMovementsAsync(MovementPage + 1), _ => CanBrowse && Selected is not null && _moreMovements);
    }
    public ObservableCollection<InventoryBatchRow> Batches { get; } = [];
    public ObservableCollection<InventoryMovementRow> Movements { get; } = [];
    public RelayCommand ReloadCommand { get; }
    public RelayCommand PreviousCommand { get; }
    public RelayCommand NextCommand { get; }
    public RelayCommand MovementsCommand { get; }
    public RelayCommand PreviousMovementsCommand { get; }
    public RelayCommand NextMovementsCommand { get; }
    private bool IsCurrent => _identity.IsSignedIn && _identity.SessionRevision == _revision;
    public bool HasAccess => IsCurrent && _identity.CanReadInventory && !_denied;
    public bool NoAccess => !HasAccess;
    public bool IsBusy => _busy;
    public bool CanBrowse => HasAccess && !_busy;
    public bool NoBatches => HasAccess && !_busy && _loaded && Batches.Count == 0;
    public bool NoMovements => HasAccess && !_busy && _movementsLoaded && Movements.Count == 0;
    public int Page => _page;
    public int MovementPage => _movementPage;
    public string Feedback { get => _feedback; private set => Set(ref _feedback, value); }
    public string MovementFeedback { get => _movementFeedback; private set => Set(ref _movementFeedback, value); }
    public string SearchText { get => _search; set { if (CanBrowse) Set(ref _search, value); } }
    public InventoryBatchRow? Selected {
        get => _selected;
        set { if (CanBrowse && Set(ref _selected, value)) { ClearMovements(); Refresh(); } }
    }
    private void ClearMovements()
    {
        Movements.Clear(); _movementsLoaded = false; _moreMovements = false; _movementPage = 1;
        MovementFeedback = "Chọn lô rồi bấm Xem biến động.";
    }
    private void ClearSession()
    {
        Batches.Clear(); _selected = null; _search = ""; _loaded = false; _more = false; _page = 1;
        ClearMovements(); OnPropertyChanged(nameof(Selected)); OnPropertyChanged(nameof(SearchText));
        Feedback = "Phiên đã thay đổi. Đăng nhập và mở lại Kho vắc xin."; Refresh();
    }
    private string Error(Exception error)
    {
        if (error is InventoryApiException { Code: "invalidSession" or "permissionDenied" }) { _denied = true; ClearSession(); }
        return error is InventoryApiException ? error.Message : "Không kết nối hoặc đọc được API. Hãy thử lại.";
    }
    private void Refresh()
    {
        foreach (var name in new[] { nameof(HasAccess), nameof(NoAccess), nameof(IsBusy), nameof(CanBrowse), nameof(NoBatches), nameof(NoMovements), nameof(Page), nameof(MovementPage) }) OnPropertyChanged(name);
        foreach (var cmd in new[] { ReloadCommand, PreviousCommand, NextCommand, MovementsCommand, PreviousMovementsCommand, NextMovementsCommand }) cmd?.NotifyCanExecuteChanged();
    }
    public async Task LoadAsync(int page = 1)
    {
        if (!CanBrowse || page < 1) return;
        if (SearchText.Trim().Length > 100) { Feedback = "Tìm kiếm tối đa 100 ký tự."; return; }
        _busy = true; _loaded = false; _more = false; _selected = null;
        OnPropertyChanged(nameof(Selected)); Batches.Clear(); ClearMovements();
        Feedback = "Đang tải lô từ API / SQL…"; Refresh();
        try {
            var result = await _service.ListAsync(page, SearchText);
            if (!IsCurrent || _denied) return;
            foreach (var row in result.Items) Batches.Add(new(row, result.FacilityDate));
            _page = page; _more = result.HasMore; _loaded = true;
            Feedback = "Đã đọc API / SQL. Chọn lô để xem biến động. Tồn là số liệu tại lúc tải; không xác nhận lô được phép tiêm.";
        } catch (Exception e) { if (IsCurrent) Feedback = Error(e); }
        finally { _busy = false; Refresh(); }
    }
    public async Task LoadMovementsAsync(int page = 1)
    {
        if (!CanBrowse || Selected is not { } lot || page < 1) return;
        _busy = true; _movementsLoaded = false; _moreMovements = false; Movements.Clear();
        MovementFeedback = $"Đang tải biến động lô {lot.Data.LotNumber}…"; Refresh();
        try {
            var result = await _service.MovementsAsync(lot.Data.LotNumber, page);
            if (!IsCurrent || _denied || Selected != lot) return;
            foreach (var row in result.Items) Movements.Add(new(row));
            _movementPage = page; _moreMovements = result.HasMore; _movementsLoaded = true;
            MovementFeedback = $"Biến động lô {lot.Data.LotNumber} · số lượng và loại theo SQL. Giờ hiển thị theo thiết bị.";
        } catch (Exception e) { if (IsCurrent) MovementFeedback = Error(e); }
        finally { _busy = false; Refresh(); }
    }
}
