namespace TiemChung.Api.Payments;

public sealed class PayOsPaymentOptions
{
    public bool Enabled { get; set; }
    public string? Mode { get; set; }
    public string? ClientId { get; set; }
    public string? ApiKey { get; set; }
    public string? ChecksumKey { get; set; }
    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
    public string? WebhookUrl { get; set; }

    public bool IsReady => Enabled && Mode == "Live" &&
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(ChecksumKey) &&
        IsSecureUrl(ReturnUrl) &&
        IsSecureUrl(CancelUrl) &&
        IsSecureUrl(WebhookUrl);

    private static bool IsSecureUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo);
}

public sealed record PayOsCreatePaymentRequest(
    long OrderCode,
    long Amount,
    string Description,
    string ReturnUrl,
    string CancelUrl);

public sealed record PayOsCreatePaymentResult(
    long OrderCode,
    string PaymentLinkId,
    string Status,
    string CheckoutUrl,
    string QrCode);

public sealed record PayOsPaymentSnapshot(
    long OrderCode,
    long Amount,
    string PaymentLinkId,
    string Status,
    string? Reference);

public sealed record PayOsWebhookEnvelope(string RawJson);

public sealed record PayOsVerifiedWebhook(
    long OrderCode,
    long Amount,
    string PaymentLinkId,
    string? Reference,
    bool IsSuccessful,
    string? ProviderCode);

public interface IPayOsPaymentProvider
{
    bool IsReady { get; }
    Task<PayOsCreatePaymentResult> CreateAsync(PayOsCreatePaymentRequest request, CancellationToken cancellationToken);
    Task<PayOsPaymentSnapshot?> ReadAsync(long orderCode, CancellationToken cancellationToken);
    PayOsVerifiedWebhook? VerifyWebhook(PayOsWebhookEnvelope envelope);
}
