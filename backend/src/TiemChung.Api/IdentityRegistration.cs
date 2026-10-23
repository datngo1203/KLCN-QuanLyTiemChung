using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public sealed class IdentityRegistration(TiemChungDbContext db, IConfiguration configuration)
{
    private static readonly PasswordHasher<TaiKhoan> Hasher = new();
    public static string Normalize(string value) => value.Trim().ToUpperInvariant();

    public async Task RegisterAsync(string email, string password, string trace, CancellationToken ct)
    {
        email = email.Trim();
        if (email.Length > 254 || !MailAddress.TryCreate(email, out var address) || address.Address != email
            || password.Length is < 12 or > 128) throw new IdentityFailure(400, "validation");
        var normalized = Normalize(email);
        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        // Serialize public register/resend races without trusting client identifiers as actors.
        await RegistrationLockAsync(ct);
        if (await db.Set<TaiKhoan>().AnyAsync(x => x.NormalizedEmail == normalized, ct)) {
            await tx.CommitAsync(ct);
            return;
        }
        var account = new TaiKhoan {
            AccountType = "Customer", LoginEmail = email, NormalizedEmail = normalized,
            TenDangNhap = "customer-" + Guid.NewGuid().ToString("N"), TrangThai = "Active",
            CreatedAt = DateTimeOffset.UtcNow
        };
        account.MatKhauHash = Hasher.HashPassword(account, password);
        db.Add(account);
        await db.SaveChangesAsync(ct);
        await AddVerificationAsync(account, ct);
        IdentityTransactions.Audit(db, "account.registered", trace, subject: account.MaTaiKhoan);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task ResendAsync(string email, string trace, CancellationToken ct)
    {
        var normalized = Normalize(email);
        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        await RegistrationLockAsync(ct);
        var account = await db.Set<TaiKhoan>().SingleOrDefaultAsync(x => x.NormalizedEmail == normalized, ct);
        if (account is not null && account.AccountType == "Customer" && account.EmailVerifiedAt is null && account.TrangThai == "Active") {
            await db.Set<EmailVerification>().Where(x => x.AccountId == account.MaTaiKhoan && x.UsedAt == null && x.InvalidatedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.InvalidatedAt, DateTimeOffset.UtcNow), ct);
            await AddVerificationAsync(account, ct);
            IdentityTransactions.Audit(db, "emailVerificationResent", trace, subject: account.MaTaiKhoan);
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    private Task RegistrationLockAsync(CancellationToken ct) => db.Database.ExecuteSqlRawAsync("""
        DECLARE @r int;
        EXEC @r=sys.sp_getapplock @Resource=N'KLCN.M1.Registration', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
        IF @r<0 THROW 51001, 'Registration lock unavailable.', 1;
        """, ct);

    private async Task AddVerificationAsync(TaiKhoan account, CancellationToken ct)
    {
        var secret = configuration["Identity:MailEncryptionKey"] ?? throw new InvalidOperationException("Mail encryption key required.");
        var key = Convert.FromBase64String(secret);
        if (key.Length != 32) throw new InvalidOperationException("Mail encryption key must be 256 bits.");
        var token = IdentityTokens.RandomToken();
        var now = DateTimeOffset.UtcNow;
        var verification = new EmailVerification { Id = Guid.NewGuid(), AccountId = account.MaTaiKhoan,
            TokenHash = IdentityTokens.Hash(token), CreatedAt = now, ExpiresAt = now.AddMinutes(30) };
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(new { email = account.LoginEmail, token });
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(key, 16)) aes.Encrypt(nonce, plaintext, ciphertext, tag);
        CryptographicOperations.ZeroMemory(plaintext);
        db.Add(verification);
        db.Add(new IdentityMailOutbox { Id = Guid.NewGuid(), VerificationId = verification.Id,
            EncryptedPayload = [..nonce, ..tag, ..ciphertext], CreatedAt = now, ExpiresAt = verification.ExpiresAt });
        await Task.CompletedTask;
    }

    public async Task ConfirmAsync(string token, string trace, CancellationToken ct)
    {
        if (token.Length > 256) throw new IdentityFailure(400, "invalidVerificationToken");
        var hash = IdentityTokens.Hash(token);
        var candidate = await db.Set<EmailVerification>().AsNoTracking().SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (candidate is null) throw new IdentityFailure(400, "invalidVerificationToken");
        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        var account = await IdentityTransactions.LockAccountAsync(db, candidate.AccountId, ct);
        var now = DateTimeOffset.UtcNow;
        var changed = await db.Set<EmailVerification>().Where(x => x.Id == candidate.Id && x.UsedAt == null && x.InvalidatedAt == null && x.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UsedAt, now), ct);
        if (account is null || account.AccountType != "Customer" || account.TrangThai != "Active" || changed != 1)
            throw new IdentityFailure(400, "invalidVerificationToken");
        account.EmailVerifiedAt = now;
        IdentityTransactions.Audit(db, "emailConfirmed", trace, subject: account.MaTaiKhoan);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
