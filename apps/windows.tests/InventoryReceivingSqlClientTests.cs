using System.Net;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using TiemChung.Domain.Entities;
using TiemChung.Tests;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class InventoryReceivingSqlClientTests
{
    [SqlTheory]
    [InlineData(false)][InlineData(true)]
    public async Task RealClientReceivesReadsBackAndReconcilesLostCommitWithoutDuplicating(bool loseResponse)
    {
        await SqlFixtureLifecycle.RunAsync(()=>InventorySqlFixture.Create(),async f=>{
            var db=f.Source.Db;
            db.Add(new TaiKhoan_QuyenHan{MaTaiKhoan=f.WarehouseAccountId,MaQuyen=(await db.Set<QuyenHan>().SingleAsync(p=>p.Code=="NhapKho")).MaQuyen});await db.SaveChangesAsync();
            var baseline=await f.Source.Snapshot();var transport=new LostResponse(f.Source.Factory.Server.CreateHandler()){Armed=loseResponse};using var http=new HttpClient(transport){BaseAddress=f.Source.Client.BaseAddress};
            var identity=new IdentityApiService(http);Assert.True((await identity.LoginAsync("x1.warehouse",f.Source.Password)).Succeeded);
            var vm=new InventoryReceivingViewModel(identity);await vm.LoadVaccinesAsync();vm.Vaccine=vm.Vaccines.First();
            vm.LotNumber=" [DEMO] X21 Windows /+ ";vm.Quantity="20";vm.ManufacturedOn=DateTime.Today.AddDays(-1);vm.ExpiresOn=DateTime.Today.AddYears(1);
            vm.SupplierName="[DEMO] Supplier";vm.DocumentNumber="[DEMO] DOC";vm.StorageLocation="[DEMO] Room";vm.InspectionConfirmed=true;
            Assert.NotNull(vm.BeginConfirmation());await vm.ConfirmAsync(true);
            if(loseResponse){Assert.Null(vm.Result);Assert.False(vm.CanRetry);await vm.ReconcileAsync();}
            var saved=Assert.IsType<InventoryReceiptDto>(vm.Result);Assert.Equal(20,saved.Received.Quantity);Assert.Equal(1,transport.Posts);
            Assert.Single(await db.Set<InventoryReceipt>().ToArrayAsync());Assert.Equal(20,(await db.Set<LoVacXin>().SingleAsync(l=>l.SoLo==saved.Received.LotNumber)).SoLuongTon);
            Assert.Equal(1,await db.Set<BienDongKho>().CountAsync(m=>m.LoaiBienDong=="NhapKho"));Assert.Equal(1,await db.Set<SecurityAudit>().CountAsync(a=>a.Action=="inventory.received"));
            Assert.Equal(3,await db.Set<MuiTiem>().CountAsync());Assert.Equal(3,await db.Set<BienDongKho>().CountAsync(m=>m.LoaiBienDong=="XuatDoTiem"));
            var inventory=new InventoryViewModel(identity:identity){SearchText=saved.Received.LotNumber};await inventory.LoadAsync();inventory.Selected=Assert.Single(inventory.Batches);await inventory.LoadMovementsAsync();Assert.Equal(20,Assert.Single(inventory.Movements).Data.Quantity);
            await identity.LogoutAsync();Assert.Null(vm.Result);Assert.Equal("",vm.LotNumber);
            Assert.True((await identity.LoginAsync("x1.warehouse",f.Source.Password)).Succeeded);Assert.Equal(saved,await new InventoryReceivingService(identity).ReceiptAsync(saved.Received.LotNumber));await identity.LogoutAsync();
        });
    }
    private sealed class LostResponse(HttpMessageHandler inner):DelegatingHandler(inner)
    {
        public bool Armed;public int Posts;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            var post=request.Method==HttpMethod.Post&&request.RequestUri!.AbsolutePath.EndsWith("/inventory/receipts");if(post)Posts++;
            var response=await base.SendAsync(request,ct);if(post&&Armed&&response.StatusCode==HttpStatusCode.Created){Armed=false;response.Dispose();throw new HttpRequestException("Synthetic lost receipt response");}return response;
        }
    }
}
