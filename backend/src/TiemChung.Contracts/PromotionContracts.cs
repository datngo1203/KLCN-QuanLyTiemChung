using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TiemChung.Contracts;

public sealed record PromotionDto(
    int Id,
    string Name,
    string Description,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    decimal DiscountAmount,
    bool IsActive,
    bool IsAssigned,
    bool IsEnabled,
    string Currency = "VND");

public sealed record PromotionListResponse(IReadOnlyList<PromotionDto> Items);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpsertPromotionRequest(
    string Name,
    string Description,
    [property: JsonRequired] DateTimeOffset StartsAt,
    [property: JsonRequired] DateTimeOffset EndsAt,
    [property: JsonRequired] decimal DiscountAmount,
    [property: JsonRequired] bool IsEnabled);