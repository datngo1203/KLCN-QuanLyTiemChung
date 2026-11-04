using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using TiemChung.Domain.Entities;
using Xunit;

namespace TiemChung.Tests;

public sealed class RF02B2VaccineEditorSqlTests
{
    [Fact, Trait("Category", "SqlIntegration")]
    public async Task WindowsVaccineEditorCreatesUpdatesReloadsAndKeepsExistingInactiveLinks()
    {
        await SqlFixtureLifecycle.RunAsync(() => CatalogSqlTests.Fixture.Create(), async f => {
            var disease = new Benh { Code = "BENH-CURRENT", TenBenh = "[DEMO] Disease current" };
            var legacy = new Benh { Code = "BENH-LINKED", TenBenh = "[DEMO] Disease linked" };
            var technology = new CongNgheVacXin { Code = "CNVX-LINKED", TenCongNghe = "[DEMO] Technology linked" };
            var unlinked = new CongNgheVacXin { Code = "CNVX-BLOCKED", TenCongNghe = "[DEMO] Technology blocked", TrangThai = "Inactive" };
            f.Db.AddRange(disease, legacy, technology, unlinked);
            await f.Db.SaveChangesAsync();
            using var http = f.Factory.CreateClient();
            var identity = new IdentityApiService(http);
            Assert.True((await identity.LoginAsync("s2.seed", f.Password)).Succeeded);
            var vm = new CatalogViewModel(new(identity), identity.Permissions) { Confirm = _ => true };
            await vm.InitializeAsync();
            vm.Name = "[DEMO] Windows B2 vaccine";
            vm.Description = "Synthetic description";
            vm.Manufacturer = " Synthetic maker ";
            vm.Origin = "Synthetic origin";
            vm.Dosage = "Synthetic dose";
            vm.Volume = "0,75";
            vm.TargetAudience = "Synthetic audience";
            vm.AdministrationRoute = vm.VaccineRoutes.Single(x => x.Label == "Khác").Value;
            vm.AdministrationRouteNote = "Synthetic route";
            vm.Price = "235000";
            vm.SourceName = "Synthetic source";
            vm.SourceUrl = "https://example.invalid/vaccine";
            vm.InformationDate = new(2026, 11, 4);
            Assert.True(vm.DiseaseSelection.Add(disease.MaBenh));
            Assert.True(vm.DiseaseSelection.Add(legacy.MaBenh));
            Assert.True(vm.TechnologySelection.Add(technology.MaCongNghe));
            Assert.False(vm.TechnologySelection.Add(unlinked.MaCongNghe));

            var regimen = Assert.IsType<CatalogRegimenDraft>(vm.AddRegimen());
            regimen.Name = "Synthetic regimen"; regimen.TargetAudience = "Regimen audience";
            regimen.Summary = "Regimen summary"; regimen.SourceName = "Regimen source";
            regimen.SourceUrl = "https://example.invalid/regimen";
            var dose = Assert.IsType<CatalogDoseDraft>(vm.AddDose(regimen));
            dose.TimingLabel = "Synthetic timing"; dose.MinimumIntervalDays = "3";
            dose.RecommendedIntervalDays = "7"; dose.Note = "Synthetic note";
            await Execute(vm, vm.SaveCommand);
            Assert.StartsWith("Đã tạo", vm.Feedback);
            var id = vm.Selected!.Id;
            var path = $"/api/v1/catalog/vaccines/{id}";
            var detail = (await http.GetFromJsonAsync<VaccineDto>(path))!;
            Assert.Equal(vm.Code, detail.Code);
            Assert.Equal(vm.Name, detail.Name);
            Assert.Equal(vm.Description, detail.Description);
            Assert.Equal("Synthetic maker", detail.Manufacturer);
            Assert.Equal(vm.Origin, detail.Origin);
            Assert.Equal(vm.Dosage, detail.Dosage);
            Assert.Equal(0.75m, detail.Volume);
            Assert.Equal(vm.TargetAudience, detail.TargetAudience);
            Assert.Equal("Other", detail.AdministrationRoute);
            Assert.Equal(vm.AdministrationRouteNote, detail.AdministrationRouteNote);
            Assert.Equal(235000m, detail.Price);
            Assert.Equal("Active", detail.Status);
            Assert.Equal(vm.SourceName, detail.SourceName);
            Assert.Equal(vm.SourceUrl, detail.SourceUrl);
            Assert.Equal(new DateTimeOffset(2026, 11, 4, 0, 0, 0, TimeSpan.FromHours(7)), detail.InformationUpdatedAt);
            Assert.Equal([disease.MaBenh, legacy.MaBenh], detail.Diseases.Select(x => x.Id).Order());
            Assert.Equal(technology.MaCongNghe, Assert.Single(detail.Technologies).Id);
            var persistedRegimen = Assert.Single(detail.Regimens);
            Assert.Equal(regimen.Name, persistedRegimen.Name);
            Assert.Equal(regimen.TargetAudience, persistedRegimen.TargetAudience);
            Assert.Equal(regimen.Summary, persistedRegimen.Summary);
            Assert.Equal(regimen.SourceName, persistedRegimen.SourceName);
            Assert.Equal(regimen.SourceUrl, persistedRegimen.SourceUrl);
            var persistedDose = Assert.Single(persistedRegimen.Doses);
            Assert.Equal(new VaccineRegimenDoseDto(persistedDose.Id, 1, dose.TimingLabel, 3, 7, dose.Note), persistedDose);

            legacy.TrangThai = technology.TrangThai = "Inactive";
            await f.Db.SaveChangesAsync();
            await Execute(vm, vm.LoadCommand);
            Assert.Contains(vm.DiseaseSelection.Selected, x => x.Id == legacy.MaBenh && !x.Active);
            Assert.False(Assert.Single(vm.TechnologySelection.Selected).Active);
            Assert.Equal(persistedDose.Note, vm.Regimens[0].Doses[0].Note);
            await f.Login();
            using var package = await f.Client.PostAsJsonAsync("/api/v1/catalog/packages",
                new PackageRequest("[DEMO] Package", 235000, [new(id, 1)], "GOI-B2", TargetAudience: "Synthetic"));
            Assert.Equal(HttpStatusCode.Created, package.StatusCode);
            vm.Status = "Inactive"; vm.Price = "340000";
            await Execute(vm, vm.SaveCommand);
            Assert.Contains("thuộc gói Active", vm.Feedback);
            Assert.True(vm.IsDirty);
            Assert.Equal(235000m, (await f.Db.Vaccines.AsNoTracking().SingleAsync()).Gia);

            vm.Status = "Active";
            vm.AdministrationRoute = vm.VaccineRoutes.Single(x => x.Label == "Đường uống").Value;
            vm.Regimens[0].Doses[0].Note = "Updated dose";
            await Execute(vm, vm.SaveCommand);
            Assert.StartsWith("Đã cập nhật", vm.Feedback);
            Assert.False(vm.IsDirty);
            var sql = await f.Db.Vaccines.AsNoTracking().SingleAsync(x => x.MaVacXin == id);
            Assert.Equal(340000m, sql.Gia);
            Assert.Equal("Active", sql.TrangThai);
            Assert.Equal("Oral", sql.DuongDung);
            Assert.Null(sql.DuongDungKhac);
            Assert.Equal(vm.SourceUrl, sql.DuongDanNguon);
            Assert.Equal(detail.InformationUpdatedAt, sql.NgayCapNhatThongTin);
            Assert.Equal(2, await f.Db.Set<VacXin_Benh>().CountAsync(x => x.MaVacXin == id));
            Assert.Equal(technology.MaCongNghe, (await f.Db.Set<VacXin_CongNghe>().AsNoTracking().SingleAsync()).MaCongNghe);
            Assert.Equal("Updated dose", (await f.Db.Set<VaccineRegimenDose>().AsNoTracking().SingleAsync()).Note);

            vm.DiseaseSelection.Remove(legacy.MaBenh);
            vm.TechnologySelection.Remove(technology.MaCongNghe);
            vm.RemoveRegimen(vm.Regimens[0]);
            await Execute(vm, vm.SaveCommand);
            Assert.StartsWith("Đã cập nhật", vm.Feedback);
            Assert.Equal(disease.MaBenh, (await f.Db.Set<VacXin_Benh>().AsNoTracking().SingleAsync()).MaBenh);
            Assert.Empty(await f.Db.Set<VacXin_CongNghe>().AsNoTracking().ToArrayAsync());
            Assert.Empty(await f.Db.Set<VaccineRegimenDose>().AsNoTracking().ToArrayAsync());
            Assert.Empty(await f.Db.Set<LichHenTiem>().AsNoTracking().ToArrayAsync());
            Assert.Empty(await f.Db.Set<LoVacXin>().AsNoTracking().ToArrayAsync());
        });
    }

    private static async Task Execute(CatalogViewModel vm, RelayCommand command)
    {
        Assert.True(command.CanExecute(null));
        command.Execute(null);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (vm.IsBusy) await Task.Delay(10, timeout.Token);
    }
}
