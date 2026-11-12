namespace TiemChung.Domain.Entities;

public sealed class NhanVien
{
    public int MaNhanVien { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string CCCD { get; set; } = string.Empty;
    public string SoDienThoai { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string BoPhan { get; set; } = string.Empty;
    public string ChucDanh { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class TaiKhoan
{
    public int MaTaiKhoan { get; set; }
    public int? MaNhanVien { get; set; }
    public string AccountType { get; set; } = "Employee";
    public string? LoginEmail { get; set; }
    public string? NormalizedEmail { get; set; }
    public string? NormalizedUsername { get; set; }
    public DateTimeOffset? EmailVerifiedAt { get; set; }
    public long SecurityVersion { get; set; } = 1;
    public string AdministrativeDuty { get; set; } = "None";
    public DateTimeOffset CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhauHash { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class QuyenHan
{
    public string Code { get; set; } = string.Empty;
    public int MaQuyen { get; set; }
    public string TenQuyen { get; set; } = string.Empty;
    public string MoTa { get; set; } = string.Empty;
}

public sealed class TaiKhoan_QuyenHan
{
    public int MaTaiKhoan { get; set; }
    public int MaQuyen { get; set; }
}

public sealed class KhachHang
{
    public int MaKhachHang { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string? CCCD { get; set; }
    public string SoDienThoai { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DanhXung { get; set; }
    public DateOnly? NgaySinh { get; set; }
}

public sealed class NguoiDuocTiem
{
    public string MaHoSo { get; private set; } = "VAC-" + Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(6));
    public int MaNguoiDuocTiem { get; set; }
    public int MaKhachHang { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string? CCCD { get; set; }
    public DateOnly NgaySinh { get; set; }
    public string GioiTinh { get; set; } = string.Empty;
    public string QuanHeVoiKhachHang { get; set; } = string.Empty;
}

public sealed class DangKyDichVu
{
    public int MaDangKy { get; set; }
    public int MaKhachHang { get; set; }
    public int MaNguoiDuocTiem { get; set; }
    public int? MaNhanVien { get; set; }
    public int? MaKhuyenMai { get; set; }
    public string? TenKhuyenMaiSnapshot { get; set; }
    public decimal? MucGiamSnapshot { get; set; }
    public DateTime NgayDangKy { get; set; }
    public string? HinhThucTiem { get; set; }
    public DateTime? ThoiGianDuKien { get; set; }
    public bool CoGioDuKien { get; set; }
    public string? TenNguoiLienHe { get; set; }
    public string? SoDienThoaiLienHe { get; set; }
    public string? EmailLienHe { get; set; }
    public string? TinhThanhLienHe { get; set; }
    public string? PhuongXaLienHe { get; set; }
    public string? DiaChiLienHe { get; set; }
    public decimal? PhiKhamTuVan { get; set; }
    public string? KenhThanhToanDuKien { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class ChiTietVacXinLe
{
    public int MaChiTietVacXin { get; set; }
    public int MaDangKy { get; set; }
    public int MaVacXin { get; set; }
    public int SoLuong { get; set; }
    public decimal GiaTaiThoiDiemDangKy { get; set; }
}

public sealed class ChiTietGoiVacXin
{
    public int MaChiTietGoi { get; set; }
    public int MaDangKy { get; set; }
    public int MaGoi { get; set; }
    public int SoLuongGoi { get; set; }
    public decimal GiaTaiThoiDiemDangKy { get; set; }
}

public sealed class ThanhToan
{
    public int MaThanhToan { get; set; }
    public int MaDangKy { get; set; }
    public int? MaNhanVien { get; set; }
    public decimal SoTien { get; set; }
    public string PhuongThuc { get; set; } = string.Empty;
    public DateTime ThoiDiem { get; set; }
    public string LoaiThanhToan { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public string? MaGiaoDich { get; set; }
    public string? TenNguoiThanhToan { get; set; }
    public string? SoDienThoaiThanhToan { get; set; }
    public string? EmailThanhToan { get; set; }
    public string? TinhThanhThanhToan { get; set; }
    public string? PhuongXaThanhToan { get; set; }
    public string? DiaChiThanhToan { get; set; }
    public string? NhaCungCapThanhToan { get; set; }
    public string? TenChuTaiKhoanThe { get; set; }
    public string? SoDinhDanhMasked { get; set; }
    public string? ThamChieuThanhToan { get; set; }
    public decimal? SoTienKhachDua { get; set; }
    public DateTimeOffset? DongYDieuKhoanLuc { get; set; }
    public long? PayOsOrderCode { get; set; }
    public string? PayOsPaymentLinkId { get; set; }
    public string? PayOsLifecycleState { get; set; }
    public string? PayOsCheckoutUrl { get; set; }
    public string? PayOsQrCode { get; set; }
    public string? PayOsReference { get; set; }
}

public sealed class KhuyenMai
{
    public int MaKhuyenMai { get; set; }
    public string TenKhuyenMai { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
    public DateTime ThoiGianBatDau { get; set; }
    public DateTime ThoiGianKetThuc { get; set; }
    public decimal MucUuDai { get; set; }
    public bool IsEnabled { get; set; }
}

public sealed class CongNgheVacXin
{
    public int MaCongNghe { get; set; }
    public string Code { get; set; } = string.Empty;
    public string TenCongNghe { get; set; } = string.Empty;
    public string MoTa { get; set; } = string.Empty;
    public string? DacDiemKyThuat { get; set; }
    public string? GhiChuChuyenMon { get; set; }
    public string? NguonThongTin { get; set; }
    public string? DuongDanNguon { get; set; }
    public DateTimeOffset? NgayCapNhatThongTin { get; set; }
    public string TrangThai { get; set; } = "Active";
}

public sealed class VacXin_CongNghe
{
    public int MaVacXin { get; set; }
    public int MaCongNghe { get; set; }
}

public sealed class Benh
{
    public string Code { get; set; } = string.Empty;
    public string TrangThai { get; set; } = "Active";
    public int MaBenh { get; set; }
    public string TenBenh { get; set; } = string.Empty;
    public string MoTa { get; set; } = string.Empty;
}

public sealed class VacXin
{
    public string Code { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public string? DoiTuongSuDung { get; set; }
    public string? DuongDung { get; set; }
    public string? DuongDungKhac { get; set; }
    public string? NguonThongTin { get; set; }
    public string? DuongDanNguon { get; set; }
    public DateTimeOffset? NgayCapNhatThongTin { get; set; }
    public int MaVacXin { get; set; }
    public string TenVacXin { get; set; } = string.Empty;
    public string NhaSanXuat { get; set; } = string.Empty;
    public string XuatXu { get; set; } = string.Empty;
    public string LieuLuong { get; set; } = string.Empty;
    public decimal TheTich { get; set; }
    public string PhanUngSauTiem { get; set; } = string.Empty;
    public decimal Gia { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class VacXin_Benh
{
    public int MaVacXin { get; set; }
    public int MaBenh { get; set; }
}

public sealed class GoiVacXin
{
    public string Code { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public string? DoiTuongSuDung { get; set; }
    public int MaGoi { get; set; }
    public string TenGoi { get; set; } = string.Empty;
    public decimal GiaGoi { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class GoiVacXin_VacXin
{
    public int MaGoi { get; set; }
    public int MaVacXin { get; set; }
    public int SoLuong { get; set; }
}

public sealed class VaccineRegimen
{
    public int Id { get; set; }
    public int VaccineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? SourceName { get; set; }
    public string? SourceUrl { get; set; }
}

public sealed class VaccineRegimenDose
{
    public int Id { get; set; }
    public int RegimenId { get; set; }
    public int DoseOrder { get; set; }
    public string TimingLabel { get; set; } = string.Empty;
    public int? MinimumIntervalDays { get; set; }
    public int? RecommendedIntervalDays { get; set; }
    public int? AnchorDoseOrder { get; set; }
    public int? IntervalValue { get; set; }
    public string? IntervalUnit { get; set; }
    public string? Note { get; set; }
}

public sealed class LoVacXin
{
    public string SoLo { get; set; } = string.Empty;
    public int MaVacXin { get; set; }
    public DateOnly NgaySanXuat { get; set; }
    public DateOnly HanSuDung { get; set; }
    public int SoLuongBanDau { get; set; }
    public int SoLuongTon { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class BienDongKho
{
    public int MaBienDong { get; set; }
    public int? MaMuiTiem { get; set; }
    public string SoLo { get; set; } = string.Empty;
    public int MaNhanVien { get; set; }
    public string LoaiBienDong { get; set; } = string.Empty;
    public int SoLuong { get; set; }
    public DateTime ThoiDiem { get; set; }
}

public sealed class PhieuKhamSangLoc
{
    public int MaPhieuKham { get; set; }
    public int? MaLichHen { get; set; }
    public DateTimeOffset? ThoiDiemKham { get; set; }
    public int MaDangKy { get; set; }
    public int MaNguoiDuocTiem { get; set; }
    public int MaNhanVien { get; set; }
    public string? ChiSoSinhTon { get; set; }
    public string? TienSuBenhLy { get; set; }
    public string KetLuan { get; set; } = string.Empty;
    public string? LyDoHoan { get; set; }
}

public sealed class ChiDinhTiem
{
    public int MaChiDinh { get; set; }
    public int MaPhieuKham { get; set; }
    public int MaVacXin { get; set; }
    public string KetQuaChiDinh { get; set; } = string.Empty;
    public DateTime ThoiGian { get; set; }
    public string GhiChu { get; set; } = string.Empty;
}

public sealed class MuiTiem
{
    public int MaMuiTiem { get; set; }
    public int? MaChiDinh { get; set; }
    public int MaNguoiDuocTiem { get; set; }
    public string SoLo { get; set; } = string.Empty;
    public int MaNhanVien { get; set; }
    public DateTime ThoiDiemTiem { get; set; }
    public string ViTriTiem { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class TheoDoiSauTiem
{
    public int MaTheoDoi { get; set; }
    public int? MaNhanVien { get; set; }
    public int MaMuiTiem { get; set; }
    public string TinhTrang { get; set; } = string.Empty;
    public string MucDoPhanUng { get; set; } = string.Empty;
    public string HuongXuTri { get; set; } = string.Empty;
    public DateTime ThoiDiem { get; set; }
}

public sealed class LichHenTiem
{
    public byte[] RowVersion { get; set; } = [];
    public int MaLichHen { get; set; }
    public int? MaDangKy { get; set; }
    public int? MaChiTietVacXin { get; set; }
    public int? MaChiTietGoi { get; set; }
    public int MaNguoiDuocTiem { get; set; }
    public DateTime ThoiGianHen { get; set; }
    public string DiemTiem { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class NhacLich
{
    public DateTime? ScheduledAt { get; set; }
    public int? MaNhanVien { get; set; }
    public string? NoiDungMoPhong { get; set; }
    public int MaNhacLich { get; set; }
    public int MaLichHen { get; set; }
    public string KenhGui { get; set; } = string.Empty;
    public DateTime ThoiDiemGui { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class HangDoiVacXin
{
    public int MaHangDoi { get; set; }
    public int MaDangKy { get; set; }
    public int MaVacXin { get; set; }
    public DateTime ThoiDiemVaoHang { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}
