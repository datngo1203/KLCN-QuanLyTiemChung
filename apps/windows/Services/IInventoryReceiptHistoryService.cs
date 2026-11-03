using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using TiemChung.Contracts;

namespace TiemChung.Desktop.Services;

public interface IInventoryReceiptHistoryService
{
    Task<InventoryReceiptHistoryListResponse> HistoryAsync(string search, int page);
    Task<InventoryReceiptDto> DetailAsync(string lotNumber);
}

public sealed class InventoryReceiptHistoryApiService(IdentityApiService identity) : IInventoryReceiptHistoryService
{
    private readonly int _revision = identity.SessionRevision;
    private async Task<T> GetAsync<T>(string route)
    {
        using var response = await identity.SendCatalogAsync(HttpMethod.Get, "api/v1/inventory/" + route,
            expectedSessionRevision: _revision);
        if (!response.IsSuccessStatusCode)
        {
            var code = response.StatusCode switch {
                HttpStatusCode.Unauthorized => "invalidSession", HttpStatusCode.Forbidden => "permissionDenied",
                HttpStatusCode.BadRequest => "validation", HttpStatusCode.NotFound => "resourceNotFound",
                _ => "requestFailed"
            };
            throw new InventoryApiException(code, code switch {
                "invalidSession" => "Phiên hết hạn. Đăng nhập và mở lại kho.",
                "permissionDenied" => "Cần Thủ kho Active có cả quyền XemKho và NhapKho.",
                "validation" => "Bộ lọc hoặc trang không hợp lệ; tìm tối đa 100 ký tự.",
                "resourceNotFound" => "Không tìm thấy phiếu tiếp nhận. Tải lại danh sách.",
                _ => "Không đọc được lịch sử tiếp nhận từ API. Hãy thử lại."
            });
        }
        return await response.Content.ReadFromJsonAsync<T>() ?? throw new JsonException();
    }

    public async Task<InventoryReceiptHistoryListResponse> HistoryAsync(string search, int page)
    {
        if (page < 1 || search.Trim().Length > 100) throw new InventoryApiException("validation","Bộ lọc hoặc trang không hợp lệ.");
        var data = await GetAsync<InventoryReceiptHistoryListResponse>(
            "receipts/history?search=" + Uri.EscapeDataString(search.Trim()) + "&page=" + page);
        if (data.Items is null || data.Items.Any(r => r is null || string.IsNullOrWhiteSpace(r.LotNumber) ||
            string.IsNullOrWhiteSpace(r.VaccineName) || string.IsNullOrWhiteSpace(r.SupplierName) ||
            string.IsNullOrWhiteSpace(r.DocumentNumber) || string.IsNullOrWhiteSpace(r.StorageLocation) || string.IsNullOrWhiteSpace(r.EmployeeName) ||
            r.Quantity <= 0 || r.MovementId <= 0 || r.EmployeeId <= 0 ||
            r.RecordedAt == default || r.RecordedAt.Offset != TimeSpan.Zero))
            throw new JsonException("Invalid receipt history response");
        return data;
    }

    public async Task<InventoryReceiptDto> DetailAsync(string lotNumber)
    {
        if (string.IsNullOrWhiteSpace(lotNumber) || lotNumber.Trim().Length > 80)
            throw new InventoryApiException("validation","Số lô không hợp lệ.");
        var data = await GetAsync<InventoryReceiptDto>(
            "receipts?lotNumber=" + Uri.EscapeDataString(lotNumber.Trim()));
        if (data.Received is null || !string.Equals(data.Received.LotNumber,lotNumber.Trim(),StringComparison.Ordinal) ||
            data.Received.VaccineId <= 0 || data.Received.Quantity <= 0 || !data.Received.InspectionConfirmed ||
            data.Received.ManufacturedOn == default || data.Received.ExpiresOn < data.Received.ManufacturedOn ||
            string.IsNullOrWhiteSpace(data.VaccineName) || string.IsNullOrWhiteSpace(data.Received.SupplierName) ||
            string.IsNullOrWhiteSpace(data.Received.DocumentNumber) || string.IsNullOrWhiteSpace(data.Received.StorageLocation) || string.IsNullOrWhiteSpace(data.EmployeeName) ||
            data.MovementId <= 0 || data.EmployeeId <= 0 ||
            data.RecordedAt == default || data.RecordedAt.Offset != TimeSpan.Zero)
            throw new JsonException("Invalid receipt detail response");
        return data;
    }
}
