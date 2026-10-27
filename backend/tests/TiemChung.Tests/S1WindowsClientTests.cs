using System.Net;
using System.Text;
using System.Text.Json;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Tests;

public sealed class S1WindowsClientTests
{
    [Fact]
    public async Task LoginChangePasswordAndLogoutUseApiAndClearSession()
    {
        var handler = new Handler();
        var service = new IdentityApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
        var changed = false; service.PasswordChanged += (_, _) => changed = true;
        Assert.False(service.IsSignedIn);
        Assert.True((await service.LoginAsync("staff", "input")).Succeeded);
        Assert.True(service.IsSignedIn);
        Assert.True((await service.ChangePasswordAsync("input", "new-password-input")).Succeeded);
        Assert.True(changed); Assert.False(service.IsSignedIn);
        Assert.Equal(new[] { "login", "me", "refresh", "change-password" }, handler.Paths);
        Assert.True((await service.LoginAsync("staff", "input")).Succeeded);
        await service.LogoutAsync(); Assert.False(service.IsSignedIn);
        Assert.Equal("logout", handler.Paths.Last());
    }

    [Fact]
    public async Task InvalidCredentialsAreRejectedAndLogoutFailureCanBeRetried()
    {
        var handler = new Handler { RejectLogin = true };
        var service = new IdentityApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
        Assert.False((await service.LoginAsync("staff", "wrong")).Succeeded);
        Assert.False(service.IsSignedIn);
        handler.RejectLogin = false;
        Assert.True((await service.LoginAsync("staff", "input")).Succeeded);
        handler.FailLogout = true;
        await Assert.ThrowsAsync<HttpRequestException>(() => service.LogoutAsync());
        Assert.True(service.IsSignedIn);
        handler.FailLogout = false;
        await service.LogoutAsync(); Assert.False(service.IsSignedIn);
    }

    [Fact]
    public async Task AccountSwitchClearsCapabilitiesAndDisablesUnauthorizedCommands()
    {
        var handler = new Handler { CanWriteProfiles = true };
        var service = new IdentityApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
        Assert.True((await service.LoginAsync("reception", "input")).Succeeded);
        Assert.True(service.CanReadProfiles); Assert.True(service.CanWriteProfiles);
        var old = new ProfilesViewModel(canWrite: service.CanWriteProfiles) { Search = "old search", Name = "old name" };
        old.Customers.Add(new(1, "old customer", null, "0900000000", "old@example.invalid"));
        await service.LogoutAsync();
        Assert.False(service.CanReadProfiles); Assert.False(service.CanWriteProfiles); Assert.Empty(service.Permissions);
        handler.CanWriteProfiles = false;
        Assert.True((await service.LoginAsync("admin", "input")).Succeeded);
        Assert.False(service.CanWriteProfiles);
        var current = new ProfilesViewModel(canWrite: service.CanWriteProfiles) {
            Name = "valid", Phone = "0900000000", Email = "valid@example.invalid", SelectedCustomer = old.Customers[0]
        };
        Assert.Empty(current.Customers); Assert.Empty(current.Recipients); Assert.Equal("", current.Search);
        Assert.False(current.LuuKhachHangCommand.CanExecute(null));
        Assert.False(current.DeleteCustomerCommand.CanExecute(null));
        Assert.False(current.TaoHoSoMoiCommand.CanExecute(null));
        Assert.False(current.NewRecipientCommand.CanExecute(null));
        var catalog = new CatalogViewModel(permissions: service.Permissions) { Name = "valid" };
        Assert.False(catalog.SaveCommand.CanExecute(null));
        Assert.False(catalog.DeleteCommand.CanExecute(null));
        Assert.False(catalog.NewCommand.CanExecute(null));
        await service.LogoutAsync();
        handler.RejectLogin = true;
        Assert.False((await service.LoginAsync("wrong", "wrong")).Succeeded);
        Assert.False(service.CanReadProfiles); Assert.Empty(service.Permissions);
    }

    [Fact]
    public async Task CatalogPriceRequiresBothGrantsAndLookupNeedsOnlyProfessionalGrant()
    {
        var handler = new Handler { Permissions = ["QuanLyDanhMucChuyenMon"] };
        var identity = new IdentityApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
        await identity.LoginAsync("staff", "input");
        var vm = new CatalogViewModel(new CatalogApiService(identity), identity.Permissions) { Name = "valid" };
        Assert.False(vm.CanWrite); Assert.False(vm.SaveCommand.CanExecute(null));
        vm.SelectTabCommand.Execute("Loại tiêm chủng");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!vm.CanInteract) await Task.Delay(10, timeout.Token);
        vm.Name = "valid";
        Assert.True(vm.CanWrite); Assert.True(vm.SaveCommand.CanExecute(null));
        var full = new CatalogViewModel(permissions: ["QuanLyDanhMucChuyenMon", "QuanLyDanhMucGia"]) { Name = "valid" };
        Assert.True(full.CanWrite); Assert.True(full.SaveCommand.CanExecute(null));
    }

    private sealed class Handler : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];
        public bool RejectLogin { get; set; }
        public bool FailLogout { get; set; }
        public bool CanWriteProfiles { get; set; }
        public string[] Permissions { get; set; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.Segments.Last(); Paths.Add(path);
            if (path == "logout" && FailLogout) throw new HttpRequestException("offline");
            if (path == "login" && RejectLogin) return Json(HttpStatusCode.Unauthorized, "{\"code\":\"invalidCredentials\"}");
            if (path is "login" or "refresh" or "logout") {
                using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
                Assert.Equal("native", body.RootElement.GetProperty("clientType").GetString());
                if (path != "login") Assert.Equal("refresh", body.RootElement.GetProperty("refreshToken").GetString());
            }
            if (path is "me" or "change-password") Assert.Equal("Bearer access", request.Headers.Authorization!.ToString());
            return path switch {
                "me" => Json(HttpStatusCode.OK, JsonSerializer.Serialize(new { accountType = "employee", permissions = Permissions,
                    capabilities = new { profilesRead = true, profilesCreate = CanWriteProfiles, profilesUpdate = CanWriteProfiles } })),
                "change-password" or "logout" => new(HttpStatusCode.NoContent),
                _ => Json(HttpStatusCode.OK, "{\"accessToken\":\"access\",\"refreshToken\":\"refresh\"}")
            };
        }
        private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}
