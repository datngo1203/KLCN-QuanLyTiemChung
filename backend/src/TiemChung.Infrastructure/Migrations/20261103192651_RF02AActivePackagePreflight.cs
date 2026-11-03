using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RF02AActivePackagePreflight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DECLARE @package int, @vaccine int;
                SELECT TOP (1) @package=p.MaGoi, @vaccine=v.MaVacXin
                FROM GoiVacXin p
                JOIN GoiVacXin_VacXin item ON item.MaGoi=p.MaGoi
                JOIN VacXin v ON v.MaVacXin=item.MaVacXin
                WHERE p.TrangThai=N'Active' AND v.TrangThai<>N'Active'
                ORDER BY p.MaGoi, v.MaVacXin;
                IF @package IS NOT NULL
                BEGIN
                    DECLARE @message nvarchar(2048)=CONCAT('RF02A: Active package ', @package,
                        ' references non-Active vaccine ', @vaccine, '; resolve with owner before migrating. No status was changed.');
                    THROW 51000, @message, 1;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
