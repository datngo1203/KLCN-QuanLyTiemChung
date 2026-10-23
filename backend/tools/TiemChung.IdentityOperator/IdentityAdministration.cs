using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

using TiemChung.Api;

namespace TiemChung.IdentityOperator;

// Invoked only by the separate operator executable, never mapped as HTTP endpoints.
public sealed class IdentityAdministration(TiemChungDbContext db)
{
    private static readonly PasswordHasher<TaiKhoan> Hasher=new();

    public sealed class ResetActorSnapshot
    {
        internal int Id { get; init; }
        internal byte[] Version { get; init; }=[];
        internal long SecurityVersion { get; init; }
        internal string Hash { get; init; }="";
    }
    public async Task<ResetActorSnapshot> AuthenticateResetActorAsync(string username,string password,CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        var actor=await AuthenticateAsync(username,password,ct);
        var snapshot=new ResetActorSnapshot { Id=actor.MaTaiKhoan,Version=actor.RowVersion.ToArray(),SecurityVersion=actor.SecurityVersion,Hash=actor.MatKhauHash };
        db.ChangeTracker.Clear(); return snapshot;
    }

    public async Task<byte[]> ResetPasswordAsync(ResetActorSnapshot snapshot,string actorUsername,string actorPassword,
        int targetId,int employeeId,byte[] expectedVersion,string password,string confirmation,CancellationToken ct)
    {
        ValidatePassword(password);
        if(password!=confirmation || password==actorPassword || expectedVersion.Length!=8 || targetId<=0 || employeeId<=0)
            throw new IdentityFailure(400,"validation");
        var hash=Hasher.HashPassword(new TaiKhoan(),password);
        db.ChangeTracker.Clear();
        await using var tx=await IdentityTransactions.BeginAsync(db,ct);
        await IdentityTransactions.AdministrationLockAsync(db,ct);
        var actor=await AuthenticateAsync(actorUsername,actorPassword,ct);
        if(actor.MaTaiKhoan!=snapshot.Id || actor.SecurityVersion!=snapshot.SecurityVersion || actor.MatKhauHash!=snapshot.Hash ||
            !actor.RowVersion.SequenceEqual(snapshot.Version)) throw new IdentityFailure(409,"concurrencyConflict");
        if(actor.MaTaiKhoan==targetId) throw new IdentityFailure(403,"permissionDenied");
        var target=await IdentityTransactions.LockAccountAsync(db,targetId,ct)??throw new IdentityFailure(404,"resourceNotFound");
        if(target.AccountType!="Employee" || target.MaNhanVien!=employeeId) throw new IdentityFailure(400,"validation");
        if(!target.RowVersion.SequenceEqual(expectedVersion)) throw new IdentityFailure(409,"concurrencyConflict");
        target.MatKhauHash=hash;
        var trace=Guid.NewGuid().ToString("N");
        await IdentityTransactions.RevokeAccountAsync(db,target,"passwordReset",trace,actor.MaTaiKhoan,ct);
        // Also normalize credentials of already revoked families; never leave an active slot there.
        var ids=db.Set<IdentitySession>().Where(x=>x.AccountId==targetId).Select(x=>x.Id);
        await db.Set<RefreshCredential>().Where(x=>ids.Contains(x.SessionId) && x.ConsumedAt==null && x.RevokedAt==null)
            .ExecuteUpdateAsync(s=>s.SetProperty(x=>x.RevokedAt,DateTimeOffset.UtcNow),ct);
        var audit=IdentityTransactions.Audit(db,"employeePasswordReset",trace,actor.MaTaiKhoan,targetId);
        audit.ChangedFields="MatKhauHash,SecurityVersion";
        audit.OperatorIdentity=await SqlOperatorBoundary.IdentityAsync(db,ct);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return target.RowVersion.ToArray();
    }

    public async Task<int> ProvisionAsync(string actorUsername,string actorPassword,int employeeId,string username,string password,CancellationToken ct)
    {
        ValidateUsername(username); ValidatePassword(password);
        await using var tx=await IdentityTransactions.BeginAsync(db,ct);
        await IdentityTransactions.AdministrationLockAsync(db,ct);
        var actor=await AuthenticateAsync(actorUsername,actorPassword,ct);
        var employee=await db.Set<NhanVien>().SingleOrDefaultAsync(x=>x.MaNhanVien==employeeId,ct);
        if(employee is null || employee.TrangThai!="Active" || employee.ChucDanh is not ("Admin" or "LeTan" or "BacSi" or "DieuDuong" or "ThuNgan" or "ThuKho"))
            throw new IdentityFailure(400,"validation");
        var normalized=IdentityRegistration.Normalize(username);
        if(await db.Set<TaiKhoan>().AnyAsync(x=>x.MaNhanVien==employeeId || x.NormalizedUsername==normalized,ct))
            throw new IdentityFailure(409,"concurrencyConflict");
        var account=new TaiKhoan { MaNhanVien=employeeId,TenDangNhap=username.Trim(),NormalizedUsername=normalized,
            AccountType="Employee",AdministrativeDuty="None",TrangThai="Active",CreatedAt=DateTimeOffset.UtcNow };
        account.MatKhauHash=Hasher.HashPassword(account,password);
        db.Add(account); await db.SaveChangesAsync(ct);
        IdentityTransactions.Audit(db,"employeeProvisioned",Guid.NewGuid().ToString("N"),actor.MaTaiKhoan,account.MaTaiKhoan).OperatorIdentity=await SqlOperatorBoundary.IdentityAsync(db,ct);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return account.MaTaiKhoan;
    }

