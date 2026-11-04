using System.Text.Json.Serialization;

namespace TiemChung.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateRegistrationRequest(
    int CustomerId,
    int RecipientId,
    RegistrationItemRequest[] Items,
    int? PromotionId = null); // Cho phép truyền hoặc không truyền ưu đãi

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RegistrationItemRequest(string Kind, int ServiceId, int Quantity,
    [property: JsonRequired] decimal ExpectedUnitPrice, RegistrationComponentRequest[]? ExpectedComponents = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RegistrationComponentRequest(int VaccineId, int Quantity);

public sealed record RegistrationProfileDto(int Id, string FullName);
public sealed record RegistrationComponentDto(int VaccineId, int Quantity, string VaccineName);
public sealed record RegistrationItemDto(int Id, string Kind, int ServiceId, string Name, int Quantity,
    decimal UnitPrice, decimal LineTotal, RegistrationComponentDto[]? Components);

public sealed record RegistrationSummaryDto(
    int Id,
    DateTimeOffset CreatedAt,
    string Status,
    RegistrationProfileDto Customer,
    RegistrationProfileDto Recipient,
    int? EmployeeId,
    decimal Subtotal,
    string Currency,
    int? PromotionId = null,
    string? PromotionName = null,
    decimal DiscountAmount = 0m,
    decimal FinalTotal = 0m);

public sealed record RegistrationDto(
    int Id,
    DateTimeOffset CreatedAt,
    string Status,
    RegistrationProfileDto Customer,
    RegistrationProfileDto Recipient,
    int? EmployeeId,
    RegistrationItemDto[] Items,
    decimal Subtotal,
    string Currency,
    int? PromotionId = null,
    string? PromotionName = null,
    decimal DiscountAmount = 0m,
    decimal FinalTotal = 0m);

public sealed record RegistrationListResponse(RegistrationSummaryDto[] Items);