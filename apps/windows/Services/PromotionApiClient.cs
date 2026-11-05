using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TiemChung.Contracts;

namespace TiemChung.Desktop.Services;

public sealed record PromotionRecord(
    int Id,
    string Name,
    string Description,
    DateTime Starts,
    DateTime Ends,
    decimal Discount,
    bool Assigned = false, bool Enabled = true, bool Effective = false)
{
    public string Status => !Enabled ? "Đã tắt" : Effective ? "Đang áp dụng" : "Ngoài thời gian áp dụng";
}

public static class PromotionApiClient
{
    private sealed record ProblemPayload(string? Code);

    public static async Task<(bool Ok, IReadOnlyList<PromotionRecord> Items, string Error)> LoadAsync(int sessionRevision, IdentityApiService? identity = null)
    {
        try
        {
            using var response = await (identity ?? IdentityApiService.Current).SendCatalogAsync(
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
            return (false, [], "Quá thời gian chờ. Vui lòng tải lại danh sách.");
        }
        catch (Exception error) when (error is HttpRequestException or System.Text.Json.JsonException)
        {
            return (false, [], "Không kết nối được máy chủ.");
        }
    }

    public static async Task<(bool Ok, PromotionRecord? Item, string Error)> SaveAsync(
        int? id, string name, string description, DateTime start, DateTime end, decimal discount, bool enabled, int sessionRevision, IdentityApiService? identity = null)
    {
        try
        {
            var startUtc = new DateTimeOffset(DateTime.SpecifyKind(start.Date, DateTimeKind.Unspecified), TimeSpan.FromHours(7));
            var endUtc = new DateTimeOffset(DateTime.SpecifyKind(end.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Unspecified), TimeSpan.FromHours(7));
            var req = new UpsertPromotionRequest(name.Trim(), description.Trim(), startUtc, endUtc, discount, enabled);

            var method = id is null ? HttpMethod.Post : HttpMethod.Put;
            var path = id is null ? "api/v1/promotions" : $"api/v1/promotions/{id.Value}";

            using var response = await (identity ?? IdentityApiService.Current).SendCatalogAsync(
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
            return (false, null, "Chưa xác định kết quả lưu. Vui lòng tải lại danh sách trước khi thao tác tiếp.");
        }
        catch (Exception error) when (error is HttpRequestException or System.Text.Json.JsonException)
        {
            return (false, null, "Không kết nối được máy chủ.");
        }
    }

    public static async Task<(bool Ok, string Error)> DeleteAsync(int id, int sessionRevision, IdentityApiService? identity = null)
    {
        try
        {
            using var response = await (identity ?? IdentityApiService.Current).SendCatalogAsync(
                HttpMethod.Delete, $"api/v1/promotions/{id}", expectedSessionRevision: sessionRevision);

            if (response.StatusCode == HttpStatusCode.NoContent || response.IsSuccessStatusCode)
                return (true, "");

            return (false, await ReadErrorAsync(response));
        }
        catch (OperationCanceledException)
        {
            return (false, "Chưa xác định kết quả xóa. Vui lòng tải lại danh sách.");
        }
        catch (Exception error) when (error is HttpRequestException or System.Text.Json.JsonException)
        {
            return (false, "Không kết nối được máy chủ.");
        }
    }

    private static PromotionRecord MapRecord(PromotionDto dto) => new(
        dto.Id,
        dto.Name,
        dto.Description,
        dto.StartsAt.ToOffset(TimeSpan.FromHours(7)).Date,
        dto.EndsAt.ToOffset(TimeSpan.FromHours(7)).Date,
        dto.DiscountAmount,
        dto.IsAssigned, dto.IsEnabled, dto.IsActive);

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>();
            return problem?.Code switch
            {
                "validation" => "Dữ liệu chương trình ưu đãi không hợp lệ (kiểm tra tên, nội dung tối đa 500 ký tự, thời hạn và mức giảm VND nguyên không âm).",
                "invalidSession" => "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
                "permissionDenied" => "Tài khoản không có quyền Quản lý khuyến mãi.",
                "resourceNotFound" => "Không tìm thấy chương trình ưu đãi trên hệ thống.",
                "promotionReferenced" => "Chương trình đã được gán vào đăng ký nên không thể xóa.",
                "dependencyUnavailable" => "Dịch vụ tạm thời chưa sẵn sàng.",
                _ => "Chưa thực hiện được yêu cầu."
            };
        }
        catch
        {
            return "Chưa thực hiện được yêu cầu.";
        }
    }
}