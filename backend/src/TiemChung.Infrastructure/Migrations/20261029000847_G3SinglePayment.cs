using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class G3SinglePayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fail before DDL; legacy payment meaning must never be guessed or rewritten.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM ThanhToan GROUP BY MaDangKy HAVING COUNT(*) > 1)
                    THROW 51000, 'G3 preflight: duplicate registration payments; review required. No data changed.', 1;
                IF EXISTS (
                    SELECT 1 FROM ThanhToan p
                    JOIN DangKyDichVu r ON r.MaDangKy=p.MaDangKy
                    JOIN NguoiDuocTiem n ON n.MaNguoiDuocTiem=r.MaNguoiDuocTiem
                    OUTER APPLY (SELECT SUM(SoLuong*GiaTaiThoiDiemDangKy) Total, COUNT(*) Lines
                        FROM ChiTietVacXinLe WHERE MaDangKy=r.MaDangKy) v
                    OUTER APPLY (SELECT SUM(SoLuongGoi*GiaTaiThoiDiemDangKy) Total, COUNT(*) Lines
                        FROM ChiTietGoiVacXin WHERE MaDangKy=r.MaDangKy) g
                    WHERE p.LoaiThanhToan COLLATE Latin1_General_100_BIN2 <> N'ToanBo'
                        OR p.PhuongThuc COLLATE Latin1_General_100_BIN2 <> N'MoPhong'
                        OR p.TrangThai COLLATE Latin1_General_100_BIN2 <> N'ThanhCong'
                        OR DATALENGTH(p.LoaiThanhToan)<>12 OR DATALENGTH(p.PhuongThuc)<>14 OR DATALENGTH(p.TrangThai)<>18
                        OR r.TrangThai COLLATE Latin1_General_100_BIN2 <> N'DaDangKy' OR DATALENGTH(r.TrangThai)<>16
                        OR r.MaKhuyenMai IS NOT NULL OR n.MaKhachHang<>r.MaKhachHang
                        OR p.SoTien<>COALESCE(v.Total,0)+COALESCE(g.Total,0)
                        OR p.SoTien<>FLOOR(p.SoTien) OR p.SoTien>9999999999999999
                        OR v.Lines+g.Lines NOT BETWEEN 1 AND 100
                        OR EXISTS (SELECT 1 FROM ChiTietVacXinLe i WHERE i.MaDangKy=r.MaDangKy AND
                            (i.SoLuong NOT BETWEEN 1 AND 100 OR i.GiaTaiThoiDiemDangKy<>FLOOR(i.GiaTaiThoiDiemDangKy)))
                        OR EXISTS (SELECT 1 FROM ChiTietGoiVacXin i WHERE i.MaDangKy=r.MaDangKy AND
                            (i.SoLuongGoi NOT BETWEEN 1 AND 100 OR i.GiaTaiThoiDiemDangKy<>FLOOR(i.GiaTaiThoiDiemDangKy)))
                )
                    THROW 51000, 'G3 preflight: incompatible legacy payment; review required. No data changed.', 1;
                """);
            migrationBuilder.DropIndex(
                name: "IX_ThanhToan_MaDangKy",
                table: "ThanhToan");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhToan_MaDangKy",
                table: "ThanhToan",
                column: "MaDangKy",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ThanhToan_MaDangKy",
                table: "ThanhToan");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhToan_MaDangKy",
                table: "ThanhToan",
                column: "MaDangKy");
        }
    }
}
