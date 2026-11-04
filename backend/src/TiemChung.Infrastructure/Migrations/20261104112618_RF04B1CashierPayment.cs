using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RF04B1CashierPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MaNhanVien",
                table: "ThanhToan",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "DiaChiThanhToan",
                table: "ThanhToan",
                type: "nvarchar(250)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DongYDieuKhoanLuc",
                table: "ThanhToan",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailThanhToan",
                table: "ThanhToan",
                type: "nvarchar(254)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaGiaoDich",
                table: "ThanhToan",
                type: "nvarchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NhaCungCapThanhToan",
                table: "ThanhToan",
                type: "nvarchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhuongXaThanhToan",
                table: "ThanhToan",
                type: "nvarchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoDienThoaiThanhToan",
                table: "ThanhToan",
                type: "varchar(11)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoDinhDanhMasked",
                table: "ThanhToan",
                type: "nvarchar(30)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SoTienKhachDua",
                table: "ThanhToan",
                type: "decimal(18,0)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenChuTaiKhoanThe",
                table: "ThanhToan",
                type: "nvarchar(150)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenNguoiThanhToan",
                table: "ThanhToan",
                type: "nvarchar(150)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThamChieuThanhToan",
                table: "ThanhToan",
                type: "nvarchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TinhThanhThanhToan",
                table: "ThanhToan",
                type: "nvarchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KenhThanhToanDuKien",
                table: "DangKyDichVu",
                type: "nvarchar(30)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThanhToan_MaGiaoDich",
                table: "ThanhToan",
                column: "MaGiaoDich",
                unique: true,
                filter: "[MaGiaoDich] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThanhToan_NhanVien",
                table: "ThanhToan",
                sql: "([PhuongThuc] NOT IN ('MoPhong','TienMat','TheTaiQuay','ChuyenKhoanQR') OR [MaNhanVien] IS NOT NULL) AND ([TrangThai] <> 'ChoXacNhan' OR ([PhuongThuc] = 'ChuyenKhoanOnline' AND [MaNhanVien] IS NULL)) AND ([PhuongThuc] <> 'ChuyenKhoanOnline' OR [TrangThai] <> 'ThanhCong' OR [MaNhanVien] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThanhToan_SoTienKhachDua",
                table: "ThanhToan",
                sql: "[SoTienKhachDua] IS NULL OR [SoTienKhachDua] >= [SoTien]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThanhToan_TrangThai",
                table: "ThanhToan",
                sql: "[TrangThai] IN ('ChoXacNhan','ThanhCong')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DangKyDichVu_KenhThanhToanDuKien",
                table: "DangKyDichVu",
                sql: "[KenhThanhToanDuKien] IS NULL OR [KenhThanhToanDuKien] IN ('TaiTrungTam','TrucTuyen')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM dbo.ThanhToan WHERE MaNhanVien IS NULL)
                    THROW 51004, 'RF-04B1 rollback blocked: payment rows without an employee exist.', 1;
                IF EXISTS (SELECT 1 FROM dbo.ThanhToan WHERE MaGiaoDich IS NOT NULL OR PhuongThuc <> N'MoPhong'
                    OR TenNguoiThanhToan IS NOT NULL OR SoDienThoaiThanhToan IS NOT NULL OR EmailThanhToan IS NOT NULL
                    OR TinhThanhThanhToan IS NOT NULL OR PhuongXaThanhToan IS NOT NULL OR DiaChiThanhToan IS NOT NULL
                    OR NhaCungCapThanhToan IS NOT NULL OR TenChuTaiKhoanThe IS NOT NULL OR SoDinhDanhMasked IS NOT NULL
                    OR ThamChieuThanhToan IS NOT NULL OR SoTienKhachDua IS NOT NULL OR DongYDieuKhoanLuc IS NOT NULL)
                    OR EXISTS (SELECT 1 FROM dbo.DangKyDichVu WHERE KenhThanhToanDuKien IS NOT NULL)
                    THROW 51005, 'RF-04B1 rollback blocked: payment history or channel preferences would be lost.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_ThanhToan_MaGiaoDich",
                table: "ThanhToan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ThanhToan_NhanVien",
                table: "ThanhToan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ThanhToan_SoTienKhachDua",
                table: "ThanhToan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ThanhToan_TrangThai",
                table: "ThanhToan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DangKyDichVu_KenhThanhToanDuKien",
                table: "DangKyDichVu");

            migrationBuilder.DropColumn(
                name: "DiaChiThanhToan",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "DongYDieuKhoanLuc",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "EmailThanhToan",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "MaGiaoDich",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "NhaCungCapThanhToan",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "PhuongXaThanhToan",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "SoDienThoaiThanhToan",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "SoDinhDanhMasked",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "SoTienKhachDua",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "TenChuTaiKhoanThe",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "TenNguoiThanhToan",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "ThamChieuThanhToan",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "TinhThanhThanhToan",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "KenhThanhToanDuKien",
                table: "DangKyDichVu");

            migrationBuilder.AlterColumn<int>(
                name: "MaNhanVien",
                table: "ThanhToan",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
