using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class AppointmentSqlClientTests
{
    [SqlTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WindowsCreatesBothKindsThroughRealApiSqlAndReconcilesLostCommitResponse(bool loseResponse)
    {
        await using var f=await PaymentSqlFixture.Create();
        var employee=new NhanVien {HoTen="[DEMO] S5 client",ChucDanh="LeTan",TrangThai="Active"};f.Db.Add(employee);await f.Db.SaveChangesAsync();
        var account=new TaiKhoan {AccountType="Employee",MaNhanVien=employee.MaNhanVien,TenDangNhap="s5.client",NormalizedUsername="S5.CLIENT",TrangThai="Active",CreatedAt=DateTimeOffset.UtcNow};account.MatKhauHash=new PasswordHasher<TaiKhoan>().HashPassword(account,f.Password);f.Db.Add(account);await f.Db.SaveChangesAsync();
        f.Db.Add(new TaiKhoan_QuyenHan {MaTaiKhoan=account.MaTaiKhoan,MaQuyen=(await f.Db.Set<QuyenHan>().SingleAsync(x=>x.Code=="QuanLyLichHen")).MaQuyen});await f.Db.SaveChangesAsync();
        var handler=new LostResponse(f.Factory.Server.CreateHandler());var identity=new IdentityApiService(new HttpClient(handler){BaseAddress=f.Client.BaseAddress});Assert.True((await identity.LoginAsync("s5.client",f.Password)).Succeeded);
        var api=new AppointmentApiService(identity);var vm=new AppointmentViewModel(api,identity);
        foreach(var kind in new[]{"vaccine","package"}) {
            await vm.NewAsync();vm.SelectedSource=Assert.Single(vm.Sources);await vm.LoadSourceDetailAsync();vm.SelectedItem=vm.Items.Single(x=>x.Kind==kind);
            vm.AppointmentDate=DateTime.Today.AddDays(2);vm.Time="09:15";vm.Location="[DEMO] Windows SQL "+kind;Assert.True(vm.CanSave);
            handler.Armed=loseResponse;await vm.SaveAsync();
            if(loseResponse){Assert.True(vm.HasPending);Assert.False(vm.CanSave);await vm.SaveAsync();await vm.ReconcileAsync();}
            Assert.False(vm.HasPending);Assert.NotNull(vm.Detail);Assert.Equal(kind,vm.Detail.ItemKind);
            var detail=await api.GetAsync(vm.Detail.Id);Assert.Equal(vm.Detail,detail);
            await using var fresh=f.NewDb();var row=await fresh.Set<LichHenTiem>().SingleAsync(x=>x.MaLichHen==detail.Id);
            Assert.Equal(f.RegistrationId,row.MaDangKy);Assert.Equal(detail.Recipient.Id,row.MaNguoiDuocTiem);Assert.Equal(detail.ScheduledAt.UtcDateTime,row.ThoiGianHen);Assert.Equal(vm.Detail.Location,row.DiemTiem);
            var audit=Assert.Single(await fresh.Set<SecurityAudit>().Where(x=>x.Action=="appointments.created"&&x.ResourceId==row.MaLichHen.ToString()).ToArrayAsync());Assert.Equal(account.MaTaiKhoan,audit.ActorAccountId);
        }
        Assert.Equal(2,handler.PostCount);Assert.Equal(2,await f.Db.Set<LichHenTiem>().CountAsync());
        await vm.LoadAsync();Assert.Equal(2,vm.Appointments.Count);await identity.LogoutAsync();Assert.Empty(vm.Appointments);Assert.Null(vm.Detail);
    }
    private sealed class LostResponse(HttpMessageHandler inner):DelegatingHandler(inner)
    {
        public bool Armed;public int PostCount;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            var post=request.Method==HttpMethod.Post&&request.RequestUri!.AbsolutePath=="/api/v1/appointments";if(post)PostCount++;
            var response=await base.SendAsync(request,ct);
            if(post&&Armed&&response.StatusCode==HttpStatusCode.Created){Armed=false;response.Dispose();throw new HttpRequestException("Synthetic lost SQL commit response");}
            return response;
        }
    }
}
