using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using TiemChung.Desktop.ViewsV2;
using Xunit;

namespace TiemChung.Windows.Ui.Tests;

public sealed partial class ComboBoxTemplateTests
{
    [Fact]
    public async Task AppointmentLifecycleRendersDateFormAndBlocksClinicalActions()
    {
        using var http = new HttpClient(new LifecycleResponses()) { BaseAddress = new Uri("http://localhost") };
        var identity = new IdentityApiService(http);
        Assert.True((await identity.LoginAsync("synthetic", "synthetic")).Succeeded);
        await host.Run(_ => {
            var model = new AppointmentViewModel(identity: identity);
            model.LoadAsync().GetAwaiter().GetResult(); model.Selected = model.Appointments[0]; model.LoadDetailAsync().GetAwaiter().GetResult();
            var view = new AppointmentView { DataContext = model };
            using var source = new HwndSource(new HwndSourceParameters("Appointment lifecycle regression") { Width = 1100, Height = 760, WindowStyle = 0 }) { RootVisual = view };
            view.Measure(new Size(1100,760)); view.Arrange(new Rect(0,0,1100,760)); view.UpdateLayout();
            Assert.True(Assert.Single(VisualChildren<Button>(view), b => Equals(b.Content,"Đổi lịch")).IsEnabled);
            Assert.True(Assert.Single(VisualChildren<Button>(view), b => Equals(b.Content,"Hủy lịch")).IsEnabled);
            model.BeginRescheduleCommand.Execute(null); view.UpdateLayout();
            var date = Assert.Single(VisualChildren<DatePicker>(view), d => AutomationProperties.GetName(d)=="Ngày hẹn mới"); Assert.True(date.IsVisible);
            Assert.True(Assert.Single(VisualChildren<Button>(view), b => Equals(b.Content,"Xem và xác nhận đổi lịch")).IsVisible);
            model.DiscardRescheduleCommand.Execute(null); model.Selected=model.Appointments[1];model.LoadDetailAsync().GetAwaiter().GetResult();view.UpdateLayout();
            Assert.False(Assert.Single(VisualChildren<Button>(view), b => Equals(b.Content,"Đổi lịch")).IsEnabled);
            Assert.False(Assert.Single(VisualChildren<Button>(view), b => Equals(b.Content,"Hủy lịch")).IsEnabled);
            Assert.Contains(VisualChildren<TextBlock>(view), t=>t.Text.Contains("đã bắt đầu khám sàng lọc"));
        });
    }

    private sealed class LifecycleResponses : HttpMessageHandler
    {
        private static AppointmentRecord Row(int id) => new(id,7,new(1,"Nguyễn Test An"),new(2,"Nguyễn Test Bình"),"vaccine",12,99,"Avaxim 80U",DateTimeOffset.UtcNow.AddDays(2),AppointmentResponses.Facility,"DaXepLich","AAAAAAAAAAE=",id==1,id==1?null:"clinicalAlreadyStarted");
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path=request.RequestUri!.AbsolutePath;
            object body=path switch {
                "/api/v1/auth/login"=>new{accessToken="synthetic",refreshToken="synthetic"},
                "/api/v1/auth/me"=>new{accountType="employee",employee=new{title="LeTan"},permissions=new[]{"QuanLyLichHen"},capabilities=new{appointments=true}},
                "/api/v1/appointments"=>new{items=new[]{Row(1),Row(2)}},
                "/api/v1/appointments/1"=>Row(1), "/api/v1/appointments/2"=>Row(2),
                _=>throw new InvalidOperationException("Unexpected lifecycle route.")
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(body)});
        }
    }

    [Fact]
    public async Task AppointmentPickerRendersReadinessAndKeepsSaveOutsideTheFormScroll()
    {
        using var http = new HttpClient(new AppointmentResponses()) { BaseAddress = new Uri("http://localhost") };
        var identity = new IdentityApiService(http);
        Assert.True((await identity.LoginAsync("synthetic", "synthetic")).Succeeded);

        await host.Run(_ => {
            var model = new AppointmentViewModel(identity: identity);
            model.NewAsync().GetAwaiter().GetResult();
            var view = new AppointmentView { DataContext = model };
            using var source = new HwndSource(new HwndSourceParameters("Appointment regression") {
                Width = 800, Height = 560, WindowStyle = 0
            }) { RootVisual = view };
            view.Measure(new Size(800, 560));
            view.Arrange(new Rect(0, 0, 800, 560));
            view.UpdateLayout();

            var picker = Assert.Single(VisualChildren<ComboBox>(view), c => AutomationProperties.GetName(c) == "Đăng ký nguồn");
            picker.SetCurrentValue(Selector.SelectedItemProperty, Assert.Single(model.Sources));
            view.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            view.UpdateLayout();
            Assert.Contains(VisualChildren<TextBlock>(picker), t => t.Text == "DK-0007 · Người được tiêm thử");
            Assert.Contains(VisualChildren<TextBlock>(picker), t => t.Text == "Chờ xác nhận thanh toán");
            Assert.Contains(VisualChildren<TextBlock>(view), t => t.Text == AppointmentResponses.Facility);
            Assert.DoesNotContain(VisualChildren<TextBox>(view), t => t.Text == AppointmentResponses.Facility);
            model.SelectedItem = Assert.Single(model.Items);
            var save = Assert.Single(VisualChildren<Button>(view), b => Equals(b.Content, "Lưu lịch hẹn"));
            Assert.True(save.IsVisible);
            Assert.False(save.IsEnabled);
            for (var parent = VisualTreeHelper.GetParent(save); parent is not null; parent = VisualTreeHelper.GetParent(parent))
                Assert.IsNotType<ScrollViewer>(parent);
        });
    }

    private sealed class AppointmentResponses : HttpMessageHandler
    {
        public const string Facility = "Trung tâm tiêm chủng VAC · Địa chỉ thử nghiệm";
        private static readonly AppointmentSourceOption Row = new(7, "DaDangKy", new(1, "Khách hàng thử"),
            new(2, "Người được tiêm thử"), DateOnly.FromDateTime(DateTime.Today.AddDays(2)), "ChoXacNhan",
            "TrucTuyen", false, "pending", Facility);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            object body = request.RequestUri!.AbsolutePath switch {
                "/api/v1/auth/login" => new { accessToken = "synthetic", refreshToken = "synthetic" },
                "/api/v1/auth/me" => new { accountType = "employee", employee = new { title = "LeTan" }, permissions = new[] { "QuanLyLichHen" }, capabilities=new { appointments=true } },
                "/api/v1/appointments/sources" => new { items = new[] { Row } },
                "/api/v1/appointments/sources/7" => new AppointmentSource(Row, [new(12, "vaccine", 99, "Vắc xin thử", 1)]),
                _ => throw new InvalidOperationException("Unexpected appointment test route.")
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
        }
    }
}
