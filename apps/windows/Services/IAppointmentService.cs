using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace TiemChung.Desktop.Services;

public sealed record AppointmentSourceOption(int Id, RegistrationPerson Customer, RegistrationPerson Recipient)
{
    public string Display => $"DK-{Id:0000} · {Recipient.FullName}";
    public override string ToString() => Display;
}
public sealed record AppointmentItem(int Id, string Kind, int ServiceId, string Name, int Quantity)
{
    public string Label => $"{(Kind == "package" ? "Gói" : "Lẻ")} · {Name} × {Quantity}";
    public override string ToString() => Label;
}
public sealed record AppointmentSource(int Id, string Status, RegistrationPerson Customer, RegistrationPerson Recipient, AppointmentItem[] Items);
public sealed record AppointmentRequest(int RegistrationId, string ItemKind, int RegistrationItemId, DateTimeOffset ScheduledAt, string Location);
public sealed record AppointmentRecord(int Id, int RegistrationId, RegistrationPerson Customer, RegistrationPerson Recipient,
    string ItemKind, int RegistrationItemId, int ServiceId, string ServiceName, DateTimeOffset ScheduledAt, string Location, string Status)
{
    public string Label => $"LH-{Id:0000} · {Recipient.FullName}";
    public string TimeLabel => ScheduledAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
    public string SourceLabel => $"DK-{RegistrationId:0000} · {(ItemKind == "package" ? "Gói" : "Lẻ")} · {ServiceName}";
    public string StatusLabel => Status == "DaXepLich" ? "Đã xếp lịch" : Status;
}
public sealed class AppointmentApiException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
public interface IAppointmentService
{
    Task<AppointmentSourceOption[]> GetSourcesAsync(int page);
    Task<AppointmentSource> GetSourceAsync(int id);
    Task<AppointmentRecord[]> ListAsync(int page, AppointmentRequest? match = null);
    Task<AppointmentRecord> GetAsync(int id);
    Task<AppointmentRecord> CreateAsync(AppointmentRequest request);
}

