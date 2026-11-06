using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RF05FAppointmentLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NhacLich_Simulation",
                table: "NhacLich");

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledAt",
                table: "NhacLich",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "LichHenTiem",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            // Before RF-05F the application could not reschedule: preserve each legacy log's original instant.
            migrationBuilder.Sql("UPDATE n SET ScheduledAt=a.ThoiGianHen FROM NhacLich n JOIN LichHenTiem a ON a.MaLichHen=n.MaLichHen WHERE n.KenhGui=N'LogMoPhong';");

            migrationBuilder.CreateIndex(
                name: "IX_NhacLich_Simulation",
                table: "NhacLich",
                columns: new[] { "MaLichHen", "ScheduledAt" },
                unique: true,
                filter: "[KenhGui] = N'LogMoPhong'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM SecurityAudit WHERE Action IN ('appointments.rescheduled','appointments.cancelled'))
                   OR EXISTS (SELECT 1 FROM LichHenTiem WHERE TrangThai=N'DaHuy')
                   OR EXISTS (SELECT 1 FROM NhacLich WHERE KenhGui=N'LogMoPhong' GROUP BY MaLichHen HAVING COUNT(*)>1)
                    THROW 51000, 'RF05F lifecycle history requires a backup restore; downgrade refused.', 1;
                """);
            migrationBuilder.DropIndex(
                name: "IX_NhacLich_Simulation",
                table: "NhacLich");

            migrationBuilder.DropColumn(
                name: "ScheduledAt",
                table: "NhacLich");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "LichHenTiem");

            migrationBuilder.CreateIndex(
                name: "IX_NhacLich_Simulation",
                table: "NhacLich",
                column: "MaLichHen",
                unique: true,
                filter: "[KenhGui] = N'LogMoPhong'");
        }
    }
}
