using TiemChung.Desktop.Services;

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

    public static IReadOnlyList<OverviewStage> CreateStages(int count = 1) =>
    [
        new("Chờ khám", "Sàng lọc & chỉ định", "A02", count),
        new("Chờ thanh toán", "Đối chiếu khoản thu", "A08", count),
        new("Chờ tiêm", "Thực hiện tiêm", "A06", count),
        new("Theo dõi sau tiêm", "Theo dõi tình trạng", "A06", count)
    ];

    public static IReadOnlyList<OverviewMetric> CreateMetrics(bool hasData = true) =>
    [
        new("Lượt đăng ký", hasData ? "6" : "0", "Đăng ký dịch vụ"),
        new("Lịch hẹn", hasData ? "4" : "0", "Ví dụ tổng hợp lịch hẹn"),
        new("Lượt tiêm", hasData ? "2" : "0", "Mũi tiêm đã ghi nhận"),
        new("Thanh toán thành công", hasData ? "1,5 triệu đ" : "0 đ", "Các khoản thu thành công")
    ];

    public static OverviewSnapshot CreateSnapshot(DateTime from, DateTime to)
    {
        var containsSampleDay = from.Date <= DateTime.Today && to.Date >= DateTime.Today;
        return containsSampleDay
            ? new(CreateWorkItems(DateTime.Today), CreateStages(), CreateMetrics())
            : CreateEmptySnapshot();
    }

    public static OverviewSnapshot CreateEmptySnapshot() => new([], CreateStages(0), CreateMetrics(false));
}

public sealed record OverviewWorkItem(string Id, string RecipientName, string Description, DateTime Time, string Status, string ScreenId)
{
    public string TimeLabel => Time.ToString("HH:mm");
}
public sealed record OverviewStage(string Label, string Description, string ScreenId, int? SampleCount);
public sealed record OverviewMetric(string Label, string Value, string Description);
