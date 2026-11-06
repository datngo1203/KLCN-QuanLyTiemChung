using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public sealed class AppointmentNotifications(TiemChungDbContext db, IReminderEmailTransport transport)
{
    internal static async Task Enqueue(TiemChungDbContext db, AppointmentDto appointment, string eventType,
        IdentityActor actor, string trace, CancellationToken ct)
    {
        var owner = await ReminderEmailEndpoints.Recipient(db, appointment, ct);
        var action = eventType switch {
            "AppointmentConfirmed" => "Xác nhận lịch hẹn đến trung tâm",
            "AppointmentRescheduled" => "Thay đổi lịch hẹn đến trung tâm",
            "AppointmentCancelled" => "Hủy lịch hẹn đến trung tâm",
            _ => throw new ArgumentOutOfRangeException(nameof(eventType))
        };
        var time = appointment.ScheduledAt.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm 'UTC+07:00 (Asia/Ho_Chi_Minh)'", CultureInfo.InvariantCulture);
        var row = new AppointmentNotification {
            AppointmentId = appointment.Id, AppointmentVersion = appointment.Version!, EventType = eventType,
            RecipientAccountId = owner?.MaTaiKhoan, RecipientEmail = owner?.LoginEmail ?? "",
            Subject = $"[VNVC] {action} LH-{appointment.Id:0000}",
            Body = $"Trung tâm tiêm chủng VNVC\n\n{action}.\nMã lịch hẹn: LH-{appointment.Id:0000}\nMã đăng ký: DK-{appointment.RegistrationId:0000}\nNgười được tiêm: {appointment.Recipient.FullName}\nDịch vụ: {appointment.ServiceName}\n{(eventType == "AppointmentCancelled" ? "Thời gian đã hủy" : "Thời gian đã xác nhận")}: {time}\nĐịa điểm: {appointment.Location}\n\nVui lòng xem lịch hẹn hiện tại trong tài khoản. Lịch hẹn không chứng minh đủ điều kiện tiêm; mỗi lượt tiêm vẫn cần khám sàng lọc và bác sĩ tư vấn/chỉ định. Nếu cần hỗ trợ, vui lòng liên hệ trung tâm.",
            CreatedByAccountId = actor.Account.MaTaiKhoan, CreatedAt = DateTimeOffset.UtcNow,
            Status = owner == null ? "GuiLoi" : "ChoGui", LastErrorCode = owner == null ? "reminderRecipientUnavailable" : null
        };
        db.Add(row);
        await db.SaveChangesAsync(ct);
        var audit = IdentityTransactions.Audit(db, "appointments.notificationEnqueued", trace, actor.Account.MaTaiKhoan, session: actor.Session.Id);
        audit.ResourceType = "AppointmentNotification"; audit.ResourceId = row.Id.ToString(CultureInfo.InvariantCulture);
        await db.SaveChangesAsync(ct);
    }

    public async Task DrainAsync(CancellationToken ct)
    {
        var ids = await db.Set<AppointmentNotification>().AsNoTracking().Where(x => x.Status == "ChoGui")
            .OrderBy(x => x.Id).Select(x => x.Id).Take(25).ToArrayAsync(ct);
        foreach (var id in ids) {
            db.ChangeTracker.Clear();
            var appointmentId = await db.Set<AppointmentNotification>().Where(x => x.Id == id).Select(x => x.AppointmentId).SingleAsync(ct);
            await using var deliveryLock = await AppointmentDeliveryLock.AcquireAsync(db, appointmentId, ct);
            AppointmentNotification row;
            await using (var tx = await db.Database.BeginTransactionAsync(ct)) {
                row = await db.Set<AppointmentNotification>().FromSqlInterpolated($"SELECT * FROM AppointmentNotification WITH (UPDLOCK,HOLDLOCK) WHERE Id={id}").SingleAsync(ct);
                if (row.Status != "ChoGui") continue;
                if (row.DispatchStarted) {
                    row.Status = "GuiLoi"; row.LastErrorCode = "deliveryUncertain";
                    await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); continue;
                }
                // Cancellation is deliverable too: revalidate ownership, not reminder eligibility/time.
                var appointment = await AppointmentEndpoints.Query(db, db.Set<LichHenTiem>().Where(x => x.MaLichHen == row.AppointmentId)).SingleAsync(ct);
                var owner = await ReminderEmailEndpoints.Recipient(db, appointment, ct);
                if (owner == null || owner.MaTaiKhoan != row.RecipientAccountId || owner.LoginEmail != row.RecipientEmail) {
                    row.Status = "GuiLoi"; row.LastErrorCode = "reminderRecipientUnavailable";
                    await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); continue;
                }
                row.DispatchStarted = true; row.Attempts++;
                await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            }
            string? error = null;
            try { await transport.DeliverAsync(new(row.Id, row.RecipientEmail, row.Subject, row.Body, "appointment"), ct); }
            catch (ReminderDeliveryFailure e) { error = e.Code is "providerUnavailable" or "providerRejected" ? e.Code : "deliveryUncertain"; }
            catch (Exception) { error = "deliveryUncertain"; }
            await using (var tx = await db.Database.BeginTransactionAsync(CancellationToken.None)) {
                row.Status = error == null ? "DaGui" : "GuiLoi"; row.LastErrorCode = error;
                row.DeliveredAt = error == null ? DateTimeOffset.UtcNow : null;
                var audit = IdentityTransactions.Audit(db, error == null ? "appointments.notificationDelivered" : "appointments.notificationFailed",
                    "appointment-dispatch", row.CreatedByAccountId, outcome: error == null ? "Success" : "Failure");
                audit.ResourceType = "AppointmentNotification"; audit.ResourceId = row.Id.ToString(CultureInfo.InvariantCulture);
                await db.SaveChangesAsync(CancellationToken.None); await tx.CommitAsync(CancellationToken.None);
            }
        }
    }
}
