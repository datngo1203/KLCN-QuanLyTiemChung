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
using TiemChung.Infrastructure.Data;
using Xunit;
using Fixture = TiemChung.Tests.AppointmentSqlTests.Fixture;

namespace TiemChung.Tests;

public sealed class ClinicalSqlTests
{
    private const string Route="/api/v1/clinical/appointments";
    private const string Previous="20261031140502_S5AppointmentSource";
    private static string Assessment(int id)=>Route+"/"+id+"/assessment";
    private static async Task Problem(HttpResponseMessage response,int status,string code)
    {
        using(response){Assert.Equal((HttpStatusCode)status,response.StatusCode);Assert.Equal("no-store",response.Headers.CacheControl!.ToString());
            var text=await response.Content.ReadAsStringAsync();using var json=JsonDocument.Parse(text);Assert.Equal(code,json.RootElement.GetProperty("code").GetString());Assert.True(json.RootElement.TryGetProperty("traceId",out _));
            Assert.DoesNotContain("SqlException",text);Assert.DoesNotContain("[DEMO] private reason",text);}
    }
    private static async Task<AppointmentDto> Appointment(Fixture f,RegistrationDto? registration=null,string kind="vaccine")
    {
        registration??=await f.Register();
        using var response=await f.Reception.PostAsJsonAsync("/api/v1/appointments",f.Request(registration,registration.Items.Single(x=>x.Kind==kind)));
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);return (await response.Content.ReadFromJsonAsync<AppointmentDto>())!;
    }
    private static async Task<ClinicalAssessmentDto> Post(HttpClient doctor,int id,CreateClinicalAssessmentRequest request)
    {
        using var response=await doctor.PostAsJsonAsync(Assessment(id),request);Assert.Equal(HttpStatusCode.Created,response.StatusCode);
        Assert.Equal("no-store",response.Headers.CacheControl!.ToString());Assert.Equal(Assessment(id),response.Headers.Location!.OriginalString);
        return (await response.Content.ReadFromJsonAsync<ClinicalAssessmentDto>())!;
    }
    private static async Task<string> Unrelated(TiemChungDbContext db)
    {
        var rows=new List<string>();await db.Database.OpenConnectionAsync();
        try {foreach(var table in new[]{"LichHenTiem","DangKyDichVu","ThanhToan","MuiTiem","LoVacXin","BienDongKho","NhacLich"}) {
            await using var command=db.Database.GetDbConnection().CreateCommand();command.CommandText=$"SELECT (SELECT * FROM [{table}] FOR JSON PATH, INCLUDE_NULL_VALUES)";
            rows.Add(Convert.ToString(await command.ExecuteScalarAsync())!);
        }}finally{await db.Database.CloseConnectionAsync();}
        return string.Join("\n",rows);
    }
    [Theory,Trait("Category","SqlIntegration")]
    [InlineData("vaccine")][InlineData("package")]
    public async Task DoctorRecordsSourceVaccineAndUtcAuditReadbackWithoutChangingOtherModules(string kind)
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            var appointment=await Appointment(f,kind:kind);using var doctor=await f.Actor("BacSi",false);var accountId=f.LastAccountId;
            var employeeId=(await f.Db.Set<TaiKhoan>().SingleAsync(x=>x.MaTaiKhoan==accountId)).MaNhanVien!.Value;
            f.Db.Add(new LoVacXin {SoLo="[DEMO] S53 preserved",MaVacXin=(await f.Db.Vaccines.FirstAsync()).MaVacXin,NgaySanXuat=new(2026,1,1),HanSuDung=new(2028,1,1),SoLuongBanDau=10,SoLuongTon=10,TrangThai="Active"});await f.Db.SaveChangesAsync();
            var before=await Unrelated(f.Db);
            var detail=(await doctor.GetFromJsonAsync<ClinicalAppointmentDto>(Route+"/"+appointment.Id))!;
            var vaccine=Assert.Single(detail.AllowedVaccines);Assert.Equal(appointment.ServiceName,detail.Appointment.ServiceName);
            Assert.Null((await doctor.GetFromJsonAsync<ClinicalAssessmentResponse>(Assessment(appointment.Id)))!.Assessment);
            var start=DateTimeOffset.UtcNow;var saved=await Post(doctor,appointment.Id,new("Đủ điều kiện",null,[vaccine.Id]));
            Assert.Equal(employeeId,saved.Doctor.Id);Assert.InRange(saved.RecordedAt,start,DateTimeOffset.UtcNow);Assert.Equal(TimeSpan.Zero,saved.RecordedAt.Offset);
            Assert.Equal(appointment.RegistrationId,saved.RegistrationId);Assert.Equal(appointment.Recipient.Id,saved.RecipientId);Assert.Null(saved.VitalSigns);Assert.Null(saved.MedicalHistory);
            var indication=Assert.Single(saved.Indications);Assert.Equal(vaccine.Id,indication.VaccineId);Assert.Equal("DuocTiem",indication.Result);Assert.Equal(saved.RecordedAt,indication.RecordedAt);
            Assert.Equal(JsonSerializer.Serialize(saved),JsonSerializer.Serialize((await doctor.GetFromJsonAsync<ClinicalAssessmentResponse>(Assessment(appointment.Id)))!.Assessment));
            f.Db.ChangeTracker.Clear();var row=await f.Db.Set<PhieuKhamSangLoc>().SingleAsync();Assert.Equal(saved.Id,row.MaPhieuKham);Assert.Equal(appointment.Id,row.MaLichHen);Assert.Null(row.ChiSoSinhTon);Assert.Null(row.TienSuBenhLy);
            var audit=await f.Db.Set<SecurityAudit>().SingleAsync(x=>x.Action=="clinical.assessed");Assert.Equal("Success",audit.Outcome);Assert.Equal("ClinicalAssessment",audit.ResourceType);Assert.Equal(saved.Id.ToString(),audit.ResourceId);Assert.Equal(accountId,audit.ActorAccountId);Assert.NotNull(audit.SessionId);
            await Problem(await doctor.PostAsJsonAsync(Assessment(appointment.Id),new CreateClinicalAssessmentRequest("Đủ điều kiện",null,[vaccine.Id])),409,"assessmentAlreadyRecorded");
            Assert.Equal(before,await Unrelated(f.Db));Assert.Single(await f.Db.Set<PhieuKhamSangLoc>().ToArrayAsync());Assert.Single(await f.Db.Set<ChiDinhTiem>().ToArrayAsync());
            Assert.Equal(appointment.Id,Assert.Single((await doctor.GetFromJsonAsync<AppointmentListResponse>(Route+"?search=LH-"+appointment.Id.ToString("0000")))!.Items).Id);
            Assert.Empty((await doctor.GetFromJsonAsync<AppointmentListResponse>(Route+"?search=missing"))!.Items);
            Assert.Empty((await doctor.GetFromJsonAsync<AppointmentListResponse>(Route+"?page=2&pageSize=1"))!.Items);
        });
    }
    [Theory,Trait("Category","SqlIntegration")]
    [InlineData(1)][InlineData(2)]
    public async Task PackageAllowsExplicitSubsetOrMultipleButNotOtherRegistrationVaccine(int count)
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            var first=await f.Db.Vaccines.FirstAsync();var package=await f.Db.Set<GoiVacXin>().FirstAsync();
            VacXin Vaccine(string name)=>new(){TenVacXin=name,MaLoaiTiemChung=first.MaLoaiTiemChung,MaLoaiVacXin=first.MaLoaiVacXin,TrangThai="Active",Gia=200};
            var second=Vaccine("[DEMO] component 2");var outside=Vaccine("[DEMO] other registration vaccine");f.Db.AddRange(second,outside);await f.Db.SaveChangesAsync();
            f.Db.Add(new GoiVacXin_VacXin {MaGoi=package.MaGoi,MaVacXin=second.MaVacXin,SoLuong=3});await f.Db.SaveChangesAsync();
            async Task<RegistrationDto> Register(RegistrationItemRequest[] items){using var response=await f.Reception.PostAsJsonAsync("/api/v1/registrations",new CreateRegistrationRequest(f.CustomerId,f.RecipientId,items));Assert.Equal(HttpStatusCode.Created,response.StatusCode);return (await response.Content.ReadFromJsonAsync<RegistrationDto>())!;}
            var other=await Register([new("vaccine",outside.MaVacXin,1,outside.Gia)]);
            var registration=await Register([new("package",package.MaGoi,4,package.GiaGoi,[new(first.MaVacXin,2),new(second.MaVacXin,3)])]);
            var appointment=await Appointment(f,registration,"package");using var doctor=await f.Actor("BacSi",false);
            var detail=(await doctor.GetFromJsonAsync<ClinicalAppointmentDto>(Route+"/"+appointment.Id))!;Assert.Equal(2,detail.AllowedVaccines.Length);
            await Problem(await doctor.PostAsJsonAsync(Assessment(appointment.Id),new CreateClinicalAssessmentRequest("Đủ điều kiện",null,[outside.MaVacXin])),400,"validation");
            var saved=await Post(doctor,appointment.Id,new("Đủ điều kiện",null,detail.AllowedVaccines.Take(count).Select(v=>v.Id).ToArray()));Assert.Equal(count,saved.Indications.Length);
            Assert.DoesNotContain(saved.Indications,v=>v.VaccineId==other.Items[0].ServiceId);Assert.Empty(await f.Db.Set<MuiTiem>().ToArrayAsync());
        });
    }
    [Theory,Trait("Category","SqlIntegration")]
    [InlineData("Tạm hoãn")][InlineData("Chống chỉ định")]
    public async Task NonEligibleConclusionsRequireReasonAndNeverProduceIndications(string conclusion)
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            var a=await Appointment(f);using var doctor=await f.Actor("BacSi",false);var before=await Unrelated(f.Db);
            await Problem(await doctor.PostAsJsonAsync(Assessment(a.Id),new CreateClinicalAssessmentRequest(conclusion," ",[])),400,"validation");
            await Problem(await doctor.PostAsJsonAsync(Assessment(a.Id),new CreateClinicalAssessmentRequest(conclusion,"[DEMO] private reason",[a.ServiceId])),400,"validation");
            var saved=await Post(doctor,a.Id,new(conclusion,"  [DEMO] private reason  ",[]));Assert.Equal("[DEMO] private reason",saved.Reason);Assert.Equal(conclusion,saved.Conclusion);Assert.Empty(saved.Indications);
            Assert.Empty(await f.Db.Set<ChiDinhTiem>().ToArrayAsync());Assert.Equal(before,await Unrelated(f.Db));
        });
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task InvalidBodiesQueriesAndWrongVaccineLeaveNoPartialRecords()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            var a=await Appointment(f);using var doctor=await f.Actor("BacSi",false);
            var valid=new CreateClinicalAssessmentRequest("Đủ điều kiện",null,[a.ServiceId]);
            foreach(var req in new[]{valid with {Conclusion=null!},valid with{Conclusion=""},valid with{Conclusion="unknown"},valid with{VaccineIds=null!},valid with{VaccineIds=[]},valid with{VaccineIds=[0]},valid with{VaccineIds=[int.MaxValue]},valid with{VaccineIds=[a.ServiceId,a.ServiceId]},valid with{Reason=new string('x',501)},valid with{VaccineIds=Enumerable.Range(1,101).ToArray()}})
                await Problem(await doctor.PostAsJsonAsync(Assessment(a.Id),req),400,"validation");
            foreach(var json in new[]{"{","{}","null","{\"conclusion\":\"Đủ điều kiện\",\"vaccineIds\":[1],\"actorId\":1}"})await Problem(await doctor.PostAsync(Assessment(a.Id),new StringContent(json,Encoding.UTF8,"application/json")),400,"validation");
            foreach(var query in new[]{"page=0","pageSize=101","page=2147483647&pageSize=100","search="+new string('x',101)})await Problem(await doctor.GetAsync(Route+"?"+query),400,"validation");
            await Problem(await doctor.GetAsync(Route+"/0"),400,"validation");await Problem(await doctor.GetAsync(Assessment(int.MaxValue)),404,"resourceNotFound");
            Assert.Empty(await f.Db.Set<PhieuKhamSangLoc>().ToArrayAsync());Assert.Empty(await f.Db.Set<ChiDinhTiem>().ToArrayAsync());Assert.Empty(await f.Db.Set<SecurityAudit>().Where(x=>x.Action=="clinical.assessed").ToArrayAsync());
        });
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task EveryRouteRequiresCurrentActiveDoctorAndNoOtherTitleOrCustomerCanReadClinicalData()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            var a=await Appointment(f);
            async Task Denied(HttpClient client,int status,string code){await Problem(await client.GetAsync(Route),status,code);await Problem(await client.GetAsync(Route+"/"+a.Id),status,code);await Problem(await client.GetAsync(Assessment(a.Id)),status,code);await Problem(await client.PostAsJsonAsync(Assessment(a.Id),new CreateClinicalAssessmentRequest("Đủ điều kiện",null,[a.ServiceId])),status,code);}
            using var anonymous=f.Base.Factory.CreateClient();await Denied(anonymous,401,"invalidSession");
            foreach(var title in new string?[]{null,"LeTan","Admin","ThuNgan","DieuDuong","ThuKho"}){using var actor=await f.Actor(title,true);await Denied(actor,403,"permissionDenied");}
            foreach(var invalid in new[]{"employee","account","session","version"}){
                using var doctor=await f.Actor("BacSi",false);var id=f.LastAccountId;
                if(invalid=="employee"){var employee=(await f.Db.Set<TaiKhoan>().SingleAsync(x=>x.MaTaiKhoan==id)).MaNhanVien;await f.Db.Set<NhanVien>().Where(x=>x.MaNhanVien==employee).ExecuteUpdateAsync(x=>x.SetProperty(e=>e.TrangThai,"Inactive"));}
                if(invalid=="account")await f.Db.Set<TaiKhoan>().Where(x=>x.MaTaiKhoan==id).ExecuteUpdateAsync(x=>x.SetProperty(e=>e.TrangThai,"Inactive"));
                if(invalid=="session")await f.Db.Set<IdentitySession>().Where(x=>x.AccountId==id).ExecuteUpdateAsync(x=>x.SetProperty(e=>e.RevokedAt,DateTimeOffset.UtcNow));
                if(invalid=="version")await f.Db.Set<TaiKhoan>().Where(x=>x.MaTaiKhoan==id).ExecuteUpdateAsync(x=>x.SetProperty(e=>e.SecurityVersion,e=>e.SecurityVersion+1));
                await Denied(doctor,401,"invalidSession");
            }
        });
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task LegacyAndInconsistentParentSourcesAreExcludedAndCannotBeAssessed()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            var a=await Appointment(f);var legacy=new LichHenTiem{MaNguoiDuocTiem=f.RecipientId,ThoiGianHen=DateTime.UtcNow,DiemTiem="[DEMO] legacy",TrangThai="Planned"};f.Db.Add(legacy);await f.Db.SaveChangesAsync();
            using var doctor=await f.Actor("BacSi",false);
            Assert.Single((await doctor.GetFromJsonAsync<AppointmentListResponse>(Route))!.Items);
            var other=new KhachHang{HoTen="[DEMO] changed customer"};f.Db.Add(other);await f.Db.SaveChangesAsync();await f.Db.Recipients.Where(x=>x.MaNguoiDuocTiem==f.RecipientId).ExecuteUpdateAsync(x=>x.SetProperty(p=>p.MaKhachHang,other.MaKhachHang));
            Assert.Empty((await doctor.GetFromJsonAsync<AppointmentListResponse>(Route))!.Items);
            foreach(var id in new[]{a.Id,legacy.MaLichHen,int.MaxValue}){await Problem(await doctor.GetAsync(Route+"/"+id),404,"resourceNotFound");await Problem(await doctor.PostAsJsonAsync(Assessment(id),new CreateClinicalAssessmentRequest("Đủ điều kiện",null,[a.ServiceId])),404,"resourceNotFound");}
        });
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task ConcurrentDoctorsCreateOneAssessmentAndOneAudit()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            var a=await Appointment(f);using var one=await f.Actor("BacSi",false);using var two=await f.Actor("BacSi",false);var req=new CreateClinicalAssessmentRequest("Đủ điều kiện",null,[a.ServiceId]);
            var responses=await Task.WhenAll(one.PostAsJsonAsync(Assessment(a.Id),req),two.PostAsJsonAsync(Assessment(a.Id),req));
            try{Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Created);Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Conflict);}finally{foreach(var r in responses)r.Dispose();}
            Assert.Single(await f.Db.Set<PhieuKhamSangLoc>().ToArrayAsync());Assert.Single(await f.Db.Set<ChiDinhTiem>().ToArrayAsync());Assert.Single(await f.Db.Set<SecurityAudit>().Where(x=>x.Action=="clinical.assessed").ToArrayAsync());
        });
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task AuditFailureRollsBackAssessmentAndIndications()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            var a=await Appointment(f);using var doctor=await f.Actor("BacSi",false);
            await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER S53AuditFailure ON dbo.SecurityAudit AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE Action='clinical.assessed') THROW 51000, 'Synthetic audit failure', 1;");
            await Problem(await doctor.PostAsJsonAsync(Assessment(a.Id),new CreateClinicalAssessmentRequest("Đủ điều kiện",null,[a.ServiceId])),503,"dependencyUnavailable");
            Assert.Empty(await f.Db.Set<PhieuKhamSangLoc>().ToArrayAsync());Assert.Empty(await f.Db.Set<ChiDinhTiem>().ToArrayAsync());Assert.Empty(await f.Db.Set<SecurityAudit>().Where(x=>x.Action=="clinical.assessed").ToArrayAsync());
        });
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task CompositeForeignKeyAndUniqueIndexesProtectDirectSqlWrites()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            var a=await Appointment(f);var other=await f.Register();
            PhieuKhamSangLoc Row()=>new(){MaLichHen=a.Id,MaDangKy=a.RegistrationId,MaNguoiDuocTiem=a.Recipient.Id,MaNhanVien=f.EmployeeId,ThoiDiemKham=DateTimeOffset.UtcNow,KetLuan="[DEMO] constraint fixture"};
            var wrong=Row();wrong.MaDangKy=other.Id;f.Db.Add(wrong);await Assert.ThrowsAsync<DbUpdateException>(()=>f.Db.SaveChangesAsync());f.Db.ChangeTracker.Clear();
            var row=Row();f.Db.Add(row);await f.Db.SaveChangesAsync();var copy=Row();f.Db.Add(copy);await Assert.ThrowsAsync<DbUpdateException>(()=>f.Db.SaveChangesAsync());f.Db.ChangeTracker.Clear();
            ChiDinhTiem Indication()=>new(){MaPhieuKham=row.MaPhieuKham,MaVacXin=a.ServiceId,ThoiGian=DateTime.UtcNow,KetQuaChiDinh="DuocTiem",GhiChu=""};
            f.Db.Add(Indication());await f.Db.SaveChangesAsync();f.Db.Add(Indication());await Assert.ThrowsAsync<DbUpdateException>(()=>f.Db.SaveChangesAsync());f.Db.ChangeTracker.Clear();
        });
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task MigrationPreservesLegacyAndBlocksLossyDownWithoutFakeBackfill()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            var r=await f.Register();var migrator=f.Db.GetService<IMigrator>();await migrator.MigrateAsync(Previous);
            await f.Db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO PhieuKhamSangLoc(MaDangKy,MaNguoiDuocTiem,MaNhanVien,ChiSoSinhTon,TienSuBenhLy,KetLuan,LyDoHoan) VALUES({r.Id},{f.RecipientId},{f.EmployeeId},{"[DEMO] preserved vital text"},{"[DEMO] preserved history"},{"[DEMO] old conclusion"},{"[DEMO] old reason"})");
            await migrator.MigrateAsync();await migrator.MigrateAsync();var legacy=await f.Db.Set<PhieuKhamSangLoc>().AsNoTracking().SingleAsync();Assert.Null(legacy.MaLichHen);Assert.Null(legacy.ThoiDiemKham);Assert.Equal("[DEMO] preserved vital text",legacy.ChiSoSinhTon);Assert.Equal("[DEMO] preserved history",legacy.TienSuBenhLy);Assert.Equal("[DEMO] old conclusion",legacy.KetLuan);Assert.Equal("[DEMO] old reason",legacy.LyDoHoan);
            await migrator.MigrateAsync(Previous);await migrator.MigrateAsync();
            var a=await Appointment(f,r);using var doctor=await f.Actor("BacSi",false);await Post(doctor,a.Id,new("Tạm hoãn","[DEMO] reason",[]));
            await Assert.ThrowsAsync<SqlException>(()=>migrator.MigrateAsync(Previous));Assert.Equal(2,await f.Db.Set<PhieuKhamSangLoc>().CountAsync());Assert.False(f.Db.Database.HasPendingModelChanges());
        });
    }
    [Fact,Trait("Category","SqlIntegration")]
    public async Task MigrationRefusesDuplicateLegacyIndicationsWithoutDeletingThem()
    {
        await SqlFixtureLifecycle.RunAsync(()=>Fixture.Create(),async f=>{
            var r=await f.Register();var migrator=f.Db.GetService<IMigrator>();await migrator.MigrateAsync(Previous);var v=r.Items.First(x=>x.Kind=="vaccine").ServiceId;
            await f.Db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO PhieuKhamSangLoc(MaDangKy,MaNguoiDuocTiem,MaNhanVien,ChiSoSinhTon,TienSuBenhLy,KetLuan) VALUES({r.Id},{f.RecipientId},{f.EmployeeId},{"[DEMO] legacy vitals"},{"[DEMO] legacy history"},{"[DEMO] legacy conclusion"})");
            for(var i=0;i<2;i++)await f.Db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ChiDinhTiem(MaPhieuKham,MaVacXin,KetQuaChiDinh,ThoiGian,GhiChu) SELECT MaPhieuKham,{v},{"DuocTiem"},{DateTime.UtcNow},{"[DEMO] duplicate preserved"} FROM PhieuKhamSangLoc");
            await Assert.ThrowsAsync<SqlException>(()=>migrator.MigrateAsync());Assert.Equal(2,await f.Db.Set<ChiDinhTiem>().CountAsync());
        });
    }
}
