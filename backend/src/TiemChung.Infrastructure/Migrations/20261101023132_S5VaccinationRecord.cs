using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S5VaccinationRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaChiDinh",
                table: "MuiTiem",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaMuiTiem",
                table: "BienDongKho",
                type: "int",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_MuiTiem_MaMuiTiem_SoLo_MaNhanVien",
                table: "MuiTiem",
                columns: new[] { "MaMuiTiem", "SoLo", "MaNhanVien" });

            migrationBuilder.CreateIndex(
                name: "IX_MuiTiem_MaChiDinh",
                table: "MuiTiem",
                column: "MaChiDinh",
                unique: true,
                filter: "[MaChiDinh] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BienDongKho_MaMuiTiem",
                table: "BienDongKho",
                column: "MaMuiTiem",
                unique: true,
                filter: "[MaMuiTiem] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BienDongKho_MaMuiTiem_SoLo_MaNhanVien",
                table: "BienDongKho",
                columns: new[] { "MaMuiTiem", "SoLo", "MaNhanVien" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_BienDongKho_Vaccination",
                table: "BienDongKho",
                sql: "[MaMuiTiem] IS NULL OR ([LoaiBienDong] = N'XuatDoTiem' AND [SoLuong] = 1)");

            migrationBuilder.AddForeignKey(
                name: "FK_BienDongKho_MuiTiem_MaMuiTiem_SoLo_MaNhanVien",
                table: "BienDongKho",
                columns: new[] { "MaMuiTiem", "SoLo", "MaNhanVien" },
                principalTable: "MuiTiem",
                principalColumns: new[] { "MaMuiTiem", "SoLo", "MaNhanVien" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MuiTiem_ChiDinhTiem_MaChiDinh",
                table: "MuiTiem",
                column: "MaChiDinh",
                principalTable: "ChiDinhTiem",
                principalColumn: "MaChiDinh",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM MuiTiem WHERE MaChiDinh IS NOT NULL) OR EXISTS (SELECT 1 FROM BienDongKho WHERE MaMuiTiem IS NOT NULL) THROW 51000, 'S5.5 vaccination links require data review before downgrade; no links were discarded.', 1;");
            migrationBuilder.DropForeignKey(
                name: "FK_BienDongKho_MuiTiem_MaMuiTiem_SoLo_MaNhanVien",
                table: "BienDongKho");

            migrationBuilder.DropForeignKey(
                name: "FK_MuiTiem_ChiDinhTiem_MaChiDinh",
                table: "MuiTiem");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_MuiTiem_MaMuiTiem_SoLo_MaNhanVien",
                table: "MuiTiem");

            migrationBuilder.DropIndex(
                name: "IX_MuiTiem_MaChiDinh",
                table: "MuiTiem");

            migrationBuilder.DropIndex(
                name: "IX_BienDongKho_MaMuiTiem",
                table: "BienDongKho");

            migrationBuilder.DropIndex(
                name: "IX_BienDongKho_MaMuiTiem_SoLo_MaNhanVien",
                table: "BienDongKho");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BienDongKho_Vaccination",
                table: "BienDongKho");

            migrationBuilder.DropColumn(
                name: "MaChiDinh",
                table: "MuiTiem");

            migrationBuilder.DropColumn(
                name: "MaMuiTiem",
                table: "BienDongKho");
        }
    }
}
