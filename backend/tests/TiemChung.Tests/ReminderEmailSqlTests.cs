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

public sealed class ReminderEmailSqlTests
{
    private static string Path(int id) => $"/api/v1/appointments/{id}/email-reminder";
    private static Task Run(Func<AppointmentSqlTests.Fixture, Task> test) => SqlFixtureLifecycle.RunAsync(AppointmentSqlTests.Fixture.Create, test);
    private static async Task<AppointmentDto> Appointment(AppointmentSqlTests.Fixture f, string kind = "vaccine")
    {
        var registration = await f.Register();
        using var response = await f.Reception.PostAsJsonAsync("/api/v1/appointments", f.Request(registration, registration.Items.Single(x => x.Kind == kind)));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AppointmentDto>())!;
    }
    private static async Task<int> Link(AppointmentSqlTests.Fixture f)
    {
        using var customer = await f.Actor(null, false);
        var id = f.LastAccountId;
        f.Db.AddRange(new AccountProfileLink { AccountId = id, CustomerId = f.CustomerId }, new AccountProfileLink { AccountId = id, RecipientId = f.RecipientId });
        await f.Db.SaveChangesAsync();
        return id;
    }
    private static async Task<ReminderEmailDto> Enqueue(AppointmentSqlTests.Fixture f, int id)
    {
        using var response = await f.Reception.PostAsJsonAsync(Path(id), new { });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ReminderEmailDto>())!;
    }
    private static async Task Problem(HttpResponseMessage response, int status, string code)
    {
        using (response) {
            Assert.Equal((HttpStatusCode)status, response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
            var text = await response.Content.ReadAsStringAsync();
            Assert.Contains($"\"code\":\"{code}\"", text); Assert.DoesNotContain("SqlException", text);
        }
    }
    private static TiemChungDbContext NewDb(AppointmentSqlTests.Fixture f) => new(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(f.Db.Database.GetConnectionString()).Options);
    private static async Task<string> State(AppointmentSqlTests.Fixture f)
    {
        var values = new List<string>();
        await f.Db.Database.OpenConnectionAsync();
        try {
            foreach (var table in new[] { "ReminderEmail", "NhacLich", "SecurityAudit", "LichHenTiem", "DangKyDichVu", "ThanhToan", "MuiTiem", "LoVacXin", "BienDongKho" }) {
                await using var cmd = f.Db.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = $"SELECT (SELECT * FROM [{table}] ORDER BY 1 FOR JSON PATH, INCLUDE_NULL_VALUES)";
                values.Add(Convert.ToString(await cmd.ExecuteScalarAsync())!);
            }
        } finally { await f.Db.Database.CloseConnectionAsync(); }
        return string.Join("\n", values);
    }

    [Theory][InlineData("vaccine")][InlineData("package")]
    public async Task VerifiedOwnerSnapshotReadOnlyReconcileAndDispatcherSuccess(string kind)
    {
        await Run(async f => {
            var a = await Appointment(f, kind); var owner = await Link(f);
            await f.Reception.PostAsJsonAsync($"/api/v1/appointments/{a.Id}/reminders", new { });
            var before = await State(f);
            var preview = (await f.Reception.GetFromJsonAsync<ReminderEmailDetailDto>(Path(a.Id)))!;
            Assert.True(preview.CanRecord); Assert.Null(preview.Reminder);
            Assert.Equal(before, await State(f));
            Assert.StartsWith("[VNVC] Nhắc lịch tiêm chủng LH-", preview.Subject);
            foreach (var text in new[] { "VNVC Hoàng Văn Thụ", $"LH-{a.Id:0000}", $"DK-{a.RegistrationId:0000}", "Người được tiêm:", "Dịch vụ:", "UTC+07:00", "Asia/Ho_Chi_Minh", a.Location, "028 7102 6595", "khám sàng lọc", "liên hệ trung tâm" })
                Assert.Contains(text, preview.Preview);
            Assert.DoesNotContain("[DEMO]", preview.Preview); Assert.DoesNotContain("mô phỏng", preview.Preview);
            var saved = await Enqueue(f, a.Id); Assert.Equal("ChoGui", saved.Status); Assert.Equal(0, saved.Attempts); Assert.Null(saved.DeliveredAt);
            Assert.Equal(preview.Preview, saved.Content); Assert.Equal(preview.MaskedRecipient, saved.MaskedRecipient);
            var row = await f.Db.Set<ReminderEmail>().AsNoTracking().SingleAsync();
            var address = (await f.Db.Set<TaiKhoan>().AsNoTracking().SingleAsync(x => x.MaTaiKhoan == owner)).LoginEmail!;
            Assert.Equal(address, row.RecipientEmail); Assert.DoesNotContain(address, await f.Reception.GetStringAsync(Path(a.Id)));
            var audit = await f.Db.Set<SecurityAudit>().SingleAsync(x => x.Action == "reminders.enqueued");
            Assert.Equal(f.AccountId, audit.ActorAccountId); Assert.NotNull(audit.SessionId); Assert.Equal(saved.Id.ToString(), audit.ResourceId);
            // Drop the first response; replay and GET converge on the same delivery.
            Assert.Equal(saved.Id, (await Enqueue(f, a.Id)).Id);
            var snapshotBody = row.Body;
            await f.Db.Recipients.Where(x => x.MaNguoiDuocTiem == a.Recipient.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.HoTen, "Nguyễn Test Đổi Tên"));
            var sink = new Capture();
            await using (var db = NewDb(f)) await new ReminderEmailDelivery(db, sink).DrainAsync(default);
            var message = Assert.Single(sink.Messages); Assert.Equal(address, message.Recipient); Assert.Equal(snapshotBody, message.Body); Assert.Equal(saved.Subject, message.Subject);
            var state = await State(f);
            var detail = (await f.Reception.GetFromJsonAsync<ReminderEmailDetailDto>(Path(a.Id)))!;
            Assert.Equal("DaGui", detail.Reminder!.Status); Assert.Equal(1, detail.Reminder.Attempts); Assert.NotNull(detail.Reminder.DeliveredAt); Assert.False(detail.Reminder.CanRetry);
            await f.Reception.GetFromJsonAsync<AppointmentListResponse>("/api/v1/reminders/email-queue"); Assert.Equal(state, await State(f));
            await using (var db = NewDb(f)) await new ReminderEmailDelivery(db, sink).DrainAsync(default);
            Assert.Single(sink.Messages); Assert.Single(await f.Db.Set<ReminderEmail>().ToArrayAsync());
            Assert.Single(await f.Db.Set<NhacLich>().ToArrayAsync()); Assert.Equal(1, await f.Db.Set<SecurityAudit>().CountAsync(x => x.Action == "reminders.delivered"));
        });
    }

    [Theory][InlineData("unverified")][InlineData("inactive")][InlineData("invalidEmail")][InlineData("missingLink")][InlineData("revokedLink")][InlineData("ambiguous")][InlineData("unverifiedOther")]
    public async Task RecipientFailsClosedWithoutEnqueue(string scenario)
    {
        await Run(async f => {
            var a = await Appointment(f); var owner = await Link(f);
            if (scenario == "unverified") await f.Db.Set<TaiKhoan>().Where(x => x.MaTaiKhoan == owner).ExecuteUpdateAsync(s => s.SetProperty(x => x.EmailVerifiedAt, (DateTimeOffset?)null));
            if (scenario == "inactive") await f.Db.Set<TaiKhoan>().Where(x => x.MaTaiKhoan == owner).ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, "Inactive"));
            if (scenario == "invalidEmail") await f.Db.Set<TaiKhoan>().Where(x => x.MaTaiKhoan == owner).ExecuteUpdateAsync(s => s.SetProperty(x => x.LoginEmail, "invalid"));
            if (scenario == "missingLink") await f.Db.Set<AccountProfileLink>().Where(x => x.AccountId == owner && x.RecipientId != null).ExecuteDeleteAsync();
            if (scenario == "revokedLink") await f.Db.Set<AccountProfileLink>().Where(x => x.AccountId == owner && x.CustomerId != null).ExecuteUpdateAsync(s => s.SetProperty(x => x.State, "Revoked"));
            if (scenario is "ambiguous" or "unverifiedOther") {
                // Simulate corrupted legacy ownership only in this GUID-owned disposable fixture.
                await f.Db.Database.ExecuteSqlRawAsync("DROP INDEX IX_AccountProfileLink_CustomerId ON AccountProfileLink; DROP INDEX IX_AccountProfileLink_RecipientId ON AccountProfileLink;");
                var second = await Link(f);
                if (scenario == "unverifiedOther") await f.Db.Set<TaiKhoan>().Where(x => x.MaTaiKhoan == second).ExecuteUpdateAsync(s => s.SetProperty(x => x.EmailVerifiedAt, (DateTimeOffset?)null));
            }
            var state = await State(f);
            var preview = (await f.Reception.GetFromJsonAsync<ReminderEmailDetailDto>(Path(a.Id)))!;
            Assert.False(preview.CanRecord); Assert.Null(preview.Preview); Assert.Equal("reminderRecipientUnavailable", preview.UnavailableReason);
            await Problem(await f.Reception.PostAsJsonAsync(Path(a.Id), new { }), 409, "reminderRecipientUnavailable");
            Assert.Empty(await f.Db.Set<ReminderEmail>().ToArrayAsync()); Assert.Equal(state, await State(f));
        });
    }

    [Fact] public async Task ConcurrentOperatorsCreateExactlyOneSnapshotAndAudit()
    {
        await Run(async f => {
            var a = await Appointment(f); await Link(f); using var other = await f.Actor("LeTan", true);
            var responses = await Task.WhenAll(f.Reception.PostAsJsonAsync(Path(a.Id), new { }), other.PostAsJsonAsync(Path(a.Id), new { }));
            try {
                Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
                var rows = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<ReminderEmailDto>())); Assert.Single(rows.Select(x => x!.Id).Distinct());
            } finally { foreach (var r in responses) r.Dispose(); }
            Assert.Single(await f.Db.Set<ReminderEmail>().ToArrayAsync()); Assert.Equal(1, await f.Db.Set<SecurityAudit>().CountAsync(x => x.Action == "reminders.enqueued"));
        });
    }

    [Fact] public async Task AuditFailureRollsBackEnqueue()
    {
        await Run(async f => {
            var a = await Appointment(f); await Link(f);
            await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER EmailAuditFailure ON SecurityAudit AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE Action='reminders.enqueued') THROW 51000, 'Synthetic failure', 1;");
            await Problem(await f.Reception.PostAsJsonAsync(Path(a.Id), new { }), 503, "dependencyUnavailable");
            Assert.Empty(await f.Db.Set<ReminderEmail>().ToArrayAsync()); Assert.Equal(0, await f.Db.Set<SecurityAudit>().CountAsync(x => x.Action == "reminders.enqueued"));
        });
    }

    [Fact] public async Task AlreadyAdministeredAppointmentCannotEnqueueEmail()
    {
        await SqlFixtureLifecycle.RunAsync(VaccinationSqlTests.Fixture.Create, async f => {
            var source = await f.Source();
            using var shot = await f.Nurse.PostAsJsonAsync("/api/v1/vaccinations", new RecordVaccinationRequest(source.Assessment.Indications.Single().Id, "[DEMO] S5-VALID-A", "Cánh tay"));
            Assert.Equal(HttpStatusCode.Created, shot.StatusCode);
            var preview = (await f.Base.Reception.GetFromJsonAsync<ReminderEmailDetailDto>(Path(source.Appointment.Id)))!;
            Assert.False(preview.CanRecord); Assert.Null(preview.Preview);
            await Problem(await f.Base.Reception.PostAsJsonAsync(Path(source.Appointment.Id), new { }), 409, "reminderSourceConflict");
            Assert.Empty(await f.Db.Set<ReminderEmail>().ToArrayAsync());
        });
    }

    [Fact] public async Task RetryAuditRollbackAndUnknownPayloadAreSafe()
    {
        await Run(async f => {
            var a = await Appointment(f); await Link(f);
            foreach (var payload in new object[] { new { subject = "PRIVATE" }, new { recipient = "other@example.invalid" }, new { body = "PRIVATE" } })
                await Problem(await f.Reception.PostAsJsonAsync(Path(a.Id), payload), 400, "validation");
            await Enqueue(f, a.Id);
            await using (var db = NewDb(f)) await new ReminderEmailDelivery(db, new Capture { Error = "providerRejected" }).DrainAsync(default);
            await f.Db.Set<ReminderEmail>().ExecuteUpdateAsync(s => s.SetProperty(x => x.NextAttemptAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
            await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RetryAuditFailure ON SecurityAudit AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE Action='reminders.retried') THROW 51000, 'Synthetic failure', 1;");
            var state = await State(f);
            await Problem(await f.Reception.PostAsJsonAsync(Path(a.Id) + "/retry", new { expectedAttempts = 1 }), 503, "dependencyUnavailable");
            Assert.Equal(state, await State(f));
            using var anonymous = f.Base.Factory.CreateClient();
            await Problem(await anonymous.GetAsync(Path(a.Id)), 401, "invalidSession");
            using var customer = await f.Actor(null, false);
            await Problem(await customer.PostAsJsonAsync(Path(a.Id) + "/retry", new { expectedAttempts = 1 }), 403, "permissionDenied");
        });
    }

    [Fact] public async Task ProviderFailureRetryRaceAndWorkerRaceKeepOneDelivery()
    {
        await Run(async f => {
            var a = await Appointment(f); await Link(f); var saved = await Enqueue(f, a.Id); var sink = new Capture { Error = "providerUnavailable" };
            await using (var db = NewDb(f)) await new ReminderEmailDelivery(db, sink).DrainAsync(default);
            var detail = (await f.Reception.GetFromJsonAsync<ReminderEmailDetailDto>(Path(a.Id)))!;
            Assert.Equal("GuiLoi", detail.Reminder!.Status); Assert.Equal(1, detail.Reminder.Attempts); Assert.Equal("providerUnavailable", detail.Reminder.LastErrorCode);
            Assert.NotNull(detail.Reminder.NextAttemptAt); Assert.False(detail.Reminder.CanRetry);
            await Problem(await f.Reception.PostAsJsonAsync(Path(a.Id) + "/retry", new { expectedAttempts = 1 }), 409, "reminderRetryUnavailable");
            await f.Db.Set<ReminderEmail>().ExecuteUpdateAsync(s => s.SetProperty(x => x.NextAttemptAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
            Assert.True((await f.Reception.GetFromJsonAsync<ReminderEmailDetailDto>(Path(a.Id)))!.Reminder!.CanRetry);
            using var other = await f.Actor("LeTan", true);
            var responses = await Task.WhenAll(f.Reception.PostAsJsonAsync(Path(a.Id) + "/retry", new { expectedAttempts = 1 }), other.PostAsJsonAsync(Path(a.Id) + "/retry", new { expectedAttempts = 1 }));
            try { Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created); Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict); }
            finally { foreach (var r in responses) r.Dispose(); }
            sink.Error = null;
            await using var one = NewDb(f); await using var two = NewDb(f);
            await Task.WhenAll(new ReminderEmailDelivery(one, sink).DrainAsync(default), new ReminderEmailDelivery(two, sink).DrainAsync(default));
            Assert.Single(sink.Messages); Assert.Equal(saved.Id, sink.Messages[0].DeliveryId);
            detail = (await f.Reception.GetFromJsonAsync<ReminderEmailDetailDto>(Path(a.Id)))!;
            Assert.Equal("DaGui", detail.Reminder!.Status); Assert.Equal(2, detail.Reminder.Attempts);
            Assert.Null(detail.Reminder.LastErrorCode); Assert.Single(await f.Db.Set<ReminderEmail>().ToArrayAsync());
            Assert.Equal(1, await f.Db.Set<SecurityAudit>().CountAsync(x => x.Action == "reminders.retried"));
        });
    }

    [Theory][InlineData("uncertain")][InlineData("crash")][InlineData("audit")]
    public async Task UncertainAcceptanceOrInterruptedClaimNeverResends(string scenario)
    {
        await Run(async f => {
            var a = await Appointment(f); await Link(f); await Enqueue(f, a.Id); var sink = new Capture { Error = scenario == "uncertain" ? "deliveryUncertain" : null };
            if (scenario == "crash") await f.Db.Set<ReminderEmail>().ExecuteUpdateAsync(s => s.SetProperty(x => x.DispatchStarted, true).SetProperty(x => x.Attempts, 1).SetProperty(x => x.LastAttemptAt, DateTimeOffset.UtcNow));
            if (scenario == "audit") await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER DeliveredAuditFailure ON SecurityAudit AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE Action='reminders.delivered') THROW 51000, 'Synthetic failure', 1;");
            await using (var db = NewDb(f)) {
                if (scenario == "audit") await Assert.ThrowsAsync<DbUpdateException>(() => new ReminderEmailDelivery(db, sink).DrainAsync(default));
                else await new ReminderEmailDelivery(db, sink).DrainAsync(default);
            }
            await using (var db = NewDb(f)) await new ReminderEmailDelivery(db, sink).DrainAsync(default);
            var row = (await f.Reception.GetFromJsonAsync<ReminderEmailDetailDto>(Path(a.Id)))!.Reminder!;
            Assert.Equal("GuiLoi", row.Status); Assert.Equal("deliveryUncertain", row.LastErrorCode); Assert.False(row.CanRetry);
            await Problem(await f.Reception.PostAsJsonAsync(Path(a.Id) + "/retry", new { expectedAttempts = 1 }), 409, "reminderRetryUnavailable");
            Assert.Equal(scenario == "audit" ? 1 : 0, sink.Messages.Count);
        });
    }

    [Theory][InlineData("revoked")][InlineData("email")][InlineData("past")]
    public async Task DispatcherRechecksSourceAndRecipientBeforeProvider(string scenario)
    {
        await Run(async f => {
            var a = await Appointment(f); var owner = await Link(f); await Enqueue(f, a.Id);
            if (scenario == "revoked") await f.Db.Set<AccountProfileLink>().Where(x => x.AccountId == owner).ExecuteUpdateAsync(s => s.SetProperty(x => x.State, "Revoked"));
            if (scenario == "email") await f.Db.Set<TaiKhoan>().Where(x => x.MaTaiKhoan == owner).ExecuteUpdateAsync(s => s.SetProperty(x => x.LoginEmail, "changed@example.invalid"));
            if (scenario == "past") await f.Db.Set<LichHenTiem>().ExecuteUpdateAsync(s => s.SetProperty(x => x.ThoiGianHen, DateTime.UtcNow.AddDays(-1)));
            var sink = new Capture(); await using (var db = NewDb(f)) await new ReminderEmailDelivery(db, sink).DrainAsync(default);
            Assert.Empty(sink.Messages); var row = await f.Db.Set<ReminderEmail>().AsNoTracking().SingleAsync(); Assert.Equal("GuiLoi", row.Status); Assert.Equal(0, row.Attempts);
        });
    }

    [Theory][InlineData("past")][InlineData("source")][InlineData("session")][InlineData("role")]
    public async Task OperatorSourceAndAuthorizationStillFailClosed(string scenario)
    {
        await Run(async f => {
            var a = await Appointment(f); await Link(f);
            if (scenario == "past") await f.Db.Set<LichHenTiem>().ExecuteUpdateAsync(s => s.SetProperty(x => x.ThoiGianHen, DateTime.UtcNow.AddDays(-1)));
            if (scenario == "source") await f.Db.Registrations.ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, "Legacy"));
            if (scenario == "session") await f.Db.Set<IdentitySession>().Where(x => x.AccountId == f.AccountId).ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow));
            if (scenario == "role") await f.Db.Set<NhanVien>().Where(x => x.MaNhanVien == f.EmployeeId).ExecuteUpdateAsync(s => s.SetProperty(x => x.ChucDanh, "Admin"));
            await Problem(await f.Reception.PostAsJsonAsync(Path(a.Id), new { }), scenario == "session" ? 401 : scenario == "role" ? 403 : 409,
                scenario == "session" ? "invalidSession" : scenario == "role" ? "permissionDenied" : "reminderSourceConflict");
            Assert.Empty(await f.Db.Set<ReminderEmail>().ToArrayAsync());
        });
    }

    [Fact] public async Task ChangedAppointmentTimeAllowsNewSnapshotAndMigrationPreservesLegacy()
    {
        await Run(async f => {
            var a = await Appointment(f); await Link(f);
            await f.Reception.PostAsJsonAsync($"/api/v1/appointments/{a.Id}/reminders", new { });
            var legacy = await f.Db.Set<NhacLich>().AsNoTracking().SingleAsync();
            var migrator = f.Db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261105025904_S6PromotionIntegration");
            await migrator.MigrateAsync(); await migrator.MigrateAsync();
            Assert.Equal(legacy.NoiDungMoPhong, (await f.Db.Set<NhacLich>().AsNoTracking().SingleAsync()).NoiDungMoPhong);
            var first = await Enqueue(f, a.Id);
            await f.Db.Set<LichHenTiem>().Where(x => x.MaLichHen == a.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.ThoiGianHen, a.ScheduledAt.UtcDateTime.AddHours(1)));
            var second = await Enqueue(f, a.Id); Assert.NotEqual(first.Id, second.Id); Assert.Equal(2, await f.Db.Set<ReminderEmail>().CountAsync());
            await migrator.MigrateAsync(); await migrator.MigrateAsync(); Assert.False(f.Db.Database.HasPendingModelChanges());
            Assert.Equal(legacy.NoiDungMoPhong, (await f.Db.Set<NhacLich>().AsNoTracking().SingleAsync()).NoiDungMoPhong);
            await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => migrator.MigrateAsync("20261105025904_S6PromotionIntegration"));
        });
    }

    private sealed class Capture : IReminderEmailTransport
    {
        public string? Error;
        public List<ReminderEmailMessage> Messages { get; } = [];
        public Task DeliverAsync(ReminderEmailMessage message, CancellationToken ct)
        {
            if (Error != null) throw new ReminderDeliveryFailure(Error);
            Messages.Add(message); return Task.CompletedTask;
        }
    }
}
