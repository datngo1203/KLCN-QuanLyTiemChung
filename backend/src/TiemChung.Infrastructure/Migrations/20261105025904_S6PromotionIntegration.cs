using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S6PromotionIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM KhuyenMai WHERE MucUuDai <> FLOOR(MucUuDai))
                    THROW 51000, 'Fractional legacy promotion amounts require explicit reconciliation.', 1;
                """);
            migrationBuilder.DropCheckConstraint("CK_KhuyenMai_MucUuDai", "KhuyenMai");
            migrationBuilder.AlterColumn<decimal>(
                name: "MucUuDai",
                table: "KhuyenMai",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,2)");
            migrationBuilder.AddCheckConstraint("CK_KhuyenMai_MucUuDai", "KhuyenMai", "[MucUuDai] >= 0");

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "KhuyenMai",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MucGiamSnapshot",
                table: "DangKyDichVu",
                type: "decimal(18,0)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenKhuyenMaiSnapshot",
                table: "DangKyDichVu",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM KhuyenMai WHERE MucUuDai > 999999 OR IsEnabled = 1)
                   OR EXISTS (SELECT 1 FROM DangKyDichVu WHERE MucGiamSnapshot IS NOT NULL OR TenKhuyenMaiSnapshot IS NOT NULL)
                    THROW 51000, 'Promotion data cannot be represented by the previous schema.', 1;
                """);
            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "KhuyenMai");

            migrationBuilder.DropColumn(
                name: "MucGiamSnapshot",
                table: "DangKyDichVu");

            migrationBuilder.DropColumn(
                name: "TenKhuyenMaiSnapshot",
                table: "DangKyDichVu");

            migrationBuilder.DropCheckConstraint("CK_KhuyenMai_MucUuDai", "KhuyenMai");
            migrationBuilder.AlterColumn<decimal>(
                name: "MucUuDai",
                table: "KhuyenMai",
                type: "decimal(8,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");
            migrationBuilder.AddCheckConstraint("CK_KhuyenMai_MucUuDai", "KhuyenMai", "[MucUuDai] >= 0");
        }
    }
}
