using System;
using System.Collections.Generic;

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
    string Currency = "VND");

public sealed record PromotionListResponse(IReadOnlyList<PromotionDto> Items);

public sealed record UpsertPromotionRequest(
    string Name,
    string Description,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    decimal DiscountAmount);