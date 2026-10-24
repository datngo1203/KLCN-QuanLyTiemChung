using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public interface IVerificationMailSink
{
    // Dedup is per sink instance/lifetime; delivery cannot roll back with SQL.
    Task DeliverAsync(Guid deliveryId,string syntheticAddress,string token,CancellationToken ct);
}

public sealed class IdentityMailSink(TiemChungDbContext db,IConfiguration config,IHostEnvironment environment)
{
    public async Task DrainAsync(IVerificationMailSink sink,CancellationToken ct)
    {
        if(!environment.IsDevelopment() && !environment.IsEnvironment("Test"))
            throw new InvalidOperationException("Mail test sink unavailable.");
        await using var tx=await IdentityTransactions.BeginAsync(db,ct);
        await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'KLCN.M1.Mail', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @r<0 THROW 51001, 'Mail lock unavailable', 1;",ct);
        var now=DateTimeOffset.UtcNow;
        await db.Set<IdentityMailOutbox>().Where(x=>x.ExpiresAt<=now && x.EncryptedPayload!=null)
            .ExecuteUpdateAsync(s=>s.SetProperty(x=>x.EncryptedPayload,(byte[]?)null),ct);
        var rows=await db.Set<IdentityMailOutbox>().Where(x=>x.SentAt==null && x.ExpiresAt>now && x.EncryptedPayload!=null &&
            (x.NextAttemptAt==null || x.NextAttemptAt<=now) &&
            db.Set<EmailVerification>().Any(v=>v.Id==x.VerificationId && v.UsedAt==null && v.InvalidatedAt==null && v.ExpiresAt>now)).OrderBy(x=>x.CreatedAt).Take(25).ToListAsync(ct);
        var key=Convert.FromBase64String(config["Identity:MailEncryptionKey"]??throw new InvalidOperationException("Mail key required."));
        foreach(var row in rows) {
            var processingNow=DateTimeOffset.UtcNow;
            if(!await db.Set<EmailVerification>().AnyAsync(v=>v.Id==row.VerificationId && v.UsedAt==null && v.InvalidatedAt==null && v.ExpiresAt>processingNow,ct)) continue;
            var bytes=row.EncryptedPayload!; var plaintext=new byte[bytes.Length-28];
            try {
                using(var aes=new AesGcm(key,16)) aes.Decrypt(bytes.AsSpan(0,12),bytes.AsSpan(28),bytes.AsSpan(12,16),plaintext);
                using var doc=JsonDocument.Parse(plaintext);
                var email=doc.RootElement.GetProperty("email").GetString()!;
                if(!IdentityMailReadiness.IsSynthetic(email))
                    throw new InvalidOperationException("Synthetic recipient required.");
                await sink.DeliverAsync(row.Id,email,doc.RootElement.GetProperty("token").GetString()!,ct);
                row.SentAt=DateTimeOffset.UtcNow; row.EncryptedPayload=null; row.LastErrorCode=null;
            } catch(Exception e) when(e is not OperationCanceledException) {
                row.LastErrorCode="testDeliveryFailed"; row.NextAttemptAt=DateTimeOffset.UtcNow.AddMinutes(1);
            } finally { CryptographicOperations.ZeroMemory(plaintext); }
            row.Attempts++; await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
    }
}
