using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TiemChung.Api;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class VaccinationSqlTests
{
    private const string Route = "/api/v1/vaccinations";
    internal static string ResultPath(int id) => Route + $"/indications/{id}/result";
    private static RecordVaccinationRequest Request(int id, string lot="[DEMO] S5-VALID-A") => new(id, lot, " Bắp tay trái ");
    private static async Task Problem(HttpResponseMessage response, int status, string code)
    {
        using (response) {
            Assert.Equal((HttpStatusCode)status, response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
            var text=await response.Content.ReadAsStringAsync();
            using var json=JsonDocument.Parse(text); Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
            Assert.True(json.RootElement.TryGetProperty("traceId", out _)); Assert.DoesNotContain("SqlException",text);
        }
    }
    private static Task Run(Func<Fixture,Task> test) => SqlFixtureLifecycle.RunAsync(Fixture.Create, test);

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("vaccine")][InlineData("package")]
    public async Task ActualSqlPersistsShotStockMovementAuditAndReadbackForSingleOrSelectedPackageComponent(string kind)
    {
        await Run(async f => {
            var source=await f.Source(kind, vaccineIds:[f.A]); var indication=Assert.Single(source.Assessment.Indications);
            var list=(await f.Nurse.GetFromJsonAsync<VaccinationListResponse>(Route+"/eligible"))!;
            Assert.Equal(indication.Id, Assert.Single(list.Items).Id);
            using var detailResponse=await f.Nurse.GetAsync(Route+$"/indications/{indication.Id}");
            Assert.Equal("no-store",detailResponse.Headers.CacheControl!.ToString());
            var text=await detailResponse.Content.ReadAsStringAsync(); Assert.DoesNotContain("medicalHistory",text); Assert.DoesNotContain("reason",text); Assert.DoesNotContain("vitalSigns",text);
            var detail=JsonSerializer.Deserialize<VaccinationDetailDto>(text,new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            Assert.Equal(f.A,detail.Indication.Vaccine.Id); Assert.Equal(source.Appointment.Id,detail.Indication.AppointmentId);
            Assert.Single(detail.Lots); Assert.Equal("[DEMO] S5-VALID-A",detail.Lots[0].Number);
            Assert.Equal(VaccinationEndpoints.FacilityDate(DateTimeOffset.UtcNow), detail.FacilityDate);
            Assert.Null((await f.Nurse.GetFromJsonAsync<VaccinationResultResponse>(ResultPath(indication.Id)))!.Vaccination);
            var start=DateTimeOffset.UtcNow;
            using var response=await f.Nurse.PostAsJsonAsync(Route,Request(indication.Id));
            Assert.Equal(HttpStatusCode.Created,response.StatusCode); Assert.Equal("no-store",response.Headers.CacheControl!.ToString());
            Assert.Equal(ResultPath(indication.Id),response.Headers.Location!.OriginalString);
            var result=(await response.Content.ReadFromJsonAsync<VaccinationDto>())!;
            Assert.Equal(indication.Id,result.IndicationId); Assert.Equal(source.Appointment.Id,result.AppointmentId);
            Assert.Equal(source.Registration.Id,result.RegistrationId); Assert.Equal(f.Base.RecipientId,result.Recipient.Id);
            Assert.Equal(f.A,result.Vaccine.Id); Assert.Equal("Bắp tay trái",result.InjectionSite); Assert.Equal("DaTiem",result.Status);
            Assert.InRange(result.AdministeredAt,start,DateTimeOffset.UtcNow); Assert.Equal(TimeSpan.Zero,result.AdministeredAt.Offset);
            Assert.Equal(f.NurseEmployeeId,result.Nurse.Id);
            f.Db.ChangeTracker.Clear();
            var shot=await f.Db.Set<MuiTiem>().SingleAsync(); var movement=await f.Db.Set<BienDongKho>().SingleAsync();
            Assert.Equal(result.Id,shot.MaMuiTiem); Assert.Equal(indication.Id,shot.MaChiDinh);
            Assert.Equal(99,(await f.Db.Set<LoVacXin>().SingleAsync(l=>l.SoLo==result.LotNumber)).SoLuongTon);
            Assert.Equal(shot.MaMuiTiem,movement.MaMuiTiem); Assert.Equal(shot.SoLo,movement.SoLo); Assert.Equal(shot.MaNhanVien,movement.MaNhanVien);
            Assert.Equal(shot.ThoiDiemTiem,movement.ThoiDiem); Assert.Equal(1,movement.SoLuong); Assert.Equal("XuatDoTiem",movement.LoaiBienDong);
            var audit=await f.Db.Set<SecurityAudit>().SingleAsync(x=>x.Action=="vaccinations.recorded");
            Assert.Equal("Success",audit.Outcome); Assert.Equal("Vaccination",audit.ResourceType); Assert.Equal(shot.MaMuiTiem.ToString(),audit.ResourceId);
            Assert.Equal(f.NurseAccountId,audit.ActorAccountId); Assert.NotNull(audit.SessionId);
            Assert.Empty(await f.Db.Set<TheoDoiSauTiem>().ToArrayAsync());
            var before=await f.Snapshot();
            for(var i=0;i<2;i++) Assert.Equal(JsonSerializer.Serialize(result),JsonSerializer.Serialize((await f.Nurse.GetFromJsonAsync<VaccinationResultResponse>(ResultPath(indication.Id)))!.Vaccination));
            await Problem(await f.Nurse.PostAsJsonAsync(Route,Request(indication.Id)),409,"vaccinationAlreadyRecorded");
            Assert.Equal(before,await f.Snapshot());
            Assert.Empty((await f.Nurse.GetFromJsonAsync<VaccinationListResponse>(Route+"/eligible"))!.Items);
            // Stored result remains readable after current lot/payment conditions change.
            await f.Db.Set<LoVacXin>().Where(l=>l.SoLo==result.LotNumber).ExecuteUpdateAsync(s=>s.SetProperty(l=>l.SoLuongTon,0).SetProperty(l=>l.TrangThai,"Inactive"));
            await f.Db.Payments.Where(p=>p.MaDangKy==source.Registration.Id).ExecuteDeleteAsync();
            Assert.Equal(result.Id,(await f.Nurse.GetFromJsonAsync<VaccinationResultResponse>(ResultPath(indication.Id)))!.Vaccination!.Id);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task PackageWithMultipleIndicationsRecordsEachIndependently()
    {
        await Run(async f => {
            var source=await f.Source("package",vaccineIds:[f.A,f.B]);
            Assert.Equal(2,source.Assessment.Indications.Length);
            foreach(var indication in source.Assessment.Indications) {
                using var response=await f.Nurse.PostAsJsonAsync(Route,Request(indication.Id,indication.VaccineId==f.A?"[DEMO] S5-VALID-A":"[DEMO] S55-VALID-B"));
                Assert.Equal(HttpStatusCode.Created,response.StatusCode);
                Assert.Equal(indication.VaccineId,(await response.Content.ReadFromJsonAsync<VaccinationDto>())!.Vaccine.Id);
            }
            Assert.Equal(2,await f.Db.Set<MuiTiem>().CountAsync()); Assert.Equal(2,await f.Db.Set<BienDongKho>().CountAsync());
            Assert.Equal(2,await f.Db.Set<SecurityAudit>().CountAsync(x=>x.Action=="vaccinations.recorded"));
            Assert.Equal(99,(await f.Db.Set<LoVacXin>().AsNoTracking().SingleAsync(l=>l.SoLo=="[DEMO] S5-VALID-A")).SoLuongTon);
            Assert.Equal(9,(await f.Db.Set<LoVacXin>().AsNoTracking().SingleAsync(l=>l.SoLo=="[DEMO] S55-VALID-B")).SoLuongTon);
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("Tạm hoãn","vaccinationNotIndicated")][InlineData("Chống chỉ định","vaccinationNotIndicated")]
    [InlineData("wrongVaccine","vaccinationSourceConflict")][InlineData("wrongResult","vaccinationNotIndicated")]
    [InlineData("legacy","vaccinationSourceConflict")][InlineData("registrationStatus","vaccinationSourceConflict")]
    public async Task RefusesInvalidClinicalOrAppointmentSourceWithoutChangingAnyInventory(string scenario,string error)
    {
        await Run(async f => {
            var source=await f.Source(); var id=source.Assessment.Indications.Single().Id;
            if(scenario is "Tạm hoãn" or "Chống chỉ định")
                await f.Db.Set<PhieuKhamSangLoc>().Where(p=>p.MaPhieuKham==source.Assessment.Id).ExecuteUpdateAsync(s=>s.SetProperty(p=>p.KetLuan,scenario));
            if(scenario=="wrongVaccine") await f.Db.Set<ChiDinhTiem>().Where(i=>i.MaChiDinh==id).ExecuteUpdateAsync(s=>s.SetProperty(i=>i.MaVacXin,f.B));
            if(scenario=="wrongResult") await f.Db.Set<ChiDinhTiem>().Where(i=>i.MaChiDinh==id).ExecuteUpdateAsync(s=>s.SetProperty(i=>i.KetQuaChiDinh,"KhongDuocTiem"));
            if(scenario=="legacy") await f.Db.Set<PhieuKhamSangLoc>().Where(p=>p.MaPhieuKham==source.Assessment.Id).ExecuteUpdateAsync(s=>s.SetProperty(p=>p.MaLichHen,(int?)null).SetProperty(p=>p.ThoiDiemKham,(DateTimeOffset?)null));
            if(scenario=="registrationStatus") await f.Db.Registrations.Where(r=>r.MaDangKy==source.Registration.Id).ExecuteUpdateAsync(s=>s.SetProperty(r=>r.TrangThai,"Legacy"));
            var before=await f.Snapshot();
            await Problem(await f.Nurse.PostAsJsonAsync(Route,Request(id)),409,error);
            Assert.Equal(before,await f.Snapshot());
            Assert.Empty((await f.Nurse.GetFromJsonAsync<VaccinationListResponse>(Route+"/eligible"))!.Items);
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("missing","vaccinationPaymentRequired")][InlineData("otherRegistration","vaccinationPaymentRequired")]
    [InlineData("amount","paymentDataConflict")][InlineData("status","paymentDataConflict")]
    [InlineData("method","paymentDataConflict")][InlineData("type","paymentDataConflict")]
    [InlineData("zero","vaccinationZeroTotalNotSupported")][InlineData("discount","paymentDataConflict")]
    [InlineData("fraction","paymentDataConflict")][InlineData("quantity","paymentDataConflict")]
    public async Task ReusesG3PaymentRulesAndFailsClosedForZeroOrInconsistentMoney(string scenario,string code)
    {
        await Run(async f => {
            var source=await f.Source(); var id=source.Assessment.Indications.Single().Id;
            var payment=await f.Db.Payments.SingleAsync(p=>p.MaDangKy==source.Registration.Id);
            if(scenario is "missing" or "zero")f.Db.Remove(payment);
            if(scenario=="otherRegistration")payment.MaDangKy=(await f.Register()).Id;
            if(scenario=="amount")payment.SoTien--;
            if(scenario=="status")payment.TrangThai="Legacy";
            if(scenario=="method")payment.PhuongThuc="Legacy";
            if(scenario=="type")payment.LoaiThanhToan="Legacy";
            if(scenario=="discount") {
                var discount=new KhuyenMai {TenKhuyenMai="[DEMO] unsnapshotted",ThoiGianBatDau=DateTime.UtcNow.AddDays(-1),ThoiGianKetThuc=DateTime.UtcNow.AddDays(1),MucUuDai=1};
                f.Db.Add(discount);await f.Db.SaveChangesAsync();
                (await f.Db.Registrations.SingleAsync(r=>r.MaDangKy==source.Registration.Id)).MaKhuyenMai=discount.MaKhuyenMai;
            }
            await f.Db.SaveChangesAsync();f.Db.ChangeTracker.Clear();
            if(scenario=="zero") {
                await f.Db.RegistrationVaccineItems.Where(i=>i.MaDangKy==source.Registration.Id).ExecuteUpdateAsync(s=>s.SetProperty(i=>i.GiaTaiThoiDiemDangKy,0));
                await f.Db.RegistrationPackageItems.Where(i=>i.MaDangKy==source.Registration.Id).ExecuteUpdateAsync(s=>s.SetProperty(i=>i.GiaTaiThoiDiemDangKy,0));
            }
            if(scenario=="fraction")await f.Db.RegistrationVaccineItems.Where(i=>i.MaDangKy==source.Registration.Id).ExecuteUpdateAsync(s=>s.SetProperty(i=>i.GiaTaiThoiDiemDangKy,1.5m));
            if(scenario=="quantity")await f.Db.RegistrationVaccineItems.Where(i=>i.MaDangKy==source.Registration.Id).ExecuteUpdateAsync(s=>s.SetProperty(i=>i.SoLuong,101));
            var before=await f.Snapshot();
            await Problem(await f.Nurse.GetAsync(Route+$"/indications/{id}"),409,code);
            await Problem(await f.Nurse.PostAsJsonAsync(Route,Request(id)),409,code);
            Assert.Equal(before,await f.Snapshot());
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("expired")][InlineData("empty")][InlineData("inactive")][InlineData("wrongVaccine")]
    [InlineData("future")][InlineData("inconsistent")][InlineData("missing")]
    public async Task InvalidLotsAreNeverConsumedAndUnavailableLotsAreNotOffered(string scenario)
    {
        await Run(async f => {
            var source=await f.Source();var id=source.Assessment.Indications.Single().Id;
            var number="[DEMO] S5-VALID-A";
            if(scenario=="expired") number="[DEMO] S5-EXPIRED-A";
            if(scenario=="empty") await f.Db.Set<LoVacXin>().Where(l=>l.SoLo==number).ExecuteUpdateAsync(s=>s.SetProperty(l=>l.SoLuongTon,0));
            if(scenario=="inactive") await f.Db.Set<LoVacXin>().Where(l=>l.SoLo==number).ExecuteUpdateAsync(s=>s.SetProperty(l=>l.TrangThai,"Inactive"));
            if(scenario=="wrongVaccine")number="[DEMO] S55-VALID-B";
            if(scenario=="future")await f.Db.Set<LoVacXin>().Where(l=>l.SoLo==number).ExecuteUpdateAsync(s=>s.SetProperty(l=>l.NgaySanXuat,VaccinationEndpoints.FacilityDate(DateTimeOffset.UtcNow).AddDays(1)));
            if(scenario=="inconsistent")await f.Db.Set<LoVacXin>().Where(l=>l.SoLo==number).ExecuteUpdateAsync(s=>s.SetProperty(l=>l.SoLuongTon,101));
            if(scenario=="missing")number="[DEMO] S55 missing";
            var detail=(await f.Nurse.GetFromJsonAsync<VaccinationDetailDto>(Route+$"/indications/{id}"))!;
            Assert.DoesNotContain(detail.Lots,l=>l.Number==number);
            var before=await f.Snapshot();
            await Problem(await f.Nurse.PostAsJsonAsync(Route,Request(id,number)),409,"vaccinationLotUnavailable");
            Assert.Equal(before,await f.Snapshot());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task ExpiryOnFacilityDayIsUsableAndTomorrowHasExpiredYesterdayInVietnam()
    {
        await Run(async f => {
            var source=await f.Source();var id=source.Assessment.Indications.Single().Id;
            var day=VaccinationEndpoints.FacilityDate(DateTimeOffset.UtcNow);
            await f.Db.Set<LoVacXin>().Where(l=>l.SoLo=="[DEMO] S5-VALID-A").ExecuteUpdateAsync(s=>s.SetProperty(l=>l.HanSuDung,day));
            using var response=await f.Nurse.PostAsJsonAsync(Route,Request(id));Assert.Equal(HttpStatusCode.Created,response.StatusCode);
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("Admin")][InlineData("LeTan")][InlineData("BacSi")][InlineData("ThuNgan")][InlineData("ThuKho")][InlineData(null)]
    public async Task AllOtherRolesAndCustomerAreDeniedEvenWhenTheyHaveUnrelatedPermissions(string? title)
    {
        await Run(async f => {
            var source=await f.Source();var id=source.Assessment.Indications.Single().Id;
            using var actor=await f.Base.Actor(title,true);var before=await f.Snapshot();
            foreach(var path in new[]{Route+"/eligible",Route+$"/indications/{id}",ResultPath(id)}) await Problem(await actor.GetAsync(path),403,"permissionDenied");
            await Problem(await actor.PostAsJsonAsync(Route,Request(id)),403,"permissionDenied");
            Assert.Equal(before,await f.Snapshot());
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("account")][InlineData("employee")][InlineData("revoked")][InlineData("version")]
    public async Task CurrentIdentityMustRemainActive(string scenario)
    {
        await Run(async f => {
            var source=await f.Source();var id=source.Assessment.Indications.Single().Id;
            if(scenario=="account")await f.Db.Set<TaiKhoan>().Where(x=>x.MaTaiKhoan==f.NurseAccountId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TrangThai,"Inactive"));
            if(scenario=="employee")await f.Db.Set<NhanVien>().Where(x=>x.MaNhanVien==f.NurseEmployeeId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TrangThai,"Inactive"));
            if(scenario=="revoked")await f.Db.Set<IdentitySession>().Where(x=>x.AccountId==f.NurseAccountId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.RevokedAt,DateTimeOffset.UtcNow));
            if(scenario=="version")await f.Db.Set<TaiKhoan>().Where(x=>x.MaTaiKhoan==f.NurseAccountId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.SecurityVersion,x=>x.SecurityVersion+1));
            var before=await f.Snapshot();
            await Problem(await f.Nurse.PostAsJsonAsync(Route,Request(id)),401,"invalidSession");
            await Problem(await f.Nurse.GetAsync(Route+"/eligible"),401,"invalidSession");Assert.Equal(before,await f.Snapshot());
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData(false)][InlineData(true)]
    public async Task ConcurrentPostsProtectDuplicateIndicationAndLastStockAcrossDifferentRegistrations(bool lastStock)
    {
        await Run(async f => {
            var first=await f.Source(); var second=lastStock?await f.Source():first;
            if(lastStock)await f.Db.Set<LoVacXin>().Where(l=>l.SoLo=="[DEMO] S5-VALID-A").ExecuteUpdateAsync(s=>s.SetProperty(l=>l.SoLuongTon,1));
            var calls=await Task.WhenAll(f.Nurse.PostAsJsonAsync(Route,Request(first.Assessment.Indications.Single().Id)),
                f.OtherNurse.PostAsJsonAsync(Route,Request(second.Assessment.Indications.Single().Id)));
            try {
                Assert.Single(calls,r=>r.StatusCode==HttpStatusCode.Created);
                await Problem(Assert.Single(calls,r=>r.StatusCode!=HttpStatusCode.Created),409,lastStock?"vaccinationLotUnavailable":"vaccinationAlreadyRecorded");
            }finally{foreach(var response in calls)response.Dispose();}
            Assert.Single(await f.Db.Set<MuiTiem>().ToArrayAsync());Assert.Single(await f.Db.Set<BienDongKho>().ToArrayAsync());
            Assert.Equal(lastStock?0:99,(await f.Db.Set<LoVacXin>().AsNoTracking().SingleAsync(l=>l.SoLo=="[DEMO] S5-VALID-A")).SoLuongTon);
            Assert.Single(await f.Db.Set<SecurityAudit>().Where(x=>x.Action=="vaccinations.recorded").ToArrayAsync());
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("SecurityAudit")][InlineData("BienDongKho")]
    public async Task AuditOrMovementInsertFailureRollsBackShotStockMovementAndAudit(string table)
    {
        await Run(async f => {
            var source=await f.Source(); var id=source.Assessment.Indications.Single().Id; var before=await f.Snapshot();
            await f.Db.Database.ExecuteSqlRawAsync(table=="SecurityAudit"
                ? "ALTER TABLE SecurityAudit ADD CONSTRAINT S55FixtureFailure CHECK (Action <> N'vaccinations.recorded')"
                : "ALTER TABLE BienDongKho ADD CONSTRAINT S55FixtureFailure CHECK (MaMuiTiem IS NULL)");
            await Problem(await f.Nurse.PostAsJsonAsync(Route,Request(id)),503,"dependencyUnavailable");
            Assert.Equal(before,await f.Snapshot());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task SqlTimeoutWhileLotIsLockedLeavesNoPartialResult()
    {
        await Run(async f => {
            var source=await f.Source();var id=source.Assessment.Indications.Single().Id;var before=await f.Snapshot();
            await using var factory=f.Base.Base.Factory.WithWebHostBuilder(b=>b.ConfigureServices(s=>{
                s.RemoveAll<TiemChungDbContext>();s.RemoveAll<DbContextOptions<TiemChungDbContext>>();
                s.AddDbContext<TiemChungDbContext>(o=>o.UseSqlServer(f.Base.Base.Database.ConnectionString,sql=>sql.CommandTimeout(1)));
            }));
            using var client=factory.CreateClient();client.DefaultRequestHeaders.Authorization=f.Nurse.DefaultRequestHeaders.Authorization;
            await using var blocker=new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(f.Base.Base.Database.ConnectionString).Options);
            await using(var tx=await blocker.Database.BeginTransactionAsync()) {
                await blocker.Database.ExecuteSqlRawAsync("UPDATE LoVacXin SET SoLuongTon=SoLuongTon WHERE SoLo=N'[DEMO] S5-VALID-A'");
                await Problem(await client.PostAsJsonAsync(Route,Request(id)),503,"dependencyUnavailable");
                await tx.RollbackAsync();
            }
            Assert.Equal(before,await f.Snapshot());Assert.Null((await f.Nurse.GetFromJsonAsync<VaccinationResultResponse>(ResultPath(id)))!.Vaccination);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task BindingQueryValidationMissingIndicationAndNoStoreAreEnforced()
    {
        await Run(async f => {
            var source=await f.Source();var id=source.Assessment.Indications.Single().Id;var before=await f.Snapshot();
            foreach(var field in new[]{"vaccineId","recipientId","appointmentId","registrationId","doctorId","employeeId","administeredAt","isEligible","isPaid","stockRemaining","status"}) {
                var body=$"{{\"indicationId\":{id},\"lotNumber\":\"[DEMO] S5-VALID-A\",\"injectionSite\":\"Synthetic\",\"{field}\":1}}";
                await Problem(await f.Nurse.PostAsync(Route,new StringContent(body,Encoding.UTF8,"application/json")),400,"validation");
            }
            foreach(var request in new[]{Request(0),Request(id) with{LotNumber=""},Request(id) with{InjectionSite=" "},Request(id) with{InjectionSite=new string('x',101)},Request(id) with{LotNumber=new string('x',81)}})
                await Problem(await f.Nurse.PostAsJsonAsync(Route,request),400,"validation");
            await Problem(await f.Nurse.PostAsJsonAsync(Route,Request(int.MaxValue)),404,"resourceNotFound");
            foreach(var query in new[]{"page=0","pageSize=101","page=2147483647&pageSize=100","search="+new string('a',101)})
                await Problem(await f.Nurse.GetAsync(Route+"/eligible?"+query),400,"validation");
            Assert.Single((await f.Nurse.GetFromJsonAsync<VaccinationListResponse>(Route+"/eligible?search=LH-"+source.Appointment.Id.ToString("0000")))!.Items);
            Assert.Empty((await f.Nurse.GetFromJsonAsync<VaccinationListResponse>(Route+"/eligible?search=missing"))!.Items);
            Assert.Empty((await f.Nurse.GetFromJsonAsync<VaccinationListResponse>(Route+"/eligible?page=2&pageSize=1"))!.Items);
            using var anonymous=f.Base.Base.Factory.CreateClient();await Problem(await anonymous.GetAsync(Route+"/eligible"),401,"invalidSession");
            Assert.Equal(before,await f.Snapshot());
        });
    }

    internal sealed record SourceData(RegistrationDto Registration, AppointmentDto Appointment, ClinicalAssessmentDto Assessment);
    internal sealed class Fixture : IAsyncDisposable
    {
        public required AppointmentSqlTests.Fixture Base {get;init;}
        public TiemChungDbContext Db=>Base.Db;
        public HttpClient Nurse=null!, OtherNurse=null!, Doctor=null!, Cashier=null!;
        public int A,B,PackageId,NurseAccountId,NurseEmployeeId;
        public static async Task<Fixture> Create()
        {
            var b=await AppointmentSqlTests.Fixture.Create();var f=new Fixture{Base=b};
            try {
                var environment=new TestEnvironment();
                await DevelopmentCatalogSeed.RunAsync(f.Db,environment);await DevelopmentInventorySeed.RunAsync(f.Db,environment);
                f.A=(await f.Db.Vaccines.SingleAsync(v=>v.TenVacXin=="[DEMO] Vắc xin A")).MaVacXin;
                f.B=(await f.Db.Vaccines.SingleAsync(v=>v.TenVacXin=="[DEMO] Vắc xin B")).MaVacXin;
                f.PackageId=(await f.Db.Set<GoiVacXin>().SingleAsync(p=>p.TenGoi=="[DEMO] Gói A + B")).MaGoi;
                var today=VaccinationEndpoints.FacilityDate(DateTimeOffset.UtcNow);
                f.Db.Add(new LoVacXin{SoLo="[DEMO] S55-VALID-B",MaVacXin=f.B,NgaySanXuat=today.AddMonths(-6),HanSuDung=today.AddYears(1),SoLuongBanDau=10,SoLuongTon=10,TrangThai="Active"});await f.Db.SaveChangesAsync();
                f.Nurse=await b.Actor("DieuDuong",false);f.NurseAccountId=b.LastAccountId;
                f.NurseEmployeeId=(await f.Db.Set<TaiKhoan>().SingleAsync(a=>a.MaTaiKhoan==f.NurseAccountId)).MaNhanVien!.Value;
                f.OtherNurse=await b.Actor("DieuDuong",false);f.Doctor=await b.Actor("BacSi",false);f.Cashier=await b.Actor("ThuNgan",false);
                foreach(var code in new[]{"XemThanhToan","GhiThanhToan"})f.Db.Add(new TaiKhoan_QuyenHan{MaTaiKhoan=b.LastAccountId,MaQuyen=(await f.Db.Set<QuyenHan>().SingleAsync(p=>p.Code==code)).MaQuyen});
                await f.Db.SaveChangesAsync();f.Db.ChangeTracker.Clear();return f;
            }catch(Exception e){await SqlFixtureLifecycle.CleanupAfterFailureAsync(e,f.DisposeAsync);throw;}
        }
        public async Task<RegistrationDto> Register()
        {
            using var response=await Base.Reception.PostAsJsonAsync("/api/v1/registrations",new CreateRegistrationRequest(Base.CustomerId,Base.RecipientId,
                [new("vaccine",A,2,150000),new("package",PackageId,1,350000,[new(A,1),new(B,1)])]));
            Assert.Equal(HttpStatusCode.Created,response.StatusCode);return (await response.Content.ReadFromJsonAsync<RegistrationDto>())!;
        }
        public async Task<SourceData> Source(string kind="vaccine",int[]? vaccineIds=null)
        {
            var registration=await Register();
            using var appointed=await Base.Reception.PostAsJsonAsync("/api/v1/appointments",Base.Request(registration,registration.Items.Single(i=>i.Kind==kind)));
            Assert.Equal(HttpStatusCode.Created,appointed.StatusCode);var appointment=(await appointed.Content.ReadFromJsonAsync<AppointmentDto>())!;
            using var assessed=await Doctor.PostAsJsonAsync($"/api/v1/clinical/appointments/{appointment.Id}/assessment",new CreateClinicalAssessmentRequest("Đủ điều kiện",null,vaccineIds??[A]));
            Assert.Equal(HttpStatusCode.Created,assessed.StatusCode);var assessment=(await assessed.Content.ReadFromJsonAsync<ClinicalAssessmentDto>())!;
            using var paid=await Cashier.PostAsJsonAsync($"/api/v1/registrations/{registration.Id}/payment",new{});Assert.Equal(HttpStatusCode.Created,paid.StatusCode);
            return new(registration,appointment,assessment);
        }
        public async Task<string> Snapshot()
        {
            var rows=new List<string>();await Db.Database.OpenConnectionAsync();
            try {foreach(var table in new[]{"MuiTiem","LoVacXin","BienDongKho","SecurityAudit","LichHenTiem","PhieuKhamSangLoc","ChiDinhTiem","DangKyDichVu","ThanhToan","TheoDoiSauTiem"}) {
                await using var command=Db.Database.GetDbConnection().CreateCommand();command.CommandText=$"SELECT (SELECT * FROM [{table}] FOR JSON PATH, INCLUDE_NULL_VALUES)";
                rows.Add(Convert.ToString(await command.ExecuteScalarAsync())!);
            }}finally{await Db.Database.CloseConnectionAsync();}
            return string.Join("\n",rows);
        }
        public ValueTask DisposeAsync()=>Base.DisposeAsync();
        private sealed class TestEnvironment : IHostEnvironment
        {
            public string EnvironmentName{get;set;}="Development";public string ApplicationName{get;set;}="VaccinationTests";
            public string ContentRootPath{get;set;}=AppContext.BaseDirectory;
            public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider{get;set;}=new Microsoft.Extensions.FileProviders.NullFileProvider();
        }
    }
}
