using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TiemChung.Api;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using Xunit;

namespace TiemChung.Tests;

public sealed class G1IdentitySqlTests
{
    [Fact, Trait("Category", "SqlIntegration")]
    public async Task ReceptionSeedPreservesExistingDataAndCapabilitiesMatchEmployeeWrites()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f => {
        f.Config["DevelopmentSeed:Username"] = "g1.reception";
        var password = Guid.NewGuid().ToString("N");
        f.Config["DevelopmentSeed:Password"] = password;
        foreach (var environment in new[] { "Production", "Test" })
            await Assert.ThrowsAsync<InvalidOperationException>(() => DevelopmentReceptionSeed.RunAsync(f.Db, new SeedEnvironment(environment), f.Config));
        await DevelopmentReceptionSeed.RunAsync(f.Db, new SeedEnvironment("Development"), f.Config);
        var account = await f.Db.Set<TaiKhoan>().SingleAsync(a => a.TenDangNhap == "g1.reception");
        var employee = await f.Db.Set<NhanVien>().SingleAsync(e => e.MaNhanVien == account.MaNhanVien);
        var hash = account.MatKhauHash;
        Assert.NotEqual(PasswordVerificationResult.Failed, new PasswordHasher<TaiKhoan>().VerifyHashedPassword(account, hash, password));
        Assert.Empty(await f.Db.Set<TaiKhoan_QuyenHan>().Where(g => g.MaTaiKhoan == account.MaTaiKhoan).ToListAsync());
        f.Config["DevelopmentSeed:Password"] = Guid.NewGuid().ToString("N");
        employee.HoTen = "[DEMO] preserved"; account.TrangThai = "Locked";
        await f.Db.SaveChangesAsync();
        await DevelopmentReceptionSeed.RunAsync(f.Db, new SeedEnvironment("Development"), f.Config);
        f.Db.ChangeTracker.Clear();
        account = await f.Db.Set<TaiKhoan>().SingleAsync(a => a.TenDangNhap == "g1.reception");
        Assert.Equal(hash, account.MatKhauHash); Assert.Equal("Locked", account.TrangThai);
        Assert.Equal("[DEMO] preserved", (await f.Db.Set<NhanVien>().SingleAsync(e => e.MaNhanVien == account.MaNhanVien)).HoTen);
        Assert.Equal(2, await f.Db.Set<TaiKhoan>().CountAsync());
        Assert.Equal(2, await f.Db.Set<NhanVien>().CountAsync());
        Assert.Equal(1, await f.Db.Set<SecurityAudit>().CountAsync(a => a.Action == "developmentReceptionSeeded"));
        f.Config["DevelopmentSeed:Username"] = "s2.seed";
        await Assert.ThrowsAsync<InvalidOperationException>(() => DevelopmentReceptionSeed.RunAsync(f.Db, new SeedEnvironment("Development"), f.Config));

        account.TrangThai = "Active"; await f.Db.SaveChangesAsync();
        using var client = f.Factory.CreateClient();
        var body = new CreateCustomerRequest("[DEMO] G1", null, "0900000000", "g1@example.invalid");
        foreach (var title in new[] { "Admin", "LeTan", "BacSi", "ThuNgan", "ThuKho", "DieuDuong" }) {
            await f.Db.Set<NhanVien>().Where(e => e.MaNhanVien == account.MaNhanVien).ExecuteUpdateAsync(s => s.SetProperty(e => e.ChucDanh, title));
            var session = await Login(client, "g1.reception", password);
            client.DefaultRequestHeaders.Authorization = new("Bearer", session.GetProperty("accessToken").GetString());
            var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
            var capabilities = me.GetProperty("capabilities");
            Assert.True(capabilities.GetProperty("profilesRead").GetBoolean());
            Assert.Equal(title == "LeTan", capabilities.GetProperty("profilesCreate").GetBoolean());
            Assert.Equal(title == "LeTan", capabilities.GetProperty("profilesUpdate").GetBoolean());
            using var created = await client.PostAsJsonAsync("/api/v1/customers", body);
            Assert.Equal(title == "LeTan" ? HttpStatusCode.Created : HttpStatusCode.Forbidden, created.StatusCode);
            if (title == "LeTan") {
                var customer = (await created.Content.ReadFromJsonAsync<CustomerDto>())!;
                Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/customers/{customer.Id}", body with { FullName = "[DEMO] G1 updated" })).StatusCode);
                Assert.Equal("[DEMO] G1 updated", (await f.Db.Customers.AsNoTracking().SingleAsync(c => c.MaKhachHang == customer.Id)).HoTen);
            }
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/catalog/injection-types", new { name = "denied", description = "" })).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/logout", new { clientType = "native", refreshToken = session.GetProperty("refreshToken").GetString() })).StatusCode);
        }
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task CustomerCapabilitiesAndPasswordChangeRevokeEveryAccessAndRefreshToken()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f => {
        var password = Guid.NewGuid().ToString("N");
        var next = Guid.NewGuid().ToString("N");
        var account = new TaiKhoan { AccountType = "Customer", LoginEmail = "g1@example.invalid", NormalizedEmail = "G1@EXAMPLE.INVALID",
            TenDangNhap = "g1.customer", TrangThai = "Active", EmailVerifiedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow };
        account.MatKhauHash = new PasswordHasher<TaiKhoan>().HashPassword(account, password);
        f.Db.Add(account); await f.Db.SaveChangesAsync();
        using var client = f.Factory.CreateClient();
        var first = await Login(client, account.LoginEmail, password);
        var second = await Login(client, account.LoginEmail, password);
        client.DefaultRequestHeaders.Authorization = new("Bearer", first.GetProperty("accessToken").GetString());
        var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        Assert.True(me.GetProperty("capabilities").GetProperty("profilesCreate").GetBoolean());
        Assert.False(me.GetProperty("capabilities").GetProperty("profilesUpdate").GetBoolean());
        using var created = await client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest("[DEMO] Owner", null, "0900000000", account.LoginEmail));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        me = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        Assert.True(me.GetProperty("capabilities").GetProperty("profilesUpdate").GetBoolean());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = "wrong", newPassword = next })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = password, newPassword = "short" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = password, newPassword = next })).StatusCode);
        foreach (var session in new[] { first, second }) {
            client.DefaultRequestHeaders.Authorization = new("Bearer", session.GetProperty("accessToken").GetString());
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { clientType = "native", refreshToken = session.GetProperty("refreshToken").GetString() })).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", new { identifier = account.LoginEmail, password, clientType = "native" })).StatusCode);
        var current = await Login(client, account.LoginEmail, next);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/logout", new { clientType = "native", refreshToken = current.GetProperty("refreshToken").GetString() })).StatusCode);
        Assert.Equal(1, await f.Db.Set<SecurityAudit>().CountAsync(a => a.Action == "passwordChanged" && a.SubjectAccountId == account.MaTaiKhoan));
        });
    }

    private static async Task<JsonElement> Login(HttpClient client, string identifier, string password)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { identifier, password, clientType = "native" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
    private sealed class SeedEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "G1Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
