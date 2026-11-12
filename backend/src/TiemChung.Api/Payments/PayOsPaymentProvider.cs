using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Exceptions;
using PayOS.Models;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

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
    long Amount,
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
    Task<PayOsVerifiedWebhook?> VerifyWebhookAsync(PayOsWebhookEnvelope envelope, CancellationToken cancellationToken);
}

public sealed class PayOsPaymentProvider : IPayOsPaymentProvider
{
    private const int TimeoutMs = 10_000;
    private readonly PayOSClient? _client;

    public PayOsPaymentProvider(IOptions<PayOsPaymentOptions> options)
    {
        var value = options.Value;
        if (!value.IsReady) return;
        _client = new PayOSClient(new PayOSOptions {
            ClientId = value.ClientId,
            ApiKey = value.ApiKey,
            ChecksumKey = value.ChecksumKey,
            TimeoutMs = TimeoutMs,
            MaxRetries = 0,
            LogLevel = LogLevel.None
        });
    }

    public bool IsReady => _client is not null;

    public async Task<PayOsCreatePaymentResult> CreateAsync(
        PayOsCreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var response = await Client.PaymentRequests.CreateAsync(new CreatePaymentLinkRequest {
            OrderCode = request.OrderCode,
            Amount = request.Amount,
            Description = request.Description,
            ReturnUrl = request.ReturnUrl,
            CancelUrl = request.CancelUrl
        }, new RequestOptions<CreatePaymentLinkRequest> {
            MaxRetries = 0,
            TimeoutMs = TimeoutMs,
            CancellationToken = cancellationToken
        });
        return new(response.OrderCode, response.Amount, response.PaymentLinkId, response.Status.ToString(),
            response.CheckoutUrl, response.QrCode);
    }

    public async Task<PayOsPaymentSnapshot?> ReadAsync(long orderCode, CancellationToken cancellationToken)
    {
        try {
            var response = await Client.PaymentRequests.GetAsync(orderCode, new RequestOptions {
                MaxRetries = 1,
                TimeoutMs = TimeoutMs,
                CancellationToken = cancellationToken
            });
            return new(response.OrderCode, response.Amount, response.Id, response.Status.ToString(),
                response.Transactions?.LastOrDefault()?.Reference);
        } catch (NotFoundException) {
            return null;
        }
    }

    public async Task<PayOsVerifiedWebhook?> VerifyWebhookAsync(
        PayOsWebhookEnvelope envelope, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(envelope.RawJson) || envelope.RawJson.Length > 65_536) return null;
        try {
            var webhook = JsonSerializer.Deserialize<Webhook>(envelope.RawJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (webhook is null) return null;
            cancellationToken.ThrowIfCancellationRequested();
            var data = await Client.Webhooks.VerifyAsync(webhook);
            return new(data.OrderCode, data.Amount, data.PaymentLinkId, data.Reference,
                webhook.Success && webhook.Code == "00" && data.Code == "00", data.Code);
        } catch (JsonException) {
            return null;
        } catch (WebhookException) {
            return null;
        } catch (PayOSException) {
            return null;
        }
    }

    private PayOSClient Client => _client ?? throw new InvalidOperationException("PayOS provider is unavailable.");
}
