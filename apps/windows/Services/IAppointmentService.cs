using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace TiemChung.Desktop.Services;

public sealed record AppointmentSourceOption(int RegistrationId, string Status, RegistrationPerson Customer,
    RegistrationPerson Recipient, DateOnly? PreferredDate, string PaymentState, string? ExpectedChannel,
    bool CanSchedule, string ReadinessCode, string Facility, DateTime? PreferredAt = null)
{
    public string Display => $"DK-{RegistrationId:0000} · {Recipient.FullName}";
    public string PreferredDateLabel => PreferredAt?.ToString("dd/MM/yyyy HH:mm 'giờ Việt Nam'", CultureInfo.InvariantCulture)
        ?? PreferredDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "Chưa có";
    public string ReadinessLabel => ReadinessCode switch {
        "paid" => "Sẵn sàng xếp lịch · Đã thanh toán",
        "center" => "Sẵn sàng xếp lịch · Thanh toán tại trung tâm",
        "zero" => "Sẵn sàng xếp lịch · Không phát sinh khoản thu",
        "pending" => "Chờ xác nhận thanh toán",
        "unpaid" => "Chờ thanh toán",
        _ => "Chưa đủ điều kiện xếp lịch"
    };
}
public sealed record AppointmentItem(int Id, string Kind, int ServiceId, string Name, int Quantity)
{
    public string Label => $"{(Kind == "package" ? "Gói" : "Lẻ")} · {Name} × {Quantity}";
    public override string ToString() => Label;
}
public sealed record AppointmentSource(AppointmentSourceOption Source, AppointmentItem[] Items);
public sealed record AppointmentRequest(int RegistrationId, string ItemKind, int RegistrationItemId, DateTimeOffset ScheduledAt);
public sealed record AppointmentRecord(int Id, int RegistrationId, RegistrationPerson Customer, RegistrationPerson Recipient,
    string ItemKind, int RegistrationItemId, int ServiceId, string ServiceName, DateTimeOffset ScheduledAt, string Location, string Status,
    string? Version = null, bool CanManage = false, string? ManagementUnavailableReason = null)
{
    public string Label => $"LH-{Id:0000} · {Recipient.FullName}";
    public string TimeLabel => ScheduledAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
    public string SourceLabel => $"DK-{RegistrationId:0000} · {(ItemKind == "package" ? "Gói" : "Lẻ")} · {ServiceName}";
    public string StatusLabel => Status switch { "DaXepLich" => "Đã xếp lịch", "DaHuy" => "Đã hủy", _ => "Trạng thái chưa xác định" };
    public string ManagementHint => ManagementUnavailableReason == "clinicalAlreadyStarted"
        ? "Lịch đã bắt đầu khám sàng lọc, không thể đổi hoặc hủy." : Status == "DaHuy" ? "Lịch đã hủy, không thể thay đổi." : "";
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
    Task<AppointmentRecord> RescheduleAsync(int id, DateTimeOffset scheduledAt, string version);
    Task<AppointmentRecord> CancelAsync(int id, string version);
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
            throw new AppointmentApiException("outcomeUnknown", "Kết quả lưu chưa xác định. Hãy đối chiếu lịch trước khi thử lại.");
        }
        if (response.IsSuccessStatusCode) return response;
        using (response) {
            if (post && response.StatusCode is not (HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or
                HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Conflict))
                throw new AppointmentApiException("outcomeUnknown", "Kết quả lưu chưa xác định. Hãy đối chiếu lịch trước khi thử lại.");
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
                "permissionDenied" => "Cần Lễ tân đang hoạt động có quyền Quản lý lịch hẹn.",
                "validation" => "Kiểm tra dòng dịch vụ và chọn thời gian hẹn trong tương lai. Bản nháp được giữ lại.",
                "resourceNotFound" => "Không tìm thấy đăng ký hoặc lịch. Hãy tra cứu lại.",
                "appointmentAlreadyExists" => "Dòng dịch vụ đã có lịch tại thời điểm này. Hãy đối chiếu lịch đã lưu.",
                "concurrencyConflict" => "Lịch đã thay đổi. Hãy tải lại trước khi tiếp tục.",
                "appointmentNotActive" => "Lịch không còn ở trạng thái đã xếp lịch. Hãy tải lại.",
                "clinicalAlreadyStarted" => "Lịch đã bắt đầu khám sàng lọc, không thể đổi hoặc hủy.",
                "appointmentBusy" => "Lịch đang được xử lý nhắc lịch. Hãy tải lại và thử sau.",
                "appointmentSourceConflict" => "Đăng ký nguồn không hợp lệ. Hãy tra cứu lại.",
                "appointmentPaymentRequired" => "Đăng ký chưa đủ điều kiện xếp lịch do thanh toán chưa hoàn tất hoặc đang chờ xác nhận.",
                "paymentDataConflict" => "Dữ liệu thanh toán cần được đối chiếu trước khi xếp lịch.",
                "reminderSourceConflict" => "Lịch không còn đủ điều kiện ghi nhắc tham dự. Hãy kiểm tra nguồn, thời gian và mũi đã ghi.",
                "reminderAlreadyRecorded" => "Lịch đã được ghi nhận nhắc lịch. Hãy đối chiếu kết quả, không ghi thêm.",
                "reminderRecipientUnavailable" => "Chưa có email đủ điều kiện nhận nhắc. Cần một tài khoản khách hàng đã xác minh email và liên kết hợp lệ.",
                "reminderRetryUnavailable" => "Chưa thể gửi lại email. Hãy tải lại để đối chiếu trạng thái hiện tại.",
                _ => "Không tải được thông tin. Bản nháp được giữ lại. Hãy thử lại."
            });
        }
    }
    public async Task<AppointmentSourceOption[]> GetSourcesAsync(int page)
    {
        using var response = await SendAsync(HttpMethod.Get, $"appointments/sources?page={page}&pageSize={PageSize}");
        var items = (await response.Content.ReadFromJsonAsync<ListResponse<AppointmentSourceOption>>())?.Items ?? throw new JsonException();
        if (items.Any(item => !ValidSource(item))) throw new JsonException();
        return items;
    }
    public async Task<AppointmentSource> GetSourceAsync(int id)
    {
        using var response = await SendAsync(HttpMethod.Get, $"appointments/sources/{id}");
        var result = await response.Content.ReadFromJsonAsync<AppointmentSource>() ?? throw new JsonException();
        if (!ValidSource(result.Source) || result.Source.RegistrationId != id || result.Items is not { Length: > 0 })
            throw new JsonException();
        return result;
    }
    private static bool ValidSource(AppointmentSourceOption? source) => source is
        { RegistrationId: > 0, Recipient: not null, Customer: not null } && !string.IsNullOrWhiteSpace(source.Facility);
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
        if (response.StatusCode != HttpStatusCode.Created) throw new AppointmentApiException("outcomeUnknown", "Hãy đối chiếu kết quả lưu.");
        try {
            var result = await response.Content.ReadFromJsonAsync<AppointmentRecord>() ?? throw new JsonException();
            if (!Matches(result, request) || string.IsNullOrWhiteSpace(result.Location) || result.Id <= 0 || result.Recipient is null || result.Customer is null)
                throw new JsonException();
            return result;
        } catch (Exception e) when (e is JsonException or HttpRequestException or OperationCanceledException) {
            throw new AppointmentApiException("recordedUnreadable", "Lịch đã được lưu. Hãy đối chiếu để mở lại chi tiết.");
        }
    }
    public static bool Matches(AppointmentRecord row, AppointmentRequest request) => row.RegistrationId == request.RegistrationId &&
        row.ItemKind == request.ItemKind && row.RegistrationItemId == request.RegistrationItemId && row.ScheduledAt == request.ScheduledAt;

    public Task<AppointmentRecord> RescheduleAsync(int id, DateTimeOffset scheduledAt, string version) =>
        MutateAsync(id, "reschedule", new { scheduledAt, version });
    public Task<AppointmentRecord> CancelAsync(int id, string version) => MutateAsync(id, "cancel", new { version });
    private async Task<AppointmentRecord> MutateAsync(int id, string action, object body)
    {
        using var response = await SendAsync(HttpMethod.Post, $"appointments/{id}/{action}", body);
        try {
            var row = await response.Content.ReadFromJsonAsync<AppointmentRecord>() ?? throw new JsonException();
            if (row.Id != id || string.IsNullOrEmpty(row.Version) || row.Customer is null || row.Recipient is null)
                throw new JsonException();
            return row;
        } catch (Exception e) when (e is JsonException or HttpRequestException or OperationCanceledException) {
            throw new AppointmentApiException("outcomeUnknown", "Kết quả thay đổi chưa xác định. Hãy đối chiếu lịch trước khi tiếp tục.");
        }
    }
}
