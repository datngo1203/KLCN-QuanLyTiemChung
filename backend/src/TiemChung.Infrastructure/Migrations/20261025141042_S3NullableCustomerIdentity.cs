using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S3NullableCustomerIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_ProfileLink_AccountRecipient", table: "AccountProfileLink");
            migrationBuilder.CreateIndex(name: "IX_ProfileLink_AccountRecipient", table: "AccountProfileLink", column: "AccountId", filter: "[State]='Active' AND [RecipientId] IS NOT NULL");
            migrationBuilder.DropIndex(
                name: "IX_KhachHang_CCCD",
                table: "KhachHang");

            migrationBuilder.AlterColumn<string>(
                name: "CCCD",
                table: "KhachHang",
                type: "varchar(12)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(12)");

            migrationBuilder.CreateIndex(
                name: "IX_KhachHang_CCCD",
                table: "KhachHang",
                column: "CCCD",
                unique: true,
                filter: "[CCCD] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_ProfileLink_AccountRecipient", table: "AccountProfileLink");
            migrationBuilder.CreateIndex(name: "IX_ProfileLink_AccountRecipient", table: "AccountProfileLink", column: "AccountId", unique: true, filter: "[State]='Active' AND [RecipientId] IS NOT NULL");
            migrationBuilder.DropIndex(
                name: "IX_KhachHang_CCCD",
                table: "KhachHang");

            migrationBuilder.AlterColumn<string>(
                name: "CCCD",
                table: "KhachHang",
                type: "varchar(12)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_KhachHang_CCCD",
                table: "KhachHang",
                column: "CCCD",
                unique: true);
        }
    }
}
