using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class VaccinationClientTests
{
    private static readonly VaccinationIndication Indication=new(12,7,3,new(1,"[DEMO] Customer"),new(2,"[DEMO] Recipient"),new(11,"[DEMO] vaccine"),DateTimeOffset.UtcNow,"[DEMO] room",new(4,"[DEMO] doctor"),DateTimeOffset.UtcNow);
    private static readonly VaccinationLot Lot=new("[DEMO] LOT",new(2020,1,1),new(2030,1,1),5);
    private static readonly VaccinationDetail Detail=new(Indication,new(2026,11,1),[Lot]);
    private static readonly VaccinationRecord Saved=new(8,12,7,3,new(2,"[DEMO] Recipient"),new(11,"[DEMO] vaccine"),Lot.Number,"Bắp tay trái",new(6,"[DEMO] nurse"),DateTimeOffset.UtcNow,"DaTiem");
    private static readonly VaccinationRequest Request=new(12,Lot.Number,"Bắp tay trái");
    private static async Task<IdentityApiService> Login(Handler handler){var identity=new IdentityApiService(new HttpClient(handler){BaseAddress=new("http://test/")});Assert.True((await identity.LoginAsync("synthetic","synthetic-password")).Succeeded);return identity;}
    private static async Task<VaccinationViewModel> Ready(Fake fake,IdentityApiService? identity=null){var vm=new VaccinationViewModel(fake,identity??await Login(new()));await vm.LoadAsync();vm.Selected=Assert.Single(vm.Queue);await vm.LoadDetailAsync();return vm;}
    private static void Fill(VaccinationViewModel vm){vm.SelectedLot=vm.Lots.Single();vm.InjectionSite=" Bắp tay trái ";}
    [Theory]
    [InlineData("DieuDuong",true)][InlineData("Admin",false)][InlineData("BacSi",false)][InlineData("LeTan",false)][InlineData("ThuNgan",false)][InlineData("ThuKho",false)]
    public async Task ExactNurseTitleIsRequiredWithoutAnInventedPermission(string title,bool allowed)
    {
        var identity=await Login(new(){Title=title});var fake=new Fake();var vm=new VaccinationViewModel(fake,identity);await vm.LoadAsync();Assert.Equal(allowed,vm.HasAccess);Assert.Equal(allowed?1:0,fake.ListCalls);
    }
    [Fact]
    public async Task ServiceUsesExactThreeFieldsRealRoutesAndNoPostRetry()
    {
        var handler=new Handler();var api=new VaccinationApiService(await Login(handler));
        await api.ListAsync(2," LH-0007 ");Assert.Contains("page=2&pageSize=25&search=LH-0007",handler.Paths.Last());
        Assert.Equal(12,(await api.GetAsync(12)).Indication.Id);Assert.Null(await api.GetResultAsync(12));
        await api.RecordAsync(Request);Assert.Equal("/api/v1/vaccinations",handler.Paths.Last());
        using var body=JsonDocument.Parse(handler.Body);Assert.Equal(new[]{"indicationId","lotNumber","injectionSite"},body.RootElement.EnumerateObject().Select(p=>p.Name).ToArray());
        Assert.Equal(1,handler.PostCount);Assert.Equal(0,handler.RefreshCount);
    }
    [Theory]
    [InlineData(400,"validation")][InlineData(401,"invalidSession")][InlineData(403,"permissionDenied")]
    [InlineData(409,"vaccinationAlreadyRecorded")][InlineData(503,"outcomeUnknown")]
    public async Task PostFailuresNeverRetryOrShowServerDetails(int status,string code)
    {
        var handler=new Handler{PostStatus=(HttpStatusCode)status};var api=new VaccinationApiService(await Login(handler));
        var error=await Assert.ThrowsAsync<VaccinationApiException>(()=>api.RecordAsync(Request));Assert.Equal(code,error.Code);Assert.DoesNotContain("PRIVATE",error.Message);Assert.Equal(1,handler.PostCount);Assert.Equal(0,handler.RefreshCount);
    }
    [Theory]
    [InlineData("network","outcomeUnknown")][InlineData("timeout","outcomeUnknown")][InlineData("invalid201","recordedUnreadable")][InlineData("wrong201","recordedUnreadable")]
    public async Task LostOrUnreadablePostNeedsGetReconciliation(string failure,string code)
    {
        var handler=new Handler{Failure=failure};var api=new VaccinationApiService(await Login(handler));
        Assert.Equal(code,(await Assert.ThrowsAsync<VaccinationApiException>(()=>api.RecordAsync(Request))).Code);Assert.Equal(1,handler.PostCount);
    }
    [Fact]
    public async Task MalformedGetResultDoesNotProveAbsenceAndWrongIndicationIsRejected()
    {
        var handler=new Handler{Failure="missingWrapper"};var api=new VaccinationApiService(await Login(handler));
        await Assert.ThrowsAsync<JsonException>(()=>api.GetResultAsync(12));handler.Failure="wrongResult";
        await Assert.ThrowsAsync<JsonException>(()=>api.GetResultAsync(12));
    }
    [Fact]
    public async Task NoDefaultLotExplicitConfirmationCancelAndReadOnlyResult()
    {
        var fake=new Fake();var vm=await Ready(fake);Assert.Null(vm.SelectedLot);Assert.Equal("",vm.InjectionSite);Assert.False(vm.CanSave);
        vm.InjectionSite="Bắp tay trái";Assert.False(vm.CanSave);vm.SelectedLot=Lot;Assert.True(vm.CanSave);Assert.False(vm.CanBrowse);
        var message=vm.BeginConfirmation();Assert.Contains("đã thực hiện",message);Assert.False(vm.CanEdit);vm.InjectionSite="blocked";Assert.Equal("Bắp tay trái",vm.InjectionSite);
        await vm.ConfirmAsync(false);Assert.Equal(0,fake.PostCalls);Assert.True(vm.CanSave);
        vm.BeginConfirmation();await vm.ConfirmAsync(true);Assert.True(vm.IsRecorded);Assert.False(vm.CanSave);Assert.False(vm.CanEdit);Assert.Equal(Saved,vm.Result);
        Assert.Equal("",vm.InjectionSite);Assert.Null(vm.SelectedLot);Assert.Empty(vm.Lots);
        await vm.LoadAsync();vm.ResultIndicationId="CD-0012";await vm.ReadResultAsync();Assert.Equal(Saved,vm.Result);Assert.False(vm.CanSave);
    }
    [Fact]
    public async Task EmptyLotsAndValidationCannotEnableRecordAndKnownErrorsKeepDraft()
    {
        var fake=new Fake{DetailValue=Detail with{Lots=[]}};var vm=await Ready(fake);Assert.True(vm.NoLots);Assert.False(vm.CanSave);vm.InjectionSite="draft";Assert.False(vm.CanSave);
        vm.ResetDraftCommand.Execute(null);fake.DetailValue=Detail;await vm.LoadDetailAsync();Fill(vm);vm.InjectionSite=new string('x',101);Assert.False(vm.CanSave);vm.InjectionSite="Bắp tay trái";
        fake.Error="vaccinationLotUnavailable";vm.BeginConfirmation();await vm.ConfirmAsync(true);Assert.Equal("Bắp tay trái",vm.InjectionSite);Assert.Equal(Lot,vm.SelectedLot);Assert.False(vm.HasPending);Assert.True(vm.CanEdit);
        vm.ResetDraftCommand.Execute(null);Assert.True(vm.CanBrowse);Assert.Null(vm.SelectedLot);Assert.Equal("",vm.InjectionSite);
    }
    [Fact]
    public async Task ReadFailureHasRetryAndRetainsNoWrongSelection()
    {
        var fake=new Fake{FailGet=true};var vm=await Ready(fake);Assert.Null(vm.Detail);Assert.False(vm.CanEdit);Assert.False(vm.CanSave);
        fake.FailGet=false;await vm.LoadDetailAsync();Assert.Equal(Detail,vm.Detail);
        fake.ListValue=[];await vm.LoadAsync();Assert.True(vm.NoResults);Assert.Null(vm.Detail);
        fake.ListError=true;await vm.LoadAsync();Assert.False(vm.NoResults);Assert.True(vm.CanBrowse);fake.ListError=false;fake.ListValue=[Indication];await vm.LoadAsync();Assert.Single(vm.Queue);
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task UnknownOutcomeLocksDraftAndOnlySameRequestCanRetryAfterSuccessfulGet(bool committed)
    {
        var fake=new Fake{Error="outcomeUnknown"};var vm=await Ready(fake);Fill(vm);vm.BeginConfirmation();await vm.ConfirmAsync(true);
        Assert.True(vm.HasPending);Assert.False(vm.CanEdit);Assert.Null(vm.BeginConfirmation());Assert.False(vm.CanBrowse);Assert.Equal(" Bắp tay trái ",vm.InjectionSite);
        await vm.RetryAsync();Assert.Equal(1,fake.PostCalls);fake.FailGet=true;await vm.ReconcileAsync();Assert.False(vm.RetryCommand.CanExecute(null));
        fake.FailGet=false;fake.Persisted=committed;await vm.ReconcileAsync();
        if(committed){Assert.True(vm.IsRecorded);Assert.False(vm.HasPending);Assert.Equal(1,fake.PostCalls);}
        else{Assert.True(vm.RetryCommand.CanExecute(null));fake.Error=null;var original=fake.Requests.Single();await vm.RetryAsync();Assert.Same(original,fake.Requests.Last());Assert.True(vm.IsRecorded);}
    }
    [Fact]
    public async Task Known201OnlyGetsEvenWhenGetReturnsNullAndDoubleSubmitIsBlocked()
    {
        var fake=new Fake{PostGate=new()};var vm=await Ready(fake);Fill(vm);vm.BeginConfirmation();var save=vm.ConfirmAsync(true);await vm.ConfirmAsync(true);Assert.Null(vm.BeginConfirmation());Assert.Equal(1,fake.PostCalls);
        fake.PostGate.SetException(new VaccinationApiException("recordedUnreadable","safe"));await save;await vm.ReconcileAsync();
        Assert.True(vm.HasPending);Assert.False(vm.RetryCommand.CanExecute(null));await vm.RetryAsync();Assert.Equal(1,fake.PostCalls);
    }
    [Theory]
    [InlineData("invalidSession")][InlineData("permissionDenied")]
    public async Task DenialClearsPatientLotDraftLookupAndPending(string code)
    {
        var fake=new Fake();var vm=await Ready(fake);Fill(vm);fake.Error=code;vm.BeginConfirmation();await vm.ConfirmAsync(true);
        Assert.Empty(vm.Queue);Assert.Empty(vm.Lots);Assert.Null(vm.Detail);Assert.Null(vm.Result);Assert.Null(vm.Selected);Assert.Equal("",vm.InjectionSite);Assert.Equal("",vm.ResultIndicationId);Assert.False(vm.HasPending);Assert.False(vm.HasAccess);
    }
    [Theory]
    [InlineData("list")][InlineData("detail")][InlineData("post")][InlineData("result")]
    public async Task SessionChangeDropsLateResponsesAndDoesNotExposePriorPatient(string stage)
    {
        var handler=new Handler();var identity=await Login(handler);var fake=new Fake();var vm=await Ready(fake,identity);Task operation;
        if(stage=="list"){fake.ListGate=new();operation=vm.LoadAsync();}
        else if(stage=="detail"){fake.DetailGate=new();operation=vm.LoadDetailAsync();}
        else if(stage=="result"){vm.ResultIndicationId="12";fake.ResultGate=new();operation=vm.ReadResultAsync();}
        else{Fill(vm);vm.BeginConfirmation();fake.PostGate=new();operation=vm.ConfirmAsync(true);}
        await identity.LogoutAsync();handler.Title="LeTan";Assert.True((await identity.LoginAsync("other","synthetic-password")).Succeeded);
        fake.ListGate?.SetResult([Indication]);fake.DetailGate?.SetResult(Detail);fake.ResultGate?.SetResult(Saved);fake.PostGate?.SetResult(Saved);await operation;
        Assert.Empty(vm.Queue);Assert.Empty(vm.Lots);Assert.Null(vm.Detail);Assert.Null(vm.Result);Assert.Null(vm.Selected);Assert.Equal("",vm.InjectionSite);Assert.False(vm.HasAccess);
    }
    [Fact]
    public async Task ServiceBoundToOldSessionDoesNotSendWithNextNursesToken()
    {
        var handler=new Handler();var identity=await Login(handler);var service=new VaccinationApiService(identity);
        await identity.LogoutAsync();Assert.True((await identity.LoginAsync("second","synthetic-password")).Succeeded);
        await Assert.ThrowsAsync<OperationCanceledException>(()=>service.ListAsync(1,""));Assert.Equal(0,handler.PostCount);
    }
    private sealed class Fake:IVaccinationService
    {
        public int ListCalls,PostCalls;public bool FailGet,Persisted,ListError;public string? Error;
        public VaccinationDetail DetailValue=Detail;public VaccinationIndication[] ListValue=[Indication];
        public TaskCompletionSource<VaccinationIndication[]>? ListGate;public TaskCompletionSource<VaccinationDetail>? DetailGate;
        public TaskCompletionSource<VaccinationRecord>? PostGate;public TaskCompletionSource<VaccinationRecord?>? ResultGate;
        public List<VaccinationRequest> Requests=[];
        public Task<VaccinationIndication[]> ListAsync(int page,string search){ListCalls++;return ListError?Task.FromException<VaccinationIndication[]>(new HttpRequestException()):ListGate?.Task??Task.FromResult(ListValue);}
        public Task<VaccinationDetail> GetAsync(int id)=>FailGet?Task.FromException<VaccinationDetail>(new HttpRequestException()):DetailGate?.Task??Task.FromResult(DetailValue);
        public Task<VaccinationRecord?> GetResultAsync(int id)=>FailGet?Task.FromException<VaccinationRecord?>(new HttpRequestException()):ResultGate?.Task??Task.FromResult(Persisted?Saved:null);
        public Task<VaccinationRecord> RecordAsync(VaccinationRequest request){PostCalls++;Requests.Add(request);if(PostGate!=null)return PostGate.Task;if(Error!=null)throw new VaccinationApiException(Error,"Safe message");Persisted=true;return Task.FromResult(Saved);}
    }
    private sealed class Handler:HttpMessageHandler
    {
        public string Title="DieuDuong",Failure="",Body="";public int PostCount,RefreshCount;public HttpStatusCode PostStatus=HttpStatusCode.Created;public List<string> Paths=[];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            var path=request.RequestUri!.PathAndQuery;Paths.Add(path);
            if(path.EndsWith("/login")||path.EndsWith("/refresh")){if(path.EndsWith("/refresh"))RefreshCount++;return Json(new{accessToken="access",refreshToken="refresh"});}
            if(path.EndsWith("/logout"))return new(HttpStatusCode.NoContent);
            if(path.EndsWith("/me"))return Json(new{accountType="employee",employee=new{title=Title},permissions=Array.Empty<string>()});
            if(request.Method==HttpMethod.Post){PostCount++;Body=await request.Content!.ReadAsStringAsync(ct);if(Failure=="network")throw new HttpRequestException();if(Failure=="timeout")throw new TaskCanceledException();if(Failure=="invalid201")return Json(new{},HttpStatusCode.Created);if(Failure=="wrong201")return Json(Saved with{IndicationId=99},HttpStatusCode.Created);return PostStatus==HttpStatusCode.Created?Json(Saved,PostStatus):Json(new{code=PostStatus==HttpStatusCode.Conflict?"vaccinationAlreadyRecorded":"validation",detail="PRIVATE SQL medical data"},PostStatus);}
            if(path.Contains("?"))return Json(new{items=new[]{Indication}});
            if(path.EndsWith("/result")){if(Failure=="missingWrapper")return Json(new{});if(Failure=="wrongResult")return Json(new{vaccination=Saved with{IndicationId=99}});return Json(new{vaccination=(VaccinationRecord?)null});}
            return Json(Detail);
        }
        private static HttpResponseMessage Json(object value,HttpStatusCode status=HttpStatusCode.OK)=>new(status){Content=JsonContent.Create(value)};
    }
}
