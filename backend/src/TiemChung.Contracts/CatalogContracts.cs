namespace TiemChung.Contracts;

public sealed record VaccineDto(int Id, string Name, string Manufacturer, string Origin, decimal Price);
public sealed record VaccineListResponse(string Source, IReadOnlyList<VaccineDto> Items);
public sealed record ModuleDto(string Code, string Name, string Owner, string Status);
