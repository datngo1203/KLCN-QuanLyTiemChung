using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TiemChung.Domain.Entities;
using Xunit;

namespace TiemChung.Tests;

public sealed class VaccinationMigrationSqlTests
{
    private const string Previous = "20261031155512_S5ClinicalAssessment";

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task UpDownUpPreservesLegacyAndProtectsNewLinksWithoutBackfill()
    {
        await SqlFixtureLifecycle.RunAsync(() => AppointmentSqlTests.Fixture.Create(), async f => {
            var registration = await f.Register();
            var vaccine = registration.Items.Single(x => x.Kind == "vaccine").ServiceId;
            f.Db.Add(new LoVacXin { SoLo="[DEMO] S55 legacy", MaVacXin=vaccine,
                NgaySanXuat=new(2020,1,1), HanSuDung=new(2030,1,1), SoLuongBanDau=10, SoLuongTon=5, TrangThai="Active" });
            await f.Db.SaveChangesAsync(); f.Db.ChangeTracker.Clear();
            var migrator = f.Db.GetService<IMigrator>();
            await migrator.MigrateAsync(Previous);
            await f.Db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO MuiTiem(MaNguoiDuocTiem,SoLo,MaNhanVien,ThoiDiemTiem,ViTriTiem,TrangThai) VALUES({f.RecipientId},{"[DEMO] S55 legacy"},{f.EmployeeId},{new DateTime(2026,1,1)},{"[DEMO] preserved site"},{"Legacy"})");
            await f.Db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO BienDongKho(SoLo,MaNhanVien,LoaiBienDong,SoLuong,ThoiDiem) VALUES({"[DEMO] S55 legacy"},{f.EmployeeId},{"Legacy"},{5},{new DateTime(2026,1,1)})");
            await migrator.MigrateAsync(); await migrator.MigrateAsync();
            var legacy = await f.Db.Set<MuiTiem>().AsNoTracking().SingleAsync();
            Assert.Null(legacy.MaChiDinh); Assert.Equal("[DEMO] preserved site", legacy.ViTriTiem);
            var movement = await f.Db.Set<BienDongKho>().AsNoTracking().SingleAsync();
            Assert.Null(movement.MaMuiTiem); Assert.Equal(5, movement.SoLuong); Assert.Equal("Legacy", movement.LoaiBienDong);
            await migrator.MigrateAsync(Previous); await migrator.MigrateAsync();
            Assert.Null((await f.Db.Set<MuiTiem>().AsNoTracking().SingleAsync()).MaChiDinh);
            Assert.Null((await f.Db.Set<BienDongKho>().AsNoTracking().SingleAsync()).MaMuiTiem);

            var assessment = new PhieuKhamSangLoc { MaDangKy=registration.Id, MaNguoiDuocTiem=f.RecipientId, MaNhanVien=f.EmployeeId, KetLuan="[DEMO] schema fixture" };
            f.Db.Add(assessment); await f.Db.SaveChangesAsync();
            var indication = new ChiDinhTiem { MaPhieuKham=assessment.MaPhieuKham, MaVacXin=vaccine, KetQuaChiDinh="DuocTiem", ThoiGian=DateTime.UtcNow };
            f.Db.Add(indication); await f.Db.SaveChangesAsync();
            MuiTiem Shot() => new() { MaChiDinh=indication.MaChiDinh, MaNguoiDuocTiem=f.RecipientId, SoLo=legacy.SoLo,
                MaNhanVien=f.EmployeeId, ThoiDiemTiem=DateTime.UtcNow, ViTriTiem="[DEMO] linked", TrangThai="DaTiem" };
            var shot = Shot(); f.Db.Add(shot); await f.Db.SaveChangesAsync();
            f.Db.Add(Shot()); await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync()); f.Db.ChangeTracker.Clear();
            BienDongKho Movement() => new() { MaMuiTiem=shot.MaMuiTiem, SoLo=shot.SoLo, MaNhanVien=f.EmployeeId,
                LoaiBienDong="XuatDoTiem", SoLuong=1, ThoiDiem=shot.ThoiDiemTiem };
            var wrong = Movement(); wrong.MaNhanVien = (await f.Db.Set<NhanVien>().FirstAsync(x => x.MaNhanVien != f.EmployeeId)).MaNhanVien;
            f.Db.Add(wrong); await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync()); f.Db.ChangeTracker.Clear();
            var wrongLot = new LoVacXin { SoLo="[DEMO] S55 other", MaVacXin=vaccine, NgaySanXuat=new(2020,1,1), HanSuDung=new(2030,1,1), SoLuongBanDau=2, SoLuongTon=2, TrangThai="Active" };
            f.Db.Add(wrongLot); await f.Db.SaveChangesAsync();
            wrong=Movement(); wrong.SoLo=wrongLot.SoLo; f.Db.Add(wrong);
            await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync()); f.Db.ChangeTracker.Clear();
            wrong=Movement(); wrong.SoLuong=2; f.Db.Add(wrong);
            await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync()); f.Db.ChangeTracker.Clear();
            f.Db.Add(Movement()); await f.Db.SaveChangesAsync();
            f.Db.Add(Movement()); await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync()); f.Db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync(Previous));
            Assert.Equal(2, await f.Db.Set<MuiTiem>().CountAsync()); Assert.Equal(2, await f.Db.Set<BienDongKho>().CountAsync());
            Assert.False(f.Db.Database.HasPendingModelChanges());
        });
    }
}
