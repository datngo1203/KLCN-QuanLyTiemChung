using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TiemChung.Api;
using TiemChung.Domain.Entities;
using TiemChung.IdentityOperator;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class IdentityAdministrationSqlTests
{
    [Fact,Trait("Category","SqlIntegration")]
    public async Task BootstrapAndAdministrationEnforceActorTargetAndRevokeInvariant()
    {
        var source=Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION")??throw new InvalidOperationException("SQL required.");
        var cs=new SqlConnectionStringBuilder(source) { InitialCatalog="KLCN_M1_Test_"+Guid.NewGuid().ToString("N") };
        await using var db=new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(cs.ConnectionString).Options);
        await db.Database.MigrateAsync();
        var adminEmployee=new NhanVien { HoTen="Synthetic Admin",CCCD="000000000001",SoDienThoai="0900000000",Email="admin@example.invalid",BoPhan="Test",ChucDanh="Admin",TrangThai="Active" };
        var staffEmployee=new NhanVien { HoTen="Synthetic Staff",CCCD="000000000002",SoDienThoai="0900000001",Email="staff@example.invalid",BoPhan="Test",ChucDanh="LeTan",TrangThai="Active" };
        db.AddRange(adminEmployee,staffEmployee); await db.SaveChangesAsync();
        var recovery=new IdentityRecovery(db);
        Assert.Equal(400,(await Assert.ThrowsAsync<IdentityFailure>(()=>recovery.BootstrapAsync(staffEmployee.MaNhanVien,"bootstrap-admin","synthetic-password-123","operator",CancellationToken.None))).Status);
        db.ChangeTracker.Clear();
        var adminId=await recovery.BootstrapAsync(adminEmployee.MaNhanVien,"bootstrap-admin","synthetic-password-123","operator",CancellationToken.None);
        Assert.Equal(12,await db.Set<TaiKhoan_QuyenHan>().CountAsync());
        db.ChangeTracker.Clear();
        Assert.Equal(409,(await Assert.ThrowsAsync<IdentityFailure>(()=>recovery.BootstrapAsync(adminEmployee.MaNhanVien,"another-admin","synthetic-password-123","operator",CancellationToken.None))).Status);
        var administration=new IdentityAdministration(db);
        db.ChangeTracker.Clear();
        Assert.Equal(403,(await Assert.ThrowsAsync<IdentityFailure>(()=>administration.ChangeAsync("bootstrap-admin","wrong-password",adminId,"set-account-status",["Locked"],CancellationToken.None))).Status);
        db.ChangeTracker.Clear();
        Assert.Equal(403,(await Assert.ThrowsAsync<IdentityFailure>(()=>administration.ChangeAsync("bootstrap-admin","synthetic-password-123",adminId,"set-account-status",["Locked"],CancellationToken.None))).Status);
        db.ChangeTracker.Clear();
        var staffId=await administration.ProvisionAsync("bootstrap-admin","synthetic-password-123",staffEmployee.MaNhanVien,"synthetic-staff","synthetic-staff-password-123",CancellationToken.None);
        var now=DateTimeOffset.UtcNow;
        var session=new IdentitySession { Id=Guid.NewGuid(),AccountId=staffId,SecurityVersion=1,CreatedAt=now,ExpiresAt=now.AddDays(7),ClientType="native" };
        db.Add(session); db.Add(new RefreshCredential { Id=Guid.NewGuid(),SessionId=session.Id,CreatedAt=now,ExpiresAt=session.ExpiresAt,TokenHash=System.Security.Cryptography.RandomNumberGenerator.GetBytes(32) });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        await administration.ChangeAsync("bootstrap-admin","synthetic-password-123",staffId,"set-account-status",["Locked"],CancellationToken.None);
        Assert.Equal(0,await db.Set<RefreshCredential>().CountAsync(x=>x.SessionId==session.Id && x.ConsumedAt==null && x.RevokedAt==null));
        Assert.NotNull((await db.Set<IdentitySession>().AsNoTracking().SingleAsync()).RevokedAt);
        db.ChangeTracker.Clear();
        var resetActor=await administration.AuthenticateResetActorAsync("bootstrap-admin","synthetic-password-123",CancellationToken.None);
        var before=await db.Set<TaiKhoan>().AsNoTracking().SingleAsync(x=>x.MaTaiKhoan==staffId);
        var expected=before.RowVersion.ToArray();
        foreach(var input in new[]{(Target:adminId,Employee:adminEmployee.MaNhanVien,Version:expected),
            (Target:int.MaxValue,Employee:staffEmployee.MaNhanVien,Version:expected),
            (Target:staffId,Employee:int.MaxValue,Version:expected),
            (Target:staffId,Employee:staffEmployee.MaNhanVien,Version:new byte[8])})
            await Assert.ThrowsAsync<IdentityFailure>(()=>administration.ResetPasswordAsync(resetActor,"bootstrap-admin","synthetic-password-123",input.Target,input.Employee,input.Version,"synthetic-reset-password-123","synthetic-reset-password-123",CancellationToken.None));
        await Assert.ThrowsAsync<IdentityFailure>(()=>administration.ResetPasswordAsync(resetActor,"bootstrap-admin","wrong",staffId,staffEmployee.MaNhanVien,expected,"synthetic-reset-password-123","synthetic-reset-password-123",CancellationToken.None));
        await Assert.ThrowsAsync<IdentityFailure>(()=>administration.ResetPasswordAsync(resetActor,"bootstrap-admin","synthetic-password-123",staffId,staffEmployee.MaNhanVien,expected,"synthetic-reset-password-123","mismatch",CancellationToken.None));
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectResetAudit ON SecurityAudit AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Action='employeePasswordReset') THROW 51009, 'Synthetic reset failure', 1; END;");
        await Assert.ThrowsAsync<DbUpdateException>(()=>administration.ResetPasswordAsync(resetActor,"bootstrap-admin","synthetic-password-123",staffId,staffEmployee.MaNhanVien,expected,"synthetic-reset-password-123","synthetic-reset-password-123",CancellationToken.None));
        db.ChangeTracker.Clear();
        var rolledBack=await db.Set<TaiKhoan>().AsNoTracking().SingleAsync(x=>x.MaTaiKhoan==staffId);
        Assert.Equal(before.MatKhauHash,rolledBack.MatKhauHash); Assert.Equal(before.SecurityVersion,rolledBack.SecurityVersion);
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER RejectResetAudit;");
        async Task<int> ResetRace()
        {
            await using var contender=new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(cs.ConnectionString).Options);
            var service=new IdentityAdministration(contender);
            try { await service.ResetPasswordAsync(resetActor,"bootstrap-admin","synthetic-password-123",staffId,staffEmployee.MaNhanVien,expected,"synthetic-reset-password-123","synthetic-reset-password-123",CancellationToken.None); return 200; }
            catch(IdentityFailure f) { return f.Status; }
        }
        Assert.Equal(new[]{200,409},(await Task.WhenAll(ResetRace(),ResetRace())).Order().ToArray());
        var reset=await db.Set<TaiKhoan>().AsNoTracking().SingleAsync(x=>x.MaTaiKhoan==staffId);
        Assert.Equal(before.SecurityVersion+1,reset.SecurityVersion); Assert.Equal("Locked",reset.TrangThai);
        Assert.Equal(before.AdministrativeDuty,reset.AdministrativeDuty); Assert.Equal(before.TenDangNhap,reset.TenDangNhap);
        var hasher=new Microsoft.AspNetCore.Identity.PasswordHasher<TaiKhoan>();
        Assert.NotEqual(Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed,hasher.VerifyHashedPassword(reset,reset.MatKhauHash,"synthetic-reset-password-123"));
        Assert.Equal(Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed,hasher.VerifyHashedPassword(reset,reset.MatKhauHash,"synthetic-staff-password-123"));
        Assert.Equal(1,await db.Set<SecurityAudit>().CountAsync(x=>x.Action=="employeePasswordReset"));
        Assert.Empty(await db.Set<IdentityRecoveryOperation>().ToListAsync());
        Assert.Equal(0,await db.Set<RefreshCredential>().CountAsync(x=>x.SessionId==session.Id && x.ConsumedAt==null && x.RevokedAt==null));
        var config=new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["Identity:SigningKey"]=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)) }).Build();
        var sessions=new IdentitySessions(db,new IdentityTokens(config));
        db.ChangeTracker.Clear();
        Assert.Equal(401,(await Assert.ThrowsAsync<IdentityFailure>(()=>sessions.LoginAsync("synthetic-staff","synthetic-reset-password-123","native","locked-reset",CancellationToken.None))).Status);
        db.ChangeTracker.Clear();
        await administration.ChangeAsync("bootstrap-admin","synthetic-password-123",staffId,"set-account-status",["Active"],CancellationToken.None);
        db.ChangeTracker.Clear();
        var loggedIn=await sessions.LoginAsync("synthetic-staff","synthetic-reset-password-123","native","reset-login",CancellationToken.None);
        Assert.NotEqual(Guid.Empty,loggedIn.SessionId);
        db.ChangeTracker.Clear();
        // Snapshot actor invalidated while target password is being prepared must fail closed.
        var staleActor=await administration.AuthenticateResetActorAsync("bootstrap-admin","synthetic-password-123",CancellationToken.None);
        await db.Set<TaiKhoan>().Where(x=>x.MaTaiKhoan==adminId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.SecurityVersion,x=>x.SecurityVersion+1));
        var freshTarget=await db.Set<TaiKhoan>().AsNoTracking().SingleAsync(x=>x.MaTaiKhoan==staffId);
        Assert.Equal(409,(await Assert.ThrowsAsync<IdentityFailure>(()=>administration.ResetPasswordAsync(staleActor,"bootstrap-admin","synthetic-password-123",staffId,staffEmployee.MaNhanVien,freshTarget.RowVersion,"synthetic-another-password","synthetic-another-password",CancellationToken.None))).Status);
        var version=(await db.Set<TaiKhoan>().AsNoTracking().SingleAsync(x=>x.MaTaiKhoan==staffId)).SecurityVersion;
        db.ChangeTracker.Clear();
        await administration.ChangeAsync("bootstrap-admin","synthetic-password-123",staffId,"set-extra-permissions",["XemHoSo"],CancellationToken.None);
        Assert.Equal(version+1,(await db.Set<TaiKhoan>().AsNoTracking().SingleAsync(x=>x.MaTaiKhoan==staffId)).SecurityVersion);
        Assert.True(await db.Set<SecurityAudit>().AnyAsync(x=>x.Action=="permissionsChanged" && x.SubjectAccountId==staffId));
        db.ChangeTracker.Clear();
        await administration.ChangeAsync("bootstrap-admin","synthetic-password-123",staffId,"set-employee-status",["Inactive"],CancellationToken.None);
        Assert.Equal("Inactive",(await db.Set<NhanVien>().AsNoTracking().SingleAsync(x=>x.MaNhanVien==staffEmployee.MaNhanVien)).TrangThai);
        Assert.True(await db.Set<SecurityAudit>().AnyAsync(x=>x.Action=="employeeStatusChanged" && x.SubjectAccountId==staffId));
        db.ChangeTracker.Clear();
        Assert.Equal(403,(await Assert.ThrowsAsync<IdentityFailure>(()=>administration.ChangeAsync("synthetic-staff","synthetic-staff-password-123",adminId,"set-account-status",["Locked"],CancellationToken.None))).Status);
        await db.Set<TaiKhoan>().Where(x=>x.MaTaiKhoan==adminId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.AdministrativeDuty,"Director"));
        db.ChangeTracker.Clear();
        Assert.Equal(403,(await Assert.ThrowsAsync<IdentityFailure>(()=>administration.ChangeAsync("bootstrap-admin","synthetic-password-123",staffId,"set-account-status",["Active"],CancellationToken.None))).Status);
    }
}
