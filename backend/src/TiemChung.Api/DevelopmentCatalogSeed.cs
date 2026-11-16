using Microsoft.EntityFrameworkCore;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Api;

public static class DevelopmentCatalogSeed
{
    // Curated final/demo snapshot. Sources checked 09/11/2026; no runtime fetch.
    private static readonly DateTimeOffset CheckedAt = new(2026, 11, 9, 0, 0, 0, TimeSpan.FromHours(7));
    private const string PriceSource = "https://vnvc.vn/gia-tiem-chung-vac-xin/";
    private const string AvaximSource = "https://vnvc.vn/avaxim-vac-xin-phong-viem-gan/";
    private const string HavaxSource = "https://vnvc.vn/vac-xin-havax/";
    private const string Gardasil9Source = "https://vnvc.vn/vac-xin-gardasil-9/";
    private const string Prevenar13Source = "https://www.ema.europa.eu/en/medicines/human/EPAR/prevenar-13";
    private const string VarivaxSource = "https://www.fda.gov/vaccines-blood-biologics/vaccines/varivax-refrigerated-and-frozen-formulations";
    private const string MmrSource = "https://www.fda.gov/vaccines-blood-biologics/vaccines/measles-mumps-and-rubella-virus-vaccine-live";
    private const string QdengaSource = "https://vnvc.vn/vac-xin-qdenga/";
    private const string InfanrixHexaSource = "https://www.ema.europa.eu/en/medicines/human/EPAR/infanrix-hexa";
    private const string HexaximSource = "https://extranet.who.int/prequal/vaccines/p/hexaxim";
    private const string RotateqSource = "https://vnvc.vn/vac-xin-rotateq/";
    private const string RotarixSource = "https://vnvc.vn/vac-xin-rotarix/";
    private const string BexseroSource = "https://vnvc.vn/vac-xin-bexsero/";
    private const string NimenrixSource = "https://vnvc.vn/vac-xin-nimenrix/";
    private const string ShingrixSource = "https://vnvc.vn/vac-xin-shingrix/";
    private const string VaxigripSource = "https://vnvc.vn/vac-xin-vaxigrip/";

    private sealed record DiseaseSeed(string Code, string Name);
    private sealed record TechnologySeed(string Code, string Name, string Description, string SourceName, string SourceUrl);
    private sealed record DoseSeed(int Order, string Label, int? AnchorOrder = null, int? IntervalValue = null, string? IntervalUnit = null);
    private sealed record RegimenSeed(string Name, string TargetAudience, string Summary, string SourceUrl, DoseSeed[] Doses);
    private sealed record VaccineSeed(
        string Code, string Name, string Manufacturer, string Origin, string Description, string TargetAudience,
        string Route, string Dosage, decimal Volume, string Reaction, decimal Price, string SourceName, string SourceUrl,
        string[] DiseaseCodes, string[] TechnologyCodes, RegimenSeed[] Regimens);

    private static readonly DiseaseSeed[] DiseaseSeeds = [
        new("BENH-VIEM-GAN-A", "Viêm gan A"),
        new("BENH-BACH-HAU", "Bạch hầu"),
        new("BENH-HO-GA", "Ho gà"),
        new("BENH-UON-VAN", "Uốn ván"),
        new("BENH-BAI-LIET", "Bại liệt"),
        new("BENH-VIEM-GAN-B", "Viêm gan B"),
        new("BENH-HIB", "Bệnh do Haemophilus influenzae type b (Hib)"),
        new("BENH-ROTAVIRUS", "Tiêu chảy cấp do Rotavirus"),
        new("BENH-PHE-CAU", "Bệnh do phế cầu"),
        new("BENH-NAO-MO-CAU-B", "Bệnh do não mô cầu nhóm B"),
        new("BENH-NAO-MO-CAU-ACWY", "Bệnh do não mô cầu nhóm A, C, Y, W-135"),
        new("BENH-SOI", "Sởi"),
        new("BENH-QUAI-BI", "Quai bị"),
        new("BENH-RUBELLA", "Rubella"),
        new("BENH-THUY-DAU", "Thủy đậu"),
        new("BENH-ZONA", "Zona thần kinh"),
        new("BENH-CUM", "Cúm mùa"),
        new("BENH-HPV", "Bệnh do HPV"),
        new("BENH-SOT-XUAT-HUYET", "Sốt xuất huyết Dengue")
    ];

