namespace TiemChung.Infrastructure.Data;

public static class IdentityPermissions
{
    // Legacy SQL code retained for compatibility; runtime access is defined by EmployeeAuthorization.
    public const string InventoryReceive = "NhapKho";
    public static readonly string[] Codes = ["XemTongQuan","QuanLyLichHen","GuiNhacLich","QuanLyKhuyenMai",
        "XemBaoCao","XemThanhToan","GhiThanhToan","QuanLyTaiKhoan","QuanLyDanhMucChuyenMon",
        "QuanLyDanhMucGia","XemKho","XemHoSo"];
}
