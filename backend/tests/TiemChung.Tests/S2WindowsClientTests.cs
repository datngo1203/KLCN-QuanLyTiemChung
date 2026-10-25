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
    private sealed class Handler : HttpMessageHandler
    {
        public int Refreshes { get; private set; }
        public int CatalogReads { get; private set; }
        public bool Forbidden { get; set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            var path=request.RequestUri!.AbsolutePath;
            object body; var status=HttpStatusCode.OK;
            if (path.EndsWith("/login")) body=new { accessToken="expired",refreshToken="refresh" };
            else if (path.EndsWith("/me")) body=new { accountType="employee" };
            else if (path.EndsWith("/refresh")) { Refreshes++; body=new { accessToken="fresh",refreshToken="rotated" }; }
            else {
                CatalogReads++;
                if (Forbidden) { status=HttpStatusCode.Forbidden; body=new { code="permissionDenied" }; }
                else if (request.Headers.Authorization?.Parameter=="expired") { status=HttpStatusCode.Unauthorized; body=new { code="invalidSession" }; }
                else { Assert.Equal("fresh",request.Headers.Authorization?.Parameter); body=new { source="database",items=Array.Empty<object>() }; }
            }
            return Task.FromResult(new HttpResponseMessage(status) { Content=JsonContent.Create(body) });
        }
    }
}
