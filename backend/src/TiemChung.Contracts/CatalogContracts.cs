namespace TiemChung.Contracts;

public sealed record VaccineDto(int Id, string Name, string Manufacturer, string Origin, decimal Price)
{
    public string Code { get; init; } = "";
    public string Status { get; init; } = "Active";
    public CatalogReferenceDto[] Technologies { get; init; } = [];
    public CatalogReferenceDto[] Diseases { get; init; } = [];
    public string? Description { get; init; }
    public string? TargetAudience { get; init; }
    public string? AdministrationRoute { get; init; }
    public string? AdministrationRouteNote { get; init; }
    public string Dosage { get; init; } = "";
    public decimal Volume { get; init; }
    public string? SourceName { get; init; }
    public string? SourceUrl { get; init; }
    public DateTimeOffset? InformationUpdatedAt { get; init; }
    public VaccineRegimenDto[] Regimens { get; init; } = [];
}
public sealed record VaccineListResponse(string Source, IReadOnlyList<VaccineDto> Items);
public sealed record ModuleDto(string Code, string Name, string Owner, string Status);

public sealed record CatalogReferenceDto(int Id, string Code, string Name);
public sealed record CatalogTypeDto(int Id, string Name, string Description, string Code = "", string Status = "Active");
public sealed record CatalogTypeRequest(string Name, string? Description, string? Code = null, string? Status = null);
public sealed record VaccineRequest(string Name, string Manufacturer, string Origin, [property: System.Text.Json.Serialization.JsonRequired] decimal Price,
    string? Code = null, string? Status = null, int[]? DiseaseIds = null, string? Description = null, string? TargetAudience = null,
    string? AdministrationRoute = null, string? AdministrationRouteNote = null, string? Dosage = null, decimal? Volume = null,
    string? SourceName = null, string? SourceUrl = null, DateTimeOffset? InformationUpdatedAt = null, VaccineRegimenRequest[]? Regimens = null, int[]? TechnologyIds = null);
public sealed record VaccineRegimenRequest(string Name, string TargetAudience, string? Summary, VaccineRegimenDoseRequest[] Doses, string? SourceName = null, string? SourceUrl = null);
public sealed record VaccineRegimenDoseRequest(int DoseOrder, string TimingLabel, int? MinimumIntervalDays = null, int? RecommendedIntervalDays = null, string? Note = null,
    int? AnchorDoseOrder = null, int? IntervalValue = null, string? IntervalUnit = null);
public sealed record VaccineRegimenDto(int Id, string Name, string TargetAudience, string? Summary, string? SourceName, string? SourceUrl, VaccineRegimenDoseDto[] Doses);
public sealed record VaccineRegimenDoseDto(int Id, int DoseOrder, string TimingLabel, int? MinimumIntervalDays, int? RecommendedIntervalDays, string? Note,
    int? AnchorDoseOrder = null, int? IntervalValue = null, string? IntervalUnit = null);
public sealed record PackageItemDto(int VaccineId, int Quantity, string? VaccineName = null, string? VaccineCode = null);
public sealed record PackageRequest(string Name, [property: System.Text.Json.Serialization.JsonRequired] decimal Price, PackageItemDto[] Items,
    string? Code = null, string? Status = null, string? Description = null, string? TargetAudience = null);
public sealed record PackageDto(int Id, string Name, decimal Price, PackageItemDto[] Items)
{
    public string Code { get; init; } = "";
    public string Status { get; init; } = "Active";
    public string? Description { get; init; }
    public string? TargetAudience { get; init; }
    public int TotalDoses => Items.Sum(x => x.Quantity);
    public CatalogReferenceDto[] Diseases { get; init; } = [];
}
public sealed record CatalogSuggestionsDto(string[] Manufacturers, string[] Origins, string[] AdministrationRoutes);

public sealed record VaccineTechnologyRequest(string Name, string? Description, string? Code = null, string? Status = null,
    string? TechnicalCharacteristics = null, string? ProfessionalNotes = null, string? SourceName = null,
    string? SourceUrl = null, DateTimeOffset? InformationUpdatedAt = null);
public sealed record VaccineTechnologyDto(int Id, string Name, string Description, string Code, string Status,
    string? TechnicalCharacteristics, string? ProfessionalNotes, string? SourceName, string? SourceUrl, DateTimeOffset? InformationUpdatedAt);
