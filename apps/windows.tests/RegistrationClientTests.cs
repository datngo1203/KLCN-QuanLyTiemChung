using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class RegistrationClientTests
{
    [Theory]
    [InlineData("LeTan", true)]
    [InlineData("BacSi", false)]
    [InlineData("DieuDuong", false)]
    [InlineData("ThuNgan", false)]
    [InlineData("ThuKho", false)]
    [InlineData("Admin", false)]
    [InlineData("Technical", false)]
    public async Task RegistrationUsesTitleNotProfilesCapability(string title, bool allowed)
    {
        var identity = await Login(new Handler { Title = title });
        Assert.False(identity.CanWriteProfiles);
        Assert.Equal(allowed, identity.CanUseRegistrations);
        var vm = new RegistrationViewModel(new Fake(), identity);
        Assert.Equal(allowed, vm.NewCommand.CanExecute(null));
        Assert.Equal(allowed, vm.ReloadCommand.CanExecute(null));
        if (!allowed) { await vm.NewAsync(); Assert.False(vm.ShowDraft); Assert.Empty(vm.Customers); }
    }

    [Fact]
    public async Task ApiUsesExactRequestDecimalSnapshotsSqlCatalogAndPagedProfiles()
    {
        var handler = new Handler(); var identity = await Login(handler);
        var api = new RegistrationApiService(identity);
        var catalog = await api.GetCatalogAsync();
        Assert.Equal(2, catalog.Length);
        Assert.Equal("package", catalog[1].Kind);
        var customers = await api.GetCustomersAsync("name & phone", 2);
        Assert.Contains(handler.Paths, x => x.Contains("page=2&pageSize=25") && x.Contains("name%20%26%20phone"));
        var recipients = await api.GetRecipientsAsync(10);
        Assert.Equal(101, recipients.Length);
        Assert.Contains(handler.Paths, x => x.Contains("recipients?customerId=10&page=2&pageSize=100"));
        var request = new RegistrationRequest(10, 20, [new("vaccine", 1, 2, 1234567890123456m, null), new("package", 2, 1, 350000m, [new(1, 2)])]);
        Assert.Equal(7, await api.CreateAsync(request));
        using var body = JsonDocument.Parse(handler.PostBody!);
        Assert.Equal(new[] { "customerId", "recipientId", "items" }, body.RootElement.EnumerateObject().Select(x => x.Name));
        var lines = body.RootElement.GetProperty("items");
        Assert.Equal(1234567890123456m, lines[0].GetProperty("expectedUnitPrice").GetDecimal());
        Assert.Equal(new[] { "vaccineId", "quantity" }, lines[1].GetProperty("expectedComponents")[0].EnumerateObject().Select(x => x.Name));
        await api.GetRegistrationsAsync(3);
        Assert.Contains(handler.Paths, x => x.Contains("registrations?page=3&pageSize=25"));
        var detail = await api.GetDetailAsync(7);
        Assert.Equal(600000m, detail.Subtotal);
        Assert.Equal(300000m, detail.Items[0].LineTotal);
        handler.Source = "demo";
        Assert.Equal("catalogUnavailable", (await Assert.ThrowsAsync<RegistrationApiException>(api.GetCatalogAsync)).Code);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(202)]
    [InlineData(204)]
    public async Task Only201ConfirmsCreate(int status)
    {
        var handler = new Handler { PostStatus = (HttpStatusCode)status };
        var api = new RegistrationApiService(await Login(handler));
        Assert.Equal("outcomeUnknown", (await Assert.ThrowsAsync<RegistrationApiException>(() => api.CreateAsync(Request))).Code);
        Assert.Equal(1, handler.PostCount);
    }

    [Theory]
    [InlineData(400, "validation")]
    [InlineData(403, "permissionDenied")]
    [InlineData(409, "catalogChanged")]
    [InlineData(503, "dependencyUnavailable")]
    public async Task ApiMapsErrorsWithoutLeakingServerDetails(int status, string code)
    {
        var handler = new Handler { PostStatus = (HttpStatusCode)status, Code = code };
        var api = new RegistrationApiService(await Login(handler));
        var error = await Assert.ThrowsAsync<RegistrationApiException>(() => api.CreateAsync(Request));
        Assert.Equal(code, error.Code); Assert.DoesNotContain("SQL JWT secret", error.Message);
        Assert.Equal(1, handler.PostCount);
    }

    [Fact]
    public async Task RefreshRetriesOnlyDefinitive401AndNeverRetriesTransportFailure()
    {
        var handler = new Handler { PostStatus = HttpStatusCode.Unauthorized };
        var api = new RegistrationApiService(await Login(handler));
        Assert.Equal("invalidSession", (await Assert.ThrowsAsync<RegistrationApiException>(() => api.CreateAsync(Request))).Code);
        Assert.Equal(2, handler.PostCount); Assert.Equal(1, handler.RefreshCount);
        handler.PostStatus = HttpStatusCode.Created; handler.OfflinePost = true;
        Assert.Equal("outcomeUnknown", (await Assert.ThrowsAsync<RegistrationApiException>(() => api.CreateAsync(Request))).Code);
        Assert.Equal(3, handler.PostCount);
        handler.OfflinePost = false; handler.TimeoutPost = true;
        Assert.Equal("outcomeUnknown", (await Assert.ThrowsAsync<RegistrationApiException>(() => api.CreateAsync(Request))).Code);
        Assert.Equal(4, handler.PostCount);
    }

    [Fact]
    public async Task DraftValidatesPairQuantityDuplicatesAndCreateNextResetsEverything()
    {
        var fake = new Fake(); var vm = await Draft(fake);
        Assert.True(vm.CanSubmit); Assert.Equal(2, vm.Lines.Count); Assert.Equal(650000m, vm.Subtotal);
        vm.CatalogItem = vm.Catalog[0]; vm.QuantityText = "4"; vm.AddLineCommand.Execute(null);
        Assert.Equal(2, vm.Lines.Count); Assert.Equal(4, vm.Lines[0].Quantity);
        foreach (var invalid in new[] { "0", "101", "1.2", "-1", "no" }) {
            vm.QuantityText = invalid; Assert.False(vm.AddLineCommand.CanExecute(null));
        }
        vm.Recipient = new(90, 999, "wrong", null, new(2000, 1, 1), "Nam", "Con"); Assert.False(vm.CanSubmit);
        vm.Customer = new(11, "another", null, "0900000000", "test@example.invalid");
        Assert.Null(vm.Recipient); Assert.Empty(vm.Recipients);
        await vm.NewAsync();
        Assert.Null(vm.Customer); Assert.Null(vm.Recipient); Assert.Null(vm.CatalogItem); Assert.Empty(vm.Lines);
        Assert.Equal("", vm.CustomerSearch); Assert.Equal("", vm.CatalogSearch); Assert.Equal("1", vm.QuantityText);
        Assert.False(vm.CanSubmit);
    }

    [Fact]
    public async Task SaveBlocksDoubleSendAndDetailFailureRetriesOnlyGet()
    {
        var fake = new Fake { CreateGate = new(), FailDetail = true }; var vm = await Draft(fake);
        var save = vm.SaveAsync();
        Assert.True(vm.IsBusy); Assert.False(vm.SaveCommand.CanExecute(null));
        await vm.SaveAsync(); Assert.Equal(1, fake.PostCount);
        fake.CreateGate.SetResult(7); await save;
        Assert.Contains("Đã tạo đăng ký #7", vm.Feedback); Assert.Null(vm.Detail); Assert.False(vm.IsCreating);
        Assert.False(vm.SaveCommand.CanExecute(null));
        await vm.OpenDetailAsync(7); Assert.Equal(1, fake.PostCount); Assert.Equal(2, fake.DetailCount);
        fake.FailDetail = false; await vm.OpenDetailAsync(7); Assert.NotNull(vm.Detail);
        await vm.NewAsync(); Assert.Empty(vm.Lines); Assert.Null(vm.Customer); Assert.Null(vm.Detail);
    }

    [Fact]
    public async Task ValidationAndNetworkErrorsKeepDraftWhileUnknownPostRequiresListReview()
    {
        var fake = new Fake { Error = "validation" }; var vm = await Draft(fake);
        await vm.SaveAsync(); Assert.Equal(2, vm.Lines.Count); Assert.NotNull(vm.Customer); Assert.True(vm.CanSubmit);
        fake.Error = "outcomeUnknown"; await vm.SaveAsync();
        Assert.True(vm.UnknownOutcome); Assert.False(vm.CanSubmit); Assert.False(vm.NewCommand.CanExecute(null));
        await vm.SaveAsync(); Assert.Equal(2, fake.PostCount);
        await vm.NewAsync(); Assert.Equal(2, vm.Lines.Count);
        fake.FailList = true; await vm.LoadAsync(); Assert.False(vm.NewCommand.CanExecute(null));
        fake.FailList = false; await vm.LoadPageAsync(3); Assert.False(vm.NewCommand.CanExecute(null));
        await vm.LoadAsync(); Assert.Equal(1, vm.Page); Assert.True(vm.NewCommand.CanExecute(null));
        await vm.NewAsync(); Assert.False(vm.UnknownOutcome); Assert.Empty(vm.Lines); Assert.Null(vm.Customer);
        fake.FailCustomers = true; vm.CustomerSearch = "retry"; await vm.SearchCustomersAsync();
        Assert.Contains("thử lại", vm.Feedback);
        fake.FailCustomers = false; await vm.SearchCustomersAsync(); Assert.NotEmpty(vm.Customers);
    }

    [Fact]
    public async Task ConflictRefreshesPriceComponentsAndRequiresExplicitConfirmationIncludingRetry()
    {
        var fake = new Fake { Error = "catalogChanged" }; var vm = await Draft(fake);
        fake.Catalog = [new(1, "Vaccine", "vaccine", 310000, null), new(2, "Package", "package", 360000, [new(1, 3, "Vaccine")])];
        fake.FailCatalog = true; await vm.SaveAsync();
        Assert.Equal(2, vm.Lines.Count); Assert.False(vm.CanSubmit); Assert.False(vm.NeedsCatalogConfirmation);
        fake.FailCatalog = false; vm.ReloadCatalogCommand.Execute(null); await Idle(vm);
        Assert.Contains("300", vm.CatalogNotice); Assert.Contains("310", vm.CatalogNotice); Assert.Contains("× 3", vm.CatalogNotice);
        Assert.True(vm.NeedsCatalogConfirmation); Assert.False(vm.CanSubmit);
        Assert.Equal(310000m, vm.Lines[0].UnitPrice); Assert.Equal(3, vm.Lines[1].Components![0].Quantity);
        vm.CatalogConfirmed = true; Assert.True(vm.CanSubmit);
        fake.Catalog = [fake.Catalog[1]]; vm.ReloadCatalogCommand.Execute(null); await Idle(vm);
        Assert.False(vm.CanSubmit); Assert.False(vm.Lines[0].Available);
        vm.RemoveLineCommand.Execute(vm.Lines[0]); vm.CatalogConfirmed = true; Assert.True(vm.CanSubmit);
    }

    [Fact]
    public async Task PaginationEmptyStateAndSessionSwitchClearAllOldDataEvenPendingResponse()
    {
        var handler = new Handler(); var identity = await Login(handler); var fake = new Fake();
        var vm = await Draft(fake, identity);
        await vm.LoadAsync(); Assert.Equal(25, vm.Registrations.Count); Assert.True(vm.NextCommand.CanExecute(null));
        await vm.LoadPageAsync(2); Assert.Empty(vm.Registrations); Assert.True(vm.PreviousCommand.CanExecute(null));
        await vm.LoadCustomersAsync(2); Assert.Equal(2, fake.CustomerPage); Assert.Null(vm.Customer); Assert.Null(vm.Recipient);
        fake.ListGate = new(); var pending = vm.LoadAsync();
        await identity.LogoutAsync();
        Assert.Empty(vm.Lines); Assert.Empty(vm.Catalog); Assert.Empty(vm.Registrations); Assert.Empty(vm.Customers);
        handler.Title = "Admin"; await identity.LoginAsync("other", "synthetic");
        fake.ListGate.SetResult([Summary]); await pending;
        Assert.Empty(vm.Registrations); Assert.False(vm.HasAccess); Assert.False(identity.CanUseRegistrations);
        Assert.False(new RegistrationViewModel(fake, identity).HasAccess);
    }

    [Theory]
    [InlineData("invalidSession")]
    [InlineData("permissionDenied")]
    public async Task ServerRevocationHidesFormAndClearsProfiles(string code)
    {
        var fake = new Fake { Error = code }; var vm = await Draft(fake);
        await vm.SaveAsync(); Assert.False(vm.HasAccess); Assert.False(vm.ShowDraft); Assert.Empty(vm.Lines); Assert.Empty(vm.Customers);
    }

    private static readonly RegistrationRequest Request = new(10, 20, [new("vaccine", 1, 1, 300000m, null)]);
    private static readonly RegistrationSummary Summary = new(7, DateTimeOffset.UtcNow, "DaDangKy", new(10, "Customer"), new(20, "Recipient"), null, 600000m, "VND");
    private static readonly RegistrationDetail Detail = new(7, DateTimeOffset.UtcNow, "DaDangKy", new(10, "Customer"), new(20, "Recipient"), 1, [new(1, "Vaccine", "vaccine", 2, 150000m, null)], 600000m, "VND");
    private static async Task<IdentityApiService> Login(Handler handler)
    {
        var identity = new IdentityApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
        Assert.True((await identity.LoginAsync("synthetic", "synthetic")).Succeeded); return identity;
    }
    private static async Task<RegistrationViewModel> Draft(Fake fake, IdentityApiService? identity = null)
    {
        var vm = new RegistrationViewModel(fake, identity ?? await Login(new Handler())); await vm.NewAsync();
        vm.Customer = vm.Customers[0]; await vm.LoadRecipientsAsync(); vm.Recipient = vm.Recipients[0];
        foreach (var item in vm.Catalog) { vm.CatalogItem = item; vm.AddLineCommand.Execute(null); }
        return vm;
    }
    private static async Task Idle(RegistrationViewModel vm)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (vm.IsBusy) await Task.Delay(10, timeout.Token);
    }
    private sealed class Fake : IRegistrationService
    {
        public RegistrationCatalogItem[] Catalog = [new(1, "Vaccine", "vaccine", 300000m, null), new(2, "Package", "package", 350000m, [new(1, 2, "Vaccine")])];
        public string? Error; public bool FailDetail, FailList, FailCustomers, FailCatalog;
        public int PostCount, DetailCount, CustomerPage;
        public TaskCompletionSource<int>? CreateGate;
        public TaskCompletionSource<RegistrationSummary[]>? ListGate;
        public Task<CustomerRecord[]> GetCustomersAsync(string search, int page) {
            CustomerPage = page; if (FailCustomers) throw new HttpRequestException("SQL JWT secret");
            return Task.FromResult<CustomerRecord[]>([new(10, "Customer", null, "0900000000", "test@example.invalid")]);
        }
        public Task<RecipientRecord[]> GetRecipientsAsync(int id) => Task.FromResult<RecipientRecord[]>([new(20, id, "Recipient", null, new(2000, 1, 1), "Nam", "Con")]);
        public Task<RegistrationCatalogItem[]> GetCatalogAsync() => FailCatalog ? throw new HttpRequestException() : Task.FromResult(Catalog);
        public Task<RegistrationSummary[]> GetRegistrationsAsync(int page) => FailList ? throw new HttpRequestException() : ListGate?.Task ?? Task.FromResult(page == 1 ? Enumerable.Repeat(Summary, 25).ToArray() : []);
        public Task<RegistrationDetail> GetDetailAsync(int id) { DetailCount++; if (FailDetail) throw new HttpRequestException(); return Task.FromResult(Detail); }
        public Task<int> CreateAsync(RegistrationRequest request) {
            PostCount++; if (Error is not null) throw new RegistrationApiException(Error, "Safe error");
            return CreateGate?.Task ?? Task.FromResult(7);
        }
    }
    private sealed class Handler : HttpMessageHandler
    {
        public string Title = "LeTan", Source = "database", Code = "invalidSession";
        public HttpStatusCode PostStatus = HttpStatusCode.Created;
        public bool OfflinePost, TimeoutPost;
        public int PostCount, RefreshCount;
        public string? PostBody;
        public List<string> Paths = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.PathAndQuery; Paths.Add(path);
            if (path.EndsWith("/login") || path.EndsWith("/refresh")) {
                if (path.EndsWith("/refresh")) RefreshCount++;
                return Json(new { accessToken = "access", refreshToken = "refresh" });
            }
            if (path.EndsWith("/logout")) return new(HttpStatusCode.NoContent);
            if (path.EndsWith("/me")) return Json(new { accountType = "employee", employee = new { title = Title }, capabilities = new { profilesRead = true, profilesCreate = false, profilesUpdate = false } });
            Assert.Equal("Bearer access", request.Headers.Authorization?.ToString());
            if (request.Method == HttpMethod.Post) {
                PostCount++; PostBody = await request.Content!.ReadAsStringAsync(ct);
                if (OfflinePost) throw new HttpRequestException("SQL JWT secret");
                if (TimeoutPost) throw new TaskCanceledException();
                return PostStatus == HttpStatusCode.Created ? Json(new { id = 7 }, PostStatus) : Json(new { code = Code, title = "SQL JWT secret" }, PostStatus);
            }
            if (path.Contains("/customers?")) return Json(new { items = new[] { new { id = 10, fullName = "Customer", phoneNumber = "0900000000", email = "test@example.invalid" } } });
            if (path.Contains("/recipients?")) return Json(new { items = Enumerable.Range(0, path.Contains("page=1&") ? 100 : 1).Select(i => new { id = i + 1, customerId = 10, fullName = "Recipient", dateOfBirth = "2000-01-01", gender = "Nam", relationshipToCustomer = "Con" }) });
            if (path.EndsWith("/catalog/vaccines")) return Json(new { source = Source, items = new[] { new { id = 1, name = "Vaccine", price = 300000m } } });
            if (path.EndsWith("/catalog/packages")) return Json(new { source = Source, items = new[] { new { id = 2, name = "Package", price = 350000m, items = new[] { new { vaccineId = 1, quantity = 2 } } } } });
            if (path.Contains("/registrations?")) return Json(new { items = new[] { Summary } });
            if (path.EndsWith("/registrations/7")) return Json(Detail);
            throw new InvalidOperationException(path);
        }
        private static HttpResponseMessage Json(object body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = JsonContent.Create(body) };
    }
}
