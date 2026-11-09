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
    private static readonly DateTimeOffset CheckedAt = new(2026,11,9,0,0,0,TimeSpan.FromHours(7));

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task CuratedGraphIsSourceBackedReadableByBothClientsAndRerunPreservesEdits()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f => {
            await Assert.ThrowsAsync<InvalidOperationException>(() => DevelopmentCatalogSeed.RunAsync(f.Db, new SeedEnvironment("Production")));
            Assert.Empty(await f.Db.Vaccines.ToArrayAsync());

            await DevelopmentCatalogSeed.RunAsync(f.Db, Development);
            f.Db.ChangeTracker.Clear();

            var vaccines = await f.Db.Vaccines.OrderBy(x => x.Code).ToArrayAsync();
            Assert.Equal(15, vaccines.Length);
            Assert.Equal(new[] {
                "VX-AVAXIM-80U","VX-BEXSERO","VX-GARDASIL-9","VX-HAVAX-05","VX-HEXAXIM",
                "VX-INFANRIX-HEXA","VX-MMR-II","VX-NIMENRIX","VX-PREVENAR-13","VX-QDENGA",
                "VX-ROTARIX","VX-ROTATEQ","VX-SHINGRIX","VX-VARIVAX","VX-VAXIGRIP"
            }, vaccines.Select(x => x.Code));
            var prices = vaccines.ToDictionary(x => x.Code, x => x.Gia);
            Assert.Equal(685000, prices["VX-AVAXIM-80U"]);
            Assert.Equal(995000, prices["VX-HEXAXIM"]);
            Assert.Equal(1190000, prices["VX-PREVENAR-13"]);
            Assert.Equal(1820000, prices["VX-BEXSERO"]);
            Assert.Equal(3680000, prices["VX-SHINGRIX"]);
            Assert.Equal(2948000, prices["VX-GARDASIL-9"]);
            Assert.Equal(1290000, prices["VX-QDENGA"]);
            Assert.All(vaccines, v => {
                Assert.Equal("Active", v.TrangThai);
                Assert.NotEmpty(v.DoiTuongSuDung!); Assert.NotEmpty(v.DuongDung!);
                Assert.True(v.Gia > 0); Assert.True(v.TheTich > 0); Assert.NotEmpty(v.LieuLuong);
                Assert.Contains("https://vnvc.vn/gia-tiem-chung-vac-xin/", v.NguonThongTin);
                Assert.InRange(v.NguonThongTin!.Length, 1, 250);
                Assert.StartsWith("http", v.DuongDanNguon);
                Assert.Equal(CheckedAt, v.NgayCapNhatThongTin);
            });
            Assert.Equal(19, await f.Db.Set<Benh>().CountAsync());
            Assert.Equal(4, await f.Db.Set<CongNgheVacXin>().CountAsync());
            Assert.Contains(await f.Db.Set<CongNgheVacXin>().ToArrayAsync(), x => x.Code == "CNVX-SONG-GIAM-DOC-LUC");
            Assert.Contains(await f.Db.Set<CongNgheVacXin>().ToArrayAsync(), x => x.Code == "CNVX-TAI-TO-HOP");
            Assert.Contains(await f.Db.Set<CongNgheVacXin>().ToArrayAsync(), x => x.Code == "CNVX-CONG-HOP");
            Assert.True(await f.Db.Set<VacXin_Benh>().CountAsync() >= 15);
            Assert.True(await f.Db.Set<VacXin_CongNghe>().CountAsync() >= 10);
            Assert.True(await f.Db.Set<VaccineRegimen>().CountAsync() >= 15);
            Assert.True(await f.Db.Set<VaccineRegimenDose>().CountAsync() >= 30);
            Assert.Empty(await f.Db.Set<GoiVacXin>().ToArrayAsync());
            Assert.DoesNotContain("DEMO", await Snapshot(f.Db));
            Assert.DoesNotContain("mô phỏng", await Snapshot(f.Db));

            var avaxim = vaccines.Single(x => x.Code == "VX-AVAXIM-80U");
            var avaximRegimen = await f.Db.Set<VaccineRegimen>().SingleAsync(x => x.VaccineId == avaxim.MaVacXin);
            var avaximDose2 = await f.Db.Set<VaccineRegimenDose>().SingleAsync(x => x.RegimenId == avaximRegimen.Id && x.DoseOrder == 2);
            Assert.Equal((1, 6, "Months"), (avaximDose2.AnchorDoseOrder, avaximDose2.IntervalValue, avaximDose2.IntervalUnit));

            var rotateq = vaccines.Single(x => x.Code == "VX-ROTATEQ");
            var rotateqRegimen = await f.Db.Set<VaccineRegimen>().SingleAsync(x => x.VaccineId == rotateq.MaVacXin);
            var rotateqDose2 = await f.Db.Set<VaccineRegimenDose>().SingleAsync(x => x.RegimenId == rotateqRegimen.Id && x.DoseOrder == 2);
            Assert.Equal((1, 4, "Weeks"), (rotateqDose2.AnchorDoseOrder, rotateqDose2.IntervalValue, rotateqDose2.IntervalUnit));

            var qdenga = vaccines.Single(x => x.Code == "VX-QDENGA");
            var qdengaRegimen = await f.Db.Set<VaccineRegimen>().SingleAsync(x => x.VaccineId == qdenga.MaVacXin);
            var qdengaDose2 = await f.Db.Set<VaccineRegimenDose>().SingleAsync(x => x.RegimenId == qdengaRegimen.Id && x.DoseOrder == 2);
            Assert.Equal((1, 3, "Months"), (qdengaDose2.AnchorDoseOrder, qdengaDose2.IntervalValue, qdengaDose2.IntervalUnit));

            using var http = f.Factory.CreateClient();
            var identity = new TiemChung.Desktop.Services.IdentityApiService(http);
            Assert.True((await identity.LoginAsync("s2.seed", f.Password)).Succeeded);
            var desktop = new TiemChung.Desktop.Services.CatalogApiService(identity);
            var windows = await desktop.ListAsync<TiemChung.Desktop.Services.CatalogVaccine>("vaccines");
            Assert.Equal(15, windows.Length);
            var web = (await f.Client.GetFromJsonAsync<VaccineListResponse>("/api/v1/catalog/vaccines"))!;
            Assert.Equal("database", web.Source); Assert.Equal(15, web.Items.Count);
            Assert.All(web.Items, v => { Assert.NotNull(v.SourceUrl); Assert.NotNull(v.InformationUpdatedAt); Assert.NotEmpty(v.Diseases); Assert.NotEmpty(v.Regimens); });
            Assert.Contains(web.Items, v => v.Code == "VX-GARDASIL-9" && v.Diseases.Any(d => d.Code == "BENH-HPV"));
            Assert.Contains(web.Items, v => v.Code == "VX-INFANRIX-HEXA" && v.Diseases.Length == 6);
            Assert.Contains(windows, v => v.Code == "VX-QDENGA");

            avaxim.Gia = 123456; avaxim.TenVacXin = "Avaxim đã chỉnh sửa"; avaxim.TrangThai = "Inactive";
            avaximDose2.TimingLabel = "Nhân viên đã chỉnh lịch tham khảo";
            avaximDose2.AnchorDoseOrder = avaximDose2.IntervalValue = null;
            avaximDose2.IntervalUnit = null;
            await f.Db.SaveChangesAsync(); f.Db.ChangeTracker.Clear();
            var before = await Snapshot(f.Db);
            await DevelopmentCatalogSeed.RunAsync(f.Db, Development);
            Assert.Equal(before, await Snapshot(f.Db));
            await identity.LogoutAsync();
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task CatalogAuditIsReadOnlyAndSeparatesGraphFromBusinessReferences()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f => {
            await SyntheticCatalogFixture.RunAsync(f.Db, Development);
            var vaccine = await f.Db.Vaccines.SingleAsync(x => x.Code == "VX-DEMO-VAC-XIN-A");
            f.Db.Add(new LoVacXin {
                SoLo = "[DEMO] AUDIT-BUSINESS-REF", MaVacXin = vaccine.MaVacXin,
                NgaySanXuat = new DateOnly(2026,1,1), HanSuDung = new DateOnly(2027,1,1),
                SoLuongBanDau = 1, SoLuongTon = 1, TrangThai = "Active"
            });
            await f.Db.SaveChangesAsync();
            f.Db.ChangeTracker.Clear();
            var before = await Snapshot(f.Db);

            var report = await DevelopmentCatalogAudit.RunAsync(f.Db, Development);
            Assert.True(report.CandidateCount >= 7);
            Assert.Contains(report.Candidates, x => x.EntityType == "Vaccine" && x.Code == "VX-DEMO-VAC-XIN-A"
                && x.BusinessDataReferences == 1 && !x.SafeToDeleteAfterGraphCleanup);
            Assert.Contains(report.Candidates, x => x.EntityType == "Vaccine" && x.Code == "VX-DEMO-VAC-XIN-B"
                && x.BusinessDataReferences == 0 && x.CatalogGraphReferences > 0 && x.SafeToDeleteAfterGraphCleanup);
            Assert.Contains(report.Candidates, x => x.EntityType == "Package" && x.Code.StartsWith("GOI-DEMO-")
                && x.BusinessDataReferences == 0);
            Assert.Contains(report.Candidates.SelectMany(x => x.References), x => x.Table == "LoVacXin" && x.Kind == "BusinessData");
            Assert.Contains("businessDataReferences", DevelopmentCatalogAudit.ToJson(report));
            Assert.Equal(before, await Snapshot(f.Db));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                DevelopmentCatalogAudit.RunAsync(f.Db, new SeedEnvironment("Production")));
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
            Assert.Equal(17, await f.Db.Vaccines.CountAsync());
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
