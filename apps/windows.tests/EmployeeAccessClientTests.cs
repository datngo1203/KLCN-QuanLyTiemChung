using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class EmployeeAccessClientTests
{
    [Fact]
    public async Task CapabilitiesFailClosedAndNeverInferAccessFromTitleOrLegacyGrants()
    {
        var h = new Handler(); var identity = await Login(h);
        Assert.True(identity.CanManageAccounts);
        Assert.False(identity.CanWritePayments); Assert.False(identity.CanReceiveInventory);
        Assert.False(identity.CanUseClinical); Assert.False(identity.CanUseVaccinations);
        Assert.False(identity.CanUseRegistrations); Assert.False(identity.CanReadProfiles);
        await identity.LogoutAsync(); Assert.False(identity.CanManageAccounts);
        h.Caps.Clear(); await identity.LoginAsync("synthetic", "synthetic");
        Assert.False(identity.CanManageAccounts); Assert.False(identity.HasCapability("overview"));
    }
    [Fact]
    public async Task A12LoadsRealApiAndSavesOnlyOneRoleThenReloadsThePersistedChoice()
    {
        var h = new Handler(); var identity = await Login(h); var model = new AccountViewModel("synthetic", identity);
        await model.LoadAsync(); model.Selected=Assert.Single(model.Items);
        model.SelectedRole=model.Roles.Single(r=>r.Title=="ThuKho"); Assert.True(model.CanSave);
        await model.SaveAsync();
        Assert.Equal("ThuKho", Assert.Single(model.Items).Title);
        Assert.Equal(1,h.Writes); Assert.Contains("đăng nhập lại", model.Feedback);
        Assert.Equal(new[] {"title","expectedTitle","version"}, h.Body.EnumerateObject().Select(p=>p.Name));
        Assert.Equal("BacSi",h.Body.GetProperty("expectedTitle").GetString());
        await identity.LogoutAsync(); Assert.Empty(model.Items); Assert.Empty(model.Roles); Assert.False(model.CanSave);
    }
    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task A12RetainsDraftOnRejectedWriteAndCannotEditOwnRole(HttpStatusCode status)
    {
        var h=new Handler { WriteStatus=status }; var identity=await Login(h);var model=new AccountViewModel("synthetic",identity);
        await model.LoadAsync(); model.Selected=Assert.Single(model.Items);model.SelectedRole=model.Roles.Last();
        await model.SaveAsync(); Assert.Equal("BacSi",model.Selected!.Title);Assert.Equal("ThuKho",model.SelectedRole!.Title);
        Assert.Equal(1,h.Writes);
        var own=new AccountViewModel("staff",identity);await own.LoadAsync();own.Selected=Assert.Single(own.Items);own.SelectedRole=own.Roles.Last();
        Assert.False(own.CanSave);await own.SaveAsync();Assert.Equal(1,h.Writes);
    }
    [Fact]
    public async Task A12DiscardsResponsesFromAnEndedSession()
    {
        var h=new Handler();var identity=await Login(h);var model=new AccountViewModel("synthetic",identity);
        h.ReadGate=new(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending=model.LoadAsync();
        h.ReadGate.SetResult(new(HttpStatusCode.OK) { Content=JsonContent.Create(h.Data()) });
        await identity.LogoutAsync(); await pending;
        Assert.Empty(model.Items);Assert.False(model.CanManageAccounts);
    }
    private static async Task<IdentityApiService> Login(Handler handler)
    {
        var identity=new IdentityApiService(new HttpClient(handler){BaseAddress=new("http://test/")});
        Assert.True((await identity.LoginAsync("synthetic","synthetic")).Succeeded);return identity;
    }
    private sealed class Handler : HttpMessageHandler
    {
        public Dictionary<string,bool> Caps=new() { ["overview"]=true,["accounts"]=true };
        public HttpStatusCode WriteStatus=HttpStatusCode.NoContent;
        public int Writes; public JsonElement Body; public string Title="BacSi";
        public TaskCompletionSource<HttpResponseMessage>? ReadGate;
        public EmployeeAccessResponse Data()=>new([new(2,2,"Nhân viên thử","staff",Title,"Active","Active",new byte[8])],
            [new("BacSi","Bác sĩ","Khám sàng lọc"),new("ThuKho","Thủ kho","Kho vắc xin · Nhập kho")]);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            object body;
            switch(request.RequestUri!.AbsolutePath) {
                case "/api/v1/auth/login": body=new {accessToken="synthetic",refreshToken="synthetic"};break;
                case "/api/v1/auth/me": body=new {accountType="employee",employee=new {title="Admin"},permissions=new[]{"GhiThanhToan","NhapKho"},capabilities=Caps};break;
                case "/api/v1/auth/logout": return new(HttpStatusCode.NoContent);
                case "/api/v1/employee-access":
                    if(ReadGate is {} gate)return await gate.Task;
                    body=Data();break;
                case "/api/v1/employee-access/2/role":
                    Writes++;Body=await request.Content!.ReadFromJsonAsync<JsonElement>(ct);
                    if(WriteStatus==HttpStatusCode.NoContent)Title=Body.GetProperty("title").GetString()!;
                    return new(WriteStatus);
                default: throw new InvalidOperationException(request.RequestUri.AbsolutePath);
            }
            return new(HttpStatusCode.OK){Content=JsonContent.Create(body)};
        }
    }
}
