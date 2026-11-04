using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionSnapshotToDangKyDichVu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MucGiamSnapshot",
                table: "DangKyDichVu",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TenKhuyenMaiSnapshot",
                table: "DangKyDichVu",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TongTienSauGiam",
                table: "DangKyDichVu",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MucGiamSnapshot",
                table: "DangKyDichVu");

            migrationBuilder.DropColumn(
                name: "TenKhuyenMaiSnapshot",
                table: "DangKyDichVu");

            migrationBuilder.DropColumn(
                name: "TongTienSauGiam",
                table: "DangKyDichVu");
        }
    }
}
