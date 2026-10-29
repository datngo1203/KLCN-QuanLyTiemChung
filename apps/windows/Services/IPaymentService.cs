using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TiemChung.Desktop.Services;

public sealed record PaymentRecord(int Id, int RegistrationId, decimal Amount, int EmployeeId,
    DateTimeOffset OccurredAt, string Type, string Method, string Status, bool IsSimulation)
{
    public string RecordedLabel => $"TT-{Id:0000} · Nhân viên #{EmployeeId} · {OccurredAt.ToLocalTime():dd/MM/yyyy HH:mm:ss}";
}
public sealed record PaymentRegistration(int Id, DateTimeOffset CreatedAt, RegistrationPerson Customer,
    RegistrationPerson Recipient, decimal Subtotal, decimal TotalDue, decimal PaidAmount, decimal RemainingAmount,
    int? PaymentId, string Currency)
{
    public string Code => $"DK-{Id:0000}";
    public string State => PaymentId is not null ? "Đã ghi nhận mô phỏng" : TotalDue == 0 ? "Không phát sinh khoản thu" : "Chưa ghi nhận thanh toán";
}
public sealed record PaymentLine(int Id, string Kind, int ServiceId, string Name, int Quantity, decimal UnitPrice,
    decimal LineTotal, RegistrationComponent[]? Components)
{
    public string KindLabel => Kind == "package" ? "Gói" : "Vắc xin lẻ";
    public string ComponentLabel => string.Join("; ", Components?.Select(x => $"{x.VaccineName} × {x.Quantity} / gói") ?? []);
}
public sealed record RegistrationPayment(int RegistrationId, string RegistrationStatus, RegistrationPerson Customer,
    RegistrationPerson Recipient, PaymentLine[] Items, decimal Subtotal,
    [property: JsonRequired] decimal TotalDue, [property: JsonRequired] decimal PaidAmount,
    [property: JsonRequired] decimal RemainingAmount, string Currency,
    [property: JsonRequired] bool RequiresPayment, [property: JsonRequired] bool CanRecordPayment,
    [property: JsonRequired] bool IsSimulation, string Message, [property: JsonRequired] PaymentRecord? Payment)
{
    public string Code => $"DK-{RegistrationId:0000}";
    public string State => Payment is not null ? "Đã ghi nhận thanh toán mô phỏng" : !RequiresPayment && TotalDue == 0
        ? "Không phát sinh khoản thu (tổng 0)" : "Chưa ghi nhận thanh toán";
    public string RecordedLabel => Payment?.RecordedLabel ?? "Chưa có giao dịch.";
}

public sealed class PaymentApiException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
public interface IPaymentService
{
    Task<PaymentRegistration[]> SearchAsync(string search, int page);
    Task<RegistrationPayment> GetAsync(int registrationId);
    Task<RegistrationPayment> RecordAsync(int registrationId);
}

public sealed class PaymentApiService(IdentityApiService? identity = null) : IPaymentService
{
    public const int PageSize = 25;
    private readonly IdentityApiService _identity = identity ?? IdentityApiService.Current;
    private readonly int _revision = (identity ?? IdentityApiService.Current).SessionRevision;
    private sealed record ListResponse(PaymentRegistration[] Items);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path)
    {
        var response = await _identity.SendCatalogAsync(method, "api/v1/" + path,
            method == HttpMethod.Post ? new { } : null, retryUnauthorized: method != HttpMethod.Post, expectedSessionRevision: _revision);
        if (response.IsSuccessStatusCode) return response;
        using (response) {
            // A POST server error, including 503, does not prove rollback.
            if (method == HttpMethod.Post && response.StatusCode is not (HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Conflict))
                throw new PaymentApiException("outcomeUnknown", "Kết quả POST chưa xác định. Hãy đối chiếu bằng GET; không gửi lại tự động.");
            string? code = null;
            try {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (json.RootElement.TryGetProperty("code", out var value) && value.ValueKind == JsonValueKind.String) code = value.GetString();
            } catch (JsonException) { }
            code = response.StatusCode switch {
                HttpStatusCode.Unauthorized => "invalidSession",
                HttpStatusCode.Forbidden => "permissionDenied",
                HttpStatusCode.BadRequest => "validation",
                _ => code ?? "requestFailed"
            };
            throw new PaymentApiException(code, code switch {
                "invalidSession" => "Phiên hết hạn. Vui lòng đăng nhập lại để đối chiếu.",
                "permissionDenied" => "Thiếu quyền Thu ngân. Vui lòng đăng nhập lại bằng tài khoản phù hợp.",
                "validation" => "Yêu cầu không hợp lệ. Kiểm tra lại từ khóa hoặc tra cứu lại đăng ký.",
                "paymentAlreadyRecorded" => "Đăng ký đã có giao dịch. Đang đọc lại kết quả từ API.",
                "paymentNotRequired" => "Không phát sinh khoản thu. Đang đọc lại chi tiết.",
                "paymentDataConflict" => "Dữ liệu thanh toán mâu thuẫn. Đã khóa ghi; cần người phụ trách kiểm tra dữ liệu.",
                "resourceNotFound" => "Không tìm thấy đăng ký. Hãy tra cứu lại.",
                _ => "Máy chủ chưa xử lý được yêu cầu. Hãy thử lại thao tác tải / đối chiếu."
            });
        }
    }
    public async Task<PaymentRegistration[]> SearchAsync(string search, int page)
    {
        using var response = await SendAsync(HttpMethod.Get, $"payment-registrations?search={Uri.EscapeDataString(search.Trim())}&page={page}&pageSize={PageSize}");
        return (await response.Content.ReadFromJsonAsync<ListResponse>())?.Items ?? throw new JsonException();
    }
    private static async Task<RegistrationPayment> ReadAsync(HttpResponseMessage response, int id)
    {
        var result = await response.Content.ReadFromJsonAsync<RegistrationPayment>() ?? throw new JsonException();
        if (result.RegistrationId != id || result.Customer is null || result.Recipient is null || result.Items is not { Length: > 0 }
            || result.Currency != "VND" || !result.IsSimulation) throw new JsonException();
        return result;
    }
    public async Task<RegistrationPayment> GetAsync(int registrationId)
    {
        using var response = await SendAsync(HttpMethod.Get, $"registrations/{registrationId}/payment");
        return await ReadAsync(response, registrationId);
    }
    public async Task<RegistrationPayment> RecordAsync(int registrationId)
    {
        var created = false;
        try {
            using var response = await SendAsync(HttpMethod.Post, $"registrations/{registrationId}/payment");
            if (response.StatusCode != HttpStatusCode.Created) throw new JsonException();
            created = true;
            var saved = await ReadAsync(response, registrationId);
            if (saved.Payment is null) throw new JsonException();
            return saved;
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException or JsonException) {
            if (created) throw new PaymentApiException("recordedUnreadable", "Đã nhận HTTP 201 nhưng không đọc được kết quả. Chỉ đối chiếu bằng GET, không ghi lại.");
            throw new PaymentApiException("outcomeUnknown", "Kết quả POST chưa xác định. Không gửi lại. Dùng Đối chiếu bằng GET để kiểm tra giao dịch đã lưu.");
        }
    }
}
