using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class S6MigrationSqlTests
{
    private const string Previous = "20261104175527_RecipientPublicProfileCode";
    private const string Current = "20261105025904_S6PromotionIntegration";

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData(true)]
    [InlineData(false)]
    public Task PreflightRefusesUnknownHistoricalAssignmentOrFractionalVndBeforeSchemaMutation(bool assigned) =>
        WithDatabase(async db => {
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync(Previous);
            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO KhuyenMai(TenKhuyenMai,NoiDung,ThoiGianBatDau,ThoiGianKetThuc,MucUuDai)
                VALUES(N'Ưu đãi cũ',N'Dữ liệu tổng hợp',GETUTCDATE(),DATEADD(day,1,GETUTCDATE()),100000);
                """);
            if (assigned) {
                await db.Database.ExecuteSqlRawAsync("""
                    INSERT INTO KhachHang(HoTen,SoDienThoai,Email) VALUES(N'Khách thử','0900000000','legacy@example.invalid');
                    DECLARE @customer int = SCOPE_IDENTITY();
                    INSERT INTO NguoiDuocTiem(MaKhachHang,HoTen,NgaySinh,GioiTinh,QuanHeVoiKhachHang,MaHoSo)
                    VALUES(@customer,N'Người thử','20200101',N'Nữ',N'Con','VAC-000000000001');
                    DECLARE @recipient int = SCOPE_IDENTITY();
                    INSERT INTO DangKyDichVu(MaKhachHang,MaNguoiDuocTiem,NgayDangKy,TrangThai,MaKhuyenMai)
                    SELECT @customer,@recipient,GETUTCDATE(),'DaDangKy',MaKhuyenMai FROM KhuyenMai;
                    """);
            } else {
                await db.Database.ExecuteSqlRawAsync("UPDATE KhuyenMai SET MucUuDai=100000.25");
            }

            var failure = await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync(Current));
            Assert.Equal(51000, failure.Number);
            Assert.Contains(assigned ? "historical reconciliation" : "Fractional legacy", failure.Message);
            Assert.Equal(Previous, (await db.Database.GetAppliedMigrationsAsync()).Last());
            Assert.Equal(0, await db.Database.SqlQueryRaw<int>("""
                SELECT COUNT(*) AS Value FROM sys.columns
                WHERE (object_id=OBJECT_ID('DangKyDichVu') AND name IN ('TenKhuyenMaiSnapshot','MucGiamSnapshot'))
                   OR (object_id=OBJECT_ID('KhuyenMai') AND name='IsEnabled')
                """).SingleAsync());
            Assert.Equal(2, await db.Database.SqlQueryRaw<int>("""
                SELECT CAST(scale AS int) AS Value FROM sys.columns
                WHERE object_id=OBJECT_ID('KhuyenMai') AND name='MucUuDai'
                """).SingleAsync());
            Assert.Equal(assigned ? 1 : 0, await db.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*) AS Value FROM DangKyDichVu WHERE MaKhuyenMai IS NOT NULL").SingleAsync());
        });

    [Fact, Trait("Category", "SqlIntegration")]
    public Task CleanLegacyUpDownUpRepeatPreservesMoneyAndMatchesModel() =>
        WithDatabase(async db => {
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync(Previous);
            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO KhuyenMai(TenKhuyenMai,NoiDung,ThoiGianBatDau,ThoiGianKetThuc,MucUuDai)
                VALUES(N'Ưu đãi cũ chưa sử dụng',N'Dữ liệu tổng hợp',GETUTCDATE(),DATEADD(day,1,GETUTCDATE()),100000);
                """);
            await migrator.MigrateAsync(Current);
            Assert.Equal(100000m, await db.Promotions.Select(p => p.MucUuDai).SingleAsync());
            Assert.False(await db.Promotions.Select(p => p.IsEnabled).SingleAsync());
            await migrator.MigrateAsync(Previous);
            Assert.Equal(Previous, (await db.Database.GetAppliedMigrationsAsync()).Last());
            await migrator.MigrateAsync(Current);
            await migrator.MigrateAsync(Current);
            Assert.Equal(Current, (await db.Database.GetAppliedMigrationsAsync()).Last());
            Assert.Equal(100000m, await db.Promotions.Select(p => p.MucUuDai).SingleAsync());
            Assert.False(db.Database.HasPendingModelChanges());
        });

    private static Task WithDatabase(Func<TiemChungDbContext, Task> test) =>
        SqlFixtureLifecycle.RunAsync(
            () => OwnedSqlTestDatabase.CreateAsync(Environment.GetEnvironmentVariable("M1_SQL_TEST_CONNECTION")
                ?? throw new InvalidOperationException("SQL required.")),
            async owner => {
                await using var db = new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>()
                    .UseSqlServer(owner.ConnectionString).Options);
                await test(db);
            });
}
