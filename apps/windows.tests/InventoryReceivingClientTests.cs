using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TiemChung.Contracts;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class InventoryReceivingClientTests
{
    private static readonly ReceiveInventoryRequest Request=new("[DEMO] / +%?#",1,new(2026,1,1),new(2030,1,1),100,"[DEMO] Supplier","[DEMO] DOC","[DEMO] Room",true);
    private static readonly InventoryReceiptDto Saved=new(Request,"[DEMO] Vaccine",2,3,"[DEMO] Warehouse",DateTimeOffset.UtcNow);
    private static async Task<IdentityApiService> Login(Handler h){var identity=new IdentityApiService(new HttpClient(h){BaseAddress=new("http://test/")});Assert.True((await identity.LoginAsync("synthetic","synthetic-password")).Succeeded);return identity;}
    private static void Fill(InventoryReceivingViewModel vm){vm.Vaccine=new(1,Saved.VaccineName);vm.LotNumber=" "+Request.LotNumber+" ";vm.Quantity="100";vm.ManufacturedOn=new(2026,1,1);vm.ExpiresOn=new(2030,1,1);vm.SupplierName=Request.SupplierName;vm.DocumentNumber=Request.DocumentNumber;vm.StorageLocation=Request.StorageLocation;vm.InspectionConfirmed=true;}
    [Theory]
    [InlineData("ThuKho",true,true)][InlineData("ThuKho",false,false)][InlineData("Admin",true,false)][InlineData("DieuDuong",true,false)]
    public async Task SeparateWritePermissionAndTitleRequired(string title,bool write,bool allowed)
    {
        var identity=await Login(new(){Title=title,Write=write});var vm=new InventoryReceivingViewModel(identity,new Fake());Assert.Equal(allowed,vm.HasAccess);
        Assert.Null(vm.ManufacturedOn);Assert.Null(vm.ExpiresOn);Assert.False(vm.InspectionConfirmed);Assert.False(vm.CanSave);Fill(vm);Assert.Equal(allowed,vm.CanSave);
    }
    [Fact]
    public async Task ConfirmationCancelAndSuccessfulGetThenNewDraftResetsEveryField()
    {
        var fake=new Fake();var vm=new InventoryReceivingViewModel(await Login(new()),fake);Fill(vm);
        Assert.NotNull(vm.BeginConfirmation());Assert.False(vm.CanEdit);await vm.ConfirmAsync(false);Assert.Equal(0,fake.Posts);Assert.True(vm.CanSave);
        vm.BeginConfirmation();await vm.ConfirmAsync(true);Assert.Equal(Saved,vm.Result);Assert.Equal(1,fake.Posts);Assert.Equal(1,fake.Gets);Assert.False(vm.CanSave);
        await vm.StartNewAsync();Assert.Null(vm.Result);Assert.Equal("",vm.LotNumber);Assert.Null(vm.Vaccine);Assert.Null(vm.ManufacturedOn);Assert.Null(vm.ExpiresOn);Assert.False(vm.InspectionConfirmed);Assert.False(vm.CanSave);
    }
    [Fact]
    public async Task UnknownOutcomeFreezesPayloadAndRequiresGetBeforeManualSameRequestRetry()
    {
        var fake=new Fake { PostError="outcomeUnknown",Missing=true };var vm=new InventoryReceivingViewModel(await Login(new()),fake);Fill(vm);vm.BeginConfirmation();await vm.ConfirmAsync(true);
        Assert.False(vm.CanEdit);Assert.False(vm.CanRetry);vm.LotNumber="OTHER";Assert.Equal(" "+Request.LotNumber+" ",vm.LotNumber);
        await vm.RetryAsync();Assert.Equal(1,fake.Posts);await vm.ReconcileAsync();Assert.True(vm.CanRetry);
        fake.PostError=null;fake.Missing=false;await vm.RetryAsync();Assert.Equal(2,fake.Posts);Assert.All(fake.Requests,r=>Assert.Equal(Request,r));Assert.Equal(Saved,vm.Result);
    }
    [Theory]
    [InlineData("recordedUnreadable")][InlineData("inventoryLotExists")]
    public async Task AcknowledgedOrDuplicateNeverRetriesAfterMissingReceipt(string error)
    {
        var fake=new Fake { PostError=error,Missing=true };var vm=new InventoryReceivingViewModel(await Login(new()),fake);Fill(vm);vm.BeginConfirmation();await vm.ConfirmAsync(true);await vm.ReconcileAsync();Assert.False(vm.CanRetry);Assert.Null(vm.Result);
        fake.Missing=false;await vm.ReconcileAsync();Assert.Equal(Saved,vm.Result);Assert.Equal(1,fake.Posts);
    }
    [Fact]
    public async Task ValidationFailurePreservesDraftForCorrection()
    {
        var vm=new InventoryReceivingViewModel(await Login(new()),new Fake{PostError="validation"});Fill(vm);vm.BeginConfirmation();await vm.ConfirmAsync(true);
        Assert.True(vm.CanEdit);Assert.Equal(Request.SupplierName,vm.SupplierName);Assert.Equal(Request.DocumentNumber,vm.DocumentNumber);Assert.Equal("100",vm.Quantity);Assert.Null(vm.Result);
    }
    [Theory]
    [InlineData("invalidSession")][InlineData("permissionDenied")]
    public async Task RejectedIdentityClearsDraftAndDisablesFurtherWrites(string code)
    {
        var vm=new InventoryReceivingViewModel(await Login(new()),new Fake{PostError=code});Fill(vm);vm.BeginConfirmation();await vm.ConfirmAsync(true);
        Assert.True(vm.NoAccess);Assert.False(vm.CanSave);Assert.Equal("",vm.LotNumber);Assert.Equal("",vm.SupplierName);Assert.Null(vm.Vaccine);Assert.False(vm.InspectionConfirmed);
    }
    [Fact]
    public async Task DifferentReceiptCannotClaimSuccessOrPermitOverwrite()
    {
        var fake=new Fake { PostError="inventoryLotExists",Result=Saved with{Received=Request with{Quantity=50}} };var vm=new InventoryReceivingViewModel(await Login(new()),fake);Fill(vm);vm.BeginConfirmation();await vm.ConfirmAsync(true);await vm.ReconcileAsync();
        Assert.Null(vm.Result);Assert.False(vm.CanRetry);Assert.Contains("khác",vm.Feedback);
    }
    [Theory]
    [InlineData("list")][InlineData("post")][InlineData("get")]
    public async Task OldSessionDiscardsLateResponseAndClearsAllDraftAndResult(string stage)
    {
        var h=new Handler();var identity=await Login(h);var fake=new Fake();var vm=new InventoryReceivingViewModel(identity,fake);Fill(vm);Task pending;
        if(stage=="list"){fake.ListGate=new();pending=vm.LoadVaccinesAsync();}
        else{vm.BeginConfirmation();if(stage=="post"){fake.PostGate=new();pending=vm.ConfirmAsync(true);}else{fake.PostError="outcomeUnknown";await vm.ConfirmAsync(true);fake.GetGate=new();pending=vm.ReconcileAsync();}}
        Assert.True(vm.IsBusy);Assert.False(vm.CanSave);await identity.LogoutAsync();h.Title="Admin";await identity.LoginAsync("other","synthetic-password");
        fake.ListGate?.SetResult(new([new(1,Saved.VaccineName)],false));fake.PostGate?.SetResult(Saved);fake.GetGate?.SetResult(Saved);await pending;
        Assert.Null(vm.Result);Assert.Null(vm.Vaccine);Assert.Empty(vm.Vaccines);Assert.Equal("",vm.SupplierName);Assert.Equal("",vm.LotNumber);Assert.False(vm.HasAccess);
    }
    [Theory]
    [InlineData(400,"validation")][InlineData(401,"invalidSession")][InlineData(403,"permissionDenied")][InlineData(409,"inventoryLotExists")][InlineData(503,"outcomeUnknown")]
    public async Task ServiceErrorsSafeAndPostNeverRefreshesOrReplays(int status,string code)
    {
        var h=new Handler { Status=(HttpStatusCode)status };var service=new InventoryReceivingService(await Login(h));
        var error=await Assert.ThrowsAsync<InventoryApiException>(()=>service.ReceiveAsync(Request));Assert.Equal(code,error.Code);Assert.DoesNotContain("PRIVATE",error.Message);Assert.Equal(1,h.Posts);Assert.Equal(0,h.Refreshes);
    }
    [Theory]
    [InlineData("network","outcomeUnknown")][InlineData("timeout","outcomeUnknown")][InlineData("bad201","recordedUnreadable")]
    public async Task PostTransportFailurePreservesUnknownOrKnownCommittedState(string failure,string code)
    {
        var h=new Handler { Failure=failure };var service=new InventoryReceivingService(await Login(h));Assert.Equal(code,(await Assert.ThrowsAsync<InventoryApiException>(()=>service.ReceiveAsync(Request))).Code);Assert.Equal(1,h.Posts);
    }
    [Fact]
    public async Task RealRoutesAndExactWritePayloadAndOldServiceCannotUseNextSession()
    {
        var h=new Handler();var identity=await Login(h);var service=new InventoryReceivingService(identity);
        Assert.Equal(Saved,await service.ReceiveAsync(Request));using var json=JsonDocument.Parse(h.Body);Assert.Equal(9,json.RootElement.EnumerateObject().Count());
        Assert.Equal(Saved,await service.ReceiptAsync(Request.LotNumber));Assert.Contains("lotNumber=%5BDEMO%5D%20%2F%20%2B%25%3F%23",h.Paths.Last());
        await service.VaccinesAsync(" A/+ ",2);Assert.Contains("search=A%2F%2B&page=2",h.Paths.Last());
        await identity.LogoutAsync();await identity.LoginAsync("next","synthetic-password");var count=h.Paths.Count;await Assert.ThrowsAsync<OperationCanceledException>(()=>service.ReceiptAsync(Request.LotNumber));Assert.Equal(count,h.Paths.Count);
    }
    private sealed class Fake:IInventoryReceivingService
    {
        public int Posts,Gets;public string? PostError;public bool Missing;public InventoryReceiptDto Result=Saved;public List<ReceiveInventoryRequest> Requests=[];
        public TaskCompletionSource<InventoryVaccineListResponse>? ListGate;public TaskCompletionSource<InventoryReceiptDto>? PostGate,GetGate;
        public Task<InventoryVaccineListResponse> VaccinesAsync(string search,int page)=>ListGate?.Task??Task.FromResult(new InventoryVaccineListResponse([new(1,Saved.VaccineName)],false));
        public Task<InventoryReceiptDto> ReceiveAsync(ReceiveInventoryRequest request){Posts++;Requests.Add(request);return PostError is {} error?Task.FromException<InventoryReceiptDto>(new InventoryApiException(error,"Synthetic")):PostGate?.Task??Task.FromResult(Result);}
        public Task<InventoryReceiptDto> ReceiptAsync(string lot){Gets++;return Missing?Task.FromException<InventoryReceiptDto>(new InventoryApiException("resourceNotFound","Missing")):GetGate?.Task??Task.FromResult(Result);}
    }
    private sealed class Handler:HttpMessageHandler
    {
        public string Title="ThuKho",Failure="",Body="";public bool Write=true;public int Posts,Refreshes;public HttpStatusCode Status=HttpStatusCode.OK;public List<string> Paths=[];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req,CancellationToken ct)
        {
            var path=req.RequestUri!.PathAndQuery;
            if(path.EndsWith("/login")||path.EndsWith("/refresh")){if(path.EndsWith("/refresh"))Refreshes++;return Json(new{accessToken="access",refreshToken="refresh"});}
            if(path.EndsWith("/logout"))return new(HttpStatusCode.NoContent);
            if(path.EndsWith("/me"))return Json(new{accountType="employee",employee=new{title=Title},permissions=Write?new[]{"XemKho","NhapKho"}:new[]{"XemKho"},capabilities=new { inventoryRead=Title=="ThuKho",inventoryReceive=Title=="ThuKho" && Write }});
            Paths.Add(path);
            if(req.Method==HttpMethod.Post){Posts++;Body=await req.Content!.ReadAsStringAsync(ct);if(Failure=="network")throw new HttpRequestException();if(Failure=="timeout")throw new TaskCanceledException();if(Failure=="bad201")return Json(new{},HttpStatusCode.Created);return Status==HttpStatusCode.OK?Json(Saved,HttpStatusCode.Created):Json(new{detail="PRIVATE SQL"},Status);}
            return path.Contains("receiving-vaccines")?Json(new InventoryVaccineListResponse([new(1,Saved.VaccineName)],false)):Json(Saved);
        }
        private static HttpResponseMessage Json(object value,HttpStatusCode status=HttpStatusCode.OK)=>new(status){Content=JsonContent.Create(value)};
    }
}
