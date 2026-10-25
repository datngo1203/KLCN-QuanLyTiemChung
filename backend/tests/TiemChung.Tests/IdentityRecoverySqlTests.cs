using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using TiemChung.Api;
using TiemChung.Domain.Entities;
using TiemChung.IdentityOperator;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class IdentityRecoverySqlTests
{
    [Fact,Trait("Category","SqlIntegration")]
    public async Task UpgradeLegacyAdminThenBindVersionAndRecoverExactlyOnce()
    {
        var source=Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION")??throw new InvalidOperationException("SQL required.");
        var cs=new SqlConnectionStringBuilder(source) { InitialCatalog="KLCN_M1_Test_"+Guid.NewGuid().ToString("N") };
        var options=new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(cs.ConnectionString).Options;
        await using var db=new TiemChungDbContext(options);
        var legacyMigration=db.Database.GetMigrations().First();
        await db.GetService<IMigrator>().MigrateAsync(legacyMigration);
        // Raw insert targets the legacy schema before RowVersion exists.
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO NhanVien (HoTen,CCCD,SoDienThoai,Email,BoPhan,ChucDanh,TrangThai)
            VALUES (N'Synthetic Admin','000000000001','0900000000','admin@example.invalid',N'Test','Admin','Active');
            INSERT INTO TaiKhoan (MaNhanVien,TenDangNhap,MatKhauHash,TrangThai)
            SELECT MaNhanVien,'old-admin','unknown-legacy-hash','Active' FROM NhanVien;
            """);
        await db.Database.MigrateAsync();
        var account=await db.Set<TaiKhoan>().AsNoTracking().SingleAsync();
        Assert.Equal("Locked",account.TrangThai); Assert.Equal("None",account.AdministrativeDuty);
        var metadata=await db.Set<IdentityRecoveryDatabase>().AsNoTracking().SingleAsync();
        var manifest=new RecoveryManifest(Guid.NewGuid(),metadata.RecoveryDatabaseId,"M1","20261023073816_M1Identity",
            account.MaTaiKhoan,account.MaNhanVien!.Value,Convert.ToBase64String(account.RowVersion),DateTimeOffset.UtcNow,DateTimeOffset.UtcNow.AddMinutes(20),"Technical");
        var recovery=new IdentityRecovery(db);
        foreach(var wrong in new[]{manifest with { RecoveryDatabaseId=Guid.NewGuid() },manifest with { M1UpgradeEpoch="M2" },
            manifest with { MigrationId="wrong" },manifest with { TargetAccountId=int.MaxValue },manifest with { EmployeeId=int.MaxValue },
            manifest with { ExpiresAt=DateTimeOffset.UtcNow.AddSeconds(-1) }}) {
            await Assert.ThrowsAsync<IdentityFailure>(()=>recovery.RecoverAsync(wrong,"synthetic-operator","synthetic-new-password-123",CancellationToken.None));
            db.ChangeTracker.Clear(); Assert.Empty(await db.Set<IdentityRecoveryOperation>().ToListAsync());
        }
        var stale=manifest with { ExpectedRowVersion=Convert.ToBase64String(new byte[8]) };
        Assert.Equal(409,(await Assert.ThrowsAsync<IdentityFailure>(()=>recovery.RecoverAsync(stale,"synthetic-operator","synthetic-new-password-123",CancellationToken.None))).Status);
        Assert.Empty(await db.Set<IdentityRecoveryOperation>().ToListAsync());
        db.ChangeTracker.Clear();
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectRecoveryAudit ON SecurityAudit AFTER INSERT AS BEGIN THROW 51009, 'Synthetic audit failure', 1; END;");
        await Assert.ThrowsAsync<DbUpdateException>(()=>recovery.RecoverAsync(manifest,"synthetic-operator","synthetic-new-password-123",CancellationToken.None));
        db.ChangeTracker.Clear();
        Assert.Empty(await db.Set<IdentityRecoveryOperation>().ToListAsync());
        Assert.Empty(await db.Set<TaiKhoan_QuyenHan>().ToListAsync());
        Assert.Equal("Locked",(await db.Set<TaiKhoan>().AsNoTracking().SingleAsync()).TrangThai);
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER RejectRecoveryAudit;");
        async Task<string> CompeteRecovery(RecoveryManifest request)
        {
            await using var connection=new TiemChungDbContext(options);
            try { return await new IdentityRecovery(connection).RecoverAsync(request,"synthetic-operator","synthetic-new-password-123",CancellationToken.None); }
            catch(IdentityFailure failure) { return failure.Code; }
        }
        var results=await Task.WhenAll(CompeteRecovery(manifest),CompeteRecovery(manifest with { OperationId=Guid.NewGuid() }));
        Assert.Single(results,x=>x=="recovered");
        Assert.Single(results,x=>x=="concurrencyConflict");
        var committed=await db.Set<IdentityRecoveryOperation>().AsNoTracking().SingleAsync();
        // The winning manifest may be either contender; bind retry to its exact fingerprint.
        if(committed.OperationId!=manifest.OperationId) {
            // Verify retry through an identical record with the winner operation ID.
            manifest=manifest with { OperationId=committed.OperationId };
        }
        var count=await db.Set<SecurityAudit>().CountAsync();
        db.ChangeTracker.Clear();
        Assert.Equal("recovered",await recovery.RecoverAsync(manifest,"synthetic-operator","synthetic-new-password-123",CancellationToken.None));
        Assert.Equal(count,await db.Set<SecurityAudit>().CountAsync());
        Assert.Single(await db.Set<IdentityRecoveryOperation>().ToListAsync());
        Assert.Equal(12,await db.Set<TaiKhoan_QuyenHan>().CountAsync());
        var legacyEmployee=new NhanVien { HoTen="Synthetic Legacy",CCCD="000000000002",SoDienThoai="0900000001",Email="legacy@example.invalid",BoPhan="Test",ChucDanh="LeTan",TrangThai="Active" };
        db.Add(legacyEmployee); await db.SaveChangesAsync();
        var legacyAccount=new TaiKhoan { AccountType="Employee",MaNhanVien=legacyEmployee.MaNhanVien,TenDangNhap="legacy-staff",NormalizedUsername="LEGACY-STAFF",
            MatKhauHash="unknown-legacy-hash",TrangThai="Active",CreatedAt=DateTimeOffset.UtcNow };
        db.Add(legacyAccount); await db.SaveChangesAsync();
        var admin=new IdentityAdministration(db);
        await Assert.ThrowsAsync<IdentityFailure>(()=>admin.AuthenticateResetActorAsync("old-admin","admin/123",CancellationToken.None));
        var actor=await admin.AuthenticateResetActorAsync("old-admin","synthetic-new-password-123",CancellationToken.None);
        var ledgerBefore=await db.Set<IdentityRecoveryOperation>().AsNoTracking().SingleAsync();
        await admin.ResetPasswordAsync(actor,"old-admin","synthetic-new-password-123",legacyAccount.MaTaiKhoan,legacyEmployee.MaNhanVien,legacyAccount.RowVersion,
            "synthetic-legacy-reset-password","synthetic-legacy-reset-password",CancellationToken.None);
        var config=new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["Identity:SigningKey"]=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)) }).Build();
        db.ChangeTracker.Clear();
        var login=await new IdentitySessions(db,new IdentityTokens(config)).LoginAsync("legacy-staff","synthetic-legacy-reset-password","native","legacy-reset-login",CancellationToken.None);
        Assert.NotEqual(Guid.Empty,login.SessionId);
        Assert.Equal(ledgerBefore.OperationId,(await db.Set<IdentityRecoveryOperation>().AsNoTracking().SingleAsync()).OperationId);
        await db.Set<TaiKhoan>().ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TrangThai,"Locked"));
        db.ChangeTracker.Clear();
        var again=manifest with { OperationId=Guid.NewGuid() };
        Assert.Equal(409,(await Assert.ThrowsAsync<IdentityFailure>(()=>recovery.RecoverAsync(again,"synthetic-operator","synthetic-new-password-123",CancellationToken.None))).Status);
        Assert.Single(await db.Set<IdentityRecoveryOperation>().ToListAsync());
    }
}
