namespace TiemChung.Desktop.ViewModels.Samples;

/// <summary>Unreviewed text hints for the A02 UI only; never make a clinical decision.</summary>
public static class DangerousAllergyKeywords
{
    // TODO: Cần bác sĩ duyệt danh sách từ khóa và cách dùng trước khi áp dụng ngoài UI mẫu.
    private static readonly string[] Terms = ["phản vệ", "khó thở", "phù mạch"];

    public static string? FindMatch(string? history) => string.IsNullOrWhiteSpace(history)
        ? null
        : Terms.FirstOrDefault(term => history.Contains(term, StringComparison.CurrentCultureIgnoreCase));
}
