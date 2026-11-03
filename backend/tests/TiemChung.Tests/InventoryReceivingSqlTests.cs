using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using Xunit;
using Fixture=TiemChung.Tests.AppointmentSqlTests.Fixture;

namespace TiemChung.Tests;

public sealed class InventoryReceivingSqlTests
{
    private const string Root="/api/v1/inventory/receipts";
    private static string Get(string lot)=>Root+"?lotNumber="+Uri.EscapeDataString(lot);
    private static async Task<ReceiveInventoryRequest> Request(Fixture f,string lot="[DEMO] X21 / +%?#")
    {
        var today=DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        return new(lot,await f.Db.Vaccines.Select(x => x.MaVacXin).FirstAsync(),today.AddMonths(-6),today.AddYears(1),100,"[DEMO] Supplier","[DEMO] DOC 001","[DEMO] Cold room A",true);
    }
    private static async Task<HttpClient> Actor(Fixture f,string? title="ThuKho",bool read=true,bool write=true)
    {
        var client=await f.Actor(title,false);
        foreach(var code in new[]{read?"XemKho":null,write?"NhapKho":null}.OfType<string>())
            f.Db.Add(new TaiKhoan_QuyenHan{MaTaiKhoan=f.LastAccountId,MaQuyen=(await f.Db.Set<QuyenHan>().SingleAsync(p=>p.Code==code)).MaQuyen});
        await f.Db.SaveChangesAsync();return client;
    }
    private static async Task Status(HttpResponseMessage response,HttpStatusCode status)
    {
        using(response){Assert.Equal(status,response.StatusCode);Assert.Equal("no-store",response.Headers.CacheControl!.ToString());Assert.DoesNotContain("SqlException",await response.Content.ReadAsStringAsync());}
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task ReceiveCommitsOneLotMovementReceiptAndAuditAndX1ReadsSqlAgain()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            var req=await Request(f);using var client=await Actor(f);var account=f.LastAccountId;
            using var response=await client.PostAsJsonAsync(Root,req);Assert.Equal(HttpStatusCode.Created,response.StatusCode);
            Assert.Equal("no-store",response.Headers.CacheControl!.ToString());
            var saved=(await response.Content.ReadFromJsonAsync<InventoryReceiptDto>())!;Assert.Equal(req,saved.Received);
            var persisted=(await client.GetFromJsonAsync<InventoryReceiptDto>(Get(req.LotNumber)))!;Assert.Equal(saved,persisted);
            var lot=await f.Db.Set<LoVacXin>().AsNoTracking().SingleAsync();Assert.Equal(100,lot.SoLuongTon);Assert.Equal(100,lot.SoLuongBanDau);Assert.Equal("Active",lot.TrangThai);
            var m=await f.Db.Set<BienDongKho>().AsNoTracking().SingleAsync();Assert.Equal(saved.MovementId,m.MaBienDong);Assert.Equal("NhapKho",m.LoaiBienDong);Assert.Null(m.MaMuiTiem);Assert.Equal(100,m.SoLuong);Assert.Equal(saved.RecordedAt.UtcDateTime,m.ThoiDiem);
            var audit=await f.Db.Set<SecurityAudit>().SingleAsync(a=>a.Action=="inventory.received");Assert.Equal(account,audit.ActorAccountId);Assert.NotNull(audit.SessionId);Assert.Equal(req.LotNumber,audit.ResourceId);
            Assert.Equal(100,Assert.Single((await client.GetFromJsonAsync<InventoryBatchListResponse>("/api/v1/inventory/lots"))!.Items).Stock);
            Assert.Equal(saved.MovementId,Assert.Single((await client.GetFromJsonAsync<InventoryMovementListResponse>("/api/v1/inventory/movements?lotNumber="+Uri.EscapeDataString(req.LotNumber)))!.Items).Id);
            await Status(await client.PostAsJsonAsync(Root,req with{LotNumber=req.LotNumber.ToLowerInvariant(),Quantity=20}),HttpStatusCode.Conflict);
            await f.Db.Set<LoVacXin>().ExecuteUpdateAsync(s=>s.SetProperty(l=>l.SoLuongTon,99));
            Assert.Equal(100,(await client.GetFromJsonAsync<InventoryReceiptDto>(Get(req.LotNumber)))!.Received.Quantity);
            Assert.Single(await f.Db.Set<InventoryReceipt>().ToArrayAsync());Assert.Single(await f.Db.Set<BienDongKho>().ToArrayAsync());Assert.Empty(await f.Db.Set<MuiTiem>().ToArrayAsync());
            Assert.Empty(await f.Db.Payments.ToArrayAsync());Assert.Empty(await f.Db.Registrations.ToArrayAsync());
        });
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task InvalidRequestsAndInactiveVaccinesCannotWriteAndOldLotsCannotBeToppedUp()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            var req=await Request(f);using var client=await Actor(f);
            var today=DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
            foreach(var invalid in new[]{req with{LotNumber=" "},req with{LotNumber=new string('a',81)},req with{Quantity=0},req with{Quantity=-1},req with{VaccineId=0},req with{VaccineId=int.MaxValue},
                req with{ManufacturedOn=default},req with{ExpiresOn=today.AddDays(-1)},req with{ManufacturedOn=today.AddDays(1)},req with{ManufacturedOn=req.ExpiresOn.AddDays(1)},
                req with{SupplierName=null!},req with{SupplierName=new string('s',201)},req with{DocumentNumber=" "},req with{StorageLocation=new string('s',201)},req with{InspectionConfirmed=false}})
                await Status(await client.PostAsJsonAsync(Root,invalid),HttpStatusCode.BadRequest);
            foreach(var body in new[]{"{}","null","{","{\"quantity\":2147483648}"})await Status(await client.PostAsync(Root,new StringContent(body,Encoding.UTF8,"application/json")),HttpStatusCode.BadRequest);
            await f.Db.Vaccines.ExecuteUpdateAsync(s=>s.SetProperty(v=>v.TrangThai,"Inactive"));
            await Status(await client.PostAsJsonAsync(Root,req),HttpStatusCode.BadRequest);
            Assert.Empty((await client.GetFromJsonAsync<InventoryVaccineListResponse>("/api/v1/inventory/receiving-vaccines"))!.Items);
            Assert.Empty(await f.Db.Set<LoVacXin>().ToArrayAsync());Assert.Empty(await f.Db.Set<BienDongKho>().ToArrayAsync());Assert.Empty(await f.Db.Set<InventoryReceipt>().ToArrayAsync());
            await f.Db.Vaccines.ExecuteUpdateAsync(s=>s.SetProperty(v=>v.TrangThai,"Active"));
            f.Db.Add(new LoVacXin{SoLo=req.LotNumber,MaVacXin=req.VaccineId,NgaySanXuat=req.ManufacturedOn,HanSuDung=req.ExpiresOn,SoLuongBanDau=10,SoLuongTon=7,TrangThai="Active"});await f.Db.SaveChangesAsync();
            await Status(await client.PostAsJsonAsync(Root,req),HttpStatusCode.Conflict);await Status(await client.GetAsync(Get(req.LotNumber)),HttpStatusCode.NotFound);
            Assert.Equal(7,(await f.Db.Set<LoVacXin>().AsNoTracking().SingleAsync()).SoLuongTon);
        });
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task OnlyCurrentActiveWarehouseWithBothGrantsCanUseAllReceiptRoutes()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            var req=await Request(f);
            async Task Denied(HttpClient client,HttpStatusCode status){await Status(await client.PostAsJsonAsync(Root,req),status);await Status(await client.GetAsync(Get(req.LotNumber)),status);await Status(await client.GetAsync("/api/v1/inventory/receiving-vaccines"),status);await Status(await client.GetAsync(Root+"/history"),status);}
            using var anonymous=f.Base.Factory.CreateClient();await Denied(anonymous,HttpStatusCode.Unauthorized);
            foreach(var title in new string?[]{null,"Admin","BacSi","LeTan","DieuDuong","ThuNgan"}){using var client=await Actor(f,title);await Denied(client,HttpStatusCode.Forbidden);}
            using var readOnly=await Actor(f,write:false);await Denied(readOnly,HttpStatusCode.Forbidden);
            using var writeOnly=await Actor(f,read:false);await Denied(writeOnly,HttpStatusCode.Forbidden);
            foreach(var scenario in new[]{"grant","account","employee","session","version"}){
                using var client=await Actor(f);var id=f.LastAccountId;
                if(scenario=="grant")await f.Db.Set<TaiKhoan_QuyenHan>().Where(g=>g.MaTaiKhoan==id).ExecuteDeleteAsync();
                if(scenario=="account")await f.Db.Set<TaiKhoan>().Where(a=>a.MaTaiKhoan==id).ExecuteUpdateAsync(s=>s.SetProperty(a=>a.TrangThai,"Inactive"));
                if(scenario=="employee"){var e=(await f.Db.Set<TaiKhoan>().SingleAsync(a=>a.MaTaiKhoan==id)).MaNhanVien;await f.Db.Set<NhanVien>().Where(n=>n.MaNhanVien==e).ExecuteUpdateAsync(s=>s.SetProperty(n=>n.TrangThai,"Inactive"));}
                if(scenario=="session")await f.Db.Set<IdentitySession>().Where(s=>s.AccountId==id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.RevokedAt,DateTimeOffset.UtcNow));
                if(scenario=="version")await f.Db.Set<TaiKhoan>().Where(a=>a.MaTaiKhoan==id).ExecuteUpdateAsync(s=>s.SetProperty(a=>a.SecurityVersion,a=>a.SecurityVersion+1));
                await Denied(client,scenario=="grant"?HttpStatusCode.Forbidden:HttpStatusCode.Unauthorized);
            }
            Assert.Empty(await f.Db.Set<LoVacXin>().ToArrayAsync());
        });
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task TwoDifferentWarehousesRacingSameLotCommitExactlyOnce()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            var req=await Request(f);using var a=await Actor(f);using var b=await Actor(f);
            var results=await Task.WhenAll(a.PostAsJsonAsync(Root,req),b.PostAsJsonAsync(Root,req));
            Assert.Single(results,r=>r.StatusCode==HttpStatusCode.Created);Assert.Single(results,r=>r.StatusCode==HttpStatusCode.Conflict);foreach(var r in results)r.Dispose();
            Assert.Single(await f.Db.Set<InventoryReceipt>().ToArrayAsync());Assert.Single(await f.Db.Set<BienDongKho>().ToArrayAsync());Assert.Equal(100,(await f.Db.Set<LoVacXin>().SingleAsync()).SoLuongTon);
            Assert.Single(await f.Db.Set<SecurityAudit>().Where(a=>a.Action=="inventory.received").ToArrayAsync());
        });
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task ExistingAdministrationCanGrantSeparateReceivePermissionAndRevokesOldSession()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            using var warehouse=await Actor(f,write:false);var id=f.LastAccountId;
            f.Db.ChangeTracker.Clear(); // Operator runs with a fresh context; discard fixture setup tracking.
            await new TiemChung.IdentityOperator.IdentityAdministration(f.Db).ChangeAsync("s2.seed",f.Base.Password,id,
                "set-extra-permissions",["XemKho","NhapKho"],CancellationToken.None);
            var codes=await (from g in f.Db.Set<TaiKhoan_QuyenHan>() join p in f.Db.Set<QuyenHan>() on g.MaQuyen equals p.MaQuyen where g.MaTaiKhoan==id select p.Code).ToArrayAsync();
            Assert.Contains("NhapKho",codes);Assert.Contains("XemKho",codes);Assert.Equal(2,codes.Length);
            await Status(await warehouse.GetAsync("/api/v1/inventory/receiving-vaccines"),HttpStatusCode.Unauthorized);
        });
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task MigrationPreservesLegacyLotsMovementsAndDoesNotGrantReceivePermission()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            await f.Db.GetService<IMigrator>().MigrateAsync("20261101134612_S5ReminderSimulation");
            var req=await Request(f);var lot=new LoVacXin{SoLo=req.LotNumber,MaVacXin=req.VaccineId,NgaySanXuat=req.ManufacturedOn,HanSuDung=req.ExpiresOn,SoLuongBanDau=100,SoLuongTon=7,TrangThai="Inactive"};f.Db.Add(lot);await f.Db.SaveChangesAsync();
            var movement=new BienDongKho{SoLo=req.LotNumber,MaNhanVien=f.EmployeeId,LoaiBienDong="Legacy",SoLuong=93,ThoiDiem=DateTime.UtcNow};f.Db.Add(movement);await f.Db.SaveChangesAsync();
            var before=(byte[])f.Db.Entry(lot).Property("RowVersion").CurrentValue!;
            await f.Db.GetService<IMigrator>().MigrateAsync();await f.Db.Entry(lot).ReloadAsync();Assert.Equal(before,(byte[])f.Db.Entry(lot).Property("RowVersion").CurrentValue!);Assert.Equal(7,lot.SoLuongTon);Assert.Equal("Inactive",lot.TrangThai);
            Assert.Single(await f.Db.Set<BienDongKho>().ToArrayAsync());Assert.Empty(await f.Db.Set<InventoryReceipt>().ToArrayAsync());
            var permission=await f.Db.Set<QuyenHan>().SingleAsync(p=>p.Code=="NhapKho");Assert.False(await f.Db.Set<TaiKhoan_QuyenHan>().AnyAsync(g=>g.MaQuyen==permission.MaQuyen));
            await f.Db.Database.MigrateAsync();Assert.Single(await f.Db.Set<QuyenHan>().Where(p=>p.Code=="NhapKho").ToArrayAsync());
        });
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task AuditFailureRollsBackReceiptLotAndMovementThenRetryCanSucceed()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            using var client=await Actor(f);var req=await Request(f);
            await f.Db.Database.ExecuteSqlRawAsync("ALTER TABLE SecurityAudit ADD CONSTRAINT X21FixtureFailure CHECK (Action <> 'inventory.received')");
            await Status(await client.PostAsJsonAsync(Root,req),HttpStatusCode.ServiceUnavailable);
            Assert.Empty(await f.Db.Set<LoVacXin>().ToArrayAsync());Assert.Empty(await f.Db.Set<BienDongKho>().ToArrayAsync());Assert.Empty(await f.Db.Set<InventoryReceipt>().ToArrayAsync());
            await f.Db.Database.ExecuteSqlRawAsync("ALTER TABLE SecurityAudit DROP CONSTRAINT X21FixtureFailure");
            await Status(await client.PostAsJsonAsync(Root,req),HttpStatusCode.Created);
            await Assert.ThrowsAnyAsync<Exception>(()=>f.Db.GetService<IMigrator>().MigrateAsync("20261101134612_S5ReminderSimulation"));
            Assert.Single(await f.Db.Set<InventoryReceipt>().ToArrayAsync());
        });
    }

    [Fact,Trait("Category","SqlIntegration")]
    public async Task ReceiptHistorySearchesSnapshotsWithoutChangingInventoryOrShowingLegacyLots()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            var first=await Request(f,"[DEMO] HIST 01");
            using var client=await Actor(f);
            using(var response=await client.GetAsync(Root+"/history")) {
                Assert.Equal(HttpStatusCode.OK,response.StatusCode);
                Assert.Equal("no-store",response.Headers.CacheControl!.ToString());
                var empty=(await response.Content.ReadFromJsonAsync<InventoryReceiptHistoryListResponse>())!;
                Assert.Empty(empty.Items);Assert.False(empty.HasMore);
            }
            await Status(await client.PostAsJsonAsync(Root,first),HttpStatusCode.Created);
            var second=first with{LotNumber="[DEMO] HIST 02 / +%?#",Quantity=23,
                SupplierName="[DEMO] Special Supplier",DocumentNumber="[DEMO] DOC Z2"};
            await Status(await client.PostAsJsonAsync(Root,second),HttpStatusCode.Created);
            // Legacy lots do not have InventoryReceipt and must not be shown as X2.1 receipts.
            f.Db.Add(new LoVacXin{SoLo="[DEMO] LEGACY",MaVacXin=first.VaccineId,NgaySanXuat=first.ManufacturedOn,
                HanSuDung=first.ExpiresOn,SoLuongBanDau=70,SoLuongTon=70,TrangThai="Active"});
            await f.Db.SaveChangesAsync();
            var all=(await client.GetFromJsonAsync<InventoryReceiptHistoryListResponse>(Root+"/history"))!;
            Assert.Equal(2,all.Items.Length);Assert.False(all.HasMore);
            Assert.DoesNotContain(all.Items,x=>x.LotNumber.Contains("LEGACY"));
            var row=Assert.Single(all.Items,x=>x.LotNumber==second.LotNumber);
            Assert.Equal(23,row.Quantity);Assert.Equal(second.DocumentNumber,row.DocumentNumber);
            Assert.Equal(second.SupplierName,row.SupplierName);Assert.Equal(second.StorageLocation,row.StorageLocation);
            Assert.True(row.MovementId>0);Assert.True(row.EmployeeId>0);
            Assert.Equal(TimeSpan.Zero,row.RecordedAt.Offset);
            foreach(var term in new[]{second.LotNumber,second.SupplierName,second.DocumentNumber,row.VaccineName}) {
                var list=(await client.GetFromJsonAsync<InventoryReceiptHistoryListResponse>(
                    Root+"/history?search="+Uri.EscapeDataString(term)))!;
                Assert.Contains(list.Items,x=>x.LotNumber==second.LotNumber);
            }
            var none=(await client.GetFromJsonAsync<InventoryReceiptHistoryListResponse>(Root+"/history?search=does-not-exist"))!;
            Assert.Empty(none.Items);Assert.False(none.HasMore);
            var page2=(await client.GetFromJsonAsync<InventoryReceiptHistoryListResponse>(Root+"/history?page=2"))!;
            Assert.Empty(page2.Items);Assert.False(page2.HasMore);
            foreach(var bad in new[]{Root+"/history?page=0",Root+"/history?page=-1",Root+"/history?page=2147483647",
                Root+"/history?search="+new string('Q',101)}) await Status(await client.GetAsync(bad),HttpStatusCode.BadRequest);
            await f.Db.Set<LoVacXin>().Where(x=>x.SoLo==second.LotNumber).ExecuteUpdateAsync(
                x=>x.SetProperty(l=>l.SoLuongTon,1));
            var again=(await client.GetFromJsonAsync<InventoryReceiptHistoryListResponse>(Root+"/history?search=HIST%2002"))!;
            Assert.Equal(23,Assert.Single(again.Items).Quantity); // source is immutable receipt movement
            Assert.Equal(2,await f.Db.Set<InventoryReceipt>().CountAsync());
            Assert.Equal(2,await f.Db.Set<BienDongKho>().CountAsync());
            Assert.Equal(2,await f.Db.Set<SecurityAudit>().CountAsync(x=>x.Action=="inventory.received"));
            Assert.Empty(await f.Db.Set<MuiTiem>().ToArrayAsync());
            Assert.Empty(await f.Db.Payments.ToArrayAsync());
        });
    }

    [Fact,Trait("Category","SqlIntegration")]
    public async Task ReceiptHistoryUsesStablePagesAndCorrectHasMoreAcrossTwentySixReceipts()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            var template=await Request(f);
            using var client=await Actor(f);
            for(var i=0;i<26;i++) {
                var request=template with { LotNumber=$"[DEMO] HISTORY PAGE {i:00}", DocumentNumber=$"[DEMO] DOC {i:00}" };
                await Status(await client.PostAsJsonAsync(Root,request),HttpStatusCode.Created);
            }
            var first=(await client.GetFromJsonAsync<InventoryReceiptHistoryListResponse>(Root+"/history?page=1"))!;
            var second=(await client.GetFromJsonAsync<InventoryReceiptHistoryListResponse>(Root+"/history?page=2"))!;
            Assert.Equal(25,first.Items.Length);Assert.True(first.HasMore);
            Assert.Single(second.Items);Assert.False(second.HasMore);
            Assert.Equal(26,first.Items.Concat(second.Items).Select(x=>x.LotNumber).Distinct().Count());
            Assert.Equal(26,await f.Db.Set<InventoryReceipt>().CountAsync());
            Assert.Equal(26,await f.Db.Set<BienDongKho>().CountAsync(m=>m.LoaiBienDong=="NhapKho"));
            Assert.Empty(await f.Db.Set<MuiTiem>().ToArrayAsync());
        });
    }

}
