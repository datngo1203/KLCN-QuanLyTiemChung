using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TiemChung.Api;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class CustomerPasswordResetSqlTests
{
    private const string Email = "rf01@example.invalid";
    private const string Password = "synthetic-before-reset";
    private const string NewPassword = "synthetic-after-reset";
    private const string RequestPath = "/api/v1/auth/password-resets/request";
    private const string ConfirmPath = "/api/v1/auth/password-resets/confirm";

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task PublicFlowDeliversSyntheticMailChangesHashAndRevokesEverySession()
    {
        await SqlFixtureLifecycle.RunAsync(Fixture.Create, async f => {
            using var client = f.Factory.CreateClient();
            Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/v1/auth/register", new { email = Email, password = Password })).StatusCode);
            var verification = await f.MailToken(1);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/email-verifications/confirm", new { token = verification })).StatusCode);
            var first = await Login(client, Password);
            var second = await Login(client, Password);
            var missing = await client.PostAsJsonAsync(RequestPath, new { email = "missing@example.invalid" });
            var existing = await client.PostAsJsonAsync(RequestPath, new { email = Email });
            Assert.Equal(HttpStatusCode.Accepted, missing.StatusCode);
            Assert.Equal(missing.StatusCode, existing.StatusCode);
            Assert.Equal(await missing.Content.ReadAsStringAsync(), await existing.Content.ReadAsStringAsync());
            var token = await f.MailToken(2);
            await Invalid(f, verification);
            var reset = await f.Db.Set<CustomerPasswordReset>().SingleAsync();
            Assert.Equal(IdentityTokens.Hash(token), reset.TokenHash);
            Assert.Equal(TimeSpan.FromMinutes(30), reset.ExpiresAt - reset.CreatedAt);
            Assert.Equal(1, await f.Db.Set<TaiKhoan>().CountAsync());
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(ConfirmPath, new { token, newPassword = "short" })).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync(ConfirmPath, new { token, newPassword = NewPassword })).StatusCode);
            foreach (var session in new[] { first, second }) {
                client.DefaultRequestHeaders.Authorization = new("Bearer", session.GetProperty("accessToken").GetString());
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { clientType = "native", refreshToken = session.GetProperty("refreshToken").GetString() })).StatusCode);
            }
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginResponse(client, Password)).StatusCode);
            var current = await Login(client, NewPassword);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/logout", new { clientType = "native", refreshToken = current.GetProperty("refreshToken").GetString() })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(ConfirmPath, new { token, newPassword = NewPassword })).StatusCode);
            f.Db.ChangeTracker.Clear();
            var account = await f.Db.Set<TaiKhoan>().SingleAsync();
            Assert.NotNull((await f.Db.Set<CustomerPasswordReset>().SingleAsync()).UsedAt);
            Assert.NotEqual(NewPassword, account.MatKhauHash);
            Assert.NotEqual(PasswordVerificationResult.Failed, new PasswordHasher<TaiKhoan>().VerifyHashedPassword(account, account.MatKhauHash, NewPassword));
            Assert.Equal(1, await f.Db.Set<SecurityAudit>().CountAsync(x => x.Action == "passwordResetConfirmed"));
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task ConcurrentRequestsLeaveOnlyNewestMailEligibleAndDisabledCustomerCannotConfirm()
    {
        await SqlFixtureLifecycle.RunAsync(Fixture.Create, async f => {
            await f.AddCustomer();
            async Task Request() {
                await using var db = f.NewDb();
                await new CustomerPasswordResets(db, f.Config).RequestAsync(Email, "parallel-request", default);
            }
            await Task.WhenAll(Request(), Request());
            Assert.Equal(2, await f.Db.Set<CustomerPasswordReset>().CountAsync());
            Assert.Equal(1, await f.Db.Set<CustomerPasswordReset>().CountAsync(x => x.InvalidatedAt == null && x.UsedAt == null));
            var token = await f.MailToken(1);
            Assert.Single(f.Factory.Services.GetRequiredService<SyntheticVerificationSink>().Messages);
            Assert.Equal(1, await f.Db.Set<IdentityMailOutbox>().CountAsync(x => x.SentAt == null));
            await f.Db.Set<TaiKhoan>().ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, "Disabled"));
            await Invalid(f, token);
            Assert.Equal(0, await f.Db.Set<CustomerPasswordReset>().CountAsync(x => x.UsedAt != null));
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("Customer", "Disabled")]
    [InlineData("Employee", "Active")]
    public async Task IneligibleAccountsReturnGenericResponseAndNeverReceiveReset(string type, string state)
    {
        await SqlFixtureLifecycle.RunAsync(Fixture.Create, async f => {
            await f.AddCustomer();
            var account = await f.Db.Set<TaiKhoan>().SingleAsync();
            if (type == "Employee") {
                var employee = new NhanVien { HoTen = "[TEST] RF01", CCCD = "000000007701", SoDienThoai = "0000000000", Email = Email, ChucDanh = "Admin", BoPhan = "Test", TrangThai = "Active" };
                f.Db.Add(employee); await f.Db.SaveChangesAsync();
                account.AccountType = type; account.MaNhanVien = employee.MaNhanVien; account.NormalizedUsername = "RF01.EMPLOYEE";
            }
            account.TrangThai = state; await f.Db.SaveChangesAsync();
            using var client = f.Factory.CreateClient();
            var response = await client.PostAsJsonAsync(RequestPath, new { email = Email });
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("passwordResetPending", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
            Assert.Empty(await f.Db.Set<CustomerPasswordReset>().ToListAsync());
            Assert.Empty(await f.Db.Set<IdentityMailOutbox>().ToListAsync());
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(RequestPath, new { email = "rf01.employee" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(ConfirmPath, new { token = "wrong", newPassword = NewPassword })).StatusCode);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task ReplacementExpiryInvalidTokensAndParallelUseAreEnforced()
    {
        await SqlFixtureLifecycle.RunAsync(Fixture.Create, async f => {
            await f.AddCustomer();
            await f.Resets.RequestAsync(Email, "first", default);
            var old = await f.MailToken(1);
            await f.Resets.RequestAsync(Email, "second", default);
            var token = await f.MailToken(2);
            await Invalid(f, old); await Invalid(f, "incorrect"); await Invalid(f, ""); await Invalid(f, new string('x', 257));
            var tokenHash = IdentityTokens.Hash(token);
            await f.Db.Set<CustomerPasswordReset>().Where(x => x.TokenHash == tokenHash).ExecuteUpdateAsync(s => s
                .SetProperty(x => x.CreatedAt, DateTimeOffset.UtcNow.AddHours(-2)).SetProperty(x => x.ExpiresAt, DateTimeOffset.UtcNow.AddHours(-1)));
            await Invalid(f, token);
            await f.Resets.RequestAsync(Email, "third", default);
            token = await f.MailToken(3);
            async Task<int> Confirm() {
                await using var db = f.NewDb();
                try { await new CustomerPasswordResets(db, f.Config).ConfirmAsync(token, NewPassword, "parallel", default); return 204; }
                catch (IdentityFailure failure) { return failure.Status; }
            }
            Assert.Equal(new[] { 204, 400 }, (await Task.WhenAll(Confirm(), Confirm())).Order().ToArray());
            await Invalid(f, token);
            Assert.Equal(1, await f.Db.Set<SecurityAudit>().CountAsync(x => x.Action == "passwordResetConfirmed"));
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task AuditFailureRollsBackTokenHashPasswordAndSessionRevocation()
    {
        await SqlFixtureLifecycle.RunAsync(Fixture.Create, async f => {
            await f.AddCustomer();
            using var client = f.Factory.CreateClient();
            var session = await Login(client, Password);
            await f.Resets.RequestAsync(Email, "first", default);
            var token = await f.MailToken(1);
            var original = (await f.Db.Set<TaiKhoan>().AsNoTracking().SingleAsync()).MatKhauHash;
            await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RF01RejectAudit ON SecurityAudit AFTER INSERT AS BEGIN IF EXISTS (SELECT 1 FROM inserted WHERE Action IN ('passwordResetRequested','passwordResetConfirmed')) THROW 51077, 'Synthetic reset failure', 1; END;");
            using (var response = await client.PostAsJsonAsync(ConfirmPath, new { token, newPassword = NewPassword }))
                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            f.Db.ChangeTracker.Clear();
            Assert.Null((await f.Db.Set<CustomerPasswordReset>().SingleAsync()).UsedAt);
            var account = await f.Db.Set<TaiKhoan>().SingleAsync();
            Assert.Equal(original, account.MatKhauHash); Assert.Equal(1, account.SecurityVersion);
            Assert.Null((await f.Db.Set<IdentitySession>().SingleAsync()).RevokedAt);
            Assert.Null((await f.Db.Set<RefreshCredential>().SingleAsync()).RevokedAt);
            await Assert.ThrowsAsync<DbUpdateException>(() => f.Resets.RequestAsync(Email, "request-rollback", default));
            f.Db.ChangeTracker.Clear();
            Assert.Null((await f.Db.Set<CustomerPasswordReset>().SingleAsync()).InvalidatedAt);
            Assert.Equal(1, await f.Db.Set<IdentityMailOutbox>().CountAsync());
            client.DefaultRequestHeaders.Authorization = new("Bearer", session.GetProperty("accessToken").GetString());
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
            await f.Db.Database.ExecuteSqlRawAsync("DROP TRIGGER RF01RejectAudit");
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync(ConfirmPath, new { token, newPassword = NewPassword })).StatusCode);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task PublicEndpointsValidatePayloadsAndReuseRateLimits()
    {
        await SqlFixtureLifecycle.RunAsync(Fixture.Create, async f => {
            using var client = f.Factory.CreateClient();
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(RequestPath, new { email = Email, accountId = 1 })).StatusCode);
            for (var i = 0; i < 3; i++)
                Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync(RequestPath, new { email = Email })).StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync(RequestPath, new { email = Email })).StatusCode);
            for (var i = 0; i < 10; i++)
                Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(ConfirmPath, new { token = "wrong", newPassword = NewPassword })).StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync(ConfirmPath, new { token = "wrong", newPassword = NewPassword })).StatusCode);
            Assert.Empty(await f.Db.Set<TaiKhoan>().ToListAsync());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task MigrationPreservesVerificationMailAndCanBeAppliedAgain()
    {
        await SqlFixtureLifecycle.RunAsync(Fixture.Create, async f => {
            var migrator = f.Db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261103075906_X21InventoryReceipt");
            await f.AddCustomer();
            var account = await f.Db.Set<TaiKhoan>().SingleAsync();
            var id = Guid.NewGuid(); var mailId = Guid.NewGuid(); var hash = IdentityTokens.Hash("legacy-token");
            var now = DateTimeOffset.UtcNow; var expiry = now.AddMinutes(30);
            await f.Db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO EmailVerification (Id,AccountId,TokenHash,CreatedAt,ExpiresAt) VALUES ({id},{account.MaTaiKhoan},{hash},{now},{expiry})");
            await f.Db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO IdentityMailOutbox (Id,VerificationId,CreatedAt,ExpiresAt,Attempts) VALUES ({mailId},{id},{now},{expiry},0)");
            await f.Db.Database.MigrateAsync(); await f.Db.Database.MigrateAsync();
            var mail = await f.Db.Set<IdentityMailOutbox>().SingleAsync();
            Assert.Equal(id, mail.VerificationId); Assert.Null(mail.PasswordResetId);
            Assert.Equal(hash, (await f.Db.Set<EmailVerification>().SingleAsync()).TokenHash);
            Assert.False(f.Db.Database.HasPendingModelChanges());
        });
    }

    private static async Task Invalid(Fixture f, string token) {
        await using var db = f.NewDb();
        Assert.Equal("invalidPasswordResetToken", (await Assert.ThrowsAsync<IdentityFailure>(() => new CustomerPasswordResets(db, f.Config).ConfirmAsync(token, NewPassword, "invalid", default))).Code);
    }
    private static Task<HttpResponseMessage> LoginResponse(HttpClient client, string password) => client.PostAsJsonAsync("/api/v1/auth/login", new { identifier = Email, password, clientType = "native" });
    private static async Task<JsonElement> Login(HttpClient client, string password) {
        using var response = await LoginResponse(client, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly OwnedSqlTestDatabase owner;
        private readonly string directory = Path.Combine(Path.GetTempPath(), "KLCN-RF01", Guid.NewGuid().ToString("N"));
        public TiemChungDbContext Db { get; }
        public IConfiguration Config { get; }
        public WebApplicationFactory<Program> Factory { get; }
        public CustomerPasswordResets Resets => new(Db, Config);
        private Fixture(OwnedSqlTestDatabase database) {
            owner = database; Db = NewDb();
            Config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["ConnectionStrings:TiemChung"] = owner.ConnectionString, ["Catalog:UseDemoData"] = "false",
                ["Identity:MailMode"] = "Synthetic", ["Identity:DevelopmentMailDirectory"] = directory,
                ["Identity:SigningKey"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
                ["Identity:MailEncryptionKey"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            }).Build();
            Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development")
                .ConfigureAppConfiguration((_, c) => c.AddConfiguration(Config)));
        }
        public TiemChungDbContext NewDb() => new(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(owner.ConnectionString).Options);
        public static async Task<Fixture> Create() {
            var owner = await OwnedSqlTestDatabase.CreateAsync(Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION") ?? throw new InvalidOperationException("SQL required."));
            var f = new Fixture(owner);
            try { await f.Db.Database.MigrateAsync(); return f; }
            catch (Exception failure) { await SqlFixtureLifecycle.CleanupAfterFailureAsync(failure, f.DisposeAsync); throw; }
        }
        public async Task AddCustomer() {
            var account = new TaiKhoan { AccountType = "Customer", LoginEmail = Email, NormalizedEmail = IdentityRegistration.Normalize(Email), TenDangNhap = "rf01", TrangThai = "Active", EmailVerifiedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow };
            account.MatKhauHash = new PasswordHasher<TaiKhoan>().HashPassword(account, Password);
            Db.Add(account); await Db.SaveChangesAsync(); Db.ChangeTracker.Clear();
        }
        public async Task<string> MailToken(int count) {
            // Resolve the hosted dispatcher before waiting for delivery through the real pickup sink.
            var sink = Factory.Services.GetRequiredService<SyntheticVerificationSink>();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            while (sink.Messages.Count < count) await Task.Delay(50, timeout.Token);
            await using var db = NewDb();
            var id = (await db.Set<IdentityMailOutbox>().AsNoTracking().OrderByDescending(x => x.CreatedAt).FirstAsync()).Id;
            using var mail = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory, id + ".json")));
            Assert.Equal(Email, mail.RootElement.GetProperty("email").GetString());
            return mail.RootElement.GetProperty("token").GetString()!;
        }
        public ValueTask DisposeAsync() => SqlFixtureLifecycle.CleanupAsync(Factory.DisposeAsync, Db.DisposeAsync, owner.DisposeAsync);
    }
}
