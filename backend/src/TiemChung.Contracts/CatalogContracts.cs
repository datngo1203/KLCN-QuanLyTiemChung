namespace TiemChung.Contracts;

public sealed record VaccineDto(int Id, string Name, string Manufacturer, string Origin, decimal Price, int VaccineTypeId = 0, int InjectionTypeId = 0);
public sealed record VaccineListResponse(string Source, IReadOnlyList<VaccineDto> Items);
public sealed record ModuleDto(string Code, string Name, string Owner, string Status);

public sealed record CatalogTypeDto(int Id, string Name, string Description);
public sealed record CatalogTypeRequest(string Name, string Description);
public sealed record VaccineRequest(string Name, string Manufacturer, string Origin, [property: System.Text.Json.Serialization.JsonRequired] decimal Price, int VaccineTypeId, int InjectionTypeId);
public sealed record PackageItemDto(int VaccineId, int Quantity, string? VaccineName = null);
public sealed record PackageRequest(string Name, [property: System.Text.Json.Serialization.JsonRequired] decimal Price, PackageItemDto[] Items);
public sealed record PackageDto(int Id, string Name, decimal Price, PackageItemDto[] Items);
