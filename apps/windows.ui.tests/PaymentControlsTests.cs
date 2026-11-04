using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Threading;
using TiemChung.Contracts;
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
    public async Task CashierEditorFormatsInputsAndShowsOnlyTheCurrentAction(int width, int height)
    {
        using var http = new HttpClient(new CashierLogin()) { BaseAddress = new Uri("http://localhost") };
        var identity = new IdentityApiService(http);
        Assert.True((await identity.LoginAsync("synthetic", "synthetic")).Succeeded);

        await host.Run(_ => {
            var service = new CashierDetails();
            var model = new PaymentViewModel(service, identity);
            model.LoadPageAsync(1).GetAwaiter().GetResult();
            var view = new PaymentView { DataContext = model };
            using var source = new HwndSource(new HwndSourceParameters("A08 regression") {
                Width = width, Height = height, WindowStyle = 0
            }) { RootVisual = view };
            view.Measure(new Size(width, height)); view.Arrange(new Rect(0, 0, width, height));
            view.UpdateLayout();
            var pendingPanel = Assert.IsType<Border>(view.FindName("PendingPanel"));
            var receipt = Assert.IsType<Border>(view.FindName("Receipt"));
            Assert.Equal(Visibility.Collapsed, pendingPanel.Visibility);
            Assert.Equal(Visibility.Collapsed, receipt.Visibility);
            model.SelectedRegistration = Assert.Single(model.Registrations);
            model.LoadDetailAsync().GetAwaiter().GetResult();
            view.UpdateLayout();
            T Field<T>(string name) where T : UIElement =>
                Assert.Single(VisualChildren<T>(view), child => AutomationProperties.GetName(child) == name);
            var selector = Field<ComboBox>("Phương thức thanh toán");
            var cash = Field<VndMoneyInput>("Số tiền khách đưa");
            var card = Field<TextBox>("Số thẻ");
            var transfer = Field<TextBox>("Tài khoản hoặc định danh ví");
            Assert.True(cash.IsVisible);
            Assert.False(card.IsVisible || transfer.IsVisible);
            Assert.Equal("Tiền mặt", Assert.Single(VisualChildren<TextBlock>(selector)).Text);
            cash.SetCurrentValue(TextBox.TextProperty, "550000");
            Assert.Equal("550 000", cash.Text);
            Assert.Equal("550000", model.Draft!.Cash);

            selector.SetCurrentValue(Selector.SelectedValueProperty, "TheTaiQuay");
            view.UpdateLayout();
            Assert.Equal("Thẻ tại quầy", Assert.Single(VisualChildren<TextBlock>(selector)).Text);
            Assert.True(card.IsVisible);
            Assert.False(cash.IsVisible || transfer.IsVisible);
            card.SetCurrentValue(TextBox.TextProperty, "4242424242424242");
            Assert.Equal("4242 4242 4242 4242", card.Text);
            Assert.Equal("TheTaiQuay", model.Draft.Method);
            Assert.True(card.IsTabStop);
            selector.SetCurrentValue(Selector.SelectedValueProperty, "ChuyenKhoanQR");
            view.UpdateLayout();
            Assert.Equal("Chuyển khoản / QR", Assert.Single(VisualChildren<TextBlock>(selector)).Text);
            Assert.True(transfer.IsVisible);
            Assert.False(cash.IsVisible || card.IsVisible);
            selector.IsDropDownOpen = true;
            try {
                var popup = Assert.IsType<Popup>(selector.Template.FindName("PART_Popup", selector));
                view.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                popup.Child.UpdateLayout();
                Assert.Equal(new[] { "Tiền mặt", "Thẻ tại quầy", "Chuyển khoản / QR" },
                    VisualChildren<TextBlock>(popup.Child).Select(label => label.Text));
            }
            finally { selector.IsDropDownOpen = false; }

            var review = Assert.IsType<Button>(view.FindName("RecordButton"));
            review.BringIntoView();
            view.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            var scroll = VisualChildren<ScrollViewer>(view).First();
            Assert.True(scroll.VerticalOffset > 0);
            Assert.True(review.IsEnabled && review.IsTabStop);

            service.Pending = true;
            model.ConfirmDiscard = _ => true;
            model.LoadDetailAsync().GetAwaiter().GetResult();
            view.UpdateLayout();
            Assert.False(review.IsVisible);
            Assert.True(pendingPanel.IsVisible);
            Assert.False(receipt.IsVisible);
            Assert.Contains(VisualChildren<Button>(view), button => button.IsVisible && button.IsEnabled &&
                Equals(button.Content, "_Xác nhận đã nhận tiền"));
        });
    }

    private sealed class CashierLogin : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = request.RequestUri!.AbsolutePath.EndsWith("/me")
                    ? JsonContent.Create(new { accountType = "employee", employee = new { title = "ThuNgan" },
                        permissions = new[] { "XemThanhToan", "GhiThanhToan" } })
                    : JsonContent.Create(new { accessToken = "synthetic", refreshToken = "synthetic" })
            });
    }

    private sealed class CashierDetails : IPaymentService
    {
        private static readonly RegistrationPerson Person = new(1, "[DEMO] Hồ sơ");
        private static readonly PaymentRegistration Row = new(1, DateTimeOffset.UtcNow, Person, Person,
            500000, 550000, 0, 550000, null, "VND", 50000);
        public bool Pending { get; set; }
        public Task<PaymentRegistration[]> SearchAsync(string search, int page) => Task.FromResult<PaymentRegistration[]>([Row]);
        public Task<RegistrationPayment> GetAsync(int id) => Task.FromResult(new RegistrationPayment(id, "DaDangKy", Person, Person,
            [new(1, "vaccine", 1, "[DEMO] Vaccine", 1, 500000, 500000, null)], 500000, 550000, 0, 550000, "VND",
            true, !Pending, true, "Mô phỏng", Pending ? new(1, id, 550000, null, DateTimeOffset.UtcNow, "ToanBo",
                "ChuyenKhoanOnline", "ChoXacNhan", true, Provider: "Synthetic Bank", Reference: "PENDING-107") : null,
            50000, Pending ? "ChoXacNhan" : "ChuaThanhToan", CanConfirmPendingPayment: Pending,
            RegistrationContact: new("[DEMO] Contact", "0900000107", null, null, null, null)));
        public Task<RegistrationPayment> RecordAsync(int id, RecordPaymentRequest request) => throw new NotSupportedException();
        public Task<RegistrationPayment> ConfirmPendingAsync(int id) => throw new NotSupportedException();
    }
}