    public async Task ChangeAsync(string actorUsername,string actorPassword,int targetId,string command,string[] values,CancellationToken ct)
    {
        await using var tx=await IdentityTransactions.BeginAsync(db,ct);
        await IdentityTransactions.AdministrationLockAsync(db,ct);
        var actor=await AuthenticateAsync(actorUsername,actorPassword,ct);
        if(actor.MaTaiKhoan==targetId) throw new IdentityFailure(403,"permissionDenied");
        var target=await IdentityTransactions.LockAccountAsync(db,targetId,ct)??throw new IdentityFailure(404,"resourceNotFound");
        var trace=Guid.NewGuid().ToString("N");
        string action;
        switch(command) {
            case "set-account-status":
                if(values.Length!=1 || values[0] is not ("Active" or "Locked")) throw new IdentityFailure(400,"validation");
                target.TrangThai=values[0]; action="accountStatusChanged"; break;
            case "set-employee-status":
                if(target.MaNhanVien is null || values.Length!=1 || values[0] is not ("Active" or "Inactive")) throw new IdentityFailure(400,"validation");
                var employee=await db.Set<NhanVien>().SingleAsync(x=>x.MaNhanVien==target.MaNhanVien,ct);
                employee.TrangThai=values[0]; action="employeeStatusChanged"; break;
            case "set-extra-permissions":
                if(values.Distinct().Count()!=values.Length || values.Any(x=>!IdentityPermissions.Codes.Contains(x)) ||
                    target.AccountType!="Employee" || target.AdministrativeDuty=="Director" && values.Contains("QuanLyTaiKhoan"))
                    throw new IdentityFailure(400,"validation");
                await db.Set<TaiKhoan_QuyenHan>().Where(x=>x.MaTaiKhoan==targetId).ExecuteDeleteAsync(ct);
                foreach(var code in values) {
                    var permission=await db.Set<QuyenHan>().SingleAsync(x=>x.Code==code,ct);
                    db.Add(new TaiKhoan_QuyenHan { MaTaiKhoan=targetId,MaQuyen=permission.MaQuyen });
                }
                action="permissionsChanged"; break;
            default: throw new IdentityFailure(400,"validation");
        }
        await IdentityTransactions.RevokeAccountAsync(db,target,command,trace,actor.MaTaiKhoan,ct);
        IdentityTransactions.Audit(db,action,trace,actor.MaTaiKhoan,targetId).OperatorIdentity=await SqlOperatorBoundary.IdentityAsync(db,ct);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }

    private async Task<TaiKhoan> AuthenticateAsync(string username,string password,CancellationToken ct)
    {
        var normalized=IdentityRegistration.Normalize(username);
        var candidate=await db.Set<TaiKhoan>().SingleOrDefaultAsync(x=>x.NormalizedUsername==normalized,ct);
        if(candidate is null) throw new IdentityFailure(401,"invalidCredentials");
        var account=await IdentityTransactions.LockAccountAsync(db,candidate.MaTaiKhoan,ct);
        var valid=false;
        try { valid=account is not null && Hasher.VerifyHashedPassword(account,account.MatKhauHash,password)!=PasswordVerificationResult.Failed; }
        catch(FormatException) { }
        if(!valid || account!.AccountType!="Employee" || account.TrangThai!="Active" || account.AdministrativeDuty=="Director" ||
            !await db.Set<NhanVien>().AnyAsync(x=>x.MaNhanVien==account.MaNhanVien && x.TrangThai=="Active" && x.ChucDanh=="Admin",ct) ||
            !await (from grant in db.Set<TaiKhoan_QuyenHan>() join permission in db.Set<QuyenHan>() on grant.MaQuyen equals permission.MaQuyen
                where grant.MaTaiKhoan==account.MaTaiKhoan && permission.Code=="QuanLyTaiKhoan" select grant).AnyAsync(ct))
            throw new IdentityFailure(403,"permissionDenied");
        return account;
    }

    internal static void ValidateUsername(string value)
    {
        var v=value.Trim();
        if(v.Length is <4 or >100 || v.Any(c=>!char.IsAsciiLetterOrDigit(c) && c is not ('.' or '_' or '-')))
            throw new IdentityFailure(400,"validation");
    }
    internal static void ValidatePassword(string value) { if(value.Length is <12 or >128) throw new IdentityFailure(400,"validation"); }
}
