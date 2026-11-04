using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using Xunit;

namespace TiemChung.Tests;

public sealed class RF03ProfilesSqlTests
{
    private static readonly DateOnly Adult = new(1990, 1, 1);
    private static readonly DateOnly Child = new(2015, 1, 1);
    private const string Phone = "0900000000";
    private const string CustomerClaim = "/api/v1/customers/claim";
    private const string RecipientClaim = "/api/v1/recipients/claim";

    [Fact, Trait("Category", "SqlIntegration")]
    public Task CustomerFieldsSurviveRoundTripAndLegacyMigrationIsReversible() =>
        SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            var migrator = f.Db.GetService<IMigrator>();
            const string previous = "20261103220655_RF02B15CatalogSemantics";
            await migrator.MigrateAsync(previous);
            Assert.Equal(0, await BirthColumns(f));
            await f.Db.Database.ExecuteSqlRawAsync("""
                INSERT KhachHang(HoTen, SoDienThoai, Email)
                VALUES(N'[DEMO] Legacy RF03', '0900000000', 'legacy@example.invalid')
                """);
            await migrator.MigrateAsync();
            await migrator.MigrateAsync();
            Assert.Equal(2, await BirthColumns(f));
            Assert.False(f.Db.Database.HasPendingModelChanges());

            await f.Db.Set<NhanVien>().ExecuteUpdateAsync(s => s.SetProperty(e => e.ChucDanh, "LeTan"));
            await f.Login();
            var legacy = Assert.Single((await f.Client.GetFromJsonAsync<CustomerListResponse>("/api/v1/customers"))!.Items);
            Assert.Null(legacy.Salutation);
            Assert.Null(legacy.DateOfBirth);
            var route = $"/api/v1/customers/{legacy.Id}";
            var create = new CreateCustomerRequest(legacy.FullName, null, Phone, legacy.Email, "Chị", Adult);

            using var missing = await f.Client.PutAsJsonAsync(route, new { legacy.FullName, phoneNumber = Phone, legacy.Email });
            Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
            using var invalidTitle = await f.Client.PutAsJsonAsync(route, create with { Salutation = "Cô" });
            Assert.Equal(HttpStatusCode.BadRequest, invalidTitle.StatusCode);
            Assert.Equal("validation", await Code(invalidTitle));
            var vietnamToday = ProfileRules.Today(DateTimeOffset.UtcNow);
            using var future = await f.Client.PostAsJsonAsync("/api/v1/customers", create with { DateOfBirth = vietnamToday.AddDays(1) });
            Assert.Equal(HttpStatusCode.BadRequest, future.StatusCode);
            using var updated = await f.Client.PutAsJsonAsync(route, create);
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            using var created = await f.Client.PostAsJsonAsync("/api/v1/customers", create with { DateOfBirth = vietnamToday });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            Assert.Equal(vietnamToday, (await created.Content.ReadFromJsonAsync<CustomerDto>())!.DateOfBirth);
            var row = await f.Db.Customers.AsNoTracking().SingleAsync(c => c.MaKhachHang == legacy.Id);
            Assert.Equal("Chị", row.DanhXung);
            Assert.Equal(Adult, row.NgaySinh);

