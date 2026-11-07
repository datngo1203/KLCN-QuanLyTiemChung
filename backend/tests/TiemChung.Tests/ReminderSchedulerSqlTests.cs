using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using TiemChung.Api;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class ReminderSchedulerSqlTests
{
    private static Task Run(Func<AppointmentSqlTests.Fixture, Task> test) => SqlFixtureLifecycle.RunAsync(AppointmentSqlTests.Fixture.Create, test);
    private static TiemChungDbContext NewDb(AppointmentSqlTests.Fixture f) => new(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(f.Db.Database.GetConnectionString()).Options);
    private static string Path(int id) => $"/api/v1/appointments/{id}/email-reminder";
    private static IConfiguration Config(int tolerance = 300) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
        ["Appointments:Hotline"] = "028 7102 6595", ["ReminderAutomation:ToleranceSeconds"] = tolerance.ToString()
    }).Build();
    private static async Task Schedule(AppointmentSqlTests.Fixture f, DateTimeOffset now, int tolerance = 300, Dictionary<string, int>? cursors = null)
    {
        await using var db = NewDb(f);
        await new ReminderScheduler(db, Config(tolerance)).EnqueueAsync(now, default, cursors);
    }
    private static async Task<int> Owner(AppointmentSqlTests.Fixture f)
    {
        using var client = await f.Actor(null, false);
        var id = f.LastAccountId;
        f.Db.AddRange(new AccountProfileLink { AccountId = id, CustomerId = f.CustomerId },
            new AccountProfileLink { AccountId = id, RecipientId = f.RecipientId });
        await f.Db.SaveChangesAsync(); return id;
    }
    private static async Task<AppointmentDto> Appointment(AppointmentSqlTests.Fixture f, DateTimeOffset time)
    {
        var registration = await f.Register();
        using var response = await f.Reception.PostAsJsonAsync("/api/v1/appointments", f.Request(registration, registration.Items[0]));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var a = (await response.Content.ReadFromJsonAsync<AppointmentDto>())!;
        // Synthetic time fixtures only; no clinical gate is bypassed or exercised here.
        await f.Db.Set<LichHenTiem>().Where(x => x.MaLichHen == a.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.ThoiGianHen, time.UtcDateTime));
        return (await f.Reception.GetFromJsonAsync<AppointmentDto>($"/api/v1/appointments/{a.Id}"))!;
    }
    private static async Task Drain(AppointmentSqlTests.Fixture f, Sink sink)
    {
        await using var db = NewDb(f); await new ReminderEmailDelivery(db, sink).DrainAsync(default);
    }

    [Theory]
    [InlineData("TMinus48", -48, 60)][InlineData("TMinus24", -24, 300)]
    [InlineData("DPlus3", 72, 300)][InlineData("DPlus7", 168, 600)]
    public async Task ExactWindowBoundariesAndFacilityMidnight(string kind, int offsetHours, int tolerance)
    {
        await Run(async f => {
            await Owner(f);
            var now = new DateTimeOffset(2026, 11, 7, 17, 0, 0, TimeSpan.Zero); // 00:00 on 08/11 at the facility.
            var upper = now.AddHours(-offsetHours);
            var times = new[] { upper.AddTicks(1), upper, upper.AddSeconds(-tolerance).AddTicks(1), upper.AddSeconds(-tolerance), upper.AddSeconds(-tolerance).AddTicks(-1) };
            var appointments = new List<AppointmentDto>();
            foreach (var time in times) appointments.Add(await Appointment(f, time));
            await Schedule(f, now, tolerance);
            var rows = await f.Db.Set<ReminderEmail>().AsNoTracking().OrderBy(x => x.ScheduledAt).ToArrayAsync();
            Assert.Equal(2, rows.Length); Assert.All(rows, r => Assert.Equal(kind, r.Kind));
            Assert.Equal(new[] { appointments[1].Id, appointments[2].Id }.Order(), rows.Select(r => r.AppointmentId).Order());
            Assert.All(rows, r => { Assert.Null(r.CreatedByAccountId); Assert.Null(r.CreatedByEmployeeId); Assert.Equal(now, r.CreatedAt); });
            Assert.Contains(upper.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture), rows.Single(r => r.AppointmentId == appointments[1].Id).Body);
            Assert.All(rows, r => {
                foreach (var text in new[] { "Người được tiêm:", "Dịch vụ:", "Địa điểm:", "Asia/Ho_Chi_Minh", "liên hệ trung tâm" }) Assert.Contains(text, r.Body);
                Assert.DoesNotContain("[DEMO]", r.Body);
                if (kind.StartsWith("DPlus")) Assert.Contains("lịch hẹn đã qua nhưng chưa có thông tin mũi tiêm hoàn tất trong hệ thống", r.Body);
                Assert.DoesNotContain("bỏ tiêm", r.Body);
            });
            await Schedule(f, now, tolerance); Assert.Equal(2, await f.Db.Set<ReminderEmail>().CountAsync());
            Assert.All(await f.Db.Set<SecurityAudit>().Where(x => x.TraceId == "reminder-scheduler").ToArrayAsync(), a => Assert.Null(a.ActorAccountId));
        });
    }

    [Fact]
    public async Task RestartTwoInstancesAndDatabaseUniqueRaceKeepOneEventPerKind()
    {
        await Run(async f => {
            await Owner(f); var now = DateTimeOffset.UtcNow;
            var a = await Appointment(f, now.AddHours(48));
            await Task.WhenAll(Schedule(f, now), Schedule(f, now));
            await Schedule(f, now.AddSeconds(30));
            var first = await f.Db.Set<ReminderEmail>().AsNoTracking().SingleAsync();
            Assert.Equal("TMinus48", first.Kind);
            // Bypass the scheduler's lock only to prove the DB business key itself closes a duplicate insert race.
            ReminderEmail Copy() { var r = JsonSerializer.Deserialize<ReminderEmail>(JsonSerializer.Serialize(first))!; r.Id = 0; r.Kind = "TMinus24"; return r; }
            await using var one = NewDb(f); await using var two = NewDb(f);
            one.Add(Copy()); two.Add(Copy());
            async Task<bool> Save(TiemChungDbContext db) {
                try { await db.SaveChangesAsync(); return true; }
                catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 }) { return false; }
            }
            var results = await Task.WhenAll(Save(one), Save(two)); Assert.Single(results, x => x);
            Assert.Equal(2, await f.Db.Set<ReminderEmail>().CountAsync());
            Assert.Equal(1, await f.Db.Set<SecurityAudit>().CountAsync(x => x.TraceId == "reminder-scheduler"));
            var detail = (await f.Reception.GetFromJsonAsync<ReminderEmailDetailDto>(Path(a.Id)))!;
            Assert.Null(detail.Reminder); Assert.True(detail.CanRecord); Assert.Equal(2, detail.History!.Length);
            Assert.All(detail.History, r => Assert.Null(r.RecordedBy));
            using var manual = await f.Reception.PostAsJsonAsync(Path(a.Id), new { }); Assert.Equal(HttpStatusCode.Created, manual.StatusCode);
            Assert.Equal("Manual", (await manual.Content.ReadFromJsonAsync<ReminderEmailDto>())!.Kind);
        });
    }

    [Fact]
    public async Task RescheduleBetweenTriggersInvalidatesOldPendingAndAllowsNewSnapshot()
    {
        await Run(async f => {
            await Owner(f); var now = DateTimeOffset.UtcNow; var a = await Appointment(f, now.AddHours(48));
            await Schedule(f, now);
            using var changed = await f.Reception.PostAsJsonAsync($"/api/v1/appointments/{a.Id}/reschedule",
                new RescheduleAppointmentRequest(a.ScheduledAt.AddDays(1).ToString("O"), a.Version!));
            Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
            await Schedule(f, now.AddHours(24)); await Schedule(f, now.AddHours(48));
            var rows = await f.Db.Set<ReminderEmail>().AsNoTracking().ToArrayAsync(); Assert.Equal(3, rows.Length);
            var old = Assert.Single(rows, r => r.ScheduledAt == a.ScheduledAt.UtcDateTime);
            Assert.Equal("GuiLoi", old.Status); Assert.Equal("reminderSourceConflict", old.LastErrorCode);
            Assert.DoesNotContain(rows, r => r.ScheduledAt == old.ScheduledAt && r.Kind == "TMinus24");
            Assert.Equal(new[] { "TMinus24", "TMinus48" }, rows.Where(r => r.Id != old.Id).Select(r => r.Kind).Order());
            var sink = new Sink(); await Drain(f, sink); Assert.DoesNotContain(sink.Messages, m => m.DeliveryId == old.Id);
        });
    }

    [Theory][InlineData("cancel")][InlineData("stale")][InlineData("email")][InlineData("unverified")][InlineData("revoked")][InlineData("parent")]
    public async Task DispatcherRevalidatesAutomatedSourceAndVerifiedOwnership(string scenario)
    {
        await Run(async f => {
            var owner = await Owner(f); var now = DateTimeOffset.UtcNow; var a = await Appointment(f, now.AddHours(24));
            await Schedule(f, now);
            if (scenario == "cancel") {
                using var result = await f.Reception.PostAsJsonAsync($"/api/v1/appointments/{a.Id}/cancel", new CancelAppointmentRequest(a.Version!));
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            }
            if (scenario == "stale") await f.Db.Set<LichHenTiem>().ExecuteUpdateAsync(s => s.SetProperty(x => x.ThoiGianHen, a.ScheduledAt.UtcDateTime.AddHours(1)));
            if (scenario == "email") await f.Db.Set<TaiKhoan>().Where(x => x.MaTaiKhoan == owner).ExecuteUpdateAsync(s => s.SetProperty(x => x.LoginEmail, "changed@example.invalid"));
            if (scenario == "unverified") await f.Db.Set<TaiKhoan>().Where(x => x.MaTaiKhoan == owner).ExecuteUpdateAsync(s => s.SetProperty(x => x.EmailVerifiedAt, (DateTimeOffset?)null));
            if (scenario == "revoked") await f.Db.Set<AccountProfileLink>().ExecuteUpdateAsync(s => s.SetProperty(x => x.State, "Revoked"));
            if (scenario == "parent") {
                var other = new KhachHang { HoTen = "[DEMO] Other customer" }; f.Db.Add(other); await f.Db.SaveChangesAsync();
                await f.Db.Recipients.ExecuteUpdateAsync(s => s.SetProperty(x => x.MaKhachHang, other.MaKhachHang));
            }
            var sink = new Sink(); await Drain(f, sink); Assert.Empty(sink.Messages);
            var row = await f.Db.Set<ReminderEmail>().AsNoTracking().SingleAsync(); Assert.Equal("GuiLoi", row.Status); Assert.Equal(0, row.Attempts);
        });
    }

    [Theory][InlineData("DPlus3", 3)][InlineData("DPlus7", 7)]
    public async Task RecallDispatchesPastAppointmentAndRetryKeepsEvent(string kind, int days)
    {
        await Run(async f => {
            await Owner(f); var now = DateTimeOffset.UtcNow; var a = await Appointment(f, now.AddDays(-days));
            await Schedule(f, now); var sink = new Sink { Error = "providerUnavailable" };
            await Drain(f, sink); await Schedule(f, now.AddSeconds(30));
            var row = await f.Db.Set<ReminderEmail>().AsNoTracking().SingleAsync(); Assert.Equal(kind, row.Kind); Assert.Equal("GuiLoi", row.Status);
            await f.Db.Set<ReminderEmail>().ExecuteUpdateAsync(s => s.SetProperty(x => x.NextAttemptAt, DateTimeOffset.UtcNow.AddSeconds(-1)));
            var detail = (await f.Reception.GetFromJsonAsync<ReminderEmailDetailDto>(Path(a.Id)))!;
            Assert.True(Assert.Single(detail.History!).CanRetry);
            using var retry = await f.Reception.PostAsJsonAsync(Path(a.Id) + "/retry", new RetryReminderEmailRequest(1, row.Id));
            Assert.Equal(HttpStatusCode.Created, retry.StatusCode);
            sink.Error = null;
            await using var one = NewDb(f); await using var two = NewDb(f);
            await Task.WhenAll(new ReminderEmailDelivery(one, sink).DrainAsync(default), new ReminderEmailDelivery(two, sink).DrainAsync(default));
            await Drain(f, sink); Assert.Single(sink.Messages);
            var sent = await f.Db.Set<ReminderEmail>().AsNoTracking().SingleAsync(); Assert.Equal(row.Id, sent.Id); Assert.Equal(2, sent.Attempts); Assert.Equal("DaGui", sent.Status);
            Assert.Contains((await f.Reception.GetFromJsonAsync<AppointmentListResponse>("/api/v1/reminders/email-queue"))!.Items, x => x.Id == a.Id);
        });
    }

    [Fact]
    public async Task UncertainAutomatedDeliveryNeverResendsOrCreatesNewEvent()
    {
        await Run(async f => {
            await Owner(f); var now = DateTimeOffset.UtcNow; var a = await Appointment(f, now.AddHours(24));
            await Schedule(f, now); var sink = new Sink { Error = "deliveryUncertain" };
            await Drain(f, sink); await Drain(f, sink); await Schedule(f, now.AddSeconds(30));
            var row = await f.Db.Set<ReminderEmail>().AsNoTracking().SingleAsync(); Assert.Equal("deliveryUncertain", row.LastErrorCode); Assert.Equal(1, row.Attempts);
            using var retry = await f.Reception.PostAsJsonAsync(Path(a.Id) + "/retry", new RetryReminderEmailRequest(1, row.Id));
            Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode); Assert.Empty(sink.Messages);
        });
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task CompletedShotPreventsNextTriggerAndPendingDispatch(bool lateAfterRecall)
    {
        await SqlFixtureLifecycle.RunAsync(VaccinationSqlTests.Fixture.Create, async f => {
            var source = await f.Source(); await Owner(f.Base);
            var now = DateTimeOffset.UtcNow;
            var scheduled = lateAfterRecall ? now.AddDays(-3) : now.AddHours(48);
            await f.Db.Set<LichHenTiem>().ExecuteUpdateAsync(s => s.SetProperty(x => x.ThoiGianHen, scheduled.UtcDateTime));
            await Schedule(f.Base, now);
            if (lateAfterRecall) await Drain(f.Base, new Sink());
            // Use the real vaccination mutation and its clinical/payment/stock fixtures.
            // At T−48 the appointment is moved to its due instant before recording, to respect the clinical time gate.
            if (!lateAfterRecall) await f.Db.Set<LichHenTiem>().ExecuteUpdateAsync(s => s.SetProperty(x => x.ThoiGianHen, now.AddSeconds(-1).UtcDateTime));
            using var shot = await f.Nurse.PostAsJsonAsync("/api/v1/vaccinations", new RecordVaccinationRequest(source.Assessment.Indications.Single().Id, "[DEMO] S5-VALID-A", "Cánh tay"));
            Assert.Equal(HttpStatusCode.Created, shot.StatusCode);
            // Restore the synthetic snapshot solely to isolate DaTiem from stale-snapshot rejection.
            if (!lateAfterRecall) await f.Db.Set<LichHenTiem>().ExecuteUpdateAsync(s => s.SetProperty(x => x.ThoiGianHen, scheduled.UtcDateTime));
            await Schedule(f.Base, now.AddDays(lateAfterRecall ? 4 : 1));
            var sink = new Sink(); await Drain(f.Base, sink); Assert.Empty(sink.Messages);
            Assert.Single(await f.Db.Set<ReminderEmail>().ToArrayAsync());
        });
    }

    [Fact]
    public async Task MigrationBackfillsManualWithoutRewritingHistoricalDelivery()
    {
        await Run(async f => {
            await Owner(f); var now = DateTimeOffset.UtcNow; var a = await Appointment(f, now.AddHours(48));
            using var manual = await f.Reception.PostAsJsonAsync(Path(a.Id), new { }); Assert.Equal(HttpStatusCode.Created, manual.StatusCode);
            await Drain(f, new Sink());
            var before = JsonSerializer.Serialize(await f.Db.Set<ReminderEmail>().AsNoTracking().SingleAsync());
            var migrator = f.Db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261106221104_RF05F1PreferredTimeProvenance");
            Assert.Equal(0, await f.Db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sys.columns WHERE object_id=OBJECT_ID('ReminderEmail') AND name='Kind'").SingleAsync());
            await migrator.MigrateAsync(); await migrator.MigrateAsync();
            Assert.Equal(before, JsonSerializer.Serialize(await f.Db.Set<ReminderEmail>().AsNoTracking().SingleAsync()));
            Assert.False(f.Db.Database.HasPendingModelChanges());
            await Schedule(f, now); Assert.Equal(2, await f.Db.Set<ReminderEmail>().CountAsync());
            await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync("20261106221104_RF05F1PreferredTimeProvenance"));
        });
    }

    [Fact]
    public async Task BoundedBatchContinuesAfterRestartAndDoesNotStarveUnsentEvents()
    {
        await Run(async f => {
            await Owner(f); var now = DateTimeOffset.UtcNow;
            var a = await Appointment(f, now.AddHours(24));
            var source = await f.Db.Set<LichHenTiem>().AsNoTracking().SingleAsync();
            for (var i = 1; i <= ReminderScheduler.BatchSize; i++) f.Db.Add(new LichHenTiem {
                MaDangKy = source.MaDangKy, MaNguoiDuocTiem = source.MaNguoiDuocTiem, MaChiTietVacXin = source.MaChiTietVacXin,
                MaChiTietGoi = source.MaChiTietGoi, DiemTiem = source.DiemTiem, TrangThai = source.TrangThai,
                ThoiGianHen = source.ThoiGianHen.AddMilliseconds(-i)
            });
            await f.Db.SaveChangesAsync();
            await Schedule(f, now); Assert.Equal(ReminderScheduler.BatchSize, await f.Db.Set<ReminderEmail>().CountAsync());
            await Schedule(f, now.AddSeconds(30)); Assert.Equal(ReminderScheduler.BatchSize + 1, await f.Db.Set<ReminderEmail>().CountAsync());
        });
    }

    [Fact]
    public async Task MissingOrRevokedRecipientDoesNotEnqueueAndAuditFailureRollsBack()
    {
        await Run(async f => {
            var now = DateTimeOffset.UtcNow; await Appointment(f, now.AddHours(24));
            await Schedule(f, now); Assert.Empty(await f.Db.Set<ReminderEmail>().ToArrayAsync());
            var owner = await Owner(f);
            await f.Db.Set<TaiKhoan>().Where(x => x.MaTaiKhoan == owner).ExecuteUpdateAsync(s => s.SetProperty(x => x.EmailVerifiedAt, (DateTimeOffset?)null));
            await Schedule(f, now); Assert.Empty(await f.Db.Set<ReminderEmail>().ToArrayAsync());
            await f.Db.Set<TaiKhoan>().Where(x => x.MaTaiKhoan == owner).ExecuteUpdateAsync(s => s.SetProperty(x => x.EmailVerifiedAt, now));
            await f.Db.Set<AccountProfileLink>().ExecuteUpdateAsync(s => s.SetProperty(x => x.State, "Revoked"));
            await Schedule(f, now); Assert.Empty(await f.Db.Set<ReminderEmail>().ToArrayAsync());
            await f.Db.Set<AccountProfileLink>().ExecuteUpdateAsync(s => s.SetProperty(x => x.State, "Active"));
            await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER SchedulerAuditFailure ON SecurityAudit AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE TraceId='reminder-scheduler') THROW 51000, 'Synthetic failure', 1;");
            await Assert.ThrowsAsync<DbUpdateException>(() => Schedule(f, now)); Assert.Empty(await f.Db.Set<ReminderEmail>().ToArrayAsync());
            await f.Db.Database.ExecuteSqlRawAsync("DROP TRIGGER SchedulerAuditFailure");
            await Schedule(f, now); Assert.Single(await f.Db.Set<ReminderEmail>().ToArrayAsync());
        });
    }

    [Fact]
    public async Task IneligibleRecipientsCannotStarveNextBoundedPage()
    {
        await Run(async f => {
            var now = DateTimeOffset.UtcNow; await Appointment(f, now.AddHours(24));
            var source = await f.Db.Set<LichHenTiem>().AsNoTracking().SingleAsync();
            for (var i = 1; i < ReminderScheduler.BatchSize; i++) f.Db.Add(new LichHenTiem {
                MaDangKy = source.MaDangKy, MaNguoiDuocTiem = source.MaNguoiDuocTiem, MaChiTietVacXin = source.MaChiTietVacXin,
                DiemTiem = source.DiemTiem, TrangThai = source.TrangThai, ThoiGianHen = source.ThoiGianHen.AddMilliseconds(-i)
            });
            await f.Db.SaveChangesAsync();
            var other = new KhachHang { HoTen = "[DEMO] Unlinked customer" }; f.Db.Add(other); await f.Db.SaveChangesAsync();
            var recipient = new NguoiDuocTiem { MaKhachHang = other.MaKhachHang, HoTen = "[DEMO] Eligible recipient" }; f.Db.Add(recipient); await f.Db.SaveChangesAsync();
            f.CustomerId = other.MaKhachHang; f.RecipientId = recipient.MaNguoiDuocTiem;
            await Owner(f); var eligible = await Appointment(f, now.AddHours(24));
            var cursors = new Dictionary<string, int>();
            await Schedule(f, now, cursors: cursors); Assert.Empty(await f.Db.Set<ReminderEmail>().ToArrayAsync());
            await Schedule(f, now.AddSeconds(30), cursors: cursors);
            Assert.Equal(eligible.Id, (await f.Db.Set<ReminderEmail>().SingleAsync()).AppointmentId);
        });
    }

    [Theory][InlineData("DaGui")][InlineData("providerUnavailable")][InlineData("providerRejected")][InlineData("deliveryUncertain")][InlineData("interruptedClaim")]
    public async Task CancelPreservesAutomatedProviderEvidence(string state)
    {
        await Run(async f => {
            await Owner(f); var now = DateTimeOffset.UtcNow; var a = await Appointment(f, now.AddHours(24));
            await Schedule(f, now);
            if (state == "interruptedClaim") await f.Db.Set<ReminderEmail>().ExecuteUpdateAsync(s => s.SetProperty(x => x.DispatchStarted, true).SetProperty(x => x.Attempts, 1).SetProperty(x => x.LastAttemptAt, now));
            else await Drain(f, new Sink { Error = state == "DaGui" ? null : state });
            var before = await f.Db.Set<ReminderEmail>().AsNoTracking().SingleAsync();
            using var cancel = await f.Reception.PostAsJsonAsync($"/api/v1/appointments/{a.Id}/cancel", new CancelAppointmentRequest(a.Version!));
            Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
            if (state == "interruptedClaim") { before.Status = "GuiLoi"; before.LastErrorCode = "deliveryUncertain"; }
            var after = await f.Db.Set<ReminderEmail>().AsNoTracking().SingleAsync(); Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(after));
            var sink = new Sink(); await Drain(f, sink); await Schedule(f, now.AddDays(4)); Assert.Empty(sink.Messages);
            Assert.Single(await f.Db.Set<ReminderEmail>().ToArrayAsync());
        });
    }

    [Theory][InlineData(0)][InlineData(59)][InlineData(601)]
    public async Task InvalidToleranceFailsBeforeQuery(int tolerance)
    {
        await using var db = new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>()
            .UseSqlServer("Server=localhost;Database=Unused;Integrated Security=true").Options);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ReminderScheduler(db, Config(tolerance)).EnqueueAsync(DateTimeOffset.UtcNow, default));
    }

    private sealed class Sink : IReminderEmailTransport
    {
        public string? Error;
        public ConcurrentBag<ReminderEmailMessage> Messages { get; } = [];
        public Task DeliverAsync(ReminderEmailMessage message, CancellationToken ct) {
            if (Error != null) throw new ReminderDeliveryFailure(Error);
            Messages.Add(message); return Task.CompletedTask;
        }
    }
}
