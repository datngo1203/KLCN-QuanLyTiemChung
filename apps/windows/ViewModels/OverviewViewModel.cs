using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels.Samples;

namespace TiemChung.Desktop.ViewModels;

/// <summary>Presentation state for A01 only. No business writes or invented API endpoints.</summary>
public sealed class OverviewViewModel : ViewModelBase
{
    private readonly TiemChungApiClient _apiClient = new();
    private readonly IDashboardService _dashboardService;
    private readonly Action<string> _navigate;
    private DateTime? _fromDate = DateTime.Today;
    private DateTime? _toDate = DateTime.Today;
    private string _searchText = "";
    private string _activeStage = "";
    private string _workMode = "Lịch hẹn";
    private string _refreshMessage = "";
    private string _apiStatus = "Chưa kiểm tra";
    private string _apiCheckedAt = "Chưa có lần kiểm tra";
    private bool _isCheckingApi;
    private bool? _apiHealthy;
    private IReadOnlyList<OverviewStage> _stages;
    private IReadOnlyList<OverviewMetric> _metrics;
    private bool _hasPeriodData;

    public OverviewViewModel(Action<string> navigate, IDashboardService? dashboardService = null)
    {
        _navigate = navigate;
        _dashboardService = dashboardService ?? new SampleDashboardService();
        var initialSnapshot = _dashboardService.GetSnapshot(DateTime.Today, DateTime.Today);
        WorkItems = new ObservableCollection<OverviewWorkItem>(initialSnapshot.WorkItems);
        WorkItemsView = CollectionViewSource.GetDefaultView(WorkItems);
        WorkItemsView.Filter = MatchesFilters;
        _stages = initialSnapshot.Stages;
        _metrics = initialSnapshot.Metrics;
        _hasPeriodData = initialSnapshot.WorkItems.Count > 0;
        RefreshCommand = new RelayCommand(_ => Refresh(), _ => HasValidPeriod);
        ClearFiltersCommand = new RelayCommand(_ => { SearchText = ""; ActiveStage = ""; FromDate = DateTime.Today; ToDate = DateTime.Today; Refresh(); });
        SelectWorkModeCommand = new RelayCommand(p => { WorkMode = p as string ?? "Lịch hẹn"; });
        SelectStageCommand = new RelayCommand(p => { if (p is OverviewStage stage) { WorkMode = "Công việc"; ActiveStage = stage.Label; } });
        ChuyenHuongNhanhCommand = new RelayCommand(p => { if (p is OverviewStage stage) _navigate(stage.ScreenId); }, _ => CanChuyenHuongNhanh);
        OpenWorkItemCommand = new RelayCommand(p => { if (p is OverviewWorkItem item) _navigate(item.ScreenId); });
        OpenStageCommand = new RelayCommand(_ => _navigate(Stages.First(s => s.Label == ActiveStage).ScreenId), _ => Stages.Any(s => s.Label == ActiveStage));
        CheckApiCommand = new RelayCommand(async _ => await CheckApiAsync(), _ => !IsCheckingApi);
        ApplyFilters();
    }

