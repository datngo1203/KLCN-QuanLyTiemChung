using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

using TiemChung.Api;

namespace TiemChung.IdentityOperator;

public sealed record RecoveryManifest(Guid OperationId,Guid RecoveryDatabaseId,string M1UpgradeEpoch,string MigrationId,
    int TargetAccountId,int EmployeeId,string ExpectedRowVersion,DateTimeOffset IssuedAt,DateTimeOffset ExpiresAt,string AssignedDuty);

// No HTTP registration. The executable verifies the actual SQL operator principal before invoking.
public sealed class IdentityRecovery(TiemChungDbContext db)
{
    private const string Migration="20261023073816_M1Identity";
    private static readonly PasswordHasher<TaiKhoan> Hasher=new();

    public async Task<string> RecoverAsync(RecoveryManifest manifest,string authenticatedOperator,string password,CancellationToken ct)
    {
        authenticatedOperator=await SqlOperatorBoundary.IdentityAsync(db,ct);
        IdentityAdministration.ValidatePassword(password);
        var fingerprint=SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(manifest));
        await using var tx=await IdentityTransactions.BeginAsync(db,ct);
        await IdentityTransactions.AdministrationLockAsync(db,ct);
        var metadata=await db.Set<IdentityRecoveryDatabase>().SingleAsync(ct);
        if(metadata.RecoveryDatabaseId!=manifest.RecoveryDatabaseId || metadata.M1UpgradeEpoch!=manifest.M1UpgradeEpoch ||
            manifest.MigrationId!=Migration || !(await db.Database.GetAppliedMigrationsAsync(ct)).Contains(Migration))
            throw new IdentityFailure(400,"validation");
        var existing=await db.Set<IdentityRecoveryOperation>().SingleOrDefaultAsync(x=>x.RecoveryDatabaseId==metadata.RecoveryDatabaseId && x.M1UpgradeEpoch==metadata.M1UpgradeEpoch,ct);
        if(existing is not null) {
            if(existing.OperationId==manifest.OperationId && CryptographicOperations.FixedTimeEquals(existing.ManifestFingerprint,fingerprint))
                return existing.ResultCode;
            throw new IdentityFailure(409,"concurrencyConflict");
        }
        var now=DateTimeOffset.UtcNow;
        if(manifest.OperationId==Guid.Empty || manifest.AssignedDuty!="Technical" || manifest.IssuedAt>now ||
            manifest.ExpiresAt<=now || manifest.ExpiresAt-manifest.IssuedAt>TimeSpan.FromMinutes(30))
            throw new IdentityFailure(400,"validation");
        if(await EffectiveAdminExistsAsync(ct)) throw new IdentityFailure(409,"concurrencyConflict");
        var account=await IdentityTransactions.LockAccountAsync(db,manifest.TargetAccountId,ct);
        byte[] expected;
        try { expected=Convert.FromBase64String(manifest.ExpectedRowVersion); }
        catch(FormatException) { throw new IdentityFailure(400,"validation"); }
        if(account is null || account.AccountType!="Employee" || account.MaNhanVien!=manifest.EmployeeId ||
            account.AdministrativeDuty=="Director" || !CryptographicOperations.FixedTimeEquals(account.RowVersion,expected) ||
            !await db.Set<NhanVien>().AnyAsync(x=>x.MaNhanVien==manifest.EmployeeId && x.ChucDanh=="Admin" && x.TrangThai=="Active",ct))
            throw new IdentityFailure(409,"concurrencyConflict");
        account.AdministrativeDuty="Technical"; account.TrangThai="Active";
        // Operator supplies a new secret securely; never accept an unrecognized legacy hash.
        account.MatKhauHash=Hasher.HashPassword(account,password);
        await GrantTechnicalAsync(account,ct);
        var trace=manifest.OperationId.ToString("N");
        await IdentityTransactions.RevokeAccountAsync(db,account,"recovery",trace,null,ct);
        db.Add(new IdentityRecoveryOperation { OperationId=manifest.OperationId,RecoveryDatabaseId=metadata.RecoveryDatabaseId,
            M1UpgradeEpoch=metadata.M1UpgradeEpoch,ManifestFingerprint=fingerprint,TargetAccountId=account.MaTaiKhoan,
            AppliedAt=now,OperatorIdentity=authenticatedOperator,ResultCode="recovered" });
        var audit=IdentityTransactions.Audit(db,"adminRecovered",trace,subject:account.MaTaiKhoan);
        audit.OperationId=manifest.OperationId; audit.OperatorIdentity=authenticatedOperator;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return "recovered";
    }

    public async Task<int> BootstrapAsync(int employeeId,string username,string password,string authenticatedOperator,CancellationToken ct)
    {
        authenticatedOperator=await SqlOperatorBoundary.IdentityAsync(db,ct);
        IdentityAdministration.ValidateUsername(username); IdentityAdministration.ValidatePassword(password);
        await using var tx=await IdentityTransactions.BeginAsync(db,ct);
        await IdentityTransactions.AdministrationLockAsync(db,ct);
        if(await db.Set<TaiKhoan>().AnyAsync(ct)) throw new IdentityFailure(409,"concurrencyConflict");
        if(!await db.Set<NhanVien>().AnyAsync(x=>x.MaNhanVien==employeeId && x.ChucDanh=="Admin" && x.TrangThai=="Active",ct))
            throw new IdentityFailure(400,"validation");
        var account=new TaiKhoan { MaNhanVien=employeeId,AccountType="Employee",TenDangNhap=username.Trim(),
            NormalizedUsername=IdentityRegistration.Normalize(username),AdministrativeDuty="Technical",TrangThai="Active",CreatedAt=DateTimeOffset.UtcNow };
        account.MatKhauHash=Hasher.HashPassword(account,password); db.Add(account); await db.SaveChangesAsync(ct);
        await GrantTechnicalAsync(account,ct);
        var audit=IdentityTransactions.Audit(db,"adminBootstrapped",Guid.NewGuid().ToString("N"),subject:account.MaTaiKhoan);
        audit.OperatorIdentity=authenticatedOperator;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return account.MaTaiKhoan;
    }

    private async Task<bool> EffectiveAdminExistsAsync(CancellationToken ct) => await (
        from a in db.Set<TaiKhoan>() join n in db.Set<NhanVien>() on a.MaNhanVien equals (int?)n.MaNhanVien
        join grant in db.Set<TaiKhoan_QuyenHan>() on a.MaTaiKhoan equals grant.MaTaiKhoan
        join p in db.Set<QuyenHan>() on grant.MaQuyen equals p.MaQuyen
        where a.AccountType=="Employee" && a.TrangThai=="Active" && a.AdministrativeDuty!="Director" &&
            n.ChucDanh=="Admin" && n.TrangThai=="Active" && p.Code=="QuanLyTaiKhoan" select a).AnyAsync(ct);

    private async Task GrantTechnicalAsync(TaiKhoan account,CancellationToken ct)
    {
        foreach(var code in IdentityPermissions.Codes) {
            var permission=await db.Set<QuyenHan>().SingleOrDefaultAsync(x=>x.Code==code,ct);
            if(permission is null) { permission=new QuyenHan { Code=code,TenQuyen=code,MoTa=code }; db.Add(permission); await db.SaveChangesAsync(ct); }
            if(!await db.Set<TaiKhoan_QuyenHan>().AnyAsync(x=>x.MaTaiKhoan==account.MaTaiKhoan && x.MaQuyen==permission.MaQuyen,ct))
                db.Add(new TaiKhoan_QuyenHan { MaTaiKhoan=account.MaTaiKhoan,MaQuyen=permission.MaQuyen });
        }
    }
}
