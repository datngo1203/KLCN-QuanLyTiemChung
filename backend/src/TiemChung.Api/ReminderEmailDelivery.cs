using System.Collections.Concurrent;
using System.Globalization;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public sealed record ReminderEmailMessage(int DeliveryId, string Recipient, string Subject, string Body);
public interface IReminderEmailTransport
{
    Task DeliverAsync(ReminderEmailMessage message, CancellationToken ct);
}
public sealed class ReminderDeliveryFailure(string code) : Exception(code)
{
    public string Code { get; } = code;
}

public sealed class SyntheticReminderEmailTransport : IReminderEmailTransport
{
    private readonly ConcurrentDictionary<int, ReminderEmailMessage> messages = new();
    public IReadOnlyDictionary<int, ReminderEmailMessage> Messages => messages;
    public Task DeliverAsync(ReminderEmailMessage message, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!IdentityMailReadiness.IsSynthetic(message.Recipient)) throw new ReminderDeliveryFailure("providerRejected");
        messages.TryAdd(message.DeliveryId, message);
        return Task.CompletedTask;
    }
}

public sealed class ReminderEmailTransport(IConfiguration config, IHostEnvironment env, SyntheticReminderEmailTransport synthetic) : IReminderEmailTransport
{
    public async Task DeliverAsync(ReminderEmailMessage message, CancellationToken ct)
    {
        if ((env.IsDevelopment() || env.IsEnvironment("Test")) && config["ReminderMail:Mode"] == "Synthetic") {
            await synthetic.DeliverAsync(message, ct); return;
        }
        if (!env.IsDevelopment() || config["ReminderMail:Mode"] != "Smtp") throw new ReminderDeliveryFailure("providerUnavailable");
        var host = config["ReminderMail:Host"];
        var user = config["ReminderMail:Username"];
        var password = config["ReminderMail:Password"];
        var sender = config["ReminderMail:SenderAddress"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password) ||
            !ReminderEmailEndpoints.ValidEmail(sender) || !int.TryParse(config["ReminderMail:Port"], out var port) || port is not (465 or 587))
            throw new ReminderDeliveryFailure("providerUnavailable");
        var mail = new MimeMessage();
        mail.From.Add(new MailboxAddress(config["ReminderMail:SenderName"] ?? "Trung tâm tiêm chủng VNVC - KLCN", sender!));
        mail.To.Add(MailboxAddress.Parse(message.Recipient));
        mail.Subject = message.Subject;
        mail.Body = new TextPart("plain") { Text = message.Body };
        // Stable across safe retries, without exposing a recipient or credential.
        mail.MessageId = $"reminder-{message.DeliveryId}@{sender!.Split('@')[1]}";
        using var smtp = new SmtpClient { Timeout = 30000 };
        try {
            await smtp.ConnectAsync(host, port, port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, ct);
            await smtp.AuthenticateAsync(user, password, ct);
        } catch (Exception) { throw new ReminderDeliveryFailure("providerUnavailable"); }
        try {
            await smtp.SendAsync(mail, ct);
        } catch (SmtpCommandException) { throw new ReminderDeliveryFailure("providerRejected"); }
        catch (Exception) { throw new ReminderDeliveryFailure("deliveryUncertain"); }
        // Once accepted, a disconnect failure cannot turn this into a retryable failure.
        try { await smtp.DisconnectAsync(true, ct); } catch (Exception) { }
    }
}

public sealed class ReminderEmailDelivery(TiemChungDbContext db, IReminderEmailTransport transport)
{
    private static void Audit(TiemChungDbContext db, ReminderEmail row, string action)
    {
        var audit = IdentityTransactions.Audit(db, action, "reminder-dispatch", row.CreatedByAccountId,
            outcome: action == "reminders.failed" ? "Failure" : "Success");
        audit.ResourceType = "ReminderEmail"; audit.ResourceId = row.Id.ToString(CultureInfo.InvariantCulture);
    }

