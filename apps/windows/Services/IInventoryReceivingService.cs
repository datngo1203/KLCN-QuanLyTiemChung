using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using TiemChung.Contracts;

namespace TiemChung.Desktop.Services;

public interface IInventoryReceivingService
{
    Task<InventoryVaccineListResponse> VaccinesAsync(string search,int page);
    Task<InventoryReceiptDto> ReceiveAsync(ReceiveInventoryRequest request);
    Task<InventoryReceiptDto> ReceiptAsync(string lot);
}
public sealed class InventoryReceivingService(IdentityApiService identity) : IInventoryReceivingService
{
    private readonly int _revision=identity.SessionRevision;
    private const string Root="api/v1/inventory/";
    private async Task<HttpResponseMessage> Send(HttpMethod method,string path,object? body=null)
    {
        var post=method==HttpMethod.Post; HttpResponseMessage response;
        try { response=await identity.SendCatalogAsync(method,Root+path,body,retryUnauthorized:!post,expectedSessionRevision:_revision); }
        catch(Exception e) when(post && e is HttpRequestException or OperationCanceledException) { throw new InventoryApiException("outcomeUnknown","Chưa xác định kết quả. Giữ nguyên yêu cầu và GET đối chiếu trước khi thử lại."); }
        if(response.IsSuccessStatusCode)return response;
        using(response) {
            var code=response.StatusCode switch {
                HttpStatusCode.Unauthorized=>"invalidSession",HttpStatusCode.Forbidden=>"permissionDenied",
                HttpStatusCode.BadRequest=>"validation",HttpStatusCode.NotFound=>"resourceNotFound",
                HttpStatusCode.Conflict=>"inventoryLotExists",_=>post?"outcomeUnknown":"requestFailed"
            };
            throw new InventoryApiException(code,code switch {
                "invalidSession"=>"Phiên hết hạn. Đăng nhập và mở lại kho.",
                "permissionDenied"=>"Cần Thủ kho Active có cả XemKho và NhapKho. Đăng nhập lại sau khi được cấp quyền.",
                "validation"=>"Dữ liệu không hợp lệ hoặc vắc xin không còn Active. Kiểm tra ngày, số lượng và thông tin tiếp nhận.",
                "resourceNotFound"=>"Chưa tìm thấy bản ghi tiếp nhận cho số lô này.",
                "inventoryLotExists"=>"Số lô đã tồn tại. GET đối chiếu; không nhập bổ sung hoặc ghi đè lô.",
                "outcomeUnknown"=>"Chưa xác định kết quả. GET đối chiếu trước khi thử lại.",
                _=>"Không đọc được API. Hãy thử lại."
            });
        }
    }
    public async Task<InventoryVaccineListResponse> VaccinesAsync(string search,int page)
    {
        using var response=await Send(HttpMethod.Get,$"receiving-vaccines?search={Uri.EscapeDataString(search.Trim())}&page={page}");
        var result=await response.Content.ReadFromJsonAsync<InventoryVaccineListResponse>()??throw new JsonException();
        if(result.Items is null || result.Items.Any(v=>v is null || v.Id<=0 || string.IsNullOrWhiteSpace(v.Name)))throw new JsonException();return result;
    }
    private static async Task<InventoryReceiptDto> Read(HttpResponseMessage response)
    {
        var result=await response.Content.ReadFromJsonAsync<InventoryReceiptDto>()??throw new JsonException();
        if(result.Received is null || result.MovementId<=0 || result.EmployeeId<=0 || result.RecordedAt==default || result.RecordedAt.Offset!=TimeSpan.Zero)throw new JsonException();return result;
    }
    public async Task<InventoryReceiptDto> ReceiptAsync(string lot)
    {
        using var response=await Send(HttpMethod.Get,"receipts?lotNumber="+Uri.EscapeDataString(lot));return await Read(response);
    }
    public async Task<InventoryReceiptDto> ReceiveAsync(ReceiveInventoryRequest request)
    {
        using var response=await Send(HttpMethod.Post,"receipts",request);
        if(response.StatusCode!=HttpStatusCode.Created)throw new InventoryApiException("outcomeUnknown","Cần GET đối chiếu kết quả nhập lô.");
        // Even a readable 201 is reconciled by GET; a broken 201 must never permit another POST.
        try { return await Read(response); }
        catch(Exception e) when(e is JsonException or HttpRequestException or OperationCanceledException) {
            throw new InventoryApiException("recordedUnreadable","API đã trả 201. Chỉ GET đối chiếu, không gửi nhập lại.");
        }
    }
}
