using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S5ClinicalAssessment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM ChiDinhTiem GROUP BY MaPhieuKham, MaVacXin HAVING COUNT(*) > 1) THROW 51000, 'Duplicate legacy indications require data review before S5.3 migration.', 1;");
            migrationBuilder.DropIndex(
                name: "IX_ChiDinhTiem_MaPhieuKham",
                table: "ChiDinhTiem");

            migrationBuilder.AlterColumn<string>(
                name: "TienSuBenhLy",
                table: "PhieuKhamSangLoc",
                type: "nvarchar(1000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)");

            migrationBuilder.AlterColumn<string>(
                name: "ChiSoSinhTon",
                table: "PhieuKhamSangLoc",
                type: "nvarchar(500)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)");

            migrationBuilder.AddColumn<int>(
                name: "MaLichHen",
                table: "PhieuKhamSangLoc",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ThoiDiemKham",
                table: "PhieuKhamSangLoc",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKhamSangLoc_MaLichHen",
                table: "PhieuKhamSangLoc",
                column: "MaLichHen",
                unique: true,
                filter: "[MaLichHen] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhieuKhamSangLoc_AppointmentTime",
                table: "PhieuKhamSangLoc",
                sql: "([MaLichHen] IS NULL AND [ThoiDiemKham] IS NULL) OR ([MaLichHen] IS NOT NULL AND [ThoiDiemKham] IS NOT NULL AND DATEPART(TZOFFSET, [ThoiDiemKham]) = 0)");

            migrationBuilder.CreateIndex(
                name: "IX_LichHenTiem_MaLichHen_MaDangKy_MaNguoiDuocTiem",
                table: "LichHenTiem",
                columns: new[] { "MaLichHen", "MaDangKy", "MaNguoiDuocTiem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChiDinhTiem_MaPhieuKham_MaVacXin",
                table: "ChiDinhTiem",
                columns: new[] { "MaPhieuKham", "MaVacXin" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PhieuKhamSangLoc_LichHenTiem_MaLichHen",
                table: "PhieuKhamSangLoc",
                column: "MaLichHen",
                principalTable: "LichHenTiem",
                principalColumn: "MaLichHen",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_PhieuKhamSangLoc_AppointmentSource",
                table: "PhieuKhamSangLoc",
                columns: new[] { "MaLichHen", "MaDangKy", "MaNguoiDuocTiem" },
                principalTable: "LichHenTiem",
                principalColumns: new[] { "MaLichHen", "MaDangKy", "MaNguoiDuocTiem" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM PhieuKhamSangLoc WHERE MaLichHen IS NOT NULL OR ThoiDiemKham IS NOT NULL OR ChiSoSinhTon IS NULL OR TienSuBenhLy IS NULL) THROW 51000, 'S5.3 assessments or uncollected data require review before downgrade.', 1;");
            migrationBuilder.DropForeignKey(name: "FK_PhieuKhamSangLoc_AppointmentSource", table: "PhieuKhamSangLoc");
            migrationBuilder.DropForeignKey(
                name: "FK_PhieuKhamSangLoc_LichHenTiem_MaLichHen",
                table: "PhieuKhamSangLoc");

            migrationBuilder.DropIndex(
                name: "IX_PhieuKhamSangLoc_MaLichHen",
                table: "PhieuKhamSangLoc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhieuKhamSangLoc_AppointmentTime",
                table: "PhieuKhamSangLoc");

            migrationBuilder.DropIndex(
                name: "IX_LichHenTiem_MaLichHen_MaDangKy_MaNguoiDuocTiem",
                table: "LichHenTiem");

            migrationBuilder.DropIndex(
                name: "IX_ChiDinhTiem_MaPhieuKham_MaVacXin",
                table: "ChiDinhTiem");

            migrationBuilder.DropColumn(
                name: "MaLichHen",
                table: "PhieuKhamSangLoc");

            migrationBuilder.DropColumn(
                name: "ThoiDiemKham",
                table: "PhieuKhamSangLoc");

            migrationBuilder.AlterColumn<string>(
                name: "TienSuBenhLy",
                table: "PhieuKhamSangLoc",
                type: "nvarchar(1000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ChiSoSinhTon",
                table: "PhieuKhamSangLoc",
                type: "nvarchar(500)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChiDinhTiem_MaPhieuKham",
                table: "ChiDinhTiem",
                column: "MaPhieuKham");
        }
    }
}
