using System.Collections.ObjectModel;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class ReportViewModel : ViewModelBase
{
    private readonly IReportService _service;
    private string _selectedTab = "Revenue";
    private DateTime _fromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5);
    private DateTime _toDate = DateTime.Today;

    // S7.1 Revenue state
    private decimal _totalRevenue;
    private int _totalTransactions;
    private bool _isSimulated = true;

    // S7.2 Vaccine Doses state
    private int _totalDoses;
    private int _vaccineCount;
    private string _topVaccineName = "—";

    // S7.3 Registration Status state
    private int _totalRegistrations;
    private int _statusTypeCount;
    private string _topStatusName = "—";

    // Common state
    private bool _hasData;
    private bool _isLoading;
    private string _validation = "";
    private string _feedback = "";

    public ReportViewModel(IReportService? service = null)
    {
        _service = service ?? new ReportApiService();
        GenerateReportCommand = new(_ => _ = GenerateAsync(), _ => CanGenerate);
        SelectRevenueTabCommand = new(_ => { SelectedTab = "Revenue"; });
        SelectVaccineDosesTabCommand = new(_ => { SelectedTab = "VaccineDoses"; });
        SelectRegistrationStatusTabCommand = new(_ => { SelectedTab = "RegistrationStatus"; });
        Refresh();
        _ = GenerateAsync();
    }

    public ObservableCollection<MonthlyRevenueItem> MonthlyItems { get; } = [];
    public ObservableCollection<VaccineDoseItem> VaccineItems { get; } = [];
    public ObservableCollection<RegistrationStatusItem> RegistrationStatusItems { get; } = [];

    public RelayCommand GenerateReportCommand { get; }
    public RelayCommand SelectRevenueTabCommand { get; }
    public RelayCommand SelectVaccineDosesTabCommand { get; }
    public RelayCommand SelectRegistrationStatusTabCommand { get; }

    public string SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (Set(ref _selectedTab, value))
            {
                OnPropertyChanged(nameof(IsRevenueTab));
                OnPropertyChanged(nameof(IsVaccineDosesTab));
                OnPropertyChanged(nameof(IsRegistrationStatusTab));
                OnPropertyChanged(nameof(ReportSubtitle));
                OnPropertyChanged(nameof(DataSourceLabel));
                ClearSnapshot();
                _ = GenerateAsync();
            }
        }
    }

    public bool IsRevenueTab => SelectedTab == "Revenue";
    public bool IsVaccineDosesTab => SelectedTab == "VaccineDoses";
    public bool IsRegistrationStatusTab => SelectedTab == "RegistrationStatus";

    public string ReportSubtitle => SelectedTab switch
    {
        "Revenue" => "Thống kê doanh thu theo tháng",
        "VaccineDoses" => "Thống kê số mũi tiêm theo vắc xin",
        "RegistrationStatus" => "Thống kê số lượt đăng ký theo trạng thái",
        _ => "Báo cáo thống kê"
    };

    public DateTime FromDate
    {
        get => _fromDate;
        set
        {
            if (Set(ref _fromDate, value))
            {
                Refresh();
            }
        }
    }

    public DateTime ToDate
    {
        get => _toDate;
        set
        {
            if (Set(ref _toDate, value))
            {
                Refresh();
            }
        }
    }

    // Revenue properties
    public decimal TotalRevenue
    {
        get => _totalRevenue;
        private set => Set(ref _totalRevenue, value);
    }

    public int TotalTransactions
    {
        get => _totalTransactions;
        private set => Set(ref _totalTransactions, value);
    }

    public bool IsSimulated
    {
        get => _isSimulated;
        private set => Set(ref _isSimulated, value);
    }

    // Vaccine Doses properties
    public int TotalDoses
    {
        get => _totalDoses;
        private set => Set(ref _totalDoses, value);
    }

    public int VaccineCount
    {
        get => _vaccineCount;
        private set => Set(ref _vaccineCount, value);
    }

    public string TopVaccineName
    {
        get => _topVaccineName;
        private set => Set(ref _topVaccineName, value);
    }

    // Registration Status properties
    public int TotalRegistrations
    {
        get => _totalRegistrations;
        private set => Set(ref _totalRegistrations, value);
    }

    public int StatusTypeCount
    {
        get => _statusTypeCount;
        private set => Set(ref _statusTypeCount, value);
    }

    public string TopStatusName
    {
        get => _topStatusName;
        private set => Set(ref _topStatusName, value);
    }

    public bool HasData
    {
        get => _hasData && !HasInvalidRange;
        private set
        {
            if (Set(ref _hasData, value))
            {
                OnPropertyChanged(nameof(HasNoData));
            }
        }
    }

    public bool HasNoData => !HasData && !HasInvalidRange && !IsLoading && string.IsNullOrEmpty(Validation);
    public bool HasInvalidRange => FromDate.Date > ToDate.Date || ToDate.Date > DateTime.Today;

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (Set(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(CanGenerate));
                OnPropertyChanged(nameof(HasNoData));
                GenerateReportCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string Validation
    {
        get => _validation;
        private set
        {
            if (Set(ref _validation, value))
            {
                OnPropertyChanged(nameof(HasValidation));
            }
        }
    }

    public bool HasValidation => !string.IsNullOrEmpty(Validation);

    public string Feedback
    {
        get => _feedback;
        private set => Set(ref _feedback, value);
    }

    public bool CanViewReports => IdentityApiService.Current.CanViewReports || _service.UseSampleData;
    public bool CanGenerate => !IsLoading && !HasInvalidRange && CanViewReports;

    public string DataSourceLabel => _service.UseSampleData
        ? "Dữ liệu mẫu"
        : (IsRevenueTab ? "Doanh thu mô phỏng (G3)" : (IsVaccineDosesTab ? "Dữ liệu tiêm thực tế" : "Dữ liệu đăng ký thực tế"));

    public bool UseSampleData
    {
        get => _service.UseSampleData;
        set
        {
            if (_service.UseSampleData == value) return;
            _service.UseSampleData = value;
            OnPropertyChanged(nameof(DataSourceLabel));
            OnPropertyChanged(nameof(CanViewReports));
            OnPropertyChanged(nameof(CanGenerate));
            _ = GenerateAsync();
        }
    }

    public async Task GenerateAsync()
    {
        if (HasInvalidRange)
        {
            Validation = ToDate.Date > DateTime.Today
                ? "Đến ngày không được sau hôm nay."
                : "Từ ngày phải bằng hoặc trước Đến ngày.";
            RefreshCommands();
            return;
        }

        if (!CanViewReports)
        {
            Validation = "Bạn không có quyền XemBaoCao để xem báo cáo thống kê.";
            ClearSnapshot();
            RefreshCommands();
            return;
        }

        Validation = "";
        Feedback = "";
        IsLoading = true;
        try
        {
            var from = DateOnly.FromDateTime(FromDate);
            var to = DateOnly.FromDateTime(ToDate);

            if (IsRevenueTab)
            {
                var report = await _service.GetMonthlyRevenueAsync(from, to);

                TotalRevenue = report.TotalRevenue;
                TotalTransactions = report.TotalTransactions;
                IsSimulated = report.IsSimulated;
                HasData = report.HasData;

                MonthlyItems.Clear();
                var max = report.Items.Count == 0 ? 0 : report.Items.Max(x => x.Revenue);
                foreach (var item in report.Items)
                {
                    var barHeight = max == 0 ? 0 : Math.Max(16, (double)(item.Revenue * 80m / max));
                    MonthlyItems.Add(item with { BarHeight = barHeight });
                }
            }
            else if (IsVaccineDosesTab)
            {
                var report = await _service.GetVaccineDosesAsync(from, to);

                TotalDoses = report.TotalDoses;
                VaccineCount = report.Items.Count;
                TopVaccineName = report.Items.Count > 0 ? report.Items[0].VaccineName : "—";
                HasData = report.HasData;

                VaccineItems.Clear();
                var maxDoses = report.Items.Count == 0 ? 0 : report.Items.Max(x => x.DoseCount);
                foreach (var item in report.Items)
                {
                    var barHeight = maxDoses == 0 ? 0 : Math.Max(16, (double)(item.DoseCount * 80.0 / maxDoses));
                    VaccineItems.Add(item with { BarWidth = barHeight });
                }
            }
            else
            {
                var report = await _service.GetRegistrationStatusesAsync(from, to);

                TotalRegistrations = report.TotalRegistrations;
                StatusTypeCount = report.Items.Count;
                TopStatusName = report.Items.Count > 0 ? report.Items[0].StatusDisplayName : "—";
                HasData = report.HasData;

                RegistrationStatusItems.Clear();
                var maxCount = report.Items.Count == 0 ? 0 : report.Items.Max(x => x.RegistrationCount);
                foreach (var item in report.Items)
                {
                    var barHeight = maxCount == 0 ? 0 : Math.Max(16, (double)(item.RegistrationCount * 80.0 / maxCount));
                    RegistrationStatusItems.Add(item with { BarHeight = barHeight });
                }
            }
        }
        catch (ReportApiException ex)
        {
            Validation = ex.Message;
            ClearSnapshot();
        }
        catch (Exception)
        {
            Validation = "Không thể tải báo cáo từ máy chủ.";
            ClearSnapshot();
        }
        finally
        {
            IsLoading = false;
            RefreshCommands();
        }
    }

    private void ClearSnapshot()
    {
        HasData = false;
        TotalRevenue = 0;
        TotalTransactions = 0;
        TotalDoses = 0;
        VaccineCount = 0;
        TopVaccineName = "—";
        TotalRegistrations = 0;
        StatusTypeCount = 0;
        TopStatusName = "—";
        MonthlyItems.Clear();
        VaccineItems.Clear();
        RegistrationStatusItems.Clear();
    }

    public void Refresh()
    {
        Validation = ToDate.Date > DateTime.Today
            ? "Đến ngày không được sau hôm nay."
            : FromDate.Date > ToDate.Date
                ? "Từ ngày phải bằng hoặc trước Đến ngày."
                : !CanViewReports
                    ? "Bạn không có quyền XemBaoCao để xem báo cáo thống kê."
                    : "";
        OnPropertyChanged(nameof(CanViewReports));
        OnPropertyChanged(nameof(CanGenerate));
        OnPropertyChanged(nameof(HasData));
        OnPropertyChanged(nameof(HasNoData));
        OnPropertyChanged(nameof(HasInvalidRange));
        GenerateReportCommand?.NotifyCanExecuteChanged();
    }

    private void RefreshCommands()
    {
        OnPropertyChanged(nameof(CanGenerate));
        GenerateReportCommand?.NotifyCanExecuteChanged();
    }
}
