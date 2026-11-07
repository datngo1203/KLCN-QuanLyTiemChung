using System.Net;
using System.Net.Http.Json;
using TiemChung.Contracts;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed partial class CatalogClientTests
{
    [Fact]
    public async Task VaccineEditorReadsFullDetailAndKeepsUnchangedGraphsAndSourceTimestamp()
    {
        var api = new VaccineHandler();
        var vm = await OpenVaccine(api);
        Assert.Equal("VX-TEST", vm.Code);
        Assert.Equal("Active", vm.Status);
        Assert.Equal("Mô tả", vm.Description);
        Assert.Equal("Hãng thử", vm.Manufacturer);
        Assert.Equal("Xuất xứ thử", vm.Origin);
        Assert.Equal("Liều thử", vm.Dosage);
        Assert.Equal("0,75", vm.Volume);
        Assert.Equal("Tham khảo", vm.TargetAudience);
        Assert.Equal("Other", vm.AdministrationRoute);
        Assert.Equal("Ghi chú đường dùng", vm.AdministrationRouteNote);
        Assert.Equal("123456", vm.Price);
        Assert.Equal("123.456 ₫", vm.PriceDisplay);
        Assert.Equal("Nguồn thử", vm.SourceName);
        Assert.Equal("https://example.invalid/product", vm.SourceUrl);
        Assert.Equal(new DateTime(2026, 11, 4), vm.InformationDate);
        var regimen = Assert.Single(vm.Regimens);
        Assert.Equal("Phác đồ thử", regimen.Name);
        Assert.Equal("Đối tượng thử", regimen.TargetAudience);
        Assert.Equal("Tóm tắt thử", regimen.Summary);
        Assert.Equal("Nguồn phác đồ", regimen.SourceName);
        Assert.Equal("https://example.invalid/regimen", regimen.SourceUrl);
        var dose = Assert.Single(regimen.Doses);
        Assert.Equal(2, dose.Order);
        Assert.Equal("Thời điểm thử", dose.TimingLabel);
        Assert.Equal("3", dose.MinimumIntervalDays);
        Assert.Equal("8", dose.RecommendedIntervalDays);
        Assert.Equal("Ghi chú thử", dose.Note);
        Assert.False(vm.IsDirty);

        vm.Name = "Đổi tên";
        await Execute(vm, vm.SaveCommand);
        var request = Assert.IsType<VaccineRequest>(api.LastWrite);
        Assert.Null(request.DiseaseIds);
        Assert.Null(request.TechnologyIds);
        Assert.Null(request.Regimens);
        Assert.Equal(VaccineHandler.Original.InformationUpdatedAt, request.InformationUpdatedAt);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public async Task VaccineTaxonomiesSearchCodeAndNameAndKeepInactiveLinksUntilExplicitRemoval()
    {
        var api = new VaccineHandler();
        var vm = await OpenVaccine(api);
        foreach (var (picker, active, inactive, newId, forbidden) in new[] {
            (vm.DiseaseSelection, 1, 2, 3, 4), (vm.TechnologySelection, 11, 12, 13, 14) }) {
            Assert.Contains(picker.Selected, x => x.Id == inactive && !x.Active && x.Label.Contains("Ngừng sử dụng"));
            Assert.Contains(picker.Selected, x => x.Id == active);
            Assert.False(picker.Add(forbidden));
            picker.Query = "  bo sung ";
            Assert.Equal(newId, Assert.Single(picker.Results).Id);
            picker.Query = picker.Results[0].Code.ToLowerInvariant();
            Assert.Equal(newId, Assert.Single(picker.Results).Id);
            picker.Query = "no-match";
            Assert.Empty(picker.Results);
            Assert.Equal(2, picker.Selected.Length);
            Assert.True(picker.Add(newId));
            Assert.False(picker.Add(newId));
            Assert.Empty(picker.Query);
        }
        await Execute(vm, vm.SaveCommand);
        Assert.Equal([1, 2, 3], api.LastWrite!.DiseaseIds!);
        Assert.Equal([11, 12, 13], api.LastWrite.TechnologyIds!);
        vm.DiseaseSelection.Remove(2);
        vm.TechnologySelection.Remove(12);
        Assert.False(vm.DiseaseSelection.Add(2));
        Assert.False(vm.TechnologySelection.Add(12));
        await Execute(vm, vm.SaveCommand);
        Assert.Equal([1, 3], api.LastWrite!.DiseaseIds!);
        Assert.Equal([11, 13], api.LastWrite.TechnologyIds!);
    }

    [Fact]
    public async Task VaccineDraftCoversNestedEditsAndProtectsRowResourceAndReload()
    {
        var api = new VaccineHandler();
        var vm = await OpenVaccine(api);
        var selected = vm.Selected;
        var dose = vm.Regimens[0].Doses[0];
        vm.DiseaseSelection.Query = "từ khóa";
        Assert.False(vm.IsDirty);
        dose.Note = "Giữ bản nháp";
        Assert.True(vm.IsDirty);
        vm.Confirm = _ => false;
        vm.Selected = null;
        await vm.SelectResourceAsync("Phòng bệnh");
        await Execute(vm, vm.LoadCommand);
        Assert.Same(selected, vm.Selected);
        Assert.Equal("Vắc xin", vm.Tab);
        Assert.Equal("Giữ bản nháp", dose.Note);
        vm.Confirm = _ => true;
        await Execute(vm, vm.RevertCommand);
        Assert.Equal("Ghi chú thử", vm.Regimens[0].Doses[0].Note);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public async Task VaccineAutocompleteAcceptsExistingOrNewValuesAndMapsRouteLabelsToCanonicalPayload()
    {
        var api = new VaccineHandler();
        var vm = await OpenVaccine(api);
        Assert.Equal(["Hãng thử"], CatalogViewModel.FindSuggestions(vm.ManufacturerSuggestions, "  hang  "));
        Assert.Equal(["Xuất xứ thử"], CatalogViewModel.FindSuggestions(vm.OriginSuggestions, "xuat"));
        Assert.Empty(CatalogViewModel.FindSuggestions(vm.OriginSuggestions, "Giá trị mới"));
        vm.Manufacturer = CatalogViewModel.FindSuggestions(vm.ManufacturerSuggestions, "hang")[0];
        vm.Origin = "  Giá trị mới ";
        vm.AdministrationRoute = vm.VaccineRoutes.Single(x => x.Label == "Tiêm bắp").Value;
        Assert.False(vm.ShowRouteNote);
        await Execute(vm, vm.SaveCommand);
        Assert.Equal("Intramuscular", api.LastWrite!.AdministrationRoute);
        Assert.Null(api.LastWrite.AdministrationRouteNote);
        Assert.Equal("Giá trị mới", vm.Origin);
        Assert.Empty(vm.AdministrationRouteNote);
    }

    [Fact]
    public async Task VaccineRegimenDoseValidationRemovalConfirmationAndDeterministicOrder()
    {
        var api = new VaccineHandler();
        var vm = await OpenVaccine(api);
        var regimen = Assert.IsType<CatalogRegimenDraft>(vm.AddRegimen());
        regimen.Name = "Bản nháp";
        regimen.TargetAudience = "Tham khảo";
        var first = Assert.IsType<CatalogDoseDraft>(vm.AddDose(regimen));
        var second = Assert.IsType<CatalogDoseDraft>(vm.AddDose(regimen));
        first.TimingLabel = "Mốc A";
        await Execute(vm, vm.SaveCommand);
        Assert.Equal(nameof(vm.Regimens), vm.ValidationField);
        Assert.Contains("mũi 2", vm.Feedback);
        Assert.Null(api.LastWrite);
        second.TimingLabel = "Mốc B";
        second.MinimumIntervalDays = "30";
        second.RecommendedIntervalDays = "15";
        await Execute(vm, vm.SaveCommand);
        Assert.Contains("khuyến nghị", vm.Feedback);
        vm.Confirm = _ => false;
        vm.RemoveDose(regimen, first);
        vm.RemoveRegimen(regimen);
        Assert.Equal(2, regimen.Doses.Count);
        Assert.Contains(regimen, vm.Regimens);
        vm.Confirm = _ => true;
        vm.RemoveDose(regimen, first);
        vm.RemoveRegimen(vm.Regimens[0]);
        second.RecommendedIntervalDays = "40";
        await Execute(vm, vm.SaveCommand);
        var persisted = Assert.Single(api.LastWrite!.Regimens!);
        Assert.Equal(new VaccineRegimenDoseRequest(1, "Mốc B", 30, 40, ""), Assert.Single(persisted.Doses));
        Assert.Equal("Mốc B", Assert.Single(Assert.Single(vm.Regimens).Doses).TimingLabel);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public async Task StructuredRegimenRoundTripsAndRemovingAnchorRequiresAnExplicitRepair()
    {
        var api = new VaccineHandler();
        var vm = await OpenVaccine(api);
        var regimen = vm.Regimens[0];
        var later = Assert.IsType<CatalogDoseDraft>(vm.AddDose(regimen));
        later.TimingLabel = "Mốc tham khảo";
        later.AnchorDoseOrder = Assert.Single(later.AnchorOptions, option => option.Order.HasValue).Order;
        later.IntervalUnit = later.IntervalUnits.Single(unit => unit.Label == "Tháng").Value;
        foreach (var invalid in new[] { "", "0", "-3", "1,5", "2147483648" }) {
            later.IntervalValue = invalid;
            await Execute(vm, vm.SaveCommand);
            Assert.Contains("số nguyên dương", vm.Feedback);
            Assert.Null(api.LastWrite);
        }
        later.IntervalValue = "6";
        later.MinimumIntervalDays = "0";
        await Execute(vm, vm.SaveCommand);
        Assert.Contains("khoảng cách ngày cũ", vm.Feedback);
        later.MinimumIntervalDays = "";
        api.Timeout = true;
        await Execute(vm, vm.SaveCommand);
        Assert.True(vm.IsDirty);
        Assert.Equal((1, "6", "Months"), (later.AnchorDoseOrder, later.IntervalValue, later.IntervalUnit));
        api.Timeout = false;
        await Execute(vm, vm.SaveCommand);
        Assert.Equal((1, 6, "Months"), (api.LastWrite!.Regimens![0].Doses[1].AnchorDoseOrder,
            api.LastWrite.Regimens[0].Doses[1].IntervalValue, api.LastWrite.Regimens[0].Doses[1].IntervalUnit));
        Assert.False(vm.IsDirty);
        var readOnly = await OpenVaccine(api, ["QuanLyDanhMucChuyenMon"]);
        var saved = readOnly.Regimens[0].Doses[1];
        Assert.Equal((1, "6", "Months"), (saved.AnchorDoseOrder, saved.IntervalValue, saved.IntervalUnit));
        Assert.False(readOnly.SaveCommand.CanExecute(null));

        regimen = vm.Regimens[0];
        later = regimen.Doses[1];
        vm.RemoveDose(regimen, regimen.Doses[0]);
        Assert.Null(later.AnchorDoseOrder);
        Assert.Equal("6", later.IntervalValue);
        Assert.True(vm.IsDirty);
        await Execute(vm, vm.SaveCommand);
        Assert.Contains("mũi neo", vm.Feedback);
    }

    [Fact]
    public async Task VaccineWriteRightsReferencedConflictAndTimeoutKeepDraftForRetry()
    {
        var api = new VaccineHandler();
        var readOnly = await OpenVaccine(api, ["QuanLyDanhMucChuyenMon"]);
        Assert.True(readOnly.IsReadOnly);
        Assert.False(readOnly.SaveCommand.CanExecute(null));
        Assert.False(readOnly.DeleteCommand.CanExecute(null));
        Assert.Null(readOnly.AddRegimen());
        var vm = await OpenVaccine(api);
        vm.Status = "Inactive";
        vm.Price = "250000";
        vm.Regimens[0].Doses[0].Note = "Giữ qua lỗi";
        api.Conflict = true;
        await Execute(vm, vm.SaveCommand);
        Assert.Contains("vắc xin thuộc gói", vm.Feedback, StringComparison.OrdinalIgnoreCase);
        api.Conflict = false;
        api.Timeout = true;
        await Execute(vm, vm.SaveCommand);
        Assert.Contains("quá thời gian", vm.Feedback);
        Assert.Equal("Inactive", vm.Status);
        Assert.Equal("250000", vm.Price);
        Assert.Equal("Giữ qua lỗi", vm.Regimens[0].Doses[0].Note);
        Assert.True(vm.IsDirty);
        api.Timeout = false;
        await Execute(vm, vm.SaveCommand);
        Assert.Equal("Inactive", vm.Selected!.Status);
        Assert.Equal(250000m, vm.Selected.Price);
        Assert.False(vm.IsDirty);
    }

    private static async Task<CatalogViewModel> OpenVaccine(VaccineHandler handler, string[]? permissions = null)
    {
        var identity = new IdentityApiService(new HttpClient(handler) { BaseAddress = new("http://test/") });
        var vm = new CatalogViewModel(new(identity), permissions ?? Permissions) { Confirm = _ => true };
        await vm.InitializeAsync();
        vm.Selected = Assert.Single(vm.Rows);
        return vm;
    }

    private sealed class VaccineHandler : HttpMessageHandler
    {
        private static readonly CatalogType[] Diseases = Options(0, "BENH");
        private static readonly CatalogType[] Technologies = Options(10, "CNVX");
        public static readonly CatalogVaccine Original = new(81, "Vắc xin thử", "Hãng thử", "Xuất xứ thử", 123456, "VX-TEST",
            TargetAudience: "Tham khảo", AdministrationRoute: "Other", AdministrationRouteNote: "Ghi chú đường dùng",
            Diseases: References(Diseases, [1, 2]), Technologies: References(Technologies, [11, 12])) {
            Description = "Mô tả", Dosage = "Liều thử", Volume = 0.75m, SourceName = "Nguồn thử", SourceUrl = "https://example.invalid/product",
            InformationUpdatedAt = new(2026, 11, 3, 20, 15, 0, TimeSpan.Zero),
            Regimens = [new(91, "Phác đồ thử", "Đối tượng thử", "Tóm tắt thử", "Nguồn phác đồ", "https://example.invalid/regimen",
                [new(92, 2, "Thời điểm thử", 3, 8, "Ghi chú thử")])]
        };
        private CatalogVaccine _saved = Original;
        public VaccineRequest? LastWrite { get; private set; }
        public bool Conflict, Timeout;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get) {
                return request.RequestUri!.Segments.Last() switch {
                    "diseases" => Ok(new CatalogList<CatalogType>("database", Diseases)),
                    "vaccine-technologies" => Ok(new CatalogList<CatalogType>("database", Technologies)),
                    "suggestions" => Ok(new CatalogSuggestions([" Hãng thử ", "Hãng Thử"], ["Xuất xứ thử"], ["Intramuscular", "Other"])),
                    _ => Ok(new CatalogList<CatalogVaccine>("database", [_saved]))
                };
            }
            if (Conflict) return new(HttpStatusCode.Conflict) { Content = JsonContent.Create(new { code = "catalogReferenced" }) };
            if (Timeout) throw new TaskCanceledException();
            var value = (await request.Content!.ReadFromJsonAsync<VaccineRequest>(cancellationToken))!;
            LastWrite = value;
            _saved = _saved with {
                Name = value.Name, Origin = value.Origin.Trim(), Manufacturer = value.Manufacturer.Trim(), Price = value.Price, Status = value.Status!,
                AdministrationRoute = value.AdministrationRoute, AdministrationRouteNote = value.AdministrationRouteNote,
                Diseases = value.DiseaseIds is null ? _saved.Diseases : References(Diseases, value.DiseaseIds),
                Technologies = value.TechnologyIds is null ? _saved.Technologies : References(Technologies, value.TechnologyIds),
                Regimens = value.Regimens?.Select(r => new VaccineRegimenDto(91, r.Name, r.TargetAudience, r.Summary, r.SourceName, r.SourceUrl,
                    r.Doses.Select(d => new VaccineRegimenDoseDto(92, d.DoseOrder, d.TimingLabel, d.MinimumIntervalDays, d.RecommendedIntervalDays, d.Note,
                        d.AnchorDoseOrder, d.IntervalValue, d.IntervalUnit)).ToArray())).ToArray() ?? _saved.Regimens
            };
            return Ok(_saved);
        }

        private static HttpResponseMessage Ok(object value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
        private static CatalogType[] Options(int offset, string prefix) => [
            new(offset + 1, "Đang dùng", "", prefix + "-CURRENT"),
            new(offset + 2, "Đã liên kết", "", prefix + "-LINKED", "Inactive"),
            new(offset + 3, "Bổ sung", "", prefix + "-ADD"),
            new(offset + 4, "Chưa liên kết", "", prefix + "-UNLINKED", "Inactive")
        ];
        private static CatalogReference[] References(CatalogType[] options, int[] ids) =>
            options.Where(x => ids.Contains(x.Id)).Select(x => new CatalogReference(x.Id, x.Code, x.Name)).ToArray();
    }
}
