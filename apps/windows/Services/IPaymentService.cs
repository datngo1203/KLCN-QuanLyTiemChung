using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TiemChung.Contracts;

namespace TiemChung.Desktop.Services;

public sealed record PaymentRecord(int Id, int RegistrationId, decimal Amount, int? EmployeeId,
    DateTimeOffset OccurredAt, string Type, string Method, string Status, bool IsSimulation,
    string? TransactionCode = null, PaymentPayer? Payer = null, string? Provider = null, string? HolderName = null,
    string? MaskedIdentifier = null, string? Reference = null, decimal? CashTendered = null, decimal? ChangeAmount = null)
{
    public string Code => TransactionCode ?? $"TT-{Id:0000}";
    public string MethodLabel => Method switch {
        "TienMat" => "Tiền mặt", "TheTaiQuay" => "Thẻ tại quầy", "ChuyenKhoanQR" => "Chuyển khoản / QR",
        "ChuyenKhoanOnline" => "Chuyển khoản trực tuyến", "TheQuocTe" => "Thẻ quốc tế",
        "TheNoiDia" => "Thẻ / tài khoản nội địa", _ => "Giao dịch trước đây"
    };
    public string OccurredLabel => OccurredAt.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm:ss");
    public string PayerAddress => string.Join(", ", new[] { Payer?.AddressLine, Payer?.WardCommune, Payer?.ProvinceCity }
        .Where(s => !string.IsNullOrWhiteSpace(s)));
    public bool HasCash => CashTendered is not null;
    public bool HasProvider => Provider is not null;
    public bool HasPayer => Payer is not null;
}
public sealed record PaymentRegistration(int Id, DateTimeOffset CreatedAt, RegistrationPerson Customer,
    RegistrationPerson Recipient, decimal Subtotal, decimal TotalDue, decimal PaidAmount, decimal RemainingAmount,
    int? PaymentId, string Currency, decimal? ConsultationFee = null, string? PaymentState = null, decimal DiscountAmount = 0)
{
    public string Code => $"DK-{Id:0000}";
    public string ConsultationFeeLabel => RegistrationSettings.FeeLabel(ConsultationFee);
    public string State => RegistrationPayment.StateLabel(PaymentState, TotalDue, PaymentId is not null);
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
    [property: JsonRequired] bool IsSimulation, string Message, [property: JsonRequired] PaymentRecord? Payment, decimal? ConsultationFee = null,
    string? PaymentState = null, string? ExpectedChannel = null, bool CanConfirmPendingPayment = false,
    RegistrationContact? RegistrationContact = null, decimal DiscountAmount = 0)
{
    public string Code => $"DK-{RegistrationId:0000}";
    public string ConsultationFeeLabel => RegistrationSettings.FeeLabel(ConsultationFee);
    public string State => StateLabel(PaymentState, TotalDue, Payment is not null);
    public bool IsPending => PaymentState == "ChoXacNhan";
    public bool HasReceipt => Payment is { Status: "ThanhCong" };
    public string ExpectedChannelLabel => ExpectedChannel switch {
        "TaiTrungTam" => "Tại trung tâm", "TrucTuyen" => "Trực tuyến", _ => "Chưa chọn"
    };
    internal static string StateLabel(string? state, decimal total, bool hasPayment) =>
        state == "ChoXacNhan" ? "Chờ xác nhận" : hasPayment ? "Đã thanh toán"
        : total == 0 ? "Không phát sinh khoản thu" : "Chưa thanh toán";
}

public sealed class PaymentApiException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
public interface IPaymentService
{
    Task<PaymentRegistration[]> SearchAsync(string search, int page);
    Task<RegistrationPayment> GetAsync(int registrationId);
    Task<RegistrationPayment> RecordAsync(int registrationId, RecordPaymentRequest request);
    Task<RegistrationPayment> ConfirmPendingAsync(int registrationId);
}

public sealed class PaymentApiService(IdentityApiService? identity = null) : IPaymentService
{
    public const int PageSize = 25;
    private readonly IdentityApiService _identity = identity ?? IdentityApiService.Current;
    private readonly int _revision = (identity ?? IdentityApiService.Current).SessionRevision;
    private sealed record ListResponse(PaymentRegistration[] Items);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body = null)
    {
        var response = await _identity.SendCatalogAsync(method, "api/v1/" + path,
            body, retryUnauthorized: method != HttpMethod.Post, expectedSessionRevision: _revision);
        if (response.IsSuccessStatusCode) return response;
        using (response) {
            // A POST server error, including 503, does not prove rollback.
            if (method == HttpMethod.Post && response.StatusCode is not (HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Conflict))
                throw new PaymentApiException("outcomeUnknown", "Chưa xác định kết quả. Vui lòng đối chiếu thanh toán.");
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
                "paymentAlreadyRecorded" => "Đăng ký đã có giao dịch. Đang đối chiếu thanh toán.",
                "paymentNotRequired" => "Không phát sinh khoản thu. Đang đọc lại chi tiết.",
                "paymentNotPending" => "Giao dịch không còn chờ xác nhận. Đang đọc lại chi tiết.",
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
    public Task<RegistrationPayment> RecordAsync(int registrationId, RecordPaymentRequest request) =>
        WriteAsync(registrationId, "", HttpStatusCode.Created, request);

    public Task<RegistrationPayment> ConfirmPendingAsync(int registrationId) =>
        WriteAsync(registrationId, "/confirm", HttpStatusCode.OK, null);

    private async Task<RegistrationPayment> WriteAsync(int registrationId, string suffix, HttpStatusCode expected, object? body)
    {
        var committed = false;
        try {
            using var response = await SendAsync(HttpMethod.Post, $"registrations/{registrationId}/payment{suffix}", body);
            if (response.StatusCode != expected) throw new JsonException();
            committed = true;
            var saved = await ReadAsync(response, registrationId);
            if (!saved.HasReceipt) throw new JsonException();
            return saved;
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException or JsonException) {
            if (committed) throw new PaymentApiException("recordedUnreadable", "Giao dịch đã xác nhận. Vui lòng đối chiếu để tải biên nhận.");
            throw new PaymentApiException("outcomeUnknown", "Chưa xác định kết quả. Vui lòng đối chiếu thanh toán.");
        }
    }
}
