using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using Xunit;
using Fixture=TiemChung.Tests.AppointmentSqlTests.Fixture;

namespace TiemChung.Tests;

public sealed class InventoryReceiptHistorySqlTests
{
    private const string Route="/api/v1/inventory/receipts/history";
    private static async Task<HttpClient> Warehouse(Fixture f)
    {
        var client=await f.Actor("ThuKho",false);
        foreach(var code in new[]{"XemKho","NhapKho"})f.Db.Add(new TaiKhoan_QuyenHan {
            MaTaiKhoan=f.LastAccountId,MaQuyen=(await f.Db.Set<QuyenHan>().SingleAsync(p=>p.Code==code)).MaQuyen });
        await f.Db.SaveChangesAsync();return client;
    }
    private static async Task<InventoryReceiptDto> Receive(Fixture f,HttpClient client,int number)
    {
        var today=DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        var request=new ReceiveInventoryRequest($"[DEMO] X221 /+%?# {number:00}",(await f.Db.Vaccines.FirstAsync()).MaVacXin,
            today.AddMonths(-1),today.AddYears(1),number+10,$"[DEMO] Supplier {number:00}",$"[DEMO] DOC {number:00}","[DEMO] Room",true);
        using var response=await client.PostAsJsonAsync("/api/v1/inventory/receipts",request);
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);return (await response.Content.ReadFromJsonAsync<InventoryReceiptDto>())!;
    }
    private static async Task<InventoryReceiptHistoryListResponse> List(HttpClient client,string query="")
    {
        using var response=await client.GetAsync(Route+query);Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        Assert.Equal("no-store",response.Headers.CacheControl!.ToString());return (await response.Content.ReadFromJsonAsync<InventoryReceiptHistoryListResponse>())!;
    }
    private static async Task<string> Snapshot(Fixture f)
    {
        var tables=await f.Db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sys.tables WHERE is_ms_shipped=0 ORDER BY name").ToArrayAsync();
        var rows=new List<string>();await f.Db.Database.OpenConnectionAsync();
        try { foreach(var table in tables){await using var cmd=f.Db.Database.GetDbConnection().CreateCommand();
            cmd.CommandText=$"SELECT (SELECT * FROM [{table.Replace("]","]]")}] FOR JSON PATH, INCLUDE_NULL_VALUES)";
            rows.Add(Convert.ToString(await cmd.ExecuteScalarAsync())!);
        }}finally{await f.Db.Database.CloseConnectionAsync();}return string.Join("\n",rows);
    }

    [Fact,Trait("Category","SqlIntegration")]
    public async Task RealReceiptsHaveExactPageBoundariesStableTiesAndGetNeverWritesAnyTable()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            using var client=await Warehouse(f);var receipts=new List<InventoryReceiptDto>();
            foreach(var count in new[]{0,1,25,26,57}){
                while(receipts.Count<count)receipts.Add(await Receive(f,client,receipts.Count));
                var before=await Snapshot(f);var actual=new List<int>();
                for(var page=1;page<=Math.Max(1,(count+24)/25)+1;page++){
                    var list=await List(client,"?page="+page);
                    Assert.Equal(Math.Clamp(count-(page-1)*25,0,25),list.Items.Length);
                    Assert.Equal(count>page*25,list.HasMore);actual.AddRange(list.Items.Select(r=>r.MovementId));
                }
                Assert.Equal(receipts.Select(r=>r.MovementId).Reverse(),actual);Assert.Equal(count,actual.Distinct().Count());
                Assert.Equal(before,await Snapshot(f));
            }
            var time=new DateTime(2026,1,1,1,2,3,DateTimeKind.Utc);
            await f.Db.Set<BienDongKho>().ExecuteUpdateAsync(s=>s.SetProperty(m=>m.ThoiDiem,time));
            var tied=new List<int>();for(var page=1;page<=3;page++)tied.AddRange((await List(client,"?page="+page)).Items.Select(r=>r.MovementId));
            Assert.Equal(receipts.Select(r=>r.MovementId).Reverse(),tied);
            var chosen=receipts[32];
            foreach(var term in new[]{chosen.Received.LotNumber,chosen.Received.SupplierName,chosen.Received.DocumentNumber}){
                var row=Assert.Single((await List(client,"?search="+Uri.EscapeDataString(" "+term+" "))).Items);
                Assert.Equal(chosen.MovementId,row.MovementId);Assert.Equal(time,row.RecordedAt.UtcDateTime);Assert.Equal(TimeSpan.Zero,row.RecordedAt.Offset);
            }
            Assert.Equal(25,(await List(client,"?search="+Uri.EscapeDataString(chosen.VaccineName))).Items.Length);
            await f.Db.Set<LoVacXin>().Where(l=>l.SoLo==chosen.Received.LotNumber).ExecuteUpdateAsync(s=>s.SetProperty(l=>l.SoLuongTon,0).SetProperty(l=>l.TrangThai,"Inactive"));
            var reread=Assert.Single((await List(client,"?search="+Uri.EscapeDataString(chosen.Received.LotNumber))).Items);
            Assert.Equal(chosen.Received.Quantity,reread.Quantity);Assert.Equal(chosen.Received.DocumentNumber,reread.DocumentNumber);
            // Legacy inventory may even have an incoming movement; absence of receipt still excludes it.
            f.Db.Add(new LoVacXin{SoLo="[DEMO] LEGACY",MaVacXin=chosen.Received.VaccineId,NgaySanXuat=chosen.Received.ManufacturedOn,HanSuDung=chosen.Received.ExpiresOn,SoLuongBanDau=20,SoLuongTon=20,TrangThai="Active"});await f.Db.SaveChangesAsync();
            f.Db.Add(new BienDongKho{SoLo="[DEMO] LEGACY",MaNhanVien=f.EmployeeId,LoaiBienDong="NhapKho",SoLuong=20,ThoiDiem=time});await f.Db.SaveChangesAsync();
            var snapshot=await Snapshot(f);Assert.Empty((await List(client,"?search=LEGACY")).Items);
            foreach(var bad in new[]{"?page=no","?page=0","?page=-1","?page=85899347","?search="+new string('a',101)}){
                using var response=await client.GetAsync(Route+bad);Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);Assert.Equal("no-store",response.Headers.CacheControl!.ToString());
            }
            foreach(var method in new[]{HttpMethod.Post,HttpMethod.Put,HttpMethod.Delete}){using var req=new HttpRequestMessage(method,Route);using var response=await client.SendAsync(req);Assert.Equal(HttpStatusCode.MethodNotAllowed,response.StatusCode);}
            Assert.Equal(snapshot,await Snapshot(f));
        });
    }

    [Fact,Trait("Category","SqlIntegration")]
    public async Task ReceiptCannotBorrowIncomingMovementFromAnotherLot()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            using var client=await Warehouse(f);var a=await Receive(f,client,1);var b=await Receive(f,client,2);
            await f.Db.Set<InventoryReceipt>().Where(r=>r.SoLo==b.Received.LotNumber).ExecuteDeleteAsync();
            await f.Db.Set<InventoryReceipt>().Where(r=>r.SoLo==a.Received.LotNumber).ExecuteUpdateAsync(s=>s.SetProperty(r=>r.MovementId,b.MovementId));
            var before=await Snapshot(f);Assert.Empty((await List(client)).Items);Assert.Equal(before,await Snapshot(f));
        });
    }

    [Fact,Trait("Category","SqlIntegration")]
    public async Task NonIncomingOrInconsistentQuantityIsNotAnX21Receipt()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            using var client=await Warehouse(f);await Receive(f,client,1);
            await f.Db.Set<BienDongKho>().ExecuteUpdateAsync(s=>s.SetProperty(m=>m.LoaiBienDong,"Legacy"));Assert.Empty((await List(client)).Items);
            await f.Db.Set<BienDongKho>().ExecuteUpdateAsync(s=>s.SetProperty(m=>m.LoaiBienDong,"NhapKho").SetProperty(m=>m.SoLuong,999));Assert.Empty((await List(client)).Items);
        });
    }

    [Fact,Trait("Category","SqlIntegration")]
    public async Task SqlDependencyFailureIs503NoStoreWithoutDatabaseDetails()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            using var client=await Warehouse(f);
            await f.Db.Database.ExecuteSqlRawAsync("EXEC sp_rename N'dbo.InventoryReceipt', N'X221FixtureUnavailable'");
            try{using var response=await client.GetAsync(Route);Assert.Equal(HttpStatusCode.ServiceUnavailable,response.StatusCode);Assert.Equal("no-store",response.Headers.CacheControl!.ToString());
                var text=await response.Content.ReadAsStringAsync();Assert.Contains("dependencyUnavailable",text);Assert.DoesNotContain("X221FixtureUnavailable",text);Assert.DoesNotContain("SqlException",text);
            }finally{await f.Db.Database.ExecuteSqlRawAsync("EXEC sp_rename N'dbo.X221FixtureUnavailable', N'InventoryReceipt'");}
        });
    }
}
