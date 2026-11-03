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
        var diseases = new List<Benh>();
        foreach (var suffix in new[] { "A", "B" }) {
            var name = "[DEMO] Bệnh mô phỏng " + suffix;
            var disease = await db.Set<Benh>().SingleOrDefaultAsync(x => x.TenBenh == name);
            if (disease == null) {
                disease = new() { Code = "BENH-DEMO-BENH-" + suffix, TenBenh = name, MoTa = "Dữ liệu mô phỏng, không phải thông tin y khoa." };
                db.Add(disease); await db.SaveChangesAsync();
            }
            diseases.Add(disease);
        }
        foreach (var suffix in new[] { "A", "B" }) {
            var injectionName = "[DEMO] Loại tiêm " + suffix;
            var injection = await db.Set<LoaiTiemChung>().SingleOrDefaultAsync(x => x.TenLoaiTiemChung == injectionName);
            if (injection == null) { injection = new() { Code = "LTC-DEMO-LOAI-TIEM-" + suffix, TenLoaiTiemChung = injectionName, MoTa = "Dữ liệu mô phỏng" }; db.Add(injection); }
            var typeName = "[DEMO] Loại vắc xin " + suffix;
            var type = await db.Set<LoaiVacXin>().SingleOrDefaultAsync(x => x.TenLoaiVacXin == typeName);
            if (type == null) { type = new() { Code = "LVX-DEMO-LOAI-VAC-XIN-" + suffix, TenLoaiVacXin = typeName, MoTa = "Dữ liệu mô phỏng" }; db.Add(type); }
            await db.SaveChangesAsync();
            var name = "[DEMO] Vắc xin " + suffix;
            if (!await db.Vaccines.AnyAsync(x => x.TenVacXin == name)) {
                var vaccine = new VacXin { Code = "VX-DEMO-VAC-XIN-" + suffix, TenVacXin = name, MaLoaiVacXin = type.MaLoaiVacXin, MaLoaiTiemChung = injection.MaLoaiTiemChung,
                    NhaSanXuat = "Nhà sản xuất demo", XuatXu = "Xuất xứ demo", Gia = suffix == "A" ? 150000 : 250000, TrangThai = "Active",
                    MoTa = "[DEMO] Sản phẩm giả lập phục vụ khóa luận, không phải dữ liệu VNVC hoặc hướng dẫn tiêm.",
                    DoiTuongSuDung = "[DEMO] Nhóm sử dụng mô phỏng", DuongDung = "Other", DuongDungKhac = "[DEMO] Đường dùng mô phỏng",
                    LieuLuong = "[DEMO] Mô tả liều giả lập", TheTich = 0.5m, NguonThongTin = "Dữ liệu mô phỏng", NgayCapNhatThongTin = DateTimeOffset.UtcNow };
                db.Add(vaccine); await db.SaveChangesAsync();
                db.AddRange(diseases.Take(suffix == "A" ? 2 : 1).Select(d => new VacXin_Benh { MaVacXin = vaccine.MaVacXin, MaBenh = d.MaBenh }));
                var regimen = new VaccineRegimen { VaccineId = vaccine.MaVacXin, Name = "[DEMO] Phác đồ tham khảo mô phỏng", TargetAudience = "[DEMO] Nhóm giả lập",
                    Summary = "[DEMO] Chỉ minh họa cấu trúc, không dùng tạo lịch hoặc chỉ định.", SourceName = "Dữ liệu mô phỏng" };
                db.Add(regimen); await db.SaveChangesAsync();
                db.AddRange(new VaccineRegimenDose { RegimenId = regimen.Id, DoseOrder = 1, TimingLabel = "[DEMO] Bước 1 mô phỏng" },
                    new VaccineRegimenDose { RegimenId = regimen.Id, DoseOrder = 2, TimingLabel = "[DEMO] Bước 2 mô phỏng", MinimumIntervalDays = 10, RecommendedIntervalDays = 20, Note = "[DEMO] Khoảng cách giả lập" });
                await db.SaveChangesAsync();
            }
        }
        if (!await db.Set<GoiVacXin>().AnyAsync(x => x.TenGoi == "[DEMO] Gói A + B")) {
            var vaccines = await db.Vaccines.Where(x => x.TenVacXin == "[DEMO] Vắc xin A" || x.TenVacXin == "[DEMO] Vắc xin B").ToArrayAsync();
            var package = new GoiVacXin { Code = "GOI-DEMO-GOI-A-B", TenGoi = "[DEMO] Gói A + B", GiaGoi = 350000, TrangThai = "Active",
                MoTa = "[DEMO] Gói mô phỏng", DoiTuongSuDung = "[DEMO] Nhóm sử dụng mô phỏng" };
            db.Add(package); await db.SaveChangesAsync();
            db.AddRange(vaccines.Select(v => new GoiVacXin_VacXin { MaGoi = package.MaGoi, MaVacXin = v.MaVacXin, SoLuong = 1 }));
            await db.SaveChangesAsync();
        }
        await tx.CommitAsync();
    }
}
