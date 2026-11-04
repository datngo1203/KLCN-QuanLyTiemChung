using System.Text.Json.Serialization;

namespace TiemChung.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateRegistrationRequest(int CustomerId, int RecipientId, RegistrationItemRequest[] Items,
    [property: JsonRequired] DateOnly? PreferredDate, [property: JsonRequired] RegistrationContactRequest? Contact);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RegistrationContactRequest(string? FullName, string? PhoneNumber, string? Email,
    string? ProvinceCity, string? WardCommune, string? AddressLine);

public sealed record RegistrationContactDto(string? FullName, string? PhoneNumber, string? Email,
    string? ProvinceCity, string? WardCommune, string? AddressLine);
public sealed record RegistrationSettingsDto(decimal ConsultationFee);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RegistrationItemRequest(string Kind, int ServiceId, int Quantity,
    [property: JsonRequired] decimal ExpectedUnitPrice, RegistrationComponentRequest[]? ExpectedComponents = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RegistrationComponentRequest(int VaccineId, int Quantity);

public sealed record RegistrationProfileDto(int Id, string FullName);
public sealed record RegistrationComponentDto(int VaccineId, int Quantity, string VaccineName);
public sealed record RegistrationItemDto(int Id, string Kind, int ServiceId, string Name, int Quantity,
    decimal UnitPrice, decimal LineTotal, RegistrationComponentDto[]? Components);
public sealed record RegistrationSummaryDto(int Id, DateTimeOffset CreatedAt, string Status,
    RegistrationProfileDto Customer, RegistrationProfileDto Recipient, int? EmployeeId, decimal Subtotal, string Currency,
    DateOnly? PreferredDate, decimal? ConsultationFee, string? ExpectedChannel = null)
{
    public decimal TotalBeforeDiscount => Subtotal + (ConsultationFee ?? 0);
}
public sealed record RegistrationDto(int Id, DateTimeOffset CreatedAt, string Status,
    RegistrationProfileDto Customer, RegistrationProfileDto Recipient, int? EmployeeId,
    RegistrationItemDto[] Items, decimal Subtotal, string Currency,
    DateOnly? PreferredDate, RegistrationContactDto? Contact, decimal? ConsultationFee, string? ExpectedChannel = null)
{
    public decimal TotalBeforeDiscount => Subtotal + (ConsultationFee ?? 0);
}
public sealed record RegistrationListResponse(RegistrationSummaryDto[] Items);
