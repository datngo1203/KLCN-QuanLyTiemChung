using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using TiemChung.Domain.Entities;
using TiemChung.Tests;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class InventoryReceiptHistorySqlClientTests
{
    [SqlTheory]
    [InlineData(false)]
    public async Task RealWindowsClientReadsReceiptAndCurrentStockSeparatelyThenClearsAfterRevocation(bool useKestrel)
    {
        await SqlFixtureLifecycle.RunAsync(()=>InventorySqlFixture.Create(useKestrel),async f=>{
            var db=f.Source.Db;
            db.Add(new TaiKhoan_QuyenHan{MaTaiKhoan=f.WarehouseAccountId,MaQuyen=(await db.Set<QuyenHan>().SingleAsync(p=>p.Code=="NhapKho")).MaQuyen});await db.SaveChangesAsync();
            using var http=f.Source.Factory.CreateClient();var identity=new IdentityApiService(http);Assert.True((await identity.LoginAsync("x1.warehouse",f.Source.Password)).Succeeded);
            var write=new InventoryReceivingService(identity);var vaccine=(await write.VaccinesAsync("",1)).Items.First();
            var day=DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
            var saved=await write.ReceiveAsync(new("[DEMO] History /+%?#",vaccine.Id,day.AddDays(-1),day.AddYears(1),40,"[DEMO] Supplier","[DEMO] DOC","[DEMO] Room",true));
            await db.Set<LoVacXin>().Where(l=>l.SoLo==saved.Received.LotNumber).ExecuteUpdateAsync(s=>s.SetProperty(l=>l.SoLuongTon,3));
            var before=await f.Source.Snapshot();var vm=new InventoryReceiptHistoryViewModel(identity){SearchText=saved.Received.DocumentNumber};await vm.LoadAsync();
            vm.Selected=Assert.Single(vm.Rows);Assert.Equal(40,vm.Selected.Data.Quantity);await vm.ReadDetailAsync();Assert.Contains("Nhập ban đầu",vm.DetailLabel,StringComparison.OrdinalIgnoreCase);
            Assert.Contains(saved.Received.DocumentNumber,vm.DetailLabel);Assert.Equal(before,await f.Source.Snapshot());Assert.Equal(1,await db.Set<InventoryReceipt>().CountAsync());
            Assert.Equal(1,await db.Set<SecurityAudit>().CountAsync(a=>a.Action=="inventory.received"));
            await identity.LogoutAsync();Assert.Empty(vm.Rows);Assert.DoesNotContain(saved.Received.DocumentNumber,vm.DetailLabel);
            Assert.True((await identity.LoginAsync("x1.warehouse",f.Source.Password)).Succeeded);
            var reopen=new InventoryReceiptHistoryViewModel(identity);await reopen.LoadAsync();reopen.Selected=Assert.Single(reopen.Rows);await reopen.ReadDetailAsync();Assert.Contains(saved.Received.DocumentNumber,reopen.DetailLabel);
            await db.Set<TaiKhoan_QuyenHan>().Where(g=>g.MaTaiKhoan==f.WarehouseAccountId && g.MaQuyen==db.Set<QuyenHan>().Single(p=>p.Code=="NhapKho").MaQuyen).ExecuteDeleteAsync();
            await reopen.ReadDetailAsync();Assert.True(reopen.NoAccess);Assert.Empty(reopen.Rows);Assert.Null(reopen.Selected);Assert.DoesNotContain(saved.Received.DocumentNumber,reopen.DetailLabel);
            Assert.Equal(before,await f.Source.Snapshot());await identity.LogoutAsync();
        });
    }
}
