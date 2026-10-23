using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TiemChung.IdentityOperator;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.BoundaryAcceptance;

// Separate suite: never provisions logins, users or grants. Requires DBA-approved DB first.
// Missing connections fail, not skip/PASS. Not silently folded into old DBA service suite.
public sealed class PrincipalTests
{
    private static TiemChungDbContext Db(string key) => new(new DbContextOptionsBuilder<TiemChungDbContext>()
        .UseSqlServer(Environment.GetEnvironmentVariable(key)??throw new InvalidOperationException("Provisioned test connection required.")).Options);

    [Fact]
    public async Task OperatorAcceptedAppDeniedByServerContext()
    {
        await using var op=Db("M1_BOUNDARY_OPERATOR_CONNECTION");
        var identity=await SqlOperatorBoundary.RequireAsync(op,default); Assert.False(string.IsNullOrWhiteSpace(identity));
        await using var app=Db("M1_BOUNDARY_APP_CONNECTION");
        await Assert.ThrowsAsync<InvalidOperationException>(()=>SqlOperatorBoundary.RequireAsync(app,default));
        Assert.NotEqual(await SqlOperatorBoundary.IdentityAsync(app,default),identity);
    }

    [Theory]
    [InlineData("INSERT dbo.TaiKhoan(TenDangNhap,NormalizedUsername,MaNhanVien,MatKhauHash,TrangThai,AccountType,AdministrativeDuty,CreatedAt,SecurityVersion) SELECT 'synthetic-denied','SYNTHETIC-DENIED',MIN(MaNhanVien),'synthetic','Active','Employee','Technical',SYSDATETIMEOFFSET(),1 FROM dbo.NhanVien WHERE MaNhanVien NOT IN(SELECT MaNhanVien FROM dbo.TaiKhoan WHERE MaNhanVien IS NOT NULL)")]
    [InlineData("UPDATE dbo.TaiKhoan SET MatKhauHash='synthetic-denied' WHERE AccountType='Employee'")]
    [InlineData("UPDATE dbo.TaiKhoan SET TrangThai='Locked' WHERE AccountType='Employee'")]
    [InlineData("UPDATE dbo.TaiKhoan SET AccountType='Customer',MaNhanVien=NULL,AdministrativeDuty='None',LoginEmail='denied@example.invalid',NormalizedEmail='DENIED@EXAMPLE.INVALID',NormalizedUsername=NULL WHERE MaTaiKhoan=(SELECT MIN(MaTaiKhoan) FROM TaiKhoan WHERE AccountType='Employee')")]
    [InlineData("UPDATE dbo.TaiKhoan SET MatKhauHash='synthetic-denied'")]
    [InlineData("MERGE dbo.TaiKhoan AS t USING (SELECT MIN(MaTaiKhoan) AS Id FROM dbo.TaiKhoan WHERE AccountType='Employee') AS s ON t.MaTaiKhoan=s.Id WHEN MATCHED THEN UPDATE SET MatKhauHash='synthetic-denied';")]
    [InlineData("UPDATE dbo.NhanVien SET TrangThai='Inactive'")]
    [InlineData("DELETE FROM dbo.TaiKhoan_QuyenHan")]
    [InlineData("DELETE FROM dbo.IdentityRecoveryOperation")]
    [InlineData("UPDATE dbo.IdentityRecoveryDatabase SET M1UpgradeEpoch='denied'")]
    [InlineData("DISABLE TRIGGER dbo.M1GuardAccount ON dbo.TaiKhoan")]
    [InlineData("INSERT dbo.SecurityAudit(OccurredAt,Action,Outcome,TraceId) VALUES(SYSDATETIMEOFFSET(),'employeePasswordReset','Success','synthetic-denied')")]
    public async Task AppCannotDirectlyMutateAdministrativeState(string sql)
    {
        await using var db=Db("M1_BOUNDARY_APP_CONNECTION");
        // Require actual Employee row so a zero-row UPDATE cannot falsely pass guard tests.
        Assert.True(await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM dbo.TaiKhoan WHERE AccountType='Employee'").SingleAsync()>0);
        Assert.True(await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM dbo.NhanVien WHERE MaNhanVien NOT IN(SELECT MaNhanVien FROM dbo.TaiKhoan WHERE MaNhanVien IS NOT NULL)").SingleAsync()>0);
        await using var tx=await db.Database.BeginTransactionAsync();
        var error=await Record.ExceptionAsync(()=>db.Database.ExecuteSqlRawAsync(sql));
        Assert.True(error is SqlException, "SQL must reject, not merely CLI.");
        var number=((SqlException)error!).Number;
        Assert.True(number is 51021 or 51022 or 229 or 1088 or 15151, "Expected guard/permission denial, not unrelated SQL syntax failure.");
        // Dispose rolls back even if a future faulty guard lets any statement proceed.
    }

    [Fact]
    public async Task OperatorCannotAlterGuardsOrRecoveryMetadata()
    {
        await using var db=Db("M1_BOUNDARY_OPERATOR_CONNECTION");
        await SqlOperatorBoundary.RequireAsync(db,default);
        foreach(var sql in new[]{"DISABLE TRIGGER dbo.M1GuardAccount ON dbo.TaiKhoan","UPDATE dbo.IdentityRecoveryDatabase SET M1UpgradeEpoch='denied'","DELETE FROM dbo.SecurityAudit"}) {
            await using var tx=await db.Database.BeginTransactionAsync();
            Assert.IsType<SqlException>(await Record.ExceptionAsync(()=>db.Database.ExecuteSqlRawAsync(sql)));
        }
    }
}
