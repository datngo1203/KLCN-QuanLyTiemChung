using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TiemChung.Api;
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
                VALUES(N'[DEMO] Legacy RF03', '0900000000', 'legacy@example.invalid');
                DECLARE @customer int = CONVERT(int, SCOPE_IDENTITY());
                INSERT NguoiDuocTiem(MaKhachHang, HoTen, NgaySinh, GioiTinh, QuanHeVoiKhachHang)
                VALUES(@customer, N'Legacy RF05 A', '2015-01-01', N'Nam', N'Con'),
                      (@customer, N'Legacy RF05 B', '2015-01-02', N'Nam', N'Con');
                """);
            await migrator.MigrateAsync();
            await migrator.MigrateAsync();
            Assert.Equal(2, await BirthColumns(f));
            var legacyCodes = await f.Db.Recipients.Where(x => x.HoTen.StartsWith("Legacy RF05"))
                .Select(x => x.MaHoSo).ToArrayAsync();
            Assert.Equal(2, legacyCodes.Distinct().Count());
            Assert.All(legacyCodes, code => Assert.Matches(@"^VAC-[0-9A-F]{12}$", code));
            Assert.Equal(1, await f.Db.Database.SqlQuery<int>($"""
                SELECT COUNT(*) AS [Value] FROM sys.indexes
                WHERE object_id = OBJECT_ID('dbo.NguoiDuocTiem') AND name = 'IX_NguoiDuocTiem_MaHoSo' AND is_unique = 1
                """).SingleAsync());
            await migrator.MigrateAsync();
            Assert.Equal(legacyCodes.Order(), (await f.Db.Recipients.Where(x => x.HoTen.StartsWith("Legacy RF05"))
                .Select(x => x.MaHoSo).ToArrayAsync()).Order());
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
            Assert.Equal(0, await f.Db.Database.SqlQuery<int>($"""
                SELECT COUNT(*) AS [Value] FROM sys.columns
                WHERE object_id = OBJECT_ID('dbo.NguoiDuocTiem') AND name = 'MaHoSo'
                """).SingleAsync());
            await migrator.MigrateAsync();
            var reappliedCodes = await f.Db.Recipients.Where(x => x.HoTen.StartsWith("Legacy RF05"))
                .Select(x => x.MaHoSo).ToArrayAsync();
            Assert.Equal(2, reappliedCodes.Distinct().Count());
            Assert.All(reappliedCodes, code => Assert.Matches(@"^VAC-[0-9A-F]{12}$", code));
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
    public Task UnauthenticatedClaimsReachTheSharedIpLimitWithoutWritingClaimAudit() =>
        SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            var body = new { fullName = "[DEMO] No session", phoneNumber = Phone, dateOfBirth = Adult };
            var expired = new IdentityTokens(f.Config).Issue(1, Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-10));
            for (var i = 0; i < 30; i++)
            {
                f.Client.DefaultRequestHeaders.Authorization = (i % 3) switch
                {
                    0 => null,
                    1 => new("Bearer", "invalid"),
                    _ => new("Bearer", expired)
                };
                using var denied = await f.Client.PostAsJsonAsync(CustomerClaim, body);
                Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
                Assert.Equal("invalidSession", await Code(denied));
            }

            foreach (var route in new[] { CustomerClaim, RecipientClaim })
            {
                using var limited = await f.Client.PostAsJsonAsync(route, body);
                Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
                Assert.Equal("rateLimited", await Code(limited));
            }
            Assert.False(await f.Db.Set<SecurityAudit>().AnyAsync(a =>
                a.Action == "profiles.claimAttempt" || a.Action == "profiles.claimOutcome"));
        });

    [Fact, Trait("Category", "SqlIntegration")]
    public Task AccountLimitsAreIndependentAndEveryClaimConsumesOnlyOneIpSlot() =>
        SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            var first = await AddAccount(f, "claim-limit-first@example.invalid");
            var second = await AddAccount(f, "claim-limit-second@example.invalid");
            using var firstClient = await Login(f, first);
            using var secondClient = await Login(f, second);
            var body = new { fullName = "[DEMO] No match", phoneNumber = Phone, dateOfBirth = Adult };
            foreach (var client in new[] { firstClient, secondClient })
            {
                for (var i = 0; i < 6; i++) await AssertUnavailable(client, CustomerClaim, body);
                using var accountLimited = await client.PostAsJsonAsync(CustomerClaim, body);
                Assert.Equal(HttpStatusCode.TooManyRequests, accountLimited.StatusCode);
                Assert.Equal("rateLimited", await Code(accountLimited));
            }

            // Fourteen authenticated requests leave exactly sixteen slots for this IP.
            for (var i = 0; i < 16; i++)
            {
                using var denied = await f.Client.PostAsJsonAsync(CustomerClaim, body);
                Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
            }
            using var ipLimited = await secondClient.PostAsJsonAsync(CustomerClaim, body);
            Assert.Equal(HttpStatusCode.TooManyRequests, ipLimited.StatusCode);
            Assert.Equal("rateLimited", await Code(ipLimited));

            var audit = await f.Db.Set<SecurityAudit>().Where(a =>
                a.Action == "profiles.claimAttempt" || a.Action == "profiles.claimOutcome").ToArrayAsync();
            Assert.Equal(28, audit.Length);
            foreach (var account in new[] { first, second })
            {
                Assert.Equal(7, audit.Count(a => a.ActorAccountId == account.MaTaiKhoan && a.Action == "profiles.claimAttempt"));
                Assert.Equal(6, audit.Count(a => a.ActorAccountId == account.MaTaiKhoan && a.Outcome == "Denied"));
                Assert.Single(audit, a => a.ActorAccountId == account.MaTaiKhoan && a.Outcome == "RateLimited");
            }
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
            var claim = new { mode = "personalInfo", fullName = target.HoTen, dateOfBirth = Child, phoneNumber = Phone };
            await AssertUnavailable(actor, RecipientClaim, claim);
            using var ownerClaim = await actor.PostAsJsonAsync(CustomerClaim, new { fullName = owner.HoTen, dateOfBirth = Adult, phoneNumber = Phone });
            Assert.Equal(HttpStatusCode.OK, ownerClaim.StatusCode);
            Assert.Empty((await actor.GetFromJsonAsync<RecipientListResponse>("/api/v1/recipients"))!.Items);
            await AssertUnavailable(actor, RecipientClaim, claim with { fullName = crossCustomer.HoTen });
            await AssertUnavailable(actor, RecipientClaim, claim with { fullName = linkedElsewhere.HoTen });
            await AssertUnavailable(actor, RecipientClaim, claim);
            var identified = new { mode = "profileCode", profileCode = target.MaHoSo.ToLowerInvariant(), phoneNumber = Phone };
            using var linked = await actor.PostAsJsonAsync(RecipientClaim, identified);
            Assert.Equal(HttpStatusCode.OK, linked.StatusCode);
            Assert.Equal(target.MaNguoiDuocTiem, (await linked.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("recipient").GetProperty("id").GetInt32());
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

    [Fact, Trait("Category", "SqlIntegration")]
    public Task RecipientClaimLinksOwnerAndRecipientOnceAcrossConcurrentAccounts() =>
        SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            var first = await AddAccount(f, "rf05-first@example.invalid");
            var second = await AddAccount(f, "rf05-second@example.invalid");
            var owner = await AddCustomer(f, "Owner RF05", "rf05-owner@example.invalid");
            var recipient = AddRecipient(f, owner, "Recipient RF05");
            await f.Db.SaveChangesAsync();
            Assert.Matches(@"^VAC-[0-9A-F]{12}$", recipient.MaHoSo);

            using var a = await Login(f, first);
            using var b = await Login(f, second);
            using var missing = await a.PostAsJsonAsync(RecipientClaim,
                new { mode = "personalInfo", fullName = recipient.HoTen, dateOfBirth = Child });
            Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
            using var mixed = await a.PostAsJsonAsync(RecipientClaim,
                new { mode = "profileCode", profileCode = recipient.MaHoSo, phoneNumber = Phone, fullName = recipient.HoTen });
            Assert.Equal(HttpStatusCode.BadRequest, mixed.StatusCode);
            var body = new { mode = "personalInfo", fullName = " recipient rf05 ", dateOfBirth = Child, phoneNumber = Phone };
            await AssertUnavailable(a, RecipientClaim, body with { phoneNumber = "0900000001" });

            var results = await Task.WhenAll(a.PostAsJsonAsync(RecipientClaim, body), b.PostAsJsonAsync(RecipientClaim, body));
            using var success = Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
            using var conflict = Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
            Assert.Equal("profileClaimUnavailable", await Code(conflict));
            var links = await f.Db.Set<AccountProfileLink>().AsNoTracking().Where(x =>
                x.CustomerId == owner.MaKhachHang || x.RecipientId == recipient.MaNguoiDuocTiem).ToListAsync();
            Assert.Equal(2, links.Count);
            var account = Assert.Single(links.Select(x => x.AccountId).Distinct());
            var winner = account == first.MaTaiKhoan ? a : b;
            using var retry = await winner.PostAsJsonAsync(RecipientClaim, body);
            Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
            Assert.Equal(2, await f.Db.Set<AccountProfileLink>().CountAsync(x =>
                x.CustomerId == owner.MaKhachHang || x.RecipientId == recipient.MaNguoiDuocTiem));
        });

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData(false)]
    [InlineData(true)]
    public Task RecipientClaimRollsBackLinksAndSuccessAuditOnSqlFailure(bool failOnAudit) =>
        SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            var account = await AddAccount(f, "rollback-rf05@example.invalid");
            var owner = await AddCustomer(f, "Owner rollback", "owner-rollback@example.invalid");
            var recipient = AddRecipient(f, owner, "Recipient rollback");
            await f.Db.SaveChangesAsync();
            using var actor = await Login(f, account);
            if (failOnAudit)
                await f.Db.Database.ExecuteSqlRawAsync("""
                    ALTER TABLE dbo.SecurityAudit ADD CONSTRAINT CK_TestClaimAuditFailure
                        CHECK (Action <> 'profiles.claimOutcome' OR Outcome <> 'Success');
                    """);
            else
                await f.Db.Database.ExecuteSqlRawAsync("""
                    ALTER TABLE dbo.AccountProfileLink ADD CONSTRAINT CK_TestClaimFailure CHECK (RecipientId IS NULL);
                    """);

            using var result = await actor.PostAsJsonAsync(RecipientClaim,
                new { mode = "profileCode", profileCode = recipient.MaHoSo, phoneNumber = Phone });
            Assert.False(result.IsSuccessStatusCode);
            Assert.Empty(await f.Db.Set<AccountProfileLink>().Where(x => x.AccountId == account.MaTaiKhoan).ToArrayAsync());
            Assert.False(await f.Db.Set<SecurityAudit>().AnyAsync(x => x.ActorAccountId == account.MaTaiKhoan
                && x.Action == "profiles.claimOutcome" && x.Outcome == "Success"));
        });

    private static Task<int> BirthColumns(CatalogSqlTests.Fixture f) => f.Db.Database.SqlQuery<int>($"""
        SELECT COUNT(*) AS [Value] FROM sys.columns
        WHERE object_id = OBJECT_ID(N'KhachHang') AND name IN (N'DanhXung', N'NgaySinh')
        """).SingleAsync();

    [Fact, Trait("Category", "SqlIntegration")]
    public Task PublicFindReturnsOnlyMatchWithoutSessionLinksOrHealthAccess() =>
        SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            var owner = await AddCustomer(f, "Nguyễn Test An", "khach01@example.invalid");
            var recipient = AddRecipient(f, owner, "Nguyễn Test Bình");
            await f.Db.SaveChangesAsync();
            var sessions = await f.Db.Set<IdentitySession>().CountAsync();
            var audits = await f.Db.Set<SecurityAudit>().CountAsync();
            var byCode = new { mode = "profileCode", profileCode = recipient.MaHoSo.ToLowerInvariant(), phoneNumber = Phone };
            var byName = new { mode = "personalInfo", fullName = " nguyễn test bình ", dateOfBirth = Child, phoneNumber = Phone };
            foreach (var body in new object[] { byCode, byName })
            {
                using var found = await f.Client.PostAsJsonAsync("/api/v1/recipients/find", body);
                Assert.Equal(HttpStatusCode.OK, found.StatusCode);
                Assert.Equal("no-store", found.Headers.CacheControl!.ToString());
                Assert.False(found.Headers.Contains("Set-Cookie"));
                Assert.Equal("{\"matched\":true}", await found.Content.ReadAsStringAsync());
            }
            foreach (var body in new object[] { byCode with { phoneNumber = "0900000001" },
                byCode with { profileCode = "VAC-000000000000" }, byName with { fullName = "Nguyễn Test Chi" },
                byName with { dateOfBirth = Child.AddDays(1) } })
            {
                using var absent = await f.Client.PostAsJsonAsync("/api/v1/recipients/find", body);
                Assert.Equal(HttpStatusCode.OK, absent.StatusCode);
                Assert.Equal("{\"matched\":false}", await absent.Content.ReadAsStringAsync());
            }
            using var noClaim = await f.Client.PostAsJsonAsync(RecipientClaim, byCode);
            Assert.Equal(HttpStatusCode.Unauthorized, noClaim.StatusCode);
            using var noHealth = await f.Client.GetAsync("/api/v1/vaccinations/history");
            Assert.Equal(HttpStatusCode.Unauthorized, noHealth.StatusCode);
            Assert.Equal(sessions, await f.Db.Set<IdentitySession>().CountAsync());
            Assert.Equal(audits, await f.Db.Set<SecurityAudit>().CountAsync());
            Assert.False(await f.Db.Set<AccountProfileLink>().AnyAsync());
            // Ownership is deliberately absent from the public result, even after someone links it.
            var account = await AddAccount(f, "khach01@example.invalid");
            using var actor = await Login(f, account);
            using var linked = await actor.PostAsJsonAsync(RecipientClaim, byCode);
            Assert.Equal(HttpStatusCode.OK, linked.StatusCode);
            using var stillFound = await f.Client.PostAsJsonAsync("/api/v1/recipients/find", byCode);
            Assert.Equal("{\"matched\":true}", await stillFound.Content.ReadAsStringAsync());
        });

    [Fact, Trait("Category", "SqlIntegration")]
    public Task PublicFindCannotAuthorizeClaimAndClaimRechecksChangedDataAndVerifiedEmail() =>
        SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            var owner = await AddCustomer(f, "Nguyễn Test An", "khach01@example.invalid");
            var recipient = AddRecipient(f, owner, "Nguyễn Test Bình");
            var account = await AddAccount(f, "khach01@example.invalid");
            await f.Db.SaveChangesAsync();
            var body = new { mode = "profileCode", profileCode = recipient.MaHoSo, phoneNumber = Phone };
            using var found = await f.Client.PostAsJsonAsync("/api/v1/recipients/find", body);
            Assert.Equal("{\"matched\":true}", await found.Content.ReadAsStringAsync());
            using var actor = await Login(f, account);
            await f.Db.Customers.Where(x => x.MaKhachHang == owner.MaKhachHang)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SoDienThoai, "0900000001"));
            await AssertUnavailable(actor, RecipientClaim, body);
            Assert.False(await f.Db.Set<AccountProfileLink>().AnyAsync());
            await f.Db.Customers.Where(x => x.MaKhachHang == owner.MaKhachHang)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SoDienThoai, Phone));
            await f.Db.Set<TaiKhoan>().Where(x => x.MaTaiKhoan == account.MaTaiKhoan)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EmailVerifiedAt, (DateTimeOffset?)null));
            using var unverified = await actor.PostAsJsonAsync(RecipientClaim, body);
            Assert.Equal(HttpStatusCode.Forbidden, unverified.StatusCode);
            Assert.Equal("emailVerificationRequired", await Code(unverified));
            using var loginDenied = await f.Client.PostAsJsonAsync("/api/v1/auth/login",
                new { identifier = account.LoginEmail, password = f.Password, clientType = "native" });
            Assert.Equal(HttpStatusCode.Forbidden, loginDenied.StatusCode);
            Assert.False(await f.Db.Set<AccountProfileLink>().AnyAsync());
            await f.Db.Set<TaiKhoan>().Where(x => x.MaTaiKhoan == account.MaTaiKhoan)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EmailVerifiedAt, DateTimeOffset.UtcNow));
            using var linked = await actor.PostAsJsonAsync(RecipientClaim, body);
            Assert.Equal(HttpStatusCode.OK, linked.StatusCode);
            using var other = await Login(f, await AddAccount(f, "khach02@example.invalid"));
            await AssertUnavailable(other, RecipientClaim, body);
            Assert.Equal(2, await f.Db.Set<AccountProfileLink>().CountAsync());
        });

    [Fact, Trait("Category", "SqlIntegration")]
    public Task PublicFindRejectsAmbiguousAndMixedInputAndSharesIpBudgetWithClaims() =>
        SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f =>
        {
            var owner = await AddCustomer(f, "Nguyễn Test An", "khach01@example.invalid");
            AddRecipient(f, owner, "Nguyễn Test Bình");
            AddRecipient(f, owner, "Nguyễn Test Bình");
            await f.Db.SaveChangesAsync();
            var body = new { mode = "personalInfo", fullName = "Nguyễn Test Bình", dateOfBirth = Child, phoneNumber = Phone };
            using var ambiguous = await f.Client.PostAsJsonAsync("/api/v1/recipients/find", body);
            Assert.Equal("{\"matched\":false}", await ambiguous.Content.ReadAsStringAsync());
            for (var i = 1; i < 30; i++)
            {
                using var invalid = await f.Client.PostAsJsonAsync("/api/v1/recipients/find",
                    new { mode = "profileCode", profileCode = "VAC-ABCDEF123456", phoneNumber = Phone, fullName = body.fullName });
                Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
                var json = await invalid.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal("validation", json.GetProperty("code").GetString());
                Assert.Equal(JsonValueKind.Null, json.GetProperty("errors").ValueKind);
            }
            foreach (var route in new[] { "/api/v1/recipients/find", RecipientClaim, CustomerClaim })
            {
                using var limited = await f.Client.PostAsJsonAsync(route, body);
                Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
                Assert.Equal("rateLimited", await Code(limited));
                Assert.NotNull(limited.Headers.RetryAfter);
            }
            Assert.False(await f.Db.Set<AccountProfileLink>().AnyAsync());
        });

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
