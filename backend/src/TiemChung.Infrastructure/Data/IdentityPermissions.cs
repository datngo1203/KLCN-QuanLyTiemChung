namespace TiemChung.Infrastructure.Data;

public static class IdentityPermissions
{
    // Explicitly granted through administration; excluded from default Admin/recovery grants.
    public const string InventoryReceive = "NhapKho";
    public static readonly string[] Codes = ["XemTongQuan","QuanLyLichHen","GuiNhacLich","QuanLyKhuyenMai",
        "XemBaoCao","XemThanhToan","GhiThanhToan","QuanLyTaiKhoan","QuanLyDanhMucChuyenMon",
        "QuanLyDanhMucGia","XemKho","XemHoSo"];
}
