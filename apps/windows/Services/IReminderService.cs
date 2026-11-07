using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace TiemChung.Desktop.Services;

public sealed record ReminderRecord(int Id,int AppointmentId,string Channel,string Status,string? Content,DateTimeOffset RecordedAt,RegistrationPerson? RecordedBy,
    string? MaskedRecipient=null,string? Subject=null,int Attempts=0,DateTimeOffset? DeliveredAt=null,DateTimeOffset? NextAttemptAt=null,string? LastErrorCode=null,bool CanRetry=false,string Kind="Manual")
{
    public string TimeLabel => RecordedAt.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm:ss 'UTC+07'");
    public string RecorderLabel => Kind != "Manual" ? "Tự động bởi hệ thống" : RecordedBy?.FullName ?? "Chưa có liên kết người ghi (dữ liệu cũ)";
    public string KindLabel => Kind switch { "Manual"=>"Nhắc lịch thủ công", "TMinus48"=>"Nhắc lịch T−48h", "TMinus24"=>"Nhắc lịch T−24h", "DPlus3"=>"Liên hệ sắp xếp lại D+3", "DPlus7"=>"Liên hệ sắp xếp lại D+7", _=>"Loại nhắc lịch chưa hỗ trợ" };
    public string StatusLabel => Status switch { "ChoGui"=>"Đang gửi", "DaGui"=>"Đã gửi", "GuiLoi"=>"Gửi lỗi", _=>"Chưa gửi" };
    public string ErrorLabel => LastErrorCode switch {
        "deliveryUncertain"=>"Chưa xác định được kết quả gửi. Cần đối chiếu trước khi gửi lại.",
        "reminderRecipientUnavailable"=>"Chưa có email đủ điều kiện nhận nhắc.",
        "reminderSourceConflict"=>"Lịch không còn đủ điều kiện gửi nhắc.",
        "providerUnavailable" or "providerRejected"=>"Chưa gửi được email. Tải lại để kiểm tra và thử lại khi được phép.", _=>"" };
}
public sealed record ReminderDetail(AppointmentRecord Appointment,string? Preview,bool CanRecord,ReminderRecord? Reminder,
    string? MaskedRecipient=null,string? Subject=null,string? UnavailableReason=null,ReminderRecord[]? History=null);
public interface IReminderService
{
    Task<AppointmentRecord[]> ListAsync(string search, int page);
    Task<ReminderDetail> GetAsync(int id);
    Task<ReminderRecord> RecordAsync(int id);
    Task<ReminderRecord> RetryAsync(int id,int expectedAttempts,int? deliveryId=null);
}
public sealed class ReminderApiService(IdentityApiService? identity=null):IReminderService
{
    private readonly AppointmentApiService _api=new(identity);
    private static string Path(int id)=>$"appointments/{id}/email-reminder";
    private sealed record QueueResponse(AppointmentRecord[] Items);
    public async Task<AppointmentRecord[]> ListAsync(string search, int page)
    {
        using var response = await _api.SendAsync(HttpMethod.Get,
            $"reminders/email-queue?search={Uri.EscapeDataString(search.Trim())}&page={page}&pageSize={AppointmentApiService.PageSize}");
        var result = await response.Content.ReadFromJsonAsync<QueueResponse>() ?? throw new JsonException();
        if (result.Items is null || result.Items.Any(a => a is null || a.Id <= 0 || a.Recipient is null || a.Status is not ("DaXepLich" or "DaHuy")) ||
            result.Items.Select(a => a.Id).Distinct().Count() != result.Items.Length) throw new JsonException();
        return result.Items;
    }
    private static bool Masked(string? value)=>value is {Length:>5} && value.Contains("***@") && !value.Any(char.IsControl);
    private static bool Valid(ReminderRecord r,int id)=>r.Id>0 && r.AppointmentId==id && r.Channel=="Email" &&
        r.Status is "ChoGui" or "DaGui" or "GuiLoi" && r.RecordedAt!=default && r.RecordedAt.Offset==TimeSpan.Zero &&
        Masked(r.MaskedRecipient) && !string.IsNullOrWhiteSpace(r.Subject) && r.Attempts>=0 &&
        (r.Status=="DaGui")==r.DeliveredAt.HasValue && (!r.CanRetry || r.Status=="GuiLoi") &&
        r.Kind is "Manual" or "TMinus48" or "TMinus24" or "DPlus3" or "DPlus7" &&
        (r.Kind=="Manual" || r.RecordedBy==null);
    public async Task<ReminderDetail> GetAsync(int id)
    {
        using var response=await _api.SendAsync(HttpMethod.Get,Path(id));
        using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if(!json.RootElement.TryGetProperty("reminder",out _))throw new JsonException();
        var result=json.RootElement.Deserialize<ReminderDetail>(new JsonSerializerOptions(JsonSerializerDefaults.Web))??throw new JsonException();
        if(result.Appointment is null || result.Appointment.Id!=id || (result.Reminder is{} r && !Valid(r,id)) ||
            (result.History?.Any(h=>h is null || !Valid(h,id))??false) ||
            (result.CanRecord && (result.Reminder!=null || string.IsNullOrWhiteSpace(result.Preview) || !Masked(result.MaskedRecipient) || string.IsNullOrWhiteSpace(result.Subject))))throw new JsonException();
        return result;
    }
    public Task<ReminderRecord> RecordAsync(int id)=>PostAsync(id,false,0);
    public Task<ReminderRecord> RetryAsync(int id,int expectedAttempts,int? deliveryId=null)=>PostAsync(id,true,expectedAttempts,deliveryId);
    private async Task<ReminderRecord> PostAsync(int id,bool retry,int expectedAttempts,int? deliveryId=null)
    {
        using var response=await _api.SendAsync(HttpMethod.Post,Path(id)+(retry?"/retry":""),retry?(object)new{expectedAttempts,deliveryId}:new{});
        if(response.StatusCode!=HttpStatusCode.Created)throw new AppointmentApiException("outcomeUnknown","Kết quả ghi nhận chưa xác định. Hãy đối chiếu trước khi thử lại.");
        try {
            var result=await response.Content.ReadFromJsonAsync<ReminderRecord>()??throw new JsonException();
            if(!Valid(result,id)||(result.Kind=="Manual" && result.RecordedBy is not{Id:>0})||string.IsNullOrWhiteSpace(result.Content))throw new JsonException();
            return result;
        }catch(Exception e)when(e is JsonException or HttpRequestException or OperationCanceledException){
            throw new AppointmentApiException("recordedUnreadable","Đã nhận xác nhận lưu. Hãy đối chiếu kết quả, không ghi lại.");
        }
    }
}
