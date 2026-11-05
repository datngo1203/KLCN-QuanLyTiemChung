using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TiemChung.Api;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using TiemChung.Infrastructure.Data;
using Xunit;

namespace TiemChung.Tests;

public sealed class DevelopmentCatalogSeedSqlTests
{
    private static readonly IHostEnvironment Development = new SeedEnvironment("Development");

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task CuratedGraphIsSourceBackedReadableByBothClientsAndRerunPreservesEdits()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f => {
            await Assert.ThrowsAsync<InvalidOperationException>(() => DevelopmentCatalogSeed.RunAsync(f.Db, new SeedEnvironment("Production")));
            Assert.Empty(await f.Db.Vaccines.ToArrayAsync());
            await DevelopmentCatalogSeed.RunAsync(f.Db, Development);
            f.Db.ChangeTracker.Clear();
            var vaccines = await f.Db.Vaccines.OrderBy(x => x.Code).ToArrayAsync();
            Assert.Equal(new[] { "Avaxim 80U", "Havax 0,5 ml" }, vaccines.Select(x => x.TenVacXin));
            Assert.Equal(new decimal[] { 685000, 295000 }, vaccines.Select(x => x.Gia));
            Assert.All(vaccines, v => {
                Assert.Equal("Intramuscular", v.DuongDung); Assert.Null(v.DuongDungKhac);
                Assert.Equal(0.5m, v.TheTich); Assert.NotEmpty(v.LieuLuong); Assert.NotEmpty(v.PhanUngSauTiem);
                Assert.Contains("https://vnvc.vn/gia-tiem-chung-vac-xin/", v.NguonThongTin);
                Assert.InRange(v.NguonThongTin!.Length, 1, 250);
                Assert.StartsWith("https://vnvc.vn/", v.DuongDanNguon);
                Assert.Equal(new DateTimeOffset(2026,11,5,0,0,0,TimeSpan.FromHours(7)), v.NgayCapNhatThongTin);
            });
            Assert.Single(await f.Db.Set<Benh>().ToArrayAsync());
            Assert.Equal(2, await f.Db.Set<VacXin_Benh>().CountAsync());
            var technology = Assert.Single(await f.Db.Set<CongNgheVacXin>().ToArrayAsync());
            Assert.StartsWith("https://www.sanofi.com/", technology.DuongDanNguon);
            Assert.Equal(vaccines[0].MaVacXin, Assert.Single(await f.Db.Set<VacXin_CongNghe>().ToArrayAsync()).MaVacXin);
            Assert.Equal(2, await f.Db.Set<VaccineRegimen>().CountAsync());
            var doses = await f.Db.Set<VaccineRegimenDose>().OrderBy(x => x.RegimenId).ThenBy(x => x.DoseOrder).ToArrayAsync();
            Assert.Equal(4, doses.Length);
            Assert.All(doses, d => { Assert.Null(d.MinimumIntervalDays); Assert.Null(d.RecommendedIntervalDays); });
            Assert.All(doses.Where(d => d.DoseOrder == 2), d => Assert.Equal("6 tháng sau mũi 1", d.TimingLabel));
            Assert.Empty(await f.Db.Set<GoiVacXin>().ToArrayAsync());
            Assert.DoesNotContain("DEMO", await Snapshot(f.Db));
            Assert.DoesNotContain("mô phỏng", await Snapshot(f.Db));

            using var http = f.Factory.CreateClient();
            var identity = new TiemChung.Desktop.Services.IdentityApiService(http);
            Assert.True((await identity.LoginAsync("s2.seed", f.Password)).Succeeded);
            var desktop = new TiemChung.Desktop.Services.CatalogApiService(identity);
            var windows = await desktop.ListAsync<TiemChung.Desktop.Services.CatalogVaccine>("vaccines");
            Assert.Equal(2, windows.Length);
            var web = (await f.Client.GetFromJsonAsync<VaccineListResponse>("/api/v1/catalog/vaccines"))!;
            Assert.Equal("database", web.Source); Assert.Equal(2, web.Items.Count);
            Assert.All(web.Items, v => { Assert.NotNull(v.SourceUrl); Assert.NotNull(v.InformationUpdatedAt); Assert.Single(v.Regimens); });

            vaccines[0].Gia = 123456; vaccines[0].TenVacXin = "Avaxim đã chỉnh sửa"; vaccines[0].TrangThai = "Inactive";
            doses[1].TimingLabel = "Nhân viên đã chỉnh lịch tham khảo";
            await f.Db.SaveChangesAsync(); f.Db.ChangeTracker.Clear();
            var before = await Snapshot(f.Db);
            await DevelopmentCatalogSeed.RunAsync(f.Db, Development);
            Assert.Equal(before, await Snapshot(f.Db));
            await identity.LogoutAsync();
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task ConcurrentSeedCreatesOneGraphAndKeepsLegacyIdentifiersAndReferences()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f => {
            await SyntheticCatalogFixture.RunAsync(f.Db, Development);
            var legacy = await Snapshot(f.Db);
            await using var other = new TiemChungDbContext(new DbContextOptionsBuilder<TiemChungDbContext>().UseSqlServer(f.Database.ConnectionString).Options);
            await Task.WhenAll(DevelopmentCatalogSeed.RunAsync(f.Db, Development), DevelopmentCatalogSeed.RunAsync(other, Development));
            Assert.Equal(4, await f.Db.Vaccines.CountAsync());
            Assert.Single(await f.Db.Set<GoiVacXin>().ToArrayAsync());
            Assert.Equal(2, await f.Db.Set<GoiVacXin_VacXin>().CountAsync());
            Assert.All(await f.Db.Vaccines.Where(v => v.Code.StartsWith("VX-DEMO-")).ToArrayAsync(), v => {
                Assert.StartsWith("[DEMO] Vắc xin", v.TenVacXin); Assert.Contains(v.Code, legacy);
            });
            var before = await Snapshot(f.Db);
            await DevelopmentCatalogSeed.RunAsync(f.Db, Development);
            Assert.Equal(before, await Snapshot(f.Db));
        });
    }

    private static async Task<string> Snapshot(TiemChungDbContext db) => JsonSerializer.Serialize(new {
        Vaccines = await db.Vaccines.AsNoTracking().OrderBy(x => x.MaVacXin).ToArrayAsync(),
        Diseases = await db.Set<Benh>().AsNoTracking().OrderBy(x => x.MaBenh).ToArrayAsync(),
        Technologies = await db.Set<CongNgheVacXin>().AsNoTracking().OrderBy(x => x.MaCongNghe).ToArrayAsync(),
        DiseaseLinks = await db.Set<VacXin_Benh>().AsNoTracking().OrderBy(x => x.MaVacXin).ThenBy(x => x.MaBenh).ToArrayAsync(),
        TechnologyLinks = await db.Set<VacXin_CongNghe>().AsNoTracking().OrderBy(x => x.MaVacXin).ThenBy(x => x.MaCongNghe).ToArrayAsync(),
        Regimens = await db.Set<VaccineRegimen>().AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
        Doses = await db.Set<VaccineRegimenDose>().AsNoTracking().OrderBy(x => x.Id).ToArrayAsync()
    });

    private sealed class SeedEnvironment(string name) : IHostEnvironment {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "CatalogSeedTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
