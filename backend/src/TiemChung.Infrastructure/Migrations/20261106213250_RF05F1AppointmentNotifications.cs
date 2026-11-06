using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RF05F1AppointmentNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentNotification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentId = table.Column<int>(type: "int", nullable: false),
                    AppointmentVersion = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RecipientAccountId = table.Column<int>(type: "int", nullable: true),
                    RecipientEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedByAccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DispatchStarted = table.Column<bool>(type: "bit", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastErrorCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentNotification", x => x.Id);
                    table.CheckConstraint("CK_AppointmentNotification_Event", "[EventType] IN ('AppointmentConfirmed','AppointmentRescheduled','AppointmentCancelled') AND [Channel]='Email'");
                    table.CheckConstraint("CK_AppointmentNotification_Status", "[Status] IN ('ChoGui','DaGui','GuiLoi') AND [Attempts]>=0 AND (([Status]='DaGui' AND [DeliveredAt] IS NOT NULL) OR ([Status]<>'DaGui' AND [DeliveredAt] IS NULL))");
                    table.ForeignKey(
                        name: "FK_AppointmentNotification_LichHenTiem_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "LichHenTiem",
                        principalColumn: "MaLichHen",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentNotification_TaiKhoan_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentNotification_TaiKhoan_RecipientAccountId",
                        column: x => x.RecipientAccountId,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentNotification_AppointmentId_AppointmentVersion",
                table: "AppointmentNotification",
                columns: new[] { "AppointmentId", "AppointmentVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentNotification_CreatedByAccountId",
                table: "AppointmentNotification",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentNotification_RecipientAccountId",
                table: "AppointmentNotification",
                column: "RecipientAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentNotification_Status_Id",
                table: "AppointmentNotification",
                columns: new[] { "Status", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM AppointmentNotification) THROW 51000, 'Appointment notification history requires a backup restore; downgrade refused.', 1;");
            migrationBuilder.DropTable(
                name: "AppointmentNotification");
        }
    }
}
