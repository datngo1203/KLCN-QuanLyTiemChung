using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class DevelopmentCatalogSeed
{
    // Curated snapshot, not a live price feed. Sources checked 2026-11-05; no runtime fetch.
    private static readonly DateTimeOffset CheckedAt = new(2026, 11, 5, 0, 0, 0, TimeSpan.FromHours(7));
    private const string PriceSource = "https://vnvc.vn/gia-tiem-chung-vac-xin/";
    private const string AvaximSource = "https://vnvc.vn/avaxim-vac-xin-phong-viem-gan/";
    private const string HavaxSource = "https://vnvc.vn/vac-xin-havax/";
    private const string AvaximInsert = "https://www.sanofi.com/assets/countries/india/docs/science-and-innovation/for-healthcare-professionals/product-information/vaccines/avaxim-80-u-api.pdf";
    // Havax 0.5 ml dose/age source (p. 7):
    // https://vnvc.vn/wp-content/uploads/2017/12/VNVC_SOTIEMCHUNG_TODINHKEM_watermark.pdf
    private const string DoseSource = "https://vnvc.vn/wp-content/uploads/2017/12/VNVC_SOTIEMCHUNG_TODINHKEM_watermark.pdf";

    public static async Task RunAsync(TiemChungDbContext db, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment()) throw new InvalidOperationException("Catalog seed is Development only.");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @r int;
            EXEC @r=sys.sp_getapplock @Resource=N'KLCN.DevelopmentCatalogSeed',
                @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
            IF @r<0 THROW 51001, 'Catalog seed lock unavailable; retry the command.', 1;
            """);

        var disease = await db.Set<Benh>().SingleOrDefaultAsync(x => x.Code == "BENH-VIEM-GAN-A");
        if (disease is null) {
            disease = new() { Code = "BENH-VIEM-GAN-A", TenBenh = "Viêm gan A", MoTa = "Bệnh do virus viêm gan A gây ra." };
            db.Add(disease); await db.SaveChangesAsync();
        }
        var technology = await db.Set<CongNgheVacXin>().SingleOrDefaultAsync(x => x.Code == "CNVX-BAT-HOAT");
        if (technology is null) {
            technology = new() { Code = "CNVX-BAT-HOAT", TenCongNghe = "Bất hoạt",
                MoTa = "Vắc xin chứa virus đã được bất hoạt.", NguonThongTin = "Sanofi — AVAXIM 80 U, thông tin sản phẩm",
                DuongDanNguon = AvaximInsert, NgayCapNhatThongTin = CheckedAt };
            db.Add(technology); await db.SaveChangesAsync();
        }

        VacXin[] vaccines = [
            new() { Code = "VX-AVAXIM-80U", TenVacXin = "Avaxim 80U", NhaSanXuat = "Sanofi Pasteur", XuatXu = "Pháp",
                MoTa = "Vắc xin phòng viêm gan A.", DoiTuongSuDung = "Trẻ từ 12 tháng đến dưới 16 tuổi.",
                DuongDung = "Intramuscular", LieuLuong = "80 U / 0,5 ml mỗi liều.", TheTich = 0.5m,
                PhanUngSauTiem = "Có thể đau, đỏ, sưng tại chỗ tiêm; sốt nhẹ, đau đầu, đau cơ, mệt mỏi.",
                Gia = 685000, TrangThai = "Active", DuongDanNguon = AvaximSource,
                NguonThongTin = $"VNVC; liều/công nghệ: Sanofi AVAXIM 80 U; giá tiêm lẻ: {PriceSource} — kiểm tra 05/11/2026, không tự cập nhật.",
                NgayCapNhatThongTin = CheckedAt },
            new() { Code = "VX-HAVAX-05", TenVacXin = "Havax 0,5 ml", NhaSanXuat = "Vabiotech", XuatXu = "Việt Nam",
                MoTa = "Vắc xin phòng viêm gan A.", DoiTuongSuDung = "Trẻ từ 24 tháng đến dưới 18 tuổi.",
                DuongDung = "Intramuscular", LieuLuong = "0,5 ml mỗi liều.", TheTich = 0.5m,
                PhanUngSauTiem = "Có thể đau, sưng, đỏ tại chỗ tiêm; ít gặp sốt, đau đầu, buồn nôn, mệt mỏi, chóng mặt.",
                Gia = 295000, TrangThai = "Active", DuongDanNguon = HavaxSource,
                NguonThongTin = $"VNVC; liều: {DoseSource}; giá tiêm lẻ: {PriceSource}; kiểm tra 05/11/2026.",
                NgayCapNhatThongTin = CheckedAt }
        ];
        foreach (var vaccine in vaccines) {
            // Existing codes, edits and historical references are never rewritten by a rerun.
            if (await db.Vaccines.AnyAsync(x => x.Code == vaccine.Code)) continue;
            db.Add(vaccine); await db.SaveChangesAsync();
            db.Add(new VacXin_Benh { MaVacXin = vaccine.MaVacXin, MaBenh = disease.MaBenh });
            // Havax technology stays unlinked: selected sources do not establish it.
            if (vaccine.Code == "VX-AVAXIM-80U")
                db.Add(new VacXin_CongNghe { MaVacXin = vaccine.MaVacXin, MaCongNghe = technology.MaCongNghe });
            var regimen = new VaccineRegimen { VaccineId = vaccine.MaVacXin, Name = "Lịch tiêm tham khảo 2 mũi",
                TargetAudience = vaccine.DoiTuongSuDung!, Summary = "Tham khảo thông tin VNVC; lịch cá nhân cần nhân viên y tế xác nhận.",
                SourceName = "VNVC — kiểm tra 05/11/2026", SourceUrl = vaccine.DuongDanNguon };
            db.Add(regimen); await db.SaveChangesAsync();
            // Calendar months must not be guessed as a fixed number of days.
            db.AddRange(new VaccineRegimenDose { RegimenId = regimen.Id, DoseOrder = 1, TimingLabel = "Lần tiêm đầu tiên" },
                new VaccineRegimenDose { RegimenId = regimen.Id, DoseOrder = 2, TimingLabel = "6 tháng sau mũi 1",
                    AnchorDoseOrder = 1, IntervalValue = 6, IntervalUnit = "Months" });
            await db.SaveChangesAsync();
        }
        // No package: synthetic compositions/prices must not be advertised as real products.
        await tx.CommitAsync();
    }
}
