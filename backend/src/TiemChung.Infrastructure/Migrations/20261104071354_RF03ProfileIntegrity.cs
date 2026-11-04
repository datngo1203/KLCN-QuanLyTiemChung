using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RF03ProfileIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DanhXung",
                table: "KhachHang",
                type: "nvarchar(10)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NgaySinh",
                table: "KhachHang",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DanhXung",
                table: "KhachHang");

            migrationBuilder.DropColumn(
                name: "NgaySinh",
                table: "KhachHang");
        }
    }
}
