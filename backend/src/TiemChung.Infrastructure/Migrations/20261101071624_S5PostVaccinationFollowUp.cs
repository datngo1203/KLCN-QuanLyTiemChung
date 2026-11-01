using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S5PostVaccinationFollowUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaNhanVien",
                table: "TheoDoiSauTiem",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TheoDoiSauTiem_MaNhanVien",
                table: "TheoDoiSauTiem",
                column: "MaNhanVien");

            migrationBuilder.AddForeignKey(
                name: "FK_TheoDoiSauTiem_NhanVien_MaNhanVien",
                table: "TheoDoiSauTiem",
                column: "MaNhanVien",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM TheoDoiSauTiem WHERE MaNhanVien IS NOT NULL) THROW 51000, 'S5.6 recorder links require data review before downgrade; no links were discarded.', 1;");
            migrationBuilder.DropForeignKey(
                name: "FK_TheoDoiSauTiem_NhanVien_MaNhanVien",
                table: "TheoDoiSauTiem");

            migrationBuilder.DropIndex(
                name: "IX_TheoDoiSauTiem_MaNhanVien",
                table: "TheoDoiSauTiem");

            migrationBuilder.DropColumn(
                name: "MaNhanVien",
                table: "TheoDoiSauTiem");
        }
    }
}
