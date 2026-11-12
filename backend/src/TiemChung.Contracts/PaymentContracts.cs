using System.Text.Json.Serialization;

namespace TiemChung.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RecordPaymentRequest(PaymentPayer? Payer, string? Method,
    decimal? CashTendered = null, CounterCard? Card = null, CounterTransfer? Transfer = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PaymentPayer(string? FullName, string? PhoneNumber, string? Email = null,
    string? ProvinceCity = null, string? WardCommune = null, string? AddressLine = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CounterCard(string? CardBrand, string? CardholderName, string? CardNumber,
    int? ExpiryMonth, int? ExpiryYear, string? AuthorizationReference);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CounterTransfer(string? SourceBankOrWallet, string? AccountHolderName,
    string? SourceAccountOrWalletId, string? TransferReference);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OnlinePaymentRequest(PaymentPayer? Payer, string? Method, bool TermsAccepted,
    InternationalCard? InternationalCard = null, DomesticPayment? Domestic = null, CounterTransfer? Transfer = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PayOsPaymentRequest(PaymentPayer? Payer, bool TermsAccepted);

public sealed record PayOsPaymentStatusDto(int RegistrationId, decimal Amount, string Currency, string State,
    string? CheckoutUrl, string? QrCode, bool CanReconcile);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record InternationalCard(string? CardBrand, string? CardholderName, string? CardNumber,
    int? ExpiryMonth, int? ExpiryYear, string? Cvv);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DomesticPayment(string? BankName, string? AccountHolderName, string? CardOrAccountNumber, string? Otp);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PaymentChannelRequest(string? Channel);

public sealed record PaymentSettingsDto(string ReceiverBankName, string ReceiverAccountName, string ReceiverAccountNumber);

public sealed record PaymentDto(int Id, int RegistrationId, decimal Amount, int? EmployeeId,
    DateTimeOffset OccurredAt, string Type, string Method, string Status, bool IsSimulation,
    string? TransactionCode, PaymentPayer? Payer, string? Provider, string? HolderName,
    string? MaskedIdentifier, string? Reference, decimal? CashTendered, decimal? ChangeAmount);

public sealed record PaymentRegistrationSummaryDto(int Id, DateTimeOffset CreatedAt,
    RegistrationProfileDto Customer, RegistrationProfileDto Recipient, decimal Subtotal, decimal TotalDue,
    decimal PaidAmount, decimal RemainingAmount, int? PaymentId, string Currency, decimal? ConsultationFee,
    string PaymentState, string? ExpectedChannel, decimal DiscountAmount = 0)
{
    public decimal TotalBeforeDiscount => Subtotal + (ConsultationFee ?? 0);
}

public sealed record PaymentRegistrationListResponse(PaymentRegistrationSummaryDto[] Items);

public sealed record RegistrationPaymentDto(int RegistrationId, string RegistrationStatus,
    RegistrationProfileDto Customer, RegistrationProfileDto Recipient, RegistrationItemDto[] Items,
    decimal Subtotal, decimal TotalDue, decimal PaidAmount, decimal RemainingAmount, string Currency,
    bool RequiresPayment, bool CanRecordPayment, bool IsSimulation, string Message, PaymentDto? Payment, decimal? ConsultationFee,
    string PaymentState, string? ExpectedChannel, bool CanConfirmPendingPayment, RegistrationContactDto? RegistrationContact,
    decimal DiscountAmount = 0)
{
    public decimal TotalBeforeDiscount => Subtotal + (ConsultationFee ?? 0);
}
