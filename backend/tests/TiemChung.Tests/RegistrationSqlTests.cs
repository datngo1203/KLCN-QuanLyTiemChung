using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
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

public sealed class RegistrationSqlTests
{
    private const string Route = "/api/v1/registrations";

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task SinglesPackagesAndMixedPersistSnapshotsAndAuditAndAllowPriceUpdates()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
        foreach (var kind in new[] { "single", "package", "mixed" }) {
            var request = f.Request(kind);
            using var response = await f.Customer.PostAsJsonAsync(Route, request);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var saved = (await response.Content.ReadFromJsonAsync<RegistrationDto>())!;
            Assert.Equal(Route + "/" + saved.Id, response.Headers.Location!.OriginalString);
            Assert.Equal("DaDangKy", saved.Status); Assert.Null(saved.EmployeeId);
            Assert.Equal("VND", saved.Currency); Assert.Equal(TimeSpan.Zero, saved.CreatedAt.Offset);
            Assert.Equal(request.Items.Sum(x => x.Quantity * x.ExpectedUnitPrice), saved.Subtotal);
            Assert.All(saved.Items, x => Assert.Equal(x.Quantity * x.UnitPrice, x.LineTotal));
            var row = await f.Db.Registrations.AsNoTracking().SingleAsync(x => x.MaDangKy == saved.Id);
            Assert.Null(row.HinhThucTiem); Assert.Null(row.ThoiGianDuKien); Assert.Null(row.MaKhuyenMai);
            var audit = await f.Db.Set<SecurityAudit>().AsNoTracking().SingleAsync(x => x.Action == "registrations.created" && x.ResourceId == saved.Id.ToString());
            Assert.Equal(f.CustomerAccount.MaTaiKhoan, audit.ActorAccountId);
            Assert.NotNull(audit.SessionId); Assert.Equal("Success", audit.Outcome);
            Assert.InRange(audit.OccurredAt, saved.CreatedAt, saved.CreatedAt.AddSeconds(30));
            var reloaded = (await f.Reception.GetFromJsonAsync<RegistrationDto>(Route + "/" + saved.Id))!;
            Assert.Equal(JsonSerializer.Serialize(saved), JsonSerializer.Serialize(reloaded));
        }
        var previous = (await f.Customer.GetFromJsonAsync<RegistrationListResponse>(Route))!.Items;
        Assert.Equal(3, previous.Length);
        Assert.True(previous.Select(x => x.Id).SequenceEqual(previous.Select(x => x.Id).OrderDescending()));
        using var page = await f.Customer.GetAsync(Route + "?page=2&pageSize=1");
        Assert.Equal(previous[1].Id, Assert.Single((await page.Content.ReadFromJsonAsync<RegistrationListResponse>())!.Items).Id);
        using var packageUpdate = await f.Admin.PutAsJsonAsync(f.PackagePath, f.PackageRequest(999000));
        Assert.Equal(HttpStatusCode.OK, packageUpdate.StatusCode);
        var vaccine = f.Vaccines[0];
        using var vaccineUpdate = await f.Admin.PutAsJsonAsync("/api/v1/catalog/vaccines/" + vaccine.MaVacXin,
            new VaccineRequest(vaccine.TenVacXin, vaccine.NhaSanXuat, vaccine.XuatXu, 777000, vaccine.MaLoaiVacXin, vaccine.MaLoaiTiemChung));
        Assert.Equal(HttpStatusCode.OK, vaccineUpdate.StatusCode);
        foreach (var old in previous) {
            var current = (await f.Customer.GetFromJsonAsync<RegistrationDto>(Route + "/" + old.Id))!;
            Assert.Equal(old.Subtotal, current.Subtotal);
            Assert.All(current.Items, x => Assert.Equal(x.Kind == "package" ? f.Package.GiaGoi : vaccine.Gia, x.UnitPrice));
        }
        using var stale = await f.Customer.PostAsJsonAsync(Route, f.Request());
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("catalogChanged", await Code(stale));
        var updated = f.Request() with { Items = [new("vaccine", vaccine.MaVacXin, 1, 777000),
            new("package", f.Package.MaGoi, 2, 999000, f.Components)] };
        using var createdByReception = await f.Reception.PostAsJsonAsync(Route, updated);
        Assert.Equal(HttpStatusCode.Created, createdByReception.StatusCode);
        var receptionRecord = (await createdByReception.Content.ReadFromJsonAsync<RegistrationDto>())!;
        Assert.Equal(f.ReceptionEmployeeId, receptionRecord.EmployeeId);
        Assert.Equal(2775000m, receptionRecord.Subtotal);
        Assert.Equal(HttpStatusCode.OK, (await f.Customer.GetAsync(Route + "/" + receptionRecord.Id)).StatusCode);
        var changed = f.PackageRequest(999000) with { Items = [new(f.Vaccines[0].MaVacXin, 2)] };
        using var denied = await f.Admin.PutAsJsonAsync(f.PackagePath, changed);
        Assert.Equal(HttpStatusCode.Conflict, denied.StatusCode); Assert.Equal("catalogReferenced", await Code(denied));
        Assert.Equal(f.Components.Length, await f.Db.Set<GoiVacXin_VacXin>().CountAsync());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task OwnershipRequiresBothCurrentLinksAndCorrectPairAndNeverLeaksOtherProfiles()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
        using var created = await f.Customer.PostAsJsonAsync(Route, f.Request());
        var saved = (await created.Content.ReadFromJsonAsync<RegistrationDto>())!;
        var links = await f.Db.Set<AccountProfileLink>().Where(x => x.AccountId == f.CustomerAccount.MaTaiKhoan).ToArrayAsync();
        foreach (var link in links) {
            link.State = "Revoked"; link.RevokedAt = DateTimeOffset.UtcNow; await f.Db.SaveChangesAsync();
            Assert.Empty((await f.Customer.GetFromJsonAsync<RegistrationListResponse>(Route))!.Items);
            Assert.Equal(HttpStatusCode.Forbidden, (await f.Customer.GetAsync(Route + "/" + saved.Id)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await f.Customer.PostAsJsonAsync(Route, f.Request())).StatusCode);
            link.State = "Active"; link.RevokedAt = null; await f.Db.SaveChangesAsync();
        }
        using var stranger = await f.Actor();
        var forbidden = await stranger.GetAsync(Route + "/" + saved.Id);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.DoesNotContain(f.CustomerProfile.HoTen, await forbidden.Content.ReadAsStringAsync());
        Assert.Empty((await stranger.GetFromJsonAsync<RegistrationListResponse>(Route + "?customerId=" + f.CustomerProfile.MaKhachHang))!.Items);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.PostAsJsonAsync(Route, f.Request())).StatusCode);
        var other = new KhachHang { HoTen = "[DEMO] Other G2", SoDienThoai = "0900000000", Email = "other-g2@example.invalid" };
        f.Db.Add(other); await f.Db.SaveChangesAsync();
        var wrongPair = f.Request() with { CustomerId = other.MaKhachHang };
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Reception.PostAsJsonAsync(Route, wrongPair)).StatusCode);
        // Both links exist, but a mismatched recipient/customer must still be refused.
        f.Recipient.MaKhachHang = other.MaKhachHang; await f.Db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Customer.PostAsJsonAsync(Route, f.Request())).StatusCode);
        Assert.Empty((await f.Customer.GetFromJsonAsync<RegistrationListResponse>(Route))!.Items);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Customer.GetAsync(Route + "/" + saved.Id)).StatusCode);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task EveryEndpointChecksSessionRoleAndCurrentEmployeeState()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
        async Task Denied(HttpClient client, HttpStatusCode expected) {
            Assert.Equal(expected, (await client.GetAsync(Route)).StatusCode);
            Assert.Equal(expected, (await client.GetAsync(Route + "/1")).StatusCode);
            Assert.Equal(expected, (await client.PostAsJsonAsync(Route, f.Request())).StatusCode);
        }
        using var anonymous = f.Base.Factory.CreateClient();
        await Denied(anonymous, HttpStatusCode.Unauthorized);
        await Denied(f.Admin, HttpStatusCode.Forbidden);
        foreach (var title in new[] { "BacSi", "DieuDuong", "ThuNgan", "ThuKho" }) {
            using var employee = await f.Actor(title);
            await Denied(employee, HttpStatusCode.Forbidden);
        }
        await f.Db.Set<NhanVien>().Where(x => x.MaNhanVien == f.ReceptionEmployeeId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ChucDanh, "ThuNgan"));
        await Denied(f.Reception, HttpStatusCode.Forbidden);
        await f.Db.Set<NhanVien>().Where(x => x.MaNhanVien == f.ReceptionEmployeeId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, "Inactive"));
        await Denied(f.Reception, HttpStatusCode.Unauthorized);
        await f.Db.Set<TaiKhoan>().Where(x => x.MaTaiKhoan == f.CustomerAccount.MaTaiKhoan)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.EmailVerifiedAt, (DateTimeOffset?)null));
        await Denied(f.Customer, HttpStatusCode.Forbidden);
        await f.Db.Set<TaiKhoan>().Where(x => x.MaTaiKhoan == f.CustomerAccount.MaTaiKhoan)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.EmailVerifiedAt, DateTimeOffset.UtcNow));
        await f.Db.Set<IdentitySession>().Where(x => x.AccountId == f.CustomerAccount.MaTaiKhoan)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow));
        await Denied(f.Customer, HttpStatusCode.Unauthorized);
        Assert.Empty(await f.Db.Registrations.ToArrayAsync());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task ValidationUnknownFieldsMissingIdsAndStaleCatalogLeaveNoPartialRegistration()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
        var valid = f.Request(); var single = valid.Items[0]; var package = valid.Items[1];
        var bad = new[] {
            valid with { CustomerId = 0 }, valid with { RecipientId = -1 }, valid with { Items = [] }, valid with { Items = null! },
            valid with { Items = [null!] }, valid with { Items = [single, single] },
            valid with { Items = [single with { Kind = "other" }] }, valid with { Items = [single with { ServiceId = 0 }] },
            valid with { Items = [single with { Quantity = 0 }] }, valid with { Items = [single with { Quantity = 101 }] },
            valid with { Items = [single with { ExpectedUnitPrice = -1 }] }, valid with { Items = [single with { ExpectedUnitPrice = 1.5m }] },
            valid with { Items = [single with { ExpectedUnitPrice = 10000000000000000m }] },
            valid with { Items = [single with { ExpectedComponents = [] }] },
            valid with { Items = [package with { ExpectedComponents = null }] },
            valid with { Items = [package with { ExpectedComponents = [] }] },
            valid with { Items = [package with { ExpectedComponents = [f.Components[0], f.Components[0]] }] },
            valid with { Items = [package with { ExpectedComponents = [new(1, 0)] }] },
            valid with { Items = Enumerable.Repeat(single, 101).ToArray() }
        };
        foreach (var req in bad) {
            using var response = await f.Customer.PostAsJsonAsync(Route, req);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal("validation", await Code(response));
        }
        foreach (var json in new[] {
            "{", "null", "{\"customerId\":1,\"recipientId\":1,\"items\":[{\"kind\":\"vaccine\",\"serviceId\":1,\"quantity\":1}]}",
            JsonSerializer.Serialize(valid, JsonOptions).TrimEnd('}') + ",\"employeeId\":1}",
            JsonSerializer.Serialize(valid, JsonOptions).Replace("\"expectedUnitPrice\":150000", "\"expectedUnitPrice\":150000,\"status\":\"Paid\"")
        }) {
            using var response = await f.Customer.PostAsync(Route, new StringContent(json, Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal("validation", await Code(response));
        }
        foreach (var req in new[] { valid with { Items = [single, package with { ServiceId = int.MaxValue }] },
            valid with { Items = [single with { ServiceId = int.MaxValue }] } })
            Assert.Equal(HttpStatusCode.NotFound, (await f.Customer.PostAsJsonAsync(Route, req)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await f.Reception.PostAsJsonAsync(Route, valid with { RecipientId = int.MaxValue })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await f.Reception.PostAsJsonAsync(Route, valid with { CustomerId = int.MaxValue })).StatusCode);
        foreach (var req in new[] { valid with { Items = [single with { ExpectedUnitPrice = 1 }] },
            valid with { Items = [package with { ExpectedComponents = [new(f.Components[0].VaccineId, 2)] }] } }) {
            using var response = await f.Customer.PostAsJsonAsync(Route, req);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); Assert.Equal("catalogChanged", await Code(response));
        }
        foreach (var query in new[] { "?page=0", "?pageSize=101", "?page=2147483647&pageSize=100", "?recipientId=-1", "?customerId=0" })
            Assert.Equal(HttpStatusCode.BadRequest, (await f.Customer.GetAsync(Route + query)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Customer.GetAsync(Route + "/0")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await f.Reception.GetAsync(Route + "/2147483647")).StatusCode);
        Assert.Empty(await f.Db.Registrations.ToArrayAsync());
        Assert.Empty(await f.Db.RegistrationVaccineItems.ToArrayAsync()); Assert.Empty(await f.Db.RegistrationPackageItems.ToArrayAsync());
        Assert.False(await f.Db.Set<SecurityAudit>().AnyAsync(x => x.Action == "registrations.created"));
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task InactiveInvalidPackagesAndMoneyOverflowAreRejectedButZeroPriceIsValid()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
        await f.Db.Vaccines.Where(x => x.MaVacXin == f.Vaccines[0].MaVacXin).ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, "Inactive"));
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Customer.PostAsJsonAsync(Route, f.Request())).StatusCode);
        await f.Db.Vaccines.ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, "Active"));
        await f.Db.Set<GoiVacXin>().ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, "Inactive"));
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Customer.PostAsJsonAsync(Route, f.Request("package"))).StatusCode);
        await f.Db.Set<GoiVacXin>().ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, "Active"));
        await f.Db.Set<GoiVacXin_VacXin>().ExecuteDeleteAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Customer.PostAsJsonAsync(Route, f.Request("package"))).StatusCode);
        await f.Db.Vaccines.ExecuteUpdateAsync(s => s.SetProperty(x => x.Gia, 9999999999999999m));
        var high = f.Request("single"); high = high with { Items = [high.Items[0] with { ExpectedUnitPrice = 9999999999999999m }] };
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Customer.PostAsJsonAsync(Route, high)).StatusCode);
        await f.Db.Vaccines.ExecuteUpdateAsync(s => s.SetProperty(x => x.Gia, 0m));
        var zero = high with { Items = [high.Items[0] with { ExpectedUnitPrice = 0 }] };
        using var response = await f.Customer.PostAsJsonAsync(Route, zero);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(0, (await response.Content.ReadFromJsonAsync<RegistrationDto>())!.Subtotal);
        Assert.Empty(await f.Db.Payments.ToArrayAsync());
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("ChiTietGoiVacXin")]
    [InlineData("SecurityAudit")]
    public async Task WriteFailureRollsBackHeaderBothKindsAndAudit(string table)
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
        var sql = table == "SecurityAudit"
            ? "CREATE TRIGGER G2FixtureFailure ON dbo.SecurityAudit AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE Action='registrations.created') THROW 51000, 'Synthetic G2 failure', 1;"
            : "CREATE TRIGGER G2FixtureFailure ON dbo.ChiTietGoiVacXin AFTER INSERT AS THROW 51000, 'Synthetic G2 failure', 1;";
        await f.Db.Database.ExecuteSqlRawAsync(sql);
        using var failed = await f.Customer.PostAsJsonAsync(Route, f.Request());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode); Assert.Equal("dependencyUnavailable", await Code(failed));
        Assert.Empty(await f.Db.Registrations.AsNoTracking().ToArrayAsync());
        Assert.Empty(await f.Db.RegistrationVaccineItems.AsNoTracking().ToArrayAsync());
        Assert.Empty(await f.Db.RegistrationPackageItems.AsNoTracking().ToArrayAsync());
        Assert.False(await f.Db.Set<SecurityAudit>().AnyAsync(x => x.Action == "registrations.created"));
        await f.Db.Database.ExecuteSqlRawAsync("DROP TRIGGER dbo.G2FixtureFailure;");
        Assert.Equal(HttpStatusCode.Created, (await f.Customer.PostAsJsonAsync(Route, f.Request())).StatusCode);
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task PackageCreationAndCatalogUpdatesSerializeInBothOrders(bool registrationFirst, bool changeComponents)
    {
        var gate = new PackageGate();
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(gate), async f => {
        var change = f.PackageRequest(changeComponents ? f.Package.GiaGoi : 900000);
        if (changeComponents) change = change with { Items = [new(f.Vaccines[0].MaVacXin, 2)] };
        Task<HttpResponseMessage> Register() => f.Customer.PostAsJsonAsync(Route, f.Request("package"));
        Task<HttpResponseMessage> Update() => f.Admin.PutAsJsonAsync(f.PackagePath, change);
        gate.Arm();
        var first = registrationFirst ? Register() : Update();
        await gate.Locked.Task.WaitAsync(TimeSpan.FromSeconds(20));
        var second = registrationFirst ? Update() : Register();
        try {
            await gate.Contending.Task.WaitAsync(TimeSpan.FromSeconds(20));
            Assert.False(second.IsCompleted);
        } finally { gate.Release.TrySetResult(); }
        using var a = await first; using var b = await second;
        Assert.Equal(registrationFirst ? HttpStatusCode.Created : HttpStatusCode.OK, a.StatusCode);
        Assert.Equal(registrationFirst && !changeComponents ? HttpStatusCode.OK : HttpStatusCode.Conflict, b.StatusCode);
        Assert.Equal(registrationFirst ? 1 : 0, await f.Db.Registrations.CountAsync());
        if (registrationFirst) {
            var dto = (await a.Content.ReadFromJsonAsync<RegistrationDto>())!;
            Assert.Equal(f.Package.GiaGoi, Assert.Single(dto.Items).UnitPrice);
            Assert.Equal(f.Components.Length, Assert.Single(dto.Items).Components!.Length);
        } else Assert.Equal("catalogChanged", await Code(b));
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task MigrationUpgradesExistingRowsWithoutRewritingThemAndIsRepeatable()
    {
        // Fixture owns a fresh random test DB. Only this test rolls it back before inserting its legacy row.
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f => {
        var migrator = f.Db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261025141042_S3NullableCustomerIdentity");
        var customer = new KhachHang { HoTen = "[DEMO] Before G2", Email = "upgrade-g2@example.invalid", SoDienThoai = "0900000000" };
        f.Db.Add(customer); await f.Db.SaveChangesAsync();
        var recipient = new NguoiDuocTiem { MaKhachHang = customer.MaKhachHang, HoTen = "[DEMO] Before G2", NgaySinh = new(2020, 1, 1) };
        f.Db.Add(recipient); await f.Db.SaveChangesAsync();
        var row = new DangKyDichVu { MaKhachHang = customer.MaKhachHang, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem,
            MaNhanVien = (await f.Db.Set<NhanVien>().SingleAsync()).MaNhanVien, NgayDangKy = DateTime.UtcNow,
            HinhThucTiem = "Legacy fixture", ThoiGianDuKien = DateTime.UtcNow.AddDays(1), TrangThai = "LegacyFixture" };
        f.Db.Add(row); await f.Db.SaveChangesAsync();
        await DevelopmentCatalogSeed.RunAsync(f.Db, new EnvironmentStub());
        f.Db.RegistrationVaccineItems.Add(new() { MaDangKy = row.MaDangKy, MaVacXin = (await f.Db.Vaccines.FirstAsync()).MaVacXin,
            SoLuong = 3, GiaTaiThoiDiemDangKy = 120000 });
        f.Db.RegistrationPackageItems.Add(new() { MaDangKy = row.MaDangKy, MaGoi = (await f.Db.Set<GoiVacXin>().SingleAsync()).MaGoi,
            SoLuongGoi = 2, GiaTaiThoiDiemDangKy = 300000 });
        await f.Db.SaveChangesAsync();
        var before = JsonSerializer.Serialize(await f.Db.Registrations.AsNoTracking().SingleAsync());
        var beforeSingles = JsonSerializer.Serialize(await f.Db.RegistrationVaccineItems.AsNoTracking().ToArrayAsync());
        var beforePackages = JsonSerializer.Serialize(await f.Db.RegistrationPackageItems.AsNoTracking().ToArrayAsync());
        await f.Db.Database.MigrateAsync(); await f.Db.Database.MigrateAsync();
        Assert.Equal(before, JsonSerializer.Serialize(await f.Db.Registrations.AsNoTracking().SingleAsync()));
        Assert.Equal(beforeSingles, JsonSerializer.Serialize(await f.Db.RegistrationVaccineItems.AsNoTracking().ToArrayAsync()));
        Assert.Equal(beforePackages, JsonSerializer.Serialize(await f.Db.RegistrationPackageItems.AsNoTracking().ToArrayAsync()));
        f.Db.Registrations.Add(new() { MaKhachHang = customer.MaKhachHang, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem,
            NgayDangKy = DateTime.UtcNow, TrangThai = "DaDangKy" });
        await f.Db.SaveChangesAsync();
        Assert.Equal(2, await f.Db.Registrations.CountAsync());
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => migrator.MigrateAsync("20261025141042_S3NullableCustomerIdentity"));
        Assert.Equal(2, await f.Db.Registrations.CountAsync());
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SingleVaccinePriceRaceUsesOnlyCommittedServerPrice(bool registrationFirst)
    {
        var gate = new PackageGate(registrationFirst ? "FROM [VacXin]" : "UPDATE [VacXin]",
            registrationFirst ? "UPDATE [VacXin]" : "FROM [VacXin]");
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(gate), async f => {
        var vaccine = f.Vaccines[0];
        Task<HttpResponseMessage> Register() => f.Customer.PostAsJsonAsync(Route, f.Request("single"));
        Task<HttpResponseMessage> Update() => f.Admin.PutAsJsonAsync("/api/v1/catalog/vaccines/" + vaccine.MaVacXin,
            new VaccineRequest(vaccine.TenVacXin, vaccine.NhaSanXuat, vaccine.XuatXu, 800000, vaccine.MaLoaiVacXin, vaccine.MaLoaiTiemChung));
        gate.Arm();
        var first = registrationFirst ? Register() : Update();
        await gate.Locked.Task.WaitAsync(TimeSpan.FromSeconds(20));
        var second = registrationFirst ? Update() : Register();
        try {
            await gate.Contending.Task.WaitAsync(TimeSpan.FromSeconds(20));
            Assert.False(second.IsCompleted);
        } finally { gate.Release.TrySetResult(); }
        using var a = await first; using var b = await second;
        Assert.Equal(registrationFirst ? HttpStatusCode.Created : HttpStatusCode.OK, a.StatusCode);
        Assert.Equal(registrationFirst ? HttpStatusCode.OK : HttpStatusCode.Conflict, b.StatusCode);
        if (registrationFirst) Assert.Equal(vaccine.Gia,
            Assert.Single((await a.Content.ReadFromJsonAsync<RegistrationDto>())!.Items).UnitPrice);
        else Assert.Equal("catalogChanged", await Code(b));
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task OpenApiDescribesActualRegistrationDtoAndOnlyThreeOperations()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
        using var json = JsonDocument.Parse(await f.Customer.GetStringAsync("/openapi/v1.json"));
        var paths = json.RootElement.GetProperty("paths");
        Assert.Equal(new[] { "get", "post" }, paths.GetProperty(Route).EnumerateObject().Select(x => x.Name).Order().ToArray());
        Assert.Single(paths.GetProperty(Route + "/{id}").EnumerateObject());
        var schemas = json.RootElement.GetProperty("components").GetProperty("schemas");
        var request = schemas.GetProperty("CreateRegistrationRequest").GetProperty("properties");
        Assert.Equal(new[] { "customerId", "items", "recipientId" }, request.EnumerateObject().Select(x => x.Name).Order().ToArray());
        Assert.Contains("expectedUnitPrice", schemas.GetProperty("RegistrationItemRequest").GetProperty("required").EnumerateArray().Select(x => x.GetString()));
        Assert.True(schemas.GetProperty("RegistrationDto").GetProperty("properties").TryGetProperty("subtotal", out _));
        });
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static async Task<string?> Code(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();

    private sealed class PackageGate(string firstPattern = "GoiVacXin WITH (UPDLOCK,HOLDLOCK)", string? contendingPattern = null) : DbCommandInterceptor
    {
        private int _count; private volatile bool _armed;
        private Guid _firstCommand;
        public TaskCompletionSource Locked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Contending { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Arm() => _armed = true;
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData data,
            InterceptionResult<DbDataReader> result, CancellationToken ct = default)
        {
            if (_armed) {
                if (command.CommandText.Contains(firstPattern) && Interlocked.CompareExchange(ref _count, 1, 0) == 0)
                    _firstCommand = data.CommandId;
                else if (Volatile.Read(ref _count) > 0 && command.CommandText.Contains(contendingPattern ?? firstPattern))
                    Contending.TrySetResult();
            }
            return ValueTask.FromResult(result);
        }
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData data,
            DbDataReader result, CancellationToken ct = default)
        {
            if (_armed && data.CommandId == _firstCommand) {
                Locked.TrySetResult(); await Release.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
            }
            return result;
        }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public required CatalogSqlTests.Fixture Base { get; init; }
        public TiemChungDbContext Db => Base.Db;
        public required HttpClient Customer { get; set; }
        public required HttpClient Reception { get; set; }
        public HttpClient Admin => _admin ?? Base.Client;
        public required TaiKhoan CustomerAccount { get; set; }
        public required KhachHang CustomerProfile { get; init; }
        public required NguoiDuocTiem Recipient { get; init; }
        public required VacXin[] Vaccines { get; init; }
        public required GoiVacXin Package { get; init; }
        public required RegistrationComponentRequest[] Components { get; init; }
        public int ReceptionEmployeeId { get; set; }
        private WebApplicationFactory<Program>? _factory;
        private HttpClient? _admin;
        private readonly List<HttpClient> _clients = [];
        public string PackagePath => "/api/v1/catalog/packages/" + Package.MaGoi;
        public PackageRequest PackageRequest(decimal price) => new(Package.TenGoi, price, Components.Select(x => new PackageItemDto(x.VaccineId, x.Quantity)).ToArray());
        public CreateRegistrationRequest Request(string kind = "mixed") {
            var single = new RegistrationItemRequest("vaccine", Vaccines[0].MaVacXin, 2, Vaccines[0].Gia);
            var package = new RegistrationItemRequest("package", Package.MaGoi, 2, Package.GiaGoi, Components.Reverse().ToArray());
            return new(CustomerProfile.MaKhachHang, Recipient.MaNguoiDuocTiem, kind == "single" ? [single] : kind == "package" ? [package] : [single, package]);
        }
        public async Task<HttpClient> Actor(string? title = null) {
            var suffix = Guid.NewGuid().ToString("N");
            NhanVien? employee = null;
            if (title != null) {
                employee = new() { HoTen = "[DEMO] G2 " + title, CCCD = "000000000000", ChucDanh = title, TrangThai = "Active" };
                Db.Add(employee); await Db.SaveChangesAsync();
            }
            var account = new TaiKhoan { AccountType = title == null ? "Customer" : "Employee", MaNhanVien = employee?.MaNhanVien,
                TenDangNhap = "g2-" + suffix, NormalizedUsername = title == null ? null : "G2-" + suffix.ToUpperInvariant(),
                LoginEmail = title == null ? suffix + "@example.invalid" : null, NormalizedEmail = title == null ? suffix.ToUpperInvariant() + "@EXAMPLE.INVALID" : null,
                EmailVerifiedAt = DateTimeOffset.UtcNow, TrangThai = "Active", CreatedAt = DateTimeOffset.UtcNow };
            account.MatKhauHash = new PasswordHasher<TaiKhoan>().HashPassword(account, Guid.NewGuid().ToString("N"));
            Db.Add(account); await Db.SaveChangesAsync();
            var session = new IdentitySession { Id = Guid.NewGuid(), AccountId = account.MaTaiKhoan, SecurityVersion = 1,
                CreatedAt = DateTimeOffset.UtcNow, ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) };
            Db.Add(session); await Db.SaveChangesAsync();
            var client = (_factory ?? Base.Factory).CreateClient(); _clients.Add(client);
            client.DefaultRequestHeaders.Authorization = new("Bearer", new IdentityTokens(Base.Config).Issue(account.MaTaiKhoan, session.Id, DateTimeOffset.UtcNow));
            if (title == "LeTan") ReceptionEmployeeId = employee!.MaNhanVien;
            if (CustomerAccount is null) CustomerAccount = account;
            return client;
        }
        public static async Task<Fixture> Create(PackageGate? gate = null) {
            var b = await CatalogSqlTests.Fixture.Create();
            Fixture? partial = null;
            try {
            await b.Login();
            await DevelopmentCatalogSeed.RunAsync(b.Db, new EnvironmentStub());
            var customer = new KhachHang { HoTen = "[DEMO] G2 Customer", SoDienThoai = "0900000000", Email = "g2@example.invalid" };
            b.Db.Add(customer); await b.Db.SaveChangesAsync();
            var recipient = new NguoiDuocTiem { MaKhachHang = customer.MaKhachHang, HoTen = "[DEMO] G2 Recipient", NgaySinh = new(2020, 1, 1), GioiTinh = "Khác", QuanHeVoiKhachHang = "Con" };
            b.Db.Add(recipient); await b.Db.SaveChangesAsync();
            var package = await b.Db.Set<GoiVacXin>().SingleAsync();
            var f = new Fixture { Base = b, Customer = null!, Reception = null!, CustomerAccount = null!, CustomerProfile = customer,
                Recipient = recipient, Package = package, Vaccines = await b.Db.Vaccines.OrderBy(x => x.MaVacXin).ToArrayAsync(),
                Components = await b.Db.Set<GoiVacXin_VacXin>().Select(x => new RegistrationComponentRequest(x.MaVacXin, x.SoLuong)).ToArrayAsync() };
            partial = f;
            if (gate != null) {
                f._factory = b.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services => {
                    services.RemoveAll<DbContextOptions<TiemChungDbContext>>(); services.RemoveAll<TiemChungDbContext>();
                    services.AddDbContext<TiemChungDbContext>(o => o.UseSqlServer(b.Db.Database.GetConnectionString()).AddInterceptors(gate));
                }));
                // Use the intercepted factory for both competing HTTP requests, with distinct accounts.
                f._admin = f._factory.CreateClient();
                f._admin.DefaultRequestHeaders.Authorization = b.Client.DefaultRequestHeaders.Authorization;
            }
            f.Customer = await f.Actor(); f.Reception = await f.Actor("LeTan");
            b.Db.AddRange(new AccountProfileLink { AccountId = f.CustomerAccount.MaTaiKhoan, CustomerId = customer.MaKhachHang, CreatedAt = DateTimeOffset.UtcNow },
                new AccountProfileLink { AccountId = f.CustomerAccount.MaTaiKhoan, RecipientId = recipient.MaNguoiDuocTiem, CreatedAt = DateTimeOffset.UtcNow });
            await b.Db.SaveChangesAsync();
            return f;
            } catch (Exception failure) {
                await SqlFixtureLifecycle.CleanupAfterFailureAsync(failure, () => partial is null ? b.DisposeAsync() : partial.DisposeAsync());
                throw;
            }
        }
        public ValueTask DisposeAsync() => SqlFixtureLifecycle.CleanupAsync(
            () => SqlFixtureLifecycle.CleanupAsync(_clients.Select<HttpClient, Func<ValueTask>>(client => () => {
                client.Dispose(); return ValueTask.CompletedTask;
            }).ToArray()),
            () => { _admin?.Dispose(); return ValueTask.CompletedTask; },
            () => _factory?.DisposeAsync() ?? ValueTask.CompletedTask,
            Base.DisposeAsync);
    }

    private sealed class EnvironmentStub : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "G2Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
