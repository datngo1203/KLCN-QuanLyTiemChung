using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TiemChung.Api;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using Xunit;

namespace TiemChung.Tests;

public sealed class RF02ACatalogSqlTests
{
    private const string Root = "/api/v1/catalog/";
    private const string Previous = "20261103115754_RF01CustomerPasswordReset";
    private static readonly IHostEnvironment Development = new SeedEnvironment();

    private static Task WithCatalog(Func<CatalogSqlTests.Fixture, Task> test) => SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f => {
        await DevelopmentCatalogSeed.RunAsync(f.Db, Development); await f.Login(); await test(f);
    });
    private static async Task<VaccineRequest> Request(CatalogSqlTests.Fixture f)
    {
        var source = await f.Db.Vaccines.AsNoTracking().OrderBy(x => x.MaVacXin).FirstAsync();
        var disease = await f.Db.Set<Benh>().OrderBy(x => x.MaBenh).FirstAsync();
        return new("Synthetic vaccine", " Synthetic maker ", " Synthetic origin ", 123000, source.MaLoaiVacXin, source.MaLoaiTiemChung,
            Code: "VX-SYNTHETIC-NEW", DiseaseIds: [disease.MaBenh], Description: " Synthetic description ", TargetAudience: " Synthetic audience ",
            AdministrationRoute: "Oral", Dosage: " Synthetic dosage ", Volume: 0.75m, SourceName: " Dữ liệu mô phỏng ", SourceUrl: "https://example.invalid/reference",
            InformationUpdatedAt: new DateTimeOffset(2026, 11, 3, 0, 0, 0, TimeSpan.Zero), Regimens: [
                new("Synthetic two-step regimen", "Synthetic group", "Reference only", [new(1,"Synthetic first step"), new(2,"Synthetic second step",10,20,"Synthetic note")]),
                new("Synthetic alternative", "Another synthetic group", null, [new(1,"Synthetic single step")], "Dữ liệu mô phỏng", "https://example.invalid/regimen")]);
    }
    private static async Task<T> Success<T>(Task<HttpResponseMessage> action, HttpStatusCode status = HttpStatusCode.OK)
    {
        using var response = await action;
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"Expected {status}, actual {response.StatusCode}: {body}");
        return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }
    private static async Task Error(Task<HttpResponseMessage> action, HttpStatusCode status, string code = "validation")
    {
        using var response = await action; Assert.Equal(status, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(code, body.GetProperty("code").GetString());
    }
    private static async Task<JsonElement[]> List(HttpClient client, string query)
    {
        var result = await Success<JsonElement>(client.GetAsync(Root + query));
        Assert.Equal("database", result.GetProperty("source").GetString()); return result.GetProperty("items").EnumerateArray().ToArray();
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task CodesValidateDuplicateCaseInsensitiveAndRemainImmutableForAllFiveResources()
    {
        await WithCatalog(async f => {
            var request = await Request(f);
            foreach (var code in new string?[] { null, "", "VX", "LVX-WRONG", "vx-lowercase", " VX-SPACE", "VX-SPACE ", "VX-A_B", "VX-Ắ", "VX--A", "VX-A\n", "VX-" + new string('A',48) })
                await Error(f.Client.PostAsJsonAsync(Root + "vaccines", request with { Code = code }), HttpStatusCode.BadRequest);
            await Error(f.Client.PostAsJsonAsync(Root + "vaccines", request with { Code = "VX-DEMO-VAC-XIN-A" }), HttpStatusCode.Conflict, "catalogDuplicate");
            foreach (var (route, prefix) in new[] { ("injection-types", "LTC"), ("vaccine-types", "LVX"), ("diseases", "BENH") }) {
                var created = await Success<CatalogTypeDto>(f.Client.PostAsJsonAsync(Root + route, new CatalogTypeRequest(" Synthetic lookup ", null, prefix + "-SYNTHETIC")), HttpStatusCode.Created);
                Assert.Equal("Synthetic lookup", created.Name); Assert.Equal("Active", created.Status);
                await Error(f.Client.PostAsJsonAsync(Root + route, new CatalogTypeRequest("synthetic LOOKUP", "", prefix + "-SECOND")), HttpStatusCode.Conflict, "catalogDuplicate");
                var updated = await Success<CatalogTypeDto>(f.Client.PutAsJsonAsync(Root + route + "/" + created.Id, new CatalogTypeRequest("Renamed", "Updated", Status:"Inactive")));
                Assert.Equal(created.Code, updated.Code); Assert.Equal("Inactive", updated.Status);
                Assert.Single(await List(f.Client, route + "?status=Inactive"));
                await Error(f.Client.PutAsJsonAsync(Root + route + "/" + created.Id, new CatalogTypeRequest("Rejected", "", prefix + "-CHANGED")), HttpStatusCode.BadRequest);
            }
            var vaccine = await Success<VaccineDto>(f.Client.PostAsJsonAsync(Root + "vaccines", request), HttpStatusCode.Created);
            var renamed = await Success<VaccineDto>(f.Client.PutAsJsonAsync(Root + "vaccines/" + vaccine.Id, request with { Name = "Renamed vaccine", Code = null, Status = "Inactive" }));
            Assert.Equal(vaccine.Code, renamed.Code); Assert.Equal("Inactive", renamed.Status);
            await Error(f.Client.PutAsJsonAsync(Root + "vaccines/" + vaccine.Id, request with { Code = "VX-CHANGED" }), HttpStatusCode.BadRequest);
            var packageReq = new PackageRequest("Synthetic package", 10, [new(vaccine.Id,2)], "GOI-SYNTHETIC", TargetAudience:"Synthetic group");
            var package = await Success<PackageDto>(f.Client.PostAsJsonAsync(Root + "packages", packageReq), HttpStatusCode.Created);
            var packageRenamed = await Success<PackageDto>(f.Client.PutAsJsonAsync(Root + "packages/" + package.Id, packageReq with { Name = "Renamed package", Code = null, Status="Inactive" }));
            Assert.Equal(package.Code, packageRenamed.Code); Assert.Equal("Inactive", packageRenamed.Status);
            await Error(f.Client.PutAsJsonAsync(Root + "packages/" + package.Id, packageReq with { Code = "GOI-CHANGED" }), HttpStatusCode.BadRequest);
            f.Db.ChangeTracker.Clear();
            var source = await f.Db.Vaccines.SingleAsync(x => x.MaVacXin == vaccine.Id);
            source.Code = "vx-demo-vac-xin-a";
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync());
            Assert.Contains(Assert.IsType<SqlException>(error.InnerException).Number, new[] {2601,2627});
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task VaccineAndPackageDetailSearchFiltersAndSuggestionsReturnCompleteSqlGraphWithoutDuplicates()
    {
        await WithCatalog(async f => {
            var vaccine = (await List(f.Client, "vaccines"))[0].GetProperty("id").GetInt32();
            var detail = await Success<VaccineDto>(f.Client.GetAsync(Root + "vaccines/" + vaccine));
            Assert.Equal(2, detail.Diseases.Length); Assert.NotEmpty(detail.Code); Assert.NotNull(detail.VaccineType); Assert.NotNull(detail.InjectionType);
            Assert.NotEmpty(detail.VaccineType.Code); Assert.NotEmpty(detail.InjectionType.Code); Assert.Equal(2, Assert.Single(detail.Regimens).Doses.Length);
            var request = await Request(f);
            var created = await Success<VaccineDto>(f.Client.PostAsJsonAsync(Root + "vaccines", request), HttpStatusCode.Created);
            Assert.Equal(2, created.Regimens.Length); Assert.Equal(2, created.Regimens[0].Doses.Length); Assert.Single(created.Regimens[1].Doses);
            Assert.Equal("Synthetic maker", created.Manufacturer); Assert.Equal("Synthetic origin", created.Origin);
            Assert.Equal("Synthetic description", created.Description); Assert.Equal("Synthetic dosage", created.Dosage); Assert.Equal(0.75m, created.Volume);
            Assert.Equal("Dữ liệu mô phỏng", created.SourceName); Assert.Equal(request.SourceUrl, created.SourceUrl); Assert.Equal(request.InformationUpdatedAt, created.InformationUpdatedAt);
            var reloaded = await Success<VaccineDto>(f.Client.GetAsync(Root + "vaccines/" + created.Id));
            Assert.Equal(JsonSerializer.Serialize(created), JsonSerializer.Serialize(reloaded));
            foreach (var search in new[] { detail.Code, detail.Name, detail.Diseases[1].Name })
                Assert.Contains(await List(f.Client, "vaccines?search=" + Uri.EscapeDataString(search)), x => x.GetProperty("id").GetInt32() == detail.Id);
            Assert.Single(await List(f.Client, "vaccines?diseaseId=" + detail.Diseases[1].Id));
            Assert.Single(await List(f.Client, $"vaccines?vaccineTypeId={detail.VaccineTypeId}&injectionTypeId={detail.InjectionTypeId}&minPrice={detail.Price}&maxPrice={detail.Price}&status=Active"));
            var packageRow = Assert.Single(await List(f.Client, "packages"));
            var package = await Success<PackageDto>(f.Client.GetAsync(Root + "packages/" + packageRow.GetProperty("id").GetInt32()));
            Assert.Equal(2, package.Items.Length); Assert.Equal(2, package.TotalDoses); Assert.Equal(2, package.Diseases.Length);
            Assert.All(package.Items, item => { Assert.NotEmpty(item.VaccineCode!); Assert.NotEmpty(item.VaccineName!); });
            foreach (var search in new[] { package.Code, package.Name, detail.Code, detail.Name, detail.Diseases[0].Name })
                Assert.Single(await List(f.Client, "packages?search=" + Uri.EscapeDataString(search)));
            Assert.Single(await List(f.Client, "packages?diseaseId=" + detail.Diseases[0].Id));
            Assert.Single(await List(f.Client, $"packages?vaccineTypeId={detail.VaccineTypeId}&injectionTypeId={detail.InjectionTypeId}&minPrice={package.Price}&maxPrice={package.Price}&status=Active"));
            var metadata = await Success<CatalogSuggestionsDto>(f.Client.GetAsync(Root + "suggestions"));
            Assert.Equal(metadata.Manufacturers.Distinct().Count(), metadata.Manufacturers.Length);
            Assert.Contains("Synthetic maker", metadata.Manufacturers); Assert.Contains("Synthetic origin", metadata.Origins);
            Assert.Equal(new[] {"Intramuscular","Subcutaneous","Intradermal","Oral","Nasal","Other"}, metadata.AdministrationRoutes);
            await Error(f.Client.GetAsync(Root + "vaccines/2147483647"), HttpStatusCode.NotFound, "resourceNotFound");
            await Error(f.Client.GetAsync(Root + "packages/2147483647"), HttpStatusCode.NotFound, "resourceNotFound");
            Assert.Empty(await f.Db.Set<LichHenTiem>().ToArrayAsync()); Assert.Empty(await f.Db.Set<ChiDinhTiem>().ToArrayAsync()); Assert.Empty(await f.Db.Set<MuiTiem>().ToArrayAsync());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task DiseaseCrudRelationUpdatesReferencedDeleteAndLegacyPutPreserveInformation()
    {
        await WithCatalog(async f => {
            var disease = await Success<CatalogTypeDto>(f.Client.PostAsJsonAsync(Root + "diseases", new CatalogTypeRequest("[DEMO] New disease", "Synthetic", "BENH-NEW")), HttpStatusCode.Created);
            var req = await Request(f); var vaccine = await Success<VaccineDto>(f.Client.PostAsJsonAsync(Root + "vaccines", req with { DiseaseIds = [req.DiseaseIds![0],disease.Id] }), HttpStatusCode.Created);
            await Error(f.Client.DeleteAsync(Root + "diseases/" + disease.Id), HttpStatusCode.Conflict, "catalogReferenced");
            await Error(f.Client.PutAsJsonAsync(Root + "vaccines/" + vaccine.Id, req with { DiseaseIds = [] }), HttpStatusCode.BadRequest);
            await Error(f.Client.PutAsJsonAsync(Root + "vaccines/" + vaccine.Id, req with { DiseaseIds = [disease.Id,disease.Id] }), HttpStatusCode.BadRequest);
            var updated = await Success<VaccineDto>(f.Client.PutAsJsonAsync(Root + "vaccines/" + vaccine.Id, req));
            Assert.Single(updated.Diseases); Assert.Equal(req.DiseaseIds![0], updated.Diseases[0].Id);
            using var delete = await f.Client.DeleteAsync(Root + "diseases/" + disease.Id); Assert.Equal(HttpStatusCode.NoContent,delete.StatusCode);
            // An old price/name request omits RF-02 fields; it must never clear the graph or legacy data.
            await f.Db.Vaccines.Where(x => x.MaVacXin == vaccine.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.PhanUngSauTiem,"Legacy preserved"));
            var oldPut = new VaccineRequest("Legacy price update", req.Manufacturer, req.Origin, 456000, req.VaccineTypeId, req.InjectionTypeId);
            var legacy = await Success<VaccineDto>(f.Client.PutAsJsonAsync(Root + "vaccines/" + vaccine.Id, oldPut));
            Assert.Equal(updated.Code,legacy.Code); Assert.Equal(updated.SourceName,legacy.SourceName); Assert.Equal(updated.Dosage,legacy.Dosage);
            Assert.Equal(updated.Volume,legacy.Volume); Assert.Equal(updated.Regimens.Length,legacy.Regimens.Length); Assert.Equal(updated.Diseases,legacy.Diseases);
            Assert.Equal("Legacy preserved",await f.Db.Vaccines.AsNoTracking().Where(x=>x.MaVacXin==vaccine.Id).Select(x=>x.PhanUngSauTiem).SingleAsync());
            Assert.DoesNotContain("phanUngSauTiem",JsonSerializer.Serialize(legacy,new JsonSerializerOptions(JsonSerializerDefaults.Web)),StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task InvalidFieldsAndNestedDosesRejectTheWholeMutation()
    {
        await WithCatalog(async f => {
            var req = await Request(f);
            var invalidDoseSets = new VaccineRegimenDoseRequest[][] { [], [new(1,"A"),new(1,"B")], [new(0,"A")], [new(21,"A")],
                [new(1,"A",-1)], [new(1,"A",0,-1)], [new(1,"A",20,10)], [new(1,"")], [new(1,new string('a',251))], [new(1,"A",Note:new string('a',501))], Enumerable.Range(1,21).Select(x=>new VaccineRegimenDoseRequest(x,"A")).ToArray() };
            var vaccine = await Success<VaccineDto>(f.Client.PostAsJsonAsync(Root + "vaccines",req),HttpStatusCode.Created);
            var before = JsonSerializer.Serialize(vaccine);
            foreach (var doses in invalidDoseSets)
                await Error(f.Client.PutAsJsonAsync(Root + "vaccines/" + vaccine.Id,req with { Name="Must rollback",DiseaseIds=[],Regimens=[new("Synthetic","Synthetic",null,doses)] }),HttpStatusCode.BadRequest);
            // Exercise nested validation independently from disease validation.
            foreach (var doses in invalidDoseSets)
                await Error(f.Client.PutAsJsonAsync(Root + "vaccines/" + vaccine.Id,req with { Name="Must rollback",Regimens=[new("Synthetic","Synthetic",null,doses)] }),HttpStatusCode.BadRequest);
            foreach (var invalid in new[] { req with { DiseaseIds=[] }, req with { TargetAudience=" " }, req with { AdministrationRoute=null }, req with { AdministrationRoute="IM" },
                req with { AdministrationRoute="Other" }, req with { AdministrationRouteNote="Not allowed" }, req with { SourceUrl="javascript:alert(1)" }, req with { SourceUrl="https://user:pass@example.invalid" },
                req with { Volume=-1 }, req with { Volume=0.001m }, req with { Dosage=new string('a',201) }, req with { Status="Draft" }, req with { Description=new string('a',2001) },
                req with { SourceName=new string('a',251) }, req with { Regimens=[new("Synthetic",new string('a',1001),null,[new(1,"A")])] } })
                await Error(f.Client.PostAsJsonAsync(Root + "vaccines",invalid with { Code="VX-INVALID-NEW" }),HttpStatusCode.BadRequest);
            Assert.Equal(before,JsonSerializer.Serialize(await Success<VaccineDto>(f.Client.GetAsync(Root + "vaccines/" + vaccine.Id))));
            Assert.Equal(3,await f.Db.Vaccines.CountAsync());
            var validOther = req with { Code="VX-OTHER",AdministrationRoute="Other",AdministrationRouteNote=" Synthetic other route " };
            var other = await Success<VaccineDto>(f.Client.PostAsJsonAsync(Root + "vaccines",validOther),HttpStatusCode.Created);
            Assert.Equal("Synthetic other route",other.AdministrationRouteNote);
            var inactiveDisease=await f.Db.Set<Benh>().FirstAsync(); inactiveDisease.TrangThai="Inactive";await f.Db.SaveChangesAsync();
            await Error(f.Client.PostAsJsonAsync(Root + "vaccines",req with { Code="VX-INACTIVE-DISEASE",DiseaseIds=[inactiveDisease.MaBenh] }),HttpStatusCode.BadRequest);
            foreach(var route in new[]{"vaccines","packages","diseases","injection-types","vaccine-types"})
                await Error(f.Client.GetAsync(Root+route+"?status=Draft"),HttpStatusCode.BadRequest);
            await Error(f.Client.PostAsJsonAsync(Root+"packages",new PackageRequest("Invalid",1,[new(vaccine.Id,1)],"GOI-INVALID")),HttpStatusCode.BadRequest);
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("SecurityAudit")]
    [InlineData("VaccineRegimenDose")]
    public async Task SqlOrAuditFailureRollsBackVaccineLinksRegimensDosesAndAudit(string failingTable)
    {
        await WithCatalog(async f => {
            var req=await Request(f); var vaccine=await Success<VaccineDto>(f.Client.PostAsJsonAsync(Root+"vaccines",req),HttpStatusCode.Created);
            var snapshot=JsonSerializer.Serialize(vaccine); var auditCount=await f.Db.Set<SecurityAudit>().CountAsync();
            var otherDisease=await f.Db.Set<Benh>().OrderByDescending(x=>x.MaBenh).FirstAsync();
            // Table names are fixed InlineData, never request data. A DB failure occurs after child replacements start.
            var triggerSql=failingTable == "SecurityAudit"
                ? "CREATE TRIGGER RF02ARejectWrite ON [SecurityAudit] AFTER INSERT AS BEGIN THROW 51001, 'Synthetic RF02A failure', 1; END;"
                : "CREATE TRIGGER RF02ARejectWrite ON [VaccineRegimenDose] AFTER INSERT AS BEGIN THROW 51001, 'Synthetic RF02A failure', 1; END;";
            await f.Db.Database.ExecuteSqlRawAsync(triggerSql);
            await Error(f.Client.PutAsJsonAsync(Root+"vaccines/"+vaccine.Id,req with { Name="Must rollback",DiseaseIds=[otherDisease.MaBenh],Regimens=[new("Replacement","Synthetic",null,[new(1,"Changed")])] }),HttpStatusCode.ServiceUnavailable,"dependencyUnavailable");
            await Error(f.Client.PostAsJsonAsync(Root+"vaccines",req with { Code="VX-ROLLBACK-CREATE" }),HttpStatusCode.ServiceUnavailable,"dependencyUnavailable");
            Assert.Equal(snapshot,JsonSerializer.Serialize(await Success<VaccineDto>(f.Client.GetAsync(Root+"vaccines/"+vaccine.Id))));
            if(failingTable=="SecurityAudit") {
                var package=(await List(f.Client,"packages")).Single();var packageId=package.GetProperty("id").GetInt32();
                var savedPackage=await Success<PackageDto>(f.Client.GetAsync(Root+"packages/"+packageId));
                var packageRequest=new PackageRequest("Rollback package",123,[new(vaccine.Id,5)],"GOI-ROLLBACK",TargetAudience:"Synthetic group");
                await Error(f.Client.PostAsJsonAsync(Root+"packages",packageRequest),HttpStatusCode.ServiceUnavailable,"dependencyUnavailable");
                await Error(f.Client.PutAsJsonAsync(Root+"packages/"+packageId,packageRequest with {Code=null}),HttpStatusCode.ServiceUnavailable,"dependencyUnavailable");
                Assert.Equal(JsonSerializer.Serialize(savedPackage),JsonSerializer.Serialize(await Success<PackageDto>(f.Client.GetAsync(Root+"packages/"+packageId))));
                Assert.Single(await f.Db.Set<GoiVacXin>().ToArrayAsync());
            }
            Assert.Equal(auditCount,await f.Db.Set<SecurityAudit>().CountAsync()); Assert.Equal(3,await f.Db.Vaccines.CountAsync());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task SeedIsIdempotentFullGraphAndObsoleteDemoFlagCannotEnableRuntimeFallback()
    {
        await WithCatalog(async f => {
            var before=JsonSerializer.Serialize(await List(f.Client,"vaccines"));
            await DevelopmentCatalogSeed.RunAsync(f.Db,Development);await DevelopmentCatalogSeed.RunAsync(f.Db,Development);
            f.Config["Catalog:UseDemoData"]="true";
            Assert.Equal(before,JsonSerializer.Serialize(await List(f.Client,"vaccines")));
            Assert.Equal(2,await f.Db.Vaccines.CountAsync());Assert.Equal(2,await f.Db.Set<Benh>().CountAsync());
            Assert.Equal(3,await f.Db.Set<VacXin_Benh>().CountAsync());Assert.Equal(2,await f.Db.Set<VaccineRegimen>().CountAsync());
            Assert.Equal(4,await f.Db.Set<VaccineRegimenDose>().CountAsync());
            Assert.All(await f.Db.Vaccines.ToArrayAsync(),v=>{Assert.StartsWith("VX-DEMO-",v.Code);Assert.StartsWith("[DEMO]",v.TenVacXin);Assert.Equal("Dữ liệu mô phỏng",v.NguonThongTin);});
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task MigrationBackfillsSemanticCollisionSafeCodesAndPreservesAllLegacyRowsAndKeys()
    {
        await SqlFixtureLifecycle.RunAsync(()=>CatalogSqlTests.Fixture.Create(async db=>{
            await db.GetService<IMigrator>().MigrateAsync(Previous);
            await db.Database.ExecuteSqlRawAsync("""
                INSERT LoaiTiemChung (TenLoaiTiemChung,MoTa) VALUES (N'Đường tiêm thử',N'Legacy');
                INSERT LoaiVacXin (TenLoaiVacXin,MoTa) VALUES (N'Loại thử',N'Legacy');
                INSERT Benh (TenBenh,MoTa) VALUES (N'Bệnh thử',N'Legacy');
                INSERT VacXin (MaLoaiVacXin,MaLoaiTiemChung,TenVacXin,NhaSanXuat,XuatXu,LieuLuong,TheTich,PhanUngSauTiem,Gia,TrangThai)
                VALUES (1,1,N'Vắc xin Đỏ',N'Maker',N'Origin',N'Legacy liều',1.25,N'Legacy reaction',100,'Active'),
                       (1,1,N'Vac xin Do',N'Maker',N'Origin',N'',0,N'Keep me',200,'Inactive'),
                       (1,1,N'Vac xin Do-2',N'Maker',N'Origin',N'',0,N'',300,'Active'),
                       (1,1,REPLICATE(N'A',150),N'Maker',N'Origin',N'',0,N'',400,'Active'),
                       (1,1,REPLICATE(N'A',150),N'Maker',N'Origin',N'',0,N'',500,'Active');
                INSERT VacXin_Benh (MaVacXin,MaBenh) VALUES (1,1);
                INSERT GoiVacXin (TenGoi,GiaGoi,TrangThai) VALUES (N'Gói thử',400,'Inactive');
                INSERT GoiVacXin_VacXin (MaGoi,MaVacXin,SoLuong) VALUES (1,1,3);
                """);
        }),async f=>{
            await f.Db.Database.MigrateAsync();await f.Db.Database.MigrateAsync();
            Assert.Equal(new[]{"VX-VAC-XIN-DO","VX-VAC-XIN-DO-2","VX-VAC-XIN-DO-2-3"},await f.Db.Vaccines.OrderBy(x=>x.MaVacXin).Take(3).Select(x=>x.Code).ToArrayAsync());
            var longCodes=await f.Db.Vaccines.Where(x=>x.MaVacXin>3).OrderBy(x=>x.MaVacXin).Select(x=>x.Code).ToArrayAsync();
            Assert.Equal(new[]{"VX-"+new string('A',47),"VX-"+new string('A',45)+"-5"},longCodes);
            var first=await f.Db.Vaccines.SingleAsync(x=>x.MaVacXin==1);Assert.Equal("Legacy liều",first.LieuLuong);Assert.Equal(1.25m,first.TheTich);Assert.Equal("Legacy reaction",first.PhanUngSauTiem);
            Assert.Null(first.DoiTuongSuDung);Assert.Null(first.NguonThongTin);Assert.Null(first.DuongDung);Assert.Empty(await f.Db.Set<VaccineRegimen>().ToArrayAsync());
            Assert.Equal("Inactive",(await f.Db.Vaccines.SingleAsync(x=>x.MaVacXin==2)).TrangThai);
            var link=Assert.Single(await f.Db.Set<VacXin_Benh>().ToArrayAsync());Assert.Equal(1,link.MaVacXin);Assert.Equal(1,link.MaBenh);
            Assert.Equal("LTC-DUONG-TIEM-THU",(await f.Db.Set<LoaiTiemChung>().SingleAsync()).Code);
            Assert.Equal("LVX-LOAI-THU",(await f.Db.Set<LoaiVacXin>().SingleAsync()).Code);
            var disease=await f.Db.Set<Benh>().SingleAsync();Assert.Equal("BENH-BENH-THU",disease.Code);Assert.Equal("Active",disease.TrangThai);
            var package=await f.Db.Set<GoiVacXin>().SingleAsync();Assert.Equal("GOI-GOI-THU",package.Code);Assert.Equal("Inactive",package.TrangThai);
            Assert.Equal(3,(await f.Db.Set<GoiVacXin_VacXin>().SingleAsync()).SoLuong);
            Assert.False(f.Db.Database.HasPendingModelChanges());
        });
    }

    [Theory, Trait("Category", "SqlIntegration")]
    [InlineData("LoaiTiemChung")]
    [InlineData("LoaiVacXin")]
    [InlineData("Benh")]
    public async Task MigrationRefusesLegacyDuplicateNamesBeforeAddingUniqueConstraints(string table)
    {
        var error=await Assert.ThrowsAsync<SqlException>(()=>CatalogSqlTests.Fixture.Create(async db=>{
            await db.GetService<IMigrator>().MigrateAsync(Previous);
            var insertSql=table switch {
                "LoaiTiemChung" => "INSERT LoaiTiemChung (TenLoaiTiemChung,MoTa) VALUES ('Legacy',''),('LEGACY','');",
                "LoaiVacXin" => "INSERT LoaiVacXin (TenLoaiVacXin,MoTa) VALUES ('Legacy',''),('LEGACY','');",
                _ => "INSERT Benh (TenBenh,MoTa) VALUES ('Legacy',''),('LEGACY','');"
            };
            await db.Database.ExecuteSqlRawAsync(insertSql);
        }));
        Assert.Equal(51000,error.Number);Assert.Contains("duplicate legacy",error.Message);
    }

    private sealed class SeedEnvironment : IHostEnvironment
    {
        public string EnvironmentName {get;set;}="Development";
        public string ApplicationName {get;set;}="RF02ATests";
        public string ContentRootPath {get;set;}=AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider {get;set;}=new NullFileProvider();
    }
}
