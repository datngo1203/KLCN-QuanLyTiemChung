using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using Xunit;

namespace TiemChung.Tests;

public sealed class AftercareSqlTests
{
    private const string Root="/api/v1/vaccinations";
    private static string Path(int id)=>$"{Root}/{id}/follow-ups";
    private static CreateFollowUpRequest Request()=>new(" [DEMO] Tình trạng do điều dưỡng nhập "," [DEMO] Mức độ được ghi nhận "," [DEMO] Xử trí thực tế đã ghi ");
    private static Task Run(Func<VaccinationSqlTests.Fixture,Task> test)=>SqlFixtureLifecycle.RunAsync(VaccinationSqlTests.Fixture.Create,test);
    private static async Task<VaccinationDto> Shot(VaccinationSqlTests.Fixture f)
    {
        var source=await f.Source();
        using var response=await f.Nurse.PostAsJsonAsync(Root,new RecordVaccinationRequest(source.Assessment.Indications.Single().Id,"[DEMO] S5-VALID-A","[DEMO] site"));
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);return (await response.Content.ReadFromJsonAsync<VaccinationDto>())!;
    }
    private static async Task Problem(HttpResponseMessage response,int status,string code)
    {
        using(response){Assert.Equal((HttpStatusCode)status,response.StatusCode);Assert.Equal("no-store",response.Headers.CacheControl!.ToString());
            var content=await response.Content.ReadAsStringAsync();using var json=JsonDocument.Parse(content);Assert.Equal(code,json.RootElement.GetProperty("code").GetString());
            Assert.True(json.RootElement.TryGetProperty("traceId",out _));Assert.DoesNotContain("SqlException",content);Assert.DoesNotContain("[DEMO]",content);}
    }
    private static async Task<string> Unchanged(VaccinationSqlTests.Fixture f)
    {
        var values=new List<string>();await f.Db.Database.OpenConnectionAsync();
        try{foreach(var table in new[]{"MuiTiem","LoVacXin","BienDongKho","DangKyDichVu","ThanhToan","PhieuKhamSangLoc","ChiDinhTiem","LichHenTiem"}){
            await using var cmd=f.Db.Database.GetDbConnection().CreateCommand();cmd.CommandText=$"SELECT (SELECT * FROM [{table}] FOR JSON PATH, INCLUDE_NULL_VALUES)";
            values.Add(Convert.ToString(await cmd.ExecuteScalarAsync())!);
        }}finally{await f.Db.Database.CloseConnectionAsync();}return string.Join("\n",values);
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task RealShotRecordsExactTrimmedTextActorUtcAndReadsWithoutChangingShotStockMoneyOrClinicalData()
    {
        await Run(async f=>{
            var shot=await Shot(f);var snapshot=await Unchanged(f);var detail=(await f.Nurse.GetFromJsonAsync<FollowUpDetailDto>(Path(shot.Id)))!;
            Assert.Null(detail.FollowUp);Assert.Equal(shot,detail.Vaccination);
            var queue=(await f.Nurse.GetFromJsonAsync<FollowUpPendingResponse>(Root+"/follow-up-pending"))!;Assert.Equal(shot,Assert.Single(queue.Items));
            var start=DateTimeOffset.UtcNow;using var saved=await f.OtherNurse.PostAsJsonAsync(Path(shot.Id),Request());Assert.Equal(HttpStatusCode.Created,saved.StatusCode);
            Assert.Equal(Path(shot.Id),saved.Headers.Location!.ToString());Assert.Equal("no-store",saved.Headers.CacheControl!.ToString());
            var result=(await saved.Content.ReadFromJsonAsync<FollowUpDto>())!;Assert.Equal(Request().Condition.Trim(),result.Condition);Assert.Equal(Request().Severity.Trim(),result.Severity);Assert.Equal(Request().Treatment.Trim(),result.Treatment);
            Assert.Equal(shot.Id,result.VaccinationId);Assert.True(result.RecordedBy!.Id>0);Assert.NotEqual(shot.Nurse.Id,result.RecordedBy.Id);
            Assert.Equal(TimeSpan.Zero,result.RecordedAt.Offset);Assert.InRange(result.RecordedAt,start,DateTimeOffset.UtcNow);Assert.True(result.RecordedAt>=shot.AdministeredAt);
            var sql=await f.Db.Set<TheoDoiSauTiem>().AsNoTracking().SingleAsync();Assert.Equal(result.RecordedBy.Id,sql.MaNhanVien);Assert.Equal(result.RecordedAt.UtcDateTime,sql.ThoiDiem);Assert.Equal(result.Treatment,sql.HuongXuTri);
            var audit=await f.Db.Set<SecurityAudit>().AsNoTracking().SingleAsync(a=>a.Action=="aftercare.recorded");Assert.Equal("FollowUp",audit.ResourceType);Assert.Equal(result.Id.ToString(),audit.ResourceId);Assert.NotNull(audit.ActorAccountId);Assert.NotNull(audit.SessionId);
            Assert.Equal(result,(await f.Nurse.GetFromJsonAsync<FollowUpDetailDto>(Path(shot.Id)))!.FollowUp);
            Assert.Empty((await f.Nurse.GetFromJsonAsync<FollowUpPendingResponse>(Root+"/follow-up-pending"))!.Items);
            await Problem(await f.Nurse.PostAsJsonAsync(Path(shot.Id),Request()),409,"followUpAlreadyRecorded");
            Assert.Equal(snapshot,await Unchanged(f));Assert.Single(await f.Db.Set<TheoDoiSauTiem>().ToArrayAsync());Assert.Single(await f.Db.Set<SecurityAudit>().Where(a=>a.Action=="aftercare.recorded").ToArrayAsync());
        });
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task FollowUpIsIndependentOfCurrentLotStockExpiryAndPaymentAndGetsHaveNoSideEffects()
    {
        await Run(async f=>{
            var shot=await Shot(f);await f.Db.Set<LoVacXin>().Where(l=>l.SoLo==shot.LotNumber).ExecuteUpdateAsync(s=>s.SetProperty(l=>l.SoLuongTon,0).SetProperty(l=>l.HanSuDung,DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1)).SetProperty(l=>l.TrangThai,"Inactive"));
            await f.Db.Set<ThanhToan>().Where(p=>p.MaDangKy==shot.RegistrationId).ExecuteUpdateAsync(s=>s.SetProperty(p=>p.TrangThai,"[DEMO] changed"));
            var before=await f.Snapshot();Assert.Null((await f.Nurse.GetFromJsonAsync<FollowUpDetailDto>(Path(shot.Id)))!.FollowUp);Assert.Equal(before,await f.Snapshot());
            var source=await Unchanged(f);using var saved=await f.Nurse.PostAsJsonAsync(Path(shot.Id),Request());Assert.Equal(HttpStatusCode.Created,saved.StatusCode);
            before=await f.Snapshot();Assert.NotNull((await f.Nurse.GetFromJsonAsync<FollowUpDetailDto>(Path(shot.Id)))!.FollowUp);Assert.Equal(before,await f.Snapshot());Assert.Equal(source,await Unchanged(f));
        });
    }
    [Theory,Trait("Category","SqlIntegration")]
    [InlineData("missing")][InlineData("legacy")][InlineData("status")][InlineData("recipient")][InlineData("vaccine")][InlineData("appointment")][InlineData("clinical")][InlineData("future")]
    public async Task RejectsMissingLegacyOrConflictingSourcesWithoutPartialWrite(string scenario)
    {
        await Run(async f=>{
            var shot=await Shot(f);var id=shot.Id;
            if(scenario=="missing")id=int.MaxValue;
            if(scenario=="legacy")await f.Db.Set<MuiTiem>().Where(m=>m.MaMuiTiem==id).ExecuteUpdateAsync(s=>s.SetProperty(m=>m.MaChiDinh,(int?)null));
            if(scenario=="status")await f.Db.Set<MuiTiem>().Where(m=>m.MaMuiTiem==id).ExecuteUpdateAsync(s=>s.SetProperty(m=>m.TrangThai,"[DEMO] not recorded"));
            if(scenario=="recipient"){
                var person=new NguoiDuocTiem{MaKhachHang=f.Base.CustomerId,HoTen="[DEMO] other"};f.Db.Add(person);await f.Db.SaveChangesAsync();
                await f.Db.Set<MuiTiem>().Where(m=>m.MaMuiTiem==id).ExecuteUpdateAsync(s=>s.SetProperty(m=>m.MaNguoiDuocTiem,person.MaNguoiDuocTiem));
            }
            if(scenario=="vaccine")await f.Db.Set<LoVacXin>().Where(l=>l.SoLo==shot.LotNumber).ExecuteUpdateAsync(s=>s.SetProperty(l=>l.MaVacXin,f.B));
            if(scenario=="appointment")await f.Db.Set<PhieuKhamSangLoc>().Where(p=>p.MaLichHen==shot.AppointmentId).ExecuteUpdateAsync(s=>s.SetProperty(p=>p.MaLichHen,(int?)null).SetProperty(p=>p.ThoiDiemKham,(DateTimeOffset?)null));
            if(scenario=="clinical")await f.Db.Set<ChiDinhTiem>().Where(i=>i.MaChiDinh==shot.IndicationId).ExecuteUpdateAsync(s=>s.SetProperty(i=>i.KetQuaChiDinh,"[DEMO] not allowed"));
            if(scenario=="future")await f.Db.Set<MuiTiem>().Where(m=>m.MaMuiTiem==id).ExecuteUpdateAsync(s=>s.SetProperty(m=>m.ThoiDiemTiem,DateTime.UtcNow.AddDays(1)));
            var before=await f.Snapshot();var status=scenario=="missing"?404:409;var code=scenario=="missing"?"resourceNotFound":"followUpSourceConflict";
            await Problem(await f.Nurse.PostAsJsonAsync(Path(id),Request()),status,code);
            if(scenario!="future")await Problem(await f.Nurse.GetAsync(Path(id)),status,code);
            Assert.Equal(before,await f.Snapshot());
            if(scenario is not("missing" or "future"))Assert.Empty((await f.Nurse.GetFromJsonAsync<FollowUpPendingResponse>(Root+"/follow-up-pending"))!.Items);
        });
    }
    [Theory,Trait("Category","SqlIntegration")]
    [InlineData("Admin")][InlineData("LeTan")][InlineData("BacSi")][InlineData("ThuNgan")][InlineData("ThuKho")][InlineData(null)]
    public async Task OtherTitlesAndCustomerCannotReadOrWrite(string? title)
    {
        await Run(async f=>{var shot=await Shot(f);using var other=await f.Base.Actor(title,true);var before=await f.Snapshot();
            await Problem(await other.GetAsync(Root+"/follow-up-pending"),403,"permissionDenied");await Problem(await other.GetAsync(Path(shot.Id)),403,"permissionDenied");
            await Problem(await other.PostAsJsonAsync(Path(shot.Id),Request()),403,"permissionDenied");Assert.Equal(before,await f.Snapshot());});
    }
    [Theory,Trait("Category","SqlIntegration")]
    [InlineData("account")][InlineData("employee")][InlineData("session")]
    public async Task IdentityMustStillBeActive(string scenario)
    {
        await Run(async f=>{var shot=await Shot(f);
            if(scenario=="account")await f.Db.Set<TaiKhoan>().Where(a=>a.MaTaiKhoan==f.NurseAccountId).ExecuteUpdateAsync(s=>s.SetProperty(a=>a.TrangThai,"Inactive"));
            if(scenario=="employee")await f.Db.Set<NhanVien>().Where(n=>n.MaNhanVien==f.NurseEmployeeId).ExecuteUpdateAsync(s=>s.SetProperty(n=>n.TrangThai,"Inactive"));
            if(scenario=="session")await f.Db.Set<IdentitySession>().Where(s=>s.AccountId==f.NurseAccountId).ExecuteUpdateAsync(s=>s.SetProperty(a=>a.RevokedAt,DateTimeOffset.UtcNow));
            var before=await f.Snapshot();await Problem(await f.Nurse.PostAsJsonAsync(Path(shot.Id),Request()),401,"invalidSession");await Problem(await f.Nurse.GetAsync(Path(shot.Id)),401,"invalidSession");Assert.Equal(before,await f.Snapshot());});
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task InputBindingLengthSearchPagingAndMissingAuthenticationAreValidated()
    {
        await Run(async f=>{var shot=await Shot(f);var before=await f.Snapshot();
            foreach(var payload in new[]{"{}","null","{","{\"condition\":\"x\",\"severity\":\"y\"}","{\"condition\":\"x\",\"severity\":\"y\",\"treatment\":\"z\",\"canLeave\":true}"})
                await Problem(await f.Nurse.PostAsync(Path(shot.Id),new StringContent(payload,Encoding.UTF8,"application/json")),400,"validation");
            foreach(var req in new[]{new CreateFollowUpRequest(" ","x","y"),new("x","\t","y"),new("x","y"," "),new(new string('x',101),"y","z"),new("x",new string('y',101),"z"),new("x","y",new string('z',501))})
                await Problem(await f.Nurse.PostAsJsonAsync(Path(shot.Id),req),400,"validation");
            foreach(var query in new[]{"page=0","pageSize=101","page=2147483647&pageSize=100","page=abc","search="+new string('x',101)})await Problem(await f.Nurse.GetAsync(Root+"/follow-up-pending?"+query),400,"validation");
            foreach(var query in new[]{"search=MT-"+shot.Id.ToString("0000"),"search=%5BDEMO%5D","page=1&pageSize=1"})Assert.Equal(shot.Id,Assert.Single((await f.Nurse.GetFromJsonAsync<FollowUpPendingResponse>(Root+"/follow-up-pending?"+query))!.Items).Id);
            Assert.Empty((await f.Nurse.GetFromJsonAsync<FollowUpPendingResponse>(Root+"/follow-up-pending?page=2&pageSize=1"))!.Items);
            await Problem(await f.Nurse.GetAsync(Path(0)),400,"validation");using var anonymous=f.Base.Base.Factory.CreateClient();await Problem(await anonymous.GetAsync(Path(shot.Id)),401,"invalidSession");Assert.Equal(before,await f.Snapshot());
        });
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task TwoNursesPostingSameShotProduceExactlyOneFollowUpAndAudit()
    {
        await Run(async f=>{var shot=await Shot(f);var before=await Unchanged(f);
            var responses=await Task.WhenAll(f.Nurse.PostAsJsonAsync(Path(shot.Id),Request()),f.OtherNurse.PostAsJsonAsync(Path(shot.Id),Request()));
            try{Assert.Single(responses,r=>r.StatusCode==HttpStatusCode.Created);await Problem(Assert.Single(responses,r=>r.StatusCode!=HttpStatusCode.Created),409,"followUpAlreadyRecorded");}finally{foreach(var response in responses)response.Dispose();}
            Assert.Single(await f.Db.Set<TheoDoiSauTiem>().ToArrayAsync());Assert.Single(await f.Db.Set<SecurityAudit>().Where(a=>a.Action=="aftercare.recorded").ToArrayAsync());Assert.Equal(before,await Unchanged(f));});
    }
    [Theory,Trait("Category","SqlIntegration")]
    [InlineData("audit")][InlineData("followup")]
    public async Task InsertOrAuditFailureRollsBackWholeOperation(string fault)
    {
        await Run(async f=>{var shot=await Shot(f);var before=await f.Snapshot();
            await f.Db.Database.ExecuteSqlRawAsync(fault=="audit"?"ALTER TABLE SecurityAudit ADD CONSTRAINT S56Fault CHECK (Action<>N'aftercare.recorded')":"ALTER TABLE TheoDoiSauTiem ADD CONSTRAINT S56Fault CHECK (MaNhanVien IS NULL)");
            await Problem(await f.Nurse.PostAsJsonAsync(Path(shot.Id),Request()),503,"dependencyUnavailable");Assert.Equal(before,await f.Snapshot());});
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task MigrationPreservesLegacyHasRecorderFkAndRefusesLossyDowngrade()
    {
        await Run(async f=>{var shot=await Shot(f);var migrator=f.Db.GetService<IMigrator>();const string previous="20261101023132_S5VaccinationRecord";
            await migrator.MigrateAsync(previous);await f.Db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO TheoDoiSauTiem(MaMuiTiem,TinhTrang,MucDoPhanUng,HuongXuTri,ThoiDiem) VALUES({shot.Id},{"[DEMO] legacy condition"},{"[DEMO] legacy severity"},{"[DEMO] legacy treatment"},{shot.AdministeredAt.UtcDateTime})");
            await migrator.MigrateAsync();await migrator.MigrateAsync();var legacy=await f.Db.Set<TheoDoiSauTiem>().AsNoTracking().SingleAsync();Assert.Null(legacy.MaNhanVien);Assert.Equal("[DEMO] legacy treatment",legacy.HuongXuTri);
            Assert.Null((await f.Nurse.GetFromJsonAsync<FollowUpDetailDto>(Path(shot.Id)))!.FollowUp!.RecordedBy);
            await migrator.MigrateAsync(previous);await migrator.MigrateAsync();Assert.Null((await f.Db.Set<TheoDoiSauTiem>().AsNoTracking().SingleAsync()).MaNhanVien);
            await Assert.ThrowsAsync<SqlException>(()=>f.Db.Database.ExecuteSqlRawAsync("UPDATE TheoDoiSauTiem SET MaNhanVien=2147483647"));
            await f.Db.Set<TheoDoiSauTiem>().ExecuteUpdateAsync(s=>s.SetProperty(r=>r.MaNhanVien,f.NurseEmployeeId));
            await Assert.ThrowsAsync<SqlException>(()=>migrator.MigrateAsync(previous));Assert.Equal(f.NurseEmployeeId,(await f.Db.Set<TheoDoiSauTiem>().AsNoTracking().SingleAsync()).MaNhanVien);Assert.False(f.Db.Database.HasPendingModelChanges());
        });
    }
}
