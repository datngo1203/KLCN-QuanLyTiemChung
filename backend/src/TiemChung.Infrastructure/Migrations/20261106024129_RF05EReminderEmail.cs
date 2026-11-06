using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RF05EReminderEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReminderEmail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentId = table.Column<int>(type: "int", nullable: false),
                    RegistrationId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    RecipientId = table.Column<int>(type: "int", nullable: false),
                    ItemKind = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RegistrationItemId = table.Column<int>(type: "int", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RecipientName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RecipientAccountId = table.Column<int>(type: "int", nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedByAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastErrorCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    DispatchStarted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderEmail", x => x.Id);
                    table.CheckConstraint("CK_ReminderEmail_Status", "[Status] IN ('ChoGui','DaGui','GuiLoi') AND [Attempts]>=0 AND (([Status]='DaGui' AND [DeliveredAt] IS NOT NULL) OR ([Status]<>'DaGui' AND [DeliveredAt] IS NULL))");
                    table.ForeignKey(
                        name: "FK_ReminderEmail_LichHenTiem_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "LichHenTiem",
                        principalColumn: "MaLichHen",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReminderEmail_NhanVien_CreatedByEmployeeId",
                        column: x => x.CreatedByEmployeeId,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReminderEmail_TaiKhoan_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReminderEmail_TaiKhoan_RecipientAccountId",
                        column: x => x.RecipientAccountId,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReminderEmail_AppointmentId_RegistrationId_RecipientId_ItemKind_RegistrationItemId_ScheduledAt",
                table: "ReminderEmail",
                columns: new[] { "AppointmentId", "RegistrationId", "RecipientId", "ItemKind", "RegistrationItemId", "ScheduledAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReminderEmail_CreatedByAccountId",
                table: "ReminderEmail",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderEmail_CreatedByEmployeeId",
                table: "ReminderEmail",
                column: "CreatedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderEmail_RecipientAccountId",
                table: "ReminderEmail",
                column: "RecipientAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderEmail_Status_CreatedAt",
                table: "ReminderEmail",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM ReminderEmail) THROW 51000, 'Reminder email deliveries exist; restore a backup rather than dropping delivery history.', 1;");
            migrationBuilder.DropTable(
                name: "ReminderEmail");
        }
    }
}
