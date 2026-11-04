using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RecipientPublicProfileCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MaHoSo",
                table: "NguoiDuocTiem",
                type: "varchar(16)",
                unicode: false,
                maxLength: 16,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE dbo.NguoiDuocTiem
                SET MaHoSo = 'VAC-' + UPPER(LEFT(REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), 12));
                """);

            migrationBuilder.AlterColumn<string>(
                name: "MaHoSo",
                table: "NguoiDuocTiem",
                type: "varchar(16)",
                unicode: false,
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(16)",
                oldUnicode: false,
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDuocTiem_MaHoSo",
                table: "NguoiDuocTiem",
                column: "MaHoSo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NguoiDuocTiem_MaHoSo",
                table: "NguoiDuocTiem");

            migrationBuilder.DropColumn(
                name: "MaHoSo",
                table: "NguoiDuocTiem");
        }
    }
}
