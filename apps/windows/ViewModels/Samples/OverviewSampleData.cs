namespace TiemChung.Desktop.ViewModels.Samples;

/// <summary>UI fixtures only. Never used by a service, API, financial or clinical operation.</summary>
public static class OverviewSampleData
{
    // TODO: Replace this opt-in UI source when the overview DTO, formulas and read service are reviewed.
    // Counts/amount below are independent display examples, not definitions of report calculations.
    public static IReadOnlyList<OverviewWorkItem> CreateWorkItems(DateTime sampleDay) =>
    [
        new("LH-01", "Người được tiêm 01", "Vắc xin A", sampleDay.Date.AddHours(9), "Chờ khám", "A02"),
        new("LH-02", "Người được tiêm 02", "Gói tiêm B", sampleDay.Date.AddHours(9.5), "Chờ thanh toán", "A08"),
        new("LH-03", "Người được tiêm 03", "Vắc xin C", sampleDay.Date.AddHours(10), "Chờ tiêm", "A06"),
        new("LH-04", "Người được tiêm 04", "Vắc xin D", sampleDay.Date.AddHours(10.5), "Theo dõi sau tiêm", "A06")
    ];

    public static IReadOnlyList<OverviewStage> CreateStages() =>
    [
        new("Chờ khám", "Sàng lọc & chỉ định", "A02", 1),
        new("Chờ thanh toán", "Đối chiếu khoản thu", "A08", 1),
        new("Chờ tiêm", "Thực hiện tiêm", "A06", 1),
        new("Theo dõi sau tiêm", "Theo dõi tình trạng", "A06", 1)
    ];

    public static IReadOnlyList<OverviewMetric> CreateMetrics() =>
    [
        new("Lượt đăng ký", "6", "Đăng ký dịch vụ"),
        new("Lịch hẹn", "4", "Ví dụ tổng hợp lịch hẹn"),
        new("Lượt tiêm", "2", "Mũi tiêm đã ghi nhận"),
        new("Thanh toán thành công", "1,5 triệu đ", "Các khoản thu thành công")
    ];
}

public sealed record OverviewWorkItem(string Id, string RecipientName, string Description, DateTime Time, string Status, string ScreenId)
{
    public string TimeLabel => Time.ToString("HH:mm");
}
public sealed record OverviewStage(string Label, string Description, string ScreenId, int? SampleCount);
public sealed record OverviewMetric(string Label, string Value, string Description);
