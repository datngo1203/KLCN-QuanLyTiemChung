using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;

namespace TiemChung.Infrastructure.Data;

internal static class ReminderEmailModel
{
    internal static void Configure(ModelBuilder model)
    {
        var email = model.Entity<ReminderEmail>();
        email.ToTable("ReminderEmail", t => {
            t.HasCheckConstraint("CK_ReminderEmail_Status", "[Status] IN ('ChoGui','DaGui','GuiLoi') AND [Attempts]>=0 AND (([Status]='DaGui' AND [DeliveredAt] IS NOT NULL) OR ([Status]<>'DaGui' AND [DeliveredAt] IS NULL))");
        });
        email.HasKey(x => x.Id);
        // A later appointment time/source can have its own delivery; retries keep this row.
        email.HasIndex(x => new { x.AppointmentId, x.RegistrationId, x.RecipientId, x.ItemKind, x.RegistrationItemId, x.ScheduledAt }).IsUnique();
        email.HasIndex(x => new { x.Status, x.CreatedAt });
        email.Property(x => x.ScheduledAt).HasColumnType("datetime2");
        email.Property(x => x.ItemKind).HasMaxLength(10);
        email.Property(x => x.Location).HasMaxLength(200);
        email.Property(x => x.RecipientName).HasMaxLength(150);
        email.Property(x => x.ServiceName).HasMaxLength(150);
        email.Property(x => x.RecipientEmail).HasMaxLength(254);
        email.Property(x => x.Subject).HasMaxLength(150);
        email.Property(x => x.Body).HasMaxLength(4000);
        email.Property(x => x.Status).HasMaxLength(10);
        email.Property(x => x.LastErrorCode).HasMaxLength(40);
        email.HasOne<LichHenTiem>().WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        email.HasOne<TaiKhoan>().WithMany().HasForeignKey(x => x.RecipientAccountId).OnDelete(DeleteBehavior.Restrict);
        email.HasOne<TaiKhoan>().WithMany().HasForeignKey(x => x.CreatedByAccountId).OnDelete(DeleteBehavior.Restrict);
        email.HasOne<NhanVien>().WithMany().HasForeignKey(x => x.CreatedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
