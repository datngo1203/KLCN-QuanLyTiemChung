using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;

namespace TiemChung.Infrastructure.Data;

internal static class AppointmentNotificationModel
{
    internal static void Configure(ModelBuilder model)
    {
        var row = model.Entity<AppointmentNotification>();
        row.ToTable("AppointmentNotification", t => {
            t.HasCheckConstraint("CK_AppointmentNotification_Event", "[EventType] IN ('AppointmentConfirmed','AppointmentRescheduled','AppointmentCancelled') AND [Channel]='Email'");
            t.HasCheckConstraint("CK_AppointmentNotification_Status", "[Status] IN ('ChoGui','DaGui','GuiLoi') AND [Attempts]>=0 AND (([Status]='DaGui' AND [DeliveredAt] IS NOT NULL) OR ([Status]<>'DaGui' AND [DeliveredAt] IS NULL))");
        });
        row.HasKey(x => x.Id);
        row.HasIndex(x => new { x.AppointmentId, x.AppointmentVersion }).IsUnique();
        row.HasIndex(x => new { x.Status, x.Id });
        row.Property(x => x.AppointmentVersion).HasMaxLength(12);
        row.Property(x => x.EventType).HasMaxLength(30);
        row.Property(x => x.Channel).HasMaxLength(10);
        row.Property(x => x.RecipientEmail).HasMaxLength(254);
        row.Property(x => x.Subject).HasMaxLength(150);
        row.Property(x => x.Body).HasMaxLength(4000);
        row.Property(x => x.Status).HasMaxLength(10);
        row.Property(x => x.LastErrorCode).HasMaxLength(40);
        row.HasOne<LichHenTiem>().WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        row.HasOne<TaiKhoan>().WithMany().HasForeignKey(x => x.RecipientAccountId).OnDelete(DeleteBehavior.Restrict);
        row.HasOne<TaiKhoan>().WithMany().HasForeignKey(x => x.CreatedByAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
