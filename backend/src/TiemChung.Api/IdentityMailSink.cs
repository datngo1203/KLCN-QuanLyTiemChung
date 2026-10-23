using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public interface IVerificationMailSink
{
    // Sink must deduplicate deliveryId, since a crash can occur after delivery before DB commit.
    Task DeliverAsync(Guid deliveryId,string syntheticAddress,string token,CancellationToken ct);
}

public sealed class IdentityMailSink(TiemChungDbContext db,IConfiguration config,IHostEnvironment environment)
{
    public async Task DrainAsync(IVerificationMailSink sink,CancellationToken ct)
    {
        if(!environment.IsDevelopment() && !environment.IsEnvironment("Test"))
            throw new InvalidOperationException("Mail test sink unavailable.");
        var now=DateTimeOffset.UtcNow;
        await db.Set<IdentityMailOutbox>().Where(x=>x.ExpiresAt<=now && x.EncryptedPayload!=null)
            .ExecuteUpdateAsync(s=>s.SetProperty(x=>x.EncryptedPayload,(byte[]?)null),ct);
        var rows=await db.Set<IdentityMailOutbox>().Where(x=>x.SentAt==null && x.ExpiresAt>now && x.EncryptedPayload!=null &&
            (x.NextAttemptAt==null || x.NextAttemptAt<=now)).OrderBy(x=>x.CreatedAt).Take(25).ToListAsync(ct);
        var key=Convert.FromBase64String(config["Identity:MailEncryptionKey"]??throw new InvalidOperationException("Mail key required."));
        foreach(var row in rows) {
            var bytes=row.EncryptedPayload!; var plaintext=new byte[bytes.Length-28];
            try {
                using(var aes=new AesGcm(key,16)) aes.Decrypt(bytes.AsSpan(0,12),bytes.AsSpan(28),bytes.AsSpan(12,16),plaintext);
                using var doc=JsonDocument.Parse(plaintext);
                var email=doc.RootElement.GetProperty("email").GetString()!;
                if(!email.EndsWith("@example.invalid",StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Synthetic recipient required.");
                await sink.DeliverAsync(row.Id,email,doc.RootElement.GetProperty("token").GetString()!,ct);
                row.SentAt=DateTimeOffset.UtcNow; row.EncryptedPayload=null; row.LastErrorCode=null;
            } catch(Exception e) when(e is not OperationCanceledException) {
                row.LastErrorCode="testDeliveryFailed"; row.NextAttemptAt=DateTimeOffset.UtcNow.AddMinutes(1);
            } finally { CryptographicOperations.ZeroMemory(plaintext); }
            row.Attempts++; await db.SaveChangesAsync(ct);
        }
    }
}
