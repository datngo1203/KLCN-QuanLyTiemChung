using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TiemChung.Api;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class AppointmentSqlTests
{
    private const string Route = "/api/v1/appointments";

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task SinglesPackagesUtcReadbackFiltersAndAuditPersistWithoutPaymentOrDoseExpansion()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var registration = await f.Register();
            foreach (var line in registration.Items) {
                var request = f.Request(registration, line);
                using var response = await f.Reception.PostAsJsonAsync(Route, request);
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                var saved = (await response.Content.ReadFromJsonAsync<AppointmentDto>())!;
                Assert.Equal(Route + "/" + saved.Id, response.Headers.Location!.OriginalString);
                Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
                Assert.Equal("DaXepLich", saved.Status); Assert.Equal(TimeSpan.Zero, saved.ScheduledAt.Offset);
                Assert.Equal(DateTimeOffset.Parse(request.ScheduledAt), saved.ScheduledAt);
                Assert.Equal(line.Id, saved.RegistrationItemId); Assert.Equal(line.Kind, saved.ItemKind);
                Assert.Equal(registration.Recipient.Id, saved.Recipient.Id); Assert.Equal("[DEMO] S5 room", saved.Location);
                var row = await f.Db.Set<LichHenTiem>().AsNoTracking().SingleAsync(x => x.MaLichHen == saved.Id);
                Assert.Equal(saved.ScheduledAt.UtcDateTime, row.ThoiGianHen);
                Assert.Equal(registration.Id, row.MaDangKy);
                Assert.Equal(JsonSerializer.Serialize(saved), JsonSerializer.Serialize(await f.Reception.GetFromJsonAsync<AppointmentDto>(Route + "/" + saved.Id)));
                var query = $"?registrationId={registration.Id}&itemKind={line.Kind}&registrationItemId={line.Id}&scheduledAt={Uri.EscapeDataString(saved.ScheduledAt.ToString("O"))}";
                Assert.Equal(saved.Id, Assert.Single((await f.Reception.GetFromJsonAsync<AppointmentListResponse>(Route + query))!.Items).Id);
                var audit = await f.Db.Set<SecurityAudit>().SingleAsync(x => x.Action == "appointments.created" && x.ResourceId == saved.Id.ToString());
                Assert.Equal("Success", audit.Outcome); Assert.Equal("Appointment", audit.ResourceType); Assert.NotNull(audit.SessionId);
                Assert.Equal(f.AccountId, audit.ActorAccountId);
                await Problem(await f.Reception.PostAsJsonAsync(Route, request with { ScheduledAt = saved.ScheduledAt.ToString("O") }), 409, "appointmentAlreadyExists");
                using var later = await f.Reception.PostAsJsonAsync(Route, request with { ScheduledAt = saved.ScheduledAt.AddHours(1).ToString("O") });
                Assert.Equal(HttpStatusCode.Created, later.StatusCode);
            }
            Assert.Equal(4, await f.Db.Set<LichHenTiem>().CountAsync());
            var all = (await f.Reception.GetFromJsonAsync<AppointmentListResponse>(Route))!.Items;
            Assert.Equal(4, all.Length); Assert.Equal(all.Select(x => x.Id).OrderDescending(), all.Select(x => x.Id));
            Assert.Single((await f.Reception.GetFromJsonAsync<AppointmentListResponse>(Route + "?page=2&pageSize=1"))!.Items);
            Assert.Empty((await f.Reception.GetFromJsonAsync<AppointmentListResponse>(Route + "?registrationId=2147483647"))!.Items);
            Assert.Empty(await f.Db.Payments.ToArrayAsync()); Assert.Empty(await f.Db.Set<MuiTiem>().ToArrayAsync());
            Assert.Empty(await f.Db.Set<NhacLich>().ToArrayAsync());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task ValidationAndWrongSourcesLeaveNoPartialData()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var first = await f.Register(); var other = await f.Register(); var valid = f.Request(first, first.Items[0]);
            foreach (var request in new[] {
                valid with { RegistrationId=0 }, valid with { RegistrationItemId=0 }, valid with { ItemKind="other" },
                valid with { Location=" " }, valid with { Location=new string('x',201) }, valid with { Location=null! },
                valid with { ScheduledAt="2026-11-02T09:00:00" }, valid with { ScheduledAt="bad" }, valid with { ScheduledAt=null! },
                valid with { ScheduledAt=DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O") },
                valid with { RegistrationItemId=other.Items[0].Id }, valid with { RegistrationItemId=int.MaxValue }
            }) await Problem(await f.Reception.PostAsJsonAsync(Route, request), 400, "validation");
            await Problem(await f.Reception.PostAsJsonAsync(Route, valid with { RegistrationId=int.MaxValue }), 404, "resourceNotFound");
            foreach (var json in new[] { "{", "{}", "null", JsonSerializer.Serialize(valid, new JsonSerializerOptions(JsonSerializerDefaults.Web)).Replace("\"location\":", "\"recipientId\":1,\"location\":") })
                await Problem(await f.Reception.PostAsync(Route, new StringContent(json, Encoding.UTF8, "application/json")), 400, "validation");
            foreach (var query in new[] { "page=0", "pageSize=101", "registrationId=-1", "recipientId=0", "itemKind=vaccine", "registrationItemId=1", "itemKind=x&registrationItemId=1", "scheduledAt=bad", "page=2147483647&pageSize=100" })
                await Problem(await f.Reception.GetAsync(Route + "?" + query), 400, "validation");
            await Problem(await f.Reception.GetAsync(Route + "/0"), 400, "validation");
            await Problem(await f.Reception.GetAsync(Route + "/2147483647"), 404, "resourceNotFound");
            await f.Db.Registrations.Where(x => x.MaDangKy == first.Id).ExecuteUpdateAsync(x => x.SetProperty(r => r.TrangThai,"Legacy"));
            await Problem(await f.Reception.PostAsJsonAsync(Route, valid), 409, "appointmentSourceConflict");
            Assert.Empty(await f.Db.Set<LichHenTiem>().ToArrayAsync());
            Assert.Empty(await f.Db.Set<SecurityAudit>().Where(x => x.Action == "appointments.created").ToArrayAsync());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task AllRoutesRequireCurrentSessionReceptionTitleAndExistingPermission()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var registration = await f.Register(); var request = f.Request(registration, registration.Items[0]);
            async Task Denied(HttpClient client, int status) {
                await Problem(await client.GetAsync(Route), status);
                await Problem(await client.GetAsync(Route + "/1"), status);
                await Problem(await client.PostAsJsonAsync(Route,request), status);
            }
            using var anonymous = f.Base.Factory.CreateClient(); await Denied(anonymous,401);
            foreach (var title in new string?[] { "LeTan", "Admin", "ThuNgan", "BacSi", "DieuDuong", "ThuKho", null }) {
                using var actor = await f.Actor(title, title != "LeTan"); await Denied(actor,403);
            }
            await f.Db.Set<TaiKhoan_QuyenHan>().Where(x=>x.MaTaiKhoan==f.AccountId).ExecuteDeleteAsync();
            await Denied(f.Reception,403);
            await f.Db.Set<NhanVien>().Where(x=>x.MaNhanVien==f.EmployeeId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TrangThai,"Inactive"));
            await Denied(f.Reception,401);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task AuditFailureRollsBackAppointment()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var registration=await f.Register();
            await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER S5AuditFailure ON dbo.SecurityAudit AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE Action='appointments.created') THROW 51000, 'Synthetic S5 failure', 1;");
            await Problem(await f.Reception.PostAsJsonAsync(Route,f.Request(registration,registration.Items[0])),503,"dependencyUnavailable");
            Assert.Empty(await f.Db.Set<LichHenTiem>().ToArrayAsync());
            Assert.Empty(await f.Db.Set<SecurityAudit>().Where(x=>x.Action=="appointments.created").ToArrayAsync());
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("vaccine")]
    [InlineData("package")]
    public async Task ConcurrentRequestsWithDifferentSessionsCreateExactlyOneAppointmentAndAudit(string kind)
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var registration=await f.Register(); var request=f.Request(registration,registration.Items.Single(x=>x.Kind==kind));
            using var second=await f.Actor("LeTan",true);
            var responses=await Task.WhenAll(f.Reception.PostAsJsonAsync(Route,request),second.PostAsJsonAsync(Route,request));
            try {
                Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Created);
                Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Conflict);
            } finally { foreach(var r in responses) r.Dispose(); }
            Assert.Equal(1,await f.Db.Set<LichHenTiem>().CountAsync());
            Assert.Equal(1,await f.Db.Set<SecurityAudit>().CountAsync(x=>x.Action=="appointments.created"));
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task DatabaseConstraintsRejectWrongLineRecipientAndDuplicateEvenWithoutApi()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var one=await f.Register();var two=await f.Register();
            var recipient=new NguoiDuocTiem { MaKhachHang=f.CustomerId,HoTen="[DEMO] Other" };f.Db.Add(recipient);await f.Db.SaveChangesAsync();
            var time=DateTime.UtcNow.AddDays(2);
            LichHenTiem Row()=>new() { MaDangKy=one.Id,MaNguoiDuocTiem=one.Recipient.Id,MaChiTietVacXin=one.Items[0].Id,ThoiGianHen=time,DiemTiem="[DEMO]",TrangThai="DaXepLich" };
            foreach(var bad in new[] { "line","recipient","both","missing" }) {
                var row=Row();
                if(bad=="line")row.MaChiTietVacXin=two.Items[0].Id;
                if(bad=="recipient")row.MaNguoiDuocTiem=recipient.MaNguoiDuocTiem;
                if(bad=="both")row.MaChiTietGoi=one.Items[1].Id;
                if(bad=="missing")row.MaChiTietVacXin=null;
                f.Db.Add(row);await Assert.ThrowsAsync<DbUpdateException>(()=>f.Db.SaveChangesAsync());f.Db.ChangeTracker.Clear();
            }
            foreach(var kind in new[] {"vaccine","package"}) {
                var row=Row();if(kind=="package"){row.MaChiTietVacXin=null;row.MaChiTietGoi=one.Items[1].Id;}
                f.Db.Add(row);await f.Db.SaveChangesAsync();f.Db.Entry(row).State=EntityState.Detached;
                row.MaLichHen=0;f.Db.Add(row);var e=await Assert.ThrowsAsync<DbUpdateException>(()=>f.Db.SaveChangesAsync());
                Assert.Equal(2601,Assert.IsType<SqlException>(e.InnerException).Number);f.Db.ChangeTracker.Clear();
            }
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task MigrationPreservesLegacyRowsAndRefusesLossyDown()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var migrator=f.Db.GetService<IMigrator>();await migrator.MigrateAsync("20261029000847_G3SinglePayment");
            await f.Db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO LichHenTiem(MaNguoiDuocTiem,ThoiGianHen,DiemTiem,TrangThai) VALUES ({f.RecipientId},{DateTime.UtcNow},{"legacy room"},{"Planned"})");
            await migrator.MigrateAsync();await migrator.MigrateAsync();
            var legacy=await f.Db.Set<LichHenTiem>().AsNoTracking().SingleAsync();Assert.Null(legacy.MaDangKy);Assert.Equal("legacy room",legacy.DiemTiem);Assert.Equal("Planned",legacy.TrangThai);
            Assert.Empty((await f.Reception.GetFromJsonAsync<AppointmentListResponse>(Route))!.Items);
            await Problem(await f.Reception.GetAsync(Route+"/"+legacy.MaLichHen),404,"resourceNotFound");
            await migrator.MigrateAsync("20261029000847_G3SinglePayment");await migrator.MigrateAsync();
            var registration=await f.Register();using var created=await f.Reception.PostAsJsonAsync(Route,f.Request(registration,registration.Items[0]));Assert.Equal(HttpStatusCode.Created,created.StatusCode);
            await Assert.ThrowsAsync<SqlException>(()=>migrator.MigrateAsync("20261029000847_G3SinglePayment"));
            Assert.Equal(2,await f.Db.Set<LichHenTiem>().CountAsync());
            Assert.False(f.Db.Database.HasPendingModelChanges());
        });
    }

    private static async Task Problem(HttpResponseMessage response,int status,string? code=null)
    {
        using(response) {
            Assert.Equal((HttpStatusCode)status,response.StatusCode);Assert.Equal("no-store",response.Headers.CacheControl!.ToString());
            var text=await response.Content.ReadAsStringAsync();using var json=JsonDocument.Parse(text);
            if(code!=null)Assert.Equal(code,json.RootElement.GetProperty("code").GetString());
            Assert.True(json.RootElement.TryGetProperty("traceId",out _));Assert.DoesNotContain("SqlException",text);
        }
    }
    private sealed class Fixture : IAsyncDisposable
    {
        public required CatalogSqlTests.Fixture Base {get;init;}
        public TiemChungDbContext Db=>Base.Db;
        public HttpClient Reception=null!;public int AccountId,EmployeeId,CustomerId,RecipientId;
        private readonly List<HttpClient> _clients=[];
        public CreateAppointmentRequest Request(RegistrationDto r,RegistrationItemDto i)=>new(r.Id,i.Kind,i.Id,DateTimeOffset.UtcNow.AddDays(2).ToOffset(TimeSpan.FromHours(7)).ToString("O"),"  [DEMO] S5 room  ");
        public async Task<HttpClient> Actor(string? title,bool grant)
        {
            NhanVien? employee=null;
            if(title!=null){employee=new(){HoTen="[DEMO] S5 "+title,ChucDanh=title,TrangThai="Active"};Db.Add(employee);await Db.SaveChangesAsync();}
            var suffix=Guid.NewGuid().ToString("N");
            var account=new TaiKhoan {AccountType=title==null?"Customer":"Employee",MaNhanVien=employee?.MaNhanVien,TenDangNhap="s5-"+suffix,NormalizedUsername=title==null?null:"S5-"+suffix.ToUpperInvariant(),LoginEmail=title==null?suffix+"@example.invalid":null,NormalizedEmail=title==null?suffix.ToUpperInvariant()+"@EXAMPLE.INVALID":null,EmailVerifiedAt=DateTimeOffset.UtcNow,TrangThai="Active",CreatedAt=DateTimeOffset.UtcNow};
            account.MatKhauHash=new PasswordHasher<TaiKhoan>().HashPassword(account,Guid.NewGuid().ToString("N"));Db.Add(account);await Db.SaveChangesAsync();
            if(grant)Db.Add(new TaiKhoan_QuyenHan {MaTaiKhoan=account.MaTaiKhoan,MaQuyen=(await Db.Set<QuyenHan>().SingleAsync(x=>x.Code=="QuanLyLichHen")).MaQuyen});
            var session=new IdentitySession {Id=Guid.NewGuid(),AccountId=account.MaTaiKhoan,SecurityVersion=account.SecurityVersion,CreatedAt=DateTimeOffset.UtcNow,ExpiresAt=DateTimeOffset.UtcNow.AddHours(1)};Db.Add(session);await Db.SaveChangesAsync();
            var client=Base.Factory.CreateClient();_clients.Add(client);client.DefaultRequestHeaders.Authorization=new("Bearer",new IdentityTokens(Base.Config).Issue(account.MaTaiKhoan,session.Id,DateTimeOffset.UtcNow));
            if(Reception==null && title=="LeTan" && grant){AccountId=account.MaTaiKhoan;EmployeeId=employee!.MaNhanVien;}
            return client;
        }
        public async Task<RegistrationDto> Register()
        {
            var v=await Db.Vaccines.AsNoTracking().FirstAsync();var p=await Db.Set<GoiVacXin>().AsNoTracking().FirstAsync();
            using var response=await Reception.PostAsJsonAsync("/api/v1/registrations",new CreateRegistrationRequest(CustomerId,RecipientId,[new("vaccine",v.MaVacXin,2,v.Gia),new("package",p.MaGoi,2,p.GiaGoi,[new(v.MaVacXin,2)])]));
            Assert.Equal(HttpStatusCode.Created,response.StatusCode);return (await response.Content.ReadFromJsonAsync<RegistrationDto>())!;
        }
        public static async Task<Fixture> Create()
        {
            var b=await CatalogSqlTests.Fixture.Create();var f=new Fixture {Base=b};
            try {
                var type=new LoaiTiemChung {TenLoaiTiemChung="[DEMO] S5"};var vt=new LoaiVacXin {TenLoaiVacXin="[DEMO] S5"};b.Db.AddRange(type,vt);await b.Db.SaveChangesAsync();
                var v=new VacXin {TenVacXin="[DEMO] S5 vaccine",MaLoaiVacXin=vt.MaLoaiVacXin,MaLoaiTiemChung=type.MaLoaiTiemChung,TrangThai="Active",Gia=100};var p=new GoiVacXin {TenGoi="[DEMO] S5 package",GiaGoi=150,TrangThai="Active"};var c=new KhachHang {HoTen="[DEMO] S5 customer"};b.Db.AddRange(v,p,c);await b.Db.SaveChangesAsync();
                var recipient=new NguoiDuocTiem {MaKhachHang=c.MaKhachHang,HoTen="[DEMO] S5 recipient"};b.Db.AddRange(recipient,new GoiVacXin_VacXin {MaGoi=p.MaGoi,MaVacXin=v.MaVacXin,SoLuong=2});await b.Db.SaveChangesAsync();
                f.CustomerId=c.MaKhachHang;f.RecipientId=recipient.MaNguoiDuocTiem;f.Reception=await f.Actor("LeTan",true);return f;
            }catch(Exception e){await SqlFixtureLifecycle.CleanupAfterFailureAsync(e,f.DisposeAsync);throw;}
        }
        public ValueTask DisposeAsync()=>SqlFixtureLifecycle.CleanupAsync(()=>{foreach(var c in _clients)c.Dispose();return ValueTask.CompletedTask;},Base.DisposeAsync);
    }
}
