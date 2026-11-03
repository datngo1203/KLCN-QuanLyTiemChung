using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RF01CustomerPasswordReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "VerificationId",
                table: "IdentityMailOutbox",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "PasswordResetId",
                table: "IdentityMailOutbox",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerPasswordReset",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<byte[]>(type: "binary(32)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    InvalidatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPasswordReset", x => x.Id);
                    table.CheckConstraint("CK_CustomerPasswordReset_Expiry", "[ExpiresAt]>[CreatedAt]");
                    table.ForeignKey(
                        name: "FK_CustomerPasswordReset_TaiKhoan_AccountId",
                        column: x => x.AccountId,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityMailOutbox_PasswordResetId",
                table: "IdentityMailOutbox",
                column: "PasswordResetId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IdentityMailOutbox_Purpose",
                table: "IdentityMailOutbox",
                sql: "([VerificationId] IS NOT NULL AND [PasswordResetId] IS NULL) OR ([VerificationId] IS NULL AND [PasswordResetId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPasswordReset_AccountId",
                table: "CustomerPasswordReset",
                column: "AccountId",
                unique: true,
                filter: "[UsedAt] IS NULL AND [InvalidatedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPasswordReset_TokenHash",
                table: "CustomerPasswordReset",
                column: "TokenHash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_IdentityMailOutbox_CustomerPasswordReset_PasswordResetId",
                table: "IdentityMailOutbox",
                column: "PasswordResetId",
                principalTable: "CustomerPasswordReset",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM CustomerPasswordReset) THROW 51077, 'RF01 reset history exists; downgrade refused.', 1;");
            migrationBuilder.DropForeignKey(
                name: "FK_IdentityMailOutbox_CustomerPasswordReset_PasswordResetId",
                table: "IdentityMailOutbox");

            migrationBuilder.DropTable(
                name: "CustomerPasswordReset");

            migrationBuilder.DropIndex(
                name: "IX_IdentityMailOutbox_PasswordResetId",
                table: "IdentityMailOutbox");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IdentityMailOutbox_Purpose",
                table: "IdentityMailOutbox");

            migrationBuilder.DropColumn(
                name: "PasswordResetId",
                table: "IdentityMailOutbox");

            migrationBuilder.AlterColumn<Guid>(
                name: "VerificationId",
                table: "IdentityMailOutbox",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
