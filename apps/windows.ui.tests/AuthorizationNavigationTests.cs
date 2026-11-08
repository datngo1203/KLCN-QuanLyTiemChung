using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using TiemChung.Desktop.ViewsV2;
using Xunit;

namespace TiemChung.Windows.Ui.Tests;

public sealed partial class ComboBoxTemplateTests
{
    [Theory]
    [InlineData("LeTan", "A01,A03,A07,A09,A14", "overview,profilesRead,profilesCreate,profilesUpdate,registrations,appointments,changePassword")]
    [InlineData("BacSi", "A01,A02,A14", "overview,profilesRead,screening,changePassword")]
    [InlineData("DieuDuong", "A01,A06,A06F,A14", "overview,profilesRead,vaccinations,aftercare,changePassword")]
    [InlineData("ThuNgan", "A01,A08,A14", "overview,paymentsRead,paymentsWrite,changePassword")]
    [InlineData("ThuKho", "A01,A05,A14", "overview,inventoryRead,inventoryReceive,changePassword")]
    [InlineData("Admin", "A01,A04,A10,A11,A12,A14", "overview,catalog,promotions,reports,accounts,changePassword")]
    public async Task AuthzR1RendersOnlyAllowedNavigationAndSearchAndDeniesDirectPageCreation(string role, string screens, string capabilities)
    {
        using var http = new HttpClient(new AccessResponses(role, capabilities)) { BaseAddress=new("http://test/") };
        var identity = new IdentityApiService(http);
        Assert.True((await identity.LoginAsync("synthetic", "synthetic")).Succeeded);
        await host.Run(app => {
            var shell = new ShellViewModel("synthetic", identity);
        Assert.False(shell.Overview.IsSampleData);
        Assert.True(shell.Overview.HasNoDashboardData);
        var expected = screens.Split(',');
            Assert.Equal(expected.Order(), shell.NavigationGroups.SelectMany(g => g.Screens).Select(s => s.Id).Order());
            Assert.All(shell.NavigationGroups, g => Assert.NotEmpty(g.Screens));
            Assert.Equal("A01", shell.SelectedScreen.Id); Assert.IsType<OverviewView>(shell.CurrentView);
            Assert.Equal(expected.Order(), shell.SearchResults.Select(s => s.Id).Order());
            foreach (var screen in NavigationRegistry.Groups.SelectMany(g => g.Screens).Where(s => !expected.Contains(s.Id))) {
                shell.SearchQuery = screen.Id; Assert.Empty(shell.SearchResults);
                var before = shell.CurrentView;
                shell.Navigate(screen.Id); Assert.Same(before, shell.CurrentView);
                Assert.Throws<UnauthorizedAccessException>(() => NavigationRegistry.CreatePage(screen.Id, shell));
            }
            // A denied navigation must not instantiate privileged ViewModels either.
            foreach (var field in typeof(ShellViewModel).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(Lazy<>)))
                Assert.False((bool)field.FieldType.GetProperty("IsValueCreated")!.GetValue(field.GetValue(shell))!);
            shell.SearchQuery = "";
            var window = new MainWindow(shell) { Width=1100, Height=700, Left=-20000, Top=-20000, ShowActivated=false };
            try {
                window.Show(); shell.ToggleNavigationCommand.Execute(null);
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
                Assert.Contains(VisualChildren<TextBlock>(window), t => t.IsVisible && t.Text == "Chưa có dữ liệu tổng quan");
                Assert.Contains(VisualChildren<TextBlock>(window), t => t.IsVisible && t.Text == "Kết nối hệ thống");
                Assert.DoesNotContain(VisualChildren<TextBlock>(window), t => t.IsVisible && (t.Text.Contains("API", StringComparison.OrdinalIgnoreCase) || t.Text.Contains("SQL", StringComparison.OrdinalIgnoreCase) || t.Text.Contains("Ví dụ", StringComparison.OrdinalIgnoreCase)));
                Assert.All(shell.Overview.Metrics, metric => Assert.Equal("—", metric.Value));
                foreach (var item in shell.NavigationGroups.SelectMany(g => g.Screens))
                    Assert.Contains(VisualChildren<TextBlock>(window), t => t.IsVisible && t.Text == item.Title);
                foreach (var denied in NavigationRegistry.Groups.SelectMany(g => g.Screens).Where(s => !expected.Contains(s.Id)))
                    Assert.DoesNotContain(VisualChildren<Button>(window), b => b.IsVisible && b.CommandParameter is NavigationItem n && n.Id == denied.Id);
            } finally { window.Close(); }
            identity.LogoutAsync().GetAwaiter().GetResult();
            Assert.False(shell.CanNavigate); Assert.Empty(shell.SearchResults);
            Assert.False(shell.CanAccess("A01"));
            var page = shell.CurrentView; shell.Navigate("A14"); Assert.Same(page, shell.CurrentView);
            identity.LoginAsync("second", "synthetic").GetAwaiter().GetResult();
            Assert.False(shell.CanNavigate); // An old shell cannot borrow the next session.
        });
    }

    [Fact]
    public async Task AuthzR1UsesCapabilitiesRatherThanGuessingTitleAndA12HasNoPermissionCheckboxes()
    {
        using var http = new HttpClient(new AccessResponses("Admin", "overview,changePassword,accounts")) { BaseAddress=new("http://test/") };
        var identity = new IdentityApiService(http); await identity.LoginAsync("synthetic", "synthetic");
        await host.Run(_ => {
            var shell = new ShellViewModel("synthetic", identity);
            Assert.False(shell.CanAccess("A04"));
            Assert.Equal("Nhân viên thử nghiệm", new EmployeeAccessRow(1, 1, "Nhân viên thử nghiệm", "synthetic", "Admin", "Active", "Active", []).ToString());
            var model = new AccountViewModel("synthetic", identity);
            model.LoadAsync().GetAwaiter().GetResult();
            model.Roles.Add(new EmployeeRoleOption("DisplayTest", "Chức danh kiểm thử", "Tổng quan"));
            model.SelectedRole = model.Roles[^1];
            var view = new AccountView { DataContext=model };
            using var source = new System.Windows.Interop.HwndSource(new System.Windows.Interop.HwndSourceParameters("A12 authorization regression") { Width=1000, Height=650, WindowStyle=0 }) { RootVisual=view };
            view.Measure(new Size(1000,650)); view.Arrange(new Rect(0,0,1000,650)); view.UpdateLayout();
            Assert.Empty(VisualChildren<CheckBox>(view));
            var picker = Assert.Single(VisualChildren<ComboBox>(view));
            Assert.Equal("Chức danh kiểm thử", picker.SelectedItem?.ToString());
            Assert.DoesNotContain(VisualChildren<TextBlock>(view), t => t.Text is "QuanLyTaiKhoan" or "GhiThanhToan" or "ThuKho");
        });
    }

    [Fact]
    public async Task AuthzR1EmptyCapabilitiesDoNotCrashOrConstructPages()
    {
        using var http = new HttpClient(new AccessResponses("Admin", "")) { BaseAddress=new("http://test/") };
        var identity = new IdentityApiService(http); await identity.LoginAsync("synthetic", "synthetic");
        await host.Run(_ => {
            var shell = new ShellViewModel("synthetic", identity);
            Assert.Empty(shell.NavigationGroups); Assert.Empty(shell.SearchResults);
            Assert.Equal("A01", shell.SelectedScreen.Id); Assert.IsType<string>(shell.CurrentView);
            Assert.False(shell.HasSecondaryMenu); Assert.False(shell.CanAccess("A12"));
            shell.Navigate("A12"); Assert.IsType<string>(shell.CurrentView);
        });
    }

    private sealed class AccessResponses(string role, string capabilities) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            object body = request.RequestUri!.AbsolutePath switch {
                "/api/v1/auth/login" => new { accessToken="synthetic", refreshToken="synthetic" },
                "/api/v1/auth/me" => new { accountType="employee", employee=new { title=role }, permissions=Array.Empty<string>(),
                    capabilities=capabilities.Split(',').ToDictionary(c => c, _ => true) },
                "/api/v1/auth/logout" => new { },
                "/api/v1/employee-access" => new EmployeeAccessResponse([new(2,2,"Nhân viên thử", "staff", "ThuKho", "Active", "Active", new byte[8])], [new("ThuKho", "Thủ kho", "Kho vắc xin · Nhập kho")]),
                _ => throw new InvalidOperationException(request.RequestUri.AbsolutePath)
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content=JsonContent.Create(body) });
        }
    }
}
