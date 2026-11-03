using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using TiemChung.Contracts;

namespace TiemChung.Desktop.Services;

public interface IInventoryService
{
    Task<InventoryBatchListResponse> ListAsync(int page, string search);
    Task<InventoryMovementListResponse> MovementsAsync(string lotNumber, int page);
}

public sealed class InventoryApiException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class InventoryApiService(IdentityApiService? identity = null) : IInventoryService
{
    public const int PageSize = 25;
    private readonly IdentityApiService _identity = identity ?? IdentityApiService.Current;
    private readonly int _revision = (identity ?? IdentityApiService.Current).SessionRevision;
    private async Task<T> Get<T>(string path)
    {
        using var response = await _identity.SendCatalogAsync(HttpMethod.Get, "api/v1/inventory/" + path,
            expectedSessionRevision: _revision);
        if (!response.IsSuccessStatusCode) {
            var code = response.StatusCode switch {
                HttpStatusCode.Unauthorized => "invalidSession", HttpStatusCode.Forbidden => "permissionDenied",
                HttpStatusCode.NotFound => "resourceNotFound", HttpStatusCode.BadRequest => "validation", _ => "requestFailed"
            };
            throw new InventoryApiException(code, code switch {
                "invalidSession" => "Phiên hết hạn. Đăng nhập và mở lại Kho vắc xin.",
                "permissionDenied" => "Cần nhân viên Thủ kho đang hoạt động có quyền XemKho. Đăng nhập và mở lại sau khi được cấp quyền.",
                "resourceNotFound" => "Lô không còn tồn tại. Tải lại danh sách lô.",
                "validation" => "Bộ lọc không hợp lệ. Tìm kiếm tối đa 100 ký tự.",
                _ => "Không đọc được API. Hãy thử lại."
            });
        }
        return await response.Content.ReadFromJsonAsync<T>() ?? throw new JsonException();
    }
    public async Task<InventoryBatchListResponse> ListAsync(int page, string search)
    {
        var result = await Get<InventoryBatchListResponse>($"lots?page={page}&pageSize={PageSize}&search={Uri.EscapeDataString(search.Trim())}");
        if (result.Items is null || result.FacilityDate == default || result.Items.Any(x => x is null || string.IsNullOrWhiteSpace(x.LotNumber))) throw new JsonException();
        return result;
    }
    public async Task<InventoryMovementListResponse> MovementsAsync(string lotNumber, int page)
    {
        var result = await Get<InventoryMovementListResponse>($"movements?lotNumber={Uri.EscapeDataString(lotNumber)}&page={page}&pageSize={PageSize}");
        if (result.Items is null || result.Items.Any(x => x is null || x.LotNumber != lotNumber || x.RecordedAt.Offset != TimeSpan.Zero)) throw new JsonException();
        return result;
    }
}
