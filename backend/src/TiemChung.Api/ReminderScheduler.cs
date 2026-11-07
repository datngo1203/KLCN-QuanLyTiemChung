using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public sealed class ReminderScheduler(TiemChungDbContext db, IConfiguration config)
{
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    public const int BatchSize = 100;
    private static readonly (string Kind, TimeSpan Offset)[] Triggers = [
        ("TMinus48", TimeSpan.FromHours(-48)), ("TMinus24", TimeSpan.FromHours(-24)),
        ("DPlus3", TimeSpan.FromDays(3)), ("DPlus7", TimeSpan.FromDays(7))
    ];

    // A small grace window after each due instant; never catch up an unbounded outage.
    internal static TimeSpan Tolerance(IConfiguration config)
    {
        var seconds = config.GetValue("ReminderAutomation:ToleranceSeconds", 300);
        if (seconds < PollInterval.TotalSeconds * 2 || seconds > 600)
            throw new InvalidOperationException("ReminderAutomation:ToleranceSeconds must be between 60 and 600.");
        return TimeSpan.FromSeconds(seconds);
    }

    public async Task EnqueueAsync(DateTimeOffset now, CancellationToken ct, Dictionary<string, int>? cursors = null)
    {
        var tolerance = Tolerance(config);
        foreach (var (kind, offset) in Triggers) {
            var upper = (now - offset).UtcDateTime;
            var lower = upper - tolerance;
            var afterId = cursors?.GetValueOrDefault(kind) ?? 0;
            var shots = from m in db.Set<MuiTiem>()
                        join i in db.Set<ChiDinhTiem>() on m.MaChiDinh equals i.MaChiDinh
                        join p in db.Set<PhieuKhamSangLoc>() on i.MaPhieuKham equals p.MaPhieuKham
                        where m.TrangThai == "DaTiem" select p.MaLichHen;
            // Rotate bounded pages so ineligible recipients cannot starve later appointments.
            var ids = await ClinicalEndpoints.Sources(db).Where(a => a.TrangThai == "DaXepLich" &&
                a.MaLichHen > afterId && a.ThoiGianHen > lower && a.ThoiGianHen <= upper && !shots.Contains(a.MaLichHen) &&
                !db.Set<ReminderEmail>().Any(r => r.AppointmentId == a.MaLichHen && r.RegistrationId == a.MaDangKy &&
                    r.RecipientId == a.MaNguoiDuocTiem && r.ScheduledAt == a.ThoiGianHen && r.Kind == kind &&
                    (r.ItemKind == "vaccine" ? r.RegistrationItemId == a.MaChiTietVacXin : r.RegistrationItemId == a.MaChiTietGoi)))
                .OrderBy(a => a.MaLichHen).Select(a => a.MaLichHen).Take(BatchSize).ToArrayAsync(ct);
            foreach (var id in ids) {
                db.ChangeTracker.Clear();
                // Same order as reschedule/cancel: session delivery lock, then SQL transaction.
                await using var deliveryLock = await AppointmentDeliveryLock.AcquireAsync(db, id, ct);
                await using var tx = await db.Database.BeginTransactionAsync(ct);
                var appointment = await db.Set<LichHenTiem>().FromSqlInterpolated($"SELECT * FROM LichHenTiem WITH (UPDLOCK,HOLDLOCK) WHERE MaLichHen={id}").SingleAsync(ct);
                if (appointment.ThoiGianHen <= lower || appointment.ThoiGianHen > upper) continue;
                try {
                    var a = await ReminderEmailEndpoints.Source(db, id, ct);
                    if (!await ReminderEmailEndpoints.Eligible(db, a, kind, now, ct) ||
                        await ReminderEmailEndpoints.Snapshot(db, a).AnyAsync(r => r.Kind == kind, ct)) continue;
                    var recipient = await ReminderEmailEndpoints.Recipient(db, a, ct);
                    if (recipient == null) continue;
                    var content = ReminderEmailEndpoints.Content(a, config, kind);
                    var row = new ReminderEmail {
                        Kind = kind, AppointmentId = id, RegistrationId = a.RegistrationId, CustomerId = a.Customer.Id,
                        RecipientId = a.Recipient.Id, ItemKind = a.ItemKind, RegistrationItemId = a.RegistrationItemId,
                        ScheduledAt = a.ScheduledAt.UtcDateTime, Location = a.Location, RecipientName = a.Recipient.FullName,
                        ServiceName = a.ServiceName, RecipientAccountId = recipient.MaTaiKhoan, RecipientEmail = recipient.LoginEmail!,
                        Subject = content.Subject, Body = content.Body, CreatedAt = now.ToUniversalTime()
                    };
                    db.Add(row); await db.SaveChangesAsync(ct);
                    var audit = IdentityTransactions.Audit(db, "reminders.enqueued", "reminder-scheduler");
                    audit.ResourceType = "ReminderEmail"; audit.ResourceId = row.Id.ToString(CultureInfo.InvariantCulture);
                    await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
                } catch (IdentityFailure e) when (e.Code == "reminderSourceConflict") {
                    // Source may have become invalid since the bounded candidate query.
                }
            }
            // Scan position is only a fairness hint. DB business keys own restart correctness.
            if (cursors != null) cursors[kind] = ids.Length == BatchSize ? ids[^1] : 0;
        }
    }
}

public sealed class ReminderSchedulerWorker(IServiceScopeFactory scopes, IConfiguration config, IHostEnvironment env,
    TimeProvider clock) : BackgroundService
{
    private readonly Dictionary<string, int> cursors = new();
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Keep RF-05E's delivery environment gate; no automation into a disabled provider.
        if (!env.IsDevelopment() || config["ReminderMail:Mode"] is not ("Smtp" or "Synthetic")) return;
        _ = ReminderScheduler.Tolerance(config);
        try {
            while (!stoppingToken.IsCancellationRequested) {
                try {
                    using var scope = scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<ReminderScheduler>().EnqueueAsync(clock.GetUtcNow(), stoppingToken, cursors);
                } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception) { /* Do not log payloads, addresses or provider errors. */ }
                await Task.Delay(ReminderScheduler.PollInterval, clock, stoppingToken);
            }
        } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
