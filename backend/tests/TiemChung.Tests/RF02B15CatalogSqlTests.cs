using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TiemChung.Contracts;
using TiemChung.Domain.Entities;
using Xunit;

namespace TiemChung.Tests;

public sealed class RF02B15CatalogSqlTests
{
    private const string Root = "/api/v1/catalog/";
    private const string Previous = "20261103192651_RF02AActivePackagePreflight";
    private static Task Run(Func<CatalogSqlTests.Fixture, Task> test) => SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f => {
        await f.Login(); await test(f);
    });
    private static async Task<T> Read<T>(Task<HttpResponseMessage> request, HttpStatusCode status = HttpStatusCode.OK)
    {
        using var response = await request;
        Assert.True(response.StatusCode == status, $"Expected {status}, actual {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
    private static async Task Error(Task<HttpResponseMessage> request, HttpStatusCode status, string code = "validation")
    {
        using var response = await request; Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }
    private static VaccineTechnologyRequest Technology(string suffix) => new("[DEMO] Technology " + suffix, " Synthetic description ", "CNVX-" + suffix,
        TechnicalCharacteristics: " Synthetic technical details ", ProfessionalNotes: " Reference only; no clinical rule ",
        SourceName: " Synthetic source ", SourceUrl: "https://example.invalid/technology", InformationUpdatedAt: new(2026, 11, 4, 0, 0, 0, TimeSpan.Zero));
    private static async Task<VaccineRequest> Vaccine(CatalogSqlTests.Fixture f)
    {
        var disease = new Benh { Code = "BENH-SYNTHETIC", TenBenh = "[DEMO] Disease" }; f.Db.Add(disease); await f.Db.SaveChangesAsync();
        return new("[DEMO] Vaccine", "Synthetic maker", "Synthetic origin", 123000, Code: "VX-SYNTHETIC", DiseaseIds: [disease.MaBenh],
            TargetAudience: "Synthetic audience", AdministrationRoute: "Oral");
    }
    private static async Task<JsonElement[]> List(HttpClient client, string query) =>
        (await Read<JsonElement>(client.GetAsync(Root + query))).GetProperty("items").EnumerateArray().ToArray();

    private static Task<string> Snapshot(TiemChung.Infrastructure.Data.TiemChungDbContext db, string table)
    {
        // Only the fixed table list in the migration test supplies identifiers.
        var sql = "SELECT (SELECT * FROM [" + table + "] FOR JSON PATH) AS Value";
        return db.Database.SqlQueryRaw<string>(sql).SingleAsync();
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task TechnologyMetadataCrudValidationSearchStatusCodeAndUniqueNamesPersist()
    {
        await Run(async f => {
            var req = Technology("A");
            foreach (var bad in new[] { req with { Code = null }, req with { Code = "LVX-A" }, req with { Code = "cnvx-a" },
                req with { Name = " " }, req with { Name = new('a', 121) }, req with { Description = new('a', 501) },
                req with { TechnicalCharacteristics = new('a', 2001) }, req with { ProfessionalNotes = new('a', 2001) },
                req with { SourceName = new('a', 251) }, req with { Status = "Draft" } })
                await Error(f.Client.PostAsJsonAsync(Root + "vaccine-technologies", bad), HttpStatusCode.BadRequest);
            foreach (var url in new[] { "ftp://example.invalid/a", "javascript:alert(1)", "file:///c:/a", "/relative", "https://user:pass@example.invalid", new string('a', 1001) })
                await Error(f.Client.PostAsJsonAsync(Root + "vaccine-technologies", req with { SourceUrl = url }), HttpStatusCode.BadRequest);
            var created = await Read<VaccineTechnologyDto>(f.Client.PostAsJsonAsync(Root + "vaccine-technologies", req), HttpStatusCode.Created);
            Assert.Equal("Synthetic technical details", created.TechnicalCharacteristics); Assert.Equal(req.InformationUpdatedAt, created.InformationUpdatedAt);
            Assert.Equal("Reference only; no clinical rule", created.ProfessionalNotes); Assert.Equal("Synthetic source", created.SourceName);
            Assert.Equal("Synthetic description", created.Description); Assert.Equal(req.SourceUrl, created.SourceUrl);
            Assert.Equal(created, await Read<VaccineTechnologyDto>(f.Client.GetAsync(Root + "vaccine-technologies/" + created.Id)));
            await Error(f.Client.PostAsJsonAsync(Root + "vaccine-technologies", req with { Name = req.Name.ToLowerInvariant(), Code = "CNVX-B" }), HttpStatusCode.Conflict, "catalogDuplicate");
            await Error(f.Client.PostAsJsonAsync(Root + "vaccine-technologies", req with { Name = "Other" }), HttpStatusCode.Conflict, "catalogDuplicate");
            await Error(f.Client.PutAsJsonAsync(Root + "vaccine-technologies/" + created.Id, req with { Code = "CNVX-CHANGED" }), HttpStatusCode.BadRequest);
            var changed = await Read<VaccineTechnologyDto>(f.Client.PutAsJsonAsync(Root + "vaccine-technologies/" + created.Id,
                req with { Name = "[DEMO] Updated", Code = null, Status = "Inactive", SourceUrl = "http://example.invalid/new", TechnicalCharacteristics = null }));
            Assert.Equal(created.Code, changed.Code); Assert.Null(changed.TechnicalCharacteristics); Assert.Equal("Inactive", changed.Status);
            Assert.Single(await List(f.Client, "vaccine-technologies?search=CNVX-A&status=Inactive"));
            Assert.Empty(await List(f.Client, "vaccine-technologies?status=Active"));
            f.Db.ChangeTracker.Clear(); var sql = await f.Db.Set<CongNgheVacXin>().SingleAsync();
            Assert.Equal(changed.SourceUrl, sql.DuongDanNguon); Assert.Equal(changed.ProfessionalNotes, sql.GhiChuChuyenMon);
            Assert.Equal(HttpStatusCode.NoContent, (await f.Client.DeleteAsync(Root + "vaccine-technologies/" + created.Id)).StatusCode);
            await Error(f.Client.GetAsync(Root + "vaccine-technologies/" + created.Id), HttpStatusCode.NotFound, "resourceNotFound");
            Assert.Equal(3, await f.Db.Set<SecurityAudit>().CountAsync(a => a.ResourceType == "vaccine-technologies"));
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task OptionalManyTechnologiesPreserveInactiveLinksRejectNewInactiveAndAllowExplicitRemoval()
    {
        await Run(async f => {
            var req = await Vaccine(f);
            var vaccine = await Read<VaccineDto>(f.Client.PostAsJsonAsync(Root + "vaccines", req), HttpStatusCode.Created);
            Assert.Empty(vaccine.Technologies); Assert.Equal("Active", vaccine.Status);
            var a = await Read<VaccineTechnologyDto>(f.Client.PostAsJsonAsync(Root + "vaccine-technologies", Technology("A")), HttpStatusCode.Created);
            var b = await Read<VaccineTechnologyDto>(f.Client.PostAsJsonAsync(Root + "vaccine-technologies", Technology("B")), HttpStatusCode.Created);
            foreach (var ids in new int[][] { [0], [-1], [int.MaxValue], [a.Id, a.Id] })
                await Error(f.Client.PutAsJsonAsync(Root + "vaccines/" + vaccine.Id, req with { TechnologyIds = ids }), HttpStatusCode.BadRequest);
            var linked = await Read<VaccineDto>(f.Client.PutAsJsonAsync(Root + "vaccines/" + vaccine.Id, req with { TechnologyIds = [a.Id, b.Id] }));
            Assert.Equal(new[] { a.Id, b.Id }, linked.Technologies.Select(t => t.Id));
            await Error(f.Client.DeleteAsync(Root + "vaccine-technologies/" + a.Id), HttpStatusCode.Conflict, "catalogReferenced");
            await Read<VaccineTechnologyDto>(f.Client.PutAsJsonAsync(Root + "vaccine-technologies/" + a.Id, Technology("A") with { Status = "Inactive" }));
            Assert.Equal(linked.Technologies, (await Read<VaccineDto>(f.Client.PutAsJsonAsync(Root + "vaccines/" + vaccine.Id, req))).Technologies);
            Assert.Equal(2, (await Read<VaccineDto>(f.Client.PutAsJsonAsync(Root + "vaccines/" + vaccine.Id, req with { TechnologyIds = [a.Id, b.Id] }))).Technologies.Length);
            await Error(f.Client.PostAsJsonAsync(Root + "vaccines", req with { Code = "VX-OTHER", TechnologyIds = [a.Id] }), HttpStatusCode.BadRequest);
            var cleared = await Read<VaccineDto>(f.Client.PutAsJsonAsync(Root + "vaccines/" + vaccine.Id, req with { TechnologyIds = [] })); Assert.Empty(cleared.Technologies);
            await Error(f.Client.PutAsJsonAsync(Root + "vaccines/" + vaccine.Id, req with { TechnologyIds = [a.Id] }), HttpStatusCode.BadRequest);
            Assert.Equal(HttpStatusCode.NoContent, (await f.Client.DeleteAsync(Root + "vaccine-technologies/" + a.Id)).StatusCode);
            Assert.Empty(await f.Db.Set<VacXin_CongNghe>().ToArrayAsync());
            Assert.Empty(await f.Db.Set<LichHenTiem>().ToArrayAsync()); Assert.Empty(await f.Db.Set<ChiDinhTiem>().ToArrayAsync()); Assert.Empty(await f.Db.Set<MuiTiem>().ToArrayAsync());
            var json = JsonSerializer.Serialize(cleared, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.DoesNotContain("vaccineType", json); Assert.DoesNotContain("injectionType", json);
            foreach (var retired in new[] { "injection-types", "vaccine-types" }) {
                Assert.Equal(HttpStatusCode.NotFound, (await f.Client.GetAsync(Root + retired)).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, (await f.Client.PostAsJsonAsync(Root + retired, new { name = "Old" })).StatusCode);
            }
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task DiseaseAndTechnologyFiltersInferPackagesFromSameComponentWithoutDuplicates()
    {
        await Run(async f => {
            var req = await Vaccine(f);
            var a = await Read<VaccineTechnologyDto>(f.Client.PostAsJsonAsync(Root + "vaccine-technologies", Technology("A")), HttpStatusCode.Created);
            var b = await Read<VaccineTechnologyDto>(f.Client.PostAsJsonAsync(Root + "vaccine-technologies", Technology("B")), HttpStatusCode.Created);
            var vaccine = await Read<VaccineDto>(f.Client.PostAsJsonAsync(Root + "vaccines", req with { TechnologyIds = [a.Id, b.Id] }), HttpStatusCode.Created);
            var disease = new Benh { Code = "BENH-OTHER", TenBenh = "[DEMO] Other" }; f.Db.Add(disease); await f.Db.SaveChangesAsync();
            var other = await Read<VaccineDto>(f.Client.PostAsJsonAsync(Root + "vaccines", req with { Code = "VX-OTHER", DiseaseIds = [disease.MaBenh] }), HttpStatusCode.Created);
            await Read<PackageDto>(f.Client.PostAsJsonAsync(Root + "packages", new PackageRequest("[DEMO] Package", 200000, [new(vaccine.Id, 2), new(other.Id, 3)], "GOI-SYNTHETIC", TargetAudience: "Synthetic")), HttpStatusCode.Created);
            foreach (var resource in new[] { "vaccines", "packages" }) {
                Assert.Single(await List(f.Client, $"{resource}?diseaseId={req.DiseaseIds![0]}&technologyId={a.Id}"));
                Assert.Empty(await List(f.Client, $"{resource}?diseaseId={disease.MaBenh}&technologyId={a.Id}"));
                Assert.Single(await List(f.Client, $"{resource}?technologyId={b.Id}&search=Synthetic"));
                foreach (var bad in new[] { "technologyId=0", "technologyId=-1", "diseaseId=0" }) await Error(f.Client.GetAsync(Root + resource + "?" + bad), HttpStatusCode.BadRequest);
            }
            var package = Assert.Single(await List(f.Client, "packages")); Assert.Equal(5, package.GetProperty("totalDoses").GetInt32());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task AuditFailureRollsBackTechnologyMetadataAndVaccineTechnologyReplacement()
    {
        await Run(async f => {
            var req = await Vaccine(f);
            var a = await Read<VaccineTechnologyDto>(f.Client.PostAsJsonAsync(Root + "vaccine-technologies", Technology("A")), HttpStatusCode.Created);
            var b = await Read<VaccineTechnologyDto>(f.Client.PostAsJsonAsync(Root + "vaccine-technologies", Technology("B")), HttpStatusCode.Created);
            var vaccine = await Read<VaccineDto>(f.Client.PostAsJsonAsync(Root + "vaccines", req with { TechnologyIds = [a.Id] }), HttpStatusCode.Created);
            var audits = await f.Db.Set<SecurityAudit>().CountAsync();
            await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RF02B15RejectAudit ON SecurityAudit AFTER INSERT AS BEGIN THROW 51001, 'Synthetic failure', 1; END;");
            await Error(f.Client.PutAsJsonAsync(Root + "vaccine-technologies/" + a.Id, Technology("A") with { TechnicalCharacteristics = "Must rollback" }), HttpStatusCode.ServiceUnavailable, "dependencyUnavailable");
            await Error(f.Client.PutAsJsonAsync(Root + "vaccines/" + vaccine.Id, req with { TechnologyIds = [b.Id] }), HttpStatusCode.ServiceUnavailable, "dependencyUnavailable");
            Assert.Equal(a, await Read<VaccineTechnologyDto>(f.Client.GetAsync(Root + "vaccine-technologies/" + a.Id)));
            Assert.Equal(vaccine.Technologies, (await Read<VaccineDto>(f.Client.GetAsync(Root + "vaccines/" + vaccine.Id))).Technologies);
            Assert.Equal(audits, await f.Db.Set<SecurityAudit>().CountAsync());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task WindowsTechnologyMetadataAndOptionalSelectionPersistWithoutDroppingInactiveRelations()
    {
        await Run(async f => {
            var req = await Vaccine(f);
            var a = await Read<VaccineTechnologyDto>(f.Client.PostAsJsonAsync(Root + "vaccine-technologies", Technology("A")), HttpStatusCode.Created);
            var b = await Read<VaccineTechnologyDto>(f.Client.PostAsJsonAsync(Root + "vaccine-technologies", Technology("B") with { Status = "Inactive" }), HttpStatusCode.Created);
            var vaccine = await Read<VaccineDto>(f.Client.PostAsJsonAsync(Root + "vaccines", req with { TechnologyIds = [a.Id] }), HttpStatusCode.Created);
            await Read<VaccineTechnologyDto>(f.Client.PutAsJsonAsync(Root + "vaccine-technologies/" + a.Id, Technology("A") with { Status = "Inactive" }));
            using var http = f.Factory.CreateClient(); var identity = new TiemChung.Desktop.Services.IdentityApiService(http);
            Assert.True((await identity.LoginAsync("s2.seed", f.Password)).Succeeded);
            var vm = new TiemChung.Desktop.ViewModels.CatalogViewModel(new(identity), identity.Permissions) { Confirm = _ => true };
            async Task Command(TiemChung.Desktop.ViewModels.RelayCommand command) {
                Assert.True(command.CanExecute(null)); command.Execute(null);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                while (!vm.CanInteract) await Task.Delay(10, timeout.Token);
            }
            await vm.InitializeAsync(); vm.Selected = Assert.Single(vm.Rows);
            Assert.Equal(a.Id, Assert.Single(vm.TechnologySelection.Selected).Id);
            Assert.False(Assert.Single(vm.TechnologySelection.Selected).Active);
            Assert.False(vm.TechnologySelection.Add(b.Id));
            vm.Name = "[DEMO] Windows update"; await Command(vm.SaveCommand); Assert.StartsWith("Đã cập nhật", vm.Feedback);
            Assert.Equal(a.Id, Assert.Single((await Read<VaccineDto>(f.Client.GetAsync(Root + "vaccines/" + vaccine.Id))).Technologies).Id);
            vm.TechnologySelection.Remove(a.Id); await Command(vm.SaveCommand); Assert.StartsWith("Đã cập nhật", vm.Feedback);
            Assert.Empty((await Read<VaccineDto>(f.Client.GetAsync(Root + "vaccines/" + vaccine.Id))).Technologies);
            await vm.SelectResourceAsync("Công nghệ vắc xin"); vm.Selected = vm.Rows.Single(t => t.Id == a.Id);
            Assert.Equal("Synthetic technical details", vm.TechnicalCharacteristics); Assert.Equal("Synthetic source", vm.SourceName);
            vm.TechnicalCharacteristics = "[DEMO] Windows technical metadata"; vm.ProfessionalNotes = "[DEMO] Reference only";
            vm.SourceUrl = "https://example.invalid/windows"; vm.InformationDate = new(2026, 11, 4);
            await Command(vm.SaveCommand); Assert.StartsWith("Đã cập nhật", vm.Feedback);
            f.Db.ChangeTracker.Clear(); var row = await f.Db.Set<CongNgheVacXin>().SingleAsync(t => t.MaCongNghe == a.Id);
            Assert.Equal(vm.TechnicalCharacteristics, row.DacDiemKyThuat); Assert.Equal(vm.ProfessionalNotes, row.GhiChuChuyenMon);
            Assert.Equal(vm.SourceUrl, row.DuongDanNguon); Assert.Equal(new DateOnly(2026, 11, 4), DateOnly.FromDateTime(row.NgayCapNhatThongTin!.Value.ToOffset(TimeSpan.FromHours(7)).Date));
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task MigrationRetiresOnlyClassificationsPreservingEntireCoreGraphAndRepeatsCleanly()
    {
        var snapshots = new Dictionary<string, string>();
        string[] tables = ["VacXin_Benh", "GoiVacXin", "GoiVacXin_VacXin", "DangKyDichVu", "ChiTietVacXinLe", "ChiTietGoiVacXin", "ThanhToan", "LoVacXin", "LichHenTiem", "PhieuKhamSangLoc", "ChiDinhTiem", "MuiTiem", "BienDongKho", "TheoDoiSauTiem"];
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(async db => {
            await db.GetService<IMigrator>().MigrateAsync(Previous);
            await db.Database.ExecuteSqlRawAsync("""
                INSERT LoaiTiemChung (Code,TenLoaiTiemChung,MoTa) VALUES ('LTC-LEGACY','Unverified injection semantics','');
                INSERT LoaiVacXin (Code,TenLoaiVacXin,MoTa) VALUES ('LVX-LEGACY','Not a confirmed technology','');
                INSERT Benh (Code,TenBenh,MoTa) VALUES ('BENH-LEGACY','[DEMO] Disease','');
                INSERT VacXin (Code,MaLoaiVacXin,MaLoaiTiemChung,TenVacXin,NhaSanXuat,XuatXu,LieuLuong,TheTich,PhanUngSauTiem,Gia,TrangThai)
                VALUES ('VX-LEGACY',1,1,'[DEMO] Vaccine','Synthetic','Synthetic','Legacy dose',0.5,'Legacy reaction',100,'Active');
                INSERT VacXin_Benh (MaVacXin,MaBenh) VALUES (1,1);
                INSERT GoiVacXin (Code,TenGoi,GiaGoi,TrangThai) VALUES ('GOI-LEGACY','[DEMO] Package',200,'Active');
                INSERT GoiVacXin_VacXin (MaGoi,MaVacXin,SoLuong) VALUES (1,1,2);
                """);
            var employee = new NhanVien { HoTen = "[DEMO] Staff", CCCD = "000000088015", TrangThai = "Active", ChucDanh = "DieuDuong" };
            db.Add(employee); await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync("INSERT KhachHang(HoTen, SoDienThoai, Email) VALUES(N'[DEMO] Customer', '', '')");
            var customerId = await db.Database.SqlQueryRaw<int>("SELECT MaKhachHang AS [Value] FROM KhachHang").SingleAsync();
            var recipient = new NguoiDuocTiem { MaKhachHang = customerId, HoTen = "[DEMO] Recipient", NgaySinh = new(2020,1,1) }; db.Add(recipient); await db.SaveChangesAsync();
            var registration = new DangKyDichVu { MaKhachHang = customerId, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, NgayDangKy = DateTime.UtcNow, TrangThai = "DaThanhToan" }; db.Add(registration); await db.SaveChangesAsync();
            var item = new ChiTietVacXinLe { MaDangKy = registration.MaDangKy, MaVacXin = 1, SoLuong = 1, GiaTaiThoiDiemDangKy = 100 };
            db.AddRange(item, new ChiTietGoiVacXin { MaDangKy = registration.MaDangKy, MaGoi = 1, SoLuongGoi = 1, GiaTaiThoiDiemDangKy = 200 },
                new ThanhToan { MaDangKy = registration.MaDangKy, MaNhanVien = employee.MaNhanVien, SoTien = 300, ThoiDiem = DateTime.UtcNow, TrangThai = "ThanhCong", PhuongThuc = "MoPhong", LoaiThanhToan = "ToanBo" },
                new LoVacXin { SoLo = "[DEMO] LEGACY", MaVacXin = 1, NgaySanXuat = new(2026,1,1), HanSuDung = new(2027,1,1), SoLuongBanDau = 10, SoLuongTon = 9, TrangThai = "Active" }); await db.SaveChangesAsync();
            var appointment = new LichHenTiem { MaDangKy = registration.MaDangKy, MaChiTietVacXin = item.MaChiTietVacXin, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, ThoiGianHen = DateTime.UtcNow, DiemTiem = "[DEMO]", TrangThai = "DaHen" }; db.Add(appointment); await db.SaveChangesAsync();
            var clinical = new PhieuKhamSangLoc { MaLichHen = appointment.MaLichHen, MaDangKy = registration.MaDangKy, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, MaNhanVien = employee.MaNhanVien, ThoiDiemKham = DateTimeOffset.UtcNow, KetLuan = "DuDieuKien" }; db.Add(clinical); await db.SaveChangesAsync();
            var indication = new ChiDinhTiem { MaPhieuKham = clinical.MaPhieuKham, MaVacXin = 1, KetQuaChiDinh = "DuocTiem", ThoiGian = DateTime.UtcNow }; db.Add(indication); await db.SaveChangesAsync();
            var shot = new MuiTiem { MaChiDinh = indication.MaChiDinh, MaNguoiDuocTiem = recipient.MaNguoiDuocTiem, MaNhanVien = employee.MaNhanVien, SoLo = "[DEMO] LEGACY", ThoiDiemTiem = DateTime.UtcNow, ViTriTiem = "[DEMO]", TrangThai = "DaTiem" }; db.Add(shot); await db.SaveChangesAsync();
            db.AddRange(new BienDongKho { MaMuiTiem = shot.MaMuiTiem, MaNhanVien = employee.MaNhanVien, SoLo = shot.SoLo, LoaiBienDong = "XuatDoTiem", SoLuong = 1, ThoiDiem = shot.ThoiDiemTiem },
                new TheoDoiSauTiem { MaMuiTiem = shot.MaMuiTiem, MaNhanVien = employee.MaNhanVien, TinhTrang = "[DEMO]", MucDoPhanUng = "[DEMO]", HuongXuTri = "[DEMO]", ThoiDiem = DateTime.UtcNow }); await db.SaveChangesAsync();
            snapshots["VacXin"] = JsonSerializer.Serialize(await db.Vaccines.AsNoTracking().SingleAsync());
            foreach (var table in tables) snapshots[table] = await Snapshot(db, table);
        }), async f => {
            await f.Db.Database.MigrateAsync(); await f.Db.Database.MigrateAsync();
            foreach (var table in tables) Assert.Equal(snapshots[table], await Snapshot(f.Db, table));
            Assert.Equal(snapshots["VacXin"], JsonSerializer.Serialize(await f.Db.Vaccines.AsNoTracking().SingleAsync()));
            var vaccine = await f.Db.Vaccines.SingleAsync(); Assert.Equal(1, vaccine.MaVacXin); Assert.Equal("VX-LEGACY", vaccine.Code);
            Assert.Equal("Legacy reaction", vaccine.PhanUngSauTiem); Assert.Equal("Legacy dose", vaccine.LieuLuong); Assert.Equal(100, vaccine.Gia); Assert.Equal(0.5m, vaccine.TheTich);
            Assert.Empty(await f.Db.Set<CongNgheVacXin>().ToArrayAsync()); Assert.Empty(await f.Db.Set<VacXin_CongNghe>().ToArrayAsync());
            Assert.False(f.Db.Database.HasPendingModelChanges()); Assert.Empty(await f.Db.Database.GetPendingMigrationsAsync());
            Assert.Equal(0, await f.Db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sys.tables WHERE name IN ('LoaiTiemChung','LoaiVacXin')").SingleAsync());
            Assert.Equal(0, await f.Db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sys.columns WHERE object_id=OBJECT_ID('VacXin') AND name IN ('MaLoaiTiemChung','MaLoaiVacXin')").SingleAsync());
            var error = await Assert.ThrowsAsync<SqlException>(() => f.Db.GetService<IMigrator>().MigrateAsync(Previous)); Assert.Equal(51000, error.Number);
            Assert.Single(await f.Db.Set<MuiTiem>().ToArrayAsync());
        });
    }

    [Fact, Trait("Category", "SqlIntegration")]
    public async Task UnexpectedLegacyForeignKeyStopsMigrationBeforeRetirement()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(async db => {
            await db.GetService<IMigrator>().MigrateAsync(Previous);
            await db.Database.ExecuteSqlRawAsync("CREATE TABLE UnexpectedConsumer (Id int PRIMARY KEY, OldType int REFERENCES LoaiVacXin(MaLoaiVacXin));");
            var error = await Assert.ThrowsAsync<SqlException>(() => db.Database.MigrateAsync()); Assert.Equal(51000, error.Number);
            Assert.Equal(2, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sys.tables WHERE name IN ('LoaiTiemChung','LoaiVacXin')").SingleAsync());
            await db.Database.ExecuteSqlRawAsync("DROP TABLE UnexpectedConsumer;");
        }), _ => Task.CompletedTask);
    }
}