    private static readonly TechnologySeed[] TechnologySeeds = [
        new("CNVX-BAT-HOAT", "Bất hoạt", "Vắc xin sử dụng tác nhân hoặc kháng nguyên đã bất hoạt.",
            "Sanofi — Avaxim/Vaxigrip", "https://www.sanofi.com/en/india/our-science/vaccine-products"),
        new("CNVX-SONG-GIAM-DOC-LUC", "Sống giảm độc lực", "Vắc xin sử dụng tác nhân sống đã được giảm độc lực.",
            "FDA/Takeda — vaccine sống giảm độc lực", QdengaSource),
        new("CNVX-TAI-TO-HOP", "Tái tổ hợp", "Vắc xin sử dụng thành phần kháng nguyên tạo bằng công nghệ tái tổ hợp.",
            "EMA — Shingrix/Bexsero", "https://www.ema.europa.eu/en/medicines/human/EPAR/shingrix"),
        new("CNVX-CONG-HOP", "Cộng hợp", "Vắc xin polysaccharide cộng hợp với protein mang.",
            "EMA — Prevenar 13", Prevenar13Source)
    ];

    private static readonly VaccineSeed[] VaccineSeeds = [
        new("VX-AVAXIM-80U", "Avaxim 80U", "Sanofi Pasteur", "Pháp",
            "Avaxim 80U giúp phòng viêm gan A, một bệnh nhiễm virus ảnh hưởng đến gan. Đây là phiên bản dành cho trẻ em, chứa virus viêm gan A chủng GBM đã được bất hoạt. Bệnh có thể lây qua thức ăn hoặc nước uống nhiễm virus và có thể gây mệt mỏi, buồn nôn hoặc vàng da.", "Trẻ từ 12 tháng đến dưới 16 tuổi.",
            "Intramuscular", "80 U / 0,5 ml mỗi liều.", 0.5m,
            "Có thể đau, đỏ, sưng tại chỗ tiêm; sốt nhẹ, đau đầu, đau cơ, mệt mỏi.",
            685000, "VNVC — thông tin sản phẩm", AvaximSource, ["BENH-VIEM-GAN-A"], ["CNVX-BAT-HOAT"],
            [new("Lịch tiêm tham khảo 2 mũi", "Trẻ từ 12 tháng đến dưới 16 tuổi.",
                "Lịch tiêm trong danh mục chỉ để tham khảo; lịch cá nhân cần được nhân viên y tế xác nhận.", AvaximSource,
                [new(1, "Lần tiêm đầu tiên"), new(2, "6 tháng sau mũi 1", 1, 6, "Months")])]),
        new("VX-HAVAX-05", "Havax 0,5 ml", "VABIOTECH", "Việt Nam",
            "Havax giúp phòng bệnh viêm gan A bằng thành phần kháng nguyên virus viêm gan A đã được bất hoạt và tinh chế. Danh mục giới thiệu phiên bản dành cho trẻ em và thanh thiếu niên của dòng vắc xin được phát triển tại Việt Nam. Phạm vi phòng bệnh của Havax là viêm gan A, khác với viêm gan B và các bệnh gan do tác nhân khác.", "Trẻ từ 24 tháng đến dưới 18 tuổi.",
            "Intramuscular", "0,5 ml mỗi liều.", 0.5m,
            "Có thể đau, sưng, đỏ tại chỗ tiêm; ít gặp sốt, đau đầu, buồn nôn, mệt mỏi, chóng mặt.",
            295000, "VNVC — thông tin sản phẩm", HavaxSource, ["BENH-VIEM-GAN-A"], [],
            [new("Lịch tiêm tham khảo 2 mũi", "Trẻ từ 24 tháng đến dưới 18 tuổi.",
                "Lịch tiêm trong danh mục chỉ để tham khảo; lịch cá nhân cần được nhân viên y tế xác nhận.", HavaxSource,
                [new(1, "Lần tiêm đầu tiên"), new(2, "6 tháng sau mũi 1", 1, 6, "Months")])]),
        new("VX-INFANRIX-HEXA", "Infanrix Hexa (6 trong 1)", "GlaxoSmithKline Biologicals", "Bỉ",
            "Infanrix Hexa phối hợp các thành phần phòng bạch hầu, uốn ván, ho gà, viêm gan B, bại liệt và bệnh do Hib trong cùng một vắc xin. Thành phần ho gà là loại vô bào, nghĩa là sử dụng những kháng nguyên chọn lọc từ vi khuẩn. Riêng thành phần Hib được đóng dưới dạng bột tách với hỗn dịch chứa các thành phần còn lại.",
            "Trẻ từ 6 tuần tuổi và trẻ nhỏ theo khuyến cáo chính thức.",
            "Intramuscular", "0,5 ml mỗi liều sau hoàn nguyên.", 0.5m, "",
            1098000, "EMA — thông tin sản phẩm", InfanrixHexaSource,
            ["BENH-BACH-HAU","BENH-HO-GA","BENH-UON-VAN","BENH-VIEM-GAN-B","BENH-BAI-LIET","BENH-HIB"], [],
            [new("Phác đồ cơ bản 3 mũi + nhắc tham khảo", "Trẻ từ 6 tuần tuổi và trẻ nhỏ.",
                "Một phác đồ tham khảo phù hợp nguồn EMA; không tự động dùng để chỉ định cá nhân.", InfanrixHexaSource,
                [new(1,"Mũi cơ bản 1"),new(2,"Ít nhất 1 tháng sau mũi 1",1,1,"Months"),
                 new(3,"Ít nhất 1 tháng sau mũi 2",2,1,"Months"),new(4,"Mũi nhắc ít nhất 6 tháng sau mũi 3",3,6,"Months")])]),
        new("VX-HEXAXIM", "Hexaxim (6 trong 1)", "Sanofi Pasteur", "Pháp",
            "Hexaxim là vắc xin phối hợp phòng bạch hầu, uốn ván, ho gà, viêm gan B, bại liệt và bệnh do Hib. Hồ sơ WHO mô tả sản phẩm ở dạng lỏng sẵn dùng, với thành phần ho gà vô bào và thành phần bại liệt bất hoạt. Hib là tên viết tắt của vi khuẩn Haemophilus influenzae týp b, có thể gây viêm màng não ở trẻ nhỏ.",
            "Trẻ từ 6 tuần đến 24 tháng tuổi theo thông tin sản phẩm.",
            "Intramuscular", "0,5 ml mỗi liều.", 0.5m, "",
            995000, "WHO — hồ sơ tiền thẩm định", HexaximSource,
            ["BENH-BACH-HAU","BENH-HO-GA","BENH-UON-VAN","BENH-VIEM-GAN-B","BENH-BAI-LIET","BENH-HIB"], [],
            [new("Phác đồ cơ bản 3 mũi + nhắc tham khảo", "Trẻ từ 6 tuần đến 24 tháng tuổi.",
                "Ba mũi cơ bản cách nhau ít nhất 4 tuần; mũi nhắc ít nhất 6 tháng sau mũi cơ bản cuối.", HexaximSource,
                [new(1,"Mũi cơ bản 1"),new(2,"Ít nhất 4 tuần sau mũi 1",1,4,"Weeks"),
                 new(3,"Ít nhất 4 tuần sau mũi 2",2,4,"Weeks"),new(4,"Mũi nhắc ít nhất 6 tháng sau mũi 3",3,6,"Months")])]),
        new("VX-ROTATEQ", "Rotateq", "Merck Sharp & Dohme (MSD)", "Mỹ",
            "Rotateq giúp phòng viêm dạ dày ruột do rotavirus ở trẻ nhỏ. Đây là vắc xin sống giảm độc lực loại ngũ giá, có năm thành phần rotavirus trong cùng một sản phẩm. Trẻ nhiễm rotavirus có thể bị nôn và tiêu chảy phân nước, đôi khi mất nước đến mức phải nhập viện.",
            "Trẻ từ 6 tuần tuổi; hoàn thành liệu trình trước 32 tuần tuổi.",
            "Oral", "2 ml mỗi liều uống.", 2.0m, "",
            665000, "VNVC — thông tin sản phẩm", RotateqSource, ["BENH-ROTAVIRUS"], ["CNVX-SONG-GIAM-DOC-LUC"],
            [new("Lịch uống tham khảo 3 liều", "Trẻ từ 6 tuần tuổi.",
                "Ba liều uống, mỗi liều cách liều trước ít nhất 4 tuần; hoàn thành trước 32 tuần tuổi.", RotateqSource,
                [new(1,"Liều 1"),new(2,"Ít nhất 4 tuần sau liều 1",1,4,"Weeks"),new(3,"Ít nhất 4 tuần sau liều 2",2,4,"Weeks")])]),
        new("VX-ROTARIX", "Rotarix", "GlaxoSmithKline Biologicals", "Bỉ",
            "Rotarix giúp phòng viêm dạ dày ruột do rotavirus ở trẻ nhỏ. Đây là vắc xin sống giảm độc lực loại đơn giá, nghĩa là thành phần vắc xin có một chủng rotavirus. Tiêu chảy cấp ở trẻ có thể do nhiều tác nhân khác nhau, trong khi phạm vi phòng bệnh của Rotarix là các trường hợp do rotavirus.",
            "Trẻ từ 6 tuần tuổi; hoàn thành liệu trình đến 24 tuần tuổi.",
            "Oral", "1,5 ml mỗi liều uống.", 1.5m, "",
            928000, "VNVC — thông tin sản phẩm", RotarixSource, ["BENH-ROTAVIRUS"], ["CNVX-SONG-GIAM-DOC-LUC"],
            [new("Lịch uống tham khảo 2 liều", "Trẻ từ 6 tuần tuổi.",
                "Hai liều uống cách nhau ít nhất 4 tuần; hoàn thành đến 24 tuần tuổi.", RotarixSource,
                [new(1,"Liều 1"),new(2,"Ít nhất 4 tuần sau liều 1",1,4,"Weeks")])]),
        new("VX-PREVENAR-13", "Prevenar 13", "Pfizer", "Bỉ",
            "Prevenar 13 giúp phòng các bệnh do 13 týp phế cầu khuẩn có trong vắc xin. Thành phần lấy từ lớp vỏ của mỗi týp được gắn với protein mang để hệ miễn dịch dễ nhận biết. Phế cầu có thể gây viêm phổi và viêm tai giữa, hoặc gây bệnh xâm lấn như nhiễm khuẩn huyết và viêm màng não.", "Trẻ từ 6 tuần tuổi và người lớn; lịch tiêm phụ thuộc độ tuổi.",
            "Intramuscular", "0,5 ml mỗi liều.", 0.5m, "",
            1190000, "EMA — thông tin sản phẩm", Prevenar13Source, ["BENH-PHE-CAU"], ["CNVX-CONG-HOP"],
            [new("Phác đồ tham khảo từ 2 tuổi trở lên", "Người từ 2 tuổi trở lên.",
                "Nguồn EMA mô tả một liều ở nhóm từ 2 tuổi trở lên; nhóm nhỏ hơn có lịch theo tuổi.", Prevenar13Source,
                [new(1,"Một liều theo khuyến cáo cho nhóm tuổi này")])]),
        new("VX-BEXSERO", "Bexsero", "GlaxoSmithKline (GSK)", "Ý",
            "Bexsero giúp phòng bệnh xâm lấn do não mô cầu nhóm B. Đây là vắc xin đa thành phần, kết hợp ba protein tái tổ hợp với một thành phần lấy từ màng ngoài của vi khuẩn. Khi vi khuẩn xâm nhập vào máu hoặc màng bao quanh não và tủy sống, bệnh có thể biểu hiện dưới dạng nhiễm khuẩn huyết hoặc viêm màng não.",
            "Trẻ em và người lớn từ 2 tháng tuổi đến 50 tuổi theo VNVC.",
            "Intramuscular", "0,5 ml mỗi liều.", 0.5m, "",
            1820000, "VNVC — thông tin sản phẩm", BexseroSource, ["BENH-NAO-MO-CAU-B"], ["CNVX-TAI-TO-HOP"],
            [new("Phác đồ tham khảo từ 2 tuổi đến 50 tuổi", "Người từ 2 tuổi đến 50 tuổi.",
                "Hai mũi cơ bản; mũi 2 cách mũi 1 ít nhất 1 tháng theo VNVC.", BexseroSource,
                [new(1,"Mũi 1"),new(2,"Ít nhất 1 tháng sau mũi 1",1,1,"Months")])]),
        new("VX-NIMENRIX", "Nimenrix", "Pfizer", "Bỉ",
            "Nimenrix giúp phòng bệnh não mô cầu xâm lấn do bốn nhóm A, C, W-135 và Y. Đây là vắc xin cộng hợp, trong đó thành phần đường từ lớp vỏ của mỗi nhóm được gắn với một protein mang để hỗ trợ đáp ứng miễn dịch. Các nhóm này có thể gây viêm màng não hoặc nhiễm khuẩn huyết khi vi khuẩn xâm nhập vào cơ thể.",
            "Trẻ từ 6 tuần tuổi trở lên và người lớn, không giới hạn trên theo VNVC.",
            "Intramuscular", "0,5 ml mỗi liều sau hoàn nguyên.", 0.5m, "",
            1750000, "VNVC — thông tin sản phẩm", NimenrixSource, ["BENH-NAO-MO-CAU-ACWY"], ["CNVX-CONG-HOP"],
            [new("Phác đồ tham khảo từ 1 tuổi trở lên", "Người từ 1 tuổi trở lên.",
                "Một mũi cơ bản; nhóm tuổi nhỏ hơn có lịch riêng theo nguồn.", NimenrixSource,
                [new(1,"Một mũi cơ bản")])]),
        new("VX-MMR-II", "M-M-R II", "Merck Sharp & Dohme (MSD)", "Mỹ",
            "M-M-R II phối hợp ba thành phần virus sống giảm độc lực để giúp phòng sởi, quai bị và rubella. Sởi có thể gây sốt cao, ho và phát ban, còn quai bị thường được nhận biết qua tình trạng sưng đau tuyến nước bọt gần hàm. Rubella có thể biểu hiện nhẹ, nhưng nhiễm rubella trong thai kỳ có thể gây dị tật bẩm sinh.", "Người từ 12 tháng tuổi trở lên.",
            "Subcutaneous", "Khoảng 0,5 ml mỗi liều sau hoàn nguyên.", 0.5m, "",
            495000, "FDA — thông tin sản phẩm", MmrSource, ["BENH-SOI","BENH-QUAI-BI","BENH-RUBELLA"], ["CNVX-SONG-GIAM-DOC-LUC"],
            [new("Lịch tham khảo trẻ em", "Trẻ từ 12 tháng tuổi.",
                "Nguồn chính thức khuyến nghị liều đầu 12–15 tháng và liều thứ hai 4–6 tuổi.", MmrSource,
                [new(1,"Liều 1: 12–15 tháng tuổi"),new(2,"Liều 2: 4–6 tuổi")])]),
        new("VX-VARIVAX", "Varivax", "Merck Sharp & Dohme (MSD)", "Mỹ",
            "Varivax là vắc xin đơn thành phần giúp phòng thủy đậu, sử dụng virus varicella-zoster chủng Oka/Merck đã được giảm độc lực. Thủy đậu thường gây phát ban ngứa với các nốt phỏng nước. Virus gây thủy đậu cũng là virus có thể tái hoạt về sau gây zona, nhưng Varivax được dùng với mục đích phòng thủy đậu.", "Người từ 12 tháng tuổi trở lên.",
            "Subcutaneous", "Khoảng 0,5 ml mỗi liều sau hoàn nguyên.", 0.5m, "",
            1118000, "FDA — thông tin sản phẩm", VarivaxSource, ["BENH-THUY-DAU"], ["CNVX-SONG-GIAM-DOC-LUC"],
            [new("Phác đồ tham khảo 12 tháng đến 12 tuổi", "Trẻ 12 tháng đến 12 tuổi.",
                "Hai liều; khoảng cách tối thiểu 3 tháng theo thông tin sản phẩm.", VarivaxSource,
                [new(1,"Mũi 1"),new(2,"Ít nhất 3 tháng sau mũi 1",1,3,"Months")]),
             new("Phác đồ tham khảo từ 13 tuổi", "Người từ 13 tuổi trở lên.",
                "Hai liều, cách nhau tối thiểu 4 tuần.", VarivaxSource,
                [new(1,"Mũi 1"),new(2,"Ít nhất 4 tuần sau mũi 1",1,4,"Weeks")])]),
        new("VX-SHINGRIX", "Shingrix", "GlaxoSmithKline (GSK)", "Bỉ",
            "Shingrix giúp phòng zona thần kinh, bệnh xảy ra khi virus từng gây thủy đậu tái hoạt. Vắc xin sử dụng một protein của virus được tạo bằng công nghệ tái tổ hợp, kết hợp với tá dược hỗ trợ đáp ứng miễn dịch. Zona có thể gây phát ban đau và để lại đau dây thần kinh kéo dài sau khi ban đã lành.", "Người từ 50 tuổi trở lên; hoặc từ 18 tuổi có nguy cơ cao theo nguồn.",
            "Intramuscular", "0,5 ml mỗi liều.", 0.5m, "",
            3680000, "VNVC — thông tin sản phẩm", ShingrixSource, ["BENH-ZONA"], ["CNVX-TAI-TO-HOP"],
            [new("Phác đồ tham khảo từ 50 tuổi", "Người từ 50 tuổi trở lên.",
                "Hai mũi; mũi 2 sau mũi 1 khoảng 2 tháng.", ShingrixSource,
                [new(1,"Mũi 1"),new(2,"2 tháng sau mũi 1",1,2,"Months")]),
             new("Phác đồ tham khảo nguy cơ cao từ 18 tuổi", "Người từ 18 tuổi có nguy cơ cao.",
                "Hai mũi; VNVC mô tả mũi 2 sau mũi 1 khoảng 1 tháng cho nhóm này.", ShingrixSource,
                [new(1,"Mũi 1"),new(2,"1 tháng sau mũi 1",1,1,"Months")])]),
        new("VX-VAXIGRIP", "Vaxigrip", "Sanofi Pasteur", "Pháp",
            "Vaxigrip trong danh mục là vắc xin cúm mùa tam giá, gồm hai thành phần cúm A và một thành phần cúm B. Sản phẩm sử dụng các mảnh virus cúm đã bất hoạt để giúp cơ thể tạo đáp ứng miễn dịch. Thành phần vắc xin cúm mùa có thể được cập nhật theo khuyến cáo từng mùa để phù hợp với các chủng virus dự kiến lưu hành.", "Người từ 6 tháng tuổi trở lên theo thông tin sản phẩm.",
            "Intramuscular", "0,5 ml mỗi liều.", 0.5m, "",
            356000, "VNVC — thông tin sản phẩm", VaxigripSource, ["BENH-CUM"], ["CNVX-BAT-HOAT"],
            [new("Phác đồ tham khảo trẻ dưới 9 tuổi chưa từng tiêm cúm", "Trẻ từ 6 tháng đến dưới 9 tuổi chưa từng tiêm cúm.",
                "Hai mũi, cách nhau ít nhất 4 tuần; sau đó tiêm nhắc hằng năm theo nguồn.", VaxigripSource,
                [new(1,"Mũi 1"),new(2,"Ít nhất 4 tuần sau mũi 1",1,4,"Weeks")]),
             new("Phác đồ tham khảo từ 9 tuổi", "Người từ 9 tuổi trở lên.",
                "Một liều mỗi mùa/năm theo khuyến cáo cập nhật.", VaxigripSource,
                [new(1,"Một liều theo mùa/năm")])]),
        new("VX-GARDASIL-9", "Gardasil 9 0,5 ml", "Merck Sharp & Dohme (MSD)", "Mỹ",
            "Gardasil 9 là vắc xin tái tổ hợp giúp phòng các bệnh do chín týp HPV. Trong số này, HPV 6 và 11 có thể gây mụn cóc sinh dục, còn bảy týp nguy cơ cao liên quan đến ung thư cổ tử cung và một số ung thư khác. Tầm soát ung thư cổ tử cung vẫn có vai trò sau tiêm vì còn những týp HPV nguy cơ cao ngoài phạm vi vắc xin.", "Nam và nữ từ 9 đến 45 tuổi.",
            "Intramuscular", "0,5 ml mỗi liều.", 0.5m,
            "Có thể đau, sưng, đỏ tại chỗ tiêm; sốt nhẹ, đau đầu, mệt mỏi, chóng mặt hoặc buồn nôn.",
            2948000, "VNVC — thông tin sản phẩm", Gardasil9Source, ["BENH-HPV"], ["CNVX-TAI-TO-HOP"],
            [new("Phác đồ tham khảo 2 mũi", "Người từ 9 đến dưới 15 tuổi.",
                "Mũi 2 cách mũi 1 từ 6–12 tháng; không ép range này thành số tháng cố định.", Gardasil9Source,
                [new(1,"Mũi 1"),new(2,"Mũi 2: 6–12 tháng sau mũi 1")]),
             new("Phác đồ tham khảo 3 mũi 0-2-6", "Người từ 15 đến 45 tuổi.",
                "Ba mũi theo mốc 0, 2 và 6 tháng.", Gardasil9Source,
                [new(1,"Mũi 1"),new(2,"2 tháng sau mũi 1",1,2,"Months"),new(3,"6 tháng sau mũi 1",1,6,"Months")])]),
        new("VX-QDENGA", "Qdenga", "Takeda", "Đức",
            "Qdenga là vắc xin sống giảm độc lực giúp phòng sốt xuất huyết do virus Dengue. Sản phẩm có thành phần đại diện cho cả bốn týp Dengue, từ DENV-1 đến DENV-4. Virus Dengue lây chủ yếu qua muỗi Aedes nhiễm virus, vì vậy tránh muỗi đốt và hạn chế nơi muỗi sinh sản vẫn là những biện pháp phòng bệnh quan trọng.", "Người từ 4 tuổi trở lên theo nguồn Việt Nam/EMA.",
            "Subcutaneous", "0,5 ml mỗi liều sau hoàn nguyên.", 0.5m, "",
            1290000, "VNVC — thông tin sản phẩm", QdengaSource, ["BENH-SOT-XUAT-HUYET"], ["CNVX-SONG-GIAM-DOC-LUC"],
            [new("Lịch tiêm tham khảo 2 mũi", "Người từ 4 tuổi trở lên.",
                "Hai mũi theo mốc 0 và 3 tháng.", QdengaSource,
                [new(1,"Mũi 1"),new(2,"3 tháng sau mũi 1",1,3,"Months")])])
    ];

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

