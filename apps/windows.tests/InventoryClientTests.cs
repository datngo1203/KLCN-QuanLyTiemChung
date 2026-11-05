using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TiemChung.Contracts;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class InventoryClientTests
{
    private static readonly InventoryBatchDto Lot = new("[DEMO] / +%?#",1,"[DEMO] Vaccine",new(2020,1,1),new(2030,1,1),100,98,"Active");
    private static readonly InventoryBatchListResponse Lots = new([Lot],true,new(2026,11,3));
    private static readonly InventoryMovementListResponse Movements = new([new(1,Lot.LotNumber,"XuatDoTiem",1,DateTimeOffset.UtcNow,2,"[DEMO] Nurse")],false);
    private static async Task<IdentityApiService> Login(Handler h)
    {
        var identity = new IdentityApiService(new HttpClient(h) { BaseAddress = new("http://test/") });
        Assert.True((await identity.LoginAsync("synthetic","synthetic-password")).Succeeded); return identity;
    }
    [Theory]
    [InlineData("ThuKho",true,true)][InlineData("ThuKho",false,false)][InlineData("Admin",true,false)]
    [InlineData("LeTan",true,false)][InlineData("DieuDuong",true,false)][InlineData("BacSi",true,false)][InlineData("ThuNgan",true,false)]
    public async Task RequiresWarehouseAndGrantBeforeLoading(string title,bool grant,bool allowed)
    {
        var h = new Handler { Title=title,Grant=grant };var vm = new InventoryViewModel(identity:await Login(h));
        await vm.LoadAsync(); Assert.Equal(allowed,vm.HasAccess); Assert.Equal(allowed?1:0,h.Paths.Count);
    }
    [Fact]
    public async Task UsesOnlyGetAndEscapesLotAndSearchWithRealRoutePaging()
    {
        var h = new Handler();var service = new InventoryApiService(await Login(h));
        Assert.Equal(Lot,(await service.ListAsync(2," x/+ ")).Items.Single());
        Assert.Equal("/api/v1/inventory/lots?page=2&pageSize=25&search=x%2F%2B",h.Paths.Last());
        Assert.Single((await service.MovementsAsync(Lot.LotNumber,3)).Items);
        Assert.Contains("lotNumber=%5BDEMO%5D%20%2F%20%2B%25%3F%23&page=3&pageSize=25",h.Paths.Last());
        Assert.All(h.Methods,m=>Assert.Equal(HttpMethod.Get,m));
    }
    [Fact]
    public async Task LoadingDisablesRequestsSelectionAndEmptyStateThenPaginationAndSelectionReset()
    {
        var fake = new Fake { ListGate=new() };var vm = new InventoryViewModel(fake,await Login(new()));
        var pending = vm.LoadAsync(); Assert.True(vm.IsBusy);Assert.False(vm.NoBatches);Assert.False(vm.ReloadCommand.CanExecute(null));
        await vm.LoadAsync(); Assert.Equal(1,fake.ListCalls);
        fake.ListGate.SetResult(Lots);await pending;fake.ListGate=null;
        Assert.True(vm.NextCommand.CanExecute(null));Assert.False(vm.PreviousCommand.CanExecute(null));
        vm.Selected=vm.Batches.Single();await vm.LoadMovementsAsync();Assert.Single(vm.Movements);
        vm.Selected=null;Assert.Empty(vm.Movements);Assert.False(vm.MovementsCommand.CanExecute(null));
        fake.ListResult=new([],false,Lots.FacilityDate);await vm.LoadAsync(2);
        Assert.True(vm.NoBatches);Assert.Equal(2,vm.Page);Assert.True(vm.PreviousCommand.CanExecute(null));Assert.False(vm.NextCommand.CanExecute(null));
        Assert.Empty(vm.Movements);Assert.Null(vm.Selected);
    }
    [Fact]
    public async Task ErrorsAreNotEmptySuccessAndGetCanRetryWithoutSampleFallback()
    {
        var fake = new Fake { ListError=new HttpRequestException() };var vm=new InventoryViewModel(fake,await Login(new()));
        await vm.LoadAsync();Assert.Empty(vm.Batches);Assert.False(vm.NoBatches);Assert.Contains("thử lại",vm.Feedback);Assert.True(vm.CanBrowse);
        fake.ListError=null;await vm.LoadAsync();vm.Selected=vm.Batches.Single();fake.MovementError=new TaskCanceledException();
        await vm.LoadMovementsAsync();Assert.Empty(vm.Movements);Assert.False(vm.NoMovements);Assert.Contains("thử lại",vm.MovementFeedback);
        Assert.NotNull(vm.Selected);fake.MovementError=null;fake.MovementResult=new([],false);await vm.LoadMovementsAsync();Assert.True(vm.NoMovements);
    }
    [Theory]
    [InlineData("invalidSession")][InlineData("permissionDenied")]
    public async Task DenialOnMovementClearsAllPreviouslyVisibleData(string code)
    {
        var fake=new Fake();var vm=new InventoryViewModel(fake,await Login(new()));await vm.LoadAsync();vm.Selected=vm.Batches.Single();
        fake.MovementError=new InventoryApiException(code,"Denied");await vm.LoadMovementsAsync();
        Assert.Empty(vm.Batches);Assert.Empty(vm.Movements);Assert.Null(vm.Selected);Assert.True(vm.NoAccess);Assert.False(vm.ReloadCommand.CanExecute(null));
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task SessionChangeClearsDataAndDiscardsLateListOrMovements(bool movements)
    {
        var h=new Handler();var identity=await Login(h);var fake=new Fake();var vm=new InventoryViewModel(fake,identity);
        await vm.LoadAsync();vm.Selected=vm.Batches.Single();Task pending;
        if(movements){fake.MovementGate=new();pending=vm.LoadMovementsAsync();}else{fake.ListGate=new();pending=vm.LoadAsync();}
        await identity.LogoutAsync();h.Title="Admin";Assert.True((await identity.LoginAsync("other","synthetic-password")).Succeeded);
        fake.ListGate?.SetResult(Lots);fake.MovementGate?.SetResult(Movements);await pending;
        Assert.Empty(vm.Batches);Assert.Empty(vm.Movements);Assert.Null(vm.Selected);Assert.False(vm.HasAccess);
    }
    [Theory]
    [InlineData(400,"validation")][InlineData(401,"invalidSession")][InlineData(403,"permissionDenied")]
    [InlineData(404,"resourceNotFound")][InlineData(503,"requestFailed")]
    public async Task HttpErrorsUseSafeMessageAndNeverExposeServerDetails(int status,string code)
    {
        var h=new Handler { Status=(HttpStatusCode)status };var service=new InventoryApiService(await Login(h));
        var error=await Assert.ThrowsAsync<InventoryApiException>(()=>service.ListAsync(1,""));
        Assert.Equal(code,error.Code);Assert.DoesNotContain("PRIVATE",error.Message);
    }
    [Fact]
    public async Task MalformedBodyAndOldServiceCannotShowDataOrUseNewSession()
    {
        var h=new Handler { Malformed=true };var identity=await Login(h);var service=new InventoryApiService(identity);
        await Assert.ThrowsAsync<JsonException>(()=>service.ListAsync(1,""));
        await identity.LogoutAsync();await identity.LoginAsync("next","synthetic-password");
        var count=h.Paths.Count;await Assert.ThrowsAsync<OperationCanceledException>(()=>service.ListAsync(1,""));Assert.Equal(count,h.Paths.Count);
    }
    [Fact]
    public void AvailabilityUsesFacilityDateAndStillShowsSqlStatusSeparately()
    {
        Assert.Equal("Hết hạn",new InventoryBatchRow(Lot with { ExpiresOn=new(2026,11,2) },Lots.FacilityDate).Availability);
        Assert.Equal("Còn tồn",new InventoryBatchRow(Lot with { ExpiresOn=Lots.FacilityDate,Status="Inactive" },Lots.FacilityDate).Availability);
        Assert.Equal("Hết tồn",new InventoryBatchRow(Lot with { Stock=0 },Lots.FacilityDate).Availability);
    }
    private sealed class Fake : IInventoryService
    {
        public int ListCalls;
        public Exception? ListError,MovementError;
        public InventoryBatchListResponse ListResult=Lots;
        public InventoryMovementListResponse MovementResult=Movements;
        public TaskCompletionSource<InventoryBatchListResponse>? ListGate;
        public TaskCompletionSource<InventoryMovementListResponse>? MovementGate;
        public Task<InventoryBatchListResponse> ListAsync(int page,string search){ListCalls++;return ListError is {} e?Task.FromException<InventoryBatchListResponse>(e):ListGate?.Task??Task.FromResult(ListResult);}
        public Task<InventoryMovementListResponse> MovementsAsync(string lot,int page)=>MovementError is {} e?Task.FromException<InventoryMovementListResponse>(e):MovementGate?.Task??Task.FromResult(MovementResult);
    }
    private sealed class Handler : HttpMessageHandler
    {
        public string Title="ThuKho";public bool Grant=true,Malformed;
        public HttpStatusCode Status=HttpStatusCode.OK;
        public List<string> Paths=[];public List<HttpMethod> Methods=[];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req,CancellationToken ct)
        {
            var path=req.RequestUri!.PathAndQuery;object value;
            if(path.EndsWith("/login")||path.EndsWith("/refresh"))value=new { accessToken="access",refreshToken="refresh" };
            else if(path.EndsWith("/logout"))return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            else if(path.EndsWith("/me"))value=new { accountType="employee",employee=new { title=Title },permissions=Grant?new[]{"XemKho"}:[], capabilities=new { inventoryRead=Title=="ThuKho" && Grant } };
            else {
                Paths.Add(path);Methods.Add(req.Method);
                value=Status!=HttpStatusCode.OK?new { detail="PRIVATE SQL" }:Malformed?new { }:path.Contains("/lots?")?Lots:Movements;
                return Task.FromResult(new HttpResponseMessage(Status){Content=JsonContent.Create(value)});
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(value)});
        }
    }
}
