using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using TiemChung.Domain.Entities;
using TiemChung.Tests;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class InventorySqlClientTests
{
    [SqlTheory]
    [InlineData(false)]
    public async Task WindowsReadsActualVaccinationStockMovementReopensAndLosesPermission(bool useKestrel)
    {
        await SqlFixtureLifecycle.RunAsync(()=>InventorySqlFixture.Create(useKestrel),async f=>{
            using var http=f.Source.Factory.CreateClient();var identity=new IdentityApiService(http);
            Assert.True((await identity.LoginAsync("x1.warehouse",f.Source.Password)).Succeeded);
            var vm=new InventoryViewModel(identity:identity);var before=await f.Source.Snapshot();
            await vm.LoadAsync();Assert.Equal(await f.Source.Db.Set<LoVacXin>().CountAsync(),vm.Batches.Count);
            var lot=await f.Source.Db.Set<BienDongKho>().Select(m=>m.SoLo).FirstAsync();
            vm.Selected=vm.Batches.Single(l=>l.Data.LotNumber==lot);await vm.LoadMovementsAsync();
            var sql=await f.Source.Db.Set<BienDongKho>().AsNoTracking().Where(m=>m.SoLo==lot).OrderByDescending(m=>m.ThoiDiem).ThenByDescending(m=>m.MaBienDong).ToArrayAsync();
            Assert.Equal(sql.Select(m=>m.MaBienDong),vm.Movements.Select(m=>m.Data.Id));
            Assert.All(vm.Movements,m=>{Assert.Equal("XuatDoTiem",m.Data.Type);Assert.Equal(1,m.Data.Quantity);});
            Assert.Equal((await f.Source.Db.Set<LoVacXin>().AsNoTracking().SingleAsync(l=>l.SoLo==lot)).SoLuongTon,vm.Selected.Data.Stock);
            Assert.Equal(before,await f.Source.Snapshot());
            await identity.LogoutAsync();Assert.Empty(vm.Batches);Assert.Empty(vm.Movements);
            Assert.True((await identity.LoginAsync("x1.warehouse",f.Source.Password)).Succeeded);
            var reopen=new InventoryViewModel(identity:identity);await reopen.LoadAsync();Assert.NotEmpty(reopen.Batches);
            await f.Source.Db.Set<TaiKhoan_QuyenHan>().Where(g=>g.MaTaiKhoan==f.WarehouseAccountId).ExecuteDeleteAsync();
            await reopen.LoadAsync();Assert.True(reopen.NoAccess);Assert.Empty(reopen.Batches);Assert.False(reopen.NoBatches);
            Assert.Equal(before,await f.Source.Snapshot());await identity.LogoutAsync();
        });
    }
}

// Reuses the existing disposable S5 source: its actual vaccination API created the stock movements.
public sealed class InventorySqlFixture : IAsyncDisposable
{
    public AftercareSqlFixture Source {get;private set;}=null!;
    public int WarehouseAccountId {get;private set;}
    public static async Task<InventorySqlFixture> Create(bool useKestrel=false)
    {
        var f=new InventorySqlFixture();
        try {
            f.Source=await AftercareSqlFixture.Create(useKestrel);var db=f.Source.Db;
            var employee=new NhanVien { HoTen="[DEMO] X1 Thủ kho",ChucDanh="ThuKho",TrangThai="Active" };
            db.Add(employee);await db.SaveChangesAsync();
            var account=new TaiKhoan { AccountType="Employee",MaNhanVien=employee.MaNhanVien,TenDangNhap="x1.warehouse",
                NormalizedUsername="X1.WAREHOUSE",TrangThai="Active",CreatedAt=DateTimeOffset.UtcNow };
            account.MatKhauHash=new PasswordHasher<TaiKhoan>().HashPassword(account,f.Source.Password);
            db.Add(account);await db.SaveChangesAsync();f.WarehouseAccountId=account.MaTaiKhoan;
            db.Add(new TaiKhoan_QuyenHan { MaTaiKhoan=account.MaTaiKhoan,MaQuyen=(await db.Set<QuyenHan>().SingleAsync(p=>p.Code=="XemKho")).MaQuyen });
            await db.SaveChangesAsync();return f;
        } catch(Exception error) { await SqlFixtureLifecycle.CleanupAfterFailureAsync(error,f.DisposeAsync);throw; }
    }
    public ValueTask DisposeAsync()=>Source?.DisposeAsync()??ValueTask.CompletedTask;
}