    public async Task DrainAsync(CancellationToken ct)
    {
        // Session lock serializes workers without keeping any SQL transaction open over SMTP.
        await db.Database.OpenConnectionAsync(ct);
        var locked = false;
        try {
            await using (var command = db.Database.GetDbConnection().CreateCommand()) {
                command.CommandText = "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'KLCN.ReminderEmail.Dispatch', @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=0; SELECT @r;";
                locked = Convert.ToInt32(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture) >= 0;
            }
            if (!locked) return;
            var ids = await db.Set<ReminderEmail>().AsNoTracking().Where(x => x.Status == "ChoGui")
                .OrderBy(x => x.CreatedAt).Select(x => x.Id).Take(25).ToArrayAsync(ct);
            foreach (var id in ids) {
                db.ChangeTracker.Clear();
                ReminderEmail row;
                await using (var tx = await db.Database.BeginTransactionAsync(ct)) {
                    row = await db.Set<ReminderEmail>().FromSqlInterpolated($"SELECT * FROM ReminderEmail WITH (UPDLOCK,HOLDLOCK) WHERE Id={id}").SingleAsync(ct);
                    if (row.Status != "ChoGui") continue;
                    if (row.DispatchStarted) {
                        // Crash/SQL failure after claim: SMTP acceptance is unknown. Never resend automatically.
                        row.Status = "GuiLoi"; row.LastErrorCode = "deliveryUncertain"; row.NextAttemptAt = null;
                        Audit(db, row, "reminders.failed"); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); continue;
                    }
                    try {
                        var a = await ReminderEmailEndpoints.Source(db, row.AppointmentId, ct);
                        var owner = await ReminderEmailEndpoints.Recipient(db, a, ct);
                        if (!await ReminderEmailEndpoints.Eligible(db, a, ct) || !await ReminderEmailEndpoints.Snapshot(db, a).AnyAsync(x => x.Id == row.Id, ct))
                            throw new IdentityFailure(409, "reminderSourceConflict");
                        if (owner?.MaTaiKhoan != row.RecipientAccountId || owner.LoginEmail != row.RecipientEmail)
                            throw new IdentityFailure(409, "reminderRecipientUnavailable");
                    } catch (IdentityFailure e) {
                        row.Status = "GuiLoi"; row.LastErrorCode = e.Code; row.NextAttemptAt = null;
                        Audit(db, row, "reminders.failed"); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); continue;
                    }
                    row.DispatchStarted = true; row.Attempts++; row.LastAttemptAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
                }
                string? error = null;
                try { await transport.DeliverAsync(new(row.Id, row.RecipientEmail, row.Subject, row.Body), ct); }
                catch (ReminderDeliveryFailure e) { error = e.Code is "providerUnavailable" or "providerRejected" ? e.Code : "deliveryUncertain"; }
                catch (Exception) { error = "deliveryUncertain"; }
                await using (var tx = await db.Database.BeginTransactionAsync(CancellationToken.None)) {
                    row.Status = error == null ? "DaGui" : "GuiLoi";
                    row.LastErrorCode = error;
                    row.DeliveredAt = error == null ? DateTimeOffset.UtcNow : null;
                    row.NextAttemptAt = error is "providerUnavailable" or "providerRejected" ? DateTimeOffset.UtcNow.AddMinutes(1) : null;
                    Audit(db, row, error == null ? "reminders.delivered" : "reminders.failed");
                    await db.SaveChangesAsync(CancellationToken.None); await tx.CommitAsync(CancellationToken.None);
                }
            }
        } finally {
            if (locked) {
                await using var release = db.Database.GetDbConnection().CreateCommand();
                release.CommandText = "EXEC sys.sp_releaseapplock @Resource=N'KLCN.ReminderEmail.Dispatch', @LockOwner='Session';";
                await release.ExecuteNonQueryAsync(CancellationToken.None);
            }
            await db.Database.CloseConnectionAsync();
        }
    }
}

public sealed class ReminderEmailDispatcher(IServiceScopeFactory scopes, IConfiguration config, IHostEnvironment env) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!(env.IsDevelopment() && config["ReminderMail:Mode"] is "Smtp" or "Synthetic")) return;
        try {
            while (!stoppingToken.IsCancellationRequested) {
                try {
                    using var scope = scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<ReminderEmailDelivery>().DrainAsync(stoppingToken);
                } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception) { /* Never log payloads or raw provider exceptions. */ }
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
