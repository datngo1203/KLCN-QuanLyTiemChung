using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RF04ARegistrationSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiaChiLienHe",
                table: "DangKyDichVu",
                type: "nvarchar(250)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailLienHe",
                table: "DangKyDichVu",
                type: "nvarchar(254)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PhiKhamTuVan",
                table: "DangKyDichVu",
                type: "decimal(18,0)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhuongXaLienHe",
                table: "DangKyDichVu",
                type: "nvarchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoDienThoaiLienHe",
                table: "DangKyDichVu",
                type: "varchar(11)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenNguoiLienHe",
                table: "DangKyDichVu",
                type: "nvarchar(150)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TinhThanhLienHe",
                table: "DangKyDichVu",
                type: "nvarchar(100)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiaChiLienHe",
                table: "DangKyDichVu");

            migrationBuilder.DropColumn(
                name: "EmailLienHe",
                table: "DangKyDichVu");

            migrationBuilder.DropColumn(
                name: "PhiKhamTuVan",
                table: "DangKyDichVu");

            migrationBuilder.DropColumn(
                name: "PhuongXaLienHe",
                table: "DangKyDichVu");

            migrationBuilder.DropColumn(
                name: "SoDienThoaiLienHe",
                table: "DangKyDichVu");

            migrationBuilder.DropColumn(
                name: "TenNguoiLienHe",
                table: "DangKyDichVu");

            migrationBuilder.DropColumn(
                name: "TinhThanhLienHe",
                table: "DangKyDichVu");
        }
    }
}