        var diseases = new Dictionary<string, Benh>(StringComparer.OrdinalIgnoreCase);
        foreach (var seed in DiseaseSeeds)
            diseases[seed.Code] = await EnsureDiseaseAsync(db, seed);

        var technologies = new Dictionary<string, CongNgheVacXin>(StringComparer.OrdinalIgnoreCase);
        foreach (var seed in TechnologySeeds)
            technologies[seed.Code] = await EnsureTechnologyAsync(db, seed);

        foreach (var seed in VaccineSeeds)
        {
            var vaccine = await EnsureVaccineAsync(db, seed);

            foreach (var code in seed.DiseaseCodes)
                if (!await db.Set<VacXin_Benh>().AnyAsync(x => x.MaVacXin == vaccine.MaVacXin && x.MaBenh == diseases[code].MaBenh))
                    db.Add(new VacXin_Benh { MaVacXin = vaccine.MaVacXin, MaBenh = diseases[code].MaBenh });

            foreach (var code in seed.TechnologyCodes)
                if (!await db.Set<VacXin_CongNghe>().AnyAsync(x => x.MaVacXin == vaccine.MaVacXin && x.MaCongNghe == technologies[code].MaCongNghe))
                    db.Add(new VacXin_CongNghe { MaVacXin = vaccine.MaVacXin, MaCongNghe = technologies[code].MaCongNghe });

            await EnsureRegimensAsync(db, vaccine, seed);
            await db.SaveChangesAsync();
        }

