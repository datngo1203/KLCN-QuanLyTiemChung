using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TiemChung.Api;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class AppointmentNotificationSqlTests
{
    private static Task Run(Func<AppointmentSqlTests.Fixture, Task> test) => SqlFixtureLifecycle.RunAsync(AppointmentSqlTests.Fixture.Create, test);
    private static async Task<HttpClient> Owner(AppointmentSqlTests.Fixture f)
    {
        var client = await f.Actor(null, false);
        f.Db.AddRange(new AccountProfileLink { AccountId = f.LastAccountId, CustomerId = f.CustomerId },
            new AccountProfileLink { AccountId = f.LastAccountId, RecipientId = f.RecipientId });
        await f.Db.SaveChangesAsync();
        return client;
    }
    private static TiemChungDbContext NewDb(AppointmentSqlTests.Fixture f) => new(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(f.Db.Database.GetConnectionString()).Options);
    private static async Task<AppointmentDto> Create(AppointmentSqlTests.Fixture f, RegistrationDto r)
    {
        using var response = await f.Reception.PostAsJsonAsync("/api/v1/appointments", f.Request(r, r.Items[0]));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AppointmentDto>())!;
    }
    private static async Task<AppointmentDto> Reschedule(AppointmentSqlTests.Fixture f, AppointmentDto a)
    {
        using var response = await f.Reception.PostAsJsonAsync($"/api/v1/appointments/{a.Id}/reschedule",
            new { version = a.Version, scheduledAt = a.ScheduledAt.AddHours(2).ToString("O") });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AppointmentDto>())!;
    }

    [Fact]
    public async Task PreferredWallClockPersistsAndConfirmedTimeNeverChangesIt()
    {
        await Run(async f => {
            using var owner = await Owner(f);
            var vaccine = await f.Db.Vaccines.FirstAsync();
            var req = RegistrationSqlTests.SyntheticRequest(f.CustomerId, f.RecipientId, [new("vaccine", vaccine.MaVacXin, 1, vaccine.Gia)]) with { PreferredTime = new(14, 45) };
            using (var missing = await owner.PostAsJsonAsync("/api/v1/registrations", req with { PreferredTime = null }))
                Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
            using var response = await owner.PostAsJsonAsync("/api/v1/registrations", req);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var registration = (await response.Content.ReadFromJsonAsync<RegistrationDto>())!;
            var preferred = req.PreferredDate!.Value.ToDateTime(new(14, 45));
            Assert.Equal(preferred, registration.PreferredAt); Assert.Equal(DateTimeKind.Unspecified, registration.PreferredAt!.Value.Kind);
            await f.Db.Registrations.Where(x => x.MaDangKy == registration.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.KenhThanhToanDuKien, "TaiTrungTam"));
            var appointment = await Create(f, registration);
            Assert.NotEqual(preferred, appointment.ScheduledAt.ToOffset(TimeSpan.FromHours(7)).DateTime);
            Assert.Equal(preferred, (await f.Db.Registrations.AsNoTracking().SingleAsync()).ThoiGianDuKien);
            Assert.Equal(preferred, (await f.Reception.GetFromJsonAsync<AppointmentSourceDetailDto>($"/api/v1/appointments/sources/{registration.Id}"))!.Source.PreferredAt);
            Assert.Equal(preferred, (await owner.GetFromJsonAsync<RegistrationDto>($"/api/v1/registrations/{registration.Id}"))!.PreferredAt);
            Assert.Equal(preferred, Assert.Single((await owner.GetFromJsonAsync<RegistrationListResponse>("/api/v1/registrations"))!.Items).PreferredAt);
            var journey = (await owner.GetFromJsonAsync<VaccinationJourneyDto>($"/api/v1/vaccination-journeys/{registration.Id}"))!;
            Assert.Equal(preferred, journey.PreferredAt); Assert.Equal(appointment.ScheduledAt, journey.NextAppointment!.ScheduledAt);
            using var other = await f.Actor(null, false);
            using var denied = await other.GetAsync($"/api/v1/vaccination-journeys/{registration.Id}");
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            foreach (var legacy in new DateTime?[] { preferred.Date, null }) {
                await f.Db.Registrations.ExecuteUpdateAsync(s => s.SetProperty(x => x.ThoiGianDuKien, legacy));
                var read = (await owner.GetFromJsonAsync<RegistrationDto>($"/api/v1/registrations/{registration.Id}"))!;
                Assert.Equal(legacy, read.PreferredAt);
            }
        });
    }

    [Fact]
    public async Task ThreeLifecycleSnapshotsDeliverAfterCommitIncludingCancelledAndNeverReplay()
    {
        await Run(async f => {
            using var owner = await Owner(f);
            var r = await f.Register(); var a = await Create(f, r);
            using (var duplicate = await f.Reception.PostAsJsonAsync("/api/v1/appointments", f.Request(r, r.Items[0]) with { ScheduledAt = a.ScheduledAt.ToString("O") }))
                Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            var b = await Reschedule(f, a);
            using var cancel = await f.Reception.PostAsJsonAsync($"/api/v1/appointments/{a.Id}/cancel", new { version = b.Version });
            Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
            using var stale = await f.Reception.PostAsJsonAsync($"/api/v1/appointments/{a.Id}/cancel", new { version = a.Version });
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
            var rows = await f.Db.Set<AppointmentNotification>().AsNoTracking().OrderBy(x => x.Id).ToArrayAsync();
            Assert.Equal(new[] { "AppointmentConfirmed", "AppointmentRescheduled", "AppointmentCancelled" }, rows.Select(x => x.EventType));
            Assert.Equal(3, rows.Select(x => x.AppointmentVersion).Distinct().Count());
            Assert.All(rows, x => { Assert.Equal("ChoGui", x.Status); Assert.Contains("khám sàng lọc", x.Body); Assert.Contains("UTC+07:00", x.Body); });
            Assert.Contains("Thời gian đã hủy", rows[2].Body);
            Assert.Empty(await f.Db.Set<ReminderEmail>().ToArrayAsync());
            await using var db = NewDb(f); var sink = new Capture(db);
            await Task.WhenAll(new AppointmentNotifications(db, sink).DrainAsync(default), DrainOther());
            async Task DrainOther() { await using var other = NewDb(f); await new AppointmentNotifications(other, new Capture(other, sink.Messages)).DrainAsync(default); }
            Assert.Equal(3, sink.Messages.Count);
            Assert.Equal(rows.Select(x => x.Body).Order(), sink.Messages.Select(x => x.Body).Order());
            Assert.All(await f.Db.Set<AppointmentNotification>().AsNoTracking().ToArrayAsync(), x => { Assert.Equal("DaGui", x.Status); Assert.Equal(1, x.Attempts); });
            await new AppointmentNotifications(db, sink).DrainAsync(default); Assert.Equal(3, sink.Messages.Count);
        });
    }

    [Theory][InlineData("create")][InlineData("reschedule")][InlineData("cancel")]
    public async Task OutboxAuditFailureRollsBackTheEntireBusinessMutation(string action)
    {
        await Run(async f => {
            using var owner = await Owner(f); var r = await f.Register();
            var a = action == "create" ? null : await Create(f, r);
            await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER NotificationAuditFailure ON SecurityAudit AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE Action='appointments.notificationEnqueued') THROW 51000, 'Synthetic failure', 1;");
            using var response = action == "create"
                ? await f.Reception.PostAsJsonAsync("/api/v1/appointments", f.Request(r, r.Items[0]))
                : action == "cancel" ? await f.Reception.PostAsJsonAsync($"/api/v1/appointments/{a!.Id}/cancel", new { version = a.Version })
                : await f.Reception.PostAsJsonAsync($"/api/v1/appointments/{a!.Id}/reschedule", new { version = a.Version, scheduledAt = a.ScheduledAt.AddHours(1) });
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.DoesNotContain("Synthetic failure", await response.Content.ReadAsStringAsync());
            Assert.Equal(a == null ? 0 : 1, await f.Db.Set<AppointmentNotification>().CountAsync());
            Assert.Equal(a == null ? 0 : 1, await f.Db.Set<LichHenTiem>().CountAsync());
            if (a != null) Assert.Equal(a, await f.Reception.GetFromJsonAsync<AppointmentDto>($"/api/v1/appointments/{a.Id}"));
            Assert.Equal(0, await f.Db.Set<SecurityAudit>().CountAsync(x => x.Action == "appointments.rescheduled" || x.Action == "appointments.cancelled"));
        });
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task MissingOrRevokedRecipientCannotLeakEmailAndDoesNotBlockAppointment(bool revoke)
    {
        await Run(async f => {
            if (revoke) await Owner(f);
            var a = await Create(f, await f.Register());
            if (revoke) await f.Db.Set<AccountProfileLink>().ExecuteUpdateAsync(s => s.SetProperty(x => x.State, "Revoked"));
            await using var db = NewDb(f); var sink = new Capture(db);
            await new AppointmentNotifications(db, sink).DrainAsync(default);
            Assert.Empty(sink.Messages);
            var row = await f.Db.Set<AppointmentNotification>().AsNoTracking().SingleAsync();
            Assert.Equal("GuiLoi", row.Status); Assert.Equal("reminderRecipientUnavailable", row.LastErrorCode);
            Assert.Equal(0, row.Attempts); Assert.Equal("DaXepLich", a.Status);
        });
    }

    [Fact]
    public async Task UnknownProviderOutcomeNeverAutomaticallyReplaysAndMigrationPreservesLegacy()
    {
        await Run(async f => {
            var r = await f.Register();
            var preferred = (await f.Db.Registrations.AsNoTracking().SingleAsync()).ThoiGianDuKien;
            var migrator = f.Db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261106144715_RF05FAppointmentLifecycle");
            await migrator.MigrateAsync(); await migrator.MigrateAsync();
            Assert.Equal(preferred, (await f.Db.Registrations.AsNoTracking().SingleAsync()).ThoiGianDuKien);
            Assert.False(f.Db.Database.HasPendingModelChanges());
            using var owner = await Owner(f); await Create(f, r);
            await f.Db.Set<AppointmentNotification>().ExecuteUpdateAsync(s => s.SetProperty(x => x.DispatchStarted, true).SetProperty(x => x.Attempts, 1));
            await using var db = NewDb(f); var sink = new Capture(db);
            await new AppointmentNotifications(db, sink).DrainAsync(default);
            await new AppointmentNotifications(db, sink).DrainAsync(default);
            Assert.Empty(sink.Messages);
            Assert.Equal("deliveryUncertain", (await f.Db.Set<AppointmentNotification>().AsNoTracking().SingleAsync()).LastErrorCode);
            await Assert.ThrowsAnyAsync<Exception>(() => migrator.MigrateAsync("20261106144715_RF05FAppointmentLifecycle"));
        });
    }

    [Fact]
    public async Task RacingMutationsEnqueueOnlyTheWinningVersion()
    {
        await Run(async f => {
            using var owner = await Owner(f); var a = await Create(f, await f.Register());
            var results = await Task.WhenAll(
                f.Reception.PostAsJsonAsync($"/api/v1/appointments/{a.Id}/cancel", new { version = a.Version }),
                f.Reception.PostAsJsonAsync($"/api/v1/appointments/{a.Id}/reschedule", new { version = a.Version, scheduledAt = a.ScheduledAt.AddHours(1) }));
            Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
            Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
            foreach (var result in results) result.Dispose();
            var current = (await f.Reception.GetFromJsonAsync<AppointmentDto>($"/api/v1/appointments/{a.Id}"))!;
            var rows = await f.Db.Set<AppointmentNotification>().AsNoTracking().OrderBy(x => x.Id).ToArrayAsync();
            Assert.Equal(2, rows.Length); Assert.Equal(current.Version, rows[1].AppointmentVersion);
            Assert.Equal(current.Status == "DaHuy" ? "AppointmentCancelled" : "AppointmentRescheduled", rows[1].EventType);
        });
    }

    [Theory][InlineData("providerUnavailable")][InlineData("providerRejected")][InlineData("deliveryUncertain")]
    public async Task ProviderFailureIsRecordedWithoutLeakingOrReplaying(string error)
    {
        await Run(async f => {
            using var owner = await Owner(f); await Create(f, await f.Register());
            await using var db = NewDb(f); var sink = new Failure(error);
            await new AppointmentNotifications(db, sink).DrainAsync(default);
            await new AppointmentNotifications(db, sink).DrainAsync(default);
            var row = await f.Db.Set<AppointmentNotification>().AsNoTracking().SingleAsync();
            Assert.Equal("GuiLoi", row.Status); Assert.Equal(error, row.LastErrorCode); Assert.Equal(1, sink.Calls);
        });
    }

    private sealed class Failure(string error) : IReminderEmailTransport
    {
        public int Calls;
        public Task DeliverAsync(ReminderEmailMessage message, CancellationToken ct) { Calls++; throw new ReminderDeliveryFailure(error); }
    }

    private sealed class Capture(TiemChungDbContext db, ConcurrentBag<ReminderEmailMessage>? messages = null) : IReminderEmailTransport
    {
        public ConcurrentBag<ReminderEmailMessage> Messages { get; } = messages ?? [];
        public Task DeliverAsync(ReminderEmailMessage message, CancellationToken ct)
        {
            Assert.Null(db.Database.CurrentTransaction); Messages.Add(message); return Task.CompletedTask;
        }
    }
}
