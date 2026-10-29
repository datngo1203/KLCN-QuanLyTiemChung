using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TiemChung.Api;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class PaymentSqlClientTests
{
    [SqlTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WindowsClientRecordsViaRealG2G3SqlAndReconcilesLostCommitResponse(bool loseResponse)
    {
        await using var f = await PaymentSqlFixture.Create();
        var transport = new LoseResponseHandler(f.Factory.Server.CreateHandler(), loseResponse);
        var identity = new IdentityApiService(new HttpClient(transport) { BaseAddress = f.Client.BaseAddress });
        Assert.True((await identity.LoginAsync("g3c.cashier", f.Password)).Succeeded);
        var api = new PaymentApiService(identity); var vm = new PaymentViewModel(api, identity);
        await vm.LoadPageAsync(1); vm.SelectedRegistration = Assert.Single(vm.Registrations); await vm.LoadDetailAsync();
        Assert.Equal(f.RegistrationId, vm.Detail!.RegistrationId); Assert.Equal(650000m, vm.Detail.TotalDue);
        Assert.Equal(2, vm.Detail.Items.Length); Assert.True(vm.CanRecord);
        vm.BeginConfirmation(); await vm.ConfirmAsync(true);
        Assert.Equal(1, transport.PostCount); Assert.False(vm.CanRecord);
        if (loseResponse) { Assert.True(vm.UnknownOutcome); Assert.Null(vm.Detail!.Payment); await vm.LoadDetailAsync(); }
        Assert.False(vm.UnknownOutcome); Assert.NotNull(vm.Detail!.Payment);
        var saved = vm.Detail.Payment;
        using var fresh = f.NewDb();
        var row = Assert.Single(await fresh.Payments.AsNoTracking().ToArrayAsync());
        Assert.Equal(f.RegistrationId, row.MaDangKy); Assert.Equal(saved.Id, row.MaThanhToan);
        Assert.Equal(650000m, saved.Amount); Assert.Equal(row.SoTien, saved.Amount);
        Assert.Equal(row.MaNhanVien, saved.EmployeeId); Assert.Equal(row.ThoiDiem, saved.OccurredAt.UtcDateTime);
        var audit = Assert.Single(await fresh.Set<SecurityAudit>().Where(a => a.Action == "payments.recorded" && a.Outcome == "Success").ToArrayAsync());
        Assert.Equal(saved.Id.ToString(), audit.ResourceId);
        var again = await api.GetAsync(f.RegistrationId); Assert.Equal(saved, again.Payment);
        Assert.Equal("paymentAlreadyRecorded", (await Assert.ThrowsAsync<PaymentApiException>(() => api.RecordAsync(f.RegistrationId))).Code);
        Assert.Equal(1, await fresh.Payments.CountAsync());
        Assert.Equal(1, await fresh.Set<SecurityAudit>().CountAsync(a => a.Action == "payments.recorded" && a.Outcome == "Success"));

        foreach (var (user, read) in new[] { ("g3c.reader", true), ("g3c.admin", false) }) {
            var other = new IdentityApiService(f.CreateClient()); Assert.True((await other.LoginAsync(user, f.Password)).Succeeded);
            var otherVm = new PaymentViewModel(identity: other); await otherVm.LoadPageAsync(1);
            Assert.Equal(read, otherVm.HasAccess); Assert.False(otherVm.CanRecord);
            var otherApi = new PaymentApiService(other);
            if (read) { otherVm.SelectedRegistration = Assert.Single(otherVm.Registrations); await otherVm.LoadDetailAsync(); Assert.NotNull(otherVm.Detail); Assert.True(otherVm.IsReadOnly); }
            else Assert.Equal("permissionDenied", (await Assert.ThrowsAsync<PaymentApiException>(() => otherApi.SearchAsync("", 1))).Code);
            Assert.Equal("permissionDenied", (await Assert.ThrowsAsync<PaymentApiException>(() => otherApi.RecordAsync(f.RegistrationId))).Code);
            await other.LogoutAsync();
        }
        await identity.LogoutAsync(); Assert.Null(vm.Detail); Assert.Empty(vm.Registrations);
    }

    private sealed class LoseResponseHandler(HttpMessageHandler inner, bool armed) : DelegatingHandler(inner)
    {
        public int PostCount;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var paymentPost = request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath.EndsWith("/payment");
            if (paymentPost) PostCount++;
            var response = await base.SendAsync(request, ct);
            if (paymentPost && armed && response.StatusCode == HttpStatusCode.Created) {
                armed = false; response.Dispose(); throw new HttpRequestException("Deliberately lost response after SQL commit.");
            }
            return response;
        }
    }
}

