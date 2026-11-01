using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S5ReminderSimulation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM NhacLich WHERE KenhGui=N'LogMoPhong' GROUP BY MaLichHen HAVING COUNT(*)>1) THROW 51000, 'Conflicting simulation reminders require data review before migration; no rows were changed.', 1;");
            migrationBuilder.DropIndex(
                name: "IX_NhacLich_MaLichHen",
                table: "NhacLich");

            migrationBuilder.AddColumn<int>(
                name: "MaNhanVien",
                table: "NhacLich",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoiDungMoPhong",
                table: "NhacLich",
                type: "nvarchar(1000)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NhacLich_MaNhanVien",
                table: "NhacLich",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_NhacLich_Simulation",
                table: "NhacLich",
                column: "MaLichHen",
                unique: true,
                filter: "[KenhGui] = N'LogMoPhong'");

            migrationBuilder.AddForeignKey(
                name: "FK_NhacLich_NhanVien_MaNhanVien",
                table: "NhacLich",
                column: "MaNhanVien",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM NhacLich WHERE MaNhanVien IS NOT NULL OR NoiDungMoPhong IS NOT NULL) THROW 51000, 'Reminder snapshots or recorder links require data review before downgrade; no data was discarded.', 1;");
            migrationBuilder.DropForeignKey(
                name: "FK_NhacLich_NhanVien_MaNhanVien",
                table: "NhacLich");

            migrationBuilder.DropIndex(
                name: "IX_NhacLich_MaNhanVien",
                table: "NhacLich");

            migrationBuilder.DropIndex(
                name: "IX_NhacLich_Simulation",
                table: "NhacLich");

            migrationBuilder.DropColumn(
                name: "MaNhanVien",
                table: "NhacLich");

            migrationBuilder.DropColumn(
                name: "NoiDungMoPhong",
                table: "NhacLich");

            migrationBuilder.CreateIndex(
                name: "IX_NhacLich_MaLichHen",
                table: "NhacLich",
                column: "MaLichHen");
        }
    }
}
