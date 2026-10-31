using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace TiemChung.Desktop.Services;

public sealed record ClinicalVaccine(int Id, string Name);
public sealed record ClinicalDetail(AppointmentRecord Appointment, ClinicalVaccine[] AllowedVaccines);
public sealed record AssessmentRequest(string Conclusion, string? Reason, int[] VaccineIds);
public sealed record ClinicalIndication(int Id, int VaccineId, string VaccineName, string Result, DateTimeOffset RecordedAt)
{
    public string ResultLabel => Result == "DuocTiem" ? "Được chỉ định cho buổi hẹn" : Result;
}
public sealed record ClinicalAssessment(int Id, int AppointmentId, int RegistrationId, int RecipientId,
    RegistrationPerson Doctor, DateTimeOffset RecordedAt, string Conclusion, string? Reason,
    string? VitalSigns, string? MedicalHistory, ClinicalIndication[] Indications)
{
    public string RecordedLabel => RecordedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
}
public sealed class ClinicalApiException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
public interface IScreeningService
{
    Task<AppointmentRecord[]> ListAsync(int page, string search);
    Task<ClinicalDetail> GetAsync(int id);
    Task<ClinicalAssessment?> GetAssessmentAsync(int id);
    Task<ClinicalAssessment> CreateAsync(int id, AssessmentRequest request);
}
public sealed class ScreeningApiService(IdentityApiService? identity = null) : IScreeningService
{
    public const int PageSize = 25;
    private readonly IdentityApiService _identity = identity ?? IdentityApiService.Current;
    private readonly int _revision = (identity ?? IdentityApiService.Current).SessionRevision;
    private sealed record ListResponse(AppointmentRecord[] Items);
    private sealed record AssessmentResponse(ClinicalAssessment? Assessment);
    private async Task<HttpResponseMessage> Send(HttpMethod method, string path, object? body = null)
    {
        var post = method == HttpMethod.Post;
        HttpResponseMessage response;
        try { response = await _identity.SendCatalogAsync(method, "api/v1/clinical/appointments" + path, body,
            retryUnauthorized: !post, expectedSessionRevision: _revision); }
        catch (Exception e) when (post && e is HttpRequestException or OperationCanceledException) {
            throw new ClinicalApiException("outcomeUnknown", "Kết quả lưu chưa xác định. Hãy GET đối chiếu trước khi thử lại.");
        }
        if (response.IsSuccessStatusCode) return response;
        using (response) {
            if (post && response.StatusCode is not (HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Conflict))
                throw new ClinicalApiException("outcomeUnknown", "Kết quả lưu chưa xác định. Hãy GET đối chiếu trước khi thử lại.");
            string? code = null;
            try { using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (json.RootElement.TryGetProperty("code", out var value) && value.ValueKind == JsonValueKind.String) code = value.GetString();
            } catch (JsonException) { }
            code = response.StatusCode switch {
                HttpStatusCode.Unauthorized => "invalidSession", HttpStatusCode.Forbidden => "permissionDenied",
                HttpStatusCode.BadRequest => "validation", _ => code ?? "requestFailed"
            };
            throw new ClinicalApiException(code, code switch {
                "invalidSession" => "Phiên hết hạn. Vui lòng đăng nhập lại.",
                "permissionDenied" => "Chỉ Bác sĩ đang hoạt động được xem và ghi kết luận.",
                "validation" => "Kiểm tra kết luận, lý do và vắc xin được chọn. Bản nháp được giữ lại.",
                "resourceNotFound" => "Lịch hoặc nguồn dịch vụ không còn hợp lệ. Hãy tải lại.",
                "assessmentAlreadyRecorded" => "Buổi hẹn đã có kết luận. Hãy GET đối chiếu, không ghi đè.",
                _ => "Không đọc được API. Hãy thử lại; bản nháp được giữ lại."
            });
        }
    }
    public async Task<AppointmentRecord[]> ListAsync(int page, string search)
    {
        using var response = await Send(HttpMethod.Get, $"?page={page}&pageSize={PageSize}&search={Uri.EscapeDataString(search.Trim())}");
        return (await response.Content.ReadFromJsonAsync<ListResponse>())?.Items ?? throw new JsonException();
    }
    public async Task<ClinicalDetail> GetAsync(int id)
    {
        using var response = await Send(HttpMethod.Get, $"/{id}");
        var result = await response.Content.ReadFromJsonAsync<ClinicalDetail>() ?? throw new JsonException();
        if (result.Appointment?.Id != id || result.AllowedVaccines is not { Length: > 0 } ||
            result.AllowedVaccines.Any(v => v.Id <= 0) || result.AllowedVaccines.Select(v => v.Id).Distinct().Count() != result.AllowedVaccines.Length) throw new JsonException();
        return result;
    }
    public async Task<ClinicalAssessment?> GetAssessmentAsync(int id)
    {
        using var response = await Send(HttpMethod.Get, $"/{id}/assessment");
        var result = await response.Content.ReadFromJsonAsync<AssessmentResponse>() ?? throw new JsonException();
        if (result.Assessment is { } assessment && !Valid(assessment, id)) throw new JsonException();
        return result.Assessment;
    }
    private static bool Valid(ClinicalAssessment a, int id) => a.Id > 0 && a.AppointmentId == id && a.RegistrationId > 0 &&
        a.RecipientId > 0 && a.Doctor is { Id: > 0 } && a.Indications is not null;
    public async Task<ClinicalAssessment> CreateAsync(int id, AssessmentRequest request)
    {
        using var response = await Send(HttpMethod.Post, $"/{id}/assessment", request);
        if (response.StatusCode != HttpStatusCode.Created) throw new ClinicalApiException("outcomeUnknown", "Cần GET đối chiếu kết quả lưu.");
        try {
            var result = await response.Content.ReadFromJsonAsync<ClinicalAssessment>() ?? throw new JsonException();
            if (!Valid(result, id) || result.Conclusion != request.Conclusion || result.Reason != request.Reason ||
                !result.Indications.Select(v => v.VaccineId).Order().SequenceEqual(request.VaccineIds.Order())) throw new JsonException();
            return result;
        } catch (Exception e) when (e is JsonException or HttpRequestException or OperationCanceledException) {
            throw new ClinicalApiException("recordedUnreadable", "API đã trả 201. Chỉ GET đối chiếu, không gửi lại.");
        }
    }
}
