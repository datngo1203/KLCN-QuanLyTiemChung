using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class DevelopmentCatalogSeed
{
    public static async Task RunAsync(TiemChungDbContext db, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment()) throw new InvalidOperationException("Catalog seed is Development only.");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        foreach (var suffix in new[] { "A", "B" }) {
            var injectionName = "[DEMO] Loại tiêm " + suffix;
            var injection = await db.Set<LoaiTiemChung>().SingleOrDefaultAsync(x => x.TenLoaiTiemChung == injectionName);
            if (injection is null) { injection = new() { TenLoaiTiemChung=injectionName, MoTa="Dữ liệu giả để demo S2" }; db.Add(injection); }
            var typeName = "[DEMO] Loại vắc xin " + suffix;
            var type = await db.Set<LoaiVacXin>().SingleOrDefaultAsync(x => x.TenLoaiVacXin == typeName);
            if (type is null) { type = new() { TenLoaiVacXin=typeName, MoTa="Dữ liệu giả để demo S2" }; db.Add(type); }
            await db.SaveChangesAsync();
            var name = "[DEMO] Vắc xin " + suffix;
            if (!await db.Vaccines.AnyAsync(x => x.TenVacXin == name)) {
                db.Add(new VacXin { TenVacXin=name, MaLoaiVacXin=type.MaLoaiVacXin, MaLoaiTiemChung=injection.MaLoaiTiemChung,
                    NhaSanXuat="Nhà sản xuất demo", XuatXu="Xuất xứ demo", Gia=suffix == "A" ? 150000 : 250000, TrangThai="Active" });
                await db.SaveChangesAsync();
            }
        }
        if (!await db.Set<GoiVacXin>().AnyAsync(x => x.TenGoi == "[DEMO] Gói A + B")) {
            var vaccines = await db.Vaccines.Where(x => x.TenVacXin == "[DEMO] Vắc xin A" || x.TenVacXin == "[DEMO] Vắc xin B").ToArrayAsync();
            var package = new GoiVacXin { TenGoi="[DEMO] Gói A + B", GiaGoi=350000, TrangThai="Active" };
            db.Add(package); await db.SaveChangesAsync();
            db.AddRange(vaccines.Select(v => new GoiVacXin_VacXin { MaGoi=package.MaGoi, MaVacXin=v.MaVacXin, SoLuong=1 }));
            await db.SaveChangesAsync();
        }
        await tx.CommitAsync();
    }
}
