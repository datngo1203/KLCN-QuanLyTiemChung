using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed partial class CatalogClientTests
{
    private static readonly string[] Permissions = ["QuanLyDanhMucChuyenMon", "QuanLyDanhMucGia"];
    private static CatalogViewModel Create(Handler handler, string[]? permissions = null) => new(new(new IdentityApiService(new HttpClient(handler) { BaseAddress = new("http://test/") })), permissions ?? Permissions) { Confirm = _ => true };
    private static async Task Execute(CatalogViewModel vm, RelayCommand command, object? parameter = null)
    {
        Assert.True(command.CanExecute(parameter)); command.Execute(parameter);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (vm.IsBusy) await Task.Delay(5, timeout.Token);
    }
    [Theory]
    [InlineData("Công nghệ vắc xin", "vaccine-technologies", "CNVX")]
    [InlineData("Phòng bệnh", "diseases", "BENH")]
    public async Task LookupCreateStatusUpdateBusinessCodeAndSelection(string tab, string resource, string prefix)
    {
        var h = new Handler(); var vm = Create(h);
        await Execute(vm, vm.SelectTabCommand, tab);
        Assert.DoesNotContain("API", vm.DataSourceLabel); Assert.DoesNotContain("SQL", vm.DataSourceLabel); Assert.DoesNotContain("DEMO", vm.DataSourceLabel);
        Assert.Equal("Đang sử dụng", CatalogStatus.Describe("Active")); Assert.Equal("Ngừng sử dụng", CatalogStatus.Describe("Inactive"));
        Assert.Equal(4, vm.Tabs.Count); Assert.All(h.Paths, x => Assert.Contains("/" + resource, x));
        vm.Name = "Đối tượng thử"; Assert.Equal(prefix + "-DOI-TUONG-THU", vm.Code);
        vm.Code = prefix + "-CUSTOM"; vm.Name = "Tên khác"; Assert.Equal(prefix + "-CUSTOM", vm.Code);
        vm.Description = "Mô tả"; vm.Status = "Inactive";
        await Execute(vm, vm.SaveCommand);
        Assert.StartsWith("Đã tạo", vm.Feedback); Assert.NotNull(vm.Selected); Assert.False(vm.IsDirty);
        Assert.Equal("Lưu thay đổi", vm.SaveLabel); Assert.True(vm.CodeReadOnly); vm.Code = prefix + "-CHANGED";
        Assert.Equal(prefix + "-CUSTOM", vm.Code); Assert.Equal("Inactive", h.LastBody.GetProperty("status").GetString());
        vm.Name = "Đã sửa"; vm.Status = "Active"; await Execute(vm, vm.SaveCommand);
        Assert.StartsWith("Đã cập nhật", vm.Feedback); Assert.Equal("PUT", h.Writes.Last()); Assert.Equal("Active", vm.Selected!.Status);
    }
    [Fact]
    public async Task SearchAndFilterAreEncodedAndEmptyStatesDistinguishNoData()
    {
        var h = new Handler(); var vm = Create(h); await Execute(vm, vm.SelectTabCommand, "Phòng bệnh");
        Assert.True(vm.ShowEmpty); Assert.Contains("Chưa có dữ liệu", vm.EmptyMessage);
        vm.Search = "BENH A&+?"; vm.StatusFilter = vm.StatusFilters[2]; await Execute(vm, vm.LoadCommand);
        Assert.Contains("search=BENH%20A%26%2B%3F&status=Inactive", h.Paths.Last());
        Assert.Contains("Không có kết quả", vm.EmptyMessage);
    }
    [Fact]
    public async Task DirtyDiscardCanCancelSwitchRowNewAndRefreshThenRevert()
    {
        var h = new Handler(); var vm = Create(h); await Execute(vm, vm.SelectTabCommand, "Phòng bệnh");
        vm.Name = "Bản nháp"; var code = vm.Code; vm.Confirm = _ => false;
        await Execute(vm, vm.SelectTabCommand, "Công nghệ vắc xin"); Assert.Equal("Phòng bệnh", vm.Tab);
        vm.Selected = new(9, "Khác", Code: "BENH-OTHER"); Assert.Null(vm.Selected);
        vm.NewCommand.Execute(null); vm.LoadCommand.Execute(null);
        Assert.Equal("Bản nháp", vm.Name); Assert.Equal(code, vm.Code);
        vm.Confirm = _ => true; vm.RevertCommand.Execute(null); Assert.False(vm.IsDirty); Assert.Empty(vm.Name);
        vm.Name = "Mới"; await Execute(vm, vm.SelectTabCommand, "Công nghệ vắc xin"); Assert.Empty(vm.Code); Assert.Empty(vm.Search); Assert.Empty(vm.Rows);
        vm.Name = "Tên"; Assert.Equal("CNVX-TEN", vm.Code);
    }
    [Fact]
    public async Task ReloadKeepsDataSelectionAndDraftWhileBusyAndAfterNetworkFailure()
    {
        var h = new Handler(); var vm = Create(h); await Execute(vm, vm.SelectTabCommand, "Phòng bệnh");
        vm.Name = "Lưu"; await Execute(vm, vm.SaveCommand); var row = vm.Selected;
        vm.Description = "Đang nhập"; h.Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.LoadCommand.Execute(null); Assert.True(vm.IsBusy); Assert.False(vm.SaveCommand.CanExecute(null));
        Assert.Single(vm.Rows); Assert.Same(row, vm.Selected); Assert.Equal("Đang nhập", vm.Description);
        Assert.Empty(vm.Feedback);
        h.FailRead = true; h.Gate.SetResult();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)); while (vm.IsBusy) await Task.Delay(5, timeout.Token);
        Assert.True(vm.LoadFailed); Assert.Single(vm.Rows); Assert.Same(row, vm.Selected); Assert.Equal("Đang nhập", vm.Description);
        Assert.Contains("Không kết nối", vm.Feedback); Assert.DoesNotContain("PRIVATE", vm.Feedback);
        h.FailRead = false; h.Gate = null; await Execute(vm, vm.LoadCommand); Assert.False(vm.LoadFailed); Assert.Equal("", vm.Description);
    }
    [Theory]
    [InlineData("catalogDuplicate", "đã tồn tại")]
    [InlineData("validation", "Kiểm tra")]
    [InlineData("permissionDenied", "quyền")]
    [InlineData("resourceNotFound", "không còn")]
    public async Task BackendErrorsKeepDraft(string error, string message)
    {
        var h = new Handler(); var vm = Create(h); await Execute(vm, vm.SelectTabCommand, "Phòng bệnh");
        vm.Name = "Bản nháp"; vm.Description = "Giữ input"; var code = vm.Code; h.Error = error;
        await Execute(vm, vm.SaveCommand); Assert.Contains(message, vm.Feedback); Assert.Equal("Giữ input", vm.Description); Assert.Equal(code, vm.Code); Assert.True(vm.IsDirty);
    }
    [Fact]
    public async Task InvalidLookupInputIsSpecificAndSendsNoWrite()
    {
        var h = new Handler(); var vm = Create(h); await Execute(vm, vm.SelectTabCommand, "Phòng bệnh");
        await Execute(vm, vm.SaveCommand); Assert.Contains("Tên", vm.Feedback);
        vm.Name = "Tên"; vm.Code = "WRONG-CODE"; await Execute(vm, vm.SaveCommand); Assert.Contains("BENH-", vm.Feedback);
        vm.Code = "BENH-VALID"; vm.Description = new('x', 501); await Execute(vm, vm.SaveCommand); Assert.Contains("500", vm.Feedback); Assert.Empty(h.Writes);
    }
    [Fact]
    public async Task DeleteConfirmsCodeNameAndReferencedFailureKeepsDraftThenSuccess()
    {
        var h = new Handler(); var vm = Create(h); await Execute(vm, vm.SelectTabCommand, "Phòng bệnh");
        vm.Name = "Tên bệnh"; await Execute(vm, vm.SaveCommand); var row = vm.Selected!;
        vm.Confirm = message => { Assert.Contains(row.Code, message); Assert.Contains(row.Name, message); return false; };
        await Execute(vm, vm.DeleteCommand); Assert.DoesNotContain("DELETE", h.Writes);
        vm.Description = "Giữ chỉnh sửa"; vm.Confirm = _ => true; h.Error = "catalogReferenced";
        await Execute(vm, vm.DeleteCommand); Assert.Contains("tham chiếu", vm.Feedback); Assert.Same(row, vm.Selected); Assert.Equal("Giữ chỉnh sửa", vm.Description);
        h.Error = null; await Execute(vm, vm.DeleteCommand); Assert.StartsWith("Đã xóa", vm.Feedback); Assert.Null(vm.Selected); Assert.Empty(vm.Rows);
    }
    [Fact]
    public async Task SuccessfulCreateWithFailedReloadKeepsCommittedIdentityAndPreventsDuplicatePost()
    {
        var h = new Handler { FailReadAfterWrite = true }; var vm = Create(h); await Execute(vm, vm.SelectTabCommand, "Phòng bệnh");
        vm.Name = "Đã tạo"; await Execute(vm, vm.SaveCommand);
        Assert.StartsWith("Đã tạo", vm.Feedback); Assert.True(vm.LoadFailed); Assert.True(vm.CodeReadOnly); Assert.NotNull(vm.Selected);
        vm.Name = "Đổi tên"; await Execute(vm, vm.SaveCommand); Assert.Equal(new[] { "POST", "PUT" }, h.Writes);
    }
    [Fact]
    public async Task ReadOnlyCanBrowseAllResourcesAndClinicalPermissionCannotWritePrices()
    {
        var h = new Handler(); var vm = Create(h, []);
        foreach (var tab in vm.Tabs) {
            await Execute(vm, vm.SelectTabCommand, tab); Assert.True(vm.IsReadOnly); vm.Name = "Read-only binding"; Assert.False(vm.IsDirty);
            Assert.True(vm.LoadCommand.CanExecute(null)); Assert.False(vm.NewCommand.CanExecute(null)); Assert.False(vm.SaveCommand.CanExecute(null)); Assert.False(vm.DeleteCommand.CanExecute(null));
        }
        var clinical = Create(new Handler(), ["QuanLyDanhMucChuyenMon"]);
        await Execute(clinical, clinical.SelectTabCommand, "Phòng bệnh"); Assert.True(clinical.CanWrite);
        await Execute(clinical, clinical.SelectTabCommand, "Vắc xin"); Assert.False(clinical.CanWrite);
    }
    [Fact]
    public async Task FourResourcesAndTechnologyMetadataRoundTripPreserveDraftOnErrorAndCommittedDataOnReloadFailure()
    {
        var h = new Handler(); var vm = Create(h);
        Assert.Equal(new[] { "Phòng bệnh", "Công nghệ vắc xin", "Vắc xin", "Gói vắc xin" }, vm.Tabs);
        await Execute(vm, vm.SelectTabCommand, "Công nghệ vắc xin");
        vm.Name = "[DEMO] Công nghệ"; vm.TechnicalCharacteristics = "Kỹ thuật synthetic"; vm.ProfessionalNotes = "Tham khảo";
        vm.SourceName = "Synthetic"; vm.SourceUrl = "https://example.invalid/technology"; vm.InformationDate = new(2026,11,4);
        h.Error = "validation"; await Execute(vm, vm.SaveCommand);
        Assert.Equal("Kỹ thuật synthetic", vm.TechnicalCharacteristics); Assert.True(vm.IsDirty);
        h.Error = null; await Execute(vm, vm.SaveCommand);
        Assert.Equal("Kỹ thuật synthetic", vm.TechnicalCharacteristics); Assert.Equal("Tham khảo", vm.ProfessionalNotes);
        Assert.Equal("Synthetic", vm.SourceName); Assert.Equal("https://example.invalid/technology", vm.SourceUrl);
        Assert.Equal(new DateTime(2026,11,4), vm.InformationDate); Assert.StartsWith("CNVX-", vm.Code); Assert.True(vm.CodeReadOnly);
        vm.TechnicalCharacteristics = "Updated"; h.FailReadAfterWrite = true; await Execute(vm, vm.SaveCommand);
        Assert.True(vm.LoadFailed); Assert.Equal("Updated", vm.TechnicalCharacteristics); Assert.Equal("Tham khảo", vm.ProfessionalNotes);
    }

    private sealed class Handler : HttpMessageHandler
    {
        public List<string> Paths { get; } = []; public List<string> Writes { get; } = [];
        public JsonElement LastBody { get; private set; }
        public string? Error; public bool FailRead, FailReadAfterWrite; public TaskCompletionSource? Gate;
        private CatalogType? _saved;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri!.PathAndQuery);
            if (request.Method == HttpMethod.Get) {
                if (Gate is not null) await Gate.Task;
                if (FailRead || (FailReadAfterWrite && Writes.Count > 0)) throw new HttpRequestException(HttpRequestError.ConnectionError, "PRIVATE endpoint");
                if (request.RequestUri.AbsolutePath.EndsWith("suggestions")) return Ok(new CatalogSuggestions([], [], ["Intramuscular", "Other"]));
                return Ok(new { source = "database", items = _saved is null ? Array.Empty<CatalogType>() : new[] { _saved } });
            }
            Writes.Add(request.Method.Method);
            if (Error is not null) return new(HttpStatusCode.Conflict) { Content = JsonContent.Create(new { code = Error }) };
            if (request.Method == HttpMethod.Delete) { _saved = null; return new(HttpStatusCode.NoContent); }
            LastBody = (await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken)).RootElement.Clone();
            _saved = JsonSerializer.Deserialize<CatalogType>(LastBody.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web))! with { Id = 71 };
            return Ok(_saved);
        }
        private static HttpResponseMessage Ok(object body) => new(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
    }
}
