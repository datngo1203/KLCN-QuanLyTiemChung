using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TiemChung.Api;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using TiemChung.Tests;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class ClinicalSqlClientTests
{
    [SqlTheory]
    [InlineData(false)][InlineData(true)]
    public async Task WindowsDoctorPersistsReadsReopensAndReconcilesLostCommitResponse(bool loseResponse)
    {
        await using var owner=await OwnedSqlTestDatabase.CreateAsync(System.Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION")!);
        var password="S53-"+Guid.NewGuid().ToString("N")+"!Aa1";
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["ConnectionStrings:TiemChung"]=owner.ConnectionString,["Catalog:UseDemoData"]="false",["Identity:MailMode"]="Disabled",
            ["Identity:SigningKey"]=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),["Logging:LogLevel:Default"]="Warning"
        }).Build();
        await using var db=new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(owner.ConnectionString).Options);
        await db.Database.MigrateAsync();foreach(var code in IdentityPermissions.Codes)db.Add(new QuyenHan{Code=code,TenQuyen=code,MoTa="[DEMO] test"});await db.SaveChangesAsync();
        await DevelopmentCatalogSeed.RunAsync(db,new Environment());
        foreach(var title in new[]{"BacSi","LeTan"}){
            var e=new NhanVien{HoTen="[DEMO] S53 "+title,ChucDanh=title,TrangThai="Active"};db.Add(e);await db.SaveChangesAsync();
            var a=new TaiKhoan{AccountType="Employee",MaNhanVien=e.MaNhanVien,TenDangNhap=title,NormalizedUsername=title.ToUpperInvariant(),TrangThai="Active",CreatedAt=DateTimeOffset.UtcNow};a.MatKhauHash=new PasswordHasher<TaiKhoan>().HashPassword(a,password);db.Add(a);await db.SaveChangesAsync();
            if(title=="LeTan")db.Add(new TaiKhoan_QuyenHan{MaTaiKhoan=a.MaTaiKhoan,MaQuyen=(await db.Set<QuyenHan>().SingleAsync(x=>x.Code=="QuanLyLichHen")).MaQuyen});await db.SaveChangesAsync();
        }
        var customer=new KhachHang{HoTen="[DEMO] Windows clinical customer"};db.Add(customer);await db.SaveChangesAsync();var recipient=new NguoiDuocTiem{MaKhachHang=customer.MaKhachHang,HoTen="[DEMO] Windows clinical recipient"};db.Add(recipient);await db.SaveChangesAsync();
        await using var factory=new WebApplicationFactory<Program>().WithWebHostBuilder(b=>b.UseEnvironment("Development").ConfigureAppConfiguration((_,c)=>c.AddConfiguration(config)).ConfigureServices(s=>{
            s.RemoveAll<DbContextOptions<TiemChungDbContext>>();s.RemoveAll<TiemChungDbContext>();s.AddDbContext<TiemChungDbContext>(o=>o.UseSqlServer(owner.ConnectionString));
        }));
        using var receptionClient=factory.CreateClient();var reception=new IdentityApiService(receptionClient);Assert.True((await reception.LoginAsync("LeTan",password)).Succeeded);
        var registrationApi=new RegistrationApiService(reception);var catalog=await registrationApi.GetCatalogAsync();var single=catalog.First(x=>x.Kind=="vaccine");var package=catalog.First(x=>x.Kind=="package");
        var registrationId=await registrationApi.CreateAsync(new(customer.MaKhachHang,recipient.MaNguoiDuocTiem,[new("vaccine",single.Id,2,single.Price,null),new("package",package.Id,2,package.Price,package.Components!.Select(c=>new RegistrationComponentRequest(c.VaccineId,c.Quantity)).ToArray())]));
        var appointmentApi=new AppointmentApiService(reception);var source=await appointmentApi.GetSourceAsync(registrationId);
        foreach(var item in source.Items)await appointmentApi.CreateAsync(new(registrationId,item.Kind,item.Id,DateTimeOffset.UtcNow.AddDays(1),"[DEMO] clinical room"));
        await reception.LogoutAsync();
        var handler=new LostResponse(factory.Server.CreateHandler());using var doctorClient=new HttpClient(handler){BaseAddress=receptionClient.BaseAddress};var identity=new IdentityApiService(doctorClient);Assert.True((await identity.LoginAsync("BacSi",password)).Succeeded);
        var service=new ScreeningApiService(identity);var vm=new ScreeningViewModel(service,identity);await vm.LoadAsync();Assert.Equal(2,vm.Appointments.Count);
        foreach(var appointment in vm.Appointments.ToArray()){
            vm.Selected=appointment;await vm.LoadDetailAsync();Assert.NotNull(vm.Detail);Assert.All(vm.Vaccines,v=>Assert.False(v.IsSelected));
            vm.Conclusion="Đủ điều kiện";vm.Vaccines[0].IsSelected=true;Assert.NotNull(vm.BeginConfirmation());handler.Armed=loseResponse;await vm.ConfirmAsync(true);
            if(loseResponse){Assert.True(vm.HasPending);Assert.False(vm.CanEdit);await vm.ReconcileAsync();}
            Assert.True(vm.IsRecorded);Assert.False(vm.HasPending);var result=await service.GetAssessmentAsync(appointment.Id);Assert.NotNull(result);Assert.Equal(vm.Assessment!.Id,result.Id);Assert.Single(result.Indications);
            var persisted=await db.Set<PhieuKhamSangLoc>().AsNoTracking().SingleAsync(x=>x.MaLichHen==appointment.Id);Assert.Equal(result.RecordedAt,persisted.ThoiDiemKham);Assert.Null(persisted.ChiSoSinhTon);Assert.Null(persisted.TienSuBenhLy);
            var reopen=new ScreeningViewModel(new ScreeningApiService(identity),identity);await reopen.LoadAsync();reopen.Selected=reopen.Appointments.Single(x=>x.Id==appointment.Id);await reopen.LoadDetailAsync();Assert.True(reopen.IsRecorded);Assert.Equal(result.Id,reopen.Assessment!.Id);Assert.False(reopen.CanEdit);
        }
        Assert.Equal(2,handler.PostCount);Assert.Equal(2,await db.Set<PhieuKhamSangLoc>().CountAsync());Assert.Equal(2,await db.Set<ChiDinhTiem>().CountAsync());Assert.Equal(2,await db.Set<SecurityAudit>().CountAsync(x=>x.Action=="clinical.assessed"));Assert.Empty(await db.Set<MuiTiem>().ToArrayAsync());Assert.Empty(await db.Set<BienDongKho>().ToArrayAsync());
        await identity.LogoutAsync();Assert.Null(vm.Detail);Assert.Null(vm.Assessment);Assert.Empty(vm.Appointments);
    }
    private sealed class LostResponse(HttpMessageHandler inner):DelegatingHandler(inner)
    {
        public bool Armed;public int PostCount;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            var post=request.Method==HttpMethod.Post && request.RequestUri!.AbsolutePath.StartsWith("/api/v1/clinical/");if(post)PostCount++;
            var response=await base.SendAsync(request,ct);if(post && Armed && response.StatusCode==HttpStatusCode.Created){Armed=false;response.Dispose();throw new HttpRequestException("Synthetic lost commit response");}return response;
        }
    }
    private sealed class Environment:IHostEnvironment
    {
        public string EnvironmentName{get;set;}="Development";public string ApplicationName{get;set;}="Clinical.Tests";public string ContentRootPath{get;set;}=AppContext.BaseDirectory;public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();
    }
}
