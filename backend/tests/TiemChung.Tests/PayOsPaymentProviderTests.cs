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
            typeof(PayOsWebhookNotification)
        }) {
            Assert.DoesNotContain(type.GetProperties(), property =>
                property.Name is "ClientId" or "ApiKey" or "ChecksumKey" or "Secret");
        }
    }

    [Fact]
    public async Task ProviderBoundaryCanBeReplacedByAFake()
    {
        IPayOsPaymentProvider provider = new FakePayOsPaymentProvider();
        var request = new PayOsCreatePaymentRequest(123, 500_000, "PAY-123",
            "https://example.invalid/return", "https://example.invalid/cancel");
        var created = await provider.CreateAsync(request, CancellationToken.None);

        Assert.Equal(123, created.OrderCode);
        Assert.Equal(created.PaymentLinkId, (await provider.ReadAsync(123, CancellationToken.None))!.PaymentLinkId);
        Assert.True(provider.VerifyWebhook(new(123, 500_000, created.PaymentLinkId, "REF-123", "test-signature")));
    }

    private sealed class FakePayOsPaymentProvider : IPayOsPaymentProvider
    {
        private readonly PayOsCreatePaymentResult _result =
            new(123, "link-123", "PENDING", "https://example.invalid/checkout", "qr-data");

        public bool IsReady => true;

        public Task<PayOsCreatePaymentResult> CreateAsync(PayOsCreatePaymentRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(_result);

        public Task<PayOsPaymentSnapshot?> ReadAsync(long orderCode, CancellationToken cancellationToken) =>
            Task.FromResult<PayOsPaymentSnapshot?>(orderCode == _result.OrderCode
                ? new(_result.OrderCode, 500_000, _result.PaymentLinkId, _result.Status, "REF-123")
                : null);

        public bool VerifyWebhook(PayOsWebhookNotification notification) =>
            notification.OrderCode == _result.OrderCode &&
            notification.PaymentLinkId == _result.PaymentLinkId &&
            notification.Signature == "test-signature";
    }
}
