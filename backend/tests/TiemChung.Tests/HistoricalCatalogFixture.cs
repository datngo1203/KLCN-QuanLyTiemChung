using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Tests;

// Historical Up/Down tests must start before the irreversible RF02B15 retirement.
internal static class HistoricalCatalogFixture
{
    internal const string Latest = "20261103192651_RF02AActivePackagePreflight";

    internal static Task ConfigureAsync(TiemChungDbContext db) => db.Database.ExecuteSqlRawAsync("""
        -- Historical suites keep the catalog migration pinned but write customers with the current EF model.
        ALTER TABLE KhachHang ADD DanhXung nvarchar(10) NULL, NgaySinh date NULL;
        INSERT LoaiTiemChung(Code,TenLoaiTiemChung,MoTa) VALUES ('LTC-SYNTHETIC-HISTORY','[DEMO] Historical classification','Synthetic fixture only');
        DECLARE @injection int=SCOPE_IDENTITY();
        INSERT LoaiVacXin(Code,TenLoaiVacXin,MoTa) VALUES ('LVX-SYNTHETIC-HISTORY','[DEMO] Historical classification','Synthetic fixture only');
        DECLARE @type int=SCOPE_IDENTITY();
        EXEC('ALTER TABLE VacXin ADD CONSTRAINT DF_HistoryInjection DEFAULT '+@injection+' FOR MaLoaiTiemChung');
        EXEC('ALTER TABLE VacXin ADD CONSTRAINT DF_HistoryType DEFAULT '+@type+' FOR MaLoaiVacXin');
        """);

    internal static async Task SeedAsync(TiemChungDbContext db)
    {
        foreach (var suffix in new[] { "A", "B" })
            db.Add(new VacXin { Code = "VX-DEMO-" + suffix, TenVacXin = "[DEMO] Vắc xin " + suffix,
                NhaSanXuat = "Synthetic", XuatXu = "Synthetic", Gia = suffix == "A" ? 150000 : 250000, TrangThai = "Active" });
        var package = new GoiVacXin { Code = "GOI-DEMO-A-B", TenGoi = "[DEMO] Gói A + B", GiaGoi = 350000, TrangThai = "Active" };
        db.Add(package); await db.SaveChangesAsync();
        var vaccines = await db.Vaccines.Where(v => v.TenVacXin == "[DEMO] Vắc xin A" || v.TenVacXin == "[DEMO] Vắc xin B").ToArrayAsync();
        db.AddRange(vaccines.Select(v => new GoiVacXin_VacXin { MaGoi = package.MaGoi, MaVacXin = v.MaVacXin, SoLuong = 1 }));
        await db.SaveChangesAsync();
    }
}
