using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public sealed class CustomerPasswordResets(TiemChungDbContext db, IConfiguration config)
{
    public async Task RequestAsync(string email, string trace, CancellationToken ct)
    {
        email = email.Trim();
        if (email.Length > 254 || !MailAddress.TryCreate(email, out var address) || address.Address != email)
            throw new IdentityFailure(400, "validation");
        var normalized = IdentityRegistration.Normalize(email);
        var candidate = await db.Set<TaiKhoan>().AsNoTracking().SingleOrDefaultAsync(x => x.NormalizedEmail == normalized, ct);
        if (candidate is null) return;
        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        var account = await IdentityTransactions.LockAccountAsync(db, candidate.MaTaiKhoan, ct);
        if (account is null || account.AccountType != "Customer" || account.TrangThai != "Active" || account.NormalizedEmail != normalized)
            return;
        var now = DateTimeOffset.UtcNow;
        await db.Set<CustomerPasswordReset>().Where(x => x.AccountId == account.MaTaiKhoan && x.UsedAt == null && x.InvalidatedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.InvalidatedAt, now), ct);
        var token = IdentityTokens.RandomToken();
        var reset = new CustomerPasswordReset { Id = Guid.NewGuid(), AccountId = account.MaTaiKhoan,
            TokenHash = IdentityTokens.Hash(token), CreatedAt = now, ExpiresAt = now.AddMinutes(30) };
        db.Add(reset);
        var mail = IdentityMailSink.CreateMail(config, account.LoginEmail!, token, now, reset.ExpiresAt);
        mail.PasswordResetId = reset.Id;
        db.Add(mail);
        IdentityTransactions.Audit(db, "passwordResetRequested", trace, subject: account.MaTaiKhoan);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task ConfirmAsync(string token, string newPassword, string trace, CancellationToken ct)
    {
        if (newPassword.Length is < 12 or > 128) throw new IdentityFailure(400, "validation");
        if (token.Length is 0 or > 256) throw new IdentityFailure(400, "invalidPasswordResetToken");
        var hash = IdentityTokens.Hash(token);
        var candidate = await db.Set<CustomerPasswordReset>().AsNoTracking().SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (candidate is null) throw new IdentityFailure(400, "invalidPasswordResetToken");
        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        var account = await IdentityTransactions.LockAccountAsync(db, candidate.AccountId, ct);
        if (account is null || account.AccountType != "Customer" || account.TrangThai != "Active")
            throw new IdentityFailure(400, "invalidPasswordResetToken");
        var now = DateTimeOffset.UtcNow;
        var changed = await db.Set<CustomerPasswordReset>().Where(x => x.Id == candidate.Id && x.UsedAt == null && x.InvalidatedAt == null && x.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UsedAt, now), ct);
        if (changed != 1) throw new IdentityFailure(400, "invalidPasswordResetToken");
        account.MatKhauHash = new PasswordHasher<TaiKhoan>().HashPassword(account, newPassword);
        await IdentityTransactions.RevokeAccountAsync(db, account, "passwordReset", trace, null, ct);
        IdentityTransactions.Audit(db, "passwordResetConfirmed", trace, subject: account.MaTaiKhoan);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
