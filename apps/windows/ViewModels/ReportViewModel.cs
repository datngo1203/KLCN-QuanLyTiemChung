using System.Collections.ObjectModel;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class ReportViewModel : ViewModelBase
{
    private readonly IReportService _service;
    private DateTime _fromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5);
    private DateTime _toDate = DateTime.Today;
    private decimal _totalRevenue;
    private int _totalTransactions;
    private bool _isSimulated = true;
    private bool _hasData;
    private bool _isLoading;
    private string _validation = "";
    private string _feedback = "";

    public ReportViewModel(IReportService? service = null)
    {
        _service = service ?? new ReportApiService();
        GenerateReportCommand = new(_ => _ = GenerateAsync(), _ => CanGenerate);
        Refresh();
        _ = GenerateAsync();
    }

    public ObservableCollection<MonthlyRevenueItem> MonthlyItems { get; } = [];
    public RelayCommand GenerateReportCommand { get; }

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

    public string DataSourceLabel => _service.UseSampleData ? "Dữ liệu mẫu" : "Doanh thu mô phỏng (G3)";

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
        MonthlyItems.Clear();
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
