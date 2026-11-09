using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PAYR1PayOsPaymentPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ThanhToan_NhanVien",
                table: "ThanhToan");

            migrationBuilder.AddColumn<string>(
                name: "PayOsCheckoutUrl",
                table: "ThanhToan",
                type: "nvarchar(2048)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayOsLifecycleState",
                table: "ThanhToan",
                type: "nvarchar(30)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PayOsOrderCode",
                table: "ThanhToan",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayOsPaymentLinkId",
                table: "ThanhToan",
                type: "nvarchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayOsQrCode",
                table: "ThanhToan",
                type: "nvarchar(2048)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayOsReference",
                table: "ThanhToan",
                type: "nvarchar(100)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThanhToan_PayOsOrderCode",
                table: "ThanhToan",
                column: "PayOsOrderCode",
                unique: true,
                filter: "[PayOsOrderCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhToan_PayOsPaymentLinkId",
                table: "ThanhToan",
                column: "PayOsPaymentLinkId",
                unique: true,
                filter: "[PayOsPaymentLinkId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThanhToan_NhanVien",
                table: "ThanhToan",
                sql: "([PhuongThuc] IN ('MoPhong','TienMat','TheTaiQuay','ChuyenKhoanQR') AND [MaNhanVien] IS NOT NULL AND [TrangThai] = 'ThanhCong') OR ([PhuongThuc] IN ('TheQuocTe','TheNoiDia') AND [MaNhanVien] IS NULL AND [TrangThai] = 'ThanhCong' AND [DongYDieuKhoanLuc] IS NOT NULL) OR ([PhuongThuc] = 'ChuyenKhoanOnline' AND [DongYDieuKhoanLuc] IS NOT NULL AND (([TrangThai] = 'ChoXacNhan' AND [MaNhanVien] IS NULL) OR ([TrangThai] = 'ThanhCong' AND [MaNhanVien] IS NOT NULL))) OR ([PhuongThuc] = 'PayOsQr' AND [MaNhanVien] IS NULL AND [DongYDieuKhoanLuc] IS NOT NULL AND (([TrangThai] = 'ChoXacNhan' AND [PayOsLifecycleState] IN ('Creating','CreateOutcomeUnknown','Pending')) OR ([TrangThai] = 'ThanhCong' AND [PayOsLifecycleState] = 'Paid')))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThanhToan_PayOsFields",
                table: "ThanhToan",
                sql: "([PhuongThuc] = 'PayOsQr') OR ([PayOsOrderCode] IS NULL AND [PayOsPaymentLinkId] IS NULL AND [PayOsLifecycleState] IS NULL AND [PayOsCheckoutUrl] IS NULL AND [PayOsQrCode] IS NULL AND [PayOsReference] IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThanhToan_PayOsQr",
                table: "ThanhToan",
                sql: "([PhuongThuc] <> 'PayOsQr' OR ([LoaiThanhToan] = 'ToanBo' AND [SoTien] > 0 AND [PayOsOrderCode] IS NOT NULL AND [PayOsOrderCode] > 0 AND [PayOsLifecycleState] IS NOT NULL AND [PayOsLifecycleState] IN ('Creating','CreateOutcomeUnknown','Pending','Paid') AND NULLIF(LTRIM(RTRIM([TenNguoiThanhToan])),N'') IS NOT NULL AND NULLIF(LTRIM(RTRIM([SoDienThoaiThanhToan])),N'') IS NOT NULL AND NULLIF(LTRIM(RTRIM([TinhThanhThanhToan])),N'') IS NOT NULL AND NULLIF(LTRIM(RTRIM([PhuongXaThanhToan])),N'') IS NOT NULL AND NULLIF(LTRIM(RTRIM([DiaChiThanhToan])),N'') IS NOT NULL AND ([PayOsPaymentLinkId] IS NULL OR NULLIF(LTRIM(RTRIM([PayOsPaymentLinkId])),N'') IS NOT NULL) AND ([PayOsCheckoutUrl] IS NULL OR NULLIF(LTRIM(RTRIM([PayOsCheckoutUrl])),N'') IS NOT NULL) AND ([PayOsQrCode] IS NULL OR NULLIF(LTRIM(RTRIM([PayOsQrCode])),N'') IS NOT NULL) AND ([PayOsReference] IS NULL OR NULLIF(LTRIM(RTRIM([PayOsReference])),N'') IS NOT NULL) AND (([PayOsLifecycleState] IN ('Creating','CreateOutcomeUnknown') AND [TrangThai] = 'ChoXacNhan') OR ([PayOsLifecycleState] = 'Pending' AND [TrangThai] = 'ChoXacNhan' AND NULLIF(LTRIM(RTRIM([PayOsPaymentLinkId])),N'') IS NOT NULL AND NULLIF(LTRIM(RTRIM([PayOsCheckoutUrl])),N'') IS NOT NULL AND NULLIF(LTRIM(RTRIM([PayOsQrCode])),N'') IS NOT NULL) OR ([PayOsLifecycleState] = 'Paid' AND [TrangThai] = 'ThanhCong' AND NULLIF(LTRIM(RTRIM([PayOsPaymentLinkId])),N'') IS NOT NULL AND NULLIF(LTRIM(RTRIM([PayOsReference])),N'') IS NOT NULL))))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF EXISTS (SELECT 1 FROM [ThanhToan] WHERE [PhuongThuc] = N'PayOsQr') " +
                "THROW 51006, N'Cannot remove PayOS payment history.', 1;");

            migrationBuilder.DropIndex(
                name: "IX_ThanhToan_PayOsOrderCode",
                table: "ThanhToan");

            migrationBuilder.DropIndex(
                name: "IX_ThanhToan_PayOsPaymentLinkId",
                table: "ThanhToan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ThanhToan_NhanVien",
                table: "ThanhToan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ThanhToan_PayOsFields",
                table: "ThanhToan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ThanhToan_PayOsQr",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "PayOsCheckoutUrl",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "PayOsLifecycleState",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "PayOsOrderCode",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "PayOsPaymentLinkId",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "PayOsQrCode",
                table: "ThanhToan");

            migrationBuilder.DropColumn(
                name: "PayOsReference",
                table: "ThanhToan");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ThanhToan_NhanVien",
                table: "ThanhToan",
                sql: "([PhuongThuc] IN ('MoPhong','TienMat','TheTaiQuay','ChuyenKhoanQR') AND [MaNhanVien] IS NOT NULL AND [TrangThai] = 'ThanhCong') OR ([PhuongThuc] IN ('TheQuocTe','TheNoiDia') AND [MaNhanVien] IS NULL AND [TrangThai] = 'ThanhCong' AND [DongYDieuKhoanLuc] IS NOT NULL) OR ([PhuongThuc] = 'ChuyenKhoanOnline' AND [DongYDieuKhoanLuc] IS NOT NULL AND (([TrangThai] = 'ChoXacNhan' AND [MaNhanVien] IS NULL) OR ([TrangThai] = 'ThanhCong' AND [MaNhanVien] IS NOT NULL)))");
        }
    }
}
