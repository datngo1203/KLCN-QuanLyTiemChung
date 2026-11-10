using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

// Explicit Development-only fixture, NOT proof that a verification email was delivered.
// Never enabled for arbitrary customer addresses or production registration.
public static class DevelopmentDemoCustomerSeed
{
    public const string Email = "demo.customer@example.invalid";

    public static async Task RunAsync(TiemChungDbContext db, IHostEnvironment environment,
        IConfiguration config, CancellationToken ct = default)
    {
        if (!environment.IsDevelopment()) throw new InvalidOperationException("Demo Customer seed is Development only.");
        var email = config["DevelopmentSeed:Username"] ?? "";
        var role = config["DevelopmentSeed:Role"] ?? "";
        var password = config["DevelopmentSeed:Password"] ?? "";
        if (email != Email || role != "Customer" || password.Length is < 12 or > 128)
            throw new InvalidOperationException("Set the fixed Development demo Customer email, role and a local password (12-128 chars).");

        var hasher = new PasswordHasher<TaiKhoan>();
        await using var tx = await IdentityTransactions.BeginAsync(db, ct);
        await IdentityTransactions.AdministrationLockAsync(db, ct);
        var normalized = IdentityRegistration.Normalize(Email);
        var existing = await db.Set<TaiKhoan>().SingleOrDefaultAsync(a => a.NormalizedEmail == normalized, ct);
        if (existing is not null) {
            // An existing account is never silently adopted or assigned a different password.
            if (existing.AccountType != "Customer" || existing.MaNhanVien is not null ||
                existing.TrangThai != "Active" || existing.LoginEmail != Email ||
                hasher.VerifyHashedPassword(existing, existing.MatKhauHash, password) == PasswordVerificationResult.Failed)
                throw new InvalidOperationException("Conflicting demo Customer exists; no account changed.");

            // A real registration for this exact synthetic address may predate fixture creation.
            // Mark it Development-verified only after proving the matching password.
            if (existing.EmailVerifiedAt is null) {
                existing.EmailVerifiedAt = DateTimeOffset.UtcNow;
                IdentityTransactions.Audit(db, "developmentDemoCustomerVerified", "development-seed", subject: existing.MaTaiKhoan);
                await db.SaveChangesAsync(ct);
            }
            await tx.CommitAsync(ct);
            return;
        }

        var account = new TaiKhoan {
            AccountType = "Customer", LoginEmail = Email, NormalizedEmail = normalized,
            TenDangNhap = "customer-" + Guid.NewGuid().ToString("N"),
            TrangThai = "Active", AdministrativeDuty = "None", CreatedAt = DateTimeOffset.UtcNow,
            EmailVerifiedAt = DateTimeOffset.UtcNow
        };
        account.MatKhauHash = hasher.HashPassword(account, password);
        db.Add(account);
        await db.SaveChangesAsync(ct);
        IdentityTransactions.Audit(db, "developmentDemoCustomerSeeded", "development-seed", subject: account.MaTaiKhoan);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
