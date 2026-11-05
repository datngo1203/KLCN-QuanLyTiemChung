using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TiemChung.Contracts;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class S6ClientTests
{
    [Fact]
    public async Task PromotionManagerLoadsCreatesUpdatesDisablesAndPreventsDeletingAssignedPromotion()
    {
        var handler = new Handler();
        var identity = await Login(handler);
        var model = new PromotionViewModel(identity);
        await model.LoadAsync();
        Assert.Single(model.Promotions);
        Assert.Contains(handler.Calls, call => call == "GET /api/v1/promotions");
        model.Name = "Chương trình mới"; model.Description = "Chương trình kiểm thử";
        model.Amount = "9999999999999999";
        await model.SaveAsync();
        Assert.Equal("POST /api/v1/promotions", handler.Calls.Last());
        Assert.Equal(9999999999999999m, handler.LastPromotion!.DiscountAmount);
        Assert.Equal(9999999999999999m, model.Selected!.Discount);
        Assert.Contains("Đã tạo", model.Feedback);
        model.Name = "Tên cập nhật"; model.Amount = "100000"; model.Enabled = false;
        await model.SaveAsync();
        Assert.StartsWith("PUT /api/v1/promotions/", handler.Calls.Last());
        Assert.False(handler.LastPromotion!.IsEnabled);
        Assert.False(model.Selected!.Enabled);
        Assert.Equal("Đã tắt", model.Selected.Status);
        Assert.Equal("Tên cập nhật", model.Selected.Name);
        Assert.Contains("Đã cập nhật", model.Feedback);
        handler.Assigned = true;
        await model.LoadAsync();
        model.Selected = Assert.Single(model.Promotions);
        Assert.True(model.IsAssigned);
        Assert.False(model.CanDelete);
        var before = handler.Calls.Count;
        await model.DeleteAsync();
        Assert.Equal(before, handler.Calls.Count);
        AssertBusinessText(model.Feedback);
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public async Task PromotionDatesUseVietnamCalendarIndependentlyOfDateTimeKind(DateTimeKind kind)
    {
        var handler = new Handler();
        var identity = await Login(handler);
        var date = DateTime.SpecifyKind(new DateTime(2026, 11, 5), kind);
        var result = await PromotionApiClient.SaveAsync(null, "Ưu đãi", "Nội dung", date, date,
            100000, true, identity.SessionRevision, identity);
        Assert.True(result.Ok, result.Error);
        Assert.Equal(new DateTimeOffset(2026, 11, 4, 17, 0, 0, TimeSpan.Zero), handler.LastPromotion!.StartsAt.ToUniversalTime());
        Assert.Equal(new DateTimeOffset(2026, 11, 5, 16, 59, 59, TimeSpan.Zero), handler.LastPromotion.EndsAt.ToUniversalTime());
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("10000000000000000")]
    [InlineData("wrong")]
    public async Task PromotionValidationRejectsInvalidMoneyWithoutSending(string amount)
    {
        var handler = new Handler();
        var model = new PromotionViewModel(await Login(handler)) {
            Name = "Chương trình", Description = "Nội dung", Amount = amount
        };
        Assert.False(model.CanSave);
        var before = handler.Calls.Count;
        await model.SaveAsync();
        Assert.Equal(before, handler.Calls.Count);
        model.Amount = "100000";
        model.End = model.Start.AddDays(-1);
        Assert.False(model.CanSave);
        Assert.NotEmpty(model.Validation);
    }

    [Theory]
    [InlineData("forbidden")]
    [InlineData("server")]
    [InlineData("network")]
    [InlineData("timeout")]
    public async Task PromotionFailuresKeepDraftAndShowSafeFeedback(string failure)
    {
        var handler = new Handler();
        var model = new PromotionViewModel(await Login(handler));
        handler.Failure = failure;
        await model.LoadAsync();
        Assert.NotEmpty(model.Feedback);
        AssertBusinessText(model.Feedback);
        Assert.Empty(model.Promotions);
        model.Name = "Giữ bản nháp"; model.Description = "Nội dung"; model.Amount = "100000";
        await model.SaveAsync();
        Assert.Equal("Giữ bản nháp", model.Name);
        Assert.Equal("100000", model.Amount);
        Assert.NotEmpty(model.Feedback);
        AssertBusinessText(model.Feedback);
    }

    [Fact]
    public async Task PromotionWithoutPermissionDoesNotLoadOrWrite()
    {
        var handler = new Handler { Manager = false };
        var model = new PromotionViewModel(await Login(handler));
        var before = handler.Calls.Count;
        await model.LoadAsync();
        Assert.False(model.CanEdit);
        Assert.Contains("không có quyền", model.Feedback);
        Assert.Equal(before, handler.Calls.Count);
    }

    [Theory]
    [InlineData(150000, 100000, false, 0, 200000)]
    [InlineData(150000, 100000, true, 100000, 100000)]
    [InlineData(100000, 300000, true, 100000, 50000)]
    public async Task RegistrationPromotionPreviewAndRequestPreserveFeeAndReadServerSnapshot(
        int subtotal, int offered, bool selected, int discount, int total)
    {
        var handler = new Handler { Price = subtotal, Offered = offered };
        var identity = await Login(handler);
        var model = await Draft(handler, identity);
        Assert.Null(model.SelectedPromotion!.Id);
        Assert.Equal("Không áp dụng ưu đãi", model.SelectedPromotion.Label);
        Assert.Contains(handler.Calls, call => call == "GET /api/v1/promotions/valid");
        if (selected) model.SelectedPromotion = model.Promotions[1];
        Assert.Equal(discount, model.DiscountPreview);
        Assert.Equal(subtotal + 50000, model.TotalBeforeDiscount);
        Assert.Equal(total, model.TotalDuePreview);
        Assert.Contains("50", model.ConsultationFeeLabel);
        await model.SaveAsync();
        using var request = JsonDocument.Parse(handler.RegistrationBody!);
        Assert.Equal(selected ? 3 : (int?)null,
            request.RootElement.TryGetProperty("promotionId", out var id) ? id.GetInt32() : null);
        Assert.Equal(subtotal, request.RootElement.GetProperty("items")[0].GetProperty("expectedUnitPrice").GetDecimal());
        Assert.False(request.RootElement.TryGetProperty("discountAmount", out _));
        Assert.False(request.RootElement.TryGetProperty("consultationFee", out _));
        var post = handler.Calls.IndexOf("POST /api/v1/registrations");
        Assert.Equal("GET /api/v1/registrations/7", handler.Calls[post + 1]);
        Assert.Equal(selected ? 25000m : 0m, model.Detail!.DiscountAmount);
        Assert.Equal(subtotal + 50000m - (selected ? 25000 : 0), model.Detail.TotalDue);
        Assert.Equal(selected ? "Tên đã chốt" : null, model.Detail.PromotionName);
        AssertBusinessText(model.Feedback);
    }

    [Fact]
    public async Task UnavailablePromotionReloadsResetsSelectionAndPreservesWholeDraft()
    {
        var handler = new Handler { Unavailable = true };
        var identity = await Login(handler);
        var model = await Draft(handler, identity);
        model.SelectedPromotion = model.Promotions[1];
        var lines = model.Lines.ToArray();
        var contact = model.ContactEmail;
        var date = model.PreferredDate;
        await model.SaveAsync();
        Assert.Equal(2, handler.Calls.Count(call => call == "GET /api/v1/promotions/valid"));
        Assert.Null(model.SelectedPromotion!.Id);
        Assert.Equal(lines, model.Lines.ToArray());
        Assert.Equal(contact, model.ContactEmail);
        Assert.Equal(date, model.PreferredDate);
        Assert.Equal("Tỉnh thử", model.ProvinceCity);
        Assert.Equal("Đường thử", model.AddressLine);
        Assert.Contains("chọn lại", model.Feedback);
        Assert.True(model.ShowDraft);
        Assert.Null(model.Detail);
        handler.Unavailable = false;
        await model.SaveAsync();
        using var request = JsonDocument.Parse(handler.RegistrationBody!);
        Assert.False(request.RootElement.TryGetProperty("promotionId", out _));
        Assert.NotNull(model.Detail);
    }

    private static async Task<IdentityApiService> Login(Handler handler)
    {
        var identity = new IdentityApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Assert.True((await identity.LoginAsync("synthetic", "synthetic")).Succeeded);
        return identity;
    }

    private static async Task<RegistrationViewModel> Draft(Handler handler, IdentityApiService identity)
    {
        var model = new RegistrationViewModel(new RegistrationApiService(identity), identity);
        await model.NewAsync();
        model.Customer = Assert.Single(model.Customers);
        await model.LoadRecipientsAsync();
        model.Recipient = Assert.Single(model.Recipients);
        model.CatalogItem = Assert.Single(model.Catalog); model.AddLineCommand.Execute(null);
        model.ProvinceCity = "Tỉnh thử"; model.WardCommune = "Phường thử"; model.AddressLine = "Đường thử";
        model.PreferredDate = model.VietnamToday.AddDays(1);
        Assert.True(model.CanSubmit, model.FormError);
        return model;
    }

    private static void AssertBusinessText(string text)
    {
        foreach (var technical in new[] { "API / SQL", "Catalog SQL", "mô phỏng", "không xử lý tiền thật",
            "Trung tâm tiêm chủng VAC", "SQL JWT secret" })
            Assert.DoesNotContain(technical, text, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class Handler : HttpMessageHandler
    {
        public bool Manager = true, Assigned, Unavailable;
        public string? Failure, RegistrationBody;
        public int Price = 150000, Offered = 100000;
        public UpsertPromotionRequest? LastPromotion;
        public List<string> Calls = [];
        private int? _selectedPromotion;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            Calls.Add($"{request.Method} {path}");
            if (path.EndsWith("/login")) return Json(new { accessToken = "access", refreshToken = "refresh" });
            if (path.EndsWith("/me")) return Json(new { accountType = "employee", employee = new { title = "LeTan" },
                permissions = Manager ? new[] { "QuanLyKhuyenMai" } : [], capabilities=new { promotions=Manager, registrations=true } });
            Assert.Equal("Bearer access", request.Headers.Authorization!.ToString());
            if (path.StartsWith("/api/v1/promotions")) {
                if (Failure == "network") throw new HttpRequestException("SQL JWT secret");
                if (Failure == "timeout") throw new TaskCanceledException();
                if (Failure is not null) return Json(new { code = Failure == "forbidden" ? "permissionDenied" : "dependencyUnavailable",
                    title = "SQL JWT secret" }, Failure == "forbidden" ? HttpStatusCode.Forbidden : HttpStatusCode.ServiceUnavailable);
                if (path.EndsWith("/valid")) return Json(new { items = new[] { new RegistrationPromotion(3, "Ưu đãi hiện hành", Offered) } });
                if (request.Method == HttpMethod.Get) return Json(new { items = new[] { Promotion() } });
                LastPromotion = await request.Content!.ReadFromJsonAsync<UpsertPromotionRequest>(ct);
                return Json(Promotion(), request.Method == HttpMethod.Post ? HttpStatusCode.Created : HttpStatusCode.OK);
            }
            if (path.EndsWith("/customers")) return Json(new { items = new[] {
                new { id = 10, fullName = "Khách thử", phoneNumber = "0900000000", email = "test@example.invalid" } } });
            if (path.EndsWith("/recipients")) return Json(new { items = new[] {
                new { id = 20, customerId = 10, fullName = "Người thử", dateOfBirth = "2020-01-01", gender = "Nữ", relationshipToCustomer = "Con" } } });
            if (path.EndsWith("/catalog/vaccines")) return Json(new { source = "database", items = new[] { new { id = 1, name = "Dịch vụ thử", price = Price } } });
            if (path.EndsWith("/catalog/packages")) return Json(new { source = "database", items = Array.Empty<object>() });
            if (path.EndsWith("/registrations/settings")) return Json(new { consultationFee = 50000 });
            if (path.EndsWith("/registrations") && request.Method == HttpMethod.Post) {
                RegistrationBody = await request.Content!.ReadAsStringAsync(ct);
                using var body = JsonDocument.Parse(RegistrationBody);
                _selectedPromotion = body.RootElement.TryGetProperty("promotionId", out var id) ? id.GetInt32() : null;
                return Unavailable ? Json(new { code = "promotionUnavailable" }, HttpStatusCode.Conflict)
                    : Json(new { id = 7 }, HttpStatusCode.Created);
            }
            if (path.EndsWith("/registrations/7")) return Json(new RegistrationDetail(7, DateTimeOffset.UtcNow, "DaDangKy",
                new(10, "Khách thử"), new(20, "Người thử"), null, [], Price, "VND",
                DateOnly.FromDateTime(DateTime.Today), RegistrationClientTests.SyntheticContact, 50000,
                _selectedPromotion is null ? null : "Tên đã chốt", _selectedPromotion is null ? 0 : 25000));
            throw new InvalidOperationException(path);
        }
        private PromotionDto Promotion() => new(3, LastPromotion?.Name ?? "Ưu đãi hiện hành",
            LastPromotion?.Description ?? "Nội dung", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1),
            LastPromotion?.DiscountAmount ?? Offered, true, Assigned, LastPromotion?.IsEnabled ?? true);
        private static HttpResponseMessage Json(object body, HttpStatusCode status = HttpStatusCode.OK) =>
            new(status) { Content = JsonContent.Create(body) };
    }
}
