using System.Collections.ObjectModel;
using System.Globalization;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed record ReportDoseRow(string Vaccine, int Doses, double BarHeight);

public sealed class ReportViewModel : ViewModelBase
{
    private readonly IReportService _service;
    private DateTime _fromDate = DateTime.Today.AddDays(-7);
    private DateTime _toDate = DateTime.Today;
    private decimal _revenue;
    private decimal _reactionRate;
    private bool _hasData;
    private string _validation = "";
    private string _feedback = "";

    public ReportViewModel(IReportService? service = null)
    {
        _service = service ?? new SampleReportService();
        GenerateReportCommand = new(_ => Generate(), _ => CanGenerate);
        ExportCommand = new(p => Export(p?.ToString() ?? ""), _ => CanExport);
        Generate();
    }

    public ObservableCollection<ReportDoseRow> Doses { get; } = [];
    public ObservableCollection<ReportExpiringLot> ExpiringLots { get; } = [];
    public RelayCommand GenerateReportCommand { get; }
    public RelayCommand ExportCommand { get; }
    public DateTime FromDate { get => _fromDate; set { if (Set(ref _fromDate, value)) { ClearSnapshot(); Refresh(); } } }
    public DateTime ToDate { get => _toDate; set { if (Set(ref _toDate, value)) { ClearSnapshot(); Refresh(); } } }
    public decimal Revenue { get => _revenue; private set => Set(ref _revenue, value); }
    public decimal ReactionRate { get => _reactionRate; private set => Set(ref _reactionRate, value); }
    public bool HasData { get => _hasData && !HasInvalidRange; private set { if (Set(ref _hasData, value)) { OnPropertyChanged(nameof(HasNoData)); OnPropertyChanged(nameof(CanExport)); } } }
    public bool HasNoData => !HasData && !HasInvalidRange;
    public bool HasInvalidRange => FromDate.Date > ToDate.Date || ToDate.Date > DateTime.Today;
    public string Validation { get => _validation; private set => Set(ref _validation, value); }
    public string Feedback { get => _feedback; private set => Set(ref _feedback, value); }
    public string DataSourceLabel => _service.UseSampleData ? "Dữ liệu mẫu" : "Chưa có nguồn dữ liệu";
    // TODO: Bind report access from the authenticated shell session when available.
    public bool CanViewReports => true;
    public bool CanGenerate => CanViewReports && !HasInvalidRange;
    public bool CanExport => CanViewReports && HasData;

    public bool UseSampleData
    {
        get => _service.UseSampleData;
        set
        {
            if (_service.UseSampleData == value) return;
            _service.UseSampleData = value;
            OnPropertyChanged(nameof(DataSourceLabel));
            Generate();
        }
    }

    private void Generate()
    {
        if (!CanGenerate)
        {
            Validation = ToDate.Date > DateTime.Today ? "Đến ngày không được sau hôm nay." : "Từ ngày phải bằng hoặc trước Đến ngày.";
            RefreshCommands();
            return;
        }
        Validation = "";
        var report = _service.GetReport(DateOnly.FromDateTime(FromDate), DateOnly.FromDateTime(ToDate));
        Revenue = report.Revenue;
        ReactionRate = report.ReactionRate;
        HasData = report.HasData;
        Doses.Clear();
        var max = report.Doses.Count == 0 ? 0 : report.Doses.Max(x => x.Doses);
        foreach (var dose in report.Doses)
            Doses.Add(new(dose.Vaccine, dose.Doses, max == 0 ? 0 : Math.Max(12, dose.Doses * 132d / max)));
        ExpiringLots.Clear();
        foreach (var lot in report.ExpiringLots) ExpiringLots.Add(lot);
        Feedback = "";
        RefreshCommands();
    }

    private void Export(string format)
    {
        if (!CanExport) return;
        Feedback = $"Xuất {format} chưa được kết nối; chưa tạo tệp.";
    }

    private void ClearSnapshot()
    {
        HasData = false;
        Revenue = 0;
        ReactionRate = 0;
        Doses.Clear();
        ExpiringLots.Clear();
        Feedback = "";
    }

    private void Refresh()
    {
        Validation = ToDate.Date > DateTime.Today ? "Đến ngày không được sau hôm nay." : FromDate.Date > ToDate.Date ? "Từ ngày phải bằng hoặc trước Đến ngày." : "";
        OnPropertyChanged(nameof(CanGenerate));
        OnPropertyChanged(nameof(HasData)); OnPropertyChanged(nameof(HasNoData)); OnPropertyChanged(nameof(HasInvalidRange)); OnPropertyChanged(nameof(CanExport));
        GenerateReportCommand?.NotifyCanExecuteChanged();
        ExportCommand?.NotifyCanExecuteChanged();
    }

    private void RefreshCommands()
    {
        OnPropertyChanged(nameof(CanGenerate)); OnPropertyChanged(nameof(CanExport));
        GenerateReportCommand?.NotifyCanExecuteChanged(); ExportCommand?.NotifyCanExecuteChanged();
    }
}