        // Final curated demo does not invent packages without a public composition + price source.
        await tx.CommitAsync();
    }

    private static async Task<VacXin> EnsureVaccineAsync(TiemChungDbContext db, VaccineSeed seed)
    {
        var provenance = $"{seed.SourceName} | VNVC — giá ưu đãi tiêm lẻ (VNĐ/liều) | {PriceSource}";
        var legacyProvenance = $"Nguồn sản phẩm: {seed.SourceUrl}; giá ưu đãi tiêm lẻ: {PriceSource}; kiểm tra 09/11/2026.";
        var row = await db.Vaccines.SingleOrDefaultAsync(x => x.Code == seed.Code);
        if (row is null)
        {
            if (await db.Vaccines.AnyAsync(x => x.TenVacXin == seed.Name))
                throw new InvalidOperationException($"Catalog seed conflict: name '{seed.Name}' already belongs to another vaccine.");
            row = new VacXin { Code = seed.Code, TrangThai = "Active" };
            db.Add(row);
        }
        else if (row.NguonThongTin != legacyProvenance && row.NguonThongTin != provenance)
        {
            throw new InvalidOperationException($"Catalog seed conflict: {seed.Code} is not owned by the curated DATA-R2 seed.");
        }

        if (await db.Vaccines.AnyAsync(x => x.MaVacXin != row.MaVacXin && x.TenVacXin == seed.Name))
            throw new InvalidOperationException($"Catalog seed conflict: name '{seed.Name}' already belongs to another vaccine.");

        row.TenVacXin = seed.Name; row.NhaSanXuat = seed.Manufacturer; row.XuatXu = seed.Origin;
        row.MoTa = seed.Description; row.DoiTuongSuDung = seed.TargetAudience; row.DuongDung = seed.Route;
        row.LieuLuong = seed.Dosage; row.TheTich = seed.Volume; row.Gia = seed.Price;
        if (!string.IsNullOrEmpty(seed.Reaction)) row.PhanUngSauTiem = seed.Reaction;
        row.NguonThongTin = provenance; row.DuongDanNguon = seed.SourceUrl; row.NgayCapNhatThongTin = CheckedAt;
        await db.SaveChangesAsync();
        return row;
    }

    private static async Task EnsureRegimensAsync(TiemChungDbContext db, VacXin vaccine, VaccineSeed seed)
    {
        var existing = await db.Set<VaccineRegimen>().Where(x => x.VaccineId == vaccine.MaVacXin).OrderBy(x => x.Id).ToArrayAsync();
        if (existing.Length != 0 && existing.Length != seed.Regimens.Length)
            throw new InvalidOperationException($"Catalog seed conflict: {seed.Code} has a customized regimen graph.");

        for (var index = 0; index < seed.Regimens.Length; index++)
        {
            var regimenSeed = seed.Regimens[index];
            var regimen = existing.Length == 0 ? new VaccineRegimen { VaccineId = vaccine.MaVacXin } : existing[index];
            regimen.Name = regimenSeed.Name; regimen.TargetAudience = regimenSeed.TargetAudience;
            regimen.Summary = regimenSeed.Summary; regimen.SourceName = seed.SourceName; regimen.SourceUrl = regimenSeed.SourceUrl;
            if (existing.Length == 0) db.Add(regimen);
            await db.SaveChangesAsync();

            var doses = await db.Set<VaccineRegimenDose>().Where(x => x.RegimenId == regimen.Id).OrderBy(x => x.DoseOrder).ToArrayAsync();
            if (doses.Length != 0 && !doses.Select(x => x.DoseOrder).SequenceEqual(regimenSeed.Doses.Select(x => x.Order)))
                throw new InvalidOperationException($"Catalog seed conflict: regimen '{regimen.Name}' has customized dose steps.");

            foreach (var doseSeed in regimenSeed.Doses)
            {
                var dose = doses.SingleOrDefault(x => x.DoseOrder == doseSeed.Order) ?? new VaccineRegimenDose { RegimenId = regimen.Id, DoseOrder = doseSeed.Order };
                dose.TimingLabel = doseSeed.Label; dose.AnchorDoseOrder = doseSeed.AnchorOrder;
                dose.IntervalValue = doseSeed.IntervalValue; dose.IntervalUnit = doseSeed.IntervalUnit;
                if (dose.Id == 0) db.Add(dose);
            }
        }
    }

    private static async Task<Benh> EnsureDiseaseAsync(TiemChungDbContext db, DiseaseSeed seed)
    {
        var row = await db.Set<Benh>().SingleOrDefaultAsync(x => x.Code == seed.Code);
        if (row is not null) return row;
        row = await db.Set<Benh>().SingleOrDefaultAsync(x => x.TenBenh == seed.Name);
        if (row is not null) return row;
        row = new Benh { Code = seed.Code, TenBenh = seed.Name, MoTa = "Nhóm phòng bệnh dùng để tra cứu danh mục.", TrangThai = "Active" };
        db.Add(row);
        await db.SaveChangesAsync();
        return row;
    }

    private static async Task<CongNgheVacXin> EnsureTechnologyAsync(TiemChungDbContext db, TechnologySeed seed)
    {
        var row = await db.Set<CongNgheVacXin>().SingleOrDefaultAsync(x => x.Code == seed.Code);
        if (row is not null) return row;
        row = await db.Set<CongNgheVacXin>().SingleOrDefaultAsync(x => x.TenCongNghe == seed.Name);
        if (row is not null) return row;
        row = new CongNgheVacXin {
            Code = seed.Code, TenCongNghe = seed.Name, MoTa = seed.Description, NguonThongTin = seed.SourceName,
            DuongDanNguon = seed.SourceUrl, NgayCapNhatThongTin = CheckedAt, TrangThai = "Active"
        };
        db.Add(row);
        await db.SaveChangesAsync();
        return row;
    }
}