    public ObservableCollection<OverviewWorkItem> WorkItems { get; }
    public ICollectionView WorkItemsView { get; }
    public IReadOnlyList<OverviewStage> Stages => _stages;
    public IReadOnlyList<OverviewMetric> Metrics => _metrics;
    public bool HasPeriodData => _hasPeriodData;
    public bool HasNoPeriodData => !_hasPeriodData;
    public RelayCommand RefreshCommand { get; }
    public RelayCommand ClearFiltersCommand { get; }
    public RelayCommand SelectWorkModeCommand { get; }
    public RelayCommand SelectStageCommand { get; }
    public RelayCommand ChuyenHuongNhanhCommand { get; }
    // TODO: Bind destination permissions from the authenticated shell session.
    public bool CanChuyenHuongNhanh => true;
    public RelayCommand OpenWorkItemCommand { get; }
    public RelayCommand OpenStageCommand { get; }
    public RelayCommand CheckApiCommand { get; }
    public bool IsSampleData => _dashboardService.UseSampleData;
    public string DataSourceLabel => IsSampleData ? "Dữ liệu mẫu" : "Chưa có nguồn dữ liệu";
    public DateTime? FromDate { get => _fromDate; set { if (Set(ref _fromDate, value)) PeriodChanged(); } }
    public DateTime? ToDate { get => _toDate; set { if (Set(ref _toDate, value)) PeriodChanged(); } }
    public bool HasValidPeriod => FromDate.HasValue && ToDate.HasValue && FromDate.Value.Date <= ToDate.Value.Date;
    public string PeriodError => HasValidPeriod ? "" : "Chọn đủ ngày; ngày bắt đầu không được sau ngày kết thúc.";
    public string PeriodLabel => HasValidPeriod ? $"{FromDate:dd/MM/yyyy} — {ToDate:dd/MM/yyyy}" : "Phạm vi ngày chưa hợp lệ";
    public string SearchText { get => _searchText; set { if (Set(ref _searchText, value)) ApplyFilters(); } }
    public string ActiveStage { get => _activeStage; private set { if (Set(ref _activeStage, value)) { ApplyFilters(); OpenStageCommand.NotifyCanExecuteChanged(); } } }
    public string WorkMode { get => _workMode; private set { if (Set(ref _workMode, value)) { OnPropertyChanged(nameof(IsAppointmentMode)); OnPropertyChanged(nameof(IsTaskMode)); } } }
    public bool IsAppointmentMode => WorkMode == "Lịch hẹn";
    public bool IsTaskMode => !IsAppointmentMode;
    public string ActiveFilterLabel => string.IsNullOrEmpty(ActiveStage) ? "Tất cả trạng thái" : ActiveStage;
    public string ResultLabel => $"{WorkItemsView.Cast<object>().Count()} kết quả · {ActiveFilterLabel}";
    public bool HasNoResults => WorkItemsView.IsEmpty;
    public string RefreshMessage { get => _refreshMessage; private set => Set(ref _refreshMessage, value); }
    public string ApiStatus { get => _apiStatus; private set => Set(ref _apiStatus, value); }
    public string ApiCheckedAt { get => _apiCheckedAt; private set => Set(ref _apiCheckedAt, value); }
    public bool? ApiHealthy { get => _apiHealthy; private set => Set(ref _apiHealthy, value); }
    public bool IsCheckingApi { get => _isCheckingApi; private set { if (Set(ref _isCheckingApi, value)) CheckApiCommand.NotifyCanExecuteChanged(); } }

    private void PeriodChanged()
    {
        OnPropertyChanged(nameof(HasValidPeriod)); OnPropertyChanged(nameof(PeriodError)); OnPropertyChanged(nameof(PeriodLabel));
        RefreshCommand.NotifyCanExecuteChanged();
        LoadPeriodData();
        ApplyFilters();
    }

    private void LoadPeriodData()
    {
        var snapshot = HasValidPeriod
            ? _dashboardService.GetSnapshot(FromDate!.Value.Date, ToDate!.Value.Date)
            : OverviewSampleData.CreateEmptySnapshot();
        WorkItems.Clear();
        foreach (var item in snapshot.WorkItems) WorkItems.Add(item);
        _stages = snapshot.Stages;
        _metrics = snapshot.Metrics;
        _hasPeriodData = snapshot.WorkItems.Count > 0;
        OnPropertyChanged(nameof(Stages));
        OnPropertyChanged(nameof(Metrics));
        OnPropertyChanged(nameof(HasPeriodData));
        OnPropertyChanged(nameof(HasNoPeriodData));
    }

    private bool MatchesFilters(object candidate) => candidate is OverviewWorkItem row && HasValidPeriod &&
        row.Time.Date >= FromDate!.Value.Date && row.Time.Date <= ToDate!.Value.Date &&
        (string.IsNullOrEmpty(ActiveStage) || row.Status == ActiveStage) &&
        (string.IsNullOrWhiteSpace(SearchText) || $"{row.Id} {row.RecipientName} {row.Description}".Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));

    private void ApplyFilters()
    {
        WorkItemsView.Refresh();
        OnPropertyChanged(nameof(HasNoResults)); OnPropertyChanged(nameof(ResultLabel)); OnPropertyChanged(nameof(ActiveFilterLabel));
    }

    private void Refresh()
    {
        LoadPeriodData();
        ApplyFilters();
        RefreshMessage = $"Đã áp dụng bộ lọc lúc {DateTime.Now:HH:mm:ss}.";
    }

    private async Task CheckApiAsync()
    {
        IsCheckingApi = true;
        ApiHealthy = null;
        ApiStatus = "Đang kiểm tra…";
        try
        {
            ApiHealthy = await _apiClient.IsHealthyAsync();
            ApiStatus = ApiHealthy == true ? "Dịch vụ phản hồi tốt" : "Dịch vụ chưa sẵn sàng";
        }
        catch (Exception)
        {
            ApiHealthy = false;
            ApiStatus = "Chưa kết nối được dịch vụ";
        }
        finally
        {
            ApiCheckedAt = $"Kiểm tra lúc {DateTime.Now:HH:mm:ss dd/MM/yyyy}";
            IsCheckingApi = false;
        }
    }
}
