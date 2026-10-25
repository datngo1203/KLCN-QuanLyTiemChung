using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace TiemChung.Desktop.Services;

public sealed record CatalogType(int Id, string Name, string Description);
public sealed record CatalogVaccine(int Id, string Name, string Manufacturer, string Origin, decimal Price, int VaccineTypeId, int InjectionTypeId);
public sealed record CatalogPackageItem(int VaccineId, int Quantity);
public sealed record CatalogPackage(int Id, string Name, decimal Price, CatalogPackageItem[] Items);
public sealed record CatalogList<T>(string Source, T[] Items);

public sealed class CatalogApiService(IdentityApiService? identity = null)
{
    private readonly IdentityApiService _identity = identity ?? IdentityApiService.Current;
    public async Task<T[]> ListAsync<T>(string resource, string search = "") {
        using var response = await _identity.SendCatalogAsync(HttpMethod.Get, $"api/v1/catalog/{resource}?search={Uri.EscapeDataString(search)}");
        await Check(response);
        return (await response.Content.ReadFromJsonAsync<CatalogList<T>>())?.Items ?? throw new HttpRequestException("Phản hồi danh mục không hợp lệ.");
    }
    public async Task SaveAsync(string resource, int? id, object body) {
        using var response = await _identity.SendCatalogAsync(id.HasValue ? HttpMethod.Put : HttpMethod.Post,
            $"api/v1/catalog/{resource}" + (id.HasValue ? $"/{id}" : ""), body);
        await Check(response);
    }
    public async Task DeleteAsync(string resource, int id) {
        using var response = await _identity.SendCatalogAsync(HttpMethod.Delete, $"api/v1/catalog/{resource}/{id}");
        await Check(response);
    }
    private static async Task Check(HttpResponseMessage response) {
        if (response.IsSuccessStatusCode) return;
        var code = "";
        try { using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); code = json.RootElement.GetProperty("code").GetString(); }
        catch (JsonException) { } catch (KeyNotFoundException) { }
        throw new HttpRequestException(code switch {
            "catalogReferenced" => "Không thể xóa: mục đang được tham chiếu.",
            "validation" => "Kiểm tra tên, loại, giá nguyên VND không âm và thành phần gói.",
            "permissionDenied" => "Bạn không có quyền ghi danh mục này.",
            "invalidSession" => "Phiên hết hạn. Vui lòng đăng nhập lại.",
            "resourceNotFound" => "Mục không còn tồn tại. Hãy tải lại danh sách.",
            _ => $"Không thực hiện được yêu cầu (HTTP {(int)response.StatusCode})."
        });
    }
}
