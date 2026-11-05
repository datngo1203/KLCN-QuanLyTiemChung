using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace TiemChung.Desktop.Services;

public sealed record ReminderRecord(int Id,int AppointmentId,string Channel,string Status,string? Content,DateTimeOffset RecordedAt,RegistrationPerson? RecordedBy)
{
    public string TimeLabel => RecordedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
    public string RecorderLabel => RecordedBy?.FullName ?? "Chưa có liên kết người ghi (dữ liệu cũ)";
}
public sealed record ReminderDetail(AppointmentRecord Appointment,string? Preview,bool CanRecord,ReminderRecord? Reminder);
public interface IReminderService
{
    Task<AppointmentRecord[]> ListAsync(string search, int page);
    Task<ReminderDetail> GetAsync(int id);
    Task<ReminderRecord> RecordAsync(int id);
}
public sealed class ReminderApiService(IdentityApiService? identity=null):IReminderService
{
    private readonly AppointmentApiService _api=new(identity);
    private static string Path(int id)=>$"appointments/{id}/reminders";
    private sealed record QueueResponse(AppointmentRecord[] Items);
    public async Task<AppointmentRecord[]> ListAsync(string search, int page)
    {
        using var response = await _api.SendAsync(HttpMethod.Get,
            $"reminders/queue?search={Uri.EscapeDataString(search.Trim())}&page={page}&pageSize={AppointmentApiService.PageSize}");
        var result = await response.Content.ReadFromJsonAsync<QueueResponse>() ?? throw new JsonException();
        if (result.Items is null || result.Items.Any(a => a is null || a.Id <= 0 || a.Recipient is null || a.Status != "DaXepLich") ||
            result.Items.Select(a => a.Id).Distinct().Count() != result.Items.Length) throw new JsonException();
        return result.Items;
    }
    private static bool Valid(ReminderRecord r,int id)=>r.Id>0 && r.AppointmentId==id && r.Channel=="LogMoPhong" && r.RecordedAt!=default && r.RecordedAt.Offset==TimeSpan.Zero;
    public async Task<ReminderDetail> GetAsync(int id)
    {
        using var response=await _api.SendAsync(HttpMethod.Get,Path(id));
        using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if(!json.RootElement.TryGetProperty("reminder",out _))throw new JsonException();
        var result=json.RootElement.Deserialize<ReminderDetail>(new JsonSerializerOptions(JsonSerializerDefaults.Web))??throw new JsonException();
        if(result.Appointment is null || result.Appointment.Id!=id || (result.Reminder is{} r && !Valid(r,id)) ||
            (result.CanRecord && (result.Reminder!=null || string.IsNullOrWhiteSpace(result.Preview))))throw new JsonException();
        return result;
    }
    public async Task<ReminderRecord> RecordAsync(int id)
    {
        using var response=await _api.SendAsync(HttpMethod.Post,Path(id),new{});
        if(response.StatusCode!=HttpStatusCode.Created)throw new AppointmentApiException("outcomeUnknown","Kết quả ghi nhận chưa xác định. Hãy đối chiếu trước khi thử lại.");
        try {
            var result=await response.Content.ReadFromJsonAsync<ReminderRecord>()??throw new JsonException();
            if(!Valid(result,id)||result.Status!="DaGhiLogMoPhong"||result.RecordedBy is not{Id:>0}||string.IsNullOrWhiteSpace(result.Content))throw new JsonException();
            return result;
        }catch(Exception e)when(e is JsonException or HttpRequestException or OperationCanceledException){
            throw new AppointmentApiException("recordedUnreadable","Đã nhận xác nhận lưu. Hãy đối chiếu kết quả, không ghi lại.");
        }
    }
}
