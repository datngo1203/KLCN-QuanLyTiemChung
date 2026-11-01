using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TiemChung.Api;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class DevelopmentInventorySeedSqlTests
{
    private static readonly IHostEnvironment Development = new SeedEnvironment("Development");

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task FirstSeedPersistsThreeScenariosAndRepeatDoesNotWriteAnyExistingLot()
    {
        await WithCatalog(async f => {
            // Starting the ordinary API in the fixture must not seed inventory.
            Assert.Empty(await f.Db.Set<LoVacXin>().ToArrayAsync());
            var beforeCatalog = JsonSerializer.Serialize(await f.Db.Vaccines.AsNoTracking().OrderBy(x => x.MaVacXin).ToArrayAsync());
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            Assert.Equal(3, await DevelopmentInventorySeed.RunAsync(f.Db, Development));
            f.Db.ChangeTracker.Clear();
            var lots = await f.Db.Set<LoVacXin>().AsNoTracking().OrderBy(x => x.SoLo).ToArrayAsync();
            Assert.Equal(3, lots.Length);
            foreach (var lot in lots) {
                Assert.StartsWith("[DEMO]", lot.SoLo);
                Assert.Equal("Active", lot.TrangThai);
                Assert.True(lot.NgaySanXuat < today);
                Assert.True(lot.NgaySanXuat < lot.HanSuDung);
                Assert.True(lot.SoLuongBanDau > 0);
                Assert.InRange(lot.SoLuongTon, 0, lot.SoLuongBanDau);
                var vaccine = await f.Db.Vaccines.SingleAsync(x => x.MaVacXin == lot.MaVacXin);
                Assert.Equal(lot.SoLo.EndsWith("-B") ? "[DEMO] Vắc xin B" : "[DEMO] Vắc xin A", vaccine.TenVacXin);
            }
            var valid = Assert.Single(lots, x => x.SoLo == "[DEMO] S5-VALID-A");
            Assert.True(valid.HanSuDung > today); Assert.Equal(100, valid.SoLuongTon);
            var expired = Assert.Single(lots, x => x.SoLo == "[DEMO] S5-EXPIRED-A");
            Assert.True(expired.HanSuDung < today); Assert.Equal(20, expired.SoLuongTon);
            var empty = Assert.Single(lots, x => x.SoLo == "[DEMO] S5-EMPTY-B");
            Assert.True(empty.HanSuDung > today); Assert.Equal(0, empty.SoLuongTon);
            var snapshot = await Snapshot(f.Db);
            Assert.Equal(0, await DevelopmentInventorySeed.RunAsync(f.Db, Development));
            Assert.Equal(snapshot, await Snapshot(f.Db)); // Includes SQL RowVersion: no UPDATE occurred.
            Assert.Equal(beforeCatalog, JsonSerializer.Serialize(await f.Db.Vaccines.AsNoTracking().OrderBy(x => x.MaVacXin).ToArrayAsync()));
            Assert.Empty(await f.Db.Set<LichHenTiem>().ToArrayAsync());
            Assert.Empty(await f.Db.Set<PhieuKhamSangLoc>().ToArrayAsync());
            Assert.Empty(await f.Db.Set<ChiDinhTiem>().ToArrayAsync());
            Assert.Empty(await f.Db.Set<ThanhToan>().ToArrayAsync());
            Assert.Empty(await f.Db.Set<MuiTiem>().ToArrayAsync());
            Assert.Empty(await f.Db.Set<BienDongKho>().ToArrayAsync());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task RerunPreservesConsumedStockDatesStatusInitialQuantityAndUnrelatedLots()
    {
        await WithCatalog(async f => {
            await DevelopmentInventorySeed.RunAsync(f.Db, Development);
            f.Db.ChangeTracker.Clear();
            var valid = await f.Db.Set<LoVacXin>().SingleAsync(x => x.SoLo == "[DEMO] S5-VALID-A");
            valid.SoLuongBanDau = 80; valid.SoLuongTon = 73;
            valid.NgaySanXuat = new(2001, 1, 1); valid.HanSuDung = new(2002, 1, 1); valid.TrangThai = "Inactive";
            foreach (var code in new[] { "OUTSIDE-S5", "[DEMO] OTHER-LOT" })
                f.Db.Add(new LoVacXin { SoLo=code, MaVacXin=valid.MaVacXin, NgaySanXuat=new(2000,1,1),
                    HanSuDung=new(2003,1,1), SoLuongBanDau=9, SoLuongTon=4, TrangThai="Inactive" });
            await f.Db.SaveChangesAsync();
            f.Db.ChangeTracker.Clear();
            var snapshot = await Snapshot(f.Db);
            Assert.Equal(0, await DevelopmentInventorySeed.RunAsync(f.Db, Development));
            Assert.Equal(snapshot, await Snapshot(f.Db));

            // A partial prior seed only adds the missing lot; surviving rows still keep their versions.
            await f.Db.Set<LoVacXin>().Where(x => x.SoLo == "[DEMO] S5-EXPIRED-A").ExecuteDeleteAsync();
            var survivors = await Snapshot(f.Db, "[DEMO] S5-EXPIRED-A");
            Assert.Equal(1, await DevelopmentInventorySeed.RunAsync(f.Db, Development));
            Assert.Equal(survivors, await Snapshot(f.Db, "[DEMO] S5-EXPIRED-A"));
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("missing")]
    [InlineData("renamed")]
    [InlineData("ambiguous")]
    public async Task MissingOrAmbiguousSourceFailsWithoutPartialInventory(string scenario)
    {
        await WithCatalog(async f => {
            var source = await f.Db.Vaccines.SingleAsync(x => x.TenVacXin == "[DEMO] Vắc xin B");
            if (scenario == "missing") {
                await f.Db.Set<GoiVacXin_VacXin>().Where(x => x.MaVacXin == source.MaVacXin).ExecuteDeleteAsync();
                f.Db.Remove(source);
            } else if (scenario == "renamed") source.TenVacXin = "Non-demo source";
            else f.Db.Add(new VacXin { TenVacXin=source.TenVacXin, MaLoaiVacXin=source.MaLoaiVacXin,
                MaLoaiTiemChung=source.MaLoaiTiemChung, NhaSanXuat="Synthetic", XuatXu="Synthetic", Gia=1, TrangThai="Active" });
            await f.Db.SaveChangesAsync(); f.Db.ChangeTracker.Clear();
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => DevelopmentInventorySeed.RunAsync(f.Db, Development));
            Assert.Contains("requires exactly one synthetic catalog vaccine '[DEMO] Vắc xin B'", error.Message);
            Assert.Empty(await f.Db.Set<LoVacXin>().ToArrayAsync());
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConflictingLotCodeFailsWithoutOverwritingOrPartialInsert(bool caseCollision)
    {
        await WithCatalog(async f => {
            var vaccine = await f.Db.Vaccines.SingleAsync(x => x.TenVacXin == (caseCollision ? "[DEMO] Vắc xin B" : "[DEMO] Vắc xin A"));
            f.Db.Add(new LoVacXin { SoLo=caseCollision ? "[demo] s5-empty-b" : "[DEMO] S5-EMPTY-B", MaVacXin=vaccine.MaVacXin,
                NgaySanXuat=new(2000,1,1), HanSuDung=new(2001,1,1), SoLuongBanDau=7, SoLuongTon=2, TrangThai="Inactive" });
            await f.Db.SaveChangesAsync(); f.Db.ChangeTracker.Clear();
            var before = await Snapshot(f.Db);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => DevelopmentInventorySeed.RunAsync(f.Db, Development));
            Assert.Contains("conflicts with existing data", error.Message);
            Assert.Equal(before, await Snapshot(f.Db));
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task SqlInsertFailureRollsBackEveryLot()
    {
        await WithCatalog(async f => {
            await f.Db.Database.ExecuteSqlRawAsync("""
                ALTER TABLE LoVacXin ADD CONSTRAINT InventoryFixtureFailure
                    CHECK (SoLo <> N'[DEMO] S5-EXPIRED-A');
                """);
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => DevelopmentInventorySeed.RunAsync(f.Db, Development));
            var sql = Assert.IsType<SqlException>(error.InnerException);
            Assert.Equal(547, sql.Number); Assert.Contains("InventoryFixtureFailure", sql.Message);
            Assert.Empty(await f.Db.Set<LoVacXin>().AsNoTracking().ToArrayAsync());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task ConcurrentSeedCommandsOnlyInsertOnce()
    {
        await WithCatalog(async f => {
            await using var other = new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>()
                .UseSqlServer(f.Database.ConnectionString).Options);
            var counts = await Task.WhenAll(DevelopmentInventorySeed.RunAsync(f.Db, Development), DevelopmentInventorySeed.RunAsync(other, Development));
            Assert.Equal(new[] { 0, 3 }, counts.Order().ToArray());
            Assert.Equal(3, await f.Db.Set<LoVacXin>().CountAsync());
            Assert.All(await f.Db.Set<LoVacXin>().AsNoTracking().ToArrayAsync(), x => Assert.InRange(x.SoLuongTon, 0, x.SoLuongBanDau));
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task ExistingAppointmentsAssessmentsIndicationsAndPaymentsRemainUnchanged()
    {
        await SqlFixtureLifecycle.RunAsync(() => AppointmentSqlTests.Fixture.Create(), async f => {
            var registration = await f.Register();
            using var created = await f.Reception.PostAsJsonAsync("/api/v1/appointments",
                f.Request(registration, registration.Items.Single(x => x.Kind == "vaccine")));
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var appointment = (await created.Content.ReadFromJsonAsync<AppointmentDto>())!;
            using var doctor = await f.Actor("BacSi", false);
            using var assessed = await doctor.PostAsJsonAsync($"/api/v1/clinical/appointments/{appointment.Id}/assessment",
                new CreateClinicalAssessmentRequest("Đủ điều kiện", null, [appointment.ServiceId]));
            Assert.Equal(HttpStatusCode.Created, assessed.StatusCode);
            using var cashier = await f.Actor("ThuNgan", false);
            foreach (var code in new[] { "XemThanhToan", "GhiThanhToan" })
                f.Db.Add(new TaiKhoan_QuyenHan { MaTaiKhoan=f.LastAccountId,
                    MaQuyen=(await f.Db.Set<QuyenHan>().SingleAsync(x => x.Code == code)).MaQuyen });
            await f.Db.SaveChangesAsync();
            using var paid = await cashier.PostAsJsonAsync($"/api/v1/registrations/{registration.Id}/payment", new { });
            Assert.Equal(HttpStatusCode.Created, paid.StatusCode);
            await DevelopmentCatalogSeed.RunAsync(f.Db, Development);
            f.Db.ChangeTracker.Clear();
            async Task<string> BusinessSnapshot() {
                var rows = new List<string>();
                await f.Db.Database.OpenConnectionAsync();
                try {
                    foreach (var table in new[] { "LichHenTiem", "PhieuKhamSangLoc", "ChiDinhTiem", "ThanhToan", "DangKyDichVu", "SecurityAudit", "MuiTiem", "BienDongKho" }) {
                        await using var command = f.Db.Database.GetDbConnection().CreateCommand();
                        command.CommandText = $"SELECT (SELECT * FROM [{table}] FOR JSON PATH, INCLUDE_NULL_VALUES)";
                        rows.Add(Convert.ToString(await command.ExecuteScalarAsync())!);
                    }
                } finally { await f.Db.Database.CloseConnectionAsync(); }
                return string.Join("\n", rows);
            }
            var before = await BusinessSnapshot();
            Assert.Equal(3, await DevelopmentInventorySeed.RunAsync(f.Db, Development));
            Assert.Equal(before, await BusinessSnapshot());
            Assert.Equal(0, await DevelopmentInventorySeed.RunAsync(f.Db, Development));
            Assert.Equal(before, await BusinessSnapshot());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task ActualCliRequiresSeparateCatalogRejectsProductionAndExitsAfterSeed()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f => {
            var missing = await Cli(f.Database.ConnectionString, "Development");
            Assert.NotEqual(0, missing.ExitCode); Assert.Contains("requires exactly one synthetic catalog vaccine", missing.Output);
            Assert.Empty(await f.Db.Vaccines.ToArrayAsync()); Assert.Empty(await f.Db.Set<LoVacXin>().ToArrayAsync());
            var combined = await Cli(f.Database.ConnectionString, "Development", "--seed-catalog");
            Assert.NotEqual(0, combined.ExitCode); Assert.Contains("separately", combined.Output);
            Assert.Empty(await f.Db.Vaccines.ToArrayAsync());
            await DevelopmentCatalogSeed.RunAsync(f.Db, Development);
            var production = await Cli(f.Database.ConnectionString, "Production");
            Assert.NotEqual(0, production.ExitCode); Assert.Contains("Development only", production.Output);
            Assert.Empty(await f.Db.Set<LoVacXin>().ToArrayAsync());
            var first = await Cli(f.Database.ConnectionString, "Development");
            Assert.True(first.ExitCode == 0, first.Output); Assert.Contains("3 synthetic [DEMO] lots added", first.Output);
            var snapshot = await Snapshot(f.Db);
            var again = await Cli(f.Database.ConnectionString, "Development");
            Assert.True(again.ExitCode == 0, again.Output); Assert.Contains("0 synthetic [DEMO] lots added", again.Output);
            Assert.Equal(snapshot, await Snapshot(f.Db));
        });
    }

    private static Task WithCatalog(Func<CatalogSqlTests.Fixture, Task> test) =>
        SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f => {
            await DevelopmentCatalogSeed.RunAsync(f.Db, Development);
            f.Db.ChangeTracker.Clear();
            await test(f);
        });

    private static async Task<string> Snapshot(TiemChungDbContext db, string except = "") => JsonSerializer.Serialize(
        await db.Set<LoVacXin>().AsNoTracking().Where(x => x.SoLo != except).OrderBy(x => x.SoLo)
            .Select(x => new { Lot=x, Version=EF.Property<byte[]>(x, "RowVersion") }).ToArrayAsync());

    private static async Task<(int ExitCode, string Output)> Cli(string connection, string environment, params string[] extra)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput=true, RedirectStandardError=true, UseShellExecute=false, CreateNoWindow=true };
        foreach (var arg in new[] { typeof(Program).Assembly.Location, "--environment", environment, "--seed-inventory" }.Concat(extra))
            start.ArgumentList.Add(arg);
        start.Environment["ConnectionStrings__TiemChung"] = connection;
        start.Environment["Catalog__UseDemoData"] = "false";
        start.Environment["Logging__LogLevel__Default"] = "Warning";
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(entireProcessTree: true); throw; }
        return (process.ExitCode, await stdout + await stderr);
    }

    private sealed class SeedEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "InventorySeedTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
