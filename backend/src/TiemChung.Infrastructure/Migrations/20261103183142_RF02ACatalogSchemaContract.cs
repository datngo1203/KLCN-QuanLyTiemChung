using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RF02ACatalogSchemaContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [LoaiTiemChung] GROUP BY [TenLoaiTiemChung] COLLATE Latin1_General_100_CI_AS HAVING COUNT(*) > 1)
                    THROW 51000, 'RF02A: duplicate legacy LoaiTiemChung names; resolve with owner before migrating.', 1;
                """);
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [LoaiVacXin] GROUP BY [TenLoaiVacXin] COLLATE Latin1_General_100_CI_AS HAVING COUNT(*) > 1)
                    THROW 51000, 'RF02A: duplicate legacy LoaiVacXin names; resolve with owner before migrating.', 1;
                """);
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [Benh] GROUP BY [TenBenh] COLLATE Latin1_General_100_CI_AS HAVING COUNT(*) > 1)
                    THROW 51000, 'RF02A: duplicate legacy Benh names; resolve with owner before migrating.', 1;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "LieuLuong",
                table: "VacXin",
                type: "nvarchar(200)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "VacXin",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                collation: "Latin1_General_100_CI_AS");

            migrationBuilder.AddColumn<string>(
                name: "DoiTuongSuDung",
                table: "VacXin",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DuongDanNguon",
                table: "VacXin",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DuongDung",
                table: "VacXin",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DuongDungKhac",
                table: "VacXin",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MoTa",
                table: "VacXin",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NgayCapNhatThongTin",
                table: "VacXin",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguonThongTin",
                table: "VacXin",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenLoaiVacXin",
                table: "LoaiVacXin",
                type: "nvarchar(120)",
                nullable: false,
                collation: "Latin1_General_100_CI_AS",
                oldClrType: typeof(string),
                oldType: "nvarchar(120)");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "LoaiVacXin",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                collation: "Latin1_General_100_CI_AS");

            migrationBuilder.AddColumn<string>(
                name: "TrangThai",
                table: "LoaiVacXin",
                type: "nvarchar(30)",
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AlterColumn<string>(
                name: "TenLoaiTiemChung",
                table: "LoaiTiemChung",
                type: "nvarchar(120)",
                nullable: false,
                collation: "Latin1_General_100_CI_AS",
                oldClrType: typeof(string),
                oldType: "nvarchar(120)");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "LoaiTiemChung",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                collation: "Latin1_General_100_CI_AS");

            migrationBuilder.AddColumn<string>(
                name: "TrangThai",
                table: "LoaiTiemChung",
                type: "nvarchar(30)",
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "GoiVacXin",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                collation: "Latin1_General_100_CI_AS");

            migrationBuilder.AddColumn<string>(
                name: "DoiTuongSuDung",
                table: "GoiVacXin",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MoTa",
                table: "GoiVacXin",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenBenh",
                table: "Benh",
                type: "nvarchar(150)",
                nullable: false,
                collation: "Latin1_General_100_CI_AS",
                oldClrType: typeof(string),
                oldType: "nvarchar(150)");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Benh",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                collation: "Latin1_General_100_CI_AS");

            migrationBuilder.AddColumn<string>(
                name: "TrangThai",
                table: "Benh",
                type: "nvarchar(30)",
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.CreateTable(
                name: "VaccineRegimen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VaccineId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TargetAudience = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    SourceName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaccineRegimen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaccineRegimen_VacXin_VaccineId",
                        column: x => x.VaccineId,
                        principalTable: "VacXin",
                        principalColumn: "MaVacXin",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VaccineRegimenDose",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegimenId = table.Column<int>(type: "int", nullable: false),
                    DoseOrder = table.Column<int>(type: "int", nullable: false),
                    TimingLabel = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    MinimumIntervalDays = table.Column<int>(type: "int", nullable: true),
                    RecommendedIntervalDays = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaccineRegimenDose", x => x.Id);
                    table.CheckConstraint("CK_VaccineRegimenDose_Intervals", "([MinimumIntervalDays] IS NULL OR [MinimumIntervalDays] >= 0) AND ([RecommendedIntervalDays] IS NULL OR [RecommendedIntervalDays] >= 0) AND ([MinimumIntervalDays] IS NULL OR [RecommendedIntervalDays] IS NULL OR [RecommendedIntervalDays] >= [MinimumIntervalDays])");
                    table.CheckConstraint("CK_VaccineRegimenDose_Order", "[DoseOrder] BETWEEN 1 AND 20");
                    table.ForeignKey(
                        name: "FK_VaccineRegimenDose_VaccineRegimen_RegimenId",
                        column: x => x.RegimenId,
                        principalTable: "VaccineRegimen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Preserve technical keys; allocate semantic codes in key order, suffixing collisions only.
            migrationBuilder.Sql("""
                DECLARE @id int, @name nvarchar(150), @slug varchar(150), @code varchar(50), @suffix varchar(30), @i int, @n int, @c nchar(1);
                DECLARE catalog_codes CURSOR LOCAL FAST_FORWARD FOR SELECT [MaLoaiTiemChung], [TenLoaiTiemChung] FROM [LoaiTiemChung] ORDER BY [MaLoaiTiemChung];
                OPEN catalog_codes;
                FETCH NEXT FROM catalog_codes INTO @id, @name;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @name = TRANSLATE(UPPER(@name COLLATE Latin1_General_100_CI_AS), N'ÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ', N'AAAAAAAAAAAAAAAAAEEEEEEEEEEEIIIIIOOOOOOOOOOOOOOOOOUUUUUUUUUUUYYYYYD');
                    SET @slug = ''; SET @i = 1;
                    WHILE @i <= LEN(@name)
                    BEGIN
                        SET @c = SUBSTRING(@name, @i, 1);
                        IF @c COLLATE Latin1_General_100_BIN2 LIKE '[A-Z0-9]' SET @slug += CONVERT(varchar(1), @c);
                        ELSE IF UNICODE(@c) NOT BETWEEN 768 AND 879 AND LEN(@slug) > 0 AND RIGHT(@slug, 1) <> '-' SET @slug += '-';
                        SET @i += 1;
                    END;
                    IF RIGHT(@slug, 1) = '-' SET @slug = LEFT(@slug, LEN(@slug) - 1);
                    IF @slug = '' THROW 51000, 'RF02A: legacy name cannot produce semantic ASCII code; owner mapping required.', 1;
                    SET @code = LEFT('LTC-' + @slug, 50);
                    IF RIGHT(@code, 1) = '-' SET @code = LEFT(@code, LEN(@code) - 1);
                    SET @n = 0;
                    WHILE EXISTS (SELECT 1 FROM [LoaiTiemChung] WHERE [Code] = @code)
                    BEGIN
                        SET @n += 1;
                        SET @suffix = '-' + CONVERT(varchar(11), @id) + CASE WHEN @n = 1 THEN '' ELSE '-' + CONVERT(varchar(11), @n) END;
                        SET @code = LEFT('LTC-' + @slug, 50 - LEN(@suffix));
                        IF RIGHT(@code, 1) = '-' SET @code = LEFT(@code, LEN(@code) - 1);
                        SET @code += @suffix;
                    END;
                    UPDATE [LoaiTiemChung] SET [Code] = @code WHERE [MaLoaiTiemChung] = @id;
                    FETCH NEXT FROM catalog_codes INTO @id, @name;
                END;
                CLOSE catalog_codes;
                DEALLOCATE catalog_codes;
                """);
            migrationBuilder.AlterColumn<string>(name: "Code", table: "LoaiTiemChung", type: "nvarchar(50)", maxLength: 50,
                nullable: false, collation: "Latin1_General_100_CI_AS", oldClrType: typeof(string), oldType: "nvarchar(50)", oldMaxLength: 50, oldNullable: true, oldCollation: "Latin1_General_100_CI_AS");

            // Preserve technical keys; allocate semantic codes in key order, suffixing collisions only.
            migrationBuilder.Sql("""
                DECLARE @id int, @name nvarchar(150), @slug varchar(150), @code varchar(50), @suffix varchar(30), @i int, @n int, @c nchar(1);
                DECLARE catalog_codes CURSOR LOCAL FAST_FORWARD FOR SELECT [MaLoaiVacXin], [TenLoaiVacXin] FROM [LoaiVacXin] ORDER BY [MaLoaiVacXin];
                OPEN catalog_codes;
                FETCH NEXT FROM catalog_codes INTO @id, @name;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @name = TRANSLATE(UPPER(@name COLLATE Latin1_General_100_CI_AS), N'ÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ', N'AAAAAAAAAAAAAAAAAEEEEEEEEEEEIIIIIOOOOOOOOOOOOOOOOOUUUUUUUUUUUYYYYYD');
                    SET @slug = ''; SET @i = 1;
                    WHILE @i <= LEN(@name)
                    BEGIN
                        SET @c = SUBSTRING(@name, @i, 1);
                        IF @c COLLATE Latin1_General_100_BIN2 LIKE '[A-Z0-9]' SET @slug += CONVERT(varchar(1), @c);
                        ELSE IF UNICODE(@c) NOT BETWEEN 768 AND 879 AND LEN(@slug) > 0 AND RIGHT(@slug, 1) <> '-' SET @slug += '-';
                        SET @i += 1;
                    END;
                    IF RIGHT(@slug, 1) = '-' SET @slug = LEFT(@slug, LEN(@slug) - 1);
                    IF @slug = '' THROW 51000, 'RF02A: legacy name cannot produce semantic ASCII code; owner mapping required.', 1;
                    SET @code = LEFT('LVX-' + @slug, 50);
                    IF RIGHT(@code, 1) = '-' SET @code = LEFT(@code, LEN(@code) - 1);
                    SET @n = 0;
                    WHILE EXISTS (SELECT 1 FROM [LoaiVacXin] WHERE [Code] = @code)
                    BEGIN
                        SET @n += 1;
                        SET @suffix = '-' + CONVERT(varchar(11), @id) + CASE WHEN @n = 1 THEN '' ELSE '-' + CONVERT(varchar(11), @n) END;
                        SET @code = LEFT('LVX-' + @slug, 50 - LEN(@suffix));
                        IF RIGHT(@code, 1) = '-' SET @code = LEFT(@code, LEN(@code) - 1);
                        SET @code += @suffix;
                    END;
                    UPDATE [LoaiVacXin] SET [Code] = @code WHERE [MaLoaiVacXin] = @id;
                    FETCH NEXT FROM catalog_codes INTO @id, @name;
                END;
                CLOSE catalog_codes;
                DEALLOCATE catalog_codes;
                """);
            migrationBuilder.AlterColumn<string>(name: "Code", table: "LoaiVacXin", type: "nvarchar(50)", maxLength: 50,
                nullable: false, collation: "Latin1_General_100_CI_AS", oldClrType: typeof(string), oldType: "nvarchar(50)", oldMaxLength: 50, oldNullable: true, oldCollation: "Latin1_General_100_CI_AS");

            // Preserve technical keys; allocate semantic codes in key order, suffixing collisions only.
            migrationBuilder.Sql("""
                DECLARE @id int, @name nvarchar(150), @slug varchar(150), @code varchar(50), @suffix varchar(30), @i int, @n int, @c nchar(1);
                DECLARE catalog_codes CURSOR LOCAL FAST_FORWARD FOR SELECT [MaBenh], [TenBenh] FROM [Benh] ORDER BY [MaBenh];
                OPEN catalog_codes;
                FETCH NEXT FROM catalog_codes INTO @id, @name;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @name = TRANSLATE(UPPER(@name COLLATE Latin1_General_100_CI_AS), N'ÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ', N'AAAAAAAAAAAAAAAAAEEEEEEEEEEEIIIIIOOOOOOOOOOOOOOOOOUUUUUUUUUUUYYYYYD');
                    SET @slug = ''; SET @i = 1;
                    WHILE @i <= LEN(@name)
                    BEGIN
                        SET @c = SUBSTRING(@name, @i, 1);
                        IF @c COLLATE Latin1_General_100_BIN2 LIKE '[A-Z0-9]' SET @slug += CONVERT(varchar(1), @c);
                        ELSE IF UNICODE(@c) NOT BETWEEN 768 AND 879 AND LEN(@slug) > 0 AND RIGHT(@slug, 1) <> '-' SET @slug += '-';
                        SET @i += 1;
                    END;
                    IF RIGHT(@slug, 1) = '-' SET @slug = LEFT(@slug, LEN(@slug) - 1);
                    IF @slug = '' THROW 51000, 'RF02A: legacy name cannot produce semantic ASCII code; owner mapping required.', 1;
                    SET @code = LEFT('BENH-' + @slug, 50);
                    IF RIGHT(@code, 1) = '-' SET @code = LEFT(@code, LEN(@code) - 1);
                    SET @n = 0;
                    WHILE EXISTS (SELECT 1 FROM [Benh] WHERE [Code] = @code)
                    BEGIN
                        SET @n += 1;
                        SET @suffix = '-' + CONVERT(varchar(11), @id) + CASE WHEN @n = 1 THEN '' ELSE '-' + CONVERT(varchar(11), @n) END;
                        SET @code = LEFT('BENH-' + @slug, 50 - LEN(@suffix));
                        IF RIGHT(@code, 1) = '-' SET @code = LEFT(@code, LEN(@code) - 1);
                        SET @code += @suffix;
                    END;
                    UPDATE [Benh] SET [Code] = @code WHERE [MaBenh] = @id;
                    FETCH NEXT FROM catalog_codes INTO @id, @name;
                END;
                CLOSE catalog_codes;
                DEALLOCATE catalog_codes;
                """);
            migrationBuilder.AlterColumn<string>(name: "Code", table: "Benh", type: "nvarchar(50)", maxLength: 50,
                nullable: false, collation: "Latin1_General_100_CI_AS", oldClrType: typeof(string), oldType: "nvarchar(50)", oldMaxLength: 50, oldNullable: true, oldCollation: "Latin1_General_100_CI_AS");

            // Preserve technical keys; allocate semantic codes in key order, suffixing collisions only.
            migrationBuilder.Sql("""
                DECLARE @id int, @name nvarchar(150), @slug varchar(150), @code varchar(50), @suffix varchar(30), @i int, @n int, @c nchar(1);
                DECLARE catalog_codes CURSOR LOCAL FAST_FORWARD FOR SELECT [MaVacXin], [TenVacXin] FROM [VacXin] ORDER BY [MaVacXin];
                OPEN catalog_codes;
                FETCH NEXT FROM catalog_codes INTO @id, @name;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @name = TRANSLATE(UPPER(@name COLLATE Latin1_General_100_CI_AS), N'ÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ', N'AAAAAAAAAAAAAAAAAEEEEEEEEEEEIIIIIOOOOOOOOOOOOOOOOOUUUUUUUUUUUYYYYYD');
                    SET @slug = ''; SET @i = 1;
                    WHILE @i <= LEN(@name)
                    BEGIN
                        SET @c = SUBSTRING(@name, @i, 1);
                        IF @c COLLATE Latin1_General_100_BIN2 LIKE '[A-Z0-9]' SET @slug += CONVERT(varchar(1), @c);
                        ELSE IF UNICODE(@c) NOT BETWEEN 768 AND 879 AND LEN(@slug) > 0 AND RIGHT(@slug, 1) <> '-' SET @slug += '-';
                        SET @i += 1;
                    END;
                    IF RIGHT(@slug, 1) = '-' SET @slug = LEFT(@slug, LEN(@slug) - 1);
                    IF @slug = '' THROW 51000, 'RF02A: legacy name cannot produce semantic ASCII code; owner mapping required.', 1;
                    SET @code = LEFT('VX-' + @slug, 50);
                    IF RIGHT(@code, 1) = '-' SET @code = LEFT(@code, LEN(@code) - 1);
                    SET @n = 0;
                    WHILE EXISTS (SELECT 1 FROM [VacXin] WHERE [Code] = @code)
                    BEGIN
                        SET @n += 1;
                        SET @suffix = '-' + CONVERT(varchar(11), @id) + CASE WHEN @n = 1 THEN '' ELSE '-' + CONVERT(varchar(11), @n) END;
                        SET @code = LEFT('VX-' + @slug, 50 - LEN(@suffix));
                        IF RIGHT(@code, 1) = '-' SET @code = LEFT(@code, LEN(@code) - 1);
                        SET @code += @suffix;
                    END;
                    UPDATE [VacXin] SET [Code] = @code WHERE [MaVacXin] = @id;
                    FETCH NEXT FROM catalog_codes INTO @id, @name;
                END;
                CLOSE catalog_codes;
                DEALLOCATE catalog_codes;
                """);
            migrationBuilder.AlterColumn<string>(name: "Code", table: "VacXin", type: "nvarchar(50)", maxLength: 50,
                nullable: false, collation: "Latin1_General_100_CI_AS", oldClrType: typeof(string), oldType: "nvarchar(50)", oldMaxLength: 50, oldNullable: true, oldCollation: "Latin1_General_100_CI_AS");

            // Preserve technical keys; allocate semantic codes in key order, suffixing collisions only.
            migrationBuilder.Sql("""
                DECLARE @id int, @name nvarchar(150), @slug varchar(150), @code varchar(50), @suffix varchar(30), @i int, @n int, @c nchar(1);
                DECLARE catalog_codes CURSOR LOCAL FAST_FORWARD FOR SELECT [MaGoi], [TenGoi] FROM [GoiVacXin] ORDER BY [MaGoi];
                OPEN catalog_codes;
                FETCH NEXT FROM catalog_codes INTO @id, @name;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @name = TRANSLATE(UPPER(@name COLLATE Latin1_General_100_CI_AS), N'ÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ', N'AAAAAAAAAAAAAAAAAEEEEEEEEEEEIIIIIOOOOOOOOOOOOOOOOOUUUUUUUUUUUYYYYYD');
                    SET @slug = ''; SET @i = 1;
                    WHILE @i <= LEN(@name)
                    BEGIN
                        SET @c = SUBSTRING(@name, @i, 1);
                        IF @c COLLATE Latin1_General_100_BIN2 LIKE '[A-Z0-9]' SET @slug += CONVERT(varchar(1), @c);
                        ELSE IF UNICODE(@c) NOT BETWEEN 768 AND 879 AND LEN(@slug) > 0 AND RIGHT(@slug, 1) <> '-' SET @slug += '-';
                        SET @i += 1;
                    END;
                    IF RIGHT(@slug, 1) = '-' SET @slug = LEFT(@slug, LEN(@slug) - 1);
                    IF @slug = '' THROW 51000, 'RF02A: legacy name cannot produce semantic ASCII code; owner mapping required.', 1;
                    SET @code = LEFT('GOI-' + @slug, 50);
                    IF RIGHT(@code, 1) = '-' SET @code = LEFT(@code, LEN(@code) - 1);
                    SET @n = 0;
                    WHILE EXISTS (SELECT 1 FROM [GoiVacXin] WHERE [Code] = @code)
                    BEGIN
                        SET @n += 1;
                        SET @suffix = '-' + CONVERT(varchar(11), @id) + CASE WHEN @n = 1 THEN '' ELSE '-' + CONVERT(varchar(11), @n) END;
                        SET @code = LEFT('GOI-' + @slug, 50 - LEN(@suffix));
                        IF RIGHT(@code, 1) = '-' SET @code = LEFT(@code, LEN(@code) - 1);
                        SET @code += @suffix;
                    END;
                    UPDATE [GoiVacXin] SET [Code] = @code WHERE [MaGoi] = @id;
                    FETCH NEXT FROM catalog_codes INTO @id, @name;
                END;
                CLOSE catalog_codes;
                DEALLOCATE catalog_codes;
                """);
            migrationBuilder.AlterColumn<string>(name: "Code", table: "GoiVacXin", type: "nvarchar(50)", maxLength: 50,
                nullable: false, collation: "Latin1_General_100_CI_AS", oldClrType: typeof(string), oldType: "nvarchar(50)", oldMaxLength: 50, oldNullable: true, oldCollation: "Latin1_General_100_CI_AS");

            migrationBuilder.CreateIndex(
                name: "IX_VacXin_Code",
                table: "VacXin",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoaiVacXin_Code",
                table: "LoaiVacXin",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoaiVacXin_TenLoaiVacXin",
                table: "LoaiVacXin",
                column: "TenLoaiVacXin",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoaiTiemChung_Code",
                table: "LoaiTiemChung",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoaiTiemChung_TenLoaiTiemChung",
                table: "LoaiTiemChung",
                column: "TenLoaiTiemChung",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoiVacXin_Code",
                table: "GoiVacXin",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Benh_Code",
                table: "Benh",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Benh_TenBenh",
                table: "Benh",
                column: "TenBenh",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaccineRegimen_VaccineId",
                table: "VaccineRegimen",
                column: "VaccineId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccineRegimenDose_RegimenId_DoseOrder",
                table: "VaccineRegimenDose",
                columns: new[] { "RegimenId", "DoseOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VaccineRegimenDose");

            migrationBuilder.DropTable(
                name: "VaccineRegimen");

            migrationBuilder.DropIndex(
                name: "IX_VacXin_Code",
                table: "VacXin");

            migrationBuilder.DropIndex(
                name: "IX_LoaiVacXin_Code",
                table: "LoaiVacXin");

            migrationBuilder.DropIndex(
                name: "IX_LoaiVacXin_TenLoaiVacXin",
                table: "LoaiVacXin");

            migrationBuilder.DropIndex(
                name: "IX_LoaiTiemChung_Code",
                table: "LoaiTiemChung");

            migrationBuilder.DropIndex(
                name: "IX_LoaiTiemChung_TenLoaiTiemChung",
                table: "LoaiTiemChung");

            migrationBuilder.DropIndex(
                name: "IX_GoiVacXin_Code",
                table: "GoiVacXin");

            migrationBuilder.DropIndex(
                name: "IX_Benh_Code",
                table: "Benh");

            migrationBuilder.DropIndex(
                name: "IX_Benh_TenBenh",
                table: "Benh");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "VacXin");

            migrationBuilder.DropColumn(
                name: "DoiTuongSuDung",
                table: "VacXin");

            migrationBuilder.DropColumn(
                name: "DuongDanNguon",
                table: "VacXin");

            migrationBuilder.DropColumn(
                name: "DuongDung",
                table: "VacXin");

            migrationBuilder.DropColumn(
                name: "DuongDungKhac",
                table: "VacXin");

            migrationBuilder.DropColumn(
                name: "MoTa",
                table: "VacXin");

            migrationBuilder.DropColumn(
                name: "NgayCapNhatThongTin",
                table: "VacXin");

            migrationBuilder.DropColumn(
                name: "NguonThongTin",
                table: "VacXin");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "LoaiVacXin");

            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "LoaiVacXin");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "LoaiTiemChung");

            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "LoaiTiemChung");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "GoiVacXin");

            migrationBuilder.DropColumn(
                name: "DoiTuongSuDung",
                table: "GoiVacXin");

            migrationBuilder.DropColumn(
                name: "MoTa",
                table: "GoiVacXin");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Benh");

            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "Benh");

            migrationBuilder.AlterColumn<string>(
                name: "LieuLuong",
                table: "VacXin",
                type: "nvarchar(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)");

            migrationBuilder.AlterColumn<string>(
                name: "TenLoaiVacXin",
                table: "LoaiVacXin",
                type: "nvarchar(120)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldCollation: "Latin1_General_100_CI_AS");

            migrationBuilder.AlterColumn<string>(
                name: "TenLoaiTiemChung",
                table: "LoaiTiemChung",
                type: "nvarchar(120)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldCollation: "Latin1_General_100_CI_AS");

            migrationBuilder.AlterColumn<string>(
                name: "TenBenh",
                table: "Benh",
                type: "nvarchar(150)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldCollation: "Latin1_General_100_CI_AS");
        }
    }
}
