using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RF04B2WebCheckout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ThanhToan_NhanVien",
                table: "ThanhToan");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThanhToan_NhanVien",
                table: "ThanhToan",
                sql: "([PhuongThuc] IN ('MoPhong','TienMat','TheTaiQuay','ChuyenKhoanQR') AND [MaNhanVien] IS NOT NULL AND [TrangThai] = 'ThanhCong') OR ([PhuongThuc] IN ('TheQuocTe','TheNoiDia') AND [MaNhanVien] IS NULL AND [TrangThai] = 'ThanhCong' AND [DongYDieuKhoanLuc] IS NOT NULL) OR ([PhuongThuc] = 'ChuyenKhoanOnline' AND [DongYDieuKhoanLuc] IS NOT NULL AND (([TrangThai] = 'ChoXacNhan' AND [MaNhanVien] IS NULL) OR ([TrangThai] = 'ThanhCong' AND [MaNhanVien] IS NOT NULL)))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM dbo.ThanhToan WITH (TABLOCKX,HOLDLOCK)
                    WHERE PhuongThuc IN ('TheQuocTe','TheNoiDia','ChuyenKhoanOnline'))
                    THROW 51006, 'Cannot roll back RF-04B2 while online payment history exists.', 1;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_ThanhToan_NhanVien",
                table: "ThanhToan");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThanhToan_NhanVien",
                table: "ThanhToan",
                sql: "([PhuongThuc] NOT IN ('MoPhong','TienMat','TheTaiQuay','ChuyenKhoanQR') OR [MaNhanVien] IS NOT NULL) AND ([TrangThai] <> 'ChoXacNhan' OR ([PhuongThuc] = 'ChuyenKhoanOnline' AND [MaNhanVien] IS NULL)) AND ([PhuongThuc] <> 'ChuyenKhoanOnline' OR [TrangThai] <> 'ThanhCong' OR [MaNhanVien] IS NOT NULL)");
        }
    }
}
