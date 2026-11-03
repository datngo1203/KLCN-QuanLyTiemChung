using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using TiemChung.Contracts;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class InventoryReceiptHistoryClientTests
{
    private static readonly ReceiveInventoryRequest Request = new("[DEMO] / +%?#",1,
        new DateOnly(2026,1,1),new DateOnly(2030,1,1),20,
        "[DEMO] Supplier","[DEMO] DOC","[DEMO] Room",true);
    private static readonly InventoryReceiptDto Saved = new(Request,"[DEMO] Vaccine",17,6,"[DEMO] Warehouse",DateTimeOffset.UtcNow);
    private static InventoryReceiptHistoryItemDto Row => new(Request.LotNumber,Saved.VaccineName,
        Request.Quantity,Request.SupplierName,Request.DocumentNumber,Request.StorageLocation,
        Saved.MovementId,Saved.EmployeeId,Saved.EmployeeName,Saved.RecordedAt);
    private static async Task<IdentityApiService> Login(Handler handler)
    {
        var identity=new IdentityApiService(new HttpClient(handler){BaseAddress=new("http://test/")});
        Assert.True((await identity.LoginAsync("warehouse","synthetic-password")).Succeeded);
        return identity;
    }
    [Theory]
    [InlineData("ThuKho",true,true)]
    [InlineData("ThuKho",false,false)]
    [InlineData("Admin",true,false)]
    [InlineData("LeTan",true,false)]
    public async Task HistoryRequiresBothWarehouseGrantsAndRole(string title,bool receive,bool allowed)
    {
        var identity=await Login(new Handler{Title=title,Receive=receive});
        var vm=new InventoryReceiptHistoryViewModel(identity,new Fake());
        Assert.Equal(allowed,vm.HasAccess);
        await vm.LoadAsync();
        Assert.Equal(allowed,vm.Rows.Count==1);
    }

    [Fact]
    public async Task HistoryViewSearchDetailsAndSessionClearUseReadOnlyService()
    {
        var h=new Handler();var identity=await Login(h);var fake=new Fake();
        var vm=new InventoryReceiptHistoryViewModel(identity,fake);
        vm.SearchText=" Supplier ";
        await vm.LoadAsync();
        Assert.True(vm.HasAccess);Assert.Single(vm.Rows);
        Assert.Equal("Supplier",fake.LastSearch);
        Assert.Equal(1,vm.Page);
        vm.Selected=Assert.Single(vm.Rows);
        await vm.ReadDetailAsync();
        Assert.Contains("PHIẾU TIẾP NHẬN",vm.DetailLabel);
        Assert.Contains(Request.DocumentNumber,vm.DetailLabel);
        Assert.Equal(1,fake.GetCount);
        await identity.LogoutAsync();
        Assert.Empty(vm.Rows);Assert.Null(vm.Selected);
        Assert.False(vm.HasAccess);Assert.DoesNotContain(Request.DocumentNumber,vm.DetailLabel);
    }

    [Fact]
    public async Task LateHistoryResultCannotRepopulateAfterIdentityChanges()
    {
        var h=new Handler();var identity=await Login(h);
        var fake=new Fake { ListGate=new() };
        var vm=new InventoryReceiptHistoryViewModel(identity,fake);
        var pending=vm.LoadAsync();Assert.True(vm.IsBusy);
        await identity.LogoutAsync();h.Title="Admin";
        Assert.True((await identity.LoginAsync("other","synthetic-password")).Succeeded);
        fake.ListGate.SetResult(new([Row],false));await pending;
        Assert.Empty(vm.Rows);Assert.False(vm.HasAccess);
    }

    [Fact]
    public async Task PaginationAndValidationKeepReadOnlyDraftAndClearStaleDetails()
    {
        var identity=await Login(new Handler());var fake=new Fake();
        var vm=new InventoryReceiptHistoryViewModel(identity,fake);
        await vm.LoadAsync();vm.Selected=Assert.Single(vm.Rows);await vm.ReadDetailAsync();
        Assert.Contains(Request.DocumentNumber,vm.DetailLabel);
        fake.Next=new([],false);
        await vm.LoadAsync(2);
        Assert.Empty(vm.Rows);Assert.True(vm.NoRows);Assert.Equal(2,vm.Page);
        Assert.DoesNotContain(Request.DocumentNumber,vm.DetailLabel);
        vm.SearchText=new string('a',101);
        await vm.LoadAsync();
        Assert.Contains("100",vm.Feedback);Assert.Equal(2,vm.Page);
    }

    [Fact]
    public async Task ServiceUrlsAreEncodedAndPinnedToOriginalSession()
    {
        var h=new Handler();var identity=await Login(h);
        var service=new InventoryReceiptHistoryApiService(identity);
        var list=await service.HistoryAsync(" [DEMO] / +%?# ",2);
        Assert.Single(list.Items);
        Assert.Contains("search=%5BDEMO%5D%20%2F%20%2B%25%3F%23&page=2",h.Paths.Last());
        Assert.Equal(Saved,await service.DetailAsync(Request.LotNumber));
        Assert.Contains("lotNumber=%5BDEMO%5D%20%2F%20%2B%25%3F%23",h.Paths.Last());
        await identity.LogoutAsync();h.Title="Admin";
        await identity.LoginAsync("other","synthetic-password");
        var count=h.Paths.Count;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>service.HistoryAsync("test",1));
        Assert.Equal(count,h.Paths.Count);
    }

    [Theory]
    [InlineData(400,"validation")]
    [InlineData(401,"invalidSession")]
    [InlineData(403,"permissionDenied")]
    [InlineData(503,"requestFailed")]
    public async Task ErrorResponsesDoNotExposeInternalServerData(int code,string expected)
    {
        var h=new Handler{Status=(HttpStatusCode)code};var service=new InventoryReceiptHistoryApiService(await Login(h));
        var error=await Assert.ThrowsAsync<InventoryApiException>(()=>service.HistoryAsync("",1));
        Assert.Equal(expected,error.Code);
        Assert.DoesNotContain("PRIVATE",error.Message);
    }

    private sealed class Fake : IInventoryReceiptHistoryService
    {
        public TaskCompletionSource<InventoryReceiptHistoryListResponse>? ListGate;
        public InventoryReceiptHistoryListResponse? Next;
        public int GetCount;
        public string LastSearch="";
        public Task<InventoryReceiptHistoryListResponse> HistoryAsync(string search,int page)
        {
            LastSearch=search.Trim();
            return ListGate?.Task??Task.FromResult(Next??new InventoryReceiptHistoryListResponse([Row],false));
        }
        public Task<InventoryReceiptDto> DetailAsync(string lot)
        {
            GetCount++;
            return Task.FromResult(Saved);
        }
    }

    private sealed class Handler : HttpMessageHandler
    {
        public string Title="ThuKho";
        public bool Receive=true;
        public HttpStatusCode Status=HttpStatusCode.OK;
        public List<string> Paths=[];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            var path=request.RequestUri!.PathAndQuery;
            if(path.EndsWith("/login")||path.EndsWith("/refresh"))
                return Task.FromResult(Json(new{accessToken="token",refreshToken="refresh"}));
            if(path.EndsWith("/logout"))return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            if(path.EndsWith("/me"))return Task.FromResult(Json(new{
                accountType="employee",employee=new{title=Title},
                permissions=Receive?new[]{"XemKho","NhapKho"}:new[]{"XemKho"}}));
            Paths.Add(path);
            if(Status!=HttpStatusCode.OK)return Task.FromResult(Json(new{detail="PRIVATE SQL"},Status));
            return Task.FromResult(path.Contains("/history") ?
                Json(new InventoryReceiptHistoryListResponse([Row],false)):Json(Saved));
        }
        private static HttpResponseMessage Json(object value,HttpStatusCode status=HttpStatusCode.OK)=>
            new(status){Content=JsonContent.Create(value)};
    }
}
