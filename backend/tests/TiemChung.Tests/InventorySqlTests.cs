using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using Xunit;
using Fixture = TiemChung.Tests.AppointmentSqlTests.Fixture;

namespace TiemChung.Tests;

public sealed class InventorySqlTests
{
    private const string Root = "/api/v1/inventory/";
    private const string Lot = "[DEMO] X1 / +%?#";
    private static string Movements(string lot = Lot) => Root + "movements?lotNumber=" + Uri.EscapeDataString(lot);
    private static async Task<HttpClient> Actor(Fixture f, string? title = "ThuKho", bool grant = true)
    {
        var client = await f.Actor(title, false);
        if (grant) {
            f.Db.Add(new TaiKhoan_QuyenHan { MaTaiKhoan = f.LastAccountId,
                MaQuyen = (await f.Db.Set<QuyenHan>().SingleAsync(p => p.Code == "XemKho")).MaQuyen });
            await f.Db.SaveChangesAsync();
        }
        return client;
    }
    private static async Task Seed(Fixture f)
    {
        var vaccine = await f.Db.Vaccines.FirstAsync();
        f.Db.AddRange(
            new LoVacXin { SoLo = Lot, MaVacXin = vaccine.MaVacXin, NgaySanXuat = new(2020,1,1), HanSuDung = new(2021,1,1), SoLuongBanDau = 10, SoLuongTon = 0, TrangThai = "Inactive" },
            new LoVacXin { SoLo = "[DEMO] X1 B", MaVacXin = vaccine.MaVacXin, NgaySanXuat = new(2020,1,1), HanSuDung = new(2030,1,1), SoLuongBanDau = 100, SoLuongTon = 98, TrangThai = "Active" });
        await f.Db.SaveChangesAsync();
        f.Db.AddRange(Enumerable.Range(1,3).Select(i => new BienDongKho { SoLo = Lot, MaNhanVien = f.EmployeeId,
            LoaiBienDong = "Nhap", SoLuong = i, ThoiDiem = new DateTime(2026,1,1,1,0,0,DateTimeKind.Utc) }));
        await f.Db.SaveChangesAsync();
    }
    private static async Task Problem(HttpResponseMessage response, int status, string code)
    {
        using (response) {
            Assert.Equal((HttpStatusCode)status, response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
            Assert.True(json.RootElement.TryGetProperty("traceId", out _));
        }
    }
    private static async Task<string> Snapshot(Fixture f)
    {
        var tables = await f.Db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sys.tables WHERE is_ms_shipped=0 ORDER BY name").ToArrayAsync();
        var rows = new List<string>();
        await f.Db.Database.OpenConnectionAsync();
        try {
            foreach (var table in tables) {
                await using var cmd = f.Db.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = $"SELECT (SELECT * FROM [{table.Replace("]", "]]")}] FOR JSON PATH, INCLUDE_NULL_VALUES)";
                rows.Add(Convert.ToString(await cmd.ExecuteScalarAsync())!);
            }
        } finally { await f.Db.Database.CloseConnectionAsync(); }
        return string.Join("\n", rows);
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task ReadsSqlLotsAndMovementsWithStablePagingAndNeverChangesPersistedData()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            await Seed(f); using var client = await Actor(f);
            var before = await Snapshot(f);
            using var response = await client.GetAsync(Root + "lots?pageSize=1");
            Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
            var first = (await response.Content.ReadFromJsonAsync<InventoryBatchListResponse>())!;
            Assert.True(first.HasMore); var row = Assert.Single(first.Items);
            Assert.Equal(Lot, row.LotNumber); Assert.Equal(0, row.Stock); Assert.Equal("Inactive", row.Status);
            Assert.Equal(new DateOnly(2021,1,1), row.ExpiresOn);
            Assert.Equal(DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime), first.FacilityDate);
            var second = (await client.GetFromJsonAsync<InventoryBatchListResponse>(Root + "lots?page=2&pageSize=1"))!;
            Assert.False(second.HasMore); Assert.Equal(98, Assert.Single(second.Items).Stock);
            Assert.Empty((await client.GetFromJsonAsync<InventoryBatchListResponse>(Root + "lots?search=missing"))!.Items);
            Assert.Equal(2, (await client.GetFromJsonAsync<InventoryBatchListResponse>(Root + "lots?search=" + Uri.EscapeDataString(" [DEMO] S5 vaccine ")))!.Items.Length);
            var movements = (await client.GetFromJsonAsync<InventoryMovementListResponse>(Movements() + "&pageSize=2"))!;
            Assert.True(movements.HasMore); Assert.Equal(new[] {3,2}, movements.Items.Select(m => m.Quantity));
            Assert.All(movements.Items, m => { Assert.Equal(Lot,m.LotNumber); Assert.Equal("Nhap",m.Type); Assert.Equal(TimeSpan.Zero,m.RecordedAt.Offset); Assert.Equal(f.EmployeeId,m.EmployeeId); });
            var last = (await client.GetFromJsonAsync<InventoryMovementListResponse>(Movements() + "&pageSize=2&page=2"))!;
            Assert.False(last.HasMore); Assert.Equal(1, Assert.Single(last.Items).Quantity);
            Assert.Empty((await client.GetFromJsonAsync<InventoryMovementListResponse>(Movements("[DEMO] X1 B")))!.Items);
            Assert.Equal(before, await Snapshot(f));
            // A later SQL stock change is visible on a new GET, rather than cached Sample state.
            await f.Db.Set<LoVacXin>().Where(l => l.SoLo == "[DEMO] X1 B").ExecuteUpdateAsync(s => s.SetProperty(l => l.SoLuongTon, 97));
            Assert.Equal(97, (await client.GetFromJsonAsync<InventoryBatchListResponse>(Root + "lots?search=X1%20B"))!.Items.Single().Stock);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task BothRoutesRequireWarehouseTitleAndCurrentGrantAndActiveIdentity()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            await Seed(f);
            async Task Denied(HttpClient client, int status, string code) {
                await Problem(await client.GetAsync(Root + "lots"), status, code);
                await Problem(await client.GetAsync(Movements()), status, code);
            }
            using var anonymous = f.Base.Factory.CreateClient(); await Denied(anonymous,401,"invalidSession");
            foreach (var title in new string?[] { null,"Admin","LeTan","BacSi","DieuDuong","ThuNgan" }) {
                using var other = await Actor(f,title); await Denied(other,403,"permissionDenied");
            }
            using var noGrant = await Actor(f,grant:false); await Denied(noGrant,403,"permissionDenied");
            using var warehouse = await Actor(f); var accountId = f.LastAccountId;
            Assert.Equal(HttpStatusCode.OK, (await warehouse.GetAsync(Root + "lots")).StatusCode);
            await f.Db.Set<TaiKhoan_QuyenHan>().Where(g => g.MaTaiKhoan == accountId).ExecuteDeleteAsync();
            await Denied(warehouse,403,"permissionDenied");
            foreach (var invalid in new[] { "employee","account","session","version","expired" }) {
                using var client = await Actor(f); var id = f.LastAccountId;
                if (invalid == "employee") { var eid = (await f.Db.Set<TaiKhoan>().SingleAsync(a => a.MaTaiKhoan == id)).MaNhanVien; await f.Db.Set<NhanVien>().Where(n => n.MaNhanVien == eid).ExecuteUpdateAsync(s => s.SetProperty(n => n.TrangThai,"Inactive")); }
                if (invalid == "account") await f.Db.Set<TaiKhoan>().Where(a => a.MaTaiKhoan == id).ExecuteUpdateAsync(s => s.SetProperty(a => a.TrangThai,"Inactive"));
                if (invalid == "version") await f.Db.Set<TaiKhoan>().Where(a => a.MaTaiKhoan == id).ExecuteUpdateAsync(s => s.SetProperty(a => a.SecurityVersion,a => a.SecurityVersion+1));
                if (invalid == "session") await f.Db.Set<IdentitySession>().Where(s => s.AccountId == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt,DateTimeOffset.UtcNow));
                if (invalid == "expired") await f.Db.Set<IdentitySession>().Where(s => s.AccountId == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.CreatedAt,DateTimeOffset.UtcNow.AddHours(-2)).SetProperty(x => x.ExpiresAt,DateTimeOffset.UtcNow.AddMinutes(-1)));
                await Denied(client,401,"invalidSession");
            }
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task EmptyValidationMissingLotAndNoWriteRoutes()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            using var client = await Actor(f);
            Assert.Empty((await client.GetFromJsonAsync<InventoryBatchListResponse>(Root + "lots"))!.Items);
            await Seed(f); var before = await Snapshot(f);
            foreach (var query in new[] {"page=0","page=-1","pageSize=0","pageSize=101","page=2147483647&pageSize=100","page=oops","search="+new string('x',101)})
                await Problem(await client.GetAsync(Root + "lots?"+query),400,"validation");
            foreach (var query in new[] {"", "?lotNumber=", "?lotNumber="+new string('x',81), "?lotNumber=x&pageSize=101"})
                await Problem(await client.GetAsync(Root + "movements"+query),400,"validation");
            await Problem(await client.GetAsync(Movements("missing")),404,"resourceNotFound");
            foreach (var method in new[] {HttpMethod.Post,HttpMethod.Put,HttpMethod.Delete}) {
                using var request = new HttpRequestMessage(method,Root+"lots");
                using var response = await client.SendAsync(request); Assert.Equal(HttpStatusCode.MethodNotAllowed,response.StatusCode);
            }
            Assert.Equal(before,await Snapshot(f));
        });
    }
}
