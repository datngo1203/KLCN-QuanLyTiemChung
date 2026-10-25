using System.Net;
using System.Text;
using System.Text.Json;
using TiemChung.Desktop.Services;
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

    private sealed class Handler : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];
        public bool RejectLogin { get; set; }
        public bool FailLogout { get; set; }
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
                "me" => Json(HttpStatusCode.OK, "{\"accountType\":\"employee\"}"),
                "change-password" or "logout" => new(HttpStatusCode.NoContent),
                _ => Json(HttpStatusCode.OK, "{\"accessToken\":\"access\",\"refreshToken\":\"refresh\"}")
            };
        }
        private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}