            await migrator.MigrateAsync(previous);
            Assert.Equal(0, await BirthColumns(f));
            await migrator.MigrateAsync();
            var afterRollback = await f.Db.Customers.AsNoTracking().SingleAsync(c => c.MaKhachHang == legacy.Id);
            Assert.Equal(legacy.FullName, afterRollback.HoTen);
            Assert.Null(afterRollback.NgaySinh);
            Assert.Null(afterRollback.DanhXung);
        });

    [Fact, Trait("Category", "SqlIntegration")]
    public Task RecipientRelationIsValidatedOnCreateAndEditAndCustomerDateCannotBreakIt() =>
        SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            var staff = f.Client;
            await f.Db.Set<NhanVien>().ExecuteUpdateAsync(s => s.SetProperty(e => e.ChucDanh, "LeTan"));
            await f.Login();
            var owner = await AddCustomer(f, "[DEMO] Owner", "owner@example.invalid");
            var legacy = await AddCustomer(f, "[DEMO] Legacy", "legacy@example.invalid", legacy: true);
            var older = Adult.AddDays(-1);
            var younger = Adult.AddDays(1);

            foreach (var (relation, valid, invalid) in new (string, DateOnly, DateOnly?)[]
            {
                ("Bản thân", Adult, younger), ("Con", younger, Adult),
                ("Cha", older, Adult), ("Mẹ", older, younger), ("Ông", older, Adult), ("Bà", older, younger),
                ("Anh", Adult, younger), ("Chị", Adult, younger), ("Em", Adult, older),
                ("Vợ", older, null), ("Chồng", younger, null), ("Cùng hộ khẩu", Adult, null)
            })
            {
                var draft = new CreateRecipientRequest(owner.MaKhachHang, "[DEMO] " + relation, null, valid, "Khác", relation);
                using var created = await staff.PostAsJsonAsync("/api/v1/recipients", draft);
                Assert.Equal(HttpStatusCode.Created, created.StatusCode);
                if (invalid is not DateOnly badDate) continue;
                using var rejected = await staff.PostAsJsonAsync("/api/v1/recipients", draft with { DateOfBirth = badDate });
                Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
                Assert.Equal("validation", await Code(rejected));
                var recipient = (await created.Content.ReadFromJsonAsync<RecipientDto>())!;
                using var rejectedEdit = await staff.PutAsJsonAsync($"/api/v1/recipients/{recipient.Id}",
                    new UpdateRecipientRequest(recipient.FullName, null, badDate, "Khác", relation));
                Assert.Equal(HttpStatusCode.BadRequest, rejectedEdit.StatusCode);
            }

            var legacyChild = new CreateRecipientRequest(legacy.MaKhachHang, "[DEMO] Child", null, older, "Nam", "Con");
            using var legacyAllowed = await staff.PostAsJsonAsync("/api/v1/recipients", legacyChild);
            Assert.Equal(HttpStatusCode.Created, legacyAllowed.StatusCode);
            foreach (var draft in new[]
            {
                legacyChild with { RelationshipToCustomer = "Khác" },
                legacyChild with { DateOfBirth = ProfileRules.Today(DateTimeOffset.UtcNow).AddDays(1) }
            })
            {
                using var invalid = await staff.PostAsJsonAsync("/api/v1/recipients", draft);
                Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            }

            using var changedOwner = await staff.PutAsJsonAsync($"/api/v1/customers/{owner.MaKhachHang}",
                new CreateCustomerRequest(owner.HoTen, null, Phone, owner.Email, "Chị", younger));
            Assert.Equal(HttpStatusCode.BadRequest, changedOwner.StatusCode);
            Assert.Equal(Adult, await f.Db.Customers.Where(c => c.MaKhachHang == owner.MaKhachHang).Select(c => c.NgaySinh).SingleAsync());
        });

    [Fact, Trait("Category", "SqlIntegration")]
    public Task CustomerClaimUsesExactVerifiedMatchAndAuditsGenericFailuresAndRateLimit() =>
        SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            var body = new { fullName = "[DEMO] Exact RF03", phoneNumber = Phone, dateOfBirth = Adult };
            var email = "exact@example.invalid";
            using var unauthenticated = await f.Client.PostAsJsonAsync(CustomerClaim, body);
            Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
            await f.Login();
            Assert.False(await CanClaim(f.Client));
            using var staffDenied = await f.Client.PostAsJsonAsync(CustomerClaim, body);
            Assert.Equal(HttpStatusCode.Forbidden, staffDenied.StatusCode);

            var account = await AddAccount(f, email);
            var otherAccount = await AddAccount(f, "other@example.invalid");
            var thirdAccount = await AddAccount(f, "third@example.invalid");
            using var actor = await Login(f, account);
            using var other = await Login(f, otherAccount);
            using var third = await Login(f, thirdAccount);
            Assert.True(await CanClaim(actor));
            var target = await AddCustomer(f, body.fullName, " EXACT@example.invalid ");
            await AddCustomer(f, "[DEMO] Legacy", otherAccount.LoginEmail!, legacy: true);
            await AssertUnavailable(other, CustomerClaim, body);
            await AssertUnavailable(other, CustomerClaim, body with { fullName = "[DEMO] Legacy" });
            Assert.Empty((await other.GetFromJsonAsync<CustomerListResponse>("/api/v1/customers"))!.Items);
            await AssertUnavailable(actor, CustomerClaim, body with { phoneNumber = "0900000001" });
            await AssertUnavailable(actor, CustomerClaim, body with { fullName = "[DEMO] Exáct RF03" });
            await AssertUnavailable(actor, CustomerClaim, body with { dateOfBirth = Adult.AddDays(1) });
            var duplicate = await AddCustomer(f, body.fullName, email);
            await AssertUnavailable(actor, CustomerClaim, body);
            f.Db.Remove(duplicate);
            await f.Db.SaveChangesAsync();

            var concurrent = await Task.WhenAll(
                actor.PostAsJsonAsync(CustomerClaim, body with { fullName = " [demo] exact rf03 " }),
                actor.PostAsJsonAsync(CustomerClaim, body));
            try
            {
                using var success = Assert.Single(concurrent, r => r.StatusCode == HttpStatusCode.OK);
                using var conflict = Assert.Single(concurrent, r => r.StatusCode == HttpStatusCode.Conflict);
                Assert.Equal("no-store", success.Headers.CacheControl?.ToString());
                Assert.Equal("profileClaimUnavailable", await Code(conflict));
                Assert.Equal(target.MaKhachHang, (await success.Content.ReadFromJsonAsync<CustomerDto>())!.Id);
            }
            finally { foreach (var response in concurrent) response.Dispose(); }

            Assert.False(await CanClaim(actor));
            Assert.Equal(target.MaKhachHang, Assert.Single((await actor.GetFromJsonAsync<CustomerListResponse>("/api/v1/customers"))!.Items).Id);
            Assert.Empty((await actor.GetFromJsonAsync<RecipientListResponse>("/api/v1/recipients"))!.Items);
            using var limited = await actor.PostAsJsonAsync(CustomerClaim, body);
            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
            Assert.Equal("rateLimited", await Code(limited));
            target.Email = thirdAccount.LoginEmail!;
            await f.Db.SaveChangesAsync();
            await AssertUnavailable(third, CustomerClaim, body);
            var link = Assert.Single(await f.Db.Set<AccountProfileLink>().Where(l => l.CustomerId == target.MaKhachHang).ToArrayAsync());
            Assert.Equal(account.MaTaiKhoan, link.AccountId);
            Assert.Equal("Active", link.State);

            var audit = await f.Db.Set<SecurityAudit>().Where(a => a.ActorAccountId == account.MaTaiKhoan &&
                (a.Action == "profiles.claimAttempt" || a.Action == "profiles.claimOutcome")).ToArrayAsync();
            Assert.Equal(7, audit.Count(a => a.Action == "profiles.claimAttempt"));
            Assert.Equal(1, audit.Count(a => a.Action == "profiles.claimOutcome" && a.Outcome == "Success"));
            Assert.Equal(5, audit.Count(a => a.Outcome == "Denied"));
            Assert.Single(audit, a => a.Outcome == "RateLimited");
            Assert.All(audit, a => { Assert.NotNull(a.SessionId); Assert.Null(a.ResourceId); Assert.Null(a.ChangedFields); });
        });

    [Fact, Trait("Category", "SqlIntegration")]
    public Task RecipientClaimCannotEscapeLinkedCustomerAndDoesNotLinkSiblings() =>
        SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            var account = await AddAccount(f, "recipient-owner@example.invalid");
            var otherAccount = await AddAccount(f, "recipient-other@example.invalid");
            var owner = await AddCustomer(f, "[DEMO] Owner", account.LoginEmail!);
            var other = await AddCustomer(f, "[DEMO] Other", otherAccount.LoginEmail!);
            var target = AddRecipient(f, owner, "[DEMO] Twins", "000000000111");
            AddRecipient(f, owner, "[DEMO] Twins", "000000000222");
            var crossCustomer = AddRecipient(f, other, "[DEMO] Foreign");
            var linkedElsewhere = AddRecipient(f, owner, "[DEMO] Already linked");
            await f.Db.SaveChangesAsync();
            f.Db.Add(new AccountProfileLink { AccountId = otherAccount.MaTaiKhoan, RecipientId = linkedElsewhere.MaNguoiDuocTiem, CreatedAt = DateTimeOffset.UtcNow });
            await f.Db.SaveChangesAsync();

            using var actor = await Login(f, account);
            var claim = new { fullName = target.HoTen, dateOfBirth = Child };
            await AssertUnavailable(actor, RecipientClaim, claim);
            using var ownerClaim = await actor.PostAsJsonAsync(CustomerClaim, new { fullName = owner.HoTen, dateOfBirth = Adult, phoneNumber = Phone });
            Assert.Equal(HttpStatusCode.OK, ownerClaim.StatusCode);
            Assert.Empty((await actor.GetFromJsonAsync<RecipientListResponse>("/api/v1/recipients"))!.Items);
            await AssertUnavailable(actor, RecipientClaim, claim with { fullName = crossCustomer.HoTen });
            await AssertUnavailable(actor, RecipientClaim, claim with { fullName = linkedElsewhere.HoTen });
            await AssertUnavailable(actor, RecipientClaim, claim);
            var identified = new { fullName = " [demo] twins ", dateOfBirth = Child, identityNumber = target.CCCD };
            using var linked = await actor.PostAsJsonAsync(RecipientClaim, identified);
            Assert.Equal(HttpStatusCode.OK, linked.StatusCode);
            Assert.Equal(target.MaNguoiDuocTiem, (await linked.Content.ReadFromJsonAsync<RecipientDto>())!.Id);
            var mine = Assert.Single((await actor.GetFromJsonAsync<RecipientListResponse>("/api/v1/recipients"))!.Items);
            Assert.Equal(target.MaNguoiDuocTiem, mine.Id);
            Assert.Single(await f.Db.Set<AccountProfileLink>().Where(l => l.AccountId == account.MaTaiKhoan && l.RecipientId != null).ToArrayAsync());
            Assert.Empty((await actor.GetFromJsonAsync<RecipientListResponse>($"/api/v1/recipients?customerId={other.MaKhachHang}"))!.Items);
            using var otherActor = await Login(f, otherAccount);
            using var otherOwner = await otherActor.PostAsJsonAsync(CustomerClaim,
                new { fullName = other.HoTen, dateOfBirth = Adult, phoneNumber = Phone });
            Assert.Equal(HttpStatusCode.OK, otherOwner.StatusCode);
            using var optionalIdentity = await otherActor.PostAsJsonAsync(RecipientClaim, claim with { fullName = crossCustomer.HoTen });
            Assert.Equal(HttpStatusCode.OK, optionalIdentity.StatusCode);
            using var repeated = await otherActor.PostAsJsonAsync(RecipientClaim, claim with { fullName = crossCustomer.HoTen });
            Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        });

    private static Task<int> BirthColumns(CatalogSqlTests.Fixture f) => f.Db.Database.SqlQuery<int>($"""
        SELECT COUNT(*) AS [Value] FROM sys.columns
        WHERE object_id = OBJECT_ID(N'KhachHang') AND name IN (N'DanhXung', N'NgaySinh')
        """).SingleAsync();

    private static async Task<string?> Code(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();

    private static async Task<bool> CanClaim(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me")).GetProperty("capabilities").GetProperty("selfClaim").GetBoolean();

    private static async Task AssertUnavailable(HttpClient client, string route, object body)
    {
        using var response = await client.PostAsJsonAsync(route, body);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("profileClaimUnavailable", json.GetProperty("code").GetString());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("errors").ValueKind);
        Assert.Equal(new[] { "code", "errors", "status", "title", "traceId", "type" },
            json.EnumerateObject().Select(p => p.Name).Order().ToArray());
    }

    private static async Task<TaiKhoan> AddAccount(CatalogSqlTests.Fixture f, string email)
    {
        var account = new TaiKhoan
        {
            AccountType = "Customer", LoginEmail = email, NormalizedEmail = email.ToUpperInvariant(), TenDangNhap = email,
            TrangThai = "Active", EmailVerifiedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow
        };
        account.MatKhauHash = new PasswordHasher<TaiKhoan>().HashPassword(account, f.Password);
        f.Db.Add(account);
        await f.Db.SaveChangesAsync();
        return account;
    }

    private static async Task<HttpClient> Login(CatalogSqlTests.Fixture f, TaiKhoan account)
    {
        var client = f.Factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { identifier = account.LoginEmail, password = f.Password, clientType = "native" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<KhachHang> AddCustomer(CatalogSqlTests.Fixture f, string name, string email, bool legacy = false)
    {
        var customer = new KhachHang { HoTen = name, Email = email, SoDienThoai = Phone, DanhXung = legacy ? null : "Chị", NgaySinh = legacy ? null : Adult };
        f.Db.Add(customer);
        await f.Db.SaveChangesAsync();
        return customer;
    }

    private static NguoiDuocTiem AddRecipient(CatalogSqlTests.Fixture f, KhachHang customer, string name, string? identity = null)
    {
        var recipient = new NguoiDuocTiem
        {
            MaKhachHang = customer.MaKhachHang, HoTen = name, CCCD = identity, NgaySinh = Child, GioiTinh = "Nam", QuanHeVoiKhachHang = "Con"
        };
        f.Db.Add(recipient);
        return recipient;
    }
}
