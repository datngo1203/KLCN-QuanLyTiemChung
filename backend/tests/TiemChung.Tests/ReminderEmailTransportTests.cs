using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TiemChung.Api;
using Xunit;

namespace TiemChung.Tests;

public sealed class ReminderEmailTransportTests
{
    [Fact] public async Task SyntheticProviderCapturesOnceAndRejectsNonSyntheticRecipient()
    {
        var synthetic = new SyntheticReminderEmailTransport();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ReminderMail:Mode"] = "Synthetic" }).Build();
        var transport = new ReminderEmailTransport(config, new Host("Test"), synthetic);
        var message = new ReminderEmailMessage(1, "khach01@example.invalid", "[VNVC] Nhắc lịch tiêm chủng LH-0001", "Nội dung nhắc lịch");
        await transport.DeliverAsync(message, default); await transport.DeliverAsync(message, default);
        Assert.Equal(message, Assert.Single(synthetic.Messages).Value);
        Assert.Equal("providerRejected", (await Assert.ThrowsAsync<ReminderDeliveryFailure>(() => transport.DeliverAsync(message with { Recipient = "invalid" }, default))).Code);
    }
    [Theory][InlineData("Production", "Synthetic")][InlineData("Test", "Smtp")][InlineData("Development", "Disabled")][InlineData("Development", "Smtp")]
    public async Task DisabledNonDevelopmentOrMissingSmtpConfigNeverCallsProvider(string environment, string mode)
    {
        var synthetic = new SyntheticReminderEmailTransport();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ReminderMail:Mode"] = mode }).Build();
        var transport = new ReminderEmailTransport(config, new Host(environment), synthetic);
        Assert.Equal("providerUnavailable", (await Assert.ThrowsAsync<ReminderDeliveryFailure>(() => transport.DeliverAsync(new(1, "khach01@example.invalid", "Subject", "Body"), default))).Code);
        Assert.Empty(synthetic.Messages);
    }
    private sealed class Host(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "ReminderEmailTest";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
