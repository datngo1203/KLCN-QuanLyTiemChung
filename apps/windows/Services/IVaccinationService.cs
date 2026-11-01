using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace TiemChung.Desktop.Services;

public sealed record VaccinationIndication(int Id, int AppointmentId, int RegistrationId,
    RegistrationPerson Customer, RegistrationPerson Recipient, ClinicalVaccine Vaccine,
    DateTimeOffset ScheduledAt, string Location, RegistrationPerson Doctor, DateTimeOffset AssessedAt)
{
    public string Label => $"CD-{Id:0000} · {Recipient.FullName}";
    public string SourceLabel => $"LH-{AppointmentId:0000} · DK-{RegistrationId:0000} · {Vaccine.Name}";
    public string TimeLabel => ScheduledAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
}
public sealed record VaccinationLot(string Number, DateOnly ManufacturedOn, DateOnly ExpiresOn, int Stock)
{
    public string Label => $"{Number} · HSD {ExpiresOn:dd/MM/yyyy} · Tồn {Stock}";
}
public sealed record VaccinationDetail(VaccinationIndication Indication, DateOnly FacilityDate, VaccinationLot[] Lots)
{
    public string FacilityDateLabel => FacilityDate.ToString("dd/MM/yyyy");
}
public sealed record VaccinationRequest(int IndicationId, string LotNumber, string InjectionSite);
public sealed record VaccinationRecord(int Id, int IndicationId, int AppointmentId, int RegistrationId,
    RegistrationPerson Recipient, ClinicalVaccine Vaccine, string LotNumber, string InjectionSite,
    RegistrationPerson Nurse, DateTimeOffset AdministeredAt, string Status)
{
    public string Label => $"MT-{Id:0000} · CD-{IndicationId:0000} · LH-{AppointmentId:0000}";
    public string RecordedLabel => AdministeredAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
}
public sealed class VaccinationApiException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
public interface IVaccinationService
{
    Task<VaccinationIndication[]> ListAsync(int page, string search);
    Task<VaccinationDetail> GetAsync(int id);
    Task<VaccinationRecord?> GetResultAsync(int id);
    Task<VaccinationRecord> RecordAsync(VaccinationRequest request);
}
public sealed class VaccinationApiService(IdentityApiService? identity = null) : IVaccinationService
{
    public const int PageSize=25;
    private readonly IdentityApiService _identity=identity??IdentityApiService.Current;
    private readonly int _revision=(identity??IdentityApiService.Current).SessionRevision;
    private const string Route="api/v1/vaccinations";
    private sealed record ListResponse(VaccinationIndication[] Items);
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
    private async Task<HttpResponseMessage> Send(HttpMethod method, string path, object? body=null)
    {
        var post=method==HttpMethod.Post;
        HttpResponseMessage response;
        try { response=await _identity.SendCatalogAsync(method,Route+path,body,retryUnauthorized:!post,expectedSessionRevision:_revision); }
        catch(Exception e) when(post && e is HttpRequestException or OperationCanceledException) {
            throw new VaccinationApiException("outcomeUnknown","Chưa xác định kết quả ghi mũi. Giữ yêu cầu và GET đối chiếu trước khi thử lại.");
        }
        if(response.IsSuccessStatusCode)return response;
        using(response) {
            if(post && response.StatusCode is not (HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Conflict))
                throw new VaccinationApiException("outcomeUnknown","Chưa xác định kết quả ghi mũi. Giữ yêu cầu và GET đối chiếu trước khi thử lại.");
            string? code=null;
            try { using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if(json.RootElement.TryGetProperty("code",out var value) && value.ValueKind==JsonValueKind.String)code=value.GetString();
            }catch(JsonException) { }
            code=response.StatusCode switch {
                HttpStatusCode.Unauthorized=>"invalidSession",HttpStatusCode.Forbidden=>"permissionDenied",
                HttpStatusCode.BadRequest=>"validation",_=>code??"requestFailed"
            };
            throw new VaccinationApiException(code,code switch {
                "invalidSession"=>"Phiên hết hạn. Đăng nhập lại để tiếp tục.",
                "permissionDenied"=>"Chỉ Điều dưỡng đang hoạt động được xem và ghi mũi.",
                "validation"=>"Kiểm tra mã chỉ định, số lô và vị trí tiêm (1–100 ký tự). Bản nháp được giữ lại.",
                "resourceNotFound"=>"Không tìm thấy chỉ định. Hãy kiểm tra mã hoặc tải lại.",
                "vaccinationAlreadyRecorded"=>"Chỉ định đã có mũi ghi nhận. Hãy GET đối chiếu, không ghi thêm.",
                "vaccinationSourceConflict"=>"Nguồn lịch/đăng ký không hợp lệ. Không thể ghi mũi; hãy tải lại.",
                "vaccinationNotIndicated"=>"Chưa có kết luận Đủ điều kiện và chỉ định DuocTiem hợp lệ cho buổi hẹn.",
                "vaccinationPaymentRequired"=>"Đăng ký chưa có thanh toán toàn bộ hợp lệ. Chưa thể ghi mũi.",
                "vaccinationZeroTotalNotSupported"=>"Đăng ký tổng 0 chưa được chốt chính sách ghi tiêm. Chưa thể ghi mũi.",
                "paymentDataConflict"=>"Dữ liệu thanh toán/giá đã chốt không nhất quán. Chưa thể ghi mũi.",
                "vaccinationLotUnavailable"=>"Lô không còn hợp lệ, hết hạn, hết tồn hoặc sai vắc xin. Giữ bản nháp để đối chiếu; xóa nháp và tải lại khi cần chọn lô mới.",
                _=>"Không đọc được API. Hãy thử lại; bản nháp được giữ lại."
            });
        }
    }
    public async Task<VaccinationIndication[]> ListAsync(int page,string search)
    {
        using var response=await Send(HttpMethod.Get,$"/eligible?page={page}&pageSize={PageSize}&search={Uri.EscapeDataString(search.Trim())}");
        return (await response.Content.ReadFromJsonAsync<ListResponse>())?.Items??throw new JsonException();
    }
    public async Task<VaccinationDetail> GetAsync(int id)
    {
        using var response=await Send(HttpMethod.Get,$"/indications/{id}");
        var result=await response.Content.ReadFromJsonAsync<VaccinationDetail>()??throw new JsonException();
        if(result.Indication?.Id!=id || result.Indication.Recipient is not {Id:>0} || result.Indication.Vaccine is not {Id:>0} ||
            result.Lots is null || result.Lots.Any(l=>string.IsNullOrWhiteSpace(l.Number) || l.Stock<=0 || l.ExpiresOn<result.FacilityDate || l.ManufacturedOn>l.ExpiresOn))throw new JsonException();
        return result;
    }
    private static bool Valid(VaccinationRecord result,int id)=>result.Id>0 && result.IndicationId==id && result.AppointmentId>0 && result.RegistrationId>0 &&
        result.Recipient is {Id:>0} && result.Vaccine is {Id:>0} && result.Nurse is {Id:>0} && !string.IsNullOrWhiteSpace(result.LotNumber) &&
        !string.IsNullOrWhiteSpace(result.InjectionSite) && result.AdministeredAt!=default && result.AdministeredAt.Offset==TimeSpan.Zero && result.Status=="DaTiem";
    public async Task<VaccinationRecord?> GetResultAsync(int id)
    {
        using var response=await Send(HttpMethod.Get,$"/indications/{id}/result");
        using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if(!json.RootElement.TryGetProperty("vaccination",out var value))throw new JsonException();
        if(value.ValueKind==JsonValueKind.Null)return null;
        var result=value.Deserialize<VaccinationRecord>(Json)??throw new JsonException();
        if(!Valid(result,id))throw new JsonException();
        return result;
    }
    public async Task<VaccinationRecord> RecordAsync(VaccinationRequest request)
    {
        using var response=await Send(HttpMethod.Post,"",request);
        if(response.StatusCode!=HttpStatusCode.Created)throw new VaccinationApiException("outcomeUnknown","Cần GET đối chiếu kết quả ghi mũi.");
        try {
            var result=await response.Content.ReadFromJsonAsync<VaccinationRecord>()??throw new JsonException();
            if(!Valid(result,request.IndicationId) || result.LotNumber!=request.LotNumber || result.InjectionSite!=request.InjectionSite.Trim())throw new JsonException();
            return result;
        }catch(Exception e) when(e is JsonException or HttpRequestException or OperationCanceledException) {
            throw new VaccinationApiException("recordedUnreadable","API đã trả 201. Chỉ GET đối chiếu, không POST lại.");
        }
    }
}
