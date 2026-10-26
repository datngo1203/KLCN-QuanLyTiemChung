using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using TiemChung.Api;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class S1AuthSqlTests
{
    [Fact, Trait("Category", "SqlIntegration")]
    public async Task DevelopmentSeedAndPasswordChangeUseExistingAuthorizationAndRevokeAllSessions()
    {
        var connection = Connection();
        await using var db = Db(connection);
        await db.Database.MigrateAsync();
        var password = Guid.NewGuid().ToString("N");
        var nextPassword = Guid.NewGuid().ToString("N");
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["DevelopmentSeed:Username"] = "s1.seed", ["DevelopmentSeed:Password"] = password
        }).Build();
        await Assert.ThrowsAsync<InvalidOperationException>(() => DevelopmentAdminSeed.RunAsync(db, new SeedEnvironment("Production"), config));
        await DevelopmentAdminSeed.RunAsync(db, new SeedEnvironment("Development"), config);
        var account = await db.Set<TaiKhoan>().SingleAsync();
        var originalHash = account.MatKhauHash;
        config["DevelopmentSeed:Password"] = nextPassword;
        await DevelopmentAdminSeed.RunAsync(db, new SeedEnvironment("Development"), config);
        Assert.Equal(originalHash, (await db.Set<TaiKhoan>().AsNoTracking().SingleAsync()).MatKhauHash);
        Assert.Single(await db.Set<NhanVien>().ToListAsync());
        Assert.Equal(12, await db.Set<TaiKhoan_QuyenHan>().CountAsync());
        Assert.Equal(1, await db.Set<SecurityAudit>().CountAsync(x => x.Action == "developmentAdminSeeded"));

        await using var factory = Host(connection);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = password, newPassword = nextPassword })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "wrong")).StatusCode);
        var first = await Tokens(await Login(client, password));
        var second = await Tokens(await Login(client, password));
        client.DefaultRequestHeaders.Authorization = new("Bearer", first.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/customers")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/customers", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = "wrong", newPassword = nextPassword })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = password, newPassword = "short" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = password, newPassword = nextPassword })).StatusCode);
        foreach (var session in new[] { first, second }) {
            client.DefaultRequestHeaders.Authorization = new("Bearer", session.GetProperty("accessToken").GetString());
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { clientType = "native", refreshToken = session.GetProperty("refreshToken").GetString() })).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, password)).StatusCode);
        var current = await Tokens(await Login(client, nextPassword));
        client.DefaultRequestHeaders.Authorization = new("Bearer", current.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/logout", new { clientType = "native", refreshToken = current.GetProperty("refreshToken").GetString() })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        db.ChangeTracker.Clear(); account = await db.Set<TaiKhoan>().SingleAsync();
        Assert.NotEqual(nextPassword, account.MatKhauHash);
        Assert.NotEqual(PasswordVerificationResult.Failed, new PasswordHasher<TaiKhoan>().VerifyHashedPassword(account, account.MatKhauHash, nextPassword));
        Assert.Equal(2, account.SecurityVersion);

        // Exercise the actual Windows service against HTTP + SQL, including its refresh transport.
        using var desktopHttp = factory.CreateClient();
        var desktop = new TiemChung.Desktop.Services.IdentityApiService(desktopHttp);
        var desktopPassword = Guid.NewGuid().ToString("N");
        Assert.True((await desktop.LoginAsync("s1.seed", nextPassword)).Succeeded);
        Assert.True((await desktop.ChangePasswordAsync(nextPassword, desktopPassword)).Succeeded);
        Assert.False(desktop.IsSignedIn);
        Assert.False((await desktop.LoginAsync("s1.seed", nextPassword)).Succeeded);
        Assert.True((await desktop.LoginAsync("s1.seed", desktopPassword)).Succeeded);
        await desktop.LogoutAsync();
        Assert.False(desktop.IsSignedIn);
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task DevelopmentPickupRegistersAndVerifiesOnComposeStyleDatabaseWithoutFixtureMarker()
    {
        var connection = Connection();
        await using var db = Db(connection); await db.Database.MigrateAsync();
        var directory = Path.Combine(Path.GetTempPath(), "KLCN-S1-mail", Guid.NewGuid().ToString("N"));
        await using var factory = Host(connection, directory);
        using var client = factory.CreateClient();
        var password = Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/register", new { email = "s1@example.com", password })).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/v1/auth/register", new { email = "s1@example.invalid", password })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/auth/login", new { identifier = "s1@example.invalid", password, clientType = "native" })).StatusCode);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var sink = factory.Services.GetRequiredService<SyntheticVerificationSink>();
        while (sink.Messages.Count == 0) await Task.Delay(50, timeout.Token);
        var delivery = sink.Messages.Single();
        using var mail = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory, delivery.Key + ".json")));
        Assert.Equal("s1@example.invalid", mail.RootElement.GetProperty("email").GetString());
        var token = mail.RootElement.GetProperty("token").GetString();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/email-verifications/confirm", new { token })).StatusCode);
        var session = await Tokens(await client.PostAsJsonAsync("/api/v1/auth/login", new { identifier = "s1@example.invalid", password, clientType = "native" }));
        client.DefaultRequestHeaders.Authorization = new("Bearer", session.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/customers", new { })).StatusCode);
        Assert.Empty(await db.Set<AccountProfileLink>().ToListAsync());
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/logout", new { clientType = "native", refreshToken = session.GetProperty("refreshToken").GetString() })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("Production")]
    [InlineData("Test")]
    public async Task DevelopmentPickupCannotEnableRegistrationOutsideDevelopment(string environment)
    {
        var connection = Connection();
        await using var db = Db(connection); await db.Database.MigrateAsync();
        var directory = Path.Combine(Path.GetTempPath(), "KLCN-S1-mail", Guid.NewGuid().ToString("N"));
        await using var factory = Host(connection, directory, environment);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "s1@example.invalid", password = Guid.NewGuid().ToString("N") })).StatusCode);
        Assert.Empty(await db.Set<TaiKhoan>().ToListAsync());
        Assert.False(Directory.Exists(directory));
    }

    private static Task<HttpResponseMessage> Login(HttpClient client, string password) => client.PostAsJsonAsync("/api/v1/auth/login", new { identifier = "s1.seed", password, clientType = "native" });
    private static async Task<JsonElement> Tokens(HttpResponseMessage response)
    {
        using (response) {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.Clone();
        }
    }
    private static string Connection() => new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION") ?? throw new InvalidOperationException("SQL required.")) {
        InitialCatalog = "KLCN_M1_Test_" + Guid.NewGuid().ToString("N")
    }.ConnectionString;
    private static TiemChungDbContext Db(string connection) => new(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(connection).Options);
    private static WebApplicationFactory<Program> Host(string connection, string? directory = null, string environment = "Development") => new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment(environment)
        .UseSetting("ConnectionStrings:TiemChung", connection).UseSetting("Catalog:UseDemoData", "false")
        .ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> {
            ["ConnectionStrings:TiemChung"] = connection, ["Catalog:UseDemoData"] = "false", ["Identity:MailMode"] = "Synthetic",
            ["Identity:SigningKey"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            ["Identity:MailEncryptionKey"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            ["Identity:DevelopmentMailDirectory"] = directory
        })).ConfigureServices(s => {
            s.RemoveAll<DbContextOptions<TiemChungDbContext>>(); s.RemoveAll<TiemChungDbContext>();
            s.AddDbContext<TiemChungDbContext>(o => o.UseSqlServer(connection));
        }));

    private sealed class SeedEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "S1Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
