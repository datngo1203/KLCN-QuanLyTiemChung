namespace TiemChung.Desktop.Services;

// TODO: A public read endpoint exists, but its DTO omits fields required by A04; use it only after mapping is reviewed.
public interface ICatalogService
{
    bool UseSampleData { get; set; }
    IReadOnlyList<CatalogVaccine> GetVaccines();
    IReadOnlyList<CatalogPackage> GetPackages();
    IReadOnlyList<string> GetInjectionTypes();
    IReadOnlyList<string> GetVaccineTypes();
    IReadOnlyList<string> GetPreventableDiseases();
}

public sealed record CatalogVaccine(int Id, string Name, string VaccineType, string InjectionType, string Manufacturer,
    string Origin, string Dose, decimal Volume, string Reactions, decimal Price, string Status, IReadOnlyList<string> Diseases);
public sealed record CatalogPackage(int Id, string Name, decimal Price, IReadOnlyList<(string Vaccine, int Doses)> Items);

public sealed class SampleCatalogService : ICatalogService
{
    public bool UseSampleData { get; set; } = true;
    public IReadOnlyList<CatalogVaccine> GetVaccines() => UseSampleData ?
    [
        new(1,"Vắc xin mẫu A","Mẫu bất hoạt","Tiêm chủng mẫu","Nhà sản xuất mẫu","Xuất xứ mẫu","0,5 ml",0.5m,"Phản ứng minh họa",450000m,"Đang kinh doanh",["Bệnh mẫu 01"]),
        new(2,"Vắc xin mẫu B","Mẫu tái tổ hợp","Tiêm chủng mẫu","Nhà sản xuất mẫu","Xuất xứ mẫu","1 liều",1m,"Thông tin mẫu",320000m,"Tạm ngừng",["Bệnh mẫu 02"])
    ] : [];
    public IReadOnlyList<CatalogPackage> GetPackages() => UseSampleData ? [new(1,"Gói mẫu 01",700000m,[("Vắc xin mẫu A",2)])] : [];
    public IReadOnlyList<string> GetInjectionTypes() => UseSampleData ? ["Tiêm chủng mẫu"] : [];
    public IReadOnlyList<string> GetVaccineTypes() => UseSampleData ? ["Mẫu bất hoạt", "Mẫu tái tổ hợp"] : [];
    public IReadOnlyList<string> GetPreventableDiseases() => UseSampleData ? ["Bệnh mẫu 01", "Bệnh mẫu 02", "Bệnh mẫu 03"] : [];
}
