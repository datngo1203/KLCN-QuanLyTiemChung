using System.Net;
using System.Net.Http.Json;
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

public sealed class VaccinationSqlClientTests
{
    [SqlTheory]
    [InlineData(false)][InlineData(true)]
    public async Task WindowsNurseRecordsBothKindsReadsReopensAndReconcilesLostSqlCommitResponse(bool loseResponse)
    {
        await SqlFixtureLifecycle.RunAsync(()=>VaccinationSqlFixture.Create(),async f=>{
            var transport=new LostResponse(f.Factory.Server.CreateHandler());
            using var client=new HttpClient(transport){BaseAddress=f.Client.BaseAddress};
            var identity=new IdentityApiService(client);Assert.True((await identity.LoginAsync("s55.nurse",f.Password)).Succeeded);
            var service=new VaccinationApiService(identity);var vm=new VaccinationViewModel(service,identity);
            await vm.LoadAsync();Assert.Equal(3,vm.Queue.Count);
            foreach(var indication in vm.Queue.ToArray()) {
                vm.Selected=indication;await vm.LoadDetailAsync();Assert.Equal(indication.Id,vm.Detail!.Indication.Id);Assert.Null(vm.SelectedLot);
                vm.SelectedLot=Assert.Single(vm.Lots);vm.InjectionSite="[DEMO] vị trí thực tế";Assert.NotNull(vm.BeginConfirmation());
                transport.Armed=loseResponse;await vm.ConfirmAsync(true);
                if(loseResponse){Assert.True(vm.HasPending);Assert.False(vm.CanEdit);await vm.RetryAsync();await vm.ReconcileAsync();}
                Assert.True(vm.IsRecorded);Assert.False(vm.CanSave);Assert.False(vm.HasPending);
                var result=await service.GetResultAsync(indication.Id);Assert.Equal(vm.Result,result);Assert.Equal(indication.Vaccine.Id,result!.Vaccine.Id);
                var row=await f.Db.Set<MuiTiem>().AsNoTracking().SingleAsync(x=>x.MaChiDinh==indication.Id);
                Assert.Equal(result.Id,row.MaMuiTiem);Assert.Equal(result.InjectionSite,row.ViTriTiem);Assert.Equal(result.AdministeredAt.UtcDateTime,row.ThoiDiemTiem);
                var movement=await f.Db.Set<BienDongKho>().AsNoTracking().SingleAsync(x=>x.MaMuiTiem==row.MaMuiTiem);
                Assert.Equal(row.SoLo,movement.SoLo);Assert.Equal(row.MaNhanVien,movement.MaNhanVien);Assert.Equal(1,movement.SoLuong);
                var reopen=new VaccinationViewModel(new VaccinationApiService(identity),identity){ResultIndicationId=indication.Id.ToString()};
                await reopen.ReadResultAsync();Assert.Equal(result,reopen.Result);Assert.True(reopen.IsRecorded);Assert.False(reopen.CanSave);
            }
            Assert.Equal(3,transport.PostCount);Assert.Equal(3,await f.Db.Set<MuiTiem>().CountAsync());Assert.Equal(3,await f.Db.Set<BienDongKho>().CountAsync());
            Assert.Equal(3,await f.Db.Set<SecurityAudit>().CountAsync(x=>x.Action=="vaccinations.recorded"));
            Assert.Equal(98,(await f.Db.Set<LoVacXin>().AsNoTracking().SingleAsync(x=>x.SoLo=="[DEMO] S5-VALID-A")).SoLuongTon);
            Assert.Equal(9,(await f.Db.Set<LoVacXin>().AsNoTracking().SingleAsync(x=>x.SoLo=="[DEMO] S55-VALID-B")).SoLuongTon);
            Assert.Empty(await f.Db.Set<TheoDoiSauTiem>().ToArrayAsync());await vm.LoadAsync();Assert.True(vm.NoResults);
            await identity.LogoutAsync();Assert.Null(vm.Detail);Assert.Null(vm.Result);Assert.Empty(vm.Queue);Assert.Empty(vm.Lots);
        });
    }
    private sealed class LostResponse(HttpMessageHandler inner):DelegatingHandler(inner)
    {
        public bool Armed;public int PostCount;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            var post=request.Method==HttpMethod.Post && request.RequestUri!.AbsolutePath=="/api/v1/vaccinations";if(post)PostCount++;
            var response=await base.SendAsync(request,ct);
            if(post && Armed && response.StatusCode==HttpStatusCode.Created){Armed=false;response.Dispose();throw new HttpRequestException("Synthetic lost commit response");}
            return response;
        }
    }
}

