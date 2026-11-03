using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class X21InventoryReceipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM QuyenHan WHERE Code='NhapKho') INSERT INTO QuyenHan(Code,TenQuyen,MoTa) VALUES ('NhapKho',N'Nhập lô vắc xin',N'Quyền nhập kho riêng X2.1; không tự cấp cho tài khoản');");
            migrationBuilder.CreateTable(
                name: "InventoryReceipt",
                columns: table => new
                {
                    SoLo = table.Column<string>(type: "nvarchar(80)", nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    StorageLocation = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    MovementId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReceipt", x => x.SoLo);
                    table.ForeignKey(
                        name: "FK_InventoryReceipt_BienDongKho_MovementId",
                        column: x => x.MovementId,
                        principalTable: "BienDongKho",
                        principalColumn: "MaBienDong",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryReceipt_LoVacXin_SoLo",
                        column: x => x.SoLo,
                        principalTable: "LoVacXin",
                        principalColumn: "SoLo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReceipt_MovementId",
                table: "InventoryReceipt",
                column: "MovementId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM InventoryReceipt) THROW 51000, 'Inventory receipts exist; refusing destructive downgrade.', 1;");
            // Retain the permission dictionary and existing grants; downgrade must not rewrite authorization.
            migrationBuilder.DropTable(
                name: "InventoryReceipt");
        }
    }
}
