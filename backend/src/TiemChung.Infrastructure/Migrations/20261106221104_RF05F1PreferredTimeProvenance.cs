using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RF05F1PreferredTimeProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CoGioDuKien",
                table: "DangKyDichVu",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddCheckConstraint(
                name: "CK_DangKyDichVu_PreferredTimeSpecified",
                table: "DangKyDichVu",
                sql: "[CoGioDuKien]=0 OR [ThoiGianDuKien] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM DangKyDichVu WHERE CoGioDuKien=1) THROW 51000, 'Explicit preferred-time provenance requires a backup restore; downgrade refused.', 1;");
            migrationBuilder.DropCheckConstraint(
                name: "CK_DangKyDichVu_PreferredTimeSpecified",
                table: "DangKyDichVu");

            migrationBuilder.DropColumn(
                name: "CoGioDuKien",
                table: "DangKyDichVu");
        }
    }
}
