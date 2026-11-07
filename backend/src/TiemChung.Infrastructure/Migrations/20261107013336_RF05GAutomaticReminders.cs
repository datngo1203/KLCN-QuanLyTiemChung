using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RF05GAutomaticReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReminderEmail_AppointmentId_RegistrationId_RecipientId_ItemKind_RegistrationItemId_ScheduledAt",
                table: "ReminderEmail");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByEmployeeId",
                table: "ReminderEmail",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByAccountId",
                table: "ReminderEmail",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "ReminderEmail",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.CreateIndex(
                name: "UX_ReminderEmail_Snapshot_Kind",
                table: "ReminderEmail",
                columns: new[] { "AppointmentId", "RegistrationId", "RecipientId", "ItemKind", "RegistrationItemId", "ScheduledAt", "Kind" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReminderEmail_Kind",
                table: "ReminderEmail",
                sql: "([Kind]='Manual' AND [CreatedByAccountId] IS NOT NULL AND [CreatedByEmployeeId] IS NOT NULL) OR ([Kind] IN ('TMinus48','TMinus24','DPlus3','DPlus7') AND [CreatedByAccountId] IS NULL AND [CreatedByEmployeeId] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_LichHenTiem_TrangThai_ThoiGianHen_MaLichHen",
                table: "LichHenTiem",
                columns: new[] { "TrangThai", "ThoiGianHen", "MaLichHen" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM ReminderEmail WHERE Kind<>'Manual') THROW 51000, 'Automatic reminder history exists; restore a backup rather than dropping delivery evidence.', 1;");
            migrationBuilder.DropIndex(
                name: "UX_ReminderEmail_Snapshot_Kind",
                table: "ReminderEmail");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ReminderEmail_Kind",
                table: "ReminderEmail");

            migrationBuilder.DropIndex(
                name: "IX_LichHenTiem_TrangThai_ThoiGianHen_MaLichHen",
                table: "LichHenTiem");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "ReminderEmail");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByEmployeeId",
                table: "ReminderEmail",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByAccountId",
                table: "ReminderEmail",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReminderEmail_AppointmentId_RegistrationId_RecipientId_ItemKind_RegistrationItemId_ScheduledAt",
                table: "ReminderEmail",
                columns: new[] { "AppointmentId", "RegistrationId", "RecipientId", "ItemKind", "RegistrationItemId", "ScheduledAt" },
                unique: true);
        }
    }
}
