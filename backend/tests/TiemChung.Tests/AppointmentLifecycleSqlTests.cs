using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TiemChung.Api;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;
using Fixture = TiemChung.Tests.AppointmentSqlTests.Fixture;

namespace TiemChung.Tests;

public sealed class AppointmentLifecycleSqlTests
{
    private static Task Run(Func<Fixture, Task> test) => SqlFixtureLifecycle.RunAsync(Fixture.Create, test);
    private static string Route(int id) => $"/api/v1/appointments/{id}";
    private static string Email(int id) => Route(id) + "/email-reminder";
    private static Task<HttpResponseMessage> Change(HttpClient client, AppointmentDto a, bool cancel = false) => cancel
        ? client.PostAsJsonAsync(Route(a.Id) + "/cancel", new CancelAppointmentRequest(a.Version!))
        : client.PostAsJsonAsync(Route(a.Id) + "/reschedule", new RescheduleAppointmentRequest(a.ScheduledAt.AddDays(1).ToString("O"), a.Version!));
    private static async Task<AppointmentDto> Appointment(Fixture f, RegistrationDto? registration = null, string kind = "vaccine")
    {
        registration ??= await f.Register();
        using var response = await f.Reception.PostAsJsonAsync("/api/v1/appointments", f.Request(registration, registration.Items.Single(x => x.Kind == kind)));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AppointmentDto>())!;
    }
    private static async Task<AppointmentDto> Changed(HttpResponseMessage response)
    {
        using (response) {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<AppointmentDto>())!;
        }
    }
    private static async Task Problem(HttpResponseMessage response, int status, string code)
    {
        using (response) {
            Assert.Equal((HttpStatusCode)status, response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
            Assert.Contains($"\"code\":\"{code}\"", await response.Content.ReadAsStringAsync());
        }
    }
    private static async Task<HttpClient> Owner(Fixture f)
    {
        var customer = await f.Actor(null, false);
        f.Db.AddRange(new AccountProfileLink { AccountId = f.LastAccountId, CustomerId = f.CustomerId },
            new AccountProfileLink { AccountId = f.LastAccountId, RecipientId = f.RecipientId });
        await f.Db.SaveChangesAsync(); return customer;
    }
    private static TiemChungDbContext NewDb(Fixture f) => new(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(f.Db.Database.GetConnectionString()).Options);

    [Theory][InlineData("vaccine")][InlineData("package")]
    public async Task RescheduleCancelPersistSameSourceAndCustomerHistoryNeverSelectsCancelled(string kind)
    {
        await Run(async f => {
            var registration = await f.Register(); var a = await Appointment(f, registration, kind); using var owner = await Owner(f);
            var changed = await Changed(await Change(f.Reception, a));
            Assert.Equal(a with { ScheduledAt = a.ScheduledAt.AddDays(1), Version = changed.Version }, changed);
            Assert.NotEqual(a.Version, changed.Version);
            Assert.Equal(changed, await owner.GetFromJsonAsync<AppointmentDto>(Route(a.Id)));
            var journey = (await owner.GetFromJsonAsync<VaccinationJourneyDto>($"/api/v1/vaccination-journeys/{registration.Id}"))!;
            Assert.Equal(changed.ScheduledAt, journey.NextAppointment!.ScheduledAt);
            var cancelled = await Changed(await Change(f.Reception, changed, true));
            Assert.Equal("DaHuy", cancelled.Status); Assert.False(cancelled.CanManage);
            Assert.Equal(cancelled, await owner.GetFromJsonAsync<AppointmentDto>(Route(a.Id)));
            journey = (await owner.GetFromJsonAsync<VaccinationJourneyDto>($"/api/v1/vaccination-journeys/{registration.Id}"))!;
            Assert.Null(journey.NextAppointment); Assert.Equal("DaHuy", Assert.Single(journey.Appointments).Status);
            Assert.All(journey.Progress, p => Assert.Null(p.NextAppointment));
            using var other = await f.Actor(null, false);
            await Problem(await other.GetAsync(Route(a.Id)), 403, "permissionDenied");
            using var doctor = await f.Actor("BacSi", false);
            Assert.DoesNotContain((await doctor.GetFromJsonAsync<AppointmentListResponse>("/api/v1/clinical/appointments"))!.Items, x => x.Id == a.Id);
            await Problem(await doctor.PostAsJsonAsync($"/api/v1/clinical/appointments/{a.Id}/assessment", new CreateClinicalAssessmentRequest("Đủ điều kiện", null, [a.ServiceId])), 409, "appointmentNotActive");
            Assert.Single(await f.Db.Set<LichHenTiem>().ToArrayAsync());
            Assert.Equal(1, await f.Db.Set<SecurityAudit>().CountAsync(x => x.Action == "appointments.rescheduled"));
            Assert.Equal(1, await f.Db.Set<SecurityAudit>().CountAsync(x => x.Action == "appointments.cancelled"));
            Assert.Empty(await f.Db.Set<ThanhToan>().ToArrayAsync());
            Assert.Equal("DaDangKy", (await f.Db.Registrations.AsNoTracking().SingleAsync()).TrangThai);
        });
    }

    [Fact] public async Task ValidationPermissionSessionInactiveMissingAndStaleFailClosed()
    {
        await Run(async f => {
            var a = await Appointment(f);
            foreach (var value in new[] { DateTimeOffset.UtcNow.ToString("O"), DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O"), "bad" })
                await Problem(await f.Reception.PostAsJsonAsync(Route(a.Id) + "/reschedule", new { scheduledAt = value, version = a.Version }), 400, "validation");
            await Problem(await f.Reception.PostAsJsonAsync(Route(a.Id) + "/cancel", new { version = "bad" }), 400, "validation");
            await Problem(await f.Reception.PostAsJsonAsync(Route(a.Id) + "/cancel", new { version = a.Version, reason = "extra" }), 400, "validation");
            foreach (var title in new[] { "Admin", "BacSi", "ThuNgan", "DieuDuong", "ThuKho", null }) {
                using var actor = await f.Actor(title, false);
                await Problem(await Change(actor, a), 403, "permissionDenied");
                await Problem(await Change(actor, a, true), 403, "permissionDenied");
            }
            using var anonymous = f.Base.Factory.CreateClient();
            await Problem(await Change(anonymous, a), 401, "invalidSession");
            await Problem(await Change(f.Reception, a with { Id = int.MaxValue }), 404, "resourceNotFound");
            var cancelled = await Changed(await Change(f.Reception, a, true));
            await Problem(await Change(f.Reception, a), 409, "concurrencyConflict");
            await Problem(await Change(f.Reception, cancelled), 409, "appointmentNotActive");
            await Problem(await Change(f.Reception, cancelled, true), 409, "appointmentNotActive");
            await f.Db.Set<TaiKhoan>().Where(x => x.MaTaiKhoan == f.AccountId).ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, "Inactive"));
            await Problem(await Change(f.Reception, cancelled, true), 401, "invalidSession");
        });
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task ScreeningAloneAndRecordedShotBlockBothMutations(bool shot)
    {
        await SqlFixtureLifecycle.RunAsync(VaccinationSqlTests.Fixture.Create, async f => {
            var source = await f.Source();
            if (shot) {
                using var response = await f.Nurse.PostAsJsonAsync("/api/v1/vaccinations", new RecordVaccinationRequest(source.Assessment.Indications.Single().Id, "[DEMO] S5-VALID-A", "Cánh tay"));
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            }
            var a = (await f.Base.Reception.GetFromJsonAsync<AppointmentDto>(Route(source.Appointment.Id)))!;
            Assert.False(a.CanManage); Assert.Equal("clinicalAlreadyStarted", a.ManagementUnavailableReason);
            await Problem(await Change(f.Base.Reception, a), 409, "clinicalAlreadyStarted");
            await Problem(await Change(f.Base.Reception, a, true), 409, "clinicalAlreadyStarted");
        });
    }

    [Fact] public async Task ConcurrentReceptionistsAndDuplicateTimeDoNotOverwrite()
    {
        await Run(async f => {
            var registration = await f.Register(); var a = await Appointment(f, registration);
            using var second = await f.Actor("LeTan", true);
            var responses = await Task.WhenAll(Change(f.Reception, a), Change(second, a, true));
            Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
            await Problem(Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict), 409, "concurrencyConflict");
            foreach (var r in responses) r.Dispose();
            a = (await f.Reception.GetFromJsonAsync<AppointmentDto>(Route(a.Id)))!;
            var b = await Appointment(f, registration);
            using var duplicate = await f.Reception.PostAsJsonAsync(Route(b.Id) + "/reschedule", new { scheduledAt = a.ScheduledAt, version = b.Version });
            await Problem(duplicate, 409, "appointmentAlreadyExists");
        });
    }

    [Theory][InlineData(false, "queued")][InlineData(true, "queued")]
    [InlineData(false, "providerUnavailable")][InlineData(true, "providerUnavailable")]
    [InlineData(false, "providerRejected")][InlineData(true, "providerRejected")]
    [InlineData(false, "deliveryUncertain")][InlineData(true, "deliveryUncertain")]
    [InlineData(false, "delivered")][InlineData(true, "delivered")]
    public async Task QueuedSnapshotsInvalidateWhileProviderOutcomesRemainHistoricalAndCannotRetry(bool cancel, string state)
    {
        await Run(async f => {
            var a = await Appointment(f); using var owner = await Owner(f);
            using (var legacy = await f.Reception.PostAsJsonAsync(Route(a.Id) + "/reminders", new { })) Assert.Equal(HttpStatusCode.Created, legacy.StatusCode);
            var log = await f.Db.Set<NhacLich>().AsNoTracking().SingleAsync();
            using (var enqueue = await f.Reception.PostAsJsonAsync(Email(a.Id), new { })) Assert.Equal(HttpStatusCode.Created, enqueue.StatusCode);
            var sink = new GateTransport { Error = state is "providerUnavailable" or "providerRejected" or "deliveryUncertain" ? state : null };
            if (state != "queued") { await using var db = NewDb(f); await new ReminderEmailDelivery(db, sink).DrainAsync(default); }
            var wasRetryable = state is "providerUnavailable" or "providerRejected";
            if (wasRetryable) {
                // Make retry available before mutation so the test exercises source/eligibility gates, not cooldown.
                await f.Db.Set<ReminderEmail>().ExecuteUpdateAsync(s => s.SetProperty(r => r.NextAttemptAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
                Assert.True((await f.Reception.GetFromJsonAsync<ReminderEmailDetailDto>(Email(a.Id)))!.Reminder!.CanRetry);
            }
            var snapshot = await f.Db.Set<ReminderEmail>().AsNoTracking().SingleAsync();
            var changed = await Changed(await Change(f.Reception, a, cancel));
            var stored = await f.Db.Set<ReminderEmail>().AsNoTracking().SingleAsync();
            if (state == "queued") {
                Assert.Equal("ChoGui", snapshot.Status);
                snapshot.Status = "GuiLoi"; snapshot.LastErrorCode = "reminderSourceConflict"; snapshot.NextAttemptAt = null;
            } else {
                Assert.Equal(state == "delivered" ? "DaGui" : "GuiLoi", stored.Status);
                Assert.Equal(state == "delivered" ? null : state, stored.LastErrorCode);
            }
            // Compare every persisted field, including provider outcome, claim state, attempts/times and content.
            Assert.Equal(JsonSerializer.Serialize(snapshot), JsonSerializer.Serialize(stored));
            var after = new GateTransport(); await using (var db = NewDb(f)) await new ReminderEmailDelivery(db, after).DrainAsync(default);
            Assert.Equal(0, after.Calls);
            await Problem(await f.Reception.PostAsJsonAsync(Email(a.Id) + "/retry", new { expectedAttempts = stored.Attempts }), 409,
                cancel && wasRetryable ? "reminderSourceConflict" : "reminderRetryUnavailable");
            var detail = (await f.Reception.GetFromJsonAsync<ReminderEmailDetailDto>(Email(a.Id)))!;
            Assert.False(detail.Reminder?.CanRetry ?? false);
            Assert.All(detail.History!, old => Assert.False(old.CanRetry));
            Assert.Equal(JsonSerializer.Serialize(snapshot), JsonSerializer.Serialize(await f.Db.Set<ReminderEmail>().AsNoTracking().SingleAsync()));
            if (!cancel) {
                Assert.True(detail.CanRecord); Assert.Null(detail.Reminder); Assert.Single(detail.History!);
                using var enqueue = await f.Reception.PostAsJsonAsync(Email(a.Id), new { }); Assert.Equal(HttpStatusCode.Created, enqueue.StatusCode);
                Assert.Equal(2, await f.Db.Set<ReminderEmail>().CountAsync());
                var legacyDetail = (await f.Reception.GetFromJsonAsync<ReminderDetailDto>(Route(a.Id) + "/reminders"))!;
                Assert.Null(legacyDetail.Reminder); Assert.True(legacyDetail.CanRecord);
            } else {
                Assert.False(detail.CanRecord);
                // RF-05G keeps cancelled appointments discoverable for read-only delivery history.
                Assert.Contains((await f.Reception.GetFromJsonAsync<AppointmentListResponse>("/api/v1/reminders/email-queue"))!.Items, x => x.Id == a.Id && x.Status == "DaHuy");
            }
            var preserved = await f.Db.Set<NhacLich>().AsNoTracking().SingleAsync();
            Assert.Equal(log.NoiDungMoPhong, preserved.NoiDungMoPhong); Assert.Equal(log.ThoiDiemGui, preserved.ThoiDiemGui);
            Assert.Equal(log.ScheduledAt, preserved.ScheduledAt);
        });
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task InFlightProviderCompletesBeforeMutationCanCommit(bool cancel)
    {
        await Run(async f => {
            var a = await Appointment(f); using var owner = await Owner(f);
            using (var enqueue = await f.Reception.PostAsJsonAsync(Email(a.Id), new { })) Assert.Equal(HttpStatusCode.Created, enqueue.StatusCode);
            var sink = new GateTransport { Hold = true };
            await using var db = NewDb(f);
            var dispatch = new ReminderEmailDelivery(db, sink).DrainAsync(default);
            await sink.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var mutation = Change(f.Reception, a, cancel);
            try {
                Assert.NotSame(mutation, await Task.WhenAny(mutation, Task.Delay(200)));
                Assert.Equal(a.Version, (await f.Reception.GetFromJsonAsync<AppointmentDto>(Route(a.Id)))!.Version);
            } finally { sink.Release.TrySetResult(); }
            await dispatch; var changed = await Changed(await mutation);
            Assert.NotEqual(a.Version, changed.Version); Assert.Equal(1, sink.Calls);
            Assert.Equal("DaGui", (await f.Db.Set<ReminderEmail>().AsNoTracking().SingleAsync()).Status);
            await using var again = NewDb(f); await new ReminderEmailDelivery(again, sink).DrainAsync(default); Assert.Equal(1, sink.Calls);
        });
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task AuditFailureRollsBackAppointmentAndPendingDelivery(bool cancel)
    {
        await Run(async f => {
            var a = await Appointment(f); using var owner = await Owner(f);
            using (var enqueue = await f.Reception.PostAsJsonAsync(Email(a.Id), new { })) Assert.Equal(HttpStatusCode.Created, enqueue.StatusCode);
            await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER LifecycleAuditFailure ON SecurityAudit AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE Action IN ('appointments.rescheduled','appointments.cancelled')) THROW 51000, 'Synthetic failure', 1;");
            await Problem(await Change(f.Reception, a, cancel), 503, "dependencyUnavailable");
            Assert.Equal(a, await f.Reception.GetFromJsonAsync<AppointmentDto>(Route(a.Id)));
            Assert.Equal("ChoGui", (await f.Db.Set<ReminderEmail>().AsNoTracking().SingleAsync()).Status);
        });
    }

    [Fact] public async Task MigrationRepeatUpDownPreservesLegacyLogsAndGuardsHistory()
    {
        await Run(async f => {
            var migrator = f.Db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261106024129_RF05EReminderEmail");
            await f.Db.Database.ExecuteSqlRawAsync("INSERT INTO LichHenTiem(MaNguoiDuocTiem,ThoiGianHen,DiemTiem,TrangThai) SELECT TOP(1) MaNguoiDuocTiem,'20300101',N'Địa điểm cũ',N'Legacy' FROM NguoiDuocTiem; INSERT INTO NhacLich(MaLichHen,KenhGui,ThoiDiemGui,TrangThai,NoiDungMoPhong) SELECT TOP(1) MaLichHen,N'LogMoPhong','20261101',N'DaGhiLogMoPhong',N'Lịch sử cũ' FROM LichHenTiem;");
            await migrator.MigrateAsync(); await migrator.MigrateAsync();
            var log = await f.Db.Set<NhacLich>().AsNoTracking().SingleAsync(); Assert.Equal(new DateTime(2030, 1, 1), log.ScheduledAt);
            Assert.False(f.Db.Database.HasPendingModelChanges());
            await migrator.MigrateAsync("20261106024129_RF05EReminderEmail"); await migrator.MigrateAsync();
            Assert.Equal("Lịch sử cũ", (await f.Db.Set<NhacLich>().AsNoTracking().SingleAsync()).NoiDungMoPhong);
            await f.Db.Database.ExecuteSqlRawAsync("UPDATE LichHenTiem SET TrangThai=N'DaHuy';");
            await Assert.ThrowsAnyAsync<Exception>(() => migrator.MigrateAsync("20261106024129_RF05EReminderEmail"));
            Assert.Equal(8, (await f.Db.Set<LichHenTiem>().AsNoTracking().SingleAsync()).RowVersion.Length);
        });
    }

    private sealed class GateTransport : IReminderEmailTransport
    {
        public bool Hold; public string? Error; public int Calls;
        public TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task DeliverAsync(ReminderEmailMessage message, CancellationToken ct)
        {
            Calls++; Entered.TrySetResult(); if (Hold) await Release.Task.WaitAsync(ct);
            if (Error != null) throw new ReminderDeliveryFailure(Error);
        }
    }
}
