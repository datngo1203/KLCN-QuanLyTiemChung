using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M1Identity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Abort before DDL if legacy data cannot be mapped without guessing.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM TaiKhoan GROUP BY UPPER(LTRIM(RTRIM(TenDangNhap))) HAVING COUNT(*)>1)
                    THROW 51000, 'M1 preflight: duplicate normalized usernames; operator resolution required.', 1;
                IF EXISTS (SELECT 1 FROM KhachHang GROUP BY CCCD HAVING COUNT(*)>1)
                    THROW 51000, 'M1 preflight: duplicate customer identity; operator resolution required.', 1;
                IF EXISTS (SELECT 1 FROM NhanVien WHERE ChucDanh NOT IN ('Admin','LeTan','BacSi','DieuDuong','ThuNgan','ThuKho'))
                    THROW 51000, 'M1 preflight: unsupported employee title.', 1;
                IF EXISTS (SELECT 1 FROM NhanVien WHERE TrangThai NOT IN ('Active','Inactive'))
                    OR EXISTS (SELECT 1 FROM TaiKhoan WHERE TrangThai NOT IN ('Active','Locked'))
                    THROW 51000, 'M1 preflight: legacy status mapping requires operator resolution.', 1;
                IF EXISTS (SELECT 1 FROM TaiKhoan WHERE LEN(LTRIM(RTRIM(TenDangNhap))) NOT BETWEEN 4 AND 100
                    OR LTRIM(RTRIM(TenDangNhap)) COLLATE Latin1_General_100_BIN2 LIKE '%[^-a-zA-Z0-9._]%')
                    THROW 51000, 'M1 preflight: unsupported username.', 1;
                IF EXISTS (SELECT 1 FROM QuyenHan WHERE TenQuyen NOT IN
                    ('XemTongQuan','QuanLyLichHen','GuiNhacLich','QuanLyKhuyenMai','XemBaoCao','XemThanhToan',
                     'GhiThanhToan','QuanLyTaiKhoan','QuanLyDanhMucChuyenMon','QuanLyDanhMucGia','XemKho','XemHoSo'))
                    THROW 51000, 'M1 preflight: permission mapping requires review.', 1;
                IF EXISTS (SELECT 1 FROM QuyenHan GROUP BY TenQuyen HAVING COUNT(*)>1)
                    THROW 51000, 'M1 preflight: duplicate permission mapping.', 1;
                """);
            migrationBuilder.DropIndex(
                name: "IX_TaiKhoan_MaNhanVien",
                table: "TaiKhoan");

            migrationBuilder.AlterColumn<int>(
                name: "MaNhanVien",
                table: "TaiKhoan",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "AccountType",
                table: "TaiKhoan",
                type: "varchar(16)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AdministrativeDuty",
                table: "TaiKhoan",
                type: "varchar(16)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "TaiKhoan",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EmailVerifiedAt",
                table: "TaiKhoan",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoginEmail",
                table: "TaiKhoan",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "TaiKhoan",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedUsername",
                table: "TaiKhoan",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TaiKhoan",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<long>(
                name: "SecurityVersion",
                table: "TaiKhoan",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "QuyenHan",
                type: "varchar(64)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AccountProfileLink",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    RecipientId = table.Column<int>(type: "int", nullable: true),
                    State = table.Column<string>(type: "varchar(16)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountProfileLink", x => x.Id);
                    table.CheckConstraint("CK_ProfileLink_State", "[State] IN ('Active','Revoked')");
                    table.CheckConstraint("CK_ProfileLink_Target", "([CustomerId] IS NULL AND [RecipientId] IS NOT NULL) OR ([CustomerId] IS NOT NULL AND [RecipientId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_AccountProfileLink_KhachHang_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "KhachHang",
                        principalColumn: "MaKhachHang",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountProfileLink_NguoiDuocTiem_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "NguoiDuocTiem",
                        principalColumn: "MaNguoiDuocTiem",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountProfileLink_TaiKhoan_AccountId",
                        column: x => x.AccountId,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmailVerification",
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
                    table.PrimaryKey("PK_EmailVerification", x => x.Id);
                    table.CheckConstraint("CK_EmailVerification_Expiry", "[ExpiresAt]>[CreatedAt]");
                    table.ForeignKey(
                        name: "FK_EmailVerification_TaiKhoan_AccountId",
                        column: x => x.AccountId,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdentityRecoveryDatabase",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    RecoveryDatabaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    M1UpgradeEpoch = table.Column<string>(type: "varchar(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityRecoveryDatabase", x => x.Id);
                    table.CheckConstraint("CK_RecoveryDatabase_Singleton", "[Id]=1 AND [M1UpgradeEpoch]='M1'");
                });

            migrationBuilder.CreateTable(
                name: "IdentityRecoveryOperation",
                columns: table => new
                {
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecoveryDatabaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    M1UpgradeEpoch = table.Column<string>(type: "varchar(16)", nullable: false),
                    ManifestFingerprint = table.Column<byte[]>(type: "binary(32)", nullable: false),
                    TargetAccountId = table.Column<int>(type: "int", nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OperatorIdentity = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ResultCode = table.Column<string>(type: "varchar(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityRecoveryOperation", x => x.OperationId);
                    table.ForeignKey(
                        name: "FK_IdentityRecoveryOperation_TaiKhoan_TargetAccountId",
                        column: x => x.TargetAccountId,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdentitySession",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    SecurityVersion = table.Column<long>(type: "bigint", nullable: false),
                    ClientType = table.Column<string>(type: "varchar(8)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevocationReason = table.Column<string>(type: "varchar(40)", nullable: true),
                    CsrfSecretHash = table.Column<byte[]>(type: "binary(32)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentitySession", x => x.Id);
                    table.CheckConstraint("CK_IdentitySession_Client", "[ClientType] IN ('web','native')");
                    table.CheckConstraint("CK_IdentitySession_Expiry", "[ExpiresAt]>[CreatedAt] AND [SecurityVersion]>0");
                    table.ForeignKey(
                        name: "FK_IdentitySession_TaiKhoan_AccountId",
                        column: x => x.AccountId,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityAudit",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActorAccountId = table.Column<int>(type: "int", nullable: true),
                    SubjectAccountId = table.Column<int>(type: "int", nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "varchar(64)", nullable: false),
                    Outcome = table.Column<string>(type: "varchar(16)", nullable: false),
                    ResourceType = table.Column<string>(type: "varchar(32)", nullable: true),
                    ResourceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TraceId = table.Column<string>(type: "varchar(64)", nullable: false),
                    ChangedFields = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OperatorIdentity = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAudit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityAudit_TaiKhoan_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecurityAudit_TaiKhoan_SubjectAccountId",
                        column: x => x.SubjectAccountId,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdentityMailOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EncryptedPayload = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastErrorCode = table.Column<string>(type: "varchar(40)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityMailOutbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdentityMailOutbox_EmailVerification_VerificationId",
                        column: x => x.VerificationId,
                        principalTable: "EmailVerification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshCredential",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<byte[]>(type: "binary(32)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReplacedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshCredential", x => x.Id);
                    table.CheckConstraint("CK_RefreshCredential_Expiry", "[ExpiresAt]>[CreatedAt]");
                    table.ForeignKey(
                        name: "FK_RefreshCredential_IdentitySession_SessionId",
                        column: x => x.SessionId,
                        principalTable: "IdentitySession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefreshCredential_RefreshCredential_ReplacedById",
                        column: x => x.ReplacedById,
                        principalTable: "RefreshCredential",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                UPDATE TaiKhoan SET AccountType='Employee', AdministrativeDuty='None',
                    NormalizedUsername=UPPER(LTRIM(RTRIM(TenDangNhap))), SecurityVersion=1, CreatedAt=SYSUTCDATETIME();
                UPDATE a SET TrangThai='Locked' FROM TaiKhoan a JOIN NhanVien n ON a.MaNhanVien=n.MaNhanVien WHERE n.ChucDanh='Admin';
                UPDATE QuyenHan SET Code=TenQuyen;
                INSERT INTO IdentityRecoveryDatabase (Id,RecoveryDatabaseId,M1UpgradeEpoch) VALUES (1,NEWID(),'M1');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_MaNhanVien",
                table: "TaiKhoan",
                column: "MaNhanVien",
                unique: true,
                filter: "[MaNhanVien] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_NormalizedEmail",
                table: "TaiKhoan",
                column: "NormalizedEmail",
                unique: true,
                filter: "[NormalizedEmail] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_NormalizedUsername",
                table: "TaiKhoan",
                column: "NormalizedUsername",
                unique: true,
                filter: "[NormalizedUsername] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TaiKhoan_Duty",
                table: "TaiKhoan",
                sql: "[AdministrativeDuty] IN ('None','Technical','Director') AND [SecurityVersion]>0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TaiKhoan_Type",
                table: "TaiKhoan",
                sql: "([AccountType]='Customer' AND [MaNhanVien] IS NULL AND [LoginEmail] IS NOT NULL AND [NormalizedEmail] IS NOT NULL AND [NormalizedUsername] IS NULL AND [AdministrativeDuty]='None') OR ([AccountType]='Employee' AND [MaNhanVien] IS NOT NULL AND [NormalizedUsername] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_QuyenHan_Code",
                table: "QuyenHan",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KhachHang_CCCD",
                table: "KhachHang",
                column: "CCCD",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountProfileLink_AccountId_State",
                table: "AccountProfileLink",
                columns: new[] { "AccountId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountProfileLink_CustomerId",
                table: "AccountProfileLink",
                column: "CustomerId",
                unique: true,
                filter: "[State]='Active' AND [CustomerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountProfileLink_RecipientId",
                table: "AccountProfileLink",
                column: "RecipientId",
                unique: true,
                filter: "[State]='Active' AND [RecipientId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileLink_AccountCustomer",
                table: "AccountProfileLink",
                column: "AccountId",
                unique: true,
                filter: "[State]='Active' AND [CustomerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileLink_AccountRecipient",
                table: "AccountProfileLink",
                column: "AccountId",
                unique: true,
                filter: "[State]='Active' AND [RecipientId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerification_AccountId",
                table: "EmailVerification",
                column: "AccountId",
                unique: true,
                filter: "[UsedAt] IS NULL AND [InvalidatedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerification_TokenHash",
                table: "EmailVerification",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentityMailOutbox_VerificationId",
                table: "IdentityMailOutbox",
                column: "VerificationId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityRecoveryOperation_RecoveryDatabaseId_M1UpgradeEpoch",
                table: "IdentityRecoveryOperation",
                columns: new[] { "RecoveryDatabaseId", "M1UpgradeEpoch" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentityRecoveryOperation_TargetAccountId",
                table: "IdentityRecoveryOperation",
                column: "TargetAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentitySession_AccountId_ExpiresAt",
                table: "IdentitySession",
                columns: new[] { "AccountId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshCredential_ReplacedById",
                table: "RefreshCredential",
                column: "ReplacedById");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshCredential_SessionId",
                table: "RefreshCredential",
                column: "SessionId",
                unique: true,
                filter: "[ConsumedAt] IS NULL AND [RevokedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshCredential_TokenHash",
                table: "RefreshCredential",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAudit_ActorAccountId",
                table: "SecurityAudit",
                column: "ActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAudit_SubjectAccountId",
                table: "SecurityAudit",
                column: "SubjectAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM TaiKhoan) OR EXISTS (SELECT 1 FROM SecurityAudit)
                   OR EXISTS (SELECT 1 FROM AccountProfileLink) OR EXISTS (SELECT 1 FROM IdentityRecoveryOperation)
                    THROW 51000, 'M1 downgrade refused: use reviewed maintenance backup restore.', 1;
                """);
            migrationBuilder.DropTable(
                name: "AccountProfileLink");

            migrationBuilder.DropTable(
                name: "IdentityMailOutbox");

            migrationBuilder.DropTable(
                name: "IdentityRecoveryDatabase");

            migrationBuilder.DropTable(
                name: "IdentityRecoveryOperation");

            migrationBuilder.DropTable(
                name: "RefreshCredential");

            migrationBuilder.DropTable(
                name: "SecurityAudit");

            migrationBuilder.DropTable(
                name: "EmailVerification");

            migrationBuilder.DropTable(
                name: "IdentitySession");

            migrationBuilder.DropIndex(
                name: "IX_TaiKhoan_MaNhanVien",
                table: "TaiKhoan");

            migrationBuilder.DropIndex(
                name: "IX_TaiKhoan_NormalizedEmail",
                table: "TaiKhoan");

            migrationBuilder.DropIndex(
                name: "IX_TaiKhoan_NormalizedUsername",
                table: "TaiKhoan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TaiKhoan_Duty",
                table: "TaiKhoan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TaiKhoan_Type",
                table: "TaiKhoan");

            migrationBuilder.DropIndex(
                name: "IX_QuyenHan_Code",
                table: "QuyenHan");

            migrationBuilder.DropIndex(
                name: "IX_KhachHang_CCCD",
                table: "KhachHang");

            migrationBuilder.DropColumn(
                name: "AccountType",
                table: "TaiKhoan");

            migrationBuilder.DropColumn(
                name: "AdministrativeDuty",
                table: "TaiKhoan");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TaiKhoan");

            migrationBuilder.DropColumn(
                name: "EmailVerifiedAt",
                table: "TaiKhoan");

            migrationBuilder.DropColumn(
                name: "LoginEmail",
                table: "TaiKhoan");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "TaiKhoan");

            migrationBuilder.DropColumn(
                name: "NormalizedUsername",
                table: "TaiKhoan");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TaiKhoan");

            migrationBuilder.DropColumn(
                name: "SecurityVersion",
                table: "TaiKhoan");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "QuyenHan");

            migrationBuilder.AlterColumn<int>(
                name: "MaNhanVien",
                table: "TaiKhoan",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_MaNhanVien",
                table: "TaiKhoan",
                column: "MaNhanVien",
                unique: true);
        }
    }
}
