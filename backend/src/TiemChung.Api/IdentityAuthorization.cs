using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public sealed record IdentityActor(TaiKhoan Account, IdentitySession Session, NhanVien? Employee, string[] Permissions)
{
    public bool IsCustomer => Account.AccountType == "Customer";
    public bool HasPermission(string code) => !IsCustomer && Permissions.Contains(code);
    public void RequirePermission(string code) { if (!HasPermission(code)) throw new IdentityFailure(403, "permissionDenied"); }
    public bool CanReadProfiles => HasPermission("XemHoSo");
    public bool CanCreateProfiles => HasPermission(EmployeeAuthorization.ProfilesWrite);
    public bool CanAdminister => HasPermission("QuanLyTaiKhoan");
}

public sealed class IdentityFailure(int status, string code) : Exception(code)
{
    public Dictionary<string,string[]>? Errors { get; init; }
    public int Status { get; } = status;
    public string Code { get; } = code;
}

public sealed class IdentityAuthorization(TiemChungDbContext db, IdentityTokens tokens)
{
    public async Task<IdentityActor> RequireAsync(HttpContext context, bool forWrite, CancellationToken ct)
    {
        var header = context.Request.Headers.Authorization.ToString();
        var identity = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? tokens.Validate(header[7..], DateTimeOffset.UtcNow) : null;
        if (identity is null) throw new IdentityFailure(401, "invalidSession");
        var (accountId, sid) = identity.Value;
        TaiKhoan? account;
        IdentitySession? session;
        if (forWrite) {
            account = await IdentityTransactions.LockAccountAsync(db, accountId, ct);
            session = await IdentityTransactions.LockSessionAsync(db, sid, ct);
        } else {
            account = await db.Set<TaiKhoan>().AsNoTracking().SingleOrDefaultAsync(x => x.MaTaiKhoan == accountId, ct);
            session = await db.Set<IdentitySession>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == sid, ct);
        }
        if (account is null || session is null || session.AccountId != accountId || account.TrangThai != "Active"
            || session.RevokedAt is not null || session.ExpiresAt <= DateTimeOffset.UtcNow
            || session.SecurityVersion != account.SecurityVersion)
            throw new IdentityFailure(401, "invalidSession");
        if (account.AccountType == "Customer") {
            if (account.EmailVerifiedAt is null) throw new IdentityFailure(403, "emailVerificationRequired");
            return new(account, session, null, []);
        }
        var employee = await db.Set<NhanVien>().AsNoTracking().SingleOrDefaultAsync(x => x.MaNhanVien == account.MaNhanVien, ct);
        if (employee?.TrangThai != "Active" || !EmployeeAuthorization.IsRole(employee.ChucDanh))
            throw new IdentityFailure(401, "invalidSession");
        var permissions = EmployeeAuthorization.PermissionsFor(employee.ChucDanh, account.AdministrativeDuty);
        return new(account, session, employee, permissions);
    }
}