public sealed class SqlTheoryAttribute : TheoryAttribute
{
    public SqlTheoryAttribute() { if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION"))) Skip = "Cần SQL test riêng qua M1_SQL_TEST_CONNECTION."; }
}

// Only random test databases are provisioned here; no demo connection, seed or data is changed.
public sealed class PaymentSqlFixture : IAsyncDisposable
{
    public required WebApplicationFactory<Program> Factory { get; init; }
    public required HttpClient Client { get; init; }
    public required TiemChungDbContext Db { get; init; }
    public required string Password { get; init; }
    public int RegistrationId { get; private set; }
    public HttpClient CreateClient() => Factory.CreateClient();
    public TiemChungDbContext NewDb() => new(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(Db.Database.GetConnectionString()).Options);
    public static async Task<PaymentSqlFixture> Create(bool useKestrel = false)
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION") ?? throw new InvalidOperationException("SQL test required.")) {
            InitialCatalog = "KLCN_G3C_Test_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var password = "G3C-" + Guid.NewGuid().ToString("N") + "!Aa1";
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["ConnectionStrings:TiemChung"] = connection, ["Catalog:UseDemoData"] = "false", ["Identity:MailMode"] = "Disabled",
            ["Identity:SigningKey"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            ["DevelopmentSeed:Username"] = "g3c.admin", ["DevelopmentSeed:Password"] = password,
            ["Logging:LogLevel:Default"] = "Warning"
        }).Build();
        var db = new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(connection).Options);
        await db.Database.MigrateAsync();
        var environment = new TestEnvironment();
        await DevelopmentAdminSeed.RunAsync(db, environment, config);
        await DevelopmentCatalogSeed.RunAsync(db, environment);
        config["DevelopmentSeed:Username"] = "g3c.reception";
        await DevelopmentReceptionSeed.RunAsync(db, environment, config);
        foreach (var (user, grants) in new[] { ("g3c.cashier", new[] { "XemThanhToan", "GhiThanhToan" }), ("g3c.reader", new[] { "XemThanhToan" }) }) {
            var employee = new NhanVien { HoTen = "[DEMO] " + user, ChucDanh = "ThuNgan", TrangThai = "Active", BoPhan = "Test" };
            db.Add(employee); await db.SaveChangesAsync();
            var account = new TaiKhoan { MaNhanVien = employee.MaNhanVien, AccountType = "Employee", TenDangNhap = user,
                NormalizedUsername = IdentityRegistration.Normalize(user), TrangThai = "Active", CreatedAt = DateTimeOffset.UtcNow };
            account.MatKhauHash = new PasswordHasher<TaiKhoan>().HashPassword(account, password);
            db.Add(account); await db.SaveChangesAsync();
            foreach (var grant in await db.Set<QuyenHan>().Where(q => grants.Contains(q.Code)).ToArrayAsync()) db.Add(new TaiKhoan_QuyenHan { MaTaiKhoan = account.MaTaiKhoan, MaQuyen = grant.MaQuyen });
            await db.SaveChangesAsync();
        }
        var customer = new KhachHang { HoTen = "[DEMO] G3C Khách hàng", Email = "g3c@example.invalid" };
        db.Add(customer); await db.SaveChangesAsync();
        var recipient = new NguoiDuocTiem { HoTen = "[DEMO] G3C Người được tiêm", MaKhachHang = customer.MaKhachHang,
            NgaySinh = new(2020, 1, 1), GioiTinh = "Khác", QuanHeVoiKhachHang = "Con" };
        db.Add(recipient); await db.SaveChangesAsync();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development")
            .ConfigureAppConfiguration((_, c) => c.AddConfiguration(config)).ConfigureServices(s => {
                s.RemoveAll<DbContextOptions<TiemChungDbContext>>(); s.RemoveAll<TiemChungDbContext>();
                s.AddDbContext<TiemChungDbContext>(o => o.UseSqlServer(connection));
            }));
        if (useKestrel) factory.UseKestrel(0);
        var f = new PaymentSqlFixture { Factory = factory, Client = factory.CreateClient(), Db = db, Password = password };
        var reception = new IdentityApiService(f.CreateClient());
        Assert.True((await reception.LoginAsync("g3c.reception", password)).Succeeded);
        var registrationApi = new RegistrationApiService(reception); var catalog = await registrationApi.GetCatalogAsync();
        var vaccine = catalog.First(x => x.Kind == "vaccine"); var package = catalog.First(x => x.Kind == "package");
        f.RegistrationId = await registrationApi.CreateAsync(new(customer.MaKhachHang, recipient.MaNguoiDuocTiem, [
            new("vaccine", vaccine.Id, 2, vaccine.Price, null),
            new("package", package.Id, 1, package.Price, package.Components!.Select(c => new RegistrationComponentRequest(c.VaccineId, c.Quantity)).ToArray())
        ]));
        await reception.LogoutAsync(); return f;
    }
    public async ValueTask DisposeAsync() { Client.Dispose(); await Factory.DisposeAsync(); await Db.DisposeAsync(); }
    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "G3C.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
