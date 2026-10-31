using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S5AppointmentSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChiTietVacXinLe_MaDangKy",
                table: "ChiTietVacXinLe");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietGoiVacXin_MaDangKy",
                table: "ChiTietGoiVacXin");

            migrationBuilder.AddColumn<int>(
                name: "MaChiTietGoi",
                table: "LichHenTiem",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaChiTietVacXin",
                table: "LichHenTiem",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaDangKy",
                table: "LichHenTiem",
                type: "int",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_DangKyDichVu_MaDangKy_MaNguoiDuocTiem",
                table: "DangKyDichVu",
                columns: new[] { "MaDangKy", "MaNguoiDuocTiem" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ChiTietVacXinLe_MaDangKy_MaChiTietVacXin",
                table: "ChiTietVacXinLe",
                columns: new[] { "MaDangKy", "MaChiTietVacXin" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ChiTietGoiVacXin_MaDangKy_MaChiTietGoi",
                table: "ChiTietGoiVacXin",
                columns: new[] { "MaDangKy", "MaChiTietGoi" });

            migrationBuilder.CreateIndex(
                name: "IX_LichHenTiem_MaDangKy_MaChiTietGoi",
                table: "LichHenTiem",
                columns: new[] { "MaDangKy", "MaChiTietGoi" });

            migrationBuilder.CreateIndex(
                name: "IX_LichHenTiem_MaDangKy_MaChiTietVacXin",
                table: "LichHenTiem",
                columns: new[] { "MaDangKy", "MaChiTietVacXin" });

            migrationBuilder.CreateIndex(
                name: "IX_LichHenTiem_MaDangKy_MaNguoiDuocTiem",
                table: "LichHenTiem",
                columns: new[] { "MaDangKy", "MaNguoiDuocTiem" });

            migrationBuilder.CreateIndex(
                name: "IX_LichHenTiem_PackageTime",
                table: "LichHenTiem",
                columns: new[] { "MaChiTietGoi", "ThoiGianHen" },
                unique: true,
                filter: "[MaChiTietGoi] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LichHenTiem_VaccineTime",
                table: "LichHenTiem",
                columns: new[] { "MaChiTietVacXin", "ThoiGianHen" },
                unique: true,
                filter: "[MaChiTietVacXin] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LichHenTiem_NguonDichVu",
                table: "LichHenTiem",
                sql: "([MaDangKy] IS NULL AND [MaChiTietVacXin] IS NULL AND [MaChiTietGoi] IS NULL) OR ([MaDangKy] IS NOT NULL AND (([MaChiTietVacXin] IS NOT NULL AND [MaChiTietGoi] IS NULL) OR ([MaChiTietVacXin] IS NULL AND [MaChiTietGoi] IS NOT NULL)))");

            migrationBuilder.AddForeignKey(
                name: "FK_LichHenTiem_ChiTietGoiVacXin_MaDangKy_MaChiTietGoi",
                table: "LichHenTiem",
                columns: new[] { "MaDangKy", "MaChiTietGoi" },
                principalTable: "ChiTietGoiVacXin",
                principalColumns: new[] { "MaDangKy", "MaChiTietGoi" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LichHenTiem_ChiTietVacXinLe_MaDangKy_MaChiTietVacXin",
                table: "LichHenTiem",
                columns: new[] { "MaDangKy", "MaChiTietVacXin" },
                principalTable: "ChiTietVacXinLe",
                principalColumns: new[] { "MaDangKy", "MaChiTietVacXin" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LichHenTiem_DangKyDichVu_MaDangKy_MaNguoiDuocTiem",
                table: "LichHenTiem",
                columns: new[] { "MaDangKy", "MaNguoiDuocTiem" },
                principalTable: "DangKyDichVu",
                principalColumns: new[] { "MaDangKy", "MaNguoiDuocTiem" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM LichHenTiem WHERE MaDangKy IS NOT NULL) THROW 51000, 'S5 appointments still reference registration items; review before downgrade.', 1;");
            migrationBuilder.DropForeignKey(
                name: "FK_LichHenTiem_ChiTietGoiVacXin_MaDangKy_MaChiTietGoi",
                table: "LichHenTiem");

            migrationBuilder.DropForeignKey(
                name: "FK_LichHenTiem_ChiTietVacXinLe_MaDangKy_MaChiTietVacXin",
                table: "LichHenTiem");

            migrationBuilder.DropForeignKey(
                name: "FK_LichHenTiem_DangKyDichVu_MaDangKy_MaNguoiDuocTiem",
                table: "LichHenTiem");

            migrationBuilder.DropIndex(
                name: "IX_LichHenTiem_MaDangKy_MaChiTietGoi",
                table: "LichHenTiem");

            migrationBuilder.DropIndex(
                name: "IX_LichHenTiem_MaDangKy_MaChiTietVacXin",
                table: "LichHenTiem");

            migrationBuilder.DropIndex(
                name: "IX_LichHenTiem_MaDangKy_MaNguoiDuocTiem",
                table: "LichHenTiem");

            migrationBuilder.DropIndex(
                name: "IX_LichHenTiem_PackageTime",
                table: "LichHenTiem");

            migrationBuilder.DropIndex(
                name: "IX_LichHenTiem_VaccineTime",
                table: "LichHenTiem");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LichHenTiem_NguonDichVu",
                table: "LichHenTiem");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_DangKyDichVu_MaDangKy_MaNguoiDuocTiem",
                table: "DangKyDichVu");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ChiTietVacXinLe_MaDangKy_MaChiTietVacXin",
                table: "ChiTietVacXinLe");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ChiTietGoiVacXin_MaDangKy_MaChiTietGoi",
                table: "ChiTietGoiVacXin");

            migrationBuilder.DropColumn(
                name: "MaChiTietGoi",
                table: "LichHenTiem");

            migrationBuilder.DropColumn(
                name: "MaChiTietVacXin",
                table: "LichHenTiem");

            migrationBuilder.DropColumn(
                name: "MaDangKy",
                table: "LichHenTiem");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietVacXinLe_MaDangKy",
                table: "ChiTietVacXinLe",
                column: "MaDangKy");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietGoiVacXin_MaDangKy",
                table: "ChiTietGoiVacXin",
                column: "MaDangKy");
        }
    }
}
