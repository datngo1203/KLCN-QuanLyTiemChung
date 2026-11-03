using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace TiemChung.Desktop.Services;

public sealed record CatalogType(int Id, string Name, string Description, string Code = "", string Status = "Active",
    string? TechnicalCharacteristics = null, string? ProfessionalNotes = null, string? SourceName = null,
    string? SourceUrl = null, DateTimeOffset? InformationUpdatedAt = null)
{
    public override string ToString() => Name;
}
public sealed record CatalogReference(int Id, string Code, string Name);
public sealed record CatalogSuggestions(string[] Manufacturers, string[] Origins, string[] AdministrationRoutes);
public sealed record CatalogVaccine(int Id, string Name, string Manufacturer, string Origin, decimal Price,
    string Code = "", string Status = "Active", string? TargetAudience = null, string? AdministrationRoute = null, string? AdministrationRouteNote = null, CatalogReference[]? Diseases = null, CatalogReference[]? Technologies = null);
public sealed record CatalogPackageItem(int VaccineId, int Quantity);
public sealed record CatalogPackage(int Id, string Name, decimal Price, CatalogPackageItem[] Items, string Code = "", string Status = "Active", string? TargetAudience = null);
public sealed record CatalogList<T>(string Source, T[] Items);

public sealed class CatalogApiService(IdentityApiService? identity = null)
{
    private readonly IdentityApiService _identity = identity ?? IdentityApiService.Current;
    public async Task<T[]> ListAsync<T>(string resource, string search = "", string? status = null) {
        using var response = await _identity.SendCatalogAsync(HttpMethod.Get, $"api/v1/catalog/{resource}?search={Uri.EscapeDataString(search)}" + (status == null ? "" : "&status=" + Uri.EscapeDataString(status)));
        await Check(response, resource);
        return (await response.Content.ReadFromJsonAsync<CatalogList<T>>())?.Items ?? throw new HttpRequestException("Phản hồi danh mục không hợp lệ.");
    }
    public async Task<CatalogSuggestions> SuggestionsAsync() {
        using var response = await _identity.SendCatalogAsync(HttpMethod.Get, "api/v1/catalog/suggestions");
        await Check(response);
        return await response.Content.ReadFromJsonAsync<CatalogSuggestions>() ?? throw new HttpRequestException("Phản hồi danh mục không hợp lệ.");
    }
    public Task<CatalogReference> SaveAsync(string resource, int? id, object body) => SaveAsync<CatalogReference>(resource, id, body);
    public async Task<T> SaveAsync<T>(string resource, int? id, object body) {
        using var response = await _identity.SendCatalogAsync(id.HasValue ? HttpMethod.Put : HttpMethod.Post,
            $"api/v1/catalog/{resource}" + (id.HasValue ? $"/{id}" : ""), body);
        await Check(response, resource);
        return await response.Content.ReadFromJsonAsync<T>() ?? throw new HttpRequestException("Phản hồi lưu danh mục không hợp lệ. Hãy tải lại để đối chiếu.");
    }
    public async Task DeleteAsync(string resource, int id) {
        using var response = await _identity.SendCatalogAsync(HttpMethod.Delete, $"api/v1/catalog/{resource}/{id}");
        await Check(response, resource);
    }
    private static async Task Check(HttpResponseMessage response, string? resource = null) {
        if (response.IsSuccessStatusCode) return;
        var code = "";
        try { using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); code = json.RootElement.GetProperty("code").GetString(); }
        catch (JsonException) { } catch (KeyNotFoundException) { }
        throw new HttpRequestException(code switch {
            "catalogReferenced" => "Không thể thực hiện: mục đang được tham chiếu. Hãy xử lý bản ghi liên quan trước." + (resource == "vaccines" ? " Vắc xin thuộc gói Active cần ngừng hoặc điều chỉnh gói trước." : ""),
            "validation" => "Kiểm tra mã viết hoa đúng tiền tố, tên, công nghệ, bệnh phòng, đối tượng, đường dùng, giá nguyên VND và thành phần gói.",
            "catalogDuplicate" => "Mã hoặc tên danh mục đã tồn tại. Hãy chọn giá trị khác.",
            "permissionDenied" => "Bạn không có quyền ghi danh mục này.",
            "invalidSession" => "Phiên hết hạn. Vui lòng đăng nhập lại.",
            "resourceNotFound" => "Mục không còn tồn tại. Hãy tải lại danh sách.",
            "dependencyUnavailable" => "Dịch vụ dữ liệu chưa sẵn sàng. Vui lòng thử lại.",
            _ => $"Không thực hiện được yêu cầu (HTTP {(int)response.StatusCode})."
        });
    }
}
