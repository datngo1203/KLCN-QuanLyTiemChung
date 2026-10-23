using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;

namespace TiemChung.Infrastructure.Data;

internal static class IdentityModel
{
    public static void Configure(ModelBuilder m)
    {
        var a = m.Entity<TaiKhoan>();
        a.Property(x => x.AccountType).HasColumnType("varchar(16)");
        a.Property(x => x.AdministrativeDuty).HasColumnType("varchar(16)");
        a.Property(x => x.LoginEmail).HasMaxLength(254);
        a.Property(x => x.NormalizedEmail).HasMaxLength(254);
        a.Property(x => x.NormalizedUsername).HasMaxLength(100);
        a.Property(x => x.RowVersion).IsRowVersion();
        a.HasIndex(x => x.NormalizedEmail).IsUnique().HasFilter("[NormalizedEmail] IS NOT NULL");
        a.HasIndex(x => x.NormalizedUsername).IsUnique().HasFilter("[NormalizedUsername] IS NOT NULL");
        a.HasIndex(x => x.MaNhanVien).IsUnique().HasFilter("[MaNhanVien] IS NOT NULL");
        a.ToTable(t => {
            t.HasCheckConstraint("CK_TaiKhoan_Type", "([AccountType]='Customer' AND [MaNhanVien] IS NULL AND [LoginEmail] IS NOT NULL AND [NormalizedEmail] IS NOT NULL AND [NormalizedUsername] IS NULL AND [AdministrativeDuty]='None') OR ([AccountType]='Employee' AND [MaNhanVien] IS NOT NULL AND [NormalizedUsername] IS NOT NULL)");
            t.HasCheckConstraint("CK_TaiKhoan_Duty", "[AdministrativeDuty] IN ('None','Technical','Director') AND [SecurityVersion]>0");
        });
        var q = m.Entity<QuyenHan>();
        q.Property(x => x.Code).HasColumnType("varchar(64)");
        q.HasIndex(x => x.Code).IsUnique();
        m.Entity<KhachHang>().HasIndex(x => x.CCCD).IsUnique();

        var s = m.Entity<IdentitySession>();
        s.ToTable("IdentitySession", t => {
            t.HasCheckConstraint("CK_IdentitySession_Expiry", "[ExpiresAt]>[CreatedAt] AND [SecurityVersion]>0");
            t.HasCheckConstraint("CK_IdentitySession_Client", "[ClientType] IN ('web','native')");
        });
        s.HasKey(x => x.Id);
        s.HasOne<TaiKhoan>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        s.Property(x => x.ClientType).HasColumnType("varchar(8)");
        s.Property(x => x.RevocationReason).HasColumnType("varchar(40)");
        s.Property(x => x.CsrfSecretHash).HasColumnType("binary(32)");
        s.Property(x => x.RowVersion).IsRowVersion();
        s.HasIndex(x => new { x.AccountId, x.ExpiresAt });

        var r = m.Entity<RefreshCredential>();
        r.ToTable("RefreshCredential", t => t.HasCheckConstraint("CK_RefreshCredential_Expiry", "[ExpiresAt]>[CreatedAt]"));
        r.HasKey(x => x.Id);
        r.HasOne<IdentitySession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
        r.HasOne<RefreshCredential>().WithMany().HasForeignKey(x => x.ReplacedById).OnDelete(DeleteBehavior.Restrict);
        r.Property(x => x.TokenHash).HasColumnType("binary(32)");
        r.Property(x => x.RowVersion).IsRowVersion();
        r.HasIndex(x => x.TokenHash).IsUnique();
        r.HasIndex(x => x.SessionId).IsUnique().HasFilter("[ConsumedAt] IS NULL AND [RevokedAt] IS NULL");

        var e = m.Entity<EmailVerification>();
        e.ToTable("EmailVerification", t => t.HasCheckConstraint("CK_EmailVerification_Expiry", "[ExpiresAt]>[CreatedAt]"));
        e.HasKey(x => x.Id);
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        e.Property(x => x.TokenHash).HasColumnType("binary(32)");
        e.HasIndex(x => x.TokenHash).IsUnique();
        e.HasIndex(x => x.AccountId).IsUnique().HasFilter("[UsedAt] IS NULL AND [InvalidatedAt] IS NULL");
        var o = m.Entity<IdentityMailOutbox>();
        o.ToTable("IdentityMailOutbox"); o.HasKey(x => x.Id);
        o.HasOne<EmailVerification>().WithMany().HasForeignKey(x => x.VerificationId).OnDelete(DeleteBehavior.Restrict);
        o.Property(x => x.LastErrorCode).HasColumnType("varchar(40)");

        var l = m.Entity<AccountProfileLink>();
        l.ToTable("AccountProfileLink", t => {
            t.HasCheckConstraint("CK_ProfileLink_Target", "([CustomerId] IS NULL AND [RecipientId] IS NOT NULL) OR ([CustomerId] IS NOT NULL AND [RecipientId] IS NULL)");
            t.HasCheckConstraint("CK_ProfileLink_State", "[State] IN ('Active','Revoked')");
        });
        l.HasKey(x => x.Id);
        l.Property(x => x.State).HasColumnType("varchar(16)");
        l.Property(x => x.RowVersion).IsRowVersion();
        l.HasOne<TaiKhoan>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        l.HasOne<KhachHang>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        l.HasOne<NguoiDuocTiem>().WithMany().HasForeignKey(x => x.RecipientId).OnDelete(DeleteBehavior.Restrict);
        l.HasIndex(x => x.CustomerId).IsUnique().HasFilter("[State]='Active' AND [CustomerId] IS NOT NULL");
        l.HasIndex(x => x.RecipientId).IsUnique().HasFilter("[State]='Active' AND [RecipientId] IS NOT NULL");
        l.HasIndex(x => x.AccountId, "IX_ProfileLink_AccountCustomer").IsUnique().HasFilter("[State]='Active' AND [CustomerId] IS NOT NULL");
        l.HasIndex(x => x.AccountId, "IX_ProfileLink_AccountRecipient").IsUnique().HasFilter("[State]='Active' AND [RecipientId] IS NOT NULL");
        l.HasIndex(x => new { x.AccountId, x.State });

        var audit = m.Entity<SecurityAudit>();
        audit.ToTable("SecurityAudit"); audit.HasKey(x => x.Id);
        audit.HasOne<TaiKhoan>().WithMany().HasForeignKey(x => x.ActorAccountId).OnDelete(DeleteBehavior.Restrict);
        audit.HasOne<TaiKhoan>().WithMany().HasForeignKey(x => x.SubjectAccountId).OnDelete(DeleteBehavior.Restrict);
        audit.Property(x => x.Action).HasColumnType("varchar(64)");
        audit.Property(x => x.Outcome).HasColumnType("varchar(16)");
        audit.Property(x => x.ResourceType).HasColumnType("varchar(32)");
        audit.Property(x => x.ResourceId).HasMaxLength(64);
        audit.Property(x => x.TraceId).HasColumnType("varchar(64)");
        audit.Property(x => x.ChangedFields).HasMaxLength(512);
        audit.Property(x => x.OperatorIdentity).HasMaxLength(256);

        var d = m.Entity<IdentityRecoveryDatabase>();
        d.ToTable("IdentityRecoveryDatabase", t => t.HasCheckConstraint("CK_RecoveryDatabase_Singleton", "[Id]=1 AND [M1UpgradeEpoch]='M1'"));
        d.HasKey(x => x.Id); d.Property(x => x.Id).ValueGeneratedNever();
        d.Property(x => x.M1UpgradeEpoch).HasColumnType("varchar(16)");
        var op = m.Entity<IdentityRecoveryOperation>();
        op.ToTable("IdentityRecoveryOperation"); op.HasKey(x => x.OperationId);
        op.Property(x => x.M1UpgradeEpoch).HasColumnType("varchar(16)");
        op.Property(x => x.ManifestFingerprint).HasColumnType("binary(32)");
        op.Property(x => x.OperatorIdentity).HasMaxLength(256);
        op.Property(x => x.ResultCode).HasColumnType("varchar(40)");
        op.HasIndex(x => new { x.RecoveryDatabaseId, x.M1UpgradeEpoch }).IsUnique();
        op.HasOne<TaiKhoan>().WithMany().HasForeignKey(x => x.TargetAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
