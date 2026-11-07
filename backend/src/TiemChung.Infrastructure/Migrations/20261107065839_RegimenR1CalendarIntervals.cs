using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RegimenR1CalendarIntervals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnchorDoseOrder",
                table: "VaccineRegimenDose",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntervalUnit",
                table: "VaccineRegimenDose",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntervalValue",
                table: "VaccineRegimenDose",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_VaccineRegimenDose_CalendarInterval",
                table: "VaccineRegimenDose",
                sql: "([AnchorDoseOrder] IS NULL AND [IntervalValue] IS NULL AND [IntervalUnit] IS NULL)\nOR ([AnchorDoseOrder] IS NOT NULL AND [IntervalValue] IS NOT NULL AND [IntervalUnit] IS NOT NULL\n    AND [AnchorDoseOrder] >= 1 AND [AnchorDoseOrder] < [DoseOrder] AND [IntervalValue] > 0\n    AND [IntervalUnit] COLLATE Latin1_General_100_BIN2 IN (N'Days', N'Weeks', N'Months', N'Years')\n    AND DATALENGTH([IntervalUnit]) = 2 * LEN([IntervalUnit])\n    AND [MinimumIntervalDays] IS NULL AND [RecommendedIntervalDays] IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [VaccineRegimenDose] WHERE [AnchorDoseOrder] IS NOT NULL) THROW 51000, 'Structured regimen intervals exist; dropping these columns would lose data.', 1;");

            migrationBuilder.DropCheckConstraint(
                name: "CK_VaccineRegimenDose_CalendarInterval",
                table: "VaccineRegimenDose");

            migrationBuilder.DropColumn(
                name: "AnchorDoseOrder",
                table: "VaccineRegimenDose");

            migrationBuilder.DropColumn(
                name: "IntervalUnit",
                table: "VaccineRegimenDose");

            migrationBuilder.DropColumn(
                name: "IntervalValue",
                table: "VaccineRegimenDose");
        }
    }
}
