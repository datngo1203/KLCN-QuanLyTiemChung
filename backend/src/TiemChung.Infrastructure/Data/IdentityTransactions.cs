using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TiemChung.Domain.Entities;

namespace TiemChung.Infrastructure.Data;

public static class IdentityTransactions
{
    public static Task<IDbContextTransaction> BeginAsync(TiemChungDbContext db, CancellationToken ct) =>
        db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

    // All auth mutations and final Profiles write checks use account -> session.
    public static Task<TaiKhoan?> LockAccountAsync(TiemChungDbContext db, int id, CancellationToken ct) =>
        db.Set<TaiKhoan>().FromSqlInterpolated($"SELECT * FROM TaiKhoan WITH (UPDLOCK,HOLDLOCK) WHERE MaTaiKhoan={id}")
            .SingleOrDefaultAsync(ct);

    public static Task<IdentitySession?> LockSessionAsync(TiemChungDbContext db, Guid id, CancellationToken ct) =>
        db.Set<IdentitySession>().FromSqlInterpolated($"SELECT * FROM IdentitySession WITH (UPDLOCK,HOLDLOCK) WHERE Id={id}")
            .SingleOrDefaultAsync(ct);

    public static async Task AdministrationLockAsync(TiemChungDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result=sys.sp_getapplock @Resource=N'KLCN.M1.Administration',
                @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
            IF @result<0 THROW 51001, 'Administration lock unavailable.', 1;
            """, ct);
    }

    public static SecurityAudit Audit(TiemChungDbContext db, string action, string traceId,
        int? actor = null, int? subject = null, Guid? session = null, string outcome = "Success")
    {
        var entry = new SecurityAudit {
            OccurredAt = DateTimeOffset.UtcNow, Action = action, Outcome = outcome,
            ActorAccountId = actor, SubjectAccountId = subject, SessionId = session,
            TraceId = traceId
        };
        db.Add(entry);
        return entry;
    }

    // Caller holds account/session locks and commits this together with audit.
    public static async Task RevokeSessionAsync(TiemChungDbContext db, IdentitySession session,
        string reason, string traceId, int? actor, CancellationToken ct)
    {
        if (session.RevokedAt is not null) return;
        var now = DateTimeOffset.UtcNow;
        session.RevokedAt = now;
        session.RevocationReason = reason;
        await db.Set<RefreshCredential>()
            .Where(x => x.SessionId == session.Id && x.ConsumedAt == null && x.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, now), ct);
        Audit(db, "sessionRevoked", traceId, actor, session.AccountId, session.Id);
    }

    public static async Task RevokeAccountAsync(TiemChungDbContext db, TaiKhoan account,
        string reason, string traceId, int? actor, CancellationToken ct)
    {
        account.SecurityVersion = checked(account.SecurityVersion + 1);
        var sessions = await db.Set<IdentitySession>().Where(x => x.AccountId == account.MaTaiKhoan && x.RevokedAt == null)
            .OrderBy(x => x.Id).ToListAsync(ct);
        foreach (var session in sessions)
            await RevokeSessionAsync(db, session, reason, traceId, actor, ct);
    }
}
