using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TiemChung.Api;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using Xunit;

namespace TiemChung.Tests;

public sealed class DevelopmentVerificationRecipientSqlTests
{
    // Reserved .test domain exercises the non-example.invalid policy without real personal data or network.
    private const string TestEmail = "verification@rf05e.test";
    private const string Password = "Verification@2026Test!";
    private static Task Run(Func<AppointmentSqlTests.Fixture, Task> test) => SqlFixtureLifecycle.RunAsync(AppointmentSqlTests.Fixture.Create, test);
    private static WebApplicationFactory<Program> Host(AppointmentSqlTests.Fixture f, string env, string? flag, string? directory) =>
        f.Base.Factory.WithWebHostBuilder(b => b.UseEnvironment(env).ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(Settings(flag, directory))));
    private static Dictionary<string, string?> Settings(string? flag, string? directory) => new() {
        ["Identity:MailMode"] = "Synthetic",
        ["Identity:MailEncryptionKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        ["Identity:DevelopmentMailDirectory"] = directory,
        ["Identity:AllowRealDevelopmentVerificationRecipients"] = flag,
        ["ReminderMail:Mode"] = "Disabled"
    };
    private static async Task Problem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        using (response) {
            Assert.Equal(status, response.StatusCode);
            Assert.Contains($"\"code\":\"{code}\"", await response.Content.ReadAsStringAsync());
        }
    }
    private static async Task<(Guid Id, string Token)> Delivered(AppointmentSqlTests.Fixture f, SyntheticVerificationSink sink, string email, int count)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (true) {
            var messages = sink.Messages.Where(x => x.Value.Address == email).ToArray();
            if (messages.Length == count) {
                var ids = messages.Select(x => x.Key).ToArray();
                var sent = await f.Db.Set<IdentityMailOutbox>().AsNoTracking().Where(x => ids.Contains(x.Id) && x.SentAt != null).CountAsync(timeout.Token);
                if (sent == count) {
                    var current = await f.Db.Set<IdentityMailOutbox>().AsNoTracking().Where(x => ids.Contains(x.Id)).OrderByDescending(x => x.CreatedAt).FirstAsync(timeout.Token);
                    return (current.Id, sink.Messages[current.Id].Token);
                }
            }
            await Task.Delay(50, timeout.Token);
        }
    }

    [Theory][InlineData(null)][InlineData("false")]
    public async Task DevelopmentDefaultOrFalseRejectsNonSyntheticBeforeWrites(string? flag)
    {
        await Run(async f => {
            using var directory = new MailDirectory();
            await using var host = Host(f, "Development", flag, directory.Path);
            using var client = host.CreateClient();
            await Problem(await client.PostAsJsonAsync("/api/v1/auth/register", new { email = TestEmail, password = Password }), HttpStatusCode.BadRequest, "validation");
            Assert.False(await f.Db.Set<TaiKhoan>().AnyAsync(x => x.LoginEmail == TestEmail));
            Assert.Empty(await f.Db.Set<IdentityMailOutbox>().ToArrayAsync());
            Assert.Empty(host.Services.GetRequiredService<SyntheticVerificationSink>().Messages);
            Assert.Empty(Directory.GetFiles(directory.Path));
        });
    }

    [Theory][InlineData(TestEmail, "true")][InlineData("verification@example.invalid", null)]
    public async Task SupportedLocalVerificationConfirmsAccountAndEnablesReminder(string email, string? flag)
    {
        await Run(async f => {
            using var directory = new MailDirectory();
            await using var host = Host(f, "Development", flag, directory.Path);
            using var client = host.CreateClient();
            var sink = host.Services.GetRequiredService<SyntheticVerificationSink>();
            Assert.Equal(email != TestEmail, IdentityMailReadiness.IsSynthetic(email));
            using var registered = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = Password });
            Assert.Equal(HttpStatusCode.Accepted, registered.StatusCode);
            var account = await f.Db.Set<TaiKhoan>().AsNoTracking().SingleAsync(x => x.LoginEmail == email);
            Assert.Null(account.EmailVerifiedAt);
            var first = await Delivered(f, sink, email, 1);
            using (var local = JsonDocument.Parse(await File.ReadAllTextAsync(System.IO.Path.Combine(directory.Path, first.Id + ".json")))) {
                Assert.Equal(email, local.RootElement.GetProperty("email").GetString());
                Assert.Equal(first.Token, local.RootElement.GetProperty("token").GetString());
            }
            Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/v1/auth/email-verifications/resend", new { email })).StatusCode);
            var fresh = await Delivered(f, sink, email, 2);
            Assert.NotEqual(first.Token, fresh.Token);
            await Problem(await client.PostAsJsonAsync("/api/v1/auth/email-verifications/confirm", new { token = first.Token }), HttpStatusCode.BadRequest, "invalidVerificationToken");
            var registration = await f.Register();
            using var appointmentResponse = await f.Reception.PostAsJsonAsync("/api/v1/appointments", f.Request(registration, registration.Items[0]));
            Assert.Equal(HttpStatusCode.Created, appointmentResponse.StatusCode);
            var appointment = (await appointmentResponse.Content.ReadFromJsonAsync<AppointmentDto>())!;
            // Ownership fixture only; EmailVerifiedAt is changed exclusively by the public confirm endpoint.
            f.Db.AddRange(new AccountProfileLink { AccountId = account.MaTaiKhoan, CustomerId = f.CustomerId },
                new AccountProfileLink { AccountId = account.MaTaiKhoan, RecipientId = f.RecipientId });
            await f.Db.SaveChangesAsync();
            var path = $"/api/v1/appointments/{appointment.Id}/email-reminder";
            Assert.False((await f.Reception.GetFromJsonAsync<ReminderEmailDetailDto>(path))!.CanRecord);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/email-verifications/confirm", new { token = fresh.Token })).StatusCode);
            var confirmed = await f.Db.Set<TaiKhoan>().AsNoTracking().SingleAsync(x => x.MaTaiKhoan == account.MaTaiKhoan);
            Assert.NotNull(confirmed.EmailVerifiedAt); Assert.Equal(account.MatKhauHash, confirmed.MatKhauHash); Assert.Equal(account.SecurityVersion, confirmed.SecurityVersion);
            await Problem(await client.PostAsJsonAsync("/api/v1/auth/email-verifications/confirm", new { token = fresh.Token }), HttpStatusCode.BadRequest, "invalidVerificationToken");
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login", new { identifier = email, password = Password, clientType = "native" })).StatusCode);
            var preview = (await f.Reception.GetFromJsonAsync<ReminderEmailDetailDto>(path))!;
            Assert.True(preview.CanRecord); Assert.Contains("***@", preview.MaskedRecipient);
            using var enqueue = await f.Reception.PostAsJsonAsync(path, new { });
            Assert.Equal(HttpStatusCode.Created, enqueue.StatusCode);
            Assert.Equal(email, (await f.Db.Set<ReminderEmail>().AsNoTracking().SingleAsync()).RecipientEmail);
            Assert.DoesNotContain(email, await enqueue.Content.ReadAsStringAsync());
            Assert.Empty(await f.Db.Set<CustomerPasswordReset>().ToArrayAsync());
            if (email == TestEmail) {
                await Problem(await client.PostAsJsonAsync("/api/v1/auth/password-resets/request", new { email }), HttpStatusCode.BadRequest, "validation");
                Assert.Empty(await f.Db.Set<CustomerPasswordReset>().ToArrayAsync());
            }
            // Turning off intake opt-in does not revoke a confirmed account or change recipient semantics.
            await using var closed = Host(f, "Development", "false", directory.Path);
            using var closedClient = closed.CreateClient();
            Assert.Equal(HttpStatusCode.OK, (await closedClient.PostAsJsonAsync("/api/v1/auth/login", new { identifier = email, password = Password, clientType = "native" })).StatusCode);
            Assert.NotNull((await f.Reception.GetFromJsonAsync<ReminderEmailDetailDto>(path))!.Reminder);
        });
    }

    [Theory][InlineData("Production", HttpStatusCode.ServiceUnavailable)][InlineData("Test", HttpStatusCode.BadRequest)]
    public async Task NonDevelopmentFlagCannotOpenRealRecipient(string environment, HttpStatusCode status)
    {
        await Run(async f => {
            await IdentityFixture.MarkSyntheticAsync(f.Db);
            using var directory = new MailDirectory();
            await using var host = Host(f, environment, "true", directory.Path);
            using var client = host.CreateClient();
            await Problem(await client.PostAsJsonAsync("/api/v1/auth/register", new { email = TestEmail, password = Password }), status,
                environment == "Production" ? "dependencyUnavailable" : "validation");
            Assert.False(await f.Db.Set<TaiKhoan>().AnyAsync(x => x.LoginEmail == TestEmail));
            Assert.Empty(await f.Db.Set<IdentityMailOutbox>().ToArrayAsync());
            var config = new ConfigurationBuilder().AddInMemoryCollection(Settings("true", directory.Path)).Build();
            var direct = new SyntheticVerificationSink(new HostEnvironment(environment), config);
            await Assert.ThrowsAsync<InvalidOperationException>(() => direct.DeliverAsync(Guid.NewGuid(), TestEmail, "synthetic-token", default));
            Assert.Empty(direct.Messages); Assert.Empty(Directory.GetFiles(directory.Path));
        });
    }

    [Fact] public async Task OptInStillRejectsInvalidEmailAndRequiresAbsoluteLocalDirectory()
    {
        await Run(async f => {
            using var directory = new MailDirectory();
            await using (var host = Host(f, "Development", "true", directory.Path)) {
                using var client = host.CreateClient();
                await Problem(await client.PostAsJsonAsync("/api/v1/auth/register", new { email = "invalid-address", password = Password }), HttpStatusCode.BadRequest, "validation");
                Assert.Empty(host.Services.GetRequiredService<SyntheticVerificationSink>().Messages);
                Assert.Empty(Directory.GetFiles(directory.Path));
            }
            foreach (var path in new string?[] { null, "relative-mail-directory" }) {
                var config = new ConfigurationBuilder().AddInMemoryCollection(Settings("true", path)).Build();
                var sink = new SyntheticVerificationSink(new HostEnvironment("Development"), config);
                await Assert.ThrowsAsync<InvalidOperationException>(() => sink.DeliverAsync(Guid.NewGuid(), TestEmail, "synthetic-token", default));
                Assert.Empty(sink.Messages);
            }
            Assert.Empty(await f.Db.Set<IdentityMailOutbox>().ToArrayAsync());
        });
    }

    private sealed class MailDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "KLCN-RF05E-Verification-" + Guid.NewGuid().ToString("N"));
        public MailDirectory() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
    private sealed class HostEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "LocalVerificationTest";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
