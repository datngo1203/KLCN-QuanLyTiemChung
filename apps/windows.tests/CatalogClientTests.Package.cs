using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TiemChung.Contracts;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed partial class CatalogClientTests
{
    [Fact]
    public async Task PackageReadsFullDetailAndWritesMetadataWithoutChangingReferencedComposition()
    {
        var api = new PackageHandler { Referenced = true };
        var vm = await OpenPackage(api);
        Assert.Equal("GOI-TEST", vm.Code);
        Assert.Equal("Gói thử", vm.Name);
        Assert.Equal("Inactive", vm.Status);
        Assert.Equal("Mô tả", vm.Description);
        Assert.Equal("Tham khảo", vm.TargetAudience);
        Assert.Equal("300000000", vm.Price);
        Assert.Equal(2, vm.Selected!.Package!.TotalDoses);
        Assert.Equal("VX-CU", Assert.Single(vm.Selected.Package.Items).VaccineCode);
        Assert.Equal("Cũ", Assert.Single(vm.Selected.Package.Items).VaccineName);
        Assert.Equal("1 loại vắc xin · 2 mũi/liều", vm.ComponentSummary);
        Assert.Equal("Bệnh cũ", vm.ComponentDiseases);
        Assert.Contains("Ngừng sử dụng", Assert.Single(vm.PackageComponents).StatusLabel);
        Assert.True(vm.CodeReadOnly);

        vm.Price = "300 000 000";
        Assert.False(vm.IsDirty);
        vm.Name = "Tên đã đổi";
        await Execute(vm, vm.SaveCommand);
        Assert.Equal("Tên đã đổi", vm.Name);
        Assert.Equal("Inactive", vm.Status);
        Assert.Equal(300000000m, api.LastWrite!.Price);
        Assert.Equal(JsonValueKind.Number, api.PriceKind);
        Assert.Equal(new PackageItemDto(1, 2), Assert.Single(api.LastWrite.Items));
        Assert.Contains("Ngừng sử dụng", Assert.Single(vm.PackageComponents).StatusLabel);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public async Task PackageSearchCompositionAndRevertPreserveExplicitSelection()
    {
        var api = new PackageHandler();
        var vm = await OpenPackage(api);
        foreach (var term in new[] { "vx-moi", "moi", "hang moi", "benh moi" }) {
            vm.ComponentSearch = term;
            Assert.Equal(2, Assert.Single(vm.ComponentCandidates).Id);
            Assert.False(vm.IsDirty);
        }
        vm.ComponentSearch = "missing";
        Assert.Empty(vm.ComponentCandidates);
        vm.ComponentSearch = "moi";
        var candidate = vm.ComponentCandidate = Assert.Single(vm.ComponentCandidates);
        vm.AddComponentCommand.Execute(null);
        vm.ComponentCandidate = candidate;
        Assert.False(vm.AddComponentCommand.CanExecute(null));
        vm.AddComponentCommand.Execute(null);
        Assert.Equal(2, vm.PackageComponents.Count());
        Assert.True(vm.IsDirty);
        var item = vm.PackageComponents.Single(x => x.Id == 2);
        item.Quantity = "7";
        Assert.Equal("2 loại vắc xin · 9 mũi/liều", vm.ComponentSummary);
        Assert.Contains("Bệnh mới", vm.ComponentDiseases);
        vm.RemoveComponentCommand.Execute(item);
        Assert.False(vm.IsDirty);
        Assert.Single(vm.PackageComponents).Quantity = "5";
        vm.Confirm = _ => false;
        vm.RevertCommand.Execute(null);
        Assert.True(vm.IsDirty);
        vm.Confirm = _ => true;
        vm.RevertCommand.Execute(null);
        Assert.Equal("2", Assert.Single(vm.PackageComponents).Quantity);
        Assert.False(vm.IsDirty);
    }

    [Theory]
    [InlineData("")]
    [InlineData("text")]
    [InlineData("0")]
    [InlineData("101")]
    [InlineData("1.5")]
    [InlineData("1 00")]
    [InlineData("1,00")]
    public async Task InvalidComponentQuantityNamesTheVaccineAndSendsNoWrite(string quantity)
    {
        var api = new PackageHandler();
        var vm = await OpenPackage(api);
        Assert.Single(vm.PackageComponents).Quantity = quantity;
        await Execute(vm, vm.SaveCommand);
        Assert.Contains("VX-CU", vm.Feedback);
        Assert.Contains("số nguyên từ 1 đến 100", vm.Feedback);
        Assert.Null(api.LastWrite);
    }

    [Theory]
    [InlineData(".")]
    [InlineData("1,000")]
    [InlineData("1.000")]
    [InlineData("-1")]
    [InlineData("1.50")]
    [InlineData("1x")]
    [InlineData("10000000000000000")]
    public async Task InvalidPackagePriceSendsNoWrite(string price)
    {
        var api = new PackageHandler();
        var vm = await OpenPackage(api);
        vm.Price = price;
        await Execute(vm, vm.SaveCommand);
        Assert.Contains("Nhập giá VND", vm.Feedback);
        Assert.Null(api.LastWrite);
    }

    [Fact]
    public async Task PackageStatusControlsInactiveComponentsAndSupportsCreatingInactivePackage()
    {
        var api = new PackageHandler();
        var vm = await OpenPackage(api);
        vm.Status = "Active";
        await Execute(vm, vm.SaveCommand);
        Assert.Contains("VX-CU", vm.Feedback);
        Assert.Contains("Ngừng sử dụng", vm.Feedback);
        Assert.Null(api.LastWrite);

        vm.NewCommand.Execute(null);
        vm.Name = "Gói mới";
        vm.TargetAudience = "Tham khảo mới";
        vm.Description = "Mô tả mới";
        vm.Price = "1 250 000";
        vm.ComponentCandidate = vm.ComponentCandidates.Single(x => x.Id == 1);
        Assert.False(vm.AddComponentCommand.CanExecute(null));
        vm.Status = "Inactive";
        vm.AddComponentCommand.Execute(null);
        await Execute(vm, vm.SaveCommand);
        Assert.Equal(HttpMethod.Post, api.LastMethod);
        Assert.Equal("GOI-GOI-MOI", vm.Code);
        Assert.Equal("Mô tả mới", vm.Description);
        Assert.Equal("Tham khảo mới", vm.TargetAudience);
        Assert.Equal("Inactive", vm.Status);
        Assert.Equal(1250000m, api.LastWrite!.Price);
        Assert.Contains("Ngừng sử dụng", Assert.Single(vm.PackageComponents).StatusLabel);
    }

    [Theory]
    [InlineData("reference", "được sử dụng trong đăng ký")]
    [InlineData("timeout", "quá thời gian")]
    [InlineData("network", "Không kết nối")]
    public async Task FailedPackageSaveKeepsDraftAndRetryReadsPersistedComposition(string failure, string feedback)
    {
        var api = new PackageHandler { Failure = failure };
        var vm = await OpenPackage(api);
        vm.Description = "Bản nháp";
        Assert.Single(vm.PackageComponents).Quantity = "4";
        await Execute(vm, vm.SaveCommand);
        Assert.Contains(feedback, vm.Feedback);
        Assert.True(vm.IsDirty);
        Assert.Equal("Bản nháp", vm.Description);
        Assert.Equal("4", Assert.Single(vm.PackageComponents).Quantity);
        api.Failure = null;
        await Execute(vm, vm.SaveCommand);
        Assert.False(vm.IsDirty);
        Assert.Equal("Bản nháp", vm.Description);
        Assert.Equal(4, vm.Selected!.Package!.TotalDoses);
    }

    [Fact]
    public async Task PackageReadOnlyRequiresBothCatalogPermissions()
    {
        foreach (var grants in new[] { Array.Empty<string>(), [Permissions[0]], [Permissions[1]] }) {
            var api = new PackageHandler();
            var vm = await OpenPackage(api, grants);
            vm.Search = "GOI-TEST";
            await Execute(vm, vm.LoadCommand);
            Assert.Equal("GOI-TEST", vm.Selected!.Code);
            Assert.Single(vm.PackageComponents);
            vm.ComponentCandidate = Assert.Single(vm.ComponentCandidates);
            Assert.False(vm.AddComponentCommand.CanExecute(null));
            Assert.False(vm.RemoveComponentCommand.CanExecute(vm.PackageComponents.Single()));
            Assert.False(vm.SaveCommand.CanExecute(null));
            Assert.Null(api.LastWrite);
        }
    }

    private static async Task<CatalogViewModel> OpenPackage(PackageHandler api, string[]? grants = null)
    {
        var vm = new CatalogViewModel(new(new IdentityApiService(new HttpClient(api) { BaseAddress = new("http://test/") })),
            grants ?? Permissions) { Confirm = _ => true };
        await vm.SelectResourceAsync("Gói vắc xin");
        vm.Selected = Assert.Single(vm.Rows);
        return vm;
    }

    private sealed class PackageHandler : HttpMessageHandler
    {
        private static readonly CatalogVaccine[] Vaccines = [
            new(1, "Cũ", "Hãng cũ", "", 10, "VX-CU", "Inactive", Diseases: [new(1, "BENH-CU", "Bệnh cũ")]),
            new(2, "Mới", "Hãng mới", "", 20, "VX-MOI", Diseases: [new(2, "BENH-MOI", "Bệnh mới")])
        ];
        private PackageDto _package = new(3, "Gói thử", 300000000m, [new(1, 2, "Cũ", "VX-CU")]) {
            Code = "GOI-TEST", Status = "Inactive", Description = "Mô tả", TargetAudience = "Tham khảo",
            Diseases = [new(1, "BENH-CU", "Bệnh cũ")]
        };
        public PackageRequest? LastWrite;
        public HttpMethod? LastMethod;
        public JsonValueKind PriceKind;
        public string? Failure;
        public bool Referenced;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.Method == HttpMethod.Get)
                return Ok(request.RequestUri!.AbsolutePath.EndsWith("/vaccines")
                    ? new { items = Vaccines } : (object)new { items = new[] { _package } });
            LastMethod = request.Method;
            var body = await request.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken: token);
            PriceKind = body.GetProperty("price").ValueKind;
            LastWrite = body.Deserialize<PackageRequest>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            if (Failure == "timeout") throw new TaskCanceledException();
            if (Failure == "network") throw new HttpRequestException(HttpRequestError.ConnectionError, "Offline");
            if (Failure == "reference" || Referenced && !LastWrite.Items.Select(i => (i.VaccineId, i.Quantity))
                .SequenceEqual(_package.Items.Select(i => (i.VaccineId, i.Quantity))))
                return new(HttpStatusCode.Conflict) { Content = JsonContent.Create(new { code = "catalogReferenced" }) };
            var items = LastWrite.Items.Select(i => {
                var vaccine = Vaccines.Single(v => v.Id == i.VaccineId);
                return i with { VaccineCode = vaccine.Code, VaccineName = vaccine.Name };
            }).ToArray();
            _package = new(3, LastWrite.Name, LastWrite.Price, items) {
                Code = LastWrite.Code!, Status = LastWrite.Status!, Description = LastWrite.Description,
                TargetAudience = LastWrite.TargetAudience,
                Diseases = items.SelectMany(i => Vaccines.Single(v => v.Id == i.VaccineId).Diseases!)
                    .Select(d => new CatalogReferenceDto(d.Id, d.Code, d.Name)).DistinctBy(d => d.Id).ToArray()
            };
            return Ok(_package);
        }

        private static HttpResponseMessage Ok(object body) => new(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
    }
}
