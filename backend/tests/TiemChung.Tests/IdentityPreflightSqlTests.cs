using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class IdentityPreflightSqlTests
{
    [Fact,Trait("Category","SqlIntegration")]
    public async Task DuplicateLegacyIdentityStopsBeforeDdlAndPreservesRows()
    {
        var cs=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION")??throw new InvalidOperationException("SQL required.")) {
            InitialCatalog="KLCN_M1_Test_"+Guid.NewGuid().ToString("N") };
        await using var db=new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(cs.ConnectionString).Options);
        var migrations=db.Database.GetMigrations().ToArray();
        await db.GetService<IMigrator>().MigrateAsync(migrations[^2]);
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO KhachHang (HoTen,CCCD,SoDienThoai,Email) VALUES
            (N'Synthetic A','000000000001','0900000000','a@example.invalid'),
            (N'Synthetic B','000000000001','0900000001','b@example.invalid');
            """);
        await Assert.ThrowsAsync<SqlException>(()=>db.Database.MigrateAsync());
        Assert.Equal(2,await db.Customers.CountAsync());
        Assert.DoesNotContain("20261023073816_M1Identity",await db.Database.GetAppliedMigrationsAsync());
        Assert.Equal(0,await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sys.columns WHERE object_id=OBJECT_ID('TaiKhoan') AND name='AccountType'").SingleAsync());
    }
}
