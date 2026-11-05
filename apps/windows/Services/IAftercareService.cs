using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace TiemChung.Desktop.Services;

public sealed record FollowUpRequest(string Condition,string Severity,string Treatment);
public sealed record FollowUpRecord(int Id,int VaccinationId,string Condition,string Severity,string Treatment,RegistrationPerson? RecordedBy,DateTimeOffset RecordedAt)
{
    public string RecorderLabel=>RecordedBy?.FullName??"Chưa có liên kết người ghi (dữ liệu cũ)";
    public string TimeLabel=>RecordedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
}
public sealed record FollowUpObservation(int VaccinationId,DateTimeOffset ObservationUntil,bool CanComplete,string? BlockReason)
{
    public string StatusLabel=>BlockReason=="followUpSourceConflict"?"Thời điểm tiêm cần được kiểm tra":CanComplete?"Đã đến mốc hoàn tất ghi nhận":"Đang theo dõi sau tiêm";
    public string UntilLabel=>$"Có thể hoàn tất ghi nhận sau: {ObservationUntil.ToLocalTime():dd/MM/yyyy HH:mm:ss} (giờ máy)";
}
public sealed record FollowUpPendingItem(VaccinationRecord Vaccination,FollowUpObservation Observation);
public sealed record FollowUpDetail(VaccinationRecord Vaccination,FollowUpRecord? FollowUp,FollowUpObservation? Observation=null);
public sealed class AftercareApiException(string code,string message):Exception(message){public string Code{get;}=code;}
public interface IAftercareService
{
    Task<FollowUpPendingItem[]> ListAsync(int page,string search);
    Task<FollowUpDetail> GetAsync(int id);
    Task<FollowUpRecord> RecordAsync(int id,FollowUpRequest request);
}
public sealed class AftercareApiService(IdentityApiService? identity=null):IAftercareService
{
    public const int PageSize=25;
    private readonly IdentityApiService _identity=identity??IdentityApiService.Current;
    private readonly int _revision=(identity??IdentityApiService.Current).SessionRevision;
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
    private const string Root="api/v1/vaccinations";
    private static string Path(int id)=>$"/{id}/follow-ups";
    private async Task<HttpResponseMessage> Send(HttpMethod method,string path,object? body=null)
    {
        var post=method==HttpMethod.Post;HttpResponseMessage response;
        try{response=await _identity.SendCatalogAsync(method,Root+path,body,retryUnauthorized:!post,expectedSessionRevision:_revision);}
        catch(Exception e)when(post && e is HttpRequestException or OperationCanceledException){throw new AftercareApiException("outcomeUnknown","Chưa xác định kết quả lưu. Giữ nội dung và đọc lại để đối chiếu trước khi thử lại.");}
        if(response.IsSuccessStatusCode)return response;
        using(response){
            if(post && response.StatusCode is not(HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Conflict))
                throw new AftercareApiException("outcomeUnknown","Chưa xác định kết quả lưu. Giữ nội dung và đọc lại để đối chiếu trước khi thử lại.");
            string? code=null;
            try{using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());if(json.RootElement.TryGetProperty("code",out var value) && value.ValueKind==JsonValueKind.String)code=value.GetString();}catch(JsonException){}
            code=response.StatusCode switch{HttpStatusCode.Unauthorized=>"invalidSession",HttpStatusCode.Forbidden=>"permissionDenied",HttpStatusCode.BadRequest=>"validation",_=>code??"requestFailed"};
            throw new AftercareApiException(code,code switch{
                "invalidSession"=>"Phiên hết hạn. Đăng nhập lại để tiếp tục.",
                "permissionDenied"=>"Chỉ Điều dưỡng đang hoạt động được xem và ghi theo dõi.",
                "validation"=>"Nhập đủ tình trạng, mức độ và xử trí; giới hạn 100/100/500 ký tự. Bản nháp được giữ lại.",
                "resourceNotFound"=>"Không tìm thấy mũi. Kiểm tra mã MT hoặc tải lại.",
                "followUpSourceConflict"=>"Mũi không có nguồn ghi tiêm hợp lệ hoặc thời điểm không phù hợp. Không thể ghi theo dõi.",
                "observationPeriodNotCompleted"=>"Chưa đủ 30 phút theo dõi sau tiêm. Bản nháp được giữ lại; làm mới mốc theo dõi trước khi hoàn tất.",
                "followUpAlreadyRecorded"=>"Mũi đã có bản ghi theo dõi. Hãy đọc lại để đối chiếu, không ghi thêm.",
                _=>"Không đọc được dữ liệu. Hãy thử lại; bản nháp được giữ lại."
            });
        }
    }
    private static bool ValidShot(VaccinationRecord? shot,int id)=>shot is{Id:>0,IndicationId:>0,AppointmentId:>0,RegistrationId:>0,Status:"DaTiem"} && shot.Id==id &&
        shot.Recipient is{Id:>0} && shot.Vaccine is{Id:>0} && shot.Nurse is{Id:>0} && !string.IsNullOrWhiteSpace(shot.LotNumber) && shot.AdministeredAt!=default && shot.AdministeredAt.Offset==TimeSpan.Zero;
    private static bool Valid(FollowUpRecord record,int id)=>record.Id>0 && record.VaccinationId==id && !string.IsNullOrWhiteSpace(record.Condition) && !string.IsNullOrWhiteSpace(record.Severity) &&
        !string.IsNullOrWhiteSpace(record.Treatment) && record.RecordedAt!=default && record.RecordedAt.Offset==TimeSpan.Zero && (record.RecordedBy is null or{Id:>0});
    private static bool ValidObservation(FollowUpObservation? observation,VaccinationRecord shot)=>observation is not null &&
        observation.VaccinationId==shot.Id && observation.ObservationUntil.Offset==TimeSpan.Zero &&
        (observation.BlockReason=="followUpSourceConflict" && !observation.CanComplete ||
         observation.ObservationUntil==shot.AdministeredAt.AddMinutes(30) &&
         (observation.CanComplete?observation.BlockReason is null:observation.BlockReason=="observationPeriodNotCompleted"));
    public async Task<FollowUpPendingItem[]> ListAsync(int page,string search)
    {
        using var response=await Send(HttpMethod.Get,$"/follow-up-pending?page={page}&pageSize={PageSize}&search={Uri.EscapeDataString(search.Trim())}");
        using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items=json.RootElement.GetProperty("items").Deserialize<VaccinationRecord[]>(Json)??throw new JsonException();
        var observations=json.RootElement.GetProperty("observations").Deserialize<FollowUpObservation[]>(Json)??throw new JsonException();
        if(items.Any(x=>!ValidShot(x,x?.Id??0)) || observations.Length!=items.Length || observations.Select(x=>x.VaccinationId).Distinct().Count()!=observations.Length)throw new JsonException();
        return items.Select(shot=>{
            var observation=observations.SingleOrDefault(x=>x.VaccinationId==shot.Id);
            if(!ValidObservation(observation,shot))throw new JsonException();
            return new FollowUpPendingItem(shot,observation!);
        }).ToArray();
    }
    public async Task<FollowUpDetail> GetAsync(int id)
    {
        using var response=await Send(HttpMethod.Get,Path(id));using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if(!json.RootElement.TryGetProperty("followUp",out _))throw new JsonException();
        var detail=json.RootElement.Deserialize<FollowUpDetail>(Json)??throw new JsonException();
        if(!ValidShot(detail.Vaccination,id) || !ValidObservation(detail.Observation,detail.Vaccination) || (detail.FollowUp is{} result && (!Valid(result,id)||result.RecordedAt<detail.Vaccination.AdministeredAt)))throw new JsonException();return detail;
    }
    public async Task<FollowUpRecord> RecordAsync(int id,FollowUpRequest request)
    {
        using var response=await Send(HttpMethod.Post,Path(id),request);
        if(response.StatusCode!=HttpStatusCode.Created)throw new AftercareApiException("outcomeUnknown","Cần đọc lại để đối chiếu kết quả lưu theo dõi.");
        try{var result=await response.Content.ReadFromJsonAsync<FollowUpRecord>()??throw new JsonException();
            if(!Valid(result,id)||result.RecordedBy is null||result.Condition!=request.Condition.Trim()||result.Severity!=request.Severity.Trim()||result.Treatment!=request.Treatment.Trim())throw new JsonException();return result;
        }catch(Exception e)when(e is JsonException or HttpRequestException or OperationCanceledException){throw new AftercareApiException("recordedUnreadable","Đã nhận xác nhận lưu. Chỉ đọc lại để đối chiếu, không gửi lại.");}
    }
}
