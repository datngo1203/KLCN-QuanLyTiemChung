using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class IdentityBackupSqlTests
{
    [Fact,Trait("Category","SqlIntegration")]
    public async Task BackupRestorePreservesDatabaseIdentityAndRecoveryLedger()
    {
        var source=Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION")??throw new InvalidOperationException("SQL required.");
        var cs=new SqlConnectionStringBuilder(source) { InitialCatalog="KLCN_M1_Test_"+Guid.NewGuid().ToString("N") };
        Guid databaseId;
        await using(var db=new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(cs.ConnectionString).Options)) {
            await db.Database.MigrateAsync();
            databaseId=(await db.Set<IdentityRecoveryDatabase>().SingleAsync()).RecoveryDatabaseId;
            var account=new TaiKhoan { AccountType="Customer",LoginEmail="backup@example.invalid",NormalizedEmail="BACKUP@EXAMPLE.INVALID",
                TenDangNhap="synthetic-backup",MatKhauHash="synthetic",TrangThai="Active",CreatedAt=DateTimeOffset.UtcNow };
            db.Add(account); await db.SaveChangesAsync();
            db.Add(new IdentityRecoveryOperation { OperationId=Guid.NewGuid(),RecoveryDatabaseId=databaseId,M1UpgradeEpoch="M1",
                TargetAccountId=account.MaTaiKhoan,ManifestFingerprint=new byte[32],AppliedAt=DateTimeOffset.UtcNow,OperatorIdentity="synthetic",ResultCode="synthetic-ledger" });
            await db.SaveChangesAsync();
        }
        var master=new SqlConnectionStringBuilder(source) { InitialCatalog="master" };
        await using var connection=new SqlConnection(master.ConnectionString); await connection.OpenAsync();
        string directory;
        await using(var get=new SqlCommand("SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))",connection))
            directory=await get.ExecuteScalarAsync() as string ?? "";
        if(directory.Length==0) {
            await using var physical=new SqlCommand("SELECT physical_name FROM sys.master_files WHERE database_id=DB_ID(@db) AND file_id=1",connection);
            physical.Parameters.AddWithValue("@db",cs.InitialCatalog);
            directory=Path.GetDirectoryName((string)(await physical.ExecuteScalarAsync())!)!;
        }
        var separator=directory.Contains('/')?"/":"\\";
        var path=directory.TrimEnd('/','\\')+separator+cs.InitialCatalog+".bak";
        var restored=cs.InitialCatalog+"_Restored";
        await using(var backup=new SqlCommand($"BACKUP DATABASE [{cs.InitialCatalog}] TO DISK=@path WITH COPY_ONLY, INIT",connection)) {
            backup.Parameters.AddWithValue("@path",path); await backup.ExecuteNonQueryAsync();
        }
        var files=new List<(string Name,string Type)>();
        await using(var list=new SqlCommand("RESTORE FILELISTONLY FROM DISK=@path",connection)) {
            list.Parameters.AddWithValue("@path",path);
            await using var reader=await list.ExecuteReaderAsync();
            while(await reader.ReadAsync()) files.Add((reader.GetString(0),reader.GetString(2)));
        }
        var moves=string.Join(",",files.Select((f,i)=>$"MOVE N'{f.Name.Replace("'","''")}' TO N'{(directory.TrimEnd('/','\\')+separator+restored+i+(f.Type=="L"?".ldf":".mdf")).Replace("'","''")}'"));
        await using(var restore=new SqlCommand($"RESTORE DATABASE [{restored}] FROM DISK=@path WITH {moves}",connection)) {
            restore.Parameters.AddWithValue("@path",path); await restore.ExecuteNonQueryAsync();
        }
        cs.InitialCatalog=restored;
        await using var result=new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(cs.ConnectionString).Options);
        Assert.Equal(databaseId,(await result.Set<IdentityRecoveryDatabase>().SingleAsync()).RecoveryDatabaseId);
        Assert.Equal("synthetic-ledger",(await result.Set<IdentityRecoveryOperation>().SingleAsync()).ResultCode);
        Assert.Contains("20261023073816_M1Identity",await result.Database.GetAppliedMigrationsAsync());
        // All names are generated synthetic targets. Backups/DBs retained for restricted diagnostics.
    }
}
