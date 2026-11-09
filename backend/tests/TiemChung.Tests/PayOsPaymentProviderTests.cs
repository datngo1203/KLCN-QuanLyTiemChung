using System.Text.Json;
using TiemChung.Api.Payments;
using Xunit;

namespace TiemChung.Tests;

public sealed class PayOsPaymentProviderTests
{
    [Fact]
    public void ReadinessRequiresLiveModeCredentialsAndSecureCallbacks()
    {
        var options = new PayOsPaymentOptions {
            Enabled = true,
            Mode = "Live",
            ClientId = "client",
            ApiKey = "api",
            ChecksumKey = "checksum",
            ReturnUrl = "https://example.invalid/return",
            CancelUrl = "https://example.invalid/cancel",
            WebhookUrl = "https://example.invalid/webhook"
        };

        Assert.True(options.IsReady);
        options.ChecksumKey = null;
        Assert.False(options.IsReady);
        options.ChecksumKey = "checksum";
        options.WebhookUrl = "http://example.invalid/webhook";
        Assert.False(options.IsReady);
        options.WebhookUrl = "https://user:password@example.invalid/webhook";
        Assert.False(options.IsReady);
        options.WebhookUrl = "https://example.invalid/webhook";
        options.Mode = "Test";
        Assert.False(options.IsReady);
        options.Mode = "Live";
        options.Enabled = false;
        Assert.False(options.IsReady);
        Assert.False(new PayOsPaymentOptions().IsReady);
    }

    [Fact]
    public void ProviderResultContractsDoNotExposeCredentials()
    {
        foreach (var type in new[] {
            typeof(PayOsCreatePaymentResult),
            typeof(PayOsPaymentSnapshot),
            typeof(PayOsVerifiedWebhook)
        }) {
            Assert.DoesNotContain(type.GetProperties(), property =>
                property.Name is "ClientId" or "ApiKey" or "ChecksumKey" or "Secret" or "RawJson" or "Signature");
        }
        Assert.NotNull(typeof(PayOsWebhookEnvelope).GetProperty(nameof(PayOsWebhookEnvelope.RawJson)));
    }

    [Fact]
    public async Task ProviderBoundaryVerifiesFullWebhookAndReturnsNormalizedResult()
    {
        IPayOsPaymentProvider provider = new FakePayOsPaymentProvider();
        var request = new PayOsCreatePaymentRequest(123, 500_000, "PAY-123",
            "https://example.invalid/return", "https://example.invalid/cancel");
        var created = await provider.CreateAsync(request, CancellationToken.None);

        Assert.Equal(123, created.OrderCode);
        Assert.Equal(created.PaymentLinkId, (await provider.ReadAsync(123, CancellationToken.None))!.PaymentLinkId);

        var verified = await provider.VerifyWebhookAsync(Webhook("00", "valid-signature"), CancellationToken.None);
        Assert.NotNull(verified);
        Assert.Equal(123, verified.OrderCode);
        Assert.Equal(500_000, verified.Amount);
        Assert.Equal(created.PaymentLinkId, verified.PaymentLinkId);
        Assert.Equal("REF-123", verified.Reference);
        Assert.True(verified.IsSuccessful);
        Assert.Equal("00", verified.ProviderCode);
        var serialized = JsonSerializer.Serialize(verified);
        Assert.DoesNotContain("RawJson", serialized);
        Assert.DoesNotContain("Signature", serialized);
        Assert.DoesNotContain("ClientId", serialized);
        Assert.DoesNotContain("ApiKey", serialized);
        Assert.DoesNotContain("ChecksumKey", serialized);
        Assert.DoesNotContain("valid-signature", serialized);
        Assert.DoesNotContain("accountNumber", serialized);
        Assert.DoesNotContain("counterAccountBankId", serialized);
    }

    [Fact]
    public async Task InvalidSignatureIsRejectedAndSignedUnsuccessfulEventStaysUnsuccessful()
    {
        IPayOsPaymentProvider provider = new FakePayOsPaymentProvider();

        Assert.Null(await provider.VerifyWebhookAsync(Webhook("00", "invalid-signature"), CancellationToken.None));
        var unsuccessful = await provider.VerifyWebhookAsync(Webhook("21", "valid-signature"), CancellationToken.None);
        Assert.NotNull(unsuccessful);
        Assert.False(unsuccessful.IsSuccessful);
        Assert.Equal("21", unsuccessful.ProviderCode);
    }

    private sealed class FakePayOsPaymentProvider : IPayOsPaymentProvider
    {
        private readonly PayOsCreatePaymentResult _result =
            new(123, 500_000, "link-123", "PENDING", "https://example.invalid/checkout", "qr-data");

        public bool IsReady => true;

        public Task<PayOsCreatePaymentResult> CreateAsync(PayOsCreatePaymentRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(_result);

        public Task<PayOsPaymentSnapshot?> ReadAsync(long orderCode, CancellationToken cancellationToken) =>
            Task.FromResult<PayOsPaymentSnapshot?>(orderCode == _result.OrderCode
                ? new(_result.OrderCode, 500_000, _result.PaymentLinkId, _result.Status, "REF-123")
                : null);

        public Task<PayOsVerifiedWebhook?> VerifyWebhookAsync(PayOsWebhookEnvelope envelope, CancellationToken cancellationToken)
        {
            using var json = JsonDocument.Parse(envelope.RawJson);
            var root = json.RootElement;
            if (root.GetProperty("signature").GetString() != "valid-signature") return Task.FromResult<PayOsVerifiedWebhook?>(null);
            var data = root.GetProperty("data");
            var code = data.GetProperty("code").GetString()!;
            return Task.FromResult<PayOsVerifiedWebhook?>(new(
                data.GetProperty("orderCode").GetInt64(),
                data.GetProperty("amount").GetInt64(),
                data.GetProperty("paymentLinkId").GetString()!,
                data.GetProperty("reference").GetString(),
                code == "00",
                code));
        }
    }

    private static PayOsWebhookEnvelope Webhook(string code, string signature) => new(
        $$"""{"code":"00","desc":"Webhook","success":true,"data":{"orderCode":123,"amount":500000,"description":"PAY-123","accountNumber":"123456789","reference":"REF-123","transactionDateTime":"2026-11-09 08:00:00","currency":"VND","paymentLinkId":"link-123","code":"{{code}}","desc":"{{(code == "00" ? "success" : "failed")}}","counterAccountBankId":"970000","counterAccountBankName":"Synthetic Bank","counterAccountName":"Synthetic","counterAccountNumber":"000111222"},"signature":"{{signature}}"}""");
}
