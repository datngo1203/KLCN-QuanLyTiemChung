using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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
        var migrations=db.Database.GetMigrations().ToArray();
        await db.GetService<IMigrator>().MigrateAsync(migrations[^2]);
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
        await db.Set<TaiKhoan>().ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TrangThai,"Locked"));
        db.ChangeTracker.Clear();
        var again=manifest with { OperationId=Guid.NewGuid() };
        Assert.Equal(409,(await Assert.ThrowsAsync<IdentityFailure>(()=>recovery.RecoverAsync(again,"synthetic-operator","synthetic-new-password-123",CancellationToken.None))).Status);
        Assert.Single(await db.Set<IdentityRecoveryOperation>().ToListAsync());
    }
}