// Shared with the local full-shell harness; every instance owns only its newly created GUID database.
public sealed class VaccinationSqlFixture : IAsyncDisposable
{
    private OwnedSqlTestDatabase _owner=null!;
    public TiemChungDbContext Db {get;private set;}=null!;
    public WebApplicationFactory<Program> Factory {get;private set;}=null!;
    public HttpClient Client {get;private set;}=null!;
    public string Password {get;private set;}="";
    public static async Task<VaccinationSqlFixture> Create(bool useKestrel=false)
    {
        var f=new VaccinationSqlFixture();
        try {
            f._owner=await OwnedSqlTestDatabase.CreateAsync(System.Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION")??throw new InvalidOperationException("SQL test connection required."));
            f.Password="S55-"+Guid.NewGuid().ToString("N")+"!Aa1";
            f.Db=new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(f._owner.ConnectionString).Options);
            await f.Db.Database.MigrateAsync();
            foreach(var code in IdentityPermissions.Codes)f.Db.Add(new QuyenHan{Code=code,TenQuyen=code,MoTa="[DEMO] test"});await f.Db.SaveChangesAsync();
            await DevelopmentCatalogSeed.RunAsync(f.Db,new TestEnvironment());await DevelopmentInventorySeed.RunAsync(f.Db,new TestEnvironment());
            var b=await f.Db.Vaccines.SingleAsync(x=>x.TenVacXin=="[DEMO] Vắc xin B");var day=VaccinationEndpoints.FacilityDate(DateTimeOffset.UtcNow);
            f.Db.Add(new LoVacXin{SoLo="[DEMO] S55-VALID-B",MaVacXin=b.MaVacXin,NgaySanXuat=day.AddMonths(-6),HanSuDung=day.AddYears(1),SoLuongBanDau=10,SoLuongTon=10,TrangThai="Active"});
            foreach(var (username,title,grants) in new[]{("s55.nurse","DieuDuong",Array.Empty<string>()),("s55.nurse2","DieuDuong",Array.Empty<string>()),
                ("s55.doctor","BacSi",Array.Empty<string>()),("s55.reception","LeTan",new[]{"QuanLyLichHen"}),
                ("s55.cashier","ThuNgan",new[]{"XemThanhToan","GhiThanhToan"}),("s55.admin","Admin",Array.Empty<string>())}) {
                var employee=new NhanVien{HoTen="[DEMO] S55 "+title,ChucDanh=title,TrangThai="Active"};f.Db.Add(employee);await f.Db.SaveChangesAsync();
                var account=new TaiKhoan{AccountType="Employee",MaNhanVien=employee.MaNhanVien,TenDangNhap=username,NormalizedUsername=username.ToUpperInvariant(),TrangThai="Active",CreatedAt=DateTimeOffset.UtcNow};
                account.MatKhauHash=new PasswordHasher<TaiKhoan>().HashPassword(account,f.Password);f.Db.Add(account);await f.Db.SaveChangesAsync();
                foreach(var grant in grants)f.Db.Add(new TaiKhoan_QuyenHan{MaTaiKhoan=account.MaTaiKhoan,MaQuyen=(await f.Db.Set<QuyenHan>().SingleAsync(x=>x.Code==grant)).MaQuyen});await f.Db.SaveChangesAsync();
            }
            var customer=new KhachHang{HoTen="[DEMO] S55 Khách hàng"};f.Db.Add(customer);await f.Db.SaveChangesAsync();
            var recipient=new NguoiDuocTiem{MaKhachHang=customer.MaKhachHang,HoTen="[DEMO] S55 Người được tiêm"};f.Db.Add(recipient);await f.Db.SaveChangesAsync();
            var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{
                ["ConnectionStrings:TiemChung"]=f._owner.ConnectionString,["Catalog:UseDemoData"]="false",["Identity:MailMode"]="Disabled",
                ["Identity:SigningKey"]=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),["Logging:LogLevel:Default"]="Warning"
            }).Build();
            f.Factory=new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>builder.UseEnvironment("Development").ConfigureAppConfiguration((_,c)=>c.AddConfiguration(config)).ConfigureServices(s=>{
                s.RemoveAll<TiemChungDbContext>();s.RemoveAll<DbContextOptions<TiemChungDbContext>>();s.AddDbContext<TiemChungDbContext>(o=>o.UseSqlServer(f._owner.ConnectionString));
            }));
            if(useKestrel)f.Factory.UseKestrel(0);
            f.Client=f.Factory.CreateClient();
            using var receptionHttp=f.Factory.CreateClient();var reception=new IdentityApiService(receptionHttp);Assert.True((await reception.LoginAsync("s55.reception",f.Password)).Succeeded);
            var registrations=new RegistrationApiService(reception);var catalog=await registrations.GetCatalogAsync();var vaccine=catalog.First(x=>x.Kind=="vaccine");var package=catalog.First(x=>x.Kind=="package");
            var registrationId=await registrations.CreateAsync(new(customer.MaKhachHang,recipient.MaNguoiDuocTiem,[new("vaccine",vaccine.Id,2,vaccine.Price,null),new("package",package.Id,1,package.Price,package.Components!.Select(c=>new RegistrationComponentRequest(c.VaccineId,c.Quantity)).ToArray())]));
            var appointments=new AppointmentApiService(reception);var source=await appointments.GetSourceAsync(registrationId);
            var created=new List<AppointmentRecord>();foreach(var item in source.Items)created.Add(await appointments.CreateAsync(new(registrationId,item.Kind,item.Id,DateTimeOffset.UtcNow.AddDays(1),"[DEMO] S55 cơ sở")));
            await reception.LogoutAsync();
            using var doctorHttp=f.Factory.CreateClient();var doctor=new IdentityApiService(doctorHttp);Assert.True((await doctor.LoginAsync("s55.doctor",f.Password)).Succeeded);
            var clinical=new ScreeningApiService(doctor);foreach(var appointment in created){var detail=await clinical.GetAsync(appointment.Id);await clinical.CreateAsync(appointment.Id,new("Đủ điều kiện",null,detail.AllowedVaccines.Select(v=>v.Id).ToArray()));}await doctor.LogoutAsync();
            using var cashierHttp=f.Factory.CreateClient();var cashier=new IdentityApiService(cashierHttp);Assert.True((await cashier.LoginAsync("s55.cashier",f.Password)).Succeeded);await new PaymentApiService(cashier).RecordAsync(registrationId);await cashier.LogoutAsync();
            f.Db.ChangeTracker.Clear();return f;
        }catch(Exception error){await SqlFixtureLifecycle.CleanupAfterFailureAsync(error,f.DisposeAsync);throw;}
    }
    public ValueTask DisposeAsync()=>SqlFixtureLifecycle.CleanupAsync(
        ()=>{Client?.Dispose();return ValueTask.CompletedTask;},()=>Factory?.DisposeAsync()??ValueTask.CompletedTask,
        ()=>Db?.DisposeAsync()??ValueTask.CompletedTask,()=>_owner?.DisposeAsync()??ValueTask.CompletedTask);
    private sealed class TestEnvironment:IHostEnvironment
    {
        public string EnvironmentName{get;set;}="Development";public string ApplicationName{get;set;}="VaccinationClient.Tests";public string ContentRootPath{get;set;}=AppContext.BaseDirectory;public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();
    }
}
