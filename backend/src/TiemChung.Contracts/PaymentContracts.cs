using System.Text.Json.Serialization;

namespace TiemChung.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RecordPaymentRequest;

public sealed record PaymentDto(int Id, int RegistrationId, decimal Amount, int EmployeeId,
    DateTimeOffset OccurredAt, string Type, string Method, string Status, bool IsSimulation);

public sealed record PaymentRegistrationSummaryDto(int Id, DateTimeOffset CreatedAt,
    RegistrationProfileDto Customer, RegistrationProfileDto Recipient, decimal Subtotal, decimal TotalDue,
    decimal PaidAmount, decimal RemainingAmount, int? PaymentId, string Currency);

public sealed record PaymentRegistrationListResponse(PaymentRegistrationSummaryDto[] Items);

public sealed record RegistrationPaymentDto(int RegistrationId, string RegistrationStatus,
    RegistrationProfileDto Customer, RegistrationProfileDto Recipient, RegistrationItemDto[] Items,
    decimal Subtotal, decimal TotalDue, decimal PaidAmount, decimal RemainingAmount, string Currency,
    bool RequiresPayment, bool CanRecordPayment, bool IsSimulation, string Message, PaymentDto? Payment);
