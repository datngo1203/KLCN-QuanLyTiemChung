using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
namespace TiemChung.Desktop.Services;

public sealed record CustomerRecord(int Id, string Name, string? NationalId, string Phone, string Email,
    string? Salutation = null, DateOnly? BirthDate = null) { public override string ToString() => Name; }
public sealed record RecipientRecord(int Id, int CustomerId, string Name, string? NationalId, DateOnly BirthDate, string Gender, string Relation) { public override string ToString() => Name; }
public interface IProfilesService
{
    Task<IReadOnlyList<CustomerRecord>> GetCustomers(string search = "");
    Task<IReadOnlyList<RecipientRecord>> GetRecipients(string search = "", int? customerId = null);
    Task<CustomerRecord> SaveCustomer(CustomerRecord customer);
    Task<RecipientRecord> SaveRecipient(RecipientRecord recipient);
    Task Delete(string resource, int id);
}
public sealed class ProfilesApiService(IdentityApiService? identity = null) : IProfilesService
{
    private readonly IdentityApiService _identity = identity ?? IdentityApiService.Current;
    private sealed record CustomerDto(int Id,string FullName,string? IdentityNumber,string PhoneNumber,string Email,
        string? Salutation, DateOnly? DateOfBirth);
    private sealed record RecipientDto(int Id,int CustomerId,string FullName,string? IdentityNumber,DateOnly DateOfBirth,string Gender,string RelationshipToCustomer);
    private sealed record ListResponse<T>(T[] Items);
    private async Task<HttpResponseMessage> Send(HttpMethod method,string path,object? body=null) {
        var response=await _identity.SendCatalogAsync(method,"api/v1/"+path,body);
        if(response.IsSuccessStatusCode) return response;
        using(response) {
            string? code=null;
            string? detail = null;
            try {
                using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                code=json.RootElement.GetProperty("code").GetString();
                if (code == "validation" && json.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    detail = string.Join(" ", errors.EnumerateObject().SelectMany(e => e.Value.EnumerateArray().Select(m => m.GetString())));
            } catch(JsonException) {} catch(KeyNotFoundException) {}
            throw new HttpRequestException(code switch {
                "permissionDenied" => "Bạn không có quyền thao tác hồ sơ này. Ghi hồ sơ nhân viên yêu cầu Lễ tân.",
                "validation" => string.IsNullOrEmpty(detail) ? "Kiểm tra thông tin hồ sơ." : detail,
                "duplicateIdentity" => "CCCD khách hàng đã tồn tại.",
                "profileReferenced" => "Hồ sơ đang được sử dụng, không thể xóa.",
                "resourceNotFound" => "Hồ sơ không còn tồn tại. Hãy tải lại.",
                "invalidSession" => "Phiên hết hạn. Vui lòng đăng nhập lại.",
                _ => "Không thể hoàn tất yêu cầu. Vui lòng thử lại."
            });
        }
    }
    private async Task<List<T>> List<T>(string resource,string search, int? customerId = null) {
        var items=new List<T>();
        for(var page=1;;page++) {
            var scope = customerId is int id ? FormattableString.Invariant($"&customerId={id}") : "";
            using var response=await Send(HttpMethod.Get,$"{resource}?page={page}&pageSize=100&search={Uri.EscapeDataString(search)}{scope}");
            var batch=(await response.Content.ReadFromJsonAsync<ListResponse<T>>())?.Items ?? throw new HttpRequestException("Phản hồi hồ sơ không hợp lệ.");
            items.AddRange(batch); if(batch.Length<100) return items;
        }
    }
    public async Task<IReadOnlyList<CustomerRecord>> GetCustomers(string search="") => (await List<CustomerDto>("customers",search)).Select(ToCustomer).ToArray();
    public async Task<IReadOnlyList<RecipientRecord>> GetRecipients(string search="", int? customerId = null) => (await List<RecipientDto>("recipients",search,customerId)).Select(x=>new RecipientRecord(x.Id,x.CustomerId,x.FullName,x.IdentityNumber,x.DateOfBirth,x.Gender,x.RelationshipToCustomer)).ToArray();
    private static CustomerRecord ToCustomer(CustomerDto dto) =>
        new(dto.Id,dto.FullName,dto.IdentityNumber,dto.PhoneNumber,dto.Email,dto.Salutation,dto.DateOfBirth);
    public async Task<CustomerRecord> SaveCustomer(CustomerRecord x) {
        using var response=await Send(x.Id==0?HttpMethod.Post:HttpMethod.Put,"customers"+(x.Id==0?"":$"/{x.Id}"),
            new { fullName=x.Name,identityNumber=string.IsNullOrWhiteSpace(x.NationalId)?null:x.NationalId,
                phoneNumber=x.Phone,email=x.Email,salutation=x.Salutation,dateOfBirth=x.BirthDate });
        var saved=(await response.Content.ReadFromJsonAsync<CustomerDto>())!;
        return ToCustomer(saved);
    }
    public async Task<RecipientRecord> SaveRecipient(RecipientRecord x) {
        object body=x.Id==0 ? new { customerId=x.CustomerId,fullName=x.Name,identityNumber=x.NationalId,dateOfBirth=x.BirthDate,gender=x.Gender,relationshipToCustomer=x.Relation } : new { fullName=x.Name,identityNumber=x.NationalId,dateOfBirth=x.BirthDate,gender=x.Gender,relationshipToCustomer=x.Relation };
        using var response=await Send(x.Id==0?HttpMethod.Post:HttpMethod.Put,"recipients"+(x.Id==0?"":$"/{x.Id}"),body);
        var saved=(await response.Content.ReadFromJsonAsync<RecipientDto>())!;
        return new(saved.Id,saved.CustomerId,saved.FullName,saved.IdentityNumber,saved.DateOfBirth,saved.Gender,saved.RelationshipToCustomer);
    }
    public async Task Delete(string resource,int id) { using var response=await Send(HttpMethod.Delete,$"{resource}/{id}"); }
}
