using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class PaymentClientTests
{
    [Theory]
    [InlineData("ThuNgan", "XemThanhToan,GhiThanhToan", true, true)]
    [InlineData("ThuNgan", "XemThanhToan", true, false)]
    [InlineData("ThuNgan", "GhiThanhToan", false, false)]
    [InlineData("ThuNgan", "", false, false)]
    [InlineData("Admin", "XemThanhToan,GhiThanhToan", false, false)]
    [InlineData("LeTan", "XemThanhToan,GhiThanhToan", false, false)]
    [InlineData("BacSi", "XemThanhToan,GhiThanhToan", false, false)]
    [InlineData("DieuDuong", "XemThanhToan,GhiThanhToan", false, false)]
    [InlineData("ThuKho", "XemThanhToan,GhiThanhToan", false, false)]
    public async Task TitleAndBothPermissionCodesAreRequired(string title, string grants, bool read, bool write)
    {
        var identity = await Login(new Handler { Title = title, Grants = grants.Split(',', StringSplitOptions.RemoveEmptyEntries) });
        Assert.Equal(read, identity.CanReadPayments); Assert.Equal(write, identity.CanWritePayments);
        var fake = new Fake(); var vm = new PaymentViewModel(fake, identity);
        await vm.LoadPageAsync(1); Assert.Equal(read, vm.HasAccess);
        Assert.Equal(read ? 1 : 0, fake.ListCount);
        if (read) { vm.SelectedRegistration = Row; await vm.LoadDetailAsync(); Assert.NotNull(vm.Detail); }
        Assert.Equal(write, vm.CanRecord); Assert.Equal(read && !write, vm.IsReadOnly);
    }

    [Fact]
    public async Task ApiSearchAndDetailsPreserveDecimalAndPostSendsOnlyEmptyObject()
    {
        var handler = new Handler(); var api = new PaymentApiService(await Login(handler));
        await api.SearchAsync("  DK-0007 & tên  ", 3);
        Assert.Contains(handler.Paths, x => x.Contains("search=DK-0007%20%26%20t%C3%AAn&page=3&pageSize=25"));
        var detail = await api.GetAsync(7);
        Assert.Equal(9999999999999999m, detail.TotalDue); Assert.Equal(9999999999999999m, detail.Items[0].UnitPrice);
        var saved = await api.RecordAsync(7);
        Assert.NotNull(saved.Payment); Assert.Equal(9999999999999999m, saved.Payment.Amount);
        Assert.Equal("{}", handler.PostBody); Assert.Equal(1, handler.PostCount);
        Assert.Equal(0, handler.RefreshCount);
    }

    [Theory]
    [InlineData(400, "validation", "validation")]
    [InlineData(401, "invalidSession", "invalidSession")]
    [InlineData(403, "permissionDenied", "permissionDenied")]
    [InlineData(409, "paymentAlreadyRecorded", "paymentAlreadyRecorded")]
    [InlineData(409, "paymentNotRequired", "paymentNotRequired")]
    [InlineData(409, "paymentDataConflict", "paymentDataConflict")]
    [InlineData(503, "dependencyUnavailable", "outcomeUnknown")]
    public async Task ApiErrorsAreSanitizedAnd401NeverReplaysPayment(int status, string code, string expectedCode)
    {
        var handler = new Handler { PostStatus = (HttpStatusCode)status, Code = code };
        var api = new PaymentApiService(await Login(handler));
        var error = await Assert.ThrowsAsync<PaymentApiException>(() => api.RecordAsync(7));
        Assert.Equal(expectedCode, error.Code); Assert.DoesNotContain("SQL token", error.Message);
        Assert.Equal(1, handler.PostCount); Assert.Equal(0, handler.RefreshCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<html>SQL token</html>")]
    public async Task Post503IsUnknownRegardlessOfResponseBody(string body)
    {
        var handler = new Handler { PostStatus = HttpStatusCode.ServiceUnavailable, PostErrorBody = body };
        var api = new PaymentApiService(await Login(handler));
        var error = await Assert.ThrowsAsync<PaymentApiException>(() => api.RecordAsync(7));
        Assert.Equal("outcomeUnknown", error.Code); Assert.DoesNotContain("SQL token", error.Message);
        Assert.Equal(1, handler.PostCount); Assert.Equal(0, handler.RefreshCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Get503RemainsDependencyUnavailableAndCanRetryOnlyGet(bool list)
    {
        var handler = new Handler { GetStatus = HttpStatusCode.ServiceUnavailable, Code = "dependencyUnavailable" };
        var api = new PaymentApiService(await Login(handler));
        var error = await Assert.ThrowsAsync<PaymentApiException>(async () => {
            if (list) await api.SearchAsync("", 1); else await api.GetAsync(7);
        });
        Assert.Equal("dependencyUnavailable", error.Code); Assert.DoesNotContain("SQL token", error.Message);
        handler.GetStatus = HttpStatusCode.OK;
        if (list) Assert.Single(await api.SearchAsync("", 1)); else Assert.Equal(7, (await api.GetAsync(7)).RegistrationId);
        Assert.Equal(0, handler.PostCount); Assert.Equal(0, handler.RefreshCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Post503KeepsRegistrationLockedUntilUserReconcilesThroughGet(bool committed)
    {
        var handler = new Handler { PostStatus = HttpStatusCode.ServiceUnavailable, Code = "dependencyUnavailable" };
        var identity = await Login(handler); var api = new PaymentApiService(identity); var vm = new PaymentViewModel(api, identity);
        await vm.LoadPageAsync(1); vm.SelectedRegistration = Assert.Single(vm.Registrations); await vm.LoadDetailAsync();
        Assert.NotNull(vm.BeginConfirmation()); await vm.ConfirmAsync(true);
        Assert.Equal(7, vm.SelectedRegistration!.Id); Assert.Equal(7, vm.Detail!.RegistrationId);
        Assert.True(vm.UnknownOutcome); Assert.False(vm.CanRecord); Assert.False(vm.CanAcknowledgeRisk);
        Assert.Contains("GET", vm.Feedback); Assert.Null(vm.BeginConfirmation()); await vm.ConfirmAsync(true);
        Assert.Equal(1, handler.PostCount);

        handler.GetStatus = HttpStatusCode.ServiceUnavailable; await vm.LoadDetailAsync();
        Assert.Equal(7, vm.SelectedRegistration.Id); Assert.True(vm.UnknownOutcome);
        Assert.False(vm.CanRecord); Assert.False(vm.CanAcknowledgeRisk);
        handler.GetStatus = HttpStatusCode.OK; handler.PaymentRecorded = committed; await vm.LoadDetailAsync();
        Assert.False(vm.CanRecord); Assert.Equal(!committed, vm.UnknownOutcome);
        Assert.Equal(!committed, vm.CanAcknowledgeRisk); Assert.Equal(committed, vm.Detail!.Payment is not null);
        if (!committed) { vm.AcknowledgeRisk(); Assert.True(vm.CanRecord); }
        Assert.Equal(1, handler.PostCount); Assert.Equal(0, handler.RefreshCount);
    }

    [Theory]
    [InlineData("network", "outcomeUnknown")]
    [InlineData("timeout", "outcomeUnknown")]
    [InlineData("invalid201", "recordedUnreadable")]
    [InlineData("200", "outcomeUnknown")]
    public async Task ApiNeverReplaysTransportOrReadFailures(string failure, string code)
    {
        var handler = new Handler { Failure = failure }; var api = new PaymentApiService(await Login(handler));
        Assert.Equal(code, (await Assert.ThrowsAsync<PaymentApiException>(() => api.RecordAsync(7))).Code);
        Assert.Equal(1, handler.PostCount); Assert.Equal(0, handler.RefreshCount);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(502)]
    [InlineData(504)]
    public async Task UnexpectedServerErrorsRequireReconciliation(int status)
    {
        var handler = new Handler { PostStatus = (HttpStatusCode)status };
        var api = new PaymentApiService(await Login(handler));
        Assert.Equal("outcomeUnknown", (await Assert.ThrowsAsync<PaymentApiException>(() => api.RecordAsync(7))).Code);
        Assert.Equal(1, handler.PostCount);
    }

    [Fact]
    public async Task SearchPaginationEmptyAndSelectionDoNotUseLocalFiltersOrOldResponses()
    {
        var fake = new Fake(); var vm = await Ready(fake);
        Assert.True(vm.NextCommand.CanExecute(null));
        vm.Search = " keyword "; await vm.SearchAsync(); Assert.Equal("keyword", fake.Search); Assert.Equal(1, vm.Page);
        vm.Search = "not applied"; await vm.LoadPageAsync(2);
        Assert.Equal("keyword", fake.Search); Assert.Equal(2, vm.Page); Assert.True(vm.NoResults); Assert.True(vm.PreviousCommand.CanExecute(null));
        vm.Search = new string('x', 101); Assert.False(vm.SearchCommand.CanExecute(null));
        fake.GetGate = new(); vm.SelectedRegistration = Row;
        var old = vm.LoadDetailAsync(); vm.SelectedRegistration = Row with { Id = 8 };
        fake.GetGate = null; await vm.LoadDetailAsync(); Assert.Equal(8, vm.Detail!.RegistrationId);
        fake.OldGate!.SetResult(Unpaid); await old;
        Assert.Equal(8, vm.Detail.RegistrationId); Assert.Equal(8, vm.SelectedRegistration!.Id);
    }

    [Fact]
    public async Task ConfirmationCancelStaleConfirmationAndDoubleSendAreBlocked()
    {
        var fake = new Fake { PostGate = new() }; var vm = await Ready(fake);
        Assert.NotNull(vm.BeginConfirmation()); Assert.False(vm.CanRecord); Assert.Null(vm.BeginConfirmation());
        await vm.ConfirmAsync(false); Assert.Equal(0, fake.PostCount); Assert.True(vm.CanRecord);
        vm.BeginConfirmation(); var post = vm.ConfirmAsync(true);
        Assert.True(vm.IsSaving); Assert.False(vm.CanRecord); Assert.False(vm.CanBrowse);
        await vm.ConfirmAsync(true); Assert.Equal(1, fake.PostCount);
        fake.Paid = true; fake.PostGate.SetResult(Paid); await post;
        Assert.Equal(1, fake.PostCount); Assert.Equal(2, fake.GetCount); Assert.NotNull(vm.Detail!.Payment); Assert.False(vm.CanRecord);
    }

    [Fact]
    public async Task Successful201KeepsReceiptWhenGetFailsAndNeverReopensRecord()
    {
        var fake = new Fake { FailAfterPost = true }; var vm = await Ready(fake);
        vm.BeginConfirmation(); await vm.ConfirmAsync(true);
        Assert.Contains("HTTP 201", vm.Feedback); Assert.NotNull(vm.Detail!.Payment); Assert.False(vm.CanRecord);
        await vm.LoadDetailAsync(); Assert.Equal(1, fake.PostCount); Assert.NotNull(vm.Detail!.Payment); Assert.Contains("đã xác nhận", vm.Feedback);
        fake.FailGet = false; fake.Paid = true; await vm.LoadDetailAsync(); Assert.NotNull(vm.Detail!.Payment);
        Assert.False(vm.CanRecord); Assert.Equal(1, fake.PostCount);
    }

    [Fact]
    public async Task UnknownOutcomeSurvivesSelectionAndRequiresFreshGetThenRiskAcknowledgement()
    {
        var fake = new Fake { Error = "outcomeUnknown" }; var vm = await Ready(fake);
        vm.BeginConfirmation(); await vm.ConfirmAsync(true); Assert.True(vm.UnknownOutcome); Assert.False(vm.CanRecord);
        vm.AcknowledgeRisk(); Assert.False(vm.CanRecord); Assert.False(vm.CanAcknowledgeRisk);
        vm.SelectedRegistration = Row with { Id = 8 }; await vm.LoadDetailAsync(); Assert.True(vm.CanRecord);
        vm.SelectedRegistration = Row; Assert.True(vm.UnknownOutcome); Assert.False(vm.CanRecord);
        fake.FailGet = true; await vm.LoadDetailAsync(); Assert.False(vm.CanAcknowledgeRisk);
        fake.FailGet = false; await vm.LoadDetailAsync(); Assert.True(vm.CanAcknowledgeRisk); Assert.False(vm.CanRecord);
        vm.AcknowledgeRisk(); Assert.True(vm.CanRecord); Assert.Equal(1, fake.PostCount);
        fake.Error = "outcomeUnknown"; vm.BeginConfirmation(); await vm.ConfirmAsync(true);
        fake.Paid = true; await vm.LoadDetailAsync(); Assert.False(vm.UnknownOutcome); Assert.False(vm.CanRecord);
    }

    [Theory]
    [InlineData("paymentAlreadyRecorded")]
    [InlineData("paymentNotRequired")]
    [InlineData("recordedUnreadable")]
    public async Task KnownConflictsAnd201ReadFailureOnlyGetAndStayLocked(string code)
    {
        var fake = new Fake { Error = code, FailAfterPost = true }; var vm = await Ready(fake);
        vm.BeginConfirmation(); await vm.ConfirmAsync(true);
        Assert.False(vm.CanRecord); Assert.Equal(2, fake.GetCount); Assert.Equal(1, fake.PostCount);
        fake.FailGet = false; await vm.LoadDetailAsync(); Assert.False(vm.CanRecord);
        Assert.False(vm.CanAcknowledgeRisk); Assert.Equal(1, fake.PostCount);
    }

    [Theory]
    [InlineData("validation", true)]
    [InlineData("outcomeUnknown", false)]
    [InlineData("paymentDataConflict", false)]
    [InlineData("invalidSession", false)]
    [InlineData("permissionDenied", false)]
    public async Task WriteErrorsRespectLocksAndRevocation(string code, bool canRetry)
    {
        var fake = new Fake { Error = code }; var vm = await Ready(fake);
        vm.BeginConfirmation(); await vm.ConfirmAsync(true);
        Assert.Equal(canRetry, vm.CanRecord); Assert.Equal(1, fake.PostCount);
        if (code == "outcomeUnknown") { Assert.True(vm.UnknownOutcome); Assert.Equal(7, vm.SelectedRegistration!.Id); Assert.False(vm.CanAcknowledgeRisk); }
        if (code is "invalidSession" or "permissionDenied") { Assert.False(vm.HasAccess); Assert.Empty(vm.Registrations); Assert.Null(vm.Detail); }
        if (code == "paymentDataConflict") { await vm.LoadDetailAsync(); Assert.False(vm.CanRecord); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ZeroTotalAndExistingPaymentNeverRecord(bool paid)
    {
        var fake = new Fake { Paid = paid, Zero = !paid }; var vm = await Ready(fake);
        Assert.False(vm.CanRecord); Assert.Null(vm.BeginConfirmation()); Assert.Equal(0, fake.PostCount);
        Assert.Equal(paid, vm.Detail!.Payment is not null);
        Assert.Contains(paid ? "Đã ghi nhận" : "Không phát sinh", vm.Detail.State);
    }

    [Fact]
    public async Task SessionSwitchClearsAllDataAndDropsPendingReadsPostsAndConfirmations()
    {
        var handler = new Handler(); var identity = await Login(handler); var fake = new Fake(); var vm = await Ready(fake, identity);
        vm.BeginConfirmation(); await identity.LogoutAsync(); await vm.ConfirmAsync(true); Assert.Equal(0, fake.PostCount);
        Assert.Null(vm.Detail); Assert.Empty(vm.Registrations); Assert.Equal("", vm.Search);
        await identity.LoginAsync("other", "synthetic"); Assert.False(vm.HasAccess);
        var next = await Ready(fake, identity); fake.GetGate = new(); var read = next.LoadDetailAsync();
        await identity.LogoutAsync(); fake.GetGate.SetResult(Unpaid); await read; Assert.Null(next.Detail);
        await identity.LoginAsync("other", "synthetic"); fake.GetGate = null; fake.PostGate = new();
        var third = await Ready(fake, identity); third.BeginConfirmation(); var post = third.ConfirmAsync(true);
        await identity.LogoutAsync(); fake.PostGate.SetResult(Paid); await post;
        Assert.Null(third.Detail); Assert.Empty(third.Registrations); Assert.False(third.HasAccess);
        // The old API service may never send using the new account, even if queued by its UI.
        await identity.LoginAsync("other", "synthetic"); var api = new PaymentApiService(identity);
        await identity.LogoutAsync(); await identity.LoginAsync("other", "synthetic");
        var count = handler.PostCount;
        await Assert.ThrowsAsync<PaymentApiException>(() => api.RecordAsync(7)); Assert.Equal(count, handler.PostCount);
    }

    public static readonly RegistrationPayment Unpaid = new(7, "DaDangKy", new(10, "[DEMO] Customer"), new(20, "[DEMO] Recipient"),
        [new(1, "vaccine", 1, "[DEMO] Vaccine", 1, 9999999999999999m, 9999999999999999m, null)], 9999999999999999m,
        9999999999999999m, 0, 9999999999999999m, "VND", true, true, true, "Mô phỏng", null);
    public static readonly RegistrationPayment Paid = Unpaid with { PaidAmount = Unpaid.TotalDue, RemainingAmount = 0, RequiresPayment = false, CanRecordPayment = false,
        Payment = new(1, 7, Unpaid.TotalDue, 9, DateTimeOffset.UtcNow, "ToanBo", "MoPhong", "ThanhCong", true) };
    private static readonly PaymentRegistration Row = new(7, DateTimeOffset.UtcNow, Unpaid.Customer, Unpaid.Recipient, Unpaid.Subtotal, Unpaid.TotalDue, 0, Unpaid.TotalDue, null, "VND");
    private static async Task<IdentityApiService> Login(Handler handler)
    {
        var identity = new IdentityApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
        Assert.True((await identity.LoginAsync("synthetic", "synthetic")).Succeeded); return identity;
    }
    private static async Task<PaymentViewModel> Ready(Fake fake, IdentityApiService? identity = null)
    {
        var vm = new PaymentViewModel(fake, identity ?? await Login(new Handler()));
        await vm.LoadPageAsync(1); vm.SelectedRegistration = Row; await vm.LoadDetailAsync(); return vm;
    }
    private sealed class Fake : IPaymentService
    {
        public int ListCount, GetCount, PostCount; public string Search = ""; public string? Error;
        public bool Paid, Zero, FailGet, FailAfterPost;
        public TaskCompletionSource<RegistrationPayment>? GetGate, OldGate, PostGate;
        public Task<PaymentRegistration[]> SearchAsync(string search, int page) { ListCount++; Search = search; return Task.FromResult(page == 1 ? Enumerable.Repeat(Row, 25).ToArray() : []); }
        public Task<RegistrationPayment> GetAsync(int id) {
            GetCount++; if (FailGet) throw new HttpRequestException("SQL token");
            if (GetGate is not null) { OldGate = GetGate; return GetGate.Task; }
            var detail = Paid ? PaymentClientTests.Paid : Zero ? Unpaid with { Subtotal = 0, TotalDue = 0, RemainingAmount = 0, RequiresPayment = false, CanRecordPayment = false } : Unpaid;
            return Task.FromResult(detail with { RegistrationId = id });
        }
        public Task<RegistrationPayment> RecordAsync(int id) {
            PostCount++; if (FailAfterPost) FailGet = true;
            if (Error is not null) throw new PaymentApiException(Error, "Safe message");
            if (PostGate is not null) return PostGate.Task;
            Paid = true; return Task.FromResult(PaymentClientTests.Paid);
        }
    }
    private sealed class Handler : HttpMessageHandler
    {
        public string Title = "ThuNgan", Code = "validation", Failure = "";
        public string[] Grants = ["XemThanhToan", "GhiThanhToan"];
        public HttpStatusCode PostStatus = HttpStatusCode.Created, GetStatus = HttpStatusCode.OK;
        public string? PostErrorBody; public bool PaymentRecorded;
        public int PostCount, RefreshCount; public string? PostBody; public List<string> Paths = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.PathAndQuery; Paths.Add(path);
            if (path.EndsWith("/login") || path.EndsWith("/refresh")) { if (path.EndsWith("/refresh")) RefreshCount++; return Json(new { accessToken = "access", refreshToken = "refresh" }); }
            if (path.EndsWith("/logout")) return new(HttpStatusCode.NoContent);
            if (path.EndsWith("/me")) return Json(new { accountType = "employee", employee = new { title = Title }, permissions = Grants });
            if (request.Method == HttpMethod.Post) {
                PostCount++; PostBody = await request.Content!.ReadAsStringAsync(ct);
                if (Failure == "network") throw new HttpRequestException("SQL token");
                if (Failure == "timeout") throw new TaskCanceledException();
                if (Failure == "invalid201") return Json(new { unknown = true }, HttpStatusCode.Created);
                if (Failure == "200") return Json(Paid);
                if (PostErrorBody is not null) return new(PostStatus) { Content = new StringContent(PostErrorBody) };
                return PostStatus == HttpStatusCode.Created ? Json(Paid, PostStatus) : Json(new { code = Code, title = "SQL token" }, PostStatus);
            }
            if (GetStatus != HttpStatusCode.OK) return Json(new { code = Code, title = "SQL token" }, GetStatus);
            return path.Contains("payment-registrations?") ? Json(new { items = new[] { Row } }) : Json(PaymentRecorded ? Paid : Unpaid);
        }
        private static HttpResponseMessage Json(object body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = JsonContent.Create(body) };
    }
}
