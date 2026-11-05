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

    [Fact]
    public async Task DifferentDocumentCannotBeDisplayedAsTheSelectedReceipt()
    {
        var fake=new Fake { Detail=Saved with { Received=Request with { DocumentNumber="WRONG DOCUMENT" } } };
        var vm=new InventoryReceiptHistoryViewModel(await Login(new()),fake);
        await vm.LoadAsync();vm.Selected=Assert.Single(vm.Rows);await vm.ReadDetailAsync();
        Assert.DoesNotContain("WRONG DOCUMENT",vm.DetailLabel);
        Assert.Contains("thử lại",vm.Feedback);
    }

    [Fact]
    public async Task EditingFilterDisablesPagingUntilSearchIsApplied()
    {
        var vm=new InventoryReceiptHistoryViewModel(await Login(new()),new Fake());
        await vm.LoadAsync(2);Assert.True(vm.PreviousCommand.CanExecute(null));
        vm.SearchText="new filter";
        Assert.False(vm.PreviousCommand.CanExecute(null));Assert.False(vm.NextCommand.CanExecute(null));
    }

    [Fact]
    public async Task LoadingPreventsDuplicateRequestAndHasMoreControlsNextExactly()
    {
        var fake=new Fake { ListGate=new() };var vm=new InventoryReceiptHistoryViewModel(await Login(new()),fake);
        var load=vm.LoadAsync();Assert.True(vm.IsBusy);Assert.False(vm.NoRows);Assert.False(vm.FindCommand.CanExecute(null));
        await vm.LoadAsync();Assert.Equal(1,fake.ListCount);fake.ListGate.SetResult(new([Row],true));await load;
        Assert.True(vm.NextCommand.CanExecute(null));Assert.False(vm.PreviousCommand.CanExecute(null));
        fake.ListGate=null;fake.Next=new([Row],false);await vm.LoadAsync(2);
        Assert.False(vm.NextCommand.CanExecute(null));Assert.True(vm.PreviousCommand.CanExecute(null));
    }

    [Fact]
    public async Task LateDetailCannotReturnIntoAnotherWarehouseSession()
    {
        var handler=new Handler();var identity=await Login(handler);var fake=new Fake();var vm=new InventoryReceiptHistoryViewModel(identity,fake);
        await vm.LoadAsync();vm.Selected=Assert.Single(vm.Rows);fake.GetGate=new();var detail=vm.ReadDetailAsync();
        Assert.False(vm.DetailCommand.CanExecute(null));await identity.LogoutAsync();await identity.LoginAsync("next-warehouse","synthetic-password");
        fake.GetGate.SetResult(Saved);await detail;Assert.Empty(vm.Rows);Assert.Null(vm.Selected);Assert.False(vm.HasAccess);Assert.DoesNotContain(Request.DocumentNumber,vm.DetailLabel);
    }

    [Theory]
    [InlineData("invalidSession")][InlineData("permissionDenied")]
    public async Task DeniedDetailClearsPreviouslyLoadedListSelectionAndDocument(string code)
    {
        var fake=new Fake();var vm=new InventoryReceiptHistoryViewModel(await Login(new()),fake);
        await vm.LoadAsync();vm.Selected=Assert.Single(vm.Rows);await vm.ReadDetailAsync();fake.GetError=new InventoryApiException(code,"Denied");await vm.ReadDetailAsync();
        Assert.Empty(vm.Rows);Assert.Null(vm.Selected);Assert.True(vm.NoAccess);Assert.False(vm.NoRows);Assert.DoesNotContain(Request.DocumentNumber,vm.DetailLabel);
    }

    [Fact]
    public async Task NetworkErrorsAreNotEmptySuccessAndRetryKeepsFilterWithoutStaleDocument()
    {
        var fake=new Fake();var vm=new InventoryReceiptHistoryViewModel(await Login(new()),fake){SearchText="Supplier"};
        await vm.LoadAsync();vm.Selected=Assert.Single(vm.Rows);await vm.ReadDetailAsync();fake.ListError=new HttpRequestException();await vm.LoadAsync();
        Assert.Empty(vm.Rows);Assert.False(vm.NoRows);Assert.Contains("thử lại",vm.Feedback);Assert.Equal("Supplier",vm.SearchText);Assert.DoesNotContain(Request.DocumentNumber,vm.DetailLabel);
        fake.ListError=null;await vm.LoadAsync();vm.Selected=Assert.Single(vm.Rows);fake.GetError=new TaskCanceledException();await vm.ReadDetailAsync();
        Assert.NotNull(vm.Selected);Assert.DoesNotContain(Request.DocumentNumber,vm.DetailLabel);fake.GetError=null;await vm.ReadDetailAsync();Assert.Contains(Request.DocumentNumber,vm.DetailLabel);
    }

    [Theory]
    [InlineData("listNull")][InlineData("listMissingSource")][InlineData("listNonUtc")]
    [InlineData("detailWrongLot")][InlineData("detailBadSource")]
    public async Task MissingOrMismatchedApiDataIsAnErrorRatherThanAReceipt(string failure)
    {
        var service=new InventoryReceiptHistoryApiService(await Login(new Handler{Failure=failure}));
        if(failure.StartsWith("list"))await Assert.ThrowsAsync<System.Text.Json.JsonException>(()=>service.HistoryAsync("",1));
        else await Assert.ThrowsAsync<System.Text.Json.JsonException>(()=>service.DetailAsync(Request.LotNumber));
    }

    private sealed class Fake : IInventoryReceiptHistoryService
    {
        public InventoryReceiptDto Detail=Saved;
        public Exception? ListError,GetError;
        public TaskCompletionSource<InventoryReceiptDto>? GetGate;
        public int ListCount;
        public TaskCompletionSource<InventoryReceiptHistoryListResponse>? ListGate;
        public InventoryReceiptHistoryListResponse? Next;
        public int GetCount;
        public string LastSearch="";
        public Task<InventoryReceiptHistoryListResponse> HistoryAsync(string search,int page)
        {
            LastSearch=search.Trim();
            ListCount++;
            return ListError is {} e?Task.FromException<InventoryReceiptHistoryListResponse>(e):ListGate?.Task??Task.FromResult(Next??new InventoryReceiptHistoryListResponse([Row],false));
        }
        public Task<InventoryReceiptDto> DetailAsync(string lot)
        {
            GetCount++;
            return GetError is {} e?Task.FromException<InventoryReceiptDto>(e):GetGate?.Task??Task.FromResult(Detail);
        }
    }

    private sealed class Handler : HttpMessageHandler
    {
        public string Title="ThuKho",Failure="";
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
                permissions=Receive?new[]{"XemKho","NhapKho"}:new[]{"XemKho"},capabilities=new { inventoryRead=Title=="ThuKho",inventoryReceive=Title=="ThuKho" && Receive }}));
            Paths.Add(path);
            if(Status!=HttpStatusCode.OK)return Task.FromResult(Json(new{detail="PRIVATE SQL"},Status));
            if(Failure=="listNull")return Task.FromResult(Json(new{items=(object?)null,hasMore=false}));
            if(Failure=="listMissingSource")return Task.FromResult(Json(new InventoryReceiptHistoryListResponse([Row with{DocumentNumber=""}],false)));
            if(Failure=="listNonUtc")return Task.FromResult(Json(new InventoryReceiptHistoryListResponse([Row with{RecordedAt=Row.RecordedAt.ToOffset(TimeSpan.FromHours(7))}],false)));
            if(Failure=="detailWrongLot")return Task.FromResult(Json(Saved with{Received=Request with{LotNumber="Other"}}));
            if(Failure=="detailBadSource")return Task.FromResult(Json(Saved with{Received=Request with{Quantity=0}}));
            return Task.FromResult(path.Contains("/history") ?
                Json(new InventoryReceiptHistoryListResponse([Row],false)):Json(Saved));
        }
        private static HttpResponseMessage Json(object value,HttpStatusCode status=HttpStatusCode.OK)=>
            new(status){Content=JsonContent.Create(value)};
    }
}
