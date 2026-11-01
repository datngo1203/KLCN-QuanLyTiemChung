using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using Xunit;

namespace TiemChung.Tests;

public sealed class VaccinationHistorySqlTests
{
    private const string Route = "/api/v1/vaccinations/history";
    private static Task Run(Func<VaccinationSqlTests.Fixture, Task> test) =>
        SqlFixtureLifecycle.RunAsync(VaccinationSqlTests.Fixture.Create, test);

    private static async Task<(HttpClient Client, int AccountId)> Owner(VaccinationSqlTests.Fixture f)
    {
        var client = await f.Base.Actor(null, false);
        var id = f.Base.LastAccountId;
        f.Db.AddRange(new AccountProfileLink { AccountId = id, CustomerId = f.Base.CustomerId, State = "Active" },
            new AccountProfileLink { AccountId = id, RecipientId = f.Base.RecipientId, State = "Active" });
        await f.Db.SaveChangesAsync();
        return (client, id);
    }
    private static async Task<VaccinationDto> Record(VaccinationSqlTests.Fixture f, int indication, bool second = false)
    {
        using var response = await f.Nurse.PostAsJsonAsync("/api/v1/vaccinations",
            new RecordVaccinationRequest(indication, second ? "[DEMO] S55-VALID-B" : "[DEMO] S5-VALID-A", "[DEMO] Bắp tay trái"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<VaccinationDto>())!;
    }
    private static async Task<VaccinationHistoryDto[]> Read(HttpClient client, string query = "")
    {
        using var response = await client.GetAsync(Route + query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        return (await response.Content.ReadFromJsonAsync<VaccinationHistoryResponse>())!.Items;
    }
    private static async Task Problem(HttpClient client, string query, int status, string code)
    {
        using var response = await client.GetAsync(Route + query);
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        var text = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(text);
        Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
        Assert.True(json.RootElement.TryGetProperty("traceId", out _));
        Assert.DoesNotContain("[DEMO]", text); Assert.DoesNotContain("SqlException", text);
        Assert.False(json.RootElement.TryGetProperty("items", out _));
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task PackageHistoryContainsOnlyRealShotsAndRemainsReadableAfterLotPaymentAndClinicalConditionsChange()
    {
        await Run(async f => {
            var (owner, _) = await Owner(f);
            Assert.Empty(await Read(owner));
            var source = await f.Source("package", [f.A, f.B]);
            Assert.Empty(await Read(owner)); // Scheduled, assessed and indicated is not administered.
            var a = await Record(f, source.Assessment.Indications.Single(i => i.VaccineId == f.A).Id);
            Assert.Equal(a.Id, Assert.Single(await Read(owner)).Id);
            var b = await Record(f, source.Assessment.Indications.Single(i => i.VaccineId == f.B).Id, true);
            Assert.Equal(new[] { b.Id, a.Id }, (await Read(owner)).Select(m => m.Id));
            Assert.Empty(await f.Db.Set<TheoDoiSauTiem>().ToArrayAsync());
            await f.Db.Set<LoVacXin>().ExecuteUpdateAsync(s => s.SetProperty(l => l.SoLuongTon, 0)
                .SetProperty(l => l.HanSuDung, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1)).SetProperty(l => l.TrangThai, "Inactive"));
            await f.Db.Payments.ExecuteDeleteAsync();
            await f.Db.Registrations.ExecuteUpdateAsync(s => s.SetProperty(r => r.TrangThai, "Legacy"));
            await f.Db.Set<PhieuKhamSangLoc>().ExecuteUpdateAsync(s => s.SetProperty(p => p.KetLuan, "Tạm hoãn"));
            var before = await f.Snapshot();
            var rows = await Read(owner);
            Assert.Equal(new[] { b.Id, a.Id }, rows.Select(m => m.Id));
            Assert.Equal(a.AdministeredAt, rows[1].AdministeredAt); Assert.Equal(TimeSpan.Zero, rows[1].AdministeredAt.Offset);
            Assert.Equal(a.Recipient, rows[1].Recipient); Assert.Equal(a.Vaccine, rows[1].Vaccine);
            Assert.Equal(a.LotNumber, rows[1].LotNumber); Assert.Equal(a.InjectionSite, rows[1].InjectionSite);
            Assert.Equal(source.Appointment.Id, rows[1].AppointmentId); Assert.Equal(source.Registration.Id, rows[1].RegistrationId);
            using var response = await owner.GetAsync(Route);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var item = json.RootElement.GetProperty("items")[0];
            Assert.Equal(new[] { "administeredAt", "appointmentId", "id", "injectionSite", "lotNumber", "recipient", "registrationId", "status", "vaccine" },
                item.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
            Assert.Equal(new[] { "fullName", "id" }, item.GetProperty("recipient").EnumerateObject().Select(p => p.Name).OrderBy(n => n));
            Assert.Equal(new[] { "id", "name" }, item.GetProperty("vaccine").EnumerateObject().Select(p => p.Name).OrderBy(n => n));
            Assert.Equal("DaTiem", item.GetProperty("status").GetString());
            Assert.Equal(before, await f.Snapshot());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task BothActiveLinksAndMatchingCustomerAreRequiredAndRecipientFilterOnlyNarrowsOwnership()
    {
        await Run(async f => {
            var source = await f.Source(); var shot = await Record(f, source.Assessment.Indications.Single().Id);
            using var a = await f.Base.Actor(null, false); var accountId = f.Base.LastAccountId;
            using var b = await f.Base.Actor(null, false);
            Assert.Empty(await Read(a)); Assert.Empty(await Read(b, "?recipientId=" + shot.Recipient.Id));
            var customerLink = new AccountProfileLink { AccountId = accountId, CustomerId = f.Base.CustomerId, State = "Active" };
            var recipientLink = new AccountProfileLink { AccountId = accountId, RecipientId = f.Base.RecipientId, State = "Active" };
            f.Db.Add(customerLink); await f.Db.SaveChangesAsync(); Assert.Empty(await Read(a));
            f.Db.Remove(customerLink); f.Db.Add(recipientLink); await f.Db.SaveChangesAsync(); Assert.Empty(await Read(a));
            customerLink = new() { AccountId = accountId, CustomerId = f.Base.CustomerId, State = "Active" };
            f.Db.Add(customerLink); await f.Db.SaveChangesAsync(); Assert.Equal(shot.Id, Assert.Single(await Read(a)).Id);
            foreach (var link in new[] { customerLink, recipientLink }) {
                link.State = "Revoked"; await f.Db.SaveChangesAsync(); Assert.Empty(await Read(a));
                link.State = "Active"; await f.Db.SaveChangesAsync(); Assert.Single(await Read(a));
            }
            var other = new KhachHang { HoTen = "[DEMO] other parent" }; f.Db.Add(other); await f.Db.SaveChangesAsync();
            await f.Db.Recipients.Where(p => p.MaNguoiDuocTiem == shot.Recipient.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.MaKhachHang, other.MaKhachHang));
            var before = await f.Snapshot();
            Assert.Empty(await Read(a)); Assert.Empty(await Read(a, "?recipientId=" + shot.Recipient.Id));
            Assert.Empty(await Read(a, "?recipientId=2147483647")); Assert.Equal(before, await f.Snapshot());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task MultipleRecipientsAreAggregatedAndOwnershipAndStableTimeOrderApplyBeforeSqlPaging()
    {
        await Run(async f => {
            var (a, accountId) = await Owner(f);
            var first = await f.Source(); var shot1 = await Record(f, first.Assessment.Indications.Single().Id);
            var person = new NguoiDuocTiem { MaKhachHang = f.Base.CustomerId, HoTen = "[DEMO] second recipient" };
            f.Db.Add(person); await f.Db.SaveChangesAsync(); f.Base.RecipientId = person.MaNguoiDuocTiem;
            var second = await f.Source(); var shot2 = await Record(f, second.Assessment.Indications.Single().Id);
            Assert.Equal(shot1.Id, Assert.Single(await Read(a, "?pageSize=1")).Id); // Newer unowned row cannot consume page.
            f.Db.Add(new AccountProfileLink { AccountId = accountId, RecipientId = person.MaNguoiDuocTiem, State = "Active" });
            await f.Db.SaveChangesAsync();
            await f.Db.Set<MuiTiem>().ExecuteUpdateAsync(s => s.SetProperty(m => m.ThoiDiemTiem, shot1.AdministeredAt.UtcDateTime));
            var before = await f.Snapshot();
            Assert.Equal(new[] { shot2.Id, shot1.Id }, (await Read(a)).Select(m => m.Id));
            Assert.Equal(shot2.Id, Assert.Single(await Read(a, "?pageSize=1")).Id);
            Assert.Equal(shot1.Id, Assert.Single(await Read(a, "?pageSize=1&page=2")).Id);
            Assert.Empty(await Read(a, "?pageSize=1&page=3"));
            Assert.Equal(shot1.Id, Assert.Single(await Read(a, "?recipientId=" + shot1.Recipient.Id)).Id);
            Assert.Equal(person.HoTen, Assert.Single(await Read(a, "?recipientId=" + person.MaNguoiDuocTiem)).Recipient.FullName);
            // Time wins over ID when timestamps differ.
            await f.Db.Set<MuiTiem>().Where(m => m.MaMuiTiem == shot1.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.ThoiDiemTiem, shot1.AdministeredAt.UtcDateTime.AddMinutes(1)));
            Assert.Equal(shot1.Id, Assert.Single(await Read(a, "?pageSize=1")).Id);
            Assert.NotEqual(before, await f.Snapshot());
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("legacy")][InlineData("status")][InlineData("shotRecipient")][InlineData("lotVaccine")]
    [InlineData("appointment")][InlineData("assessmentRecipient")][InlineData("assessmentRegistration")][InlineData("vaccineSource")]
    public async Task UnprovenSourcesAreExcludedAndCompositeSqlForeignKeyRejectsMismatches(string scenario)
    {
        await Run(async f => {
            var (owner, _) = await Owner(f); var source = await f.Source();
            var shot = await Record(f, source.Assessment.Indications.Single().Id);
            if (scenario == "legacy") await f.Db.Set<MuiTiem>().ExecuteUpdateAsync(s => s.SetProperty(m => m.MaChiDinh, (int?)null));
            if (scenario == "status") await f.Db.Set<MuiTiem>().ExecuteUpdateAsync(s => s.SetProperty(m => m.TrangThai, "Legacy"));
            if (scenario is "shotRecipient" or "assessmentRecipient") {
                var person = new NguoiDuocTiem { MaKhachHang = f.Base.CustomerId, HoTen = "[DEMO] wrong source" };
                f.Db.Add(person); await f.Db.SaveChangesAsync();
                if (scenario == "shotRecipient") await f.Db.Set<MuiTiem>().ExecuteUpdateAsync(s => s.SetProperty(m => m.MaNguoiDuocTiem, person.MaNguoiDuocTiem));
                else {
                    await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => f.Db.Set<PhieuKhamSangLoc>()
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.MaNguoiDuocTiem, person.MaNguoiDuocTiem)));
                    Assert.Single(await Read(owner)); return;
                }
            }
            if (scenario == "lotVaccine") await f.Db.Set<LoVacXin>().Where(l => l.SoLo == shot.LotNumber).ExecuteUpdateAsync(s => s.SetProperty(l => l.MaVacXin, f.B));
            if (scenario == "appointment") await f.Db.Set<PhieuKhamSangLoc>().ExecuteUpdateAsync(s => s.SetProperty(p => p.MaLichHen, (int?)null).SetProperty(p => p.ThoiDiemKham, (DateTimeOffset?)null));
            if (scenario == "assessmentRegistration") {
                var other = await f.Register();
                await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => f.Db.Set<PhieuKhamSangLoc>()
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.MaDangKy, other.Id)));
                Assert.Single(await Read(owner)); return;
            }
            if (scenario == "vaccineSource") {
                await f.Db.Set<ChiDinhTiem>().ExecuteUpdateAsync(s => s.SetProperty(i => i.MaVacXin, f.B));
                await f.Db.Set<LoVacXin>().Where(l => l.SoLo == shot.LotNumber).ExecuteUpdateAsync(s => s.SetProperty(l => l.MaVacXin, f.B));
            }
            var before = await f.Snapshot(); Assert.Empty(await Read(owner)); Assert.Equal(before, await f.Snapshot());
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("Admin")][InlineData("LeTan")][InlineData("BacSi")][InlineData("DieuDuong")][InlineData("ThuNgan")][InlineData("ThuKho")]
    public async Task AllEmployeeTitlesAreDenied(string title)
    {
        await Run(async f => {
            using var actor = await f.Base.Actor(title, true); var before = await f.Snapshot();
            await Problem(actor, "", 403, "permissionDenied"); Assert.Equal(before, await f.Snapshot());
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("inactive", 401, "invalidSession")][InlineData("unverified", 403, "permissionDenied")]
    [InlineData("revoked", 401, "invalidSession")][InlineData("expired", 401, "invalidSession")]
    [InlineData("version", 401, "invalidSession")]
    public async Task RequiresCurrentVerifiedActiveCustomer(string scenario, int status, string code)
    {
        await Run(async f => {
            var (owner, id) = await Owner(f);
            if (scenario == "inactive") await f.Db.Set<TaiKhoan>().Where(a => a.MaTaiKhoan == id).ExecuteUpdateAsync(s => s.SetProperty(a => a.TrangThai, "Inactive"));
            if (scenario == "unverified") await f.Db.Set<TaiKhoan>().Where(a => a.MaTaiKhoan == id).ExecuteUpdateAsync(s => s.SetProperty(a => a.EmailVerifiedAt, (DateTimeOffset?)null));
            if (scenario == "version") await f.Db.Set<TaiKhoan>().Where(a => a.MaTaiKhoan == id).ExecuteUpdateAsync(s => s.SetProperty(a => a.SecurityVersion, a => a.SecurityVersion + 1));
            if (scenario == "revoked") await f.Db.Set<IdentitySession>().Where(a => a.AccountId == id).ExecuteUpdateAsync(s => s.SetProperty(a => a.RevokedAt, DateTimeOffset.UtcNow));
            if (scenario == "expired") await f.Db.Set<IdentitySession>().Where(a => a.AccountId == id).ExecuteUpdateAsync(s => s.SetProperty(a => a.CreatedAt, DateTimeOffset.UtcNow.AddHours(-2)).SetProperty(a => a.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
            var before = await f.Snapshot(); await Problem(owner, "", status, code); Assert.Equal(before, await f.Snapshot());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task QueryValidationAnonymousAndSqlFailureReturnSafeNoStoreProblems()
    {
        await Run(async f => {
            var (owner, _) = await Owner(f);
            foreach (var query in new[] { "recipientId=0", "recipientId=-1", "recipientId=abc", "page=0", "page=-1", "pageSize=0", "pageSize=101", "page=abc", "page=2147483647&pageSize=100", "page=2147483648" })
                await Problem(owner, "?" + query, 400, "validation");
            Assert.Empty(await Read(owner, "?pageSize=100"));
            using var anonymous = f.Base.Base.Factory.CreateClient();
            await Problem(anonymous, "", 401, "invalidSession");
            anonymous.DefaultRequestHeaders.Authorization = new("Bearer", "invalid");
            await Problem(anonymous, "", 401, "invalidSession");
            var before = await f.Snapshot();
            await f.Db.Database.ExecuteSqlRawAsync("EXEC sp_rename 'MuiTiem', 'S57Unavailable'");
            try { await Problem(owner, "", 503, "dependencyUnavailable"); }
            finally { await f.Db.Database.ExecuteSqlRawAsync("EXEC sp_rename 'S57Unavailable', 'MuiTiem'"); }
            Assert.Equal(before, await f.Snapshot());
        });
    }
}
