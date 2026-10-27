using System.Net;
using System.Net.Http.Json;
using TiemChung.Desktop.Services;
using Xunit;

namespace TiemChung.Tests;

public sealed class S2WindowsClientTests
{
    [Fact]
    public async Task ExpiredCatalogAccessRefreshesOnceAndForbiddenDoesNotRefresh()
    {
        var handler=new Handler();
        var identity=new IdentityApiService(new HttpClient(handler) { BaseAddress=new Uri("http://localhost/") });
        Assert.True((await identity.LoginAsync("staff","input")).Succeeded);
        var catalog=new CatalogApiService(identity);
        Assert.Empty(await catalog.ListAsync<CatalogType>("injection-types"));
        Assert.Equal(1,handler.Refreshes); Assert.Equal(2,handler.CatalogReads);
        handler.Forbidden=true;
        var error=await Assert.ThrowsAsync<HttpRequestException>(()=>catalog.SaveAsync("injection-types",null,new { name="Test",description="" }));
        Assert.Contains("không có quyền",error.Message); Assert.Equal(1,handler.Refreshes);
    }
    [Fact]
    public async Task LogoutWaitsForInFlightRefreshAndRevokesRotatedCredential()
    {
        var handler = new Handler { PauseRefresh = true };
        var identity = new IdentityApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
        await identity.LoginAsync("staff", "input");
        var load = new CatalogApiService(identity).ListAsync<CatalogType>("injection-types");
        await handler.RefreshStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var logout = identity.LogoutAsync();
        Assert.False(logout.IsCompleted);
        handler.ReleaseRefresh.SetResult();
        await load; await logout;
        Assert.Equal("rotated", handler.RevokedToken);
        Assert.False(identity.IsSignedIn); Assert.Empty(identity.Permissions);
    }
    private sealed class Handler : HttpMessageHandler
    {
        public int Refreshes { get; private set; }
        public int CatalogReads { get; private set; }
        public bool Forbidden { get; set; }
        public bool PauseRefresh { get; set; }
        public TaskCompletionSource RefreshStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseRefresh { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string? RevokedToken { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            var path=request.RequestUri!.AbsolutePath;
            object body; var status=HttpStatusCode.OK;
            if (path.EndsWith("/login")) body=new { accessToken="expired",refreshToken="refresh" };
            else if (path.EndsWith("/me")) body=new { accountType="employee" };
            else if (path.EndsWith("/refresh")) {
                Refreshes++; RefreshStarted.TrySetResult();
                if (PauseRefresh) await ReleaseRefresh.Task.WaitAsync(cancellationToken);
                body=new { accessToken="fresh",refreshToken="rotated" };
            }
            else if (path.EndsWith("/logout")) {
                RevokedToken = (await request.Content!.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken)).GetProperty("refreshToken").GetString();
                return new(HttpStatusCode.NoContent);
            }
            else {
                CatalogReads++;
                if (Forbidden) { status=HttpStatusCode.Forbidden; body=new { code="permissionDenied" }; }
                else if (request.Headers.Authorization?.Parameter=="expired") { status=HttpStatusCode.Unauthorized; body=new { code="invalidSession" }; }
                else { Assert.Equal("fresh",request.Headers.Authorization?.Parameter); body=new { source="database",items=Array.Empty<object>() }; }
            }
            return new HttpResponseMessage(status) { Content=JsonContent.Create(body) };
        }
    }
}
