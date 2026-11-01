using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using TiemChung.Tests;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class AftercareSqlClientTests
{
    [SqlTheory]
    [InlineData(false)][InlineData(true)]
    public async Task ActualWindowsClientRecordsThreeShotsAndReopensAfterLoginWithoutTouchingInventory(bool lost)
    {
        await SqlFixtureLifecycle.RunAsync(()=>AftercareSqlFixture.Create(),async f=>{
            var transport=new LostResponse(f.Factory.Server.CreateHandler());using var http=new HttpClient(transport){BaseAddress=f.Client.BaseAddress};
            var identity=new IdentityApiService(http);Assert.True((await identity.LoginAsync("s55.nurse",f.Password)).Succeeded);
            var service=new AftercareApiService(identity);var vm=new AftercareViewModel(service,identity);await vm.LoadAsync();Assert.Equal(3,vm.Queue.Count);var before=await f.Snapshot();
            foreach(var shot in vm.Queue.ToArray()){
                vm.Selected=shot;await vm.LoadDetailAsync();Assert.Equal(shot,vm.Detail!.Vaccination);Assert.Null(vm.Result);Assert.Equal("",vm.Condition);
                vm.Condition=" [DEMO] Theo dõi thực tế ";vm.Severity=" [DEMO] Mức độ thực tế ";vm.Treatment=" [DEMO] Thông tin xử trí đã nhập ";Assert.NotNull(vm.BeginConfirmation());transport.Armed=lost;await vm.ConfirmAsync(true);
                if(lost){Assert.True(vm.HasPending);Assert.False(vm.CanEdit);await vm.RetryAsync();await vm.ReconcileAsync();}
                Assert.True(vm.IsRecorded);Assert.False(vm.CanSave);Assert.False(vm.HasPending);var result=vm.Result!;
                var sql=await f.Db.Set<TheoDoiSauTiem>().AsNoTracking().SingleAsync(r=>r.MaMuiTiem==shot.Id);Assert.Equal(result.Id,sql.MaTheoDoi);Assert.Equal(result.RecordedBy!.Id,sql.MaNhanVien);Assert.Equal(result.RecordedAt.UtcDateTime,sql.ThoiDiem);Assert.Equal(result.Treatment,sql.HuongXuTri);
                var audit=await f.Db.Set<SecurityAudit>().AsNoTracking().SingleAsync(a=>a.Action=="aftercare.recorded"&&a.ResourceId==result.Id.ToString());Assert.NotNull(audit.ActorAccountId);Assert.NotNull(audit.SessionId);
                Assert.Equal(result,(await service.GetAsync(shot.Id)).FollowUp);
            }
            Assert.Equal(3,transport.PostCount);Assert.Equal(before,await f.Snapshot());await vm.LoadAsync();Assert.True(vm.NoResults);
            await identity.LogoutAsync();Assert.Empty(vm.Queue);Assert.Null(vm.Detail);Assert.True((await identity.LoginAsync("s55.nurse2",f.Password)).Succeeded);
            var reopen=new AftercareViewModel(new AftercareApiService(identity),identity){ResultVaccinationId="MT-0001"};await reopen.ReadResultAsync();Assert.True(reopen.IsRecorded);Assert.False(reopen.CanEdit);Assert.Equal(1,reopen.Result!.VaccinationId);Assert.Equal(before,await f.Snapshot());
        });
    }
    private sealed class LostResponse(HttpMessageHandler inner):DelegatingHandler(inner)
    {
        public bool Armed;public int PostCount;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct){var post=request.Method==HttpMethod.Post&&request.RequestUri!.AbsolutePath.EndsWith("/follow-ups");if(post)PostCount++;var response=await base.SendAsync(request,ct);if(post&&Armed&&response.StatusCode==HttpStatusCode.Created){Armed=false;response.Dispose();throw new HttpRequestException("Synthetic lost followup response");}return response;}
    }
}

// Shared with the full-shell runtime harness; ownership remains in the existing GUID fixture.
public sealed class AftercareSqlFixture:IAsyncDisposable
{
    private VaccinationSqlFixture _base=null!;
    public TiemChungDbContext Db=>_base.Db;
    public WebApplicationFactory<Program> Factory=>_base.Factory;
    public HttpClient Client=>_base.Client;
    public string Password=>_base.Password;
    public static async Task<AftercareSqlFixture> Create(bool useKestrel=false)
    {
        var f=new AftercareSqlFixture();
        try{f._base=await VaccinationSqlFixture.Create(useKestrel);using var client=useKestrel?new HttpClient{BaseAddress=f.Client.BaseAddress}:f.Factory.CreateClient();
            var identity=new IdentityApiService(client);Assert.True((await identity.LoginAsync("s55.nurse",f.Password)).Succeeded);var vaccinations=new VaccinationApiService(identity);
            foreach(var indication in await vaccinations.ListAsync(1,"")){var detail=await vaccinations.GetAsync(indication.Id);await vaccinations.RecordAsync(new(indication.Id,Assert.Single(detail.Lots).Number,"[DEMO] S56 source site"));}
            await identity.LogoutAsync();f.Db.ChangeTracker.Clear();return f;
        }catch(Exception error){await SqlFixtureLifecycle.CleanupAfterFailureAsync(error,f.DisposeAsync);throw;}
    }
    public async Task<string> Snapshot()
    {
        var values=new List<string>();await Db.Database.OpenConnectionAsync();
        try{foreach(var table in new[]{"MuiTiem","LoVacXin","BienDongKho","DangKyDichVu","ThanhToan","PhieuKhamSangLoc","ChiDinhTiem","LichHenTiem"}){await using var cmd=Db.Database.GetDbConnection().CreateCommand();cmd.CommandText=$"SELECT (SELECT * FROM [{table}] FOR JSON PATH, INCLUDE_NULL_VALUES)";values.Add(Convert.ToString(await cmd.ExecuteScalarAsync())!);}}
        finally{await Db.Database.CloseConnectionAsync();}return string.Join("\n",values);
    }
    public ValueTask DisposeAsync()=>_base?.DisposeAsync()??ValueTask.CompletedTask;
}
