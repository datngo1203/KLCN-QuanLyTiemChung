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
            foreach (var title in new string?[] { "LeTan", "Admin", "ThuNgan", "BacSi", "DieuDuong", "ThuKho" }) {
                using var actor = await f.Actor(title, title != "LeTan"); await Denied(actor,403);
            }
            using var customer = await f.Actor(null, false);
            Assert.Empty((await customer.GetFromJsonAsync<AppointmentListResponse>(Route))!.Items);
            await Problem(await customer.GetAsync(Route + "/1"),403,"permissionDenied");
            await Problem(await customer.PostAsJsonAsync(Route,request),403,"permissionDenied");
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

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task CustomerOwnershipFiltersBeforePagingAndDetailWithoutWritingSql()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var own = await f.Register();
            var appointments = new List<AppointmentDto>();
            foreach (var line in own.Items) {
                using var response = await f.Reception.PostAsJsonAsync(Route, f.Request(own, line));
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                appointments.Add((await response.Content.ReadFromJsonAsync<AppointmentDto>())!);
            }
            using var customer = await f.Actor(null, false);
            var ownerId = f.LastAccountId;
            f.Db.AddRange(new AccountProfileLink { AccountId=ownerId, CustomerId=f.CustomerId, State="Active" },
                new AccountProfileLink { AccountId=ownerId, RecipientId=f.RecipientId, State="Active" });
            var otherCustomer = new KhachHang { HoTen="[DEMO] S5.2 other customer" };
            f.Db.Add(otherCustomer); await f.Db.SaveChangesAsync();
            var otherRecipient = new NguoiDuocTiem { MaKhachHang=otherCustomer.MaKhachHang, HoTen="[DEMO] S5.2 other recipient" };
            f.Db.Add(otherRecipient); await f.Db.SaveChangesAsync();
            f.CustomerId=otherCustomer.MaKhachHang; f.RecipientId=otherRecipient.MaNguoiDuocTiem;
            var other = await f.Register();
            using var created = await f.Reception.PostAsJsonAsync(Route, f.Request(other, other.Items[0]));
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var hidden = (await created.Content.ReadFromJsonAsync<AppointmentDto>())!;
            using var customerB = await f.Actor(null, false);
            f.Db.AddRange(new AccountProfileLink { AccountId=f.LastAccountId, CustomerId=f.CustomerId, State="Active" },
                new AccountProfileLink { AccountId=f.LastAccountId, RecipientId=f.RecipientId, State="Active" },
                new LichHenTiem { MaNguoiDuocTiem=own.Recipient.Id, ThoiGianHen=DateTime.UtcNow, DiemTiem="[DEMO] legacy", TrangThai="Planned" });
            await f.Db.SaveChangesAsync();
            var legacyId = await f.Db.Set<LichHenTiem>().Where(x=>x.MaDangKy==null).Select(x=>x.MaLichHen).SingleAsync();
            var before = await Snapshot(f.Db);
            var all = (await customer.GetFromJsonAsync<AppointmentListResponse>(Route + "?pageSize=100"))!.Items;
            Assert.Equal(appointments.Select(x=>x.Id).OrderDescending(), all.Select(x=>x.Id));
            Assert.Equal(hidden.Id, Assert.Single((await customerB.GetFromJsonAsync<AppointmentListResponse>(Route))!.Items).Id);
            await Problem(await customerB.GetAsync(Route+"/"+all[0].Id),403,"permissionDenied");
            for (var page=1; page<=2; page++)
                Assert.Equal(all[page-1].Id, Assert.Single((await customer.GetFromJsonAsync<AppointmentListResponse>(Route+$"?page={page}&pageSize=1"))!.Items).Id);
            foreach (var query in new[] { "page=3&pageSize=1", $"registrationId={other.Id}", $"recipientId={other.Recipient.Id}",
                $"registrationId={own.Id}&recipientId={other.Recipient.Id}", "accountId="+f.LastAccountId+"&ownerId="+f.LastAccountId+"&page=3&pageSize=1" })
                Assert.Empty((await customer.GetFromJsonAsync<AppointmentListResponse>(Route+"?"+query))!.Items);
            foreach (var row in all) {
                Assert.Equal(JsonSerializer.Serialize(row), JsonSerializer.Serialize(await customer.GetFromJsonAsync<AppointmentDto>(Route+"/"+row.Id)));
                var query=$"?registrationId={own.Id}&recipientId={own.Recipient.Id}&itemKind={row.ItemKind}&registrationItemId={row.RegistrationItemId}&scheduledAt={Uri.EscapeDataString(row.ScheduledAt.ToString("O"))}";
                Assert.Equal(row.Id, Assert.Single((await customer.GetFromJsonAsync<AppointmentListResponse>(Route+query))!.Items).Id);
            }
            foreach (var id in new[] { hidden.Id, int.MaxValue, legacyId })
                await Problem(await customer.GetAsync(Route+"/"+id),403,"permissionDenied");
            await Problem(await f.Reception.GetAsync(Route+"/2147483647"),404,"resourceNotFound");
            Assert.Equal(before, await Snapshot(f.Db));
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task BothDirectActiveLinksAndCurrentRecipientCustomerPairAreRequired()
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            var registration=await f.Register();
            using var created=await f.Reception.PostAsJsonAsync(Route,f.Request(registration,registration.Items[0]));
            Assert.Equal(HttpStatusCode.Created,created.StatusCode);
            var appointment=(await created.Content.ReadFromJsonAsync<AppointmentDto>())!;
            using var customer=await f.Actor(null,false);
            var id=f.LastAccountId;
            async Task Check(bool allowed) {
                var rows=(await customer.GetFromJsonAsync<AppointmentListResponse>(Route))!.Items;
                if (allowed) { Assert.Equal(appointment.Id,Assert.Single(rows).Id); Assert.NotNull(await customer.GetFromJsonAsync<AppointmentDto>(Route+"/"+appointment.Id)); }
                else { Assert.Empty(rows); await Problem(await customer.GetAsync(Route+"/"+appointment.Id),403,"permissionDenied"); }
            }
            await Check(false);
            var c=new AccountProfileLink { AccountId=id,CustomerId=f.CustomerId,State="Active" };
            var p=new AccountProfileLink { AccountId=id,RecipientId=f.RecipientId,State="Active" };
            f.Db.Add(c); await f.Db.SaveChangesAsync(); await Check(false); // Customer only
            f.Db.Remove(c); f.Db.Add(p); await f.Db.SaveChangesAsync(); await Check(false); // Recipient only
            c=new AccountProfileLink { AccountId=id,CustomerId=f.CustomerId,State="Active" };
            f.Db.Add(c); await f.Db.SaveChangesAsync(); await Check(true);
            foreach (var link in new[] { c,p }) {
                link.State="Revoked"; await f.Db.SaveChangesAsync(); await Check(false);
                link.State="Active"; await f.Db.SaveChangesAsync(); await Check(true);
            }
            var other=new KhachHang { HoTen="[DEMO] Changed parent" }; f.Db.Add(other); await f.Db.SaveChangesAsync();
            await f.Db.Recipients.Where(x=>x.MaNguoiDuocTiem==f.RecipientId).ExecuteUpdateAsync(x=>x.SetProperty(p=>p.MaKhachHang,other.MaKhachHang));
            await Check(false);
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("inactive",401,"invalidSession")]
    [InlineData("unverified",403,"emailVerificationRequired")]
    [InlineData("revoked",401,"invalidSession")]
    [InlineData("expired",401,"invalidSession")]
    [InlineData("version",401,"invalidSession")]
    public async Task CustomerReadsRequireCurrentVerifiedActiveIdentity(string invalid,int status,string code)
    {
        await SqlFixtureLifecycle.RunAsync(() => Fixture.Create(), async f => {
            using var customer=await f.Actor(null,false); var id=f.LastAccountId;
            if (invalid=="inactive") await f.Db.Set<TaiKhoan>().Where(x=>x.MaTaiKhoan==id).ExecuteUpdateAsync(x=>x.SetProperty(a=>a.TrangThai,"Inactive"));
            if (invalid=="unverified") await f.Db.Set<TaiKhoan>().Where(x=>x.MaTaiKhoan==id).ExecuteUpdateAsync(x=>x.SetProperty(a=>a.EmailVerifiedAt,(DateTimeOffset?)null));
            if (invalid=="version") await f.Db.Set<TaiKhoan>().Where(x=>x.MaTaiKhoan==id).ExecuteUpdateAsync(x=>x.SetProperty(a=>a.SecurityVersion,a=>a.SecurityVersion+1));
            if (invalid=="revoked") await f.Db.Set<IdentitySession>().Where(x=>x.AccountId==id).ExecuteUpdateAsync(x=>x.SetProperty(a=>a.RevokedAt,DateTimeOffset.UtcNow));
            if (invalid=="expired") await f.Db.Set<IdentitySession>().Where(x=>x.AccountId==id).ExecuteUpdateAsync(x=>x.SetProperty(a=>a.CreatedAt,DateTimeOffset.UtcNow.AddHours(-2)).SetProperty(a=>a.ExpiresAt,DateTimeOffset.UtcNow.AddMinutes(-1)));
            await Problem(await customer.GetAsync(Route),status,code);
            await Problem(await customer.GetAsync(Route+"/2147483647"),status,code);
        });
    }

    private static async Task<string> Snapshot(TiemChungDbContext db)
    {
        // Compare all persisted user-table data, including sessions/audit, before and after GETs.
        var tables=await db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sys.tables WHERE is_ms_shipped=0 ORDER BY name").ToArrayAsync();
        var rows=new List<string>();
        foreach (var table in tables) {
            var name=new SqlCommandBuilder().QuoteIdentifier(table);
            await db.Database.OpenConnectionAsync();
            await using var command=db.Database.GetDbConnection().CreateCommand();
            command.CommandText=$"SELECT (SELECT * FROM {name} FOR JSON PATH, INCLUDE_NULL_VALUES)";
            rows.Add(Convert.ToString(await command.ExecuteScalarAsync())!);
            await db.Database.CloseConnectionAsync();
        }
        return string.Join("\n",rows);
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
        public HttpClient Reception=null!;public int AccountId,EmployeeId,CustomerId,RecipientId,LastAccountId;
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
            LastAccountId=account.MaTaiKhoan;
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