public sealed class AppointmentApiService(IdentityApiService? identity = null) : IAppointmentService
{
    public const int PageSize = 25;
    private readonly IdentityApiService _identity = identity ?? IdentityApiService.Current;
    private readonly int _revision = (identity ?? IdentityApiService.Current).SessionRevision;
    private sealed record ListResponse<T>(T[] Items);
    internal async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body = null)
    {
        var post = method == HttpMethod.Post;
        HttpResponseMessage response;
        try { response = await _identity.SendCatalogAsync(method, "api/v1/" + path, body,
            retryUnauthorized: !post, expectedSessionRevision: _revision); }
        catch (Exception e) when (post && e is HttpRequestException or OperationCanceledException) {
            throw new AppointmentApiException("outcomeUnknown", "Kết quả lưu chưa xác định. Dùng GET đối chiếu trước khi thử lại.");
        }
        if (response.IsSuccessStatusCode) return response;
        using (response) {
            if (post && response.StatusCode is not (HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or
                HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Conflict))
                throw new AppointmentApiException("outcomeUnknown", "Kết quả lưu chưa xác định. Dùng GET đối chiếu trước khi thử lại.");
            string? code = null;
            try {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (json.RootElement.TryGetProperty("code", out var value) && value.ValueKind == JsonValueKind.String) code = value.GetString();
            } catch (JsonException) { }
            code = response.StatusCode switch {
                HttpStatusCode.Unauthorized => "invalidSession", HttpStatusCode.Forbidden => "permissionDenied",
                HttpStatusCode.BadRequest => "validation", _ => code ?? "requestFailed"
            };
            throw new AppointmentApiException(code, code switch {
                "invalidSession" => "Phiên hết hạn. Vui lòng đăng nhập lại.",
                "permissionDenied" => "Cần Lễ tân Active có quyền Quản lý lịch hẹn.",
                "validation" => "Kiểm tra dòng dịch vụ, thời gian tương lai và địa điểm 1–200 ký tự. Bản nháp được giữ lại.",
                "resourceNotFound" => "Không tìm thấy đăng ký hoặc lịch. Hãy tra cứu lại.",
                "appointmentAlreadyExists" => "Dòng dịch vụ đã có lịch tại thời điểm này. Dùng GET đối chiếu.",
                "appointmentSourceConflict" => "Đăng ký nguồn không hợp lệ. Hãy tra cứu lại.",
                "reminderSourceConflict" => "Lịch không còn đủ điều kiện ghi nhắc tham dự. Hãy kiểm tra nguồn, thời gian và mũi đã ghi.",
                "reminderAlreadyRecorded" => "Lịch đã có log mô phỏng. Hãy GET đối chiếu, không ghi thêm.",
                _ => "Không đọc được API. Hãy thử tải lại; bản nháp được giữ lại."
            });
        }
    }
    public async Task<AppointmentSourceOption[]> GetSourcesAsync(int page)
    {
        using var response = await SendAsync(HttpMethod.Get, $"registrations?page={page}&pageSize={PageSize}");
        return (await response.Content.ReadFromJsonAsync<ListResponse<AppointmentSourceOption>>())?.Items ?? throw new JsonException();
    }
    public async Task<AppointmentSource> GetSourceAsync(int id)
    {
        using var response = await SendAsync(HttpMethod.Get, $"registrations/{id}");
        var result = await response.Content.ReadFromJsonAsync<AppointmentSource>() ?? throw new JsonException();
        if (result.Id != id || result.Recipient is null || result.Customer is null || result.Items is not { Length: > 0 }) throw new JsonException();
        return result;
    }
    public async Task<AppointmentRecord[]> ListAsync(int page, AppointmentRequest? match = null)
    {
        var query = $"appointments?page={page}&pageSize={PageSize}";
        if (match is not null) query += $"&registrationId={match.RegistrationId}&itemKind={match.ItemKind}&registrationItemId={match.RegistrationItemId}&scheduledAt={Uri.EscapeDataString(match.ScheduledAt.ToString("O", CultureInfo.InvariantCulture))}";
        using var response = await SendAsync(HttpMethod.Get, query);
        return (await response.Content.ReadFromJsonAsync<ListResponse<AppointmentRecord>>())?.Items ?? throw new JsonException();
    }
    public async Task<AppointmentRecord> GetAsync(int id)
    {
        using var response = await SendAsync(HttpMethod.Get, $"appointments/{id}");
        var result = await response.Content.ReadFromJsonAsync<AppointmentRecord>() ?? throw new JsonException();
        if (result.Id != id || result.Recipient is null || result.Customer is null) throw new JsonException();
        return result;
    }
    public async Task<AppointmentRecord> CreateAsync(AppointmentRequest request)
    {
        using var response = await SendAsync(HttpMethod.Post, "appointments", request);
        if (response.StatusCode != HttpStatusCode.Created) throw new AppointmentApiException("outcomeUnknown", "Cần GET đối chiếu kết quả lưu.");
        try {
            var result = await response.Content.ReadFromJsonAsync<AppointmentRecord>() ?? throw new JsonException();
            if (!Matches(result, request) || result.Location != request.Location || result.Id <= 0 || result.Recipient is null || result.Customer is null)
                throw new JsonException();
            return result;
        } catch (Exception e) when (e is JsonException or HttpRequestException or OperationCanceledException) {
            throw new AppointmentApiException("recordedUnreadable", "API đã trả 201. Chỉ GET đối chiếu, không gửi lại.");
        }
    }
    public static bool Matches(AppointmentRecord row, AppointmentRequest request) => row.RegistrationId == request.RegistrationId &&
        row.ItemKind == request.ItemKind && row.RegistrationItemId == request.RegistrationItemId && row.ScheduledAt == request.ScheduledAt;
}
