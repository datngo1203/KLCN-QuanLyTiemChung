using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Threading;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using TiemChung.Desktop.ViewsV2;
using Xunit;

namespace TiemChung.Windows.Ui.Tests;

public sealed partial class ComboBoxTemplateTests
{
    [Theory]
    [InlineData(800, 560)]
    [InlineData(1200, 760)]
    public async Task S6RegistrationComboRendersLabelsAndBindsDiscountedSummary(int width, int height)
    {
        using var http = new HttpClient(new S6Login()) { BaseAddress = new Uri("http://localhost") };
        var identity = new IdentityApiService(http);
        Assert.True((await identity.LoginAsync("synthetic", "synthetic")).Succeeded);
        await host.Run(_ => {
            var model = new RegistrationViewModel(new S6Registrations(), identity);
            model.NewAsync().GetAwaiter().GetResult();
            model.CatalogItem = Assert.Single(model.Catalog);
            model.AddLineCommand.Execute(null);
            var view = new RegistrationView { DataContext = model };
            using var source = new HwndSource(new HwndSourceParameters("S6 registration regression") {
                Width = width, Height = height, WindowStyle = 0
            }) { RootVisual = view };
            view.Measure(new Size(width, height)); view.Arrange(new Rect(0, 0, width, height)); view.UpdateLayout();
            var combo = Assert.Single(VisualChildren<ComboBox>(view), c => AutomationProperties.GetName(c) == "Ưu đãi");
            Assert.NotNull(combo.ItemTemplate);
            Assert.Equal("Không áp dụng ưu đãi", Assert.Single(VisualChildren<TextBlock>(combo)).Text);
            combo.SetCurrentValue(Selector.SelectedItemProperty, model.Promotions[1]);
            view.UpdateLayout();
            Assert.Same(model.Promotions[1], model.SelectedPromotion);
            Assert.Equal(model.Promotions[1].Label, Assert.Single(VisualChildren<TextBlock>(combo)).Text);
            Assert.Equal(150000m, model.DiscountPreview);
            Assert.Equal(50000m, model.TotalDuePreview);
            Assert.True(combo.IsTabStop);
            combo.IsDropDownOpen = true;
            try {
                var popup = Assert.IsType<Popup>(combo.Template.FindName("PART_Popup", combo));
                view.Dispatcher.Invoke(() => {}, DispatcherPriority.ApplicationIdle);
                popup.Child.UpdateLayout();
                foreach (var option in model.Promotions) {
                    var item = Assert.IsType<ComboBoxItem>(combo.ItemContainerGenerator.ContainerFromItem(option));
                    Assert.Equal(option.Label, Assert.Single(VisualChildren<TextBlock>(item)).Text);
                }
            } finally { combo.IsDropDownOpen = false; }

            var text = string.Join(Environment.NewLine, VisualChildren<TextBlock>(view).Select(t => t.Text));
            Assert.Contains("Tổng thanh toán: 50", text);
            Assert.Contains("Phí khám/tư vấn: 50", text);
            Assert.Contains("Ưu đãi: 150", text);
            foreach (var forbidden in new[] { "API / SQL", "Catalog SQL", "mô phỏng",
                "không xử lý tiền thật", "Trung tâm tiêm chủng VAC", "RegistrationPromotion {" })
                Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
        });
    }

    private sealed class S6Login : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = request.RequestUri!.AbsolutePath.EndsWith("/login")
                    ? JsonContent.Create(new { accessToken = "access", refreshToken = "refresh" })
                    : JsonContent.Create(new { accountType = "employee", employee = new { title = "LeTan" } })
            });
    }

    private sealed class S6Registrations : IRegistrationService
    {
        public Task<CustomerRecord[]> GetCustomersAsync(string search, int page) => Task.FromResult(Array.Empty<CustomerRecord>());
        public Task<RecipientRecord[]> GetRecipientsAsync(int id) => Task.FromResult(Array.Empty<RecipientRecord>());
        public Task<RegistrationCatalogItem[]> GetCatalogAsync() => Task.FromResult<RegistrationCatalogItem[]>([new(1,"Dịch vụ thử","vaccine",150000,null)]);
        public Task<RegistrationSettings> GetSettingsAsync() => Task.FromResult(new RegistrationSettings(50000));
        public Task<RegistrationPromotion[]> GetPromotionsAsync() => Task.FromResult<RegistrationPromotion[]>([new(3,"Ưu đãi thử",300000)]);
        public Task<RegistrationSummary[]> GetRegistrationsAsync(int page) => Task.FromResult(Array.Empty<RegistrationSummary>());
        public Task<RegistrationDetail> GetDetailAsync(int id) => throw new NotSupportedException();
        public Task<int> CreateAsync(RegistrationRequest request) => throw new NotSupportedException();
    }
}
