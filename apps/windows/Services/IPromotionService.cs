using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TiemChung.Contracts;

namespace TiemChung.Desktop.Services;

public interface IPromotionService
{
    bool UseSampleData { get; set; }
    IReadOnlyList<PromotionRecord> GetPromotions();
    bool IsAssigned(int id);
}

public sealed record PromotionRecord(
    int Id,
    string Name,
    string Description,
    DateTime Starts,
    DateTime Ends,
    decimal Discount,
    bool Assigned = false)
{
    public string Status => Starts.Date <= DateTime.Today && Ends.Date >= DateTime.Today
        ? "Đang hoạt động"
        : "Hết hiệu lực";
}

public sealed class SamplePromotionService : IPromotionService
{
    public bool UseSampleData { get; set; } = true;

    public IReadOnlyList<PromotionRecord> GetPromotions() => UseSampleData
        ? [
            new(1, "Chương trình mẫu", "Nội dung minh họa", DateTime.Today.AddDays(-3), DateTime.Today.AddDays(10), 50000m, true),
            new(2, "Ưu đãi mẫu đã kết thúc", "Nội dung minh họa", DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-2), 0m, false)
        ]
        : [];

    public bool IsAssigned(int id) => id == 1;
}

public static class PromotionApiClient
{
    private sealed record ProblemPayload(string? Code);

    public static async Task<(bool Ok, IReadOnlyList<PromotionRecord> Items, string Error)> LoadAsync(int sessionRevision)
    {
        try
        {
            using var response = await IdentityApiService.Current.SendCatalogAsync(
                HttpMethod.Get, "api/v1/promotions", expectedSessionRevision: sessionRevision);

            if (!response.IsSuccessStatusCode)
                return (false, [], await ReadErrorAsync(response));

            var payload = await response.Content.ReadFromJsonAsync<PromotionListResponse>();
            if (payload?.Items is null)
                return (false, [], "Phản hồi danh sách ưu đãi không hợp lệ.");

            var list = new List<PromotionRecord>(payload.Items.Count);
            foreach (var dto in payload.Items)
            {
                list.Add(MapRecord(dto));
            }
            return (true, list, "");
        }
        catch (OperationCanceledException)
        {
            return (false, [], "");
        }
        catch (HttpRequestException)
        {
            return (false, [], "Không kết nối được máy chủ API.");
        }
    }

    public static async Task<(bool Ok, PromotionRecord? Item, string Error)> SaveAsync(
        int? id, string name, string description, DateTime start, DateTime end, decimal discount, int sessionRevision)
    {
        try
        {
            var startUtc = new DateTimeOffset(DateTime.SpecifyKind(start.Date, DateTimeKind.Utc));
            var endUtc = new DateTimeOffset(DateTime.SpecifyKind(end.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc));
            var req = new UpsertPromotionRequest(name.Trim(), description.Trim(), startUtc, endUtc, discount);

            var method = id is null ? HttpMethod.Post : HttpMethod.Put;
            var path = id is null ? "api/v1/promotions" : $"api/v1/promotions/{id.Value}";

            using var response = await IdentityApiService.Current.SendCatalogAsync(
                method, path, req, expectedSessionRevision: sessionRevision);

            if (!response.IsSuccessStatusCode)
                return (false, null, await ReadErrorAsync(response));

            var dto = await response.Content.ReadFromJsonAsync<PromotionDto>();
            return dto is null
                ? (false, null, "Đã lưu nhưng không đọc được dữ liệu trả về. Vui lòng tải lại.")
                : (true, MapRecord(dto), "");
        }
        catch (OperationCanceledException)
        {
            return (false, null, "");
        }
        catch (HttpRequestException)
        {
            return (false, null, "Không kết nối được máy chủ API.");
        }
    }

    public static async Task<(bool Ok, string Error)> DeleteAsync(int id, int sessionRevision)
    {
        try
        {
            using var response = await IdentityApiService.Current.SendCatalogAsync(
                HttpMethod.Delete, $"api/v1/promotions/{id}", expectedSessionRevision: sessionRevision);

            if (response.StatusCode == HttpStatusCode.NoContent || response.IsSuccessStatusCode)
                return (true, "");

            return (false, await ReadErrorAsync(response));
        }
        catch (OperationCanceledException)
        {
            return (false, "");
        }
        catch (HttpRequestException)
        {
            return (false, "Không kết nối được máy chủ API.");
        }
    }

    private static PromotionRecord MapRecord(PromotionDto dto) => new(
        dto.Id,
        dto.Name,
        dto.Description,
        dto.StartsAt.UtcDateTime.Date,
        dto.EndsAt.UtcDateTime.Date,
        dto.DiscountAmount,
        dto.IsAssigned);

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>();
            return problem?.Code switch
            {
                "validation" => "Dữ liệu chương trình ưu đãi không hợp lệ (kiểm tra tên, nội dung tối đa 500 ký tự, thời hạn và mức giảm VND nguyên không âm).",
                "invalidSession" => "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
                "permissionDenied" => "Tài khoản không có quyền Quản lý khuyến mãi (QuanLyKhuyenMai).",
                "resourceNotFound" => "Không tìm thấy chương trình ưu đãi trên hệ thống.",
                "promotionReferenced" => "Chương trình đã được gán vào đăng ký nên không thể xóa.",
                "dependencyUnavailable" => "Cơ sở dữ liệu tạm thời không sẵn sàng (HTTP 503).",
                _ => $"Không thực hiện được yêu cầu (HTTP {(int)response.StatusCode})."
            };
        }
        catch
        {
            return $"Không thực hiện được yêu cầu (HTTP {(int)response.StatusCode}).";
        }
    }
}