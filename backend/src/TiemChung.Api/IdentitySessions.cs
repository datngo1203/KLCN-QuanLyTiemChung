using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public sealed record SessionTokens(string AccessToken, string RefreshToken, Guid SessionId, string? CsrfToken);

public sealed class IdentitySessions(TiemChungDbContext db, IdentityTokens tokens)
{
    private static readonly PasswordHasher<TaiKhoan> Hasher = new();

    public async Task ChangePasswordAsync(HttpContext context, IdentityAuthorization auth, string currentPassword,
        string newPassword, CancellationToken ct)
    {
        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        var actor = await auth.RequireAsync(context, true, ct);
        if (!PasswordMatches(actor.Account, currentPassword)) throw new IdentityFailure(401, "invalidCredentials");
        if (newPassword.Length is < 12 or > 128 || newPassword == currentPassword)
            throw new IdentityFailure(400, "validation");
        actor.Account.MatKhauHash = Hasher.HashPassword(actor.Account, newPassword);
        await IdentityTransactions.RevokeAccountAsync(db, actor.Account, "passwordChanged", context.TraceIdentifier,
            actor.Account.MaTaiKhoan, ct);
        IdentityTransactions.Audit(db, "passwordChanged", context.TraceIdentifier, actor.Account.MaTaiKhoan, actor.Account.MaTaiKhoan);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<SessionTokens> LoginAsync(string identifier, string password, string clientType, string trace, CancellationToken ct)
    {
        CheckClient(clientType);
        var normalized = IdentityRegistration.Normalize(identifier);
        var candidate = await db.Set<TaiKhoan>().AsNoTracking().SingleOrDefaultAsync(x =>
            identifier.Contains('@') ? x.NormalizedEmail == normalized : x.NormalizedUsername == normalized, ct);
        if (candidate is null) throw new IdentityFailure(401, "invalidCredentials");
        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        var account = await IdentityTransactions.LockAccountAsync(db, candidate.MaTaiKhoan, ct);
        if (account is null || !await AccountActiveAsync(account, ct) || !PasswordMatches(account, password)) {
            IdentityTransactions.Audit(db, "loginFailed", trace, outcome: "Denied");
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            throw new IdentityFailure(401, "invalidCredentials");
        }
        if (account.AccountType == "Customer" && account.EmailVerifiedAt is null)
            throw new IdentityFailure(403, "emailVerificationRequired");
        var now = DateTimeOffset.UtcNow;
        var csrf = clientType == "web" ? IdentityTokens.RandomToken() : null;
        var session = new IdentitySession { Id = Guid.NewGuid(), AccountId = account.MaTaiKhoan,
            SecurityVersion = account.SecurityVersion, ClientType = clientType, CreatedAt = now,
            ExpiresAt = now.AddDays(7), CsrfSecretHash = csrf is null ? null : IdentityTokens.Hash(csrf) };
        var refresh = IdentityTokens.RandomToken();
        db.Add(session); db.Add(Credential(session, refresh, now));
        IdentityTransactions.Audit(db, "loginSucceeded", trace, account.MaTaiKhoan, account.MaTaiKhoan, session.Id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new(tokens.Issue(account.MaTaiKhoan, session.Id, now), refresh, session.Id, csrf);
    }

    public async Task<SessionTokens> RefreshAsync(string refresh, string clientType, string? csrf, string trace, CancellationToken ct)
    {
        CheckClient(clientType);
        var candidate = await FindAsync(refresh, ct) ?? throw new IdentityFailure(401, "invalidSession");
        var accountId = await db.Set<IdentitySession>().Where(x => x.Id == candidate.SessionId).Select(x => x.AccountId).SingleAsync(ct);
        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        var account = await IdentityTransactions.LockAccountAsync(db, accountId, ct);
        var session = await IdentityTransactions.LockSessionAsync(db, candidate.SessionId, ct);
        var now = DateTimeOffset.UtcNow;
        if (account is null || session is null || !await AccountActiveAsync(account, ct) ||
            account.AccountType == "Customer" && account.EmailVerifiedAt is null || session.RevokedAt is not null ||
            session.ExpiresAt <= now || session.SecurityVersion != account.SecurityVersion)
            throw new IdentityFailure(401, "invalidSession");
        CheckTransport(session, clientType, csrf);
        var credential = await db.Set<RefreshCredential>().SingleAsync(x => x.Id == candidate.Id, ct);
        if (credential.ConsumedAt is { } consumed) {
            if (now - consumed <= TimeSpan.FromSeconds(5)) throw new IdentityFailure(409, "refreshInProgress");
            await IdentityTransactions.RevokeSessionAsync(db, session, "refreshReplay", trace, accountId, ct);
            IdentityTransactions.Audit(db, "refreshReplay", trace, accountId, accountId, session.Id, "Denied");
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            throw new IdentityFailure(401, "refreshReplay");
        }
        if (credential.RevokedAt is not null || credential.ExpiresAt <= now) throw new IdentityFailure(401, "invalidSession");
        credential.ConsumedAt = now;
        await db.SaveChangesAsync(ct); // free filtered unique slot before inserting successor
        var next = IdentityTokens.RandomToken();
        var successor = Credential(session, next, now);
        db.Add(successor);
        await db.SaveChangesAsync(ct);
        credential.ReplacedById = successor.Id;
        var nextCsrf = clientType == "web" ? IdentityTokens.RandomToken() : null;
        session.CsrfSecretHash = nextCsrf is null ? null : IdentityTokens.Hash(nextCsrf);
        IdentityTransactions.Audit(db, "refreshRotated", trace, accountId, accountId, session.Id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new(tokens.Issue(accountId, session.Id, now), next, session.Id, nextCsrf);
    }

    public async Task LogoutAsync(string refresh, string clientType, string? csrf, string trace, CancellationToken ct)
    {
        CheckClient(clientType);
        var candidate = await FindAsync(refresh, ct);
        if (candidate is null) return;
        var accountId = await db.Set<IdentitySession>().Where(x => x.Id == candidate.SessionId).Select(x => x.AccountId).SingleAsync(ct);
        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        await IdentityTransactions.LockAccountAsync(db, accountId, ct);
        var session = await IdentityTransactions.LockSessionAsync(db, candidate.SessionId, ct);
        if (session is null) return;
        CheckTransport(session, clientType, csrf);
        await IdentityTransactions.RevokeSessionAsync(db, session, "logout", trace, accountId, ct);
        IdentityTransactions.Audit(db, "logout", trace, accountId, accountId, session.Id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }

    private Task<RefreshCredential?> FindAsync(string token, CancellationToken ct)
    {
        if (token.Length > 256) return Task.FromResult<RefreshCredential?>(null);
        var hash = IdentityTokens.Hash(token);
        return db.Set<RefreshCredential>().AsNoTracking().SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
    }

    private static RefreshCredential Credential(IdentitySession s, string token, DateTimeOffset now) => new() {
        Id = Guid.NewGuid(), SessionId = s.Id, TokenHash = IdentityTokens.Hash(token), CreatedAt = now, ExpiresAt = s.ExpiresAt
    };
    private static void CheckClient(string type) { if (type is not ("web" or "native")) throw new IdentityFailure(400, "validation"); }
    private static bool PasswordMatches(TaiKhoan account, string password)
    {
        try { return Hasher.VerifyHashedPassword(account, account.MatKhauHash, password) != PasswordVerificationResult.Failed; }
        catch (FormatException) { return false; }
    }
    private async Task<bool> AccountActiveAsync(TaiKhoan a, CancellationToken ct) => a.TrangThai == "Active" &&
        (a.AccountType == "Customer" || a.AccountType == "Employee" &&
         await db.Set<NhanVien>().AnyAsync(x => x.MaNhanVien == a.MaNhanVien && x.TrangThai == "Active", ct));
    private static void CheckTransport(IdentitySession s, string type, string? csrf)
    {
        if (s.ClientType != type) throw new IdentityFailure(400, "validation");
        if (type == "web" && (csrf is null || s.CsrfSecretHash is null ||
            !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(IdentityTokens.Hash(csrf), s.CsrfSecretHash)))
            throw new IdentityFailure(403, "csrfFailed");
    }
}
