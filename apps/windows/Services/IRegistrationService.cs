using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace TiemChung.Desktop.Services;

public sealed record RegistrationPerson(int Id, string FullName);
public sealed record RegistrationComponent(int VaccineId, int Quantity, string? VaccineName = null);
public sealed record RegistrationCatalogItem(int Id, string Name, string Kind, decimal Price, RegistrationComponent[]? Components)
{
    public string Label => $"{(Kind == "package" ? "Gói" : "Lẻ")} · {Name} · {Price:N0} đ";
    public override string ToString() => Label;
    public string ComponentLabel => string.Join("; ", Components?.Select(x => $"{x.VaccineName ?? $"Vắc xin #{x.VaccineId}"} × {x.Quantity}") ?? []);
}
public sealed record RegistrationLine(int ServiceId, string Name, string Kind, int Quantity, decimal UnitPrice,
    RegistrationComponent[]? Components, bool Available = true)
{
    public decimal LineTotal => Quantity * UnitPrice;
    public string KindLabel => Kind == "package" ? "Gói" : "Vắc xin lẻ";
    public string ComponentLabel => string.Join("; ", Components?.Select(x => $"{x.VaccineName ?? $"Vắc xin #{x.VaccineId}"} × {x.Quantity} / gói") ?? []);
    public string AvailabilityLabel => Available ? "" : "Dịch vụ không còn trong Catalog SQL. Hãy xóa dòng này.";
}
public sealed record RegistrationSummary(int Id, DateTimeOffset CreatedAt, string Status, RegistrationPerson Customer,
    RegistrationPerson Recipient, int? EmployeeId, decimal Subtotal, string Currency)
{
    public string Code => $"DK-{Id:0000}";
    public string StatusLabel => Status == "DaDangKy" ? "Đã đăng ký" : Status;
    public string Display => $"{Code} · {Recipient.FullName} · {Subtotal:N0} đ";
    public string CreatedLabel => CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}
public sealed record RegistrationDetail(int Id, DateTimeOffset CreatedAt, string Status, RegistrationPerson Customer,
    RegistrationPerson Recipient, int? EmployeeId, RegistrationLine[] Items, decimal Subtotal, string Currency)
{
    public string StatusLabel => Status == "DaDangKy" ? "Đã đăng ký" : Status;
}
public sealed record RegistrationRequestItem(string Kind, int ServiceId, int Quantity, decimal ExpectedUnitPrice,
    RegistrationComponentRequest[]? ExpectedComponents);
public sealed record RegistrationComponentRequest(int VaccineId, int Quantity);
public sealed record RegistrationRequest(int CustomerId, int RecipientId, RegistrationRequestItem[] Items);

public sealed class RegistrationApiException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public interface IRegistrationService
{
    Task<CustomerRecord[]> GetCustomersAsync(string search, int page);
    Task<RecipientRecord[]> GetRecipientsAsync(int customerId);
    Task<RegistrationCatalogItem[]> GetCatalogAsync();
    Task<RegistrationSummary[]> GetRegistrationsAsync(int page);
    Task<RegistrationDetail> GetDetailAsync(int id);
    Task<int> CreateAsync(RegistrationRequest request);
}

public sealed class RegistrationApiService(IdentityApiService? identity = null) : IRegistrationService
{
    public const int PageSize = 25;
    private readonly IdentityApiService _identity = identity ?? IdentityApiService.Current;
    private sealed record ListResponse<T>(T[] Items);
    private sealed record CustomerDto(int Id, string FullName, string? IdentityNumber, string PhoneNumber, string Email);
    private sealed record RecipientDto(int Id, int CustomerId, string FullName, string? IdentityNumber, DateOnly DateOfBirth, string Gender, string RelationshipToCustomer);
    private sealed record Created(int Id);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body = null)
    {
        var response = await _identity.SendCatalogAsync(method, "api/v1/" + path, body);
        if (response.IsSuccessStatusCode) return response;
        using (response) {
            string? code = null;
            try {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (json.RootElement.TryGetProperty("code", out var value)) code = value.GetString();
            } catch (JsonException) { }
            code = response.StatusCode switch {
                HttpStatusCode.Unauthorized => "invalidSession",
                HttpStatusCode.Forbidden => "permissionDenied",
                _ => code
            };
            throw new RegistrationApiException(code ?? "requestFailed", code switch {
                "invalidSession" => "Phiên hết hạn. Vui lòng đăng nhập lại.",
                "permissionDenied" => "Chỉ nhân viên Active chức danh Lễ tân được tạo và xem đăng ký.",
                "validation" => "Kiểm tra hồ sơ, số lượng 1–100 và tổng tiền hợp lệ. Bản nháp được giữ lại.",
                "catalogChanged" => "Giá hoặc thành phần gói đã thay đổi. Cần xem và xác nhận lại.",
                "resourceNotFound" => "Hồ sơ hoặc dịch vụ không còn tồn tại. Hãy tra cứu lại.",
                _ => "Máy chủ chưa xử lý được yêu cầu. Hãy thử lại sau."
            });
        }
    }

    private async Task<T[]> ListAsync<T>(string path)
    {
        using var response = await SendAsync(HttpMethod.Get, path);
        return (await response.Content.ReadFromJsonAsync<ListResponse<T>>())?.Items ?? throw new JsonException();
    }

    public async Task<CustomerRecord[]> GetCustomersAsync(string search, int page) =>
        (await ListAsync<CustomerDto>($"customers?search={Uri.EscapeDataString(search.Trim())}&page={page}&pageSize={PageSize}"))
        .Select(x => new CustomerRecord(x.Id, x.FullName, x.IdentityNumber, x.PhoneNumber, x.Email)).ToArray();

    public async Task<RecipientRecord[]> GetRecipientsAsync(int customerId)
    {
        var result = new List<RecipientRecord>();
        for (var page = 1; ; page++) {
            var batch = await ListAsync<RecipientDto>($"recipients?customerId={customerId}&page={page}&pageSize=100");
            result.AddRange(batch.Where(x => x.CustomerId == customerId).Select(x =>
                new RecipientRecord(x.Id, x.CustomerId, x.FullName, x.IdentityNumber, x.DateOfBirth, x.Gender, x.RelationshipToCustomer)));
            if (batch.Length < 100) return result.ToArray();
        }
    }

    public async Task<RegistrationCatalogItem[]> GetCatalogAsync()
    {
        async Task<T[]> Catalog<T>(string resource) {
            using var response = await SendAsync(HttpMethod.Get, "catalog/" + resource);
            var list = await response.Content.ReadFromJsonAsync<CatalogList<T>>();
            if (list?.Source != "database") throw new RegistrationApiException("catalogUnavailable", "Đăng ký yêu cầu Catalog SQL thật. Không thể dùng danh mục Sample.");
            return list.Items;
        }
        var vaccines = await Catalog<CatalogVaccine>("vaccines");
        var packages = await Catalog<CatalogPackage>("packages");
        return vaccines.Select(x => new RegistrationCatalogItem(x.Id, x.Name, "vaccine", x.Price, null))
            .Concat(packages.Select(x => new RegistrationCatalogItem(x.Id, x.Name, "package", x.Price,
                x.Items.Select(c => new RegistrationComponent(c.VaccineId, c.Quantity, vaccines.FirstOrDefault(v => v.Id == c.VaccineId)?.Name)).ToArray())))
            .ToArray();
    }

    public Task<RegistrationSummary[]> GetRegistrationsAsync(int page) =>
        ListAsync<RegistrationSummary>($"registrations?page={page}&pageSize={PageSize}");

    public async Task<RegistrationDetail> GetDetailAsync(int id)
    {
        using var response = await SendAsync(HttpMethod.Get, $"registrations/{id}");
        return await response.Content.ReadFromJsonAsync<RegistrationDetail>() ?? throw new JsonException();
    }

    public async Task<int> CreateAsync(RegistrationRequest request)
    {
        try {
            using var response = await SendAsync(HttpMethod.Post, "registrations", request);
            if (response.StatusCode != HttpStatusCode.Created) throw new JsonException();
            var created = await response.Content.ReadFromJsonAsync<Created>();
            return created is { Id: > 0 } ? created.Id : throw new JsonException();
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException or JsonException) {
            // A transport/read failure may happen after commit. Only the user can decide to create again.
            throw new RegistrationApiException("outcomeUnknown", "Chưa xác định kết quả gửi. Không gửi lại tự động. Hãy tra cứu danh sách trước khi quyết định tạo lại.");
        }
    }
}
