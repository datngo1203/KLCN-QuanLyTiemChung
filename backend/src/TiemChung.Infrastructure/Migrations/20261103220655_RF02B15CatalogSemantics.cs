using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RF02B15CatalogSemantics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Retire only the unverified classifications; never map them to technologies.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.foreign_keys
                    WHERE referenced_object_id IN (OBJECT_ID(N'LoaiTiemChung'), OBJECT_ID(N'LoaiVacXin'))
                      AND name NOT IN (N'FK_VacXin_LoaiTiemChung_MaLoaiTiemChung', N'FK_VacXin_LoaiVacXin_MaLoaiVacXin'))
                    THROW 51000, 'Unexpected legacy taxonomy dependency; coordinator review required.', 1;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_VacXin_LoaiTiemChung_MaLoaiTiemChung",
                table: "VacXin");

            migrationBuilder.DropForeignKey(
                name: "FK_VacXin_LoaiVacXin_MaLoaiVacXin",
                table: "VacXin");

            migrationBuilder.DropTable(
                name: "LoaiTiemChung");

            migrationBuilder.DropTable(
                name: "LoaiVacXin");

            migrationBuilder.DropIndex(
                name: "IX_VacXin_MaLoaiTiemChung",
                table: "VacXin");

            migrationBuilder.DropIndex(
                name: "IX_VacXin_MaLoaiVacXin",
                table: "VacXin");

            migrationBuilder.DropColumn(
                name: "MaLoaiTiemChung",
                table: "VacXin");

            migrationBuilder.DropColumn(
                name: "MaLoaiVacXin",
                table: "VacXin");

            migrationBuilder.CreateTable(
                name: "CongNgheVacXin",
                columns: table => new
                {
                    MaCongNghe = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, collation: "Latin1_General_100_CI_AS"),
                    TenCongNghe = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false, collation: "Latin1_General_100_CI_AS"),
                    MoTa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DacDiemKyThuat = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    GhiChuChuyenMon = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NguonThongTin = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DuongDanNguon = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NgayCapNhatThongTin = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongNgheVacXin", x => x.MaCongNghe);
                    table.CheckConstraint("CK_CongNgheVacXin_Status", "[TrangThai] IN ('Active','Inactive')");
                });

            migrationBuilder.CreateTable(
                name: "VacXin_CongNghe",
                columns: table => new
                {
                    MaVacXin = table.Column<int>(type: "int", nullable: false),
                    MaCongNghe = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacXin_CongNghe", x => new { x.MaVacXin, x.MaCongNghe });
                    table.ForeignKey(
                        name: "FK_VacXin_CongNghe_CongNgheVacXin_MaCongNghe",
                        column: x => x.MaCongNghe,
                        principalTable: "CongNgheVacXin",
                        principalColumn: "MaCongNghe",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VacXin_CongNghe_VacXin_MaVacXin",
                        column: x => x.MaVacXin,
                        principalTable: "VacXin",
                        principalColumn: "MaVacXin",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CongNgheVacXin_Code",
                table: "CongNgheVacXin",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CongNgheVacXin_TenCongNghe",
                table: "CongNgheVacXin",
                column: "TenCongNghe",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VacXin_CongNghe_MaCongNghe",
                table: "VacXin_CongNghe",
                column: "MaCongNghe");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removed assignments cannot be reconstructed without inventing semantics.
            migrationBuilder.Sql("THROW 51000, 'RF02B15 semantic retirement is irreversible; restore a pre-migration backup instead of guessing classifications.', 1;");
        }
    }
}
