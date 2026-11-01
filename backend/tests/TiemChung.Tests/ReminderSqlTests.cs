using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using Xunit;

namespace TiemChung.Tests;

public sealed class ReminderSqlTests
{
    private static string Path(int id) => $"/api/v1/appointments/{id}/reminders";
    private static Task Run(Func<AppointmentSqlTests.Fixture,Task> action) => SqlFixtureLifecycle.RunAsync(AppointmentSqlTests.Fixture.Create,action);
    private static async Task<AppointmentDto> Appointment(AppointmentSqlTests.Fixture f,string kind="vaccine") {
        var r=await f.Register();using var response=await f.Reception.PostAsJsonAsync("/api/v1/appointments",f.Request(r,r.Items.Single(i=>i.Kind==kind)));
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);return (await response.Content.ReadFromJsonAsync<AppointmentDto>())!;
    }
    private static async Task Problem(HttpResponseMessage response,int status,string code) {
        using(response){Assert.Equal((HttpStatusCode)status,response.StatusCode);Assert.Equal("no-store",response.Headers.CacheControl!.ToString());
            var text=await response.Content.ReadAsStringAsync();Assert.Contains("\"code\":\""+code+"\"",text);Assert.DoesNotContain("[DEMO]",text);Assert.DoesNotContain("SqlException",text);}
    }
    private static async Task<string> Snapshot(AppointmentSqlTests.Fixture f) {
        await f.Db.Database.OpenConnectionAsync();var rows=new List<string>();
        try{foreach(var table in new[]{"LichHenTiem","DangKyDichVu","ThanhToan","MuiTiem","LoVacXin","BienDongKho","TheoDoiSauTiem"}){
            await using var cmd=f.Db.Database.GetDbConnection().CreateCommand();cmd.CommandText=$"SELECT (SELECT * FROM [{table}] FOR JSON PATH, INCLUDE_NULL_VALUES)";
            rows.Add(Convert.ToString(await cmd.ExecuteScalarAsync())!);
        }}finally{await f.Db.Database.CloseConnectionAsync();}return string.Join("\n",rows);
    }
    [Theory][InlineData("vaccine")][InlineData("package")]
    public async Task RealSqlSnapshotActorAuditReadbackIsManualUniqueAndDoesNotChangeSource(string kind) {
        await Run(async f=>{
            var a=await Appointment(f,kind);var before=await Snapshot(f);Assert.Empty(await f.Db.Set<NhacLich>().ToArrayAsync());
            var preview=(await f.Reception.GetFromJsonAsync<ReminderDetailDto>(Path(a.Id)))!;Assert.Null(preview.Reminder);Assert.True(preview.CanRecord);
            Assert.Contains(a.Recipient.FullName,preview.Preview);Assert.Contains(a.ServiceName,preview.Preview);Assert.Contains(a.Location,preview.Preview);
            Assert.Contains(a.ScheduledAt.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm:ss",System.Globalization.CultureInfo.InvariantCulture),preview.Preview);
            Assert.Contains("chưa được gửi",preview.Preview);Assert.Empty(await f.Db.Set<NhacLich>().ToArrayAsync());
            using var response=await f.Reception.PostAsJsonAsync(Path(a.Id),new{});Assert.Equal(HttpStatusCode.Created,response.StatusCode);
            Assert.Equal("no-store",response.Headers.CacheControl!.ToString());Assert.Equal(Path(a.Id),response.Headers.Location!.OriginalString);
            var saved=(await response.Content.ReadFromJsonAsync<ReminderDto>())!;Assert.Equal(preview.Preview,saved.Content);Assert.Equal(f.EmployeeId,saved.RecordedBy!.Id);
            Assert.Equal("LogMoPhong",saved.Channel);Assert.Equal("DaGhiLogMoPhong",saved.Status);Assert.Equal(TimeSpan.Zero,saved.RecordedAt.Offset);
            var audit=await f.Db.Set<SecurityAudit>().SingleAsync(x=>x.Action=="reminders.simulated");Assert.Equal(f.AccountId,audit.ActorAccountId);Assert.NotNull(audit.SessionId);Assert.Equal(saved.Id.ToString(),audit.ResourceId);
            await Problem(await f.Reception.PostAsJsonAsync(Path(a.Id),new{}),409,"reminderAlreadyRecorded");
            Assert.Equal(before,await Snapshot(f));
            await f.Db.Set<LichHenTiem>().Where(x=>x.MaLichHen==a.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.DiemTiem,"[DEMO] changed"));
            before=await Snapshot(f);var detail=(await f.Reception.GetFromJsonAsync<ReminderDetailDto>(Path(a.Id)))!;
            Assert.Equal(saved,detail.Reminder);Assert.Null(detail.Preview);Assert.False(detail.CanRecord);Assert.Equal(before,await Snapshot(f));
            Assert.Single(await f.Db.Set<NhacLich>().ToArrayAsync());Assert.Equal(1,await f.Db.Set<SecurityAudit>().CountAsync(x=>x.Action=="reminders.simulated"));
        });
    }
    [Theory][InlineData("Admin")][InlineData("BacSi")][InlineData("DieuDuong")][InlineData("ThuNgan")][InlineData("ThuKho")][InlineData(null)][InlineData("LeTan")]
    public async Task OtherActorsCannotReadOrWrite(string? title) {
        await Run(async f=>{var a=await Appointment(f);using var actor=await f.Actor(title,title!="LeTan");
            await Problem(await actor.GetAsync(Path(a.Id)),403,"permissionDenied");await Problem(await actor.PostAsJsonAsync(Path(a.Id),new{}),403,"permissionDenied");});
    }
    [Theory][InlineData("past")][InlineData("status")][InlineData("legacy")][InlineData("parent")][InlineData("registration")]
    public async Task InvalidSourceIsRejected(string scenario) {
        await Run(async f=>{var a=await Appointment(f);
            if(scenario=="past")await f.Db.Set<LichHenTiem>().ExecuteUpdateAsync(s=>s.SetProperty(x=>x.ThoiGianHen,DateTime.UtcNow.AddDays(-1)));
            if(scenario=="status")await f.Db.Set<LichHenTiem>().ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TrangThai,"Legacy"));
            if(scenario=="legacy")await f.Db.Set<LichHenTiem>().ExecuteUpdateAsync(s=>s.SetProperty(x=>x.MaDangKy,(int?)null).SetProperty(x=>x.MaChiTietVacXin,(int?)null));
            if(scenario=="registration")await f.Db.Registrations.ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TrangThai,"Legacy"));
            if(scenario=="parent") {var c=new KhachHang{HoTen="[DEMO] other"};f.Db.Add(c);await f.Db.SaveChangesAsync();await f.Db.Recipients.ExecuteUpdateAsync(s=>s.SetProperty(x=>x.MaKhachHang,c.MaKhachHang));}
            var before=await Snapshot(f);await Problem(await f.Reception.PostAsJsonAsync(Path(a.Id),new{}),409,"reminderSourceConflict");Assert.Empty(await f.Db.Set<NhacLich>().ToArrayAsync());Assert.Equal(before,await Snapshot(f));});
    }
    [Theory][InlineData("account")][InlineData("employee")][InlineData("session")]
    public async Task IdentityIsCheckedAtOperationTime(string target) {
        await Run(async f=>{var a=await Appointment(f);
            if(target=="account")await f.Db.Set<TaiKhoan>().Where(x=>x.MaTaiKhoan==f.AccountId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TrangThai,"Inactive"));
            if(target=="employee")await f.Db.Set<NhanVien>().Where(x=>x.MaNhanVien==f.EmployeeId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TrangThai,"Inactive"));
            if(target=="session")await f.Db.Set<IdentitySession>().Where(x=>x.AccountId==f.AccountId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.RevokedAt,DateTimeOffset.UtcNow));
            await Problem(await f.Reception.GetAsync(Path(a.Id)),401,"invalidSession");await Problem(await f.Reception.PostAsJsonAsync(Path(a.Id),new{}),401,"invalidSession");});
    }
    [Fact] public async Task ConcurrentPostsCommitExactlyOneLogAndAudit() {
        await Run(async f=>{var a=await Appointment(f);using var second=await f.Actor("LeTan",true);var before=await Snapshot(f);
            var responses=await Task.WhenAll(f.Reception.PostAsJsonAsync(Path(a.Id),new{}),second.PostAsJsonAsync(Path(a.Id),new{}));
            Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Created);await Problem(Assert.Single(responses,x=>x.StatusCode!=HttpStatusCode.Created),409,"reminderAlreadyRecorded");
            foreach(var r in responses)r.Dispose();Assert.Single(await f.Db.Set<NhacLich>().ToArrayAsync());Assert.Equal(1,await f.Db.Set<SecurityAudit>().CountAsync(x=>x.Action=="reminders.simulated"));Assert.Equal(before,await Snapshot(f));});
    }
    [Theory][InlineData("audit")][InlineData("reminder")]
    public async Task InsertFailuresRollbackBothRecords(string target) {
        await Run(async f=>{var a=await Appointment(f);await f.Db.Database.ExecuteSqlRawAsync(target=="audit"?"ALTER TABLE SecurityAudit ADD CONSTRAINT S57Fault CHECK(Action<>N'reminders.simulated')":"ALTER TABLE NhacLich ADD CONSTRAINT S57Fault CHECK(KenhGui<>N'LogMoPhong')");
            await Problem(await f.Reception.PostAsJsonAsync(Path(a.Id),new{}),503,"dependencyUnavailable");Assert.Empty(await f.Db.Set<NhacLich>().ToArrayAsync());Assert.Equal(0,await f.Db.Set<SecurityAudit>().CountAsync(x=>x.Action=="reminders.simulated"));});
    }
    [Fact] public async Task BindingMissingAndAnonymousAreSafe() {
        await Run(async f=>{var a=await Appointment(f);using var anonymous=f.Base.Factory.CreateClient();await Problem(await anonymous.GetAsync(Path(a.Id)),401,"invalidSession");
            await Problem(await f.Reception.GetAsync(Path(0)),400,"validation");await Problem(await f.Reception.GetAsync(Path(int.MaxValue)),404,"resourceNotFound");
            await Problem(await f.Reception.PostAsJsonAsync(Path(a.Id),new{content="PRIVATE"}),400,"validation");});
    }
    [Fact] public async Task AlreadyAdministeredAppointmentCannotGetAttendanceReminder() {
        await SqlFixtureLifecycle.RunAsync(VaccinationSqlTests.Fixture.Create,async f=>{
            var source=await f.Source();var indication=source.Assessment.Indications.Single();
            using var shot=await f.Nurse.PostAsJsonAsync("/api/v1/vaccinations",new RecordVaccinationRequest(indication.Id,"[DEMO] S5-VALID-A","[DEMO] site"));
            Assert.Equal(HttpStatusCode.Created,shot.StatusCode);
            var before=await f.Snapshot();var preview=(await f.Base.Reception.GetFromJsonAsync<ReminderDetailDto>(Path(source.Appointment.Id)))!;
            Assert.False(preview.CanRecord);Assert.Null(preview.Preview);
            await Problem(await f.Base.Reception.PostAsJsonAsync(Path(source.Appointment.Id),new{}),409,"reminderSourceConflict");
            Assert.Empty(await f.Db.Set<NhacLich>().ToArrayAsync());Assert.Equal(before,await f.Snapshot());
        });
    }
    [Fact] public async Task MigrationPreservesLegacyRejectsConflictAndGuardsDown() {
        await Run(async f=>{var a=await Appointment(f);var migrator=f.Db.GetService<IMigrator>();const string prior="20261101071624_S5PostVaccinationFollowUp";
            await migrator.MigrateAsync(prior);await f.Db.Database.ExecuteSqlInterpolatedAsync($"INSERT NhacLich(MaLichHen,KenhGui,ThoiDiemGui,TrangThai) VALUES({a.Id},{"Email"},{DateTime.UtcNow},{"Legacy"})");
            await migrator.MigrateAsync();var legacy=await f.Db.Set<NhacLich>().AsNoTracking().SingleAsync();Assert.Null(legacy.MaNhanVien);Assert.Null(legacy.NoiDungMoPhong);
            await migrator.MigrateAsync(prior);await f.Db.Database.ExecuteSqlInterpolatedAsync($"INSERT NhacLich(MaLichHen,KenhGui,ThoiDiemGui,TrangThai) VALUES({a.Id},{"LogMoPhong"},{DateTime.UtcNow},{"Legacy"}),({a.Id},{"LogMoPhong"},{DateTime.UtcNow},{"Legacy"})");
            await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(()=>migrator.MigrateAsync());
            // Only remove these two newly inserted conflict fixtures; retain legacy Email and every source row.
            await f.Db.Database.ExecuteSqlRawAsync("DELETE NhacLich WHERE KenhGui=N'LogMoPhong'");await migrator.MigrateAsync();
            using var created=await f.Reception.PostAsJsonAsync(Path(a.Id),new{});Assert.Equal(HttpStatusCode.Created,created.StatusCode);
            await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(()=>migrator.MigrateAsync(prior));Assert.False(f.Db.Database.HasPendingModelChanges());});
    }
}
