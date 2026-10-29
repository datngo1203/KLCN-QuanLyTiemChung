using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TiemChung.Api;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class PaymentSqlTests
{
    private const string ListPath = "/api/v1/payment-registrations";
    private static string Path(int id) => $"/api/v1/registrations/{id}/payment";

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("single")]
    [InlineData("package")]
    [InlineData("mixed")]
    public async Task FullPaymentUsesSnapshotsAndPersistsWithAuditAndFreshGet(string kind)
    {
        await using var f = await Fixture.Create();
        var registration = await f.Register(kind);
        await f.Db.Vaccines.ExecuteUpdateAsync(s => s.SetProperty(x => x.Gia, 777000m));
        await f.Db.Set<GoiVacXin>().ExecuteUpdateAsync(s => s.SetProperty(x => x.GiaGoi, 999000m));
        var before = await f.Cashier.Client.GetFromJsonAsync<RegistrationPaymentDto>(Path(registration.Id));
        Assert.Equal(registration.Subtotal, before!.TotalDue); Assert.True(before.CanRecordPayment);
        Assert.True(before.RequiresPayment); Assert.Null(before.Payment); Assert.True(before.IsSimulation);
        using var response = await f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), new { });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(Path(registration.Id), response.Headers.Location!.OriginalString);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        var saved = (await response.Content.ReadFromJsonAsync<RegistrationPaymentDto>())!;
        Assert.Equal(registration.Subtotal, saved.Subtotal); Assert.Equal(saved.Subtotal, saved.TotalDue);
        Assert.Equal(saved.TotalDue, saved.PaidAmount); Assert.Equal(0, saved.RemainingAmount);
        Assert.False(saved.RequiresPayment); Assert.False(saved.CanRecordPayment);
        Assert.NotNull(saved.Payment); Assert.Equal("ToanBo", saved.Payment.Type);
        Assert.Equal("MoPhong", saved.Payment.Method); Assert.Equal("ThanhCong", saved.Payment.Status);
        Assert.True(saved.Payment.IsSimulation); Assert.Equal(f.Cashier.Employee!.MaNhanVien, saved.Payment.EmployeeId);
        Assert.Equal(TimeSpan.Zero, saved.Payment.OccurredAt.Offset);
        Assert.Equal(JsonSerializer.Serialize(registration.Items), JsonSerializer.Serialize(saved.Items));
        await using var fresh = f.NewDb();
        var payment = Assert.Single(await fresh.Payments.AsNoTracking().ToArrayAsync());
        Assert.Equal(saved.TotalDue, payment.SoTien); Assert.Equal(registration.Id, payment.MaDangKy);
        var audit = Assert.Single(await fresh.Set<SecurityAudit>().Where(a => a.Action == "payments.recorded").ToArrayAsync());
        Assert.Equal("Payment", audit.ResourceType); Assert.Equal(payment.MaThanhToan.ToString(), audit.ResourceId);
        Assert.Equal(f.Cashier.Account.MaTaiKhoan, audit.ActorAccountId); Assert.Equal(f.Cashier.Session.Id, audit.SessionId);
        Assert.Equal("Success", audit.Outcome);
        Assert.Equal("DaDangKy", (await fresh.Registrations.SingleAsync()).TrangThai);
        Assert.Empty(await fresh.Set<LichHenTiem>().ToArrayAsync());
        // Each HTTP request uses a new scoped DbContext, including the Customer verification GET.
        var reloaded = await f.Customer.Client.GetFromJsonAsync<RegistrationPaymentDto>(Path(registration.Id));
        Assert.Equal(JsonSerializer.Serialize(saved with { CanRecordPayment = false }), JsonSerializer.Serialize(reloaded));
        await Problem(await f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), new { }), 409, "paymentAlreadyRecorded");
        Assert.Equal(1, await fresh.Payments.CountAsync());
        Assert.Equal(1, await fresh.Set<SecurityAudit>().CountAsync(a => a.Action == "payments.recorded"));
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DifferentCashiersSerializeInBothOrders(bool secondCashierFirst)
    {
        var gate = new PaymentGate();
        await using var f = await Fixture.Create(gate);
        var registration = await f.Register();
        var other = await f.Actor("ThuNgan", "XemThanhToan", "GhiThanhToan");
        var firstActor = secondCashierFirst ? other : f.Cashier;
        var secondActor = secondCashierFirst ? f.Cashier : other;
        gate.Armed = true;
        var first = firstActor.Client.PostAsJsonAsync(Path(registration.Id), new { });
        await gate.Locked.Task.WaitAsync(TimeSpan.FromSeconds(20));
        var second = secondActor.Client.PostAsJsonAsync(Path(registration.Id), new { });
        try {
            await gate.Contending.Task.WaitAsync(TimeSpan.FromSeconds(20));
            Assert.False(second.IsCompleted);
        } finally { gate.Release.TrySetResult(); }
        using var success = await first;
        Assert.Equal(HttpStatusCode.Created, success.StatusCode);
        await Problem(await second, 409, "paymentAlreadyRecorded");
        await using var fresh = f.NewDb();
        var payment = Assert.Single(await fresh.Payments.ToArrayAsync());
        Assert.Equal(firstActor.Employee!.MaNhanVien, payment.MaNhanVien);
        var audit = Assert.Single(await fresh.Set<SecurityAudit>().Where(a => a.Action == "payments.recorded").ToArrayAsync());
        Assert.Equal(firstActor.Account.MaTaiKhoan, audit.ActorAccountId);
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task EveryRouteEnforcesRoleGrantsAndCurrentIdentityState()
    {
        await using var f = await Fixture.Create();
        var registration = await f.Register();
        async Task Denied(HttpClient client, int expected) {
            await Problem(await client.GetAsync(ListPath), expected);
            await Problem(await client.GetAsync(Path(registration.Id)), expected);
            await Problem(await client.PostAsJsonAsync(Path(registration.Id), new { }), expected);
        }
        using var anonymous = f.Base.Factory.CreateClient();
        await Denied(anonymous, 401);
        // Admin's existing seed has every permission code, including both payment grants.
        await Denied(f.Base.Client, 403);
        foreach (var title in new[] { "LeTan", "BacSi", "DieuDuong", "ThuKho" }) {
            var actor = await f.Actor(title, "XemThanhToan", "GhiThanhToan");
            await Denied(actor.Client, 403);
        }
        var reader = await f.Actor("ThuNgan", "XemThanhToan");
        var view = await reader.Client.GetFromJsonAsync<RegistrationPaymentDto>(Path(registration.Id));
        Assert.False(view!.CanRecordPayment); Assert.True(view.RequiresPayment);
        Assert.Equal(HttpStatusCode.OK, (await reader.Client.GetAsync(ListPath)).StatusCode);
        await Problem(await reader.Client.PostAsJsonAsync(Path(registration.Id), new { }), 403, "permissionDenied");
        var writerOnly = await f.Actor("ThuNgan", "GhiThanhToan");
        await Denied(writerOnly.Client, 403);
        await f.Db.Set<TaiKhoan_QuyenHan>().Where(g => g.MaTaiKhoan == f.Cashier.Account.MaTaiKhoan).ExecuteDeleteAsync();
        await Denied(f.Cashier.Client, 403);
        var inactive = await f.Actor("ThuNgan", "XemThanhToan", "GhiThanhToan");
        await f.Db.Set<NhanVien>().Where(e => e.MaNhanVien == inactive.Employee!.MaNhanVien)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.TrangThai, "Inactive"));
        await Denied(inactive.Client, 401);
        var revoked = await f.Actor("ThuNgan", "XemThanhToan", "GhiThanhToan");
        await f.Db.Set<IdentitySession>().Where(s => s.Id == revoked.Session.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow));
        await Denied(revoked.Client, 401);
        var locked = await f.Actor("ThuNgan", "XemThanhToan", "GhiThanhToan");
        await f.Db.Set<TaiKhoan>().Where(a => a.MaTaiKhoan == locked.Account.MaTaiKhoan)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, "Locked"));
        await Denied(locked.Client, 401);
        Assert.Empty(await f.Db.Payments.ToArrayAsync());
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task CustomerReadRequiresBothCurrentLinksAndCorrectPair()
    {
        await using var f = await Fixture.Create();
        var registration = await f.Register();
        await f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), new { });
        var own = await f.Customer.Client.GetFromJsonAsync<RegistrationPaymentDto>(Path(registration.Id));
        Assert.NotNull(own!.Payment); Assert.False(own.CanRecordPayment);
        await Problem(await f.Customer.Client.GetAsync(ListPath), 403, "permissionDenied");
        await Problem(await f.Customer.Client.PostAsJsonAsync(Path(registration.Id), new { }), 403, "permissionDenied");
        var stranger = await f.Actor(null);
        foreach (var id in new[] { registration.Id, int.MaxValue }) {
            using var response = await stranger.Client.GetAsync(Path(id));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.DoesNotContain(f.CustomerProfile.HoTen, await response.Content.ReadAsStringAsync());
        }
        await Problem(await f.Customer.Client.GetAsync(Path(int.MaxValue)), 403, "permissionDenied");
        foreach (var link in await f.Db.Set<AccountProfileLink>().Where(l => l.AccountId == f.Customer.Account.MaTaiKhoan).ToArrayAsync()) {
            link.State = "Revoked"; await f.Db.SaveChangesAsync();
            await Problem(await f.Customer.Client.GetAsync(Path(registration.Id)), 403, "permissionDenied");
            link.State = "Active"; await f.Db.SaveChangesAsync();
        }
        var other = new KhachHang { HoTen = "[DEMO] G3 other", Email = "other@example.invalid" };
        f.Db.Add(other); await f.Db.SaveChangesAsync();
        await f.Db.Recipients.Where(p => p.MaNguoiDuocTiem == f.Recipient.MaNguoiDuocTiem)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.MaKhachHang, other.MaKhachHang));
        await Problem(await f.Customer.Client.GetAsync(Path(registration.Id)), 403, "permissionDenied");
        await f.Db.Recipients.Where(p => p.MaNguoiDuocTiem == f.Recipient.MaNguoiDuocTiem)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.MaKhachHang, f.CustomerProfile.MaKhachHang));
        await f.Db.Set<TaiKhoan>().Where(a => a.MaTaiKhoan == f.Customer.Account.MaTaiKhoan)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.EmailVerifiedAt, (DateTimeOffset?)null));
        await Problem(await f.Customer.Client.GetAsync(Path(registration.Id)), 403, "emailVerificationRequired");
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task ZeroTotalHasNoPaymentAndCannotBeRecorded()
    {
        await using var f = await Fixture.Create();
        await f.Db.Vaccines.ExecuteUpdateAsync(s => s.SetProperty(x => x.Gia, 0m));
        var registration = await f.Register("single");
        var detail = await f.Cashier.Client.GetFromJsonAsync<RegistrationPaymentDto>(Path(registration.Id));
        Assert.Equal(0, detail!.Subtotal); Assert.Equal(0, detail.TotalDue); Assert.Equal(0, detail.PaidAmount);
        Assert.Equal(0, detail.RemainingAmount); Assert.False(detail.RequiresPayment); Assert.False(detail.CanRecordPayment);
        Assert.Null(detail.Payment); Assert.Equal("Không phát sinh khoản thu.", detail.Message);
        await Problem(await f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), new { }), 409, "paymentNotRequired");
        Assert.Empty(await f.Db.Payments.ToArrayAsync());
        Assert.False(await f.Db.Set<SecurityAudit>().AnyAsync(a => a.Action == "payments.recorded"));
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task LargestG2VndAmountRemainsExactThroughSqlAndJson()
    {
        await using var f = await Fixture.Create();
        const decimal limit = 9999999999999999m;
        await f.Db.Vaccines.ExecuteUpdateAsync(s => s.SetProperty(v => v.Gia, limit));
        var vaccine = await f.Db.Vaccines.AsNoTracking().FirstAsync();
        var request = new CreateRegistrationRequest(f.CustomerProfile.MaKhachHang, f.Recipient.MaNguoiDuocTiem,
            [new("vaccine", vaccine.MaVacXin, 1, limit)]);
        using var created = await f.Customer.Client.PostAsJsonAsync("/api/v1/registrations", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var registration = (await created.Content.ReadFromJsonAsync<RegistrationDto>())!;
        using var response = await f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), new { });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var saved = (await response.Content.ReadFromJsonAsync<RegistrationPaymentDto>())!;
        Assert.Equal(limit, saved.TotalDue); Assert.Equal(limit, saved.Payment!.Amount);
        await using var fresh = f.NewDb();
        Assert.Equal(limit, (await fresh.Payments.SingleAsync()).SoTien);
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task UnrelatedUniqueViolationIsNotReportedAsDuplicatePayment()
    {
        await using var f = await Fixture.Create();
        var first = await f.Register(); var second = await f.Register();
        Assert.Equal(HttpStatusCode.Created, (await f.Cashier.Client.PostAsJsonAsync(Path(first.Id), new { })).StatusCode);
        await f.Db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX G3FixtureOtherUnique ON dbo.ThanhToan(SoTien);");
        await Problem(await f.Cashier.Client.PostAsJsonAsync(Path(second.Id), new { }), 503, "dependencyUnavailable");
        Assert.Equal(1, await f.Db.Payments.CountAsync());
        Assert.Equal(1, await f.Db.Set<SecurityAudit>().CountAsync(a => a.Action == "payments.recorded"));
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("payment")]
    [InlineData("audit")]
    public async Task PaymentOrAuditFailureRollsBackBothAndLeavesRegistrationUnchanged(string failure)
    {
        await using var f = await Fixture.Create();
        var registration = await f.Register();
        await f.Db.Database.ExecuteSqlRawAsync(failure == "payment"
            ? "ALTER TABLE dbo.ThanhToan ADD CONSTRAINT G3FixtureFailure CHECK (SoTien < 0);"
            : "CREATE TRIGGER G3FixtureFailure ON dbo.SecurityAudit AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE Action='payments.recorded') THROW 51000, 'Synthetic G3 audit failure', 1;");
        await Problem(await f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), new { }), 503, "dependencyUnavailable");
        await using var fresh = f.NewDb();
        Assert.Empty(await fresh.Payments.ToArrayAsync());
        Assert.False(await fresh.Set<SecurityAudit>().AnyAsync(a => a.Action == "payments.recorded"));
        Assert.Equal("DaDangKy", (await fresh.Registrations.SingleAsync()).TrangThai);
        await f.Db.Database.ExecuteSqlRawAsync(failure == "payment"
            ? "ALTER TABLE dbo.ThanhToan DROP CONSTRAINT G3FixtureFailure;" : "DROP TRIGGER dbo.G3FixtureFailure;");
        Assert.Equal(HttpStatusCode.Created, (await f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), new { })).StatusCode);
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task SearchPaginationFiltersAndValidationAreStableAndDoNotWrite()
    {
        await using var f = await Fixture.Create();
        var first = await f.Register("single"); var second = await f.Register("package");
        var all = (await f.Cashier.Client.GetFromJsonAsync<PaymentRegistrationListResponse>(ListPath))!.Items;
        Assert.Equal(new[] { second.Id, first.Id }, all.Select(x => x.Id));
        Assert.All(all, x => { Assert.Equal(x.Subtotal, x.TotalDue); Assert.Equal(x.TotalDue, x.RemainingAmount); Assert.Null(x.PaymentId); });
        var page = (await f.Cashier.Client.GetFromJsonAsync<PaymentRegistrationListResponse>(ListPath + "?page=2&pageSize=1"))!.Items;
        Assert.Equal(first.Id, Assert.Single(page).Id);
        foreach (var search in new[] { first.Id.ToString(), $"DK-{first.Id:0000}" })
            Assert.Equal(first.Id, Assert.Single((await f.Cashier.Client.GetFromJsonAsync<PaymentRegistrationListResponse>(ListPath + "?search=" + search))!.Items).Id);
        foreach (var search in new[] { f.CustomerProfile.HoTen, f.Recipient.HoTen })
            Assert.Equal(2, (await f.Cashier.Client.GetFromJsonAsync<PaymentRegistrationListResponse>(ListPath + "?search=" + Uri.EscapeDataString(search)))!.Items.Length);
        Assert.Equal(2, (await f.Cashier.Client.GetFromJsonAsync<PaymentRegistrationListResponse>(ListPath + $"?customerId={f.CustomerProfile.MaKhachHang}&recipientId={f.Recipient.MaNguoiDuocTiem}"))!.Items.Length);
        Assert.Empty((await f.Cashier.Client.GetFromJsonAsync<PaymentRegistrationListResponse>(ListPath + "?recipientId=2147483647"))!.Items);
        Assert.Empty((await f.Cashier.Client.GetFromJsonAsync<PaymentRegistrationListResponse>(ListPath + "?page=3&pageSize=1"))!.Items);
        foreach (var query in new[] { "?page=0", "?pageSize=101", "?page=2147483647&pageSize=100", "?customerId=-1", "?recipientId=0", "?page=abc", "?search=" + new string('x', 101) })
            await Problem(await f.Cashier.Client.GetAsync(ListPath + query), 400, "validation");
        foreach (var id in new[] { 0, -1 }) {
            await Problem(await f.Cashier.Client.GetAsync(Path(id)), 400, "validation");
            await Problem(await f.Cashier.Client.PostAsJsonAsync(Path(id), new { }), 400, "validation");
        }
        await Problem(await f.Cashier.Client.GetAsync(Path(int.MaxValue)), 404, "resourceNotFound");
        await Problem(await f.Cashier.Client.PostAsJsonAsync(Path(int.MaxValue), new { }), 404, "resourceNotFound");
        foreach (var field in new[] { "amount", "employeeId", "status", "method", "type", "discount", "paidAt", "unexpected" })
            await Problem(await f.Cashier.Client.PostAsync(Path(first.Id), new StringContent("{\"" + field + "\":1}", Encoding.UTF8, "application/json")), 400, "validation");
        foreach (var json in new[] { "{", "null", "[]" })
            await Problem(await f.Cashier.Client.PostAsync(Path(first.Id), new StringContent(json, Encoding.UTF8, "application/json")), 400, "validation");
        Assert.Empty(await f.Db.Payments.ToArrayAsync());
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("status")]
    [InlineData("type")]
    [InlineData("method")]
    [InlineData("amount")]
    [InlineData("case")]
    [InlineData("space")]
    public async Task IncompatibleExistingPaymentsAreNeitherAcceptedNorOverwritten(string kind)
    {
        await using var f = await Fixture.Create();
        var registration = await f.Register();
        var payment = f.Payment(registration);
        Corrupt(payment, kind); f.Db.Add(payment); await f.Db.SaveChangesAsync();
        var before = JsonSerializer.Serialize(await f.Db.Payments.AsNoTracking().SingleAsync());
        await Problem(await f.Cashier.Client.GetAsync(Path(registration.Id)), 409, "paymentDataConflict");
        await Problem(await f.Customer.Client.GetAsync(Path(registration.Id)), 409, "paymentDataConflict");
        await Problem(await f.Cashier.Client.GetAsync(ListPath), 409, "paymentDataConflict");
        await Problem(await f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), new { }), 409, "paymentDataConflict");
        await using var fresh = f.NewDb();
        Assert.Equal(before, JsonSerializer.Serialize(await fresh.Payments.SingleAsync()));
        Assert.False(await fresh.Set<SecurityAudit>().AnyAsync(a => a.Action == "payments.recorded"));
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("overflow")]
    [InlineData("fraction")]
    [InlineData("quantity")]
    [InlineData("empty")]
    [InlineData("registrationStatus")]
    [InlineData("promotion")]
    public async Task InvalidStoredRegistrationMoneyFailsClosed(string kind)
    {
        await using var f = await Fixture.Create();
        var registration = await f.Register("single");
        switch (kind) {
            case "overflow": await f.Db.RegistrationVaccineItems.ExecuteUpdateAsync(s => s.SetProperty(x => x.GiaTaiThoiDiemDangKy, 9999999999999999m)); break;
            case "fraction": await f.Db.RegistrationVaccineItems.ExecuteUpdateAsync(s => s.SetProperty(x => x.GiaTaiThoiDiemDangKy, 1.5m)); break;
            case "quantity": await f.Db.RegistrationVaccineItems.ExecuteUpdateAsync(s => s.SetProperty(x => x.SoLuong, 101)); break;
            case "empty": await f.Db.RegistrationVaccineItems.ExecuteDeleteAsync(); break;
            case "registrationStatus": await f.Db.Registrations.ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, "Legacy")); break;
            case "promotion":
                var promo = new KhuyenMai { TenKhuyenMai = "[DEMO] not snapshotted", ThoiGianBatDau = DateTime.UtcNow, ThoiGianKetThuc = DateTime.UtcNow.AddDays(1), MucUuDai = 100 };
                f.Db.Add(promo); await f.Db.SaveChangesAsync();
                await f.Db.Registrations.ExecuteUpdateAsync(s => s.SetProperty(x => x.MaKhuyenMai, promo.MaKhuyenMai)); break;
        }
        await Problem(await f.Cashier.Client.GetAsync(Path(registration.Id)), 409, "paymentDataConflict");
        await Problem(await f.Cashier.Client.PostAsJsonAsync(Path(registration.Id), new { }), 409, "paymentDataConflict");
        Assert.Empty(await f.Db.Payments.ToArrayAsync());
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("valid")]
    [InlineData("duplicate")]
    [InlineData("status")]
    [InlineData("type")]
    [InlineData("method")]
    [InlineData("amount")]
    [InlineData("case")]
    [InlineData("space")]
    public async Task MigrationPreservesCompatibleDataAndStopsBeforeDdlForConflicts(string kind)
    {
        await using var f = await Fixture.Create();
        var registration = await f.Register();
        var migrator = f.Db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261028172727_G2RegistrationNullableFields");
        var payment = f.Payment(registration);
        if (kind is not ("valid" or "duplicate")) Corrupt(payment, kind);
        f.Db.Add(payment); if (kind == "duplicate") f.Db.Add(f.Payment(registration));
        await f.Db.SaveChangesAsync();
        var before = JsonSerializer.Serialize(await f.Db.Payments.AsNoTracking().OrderBy(p => p.MaThanhToan).ToArrayAsync());
        if (kind == "valid") {
            await migrator.MigrateAsync(); await migrator.MigrateAsync();
            Assert.Equal(1, await UniqueIndex(f.Db));
            f.Db.Add(f.Payment(registration));
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync());
            Assert.IsType<SqlException>(error.InnerException);
            f.Db.ChangeTracker.Clear();
        } else {
            var error = await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync());
            Assert.Contains(kind == "duplicate" ? "duplicate registration payments" : "incompatible legacy payment", error.Message);
            Assert.Equal(0, await UniqueIndex(f.Db));
            Assert.DoesNotContain("20261029000847_G3SinglePayment", await f.Db.Database.GetAppliedMigrationsAsync());
        }
        Assert.Equal(before, JsonSerializer.Serialize(await f.Db.Payments.AsNoTracking().OrderBy(p => p.MaThanhToan).ToArrayAsync()));
        Assert.Equal(registration.Subtotal, (await f.Customer.Client.GetFromJsonAsync<RegistrationDto>("/api/v1/registrations/" + registration.Id))!.Subtotal);
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task OpenApiHasOnlyApprovedPaymentRoutesAndActualDtos()
    {
        await using var f = await Fixture.Create();
        using var json = JsonDocument.Parse(await f.Base.Client.GetStringAsync("/openapi/v1.json"));
        var paths = json.RootElement.GetProperty("paths");
        Assert.Equal(new[] { "get" }, paths.GetProperty(ListPath).EnumerateObject().Select(x => x.Name));
        var detail = paths.GetProperty("/api/v1/registrations/{id}/payment");
        Assert.Equal(new[] { "get", "post" }, detail.EnumerateObject().Select(x => x.Name).Order());
        Assert.DoesNotContain(paths.EnumerateObject(), p => p.Name.StartsWith("/api/v1/payments"));
        var schemas = json.RootElement.GetProperty("components").GetProperty("schemas");
        var request = schemas.GetProperty("RecordPaymentRequest");
        if (request.TryGetProperty("properties", out var requestProperties)) Assert.Empty(requestProperties.EnumerateObject());
        Assert.False(request.GetProperty("additionalProperties").GetBoolean());
        var props = schemas.GetProperty("RegistrationPaymentDto").GetProperty("properties");
        foreach (var field in new[] { "registrationId", "registrationStatus", "customer", "recipient", "items", "subtotal", "totalDue", "paidAmount", "remainingAmount", "requiresPayment", "canRecordPayment", "isSimulation", "payment" })
            Assert.True(props.TryGetProperty(field, out _), field);
        Assert.Contains("RegistrationPaymentDto", detail.GetProperty("post").GetProperty("responses").GetProperty("201").GetRawText());
    }

    private static Task<int> UniqueIndex(TiemChungDbContext db) => db.Database.SqlQueryRaw<int>(
        "SELECT CAST(is_unique AS int) AS Value FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.ThanhToan') AND name='IX_ThanhToan_MaDangKy'").SingleAsync();

    private static void Corrupt(ThanhToan payment, string kind)
    {
        switch (kind) {
            case "status": payment.TrangThai = "DangXuLy"; break;
            case "type": payment.LoaiThanhToan = "DatCoc"; break;
            case "method": payment.PhuongThuc = "TienMat"; break;
            case "amount": payment.SoTien -= 1; break;
            case "case": payment.TrangThai = "thanhcong"; break;
            case "space": payment.PhuongThuc = "MoPhong "; break;
        }
    }

    private static async Task Problem(HttpResponseMessage response, int status, string? code = null)
    {
        using (response) {
            Assert.Equal((HttpStatusCode)status, response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
            var text = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(text);
            if (code is not null) Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
            Assert.True(json.RootElement.TryGetProperty("traceId", out _));
            Assert.DoesNotContain("SqlException", text); Assert.DoesNotContain("ConnectionStrings", text);
        }
    }

    private sealed class PaymentGate : DbCommandInterceptor
    {
        public bool Armed;
        private int _count;
        private Guid _first;
        public TaskCompletionSource Locked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Contending { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData data,
            InterceptionResult<DbDataReader> result, CancellationToken ct = default)
        {
            if (Armed && command.CommandText.Contains("DangKyDichVu WITH (UPDLOCK,HOLDLOCK)")) {
                if (Interlocked.Increment(ref _count) == 1) _first = data.CommandId;
                else Contending.TrySetResult();
            }
            return ValueTask.FromResult(result);
        }
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData data,
            DbDataReader result, CancellationToken ct = default)
        {
            if (Armed && data.CommandId == _first) {
                Locked.TrySetResult(); await Release.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
            }
            return result;
        }
    }

    private sealed record TestActor(TaiKhoan Account, NhanVien? Employee, IdentitySession Session, HttpClient Client);

    private sealed class Fixture : IAsyncDisposable
    {
        public required CatalogSqlTests.Fixture Base { get; init; }
        public TiemChungDbContext Db => Base.Db;
        public required KhachHang CustomerProfile { get; init; }
        public required NguoiDuocTiem Recipient { get; init; }
        public TestActor Customer { get; private set; } = null!;
        public TestActor Cashier { get; private set; } = null!;
        private WebApplicationFactory<Program>? _factory;
        private readonly List<HttpClient> _clients = [];
        public TiemChungDbContext NewDb() => new(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(Db.Database.GetConnectionString()).Options);
        public ThanhToan Payment(RegistrationDto registration) => new() {
            MaDangKy = registration.Id, MaNhanVien = Cashier.Employee!.MaNhanVien, SoTien = registration.Subtotal,
            PhuongThuc = "MoPhong", LoaiThanhToan = "ToanBo", TrangThai = "ThanhCong", ThoiDiem = DateTime.UtcNow
        };
        public async Task<TestActor> Actor(string? title, params string[] permissions)
        {
            var suffix = Guid.NewGuid().ToString("N");
            NhanVien? employee = null;
            if (title is not null) {
                employee = new() { HoTen = "[DEMO] G3 " + title, ChucDanh = title, TrangThai = "Active" };
                Db.Add(employee); await Db.SaveChangesAsync();
            }
            var account = new TaiKhoan {
                AccountType = title is null ? "Customer" : "Employee", MaNhanVien = employee?.MaNhanVien,
                TenDangNhap = "g3-" + suffix, NormalizedUsername = title is null ? null : "G3-" + suffix.ToUpperInvariant(),
                LoginEmail = title is null ? suffix + "@example.invalid" : null,
                NormalizedEmail = title is null ? suffix.ToUpperInvariant() + "@EXAMPLE.INVALID" : null,
                EmailVerifiedAt = DateTimeOffset.UtcNow, TrangThai = "Active", CreatedAt = DateTimeOffset.UtcNow
            };
            account.MatKhauHash = new PasswordHasher<TaiKhoan>().HashPassword(account, Guid.NewGuid().ToString("N"));
            Db.Add(account); await Db.SaveChangesAsync();
            foreach (var code in permissions) Db.Add(new TaiKhoan_QuyenHan {
                MaTaiKhoan = account.MaTaiKhoan, MaQuyen = (await Db.Set<QuyenHan>().SingleAsync(p => p.Code == code)).MaQuyen
            });
            var session = new IdentitySession { Id = Guid.NewGuid(), AccountId = account.MaTaiKhoan,
                SecurityVersion = account.SecurityVersion, CreatedAt = DateTimeOffset.UtcNow, ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) };
            Db.Add(session); await Db.SaveChangesAsync();
            var client = (_factory ?? Base.Factory).CreateClient(); _clients.Add(client);
            client.DefaultRequestHeaders.Authorization = new("Bearer", new IdentityTokens(Base.Config).Issue(account.MaTaiKhoan, session.Id, DateTimeOffset.UtcNow));
            return new(account, employee, session, client);
        }
        public async Task<RegistrationDto> Register(string kind = "mixed")
        {
            var vaccine = await Db.Vaccines.AsNoTracking().OrderBy(v => v.MaVacXin).FirstAsync();
            var package = await Db.Set<GoiVacXin>().AsNoTracking().SingleAsync();
            var components = await Db.Set<GoiVacXin_VacXin>().Select(x => new RegistrationComponentRequest(x.MaVacXin, x.SoLuong)).ToArrayAsync();
            var single = new RegistrationItemRequest("vaccine", vaccine.MaVacXin, 2, vaccine.Gia);
            var bundled = new RegistrationItemRequest("package", package.MaGoi, 2, package.GiaGoi, components);
            var request = new CreateRegistrationRequest(CustomerProfile.MaKhachHang, Recipient.MaNguoiDuocTiem,
                kind == "single" ? [single] : kind == "package" ? [bundled] : [single, bundled]);
            using var response = await Customer.Client.PostAsJsonAsync("/api/v1/registrations", request);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<RegistrationDto>())!;
        }
        public static async Task<Fixture> Create(PaymentGate? gate = null)
        {
            var b = await CatalogSqlTests.Fixture.Create(); await b.Login();
            await DevelopmentCatalogSeed.RunAsync(b.Db, new SeedEnvironment());
            var customer = new KhachHang { HoTen = "[DEMO] G3 Customer", Email = "g3@example.invalid" };
            b.Db.Add(customer); await b.Db.SaveChangesAsync();
            var recipient = new NguoiDuocTiem { HoTen = "[DEMO] G3 Recipient", MaKhachHang = customer.MaKhachHang,
                NgaySinh = new(2020, 1, 1), GioiTinh = "Khác", QuanHeVoiKhachHang = "Con" };
            b.Db.Add(recipient); await b.Db.SaveChangesAsync();
            var f = new Fixture { Base = b, CustomerProfile = customer, Recipient = recipient };
            if (gate is not null) f._factory = b.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services => {
                services.RemoveAll<DbContextOptions<TiemChungDbContext>>(); services.RemoveAll<TiemChungDbContext>();
                services.AddDbContext<TiemChungDbContext>(o => o.UseSqlServer(b.Db.Database.GetConnectionString()).AddInterceptors(gate));
            }));
            f.Customer = await f.Actor(null); f.Cashier = await f.Actor("ThuNgan", "XemThanhToan", "GhiThanhToan");
            b.Db.AddRange(new AccountProfileLink { AccountId = f.Customer.Account.MaTaiKhoan, CustomerId = customer.MaKhachHang },
                new AccountProfileLink { AccountId = f.Customer.Account.MaTaiKhoan, RecipientId = recipient.MaNguoiDuocTiem });
            await b.Db.SaveChangesAsync(); return f;
        }
        public async ValueTask DisposeAsync()
        {
            foreach (var client in _clients) client.Dispose();
            if (_factory is not null) await _factory.DisposeAsync();
            await Base.DisposeAsync();
        }
    }

    private sealed class SeedEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "G3Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
