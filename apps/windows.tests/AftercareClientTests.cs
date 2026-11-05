using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class AftercareClientTests
{
    private static readonly VaccinationRecord Shot=new(8,12,7,3,new(2,"[DEMO] Recipient"),new(11,"[DEMO] vaccine"),"[DEMO] LOT","[DEMO] site",new(6,"[DEMO] nurse"),DateTimeOffset.UtcNow.AddMinutes(-31),"DaTiem");
    private static readonly FollowUpRequest Request=new("[DEMO] tình trạng","[DEMO] mức độ","[DEMO] xử trí thực tế");
    private static readonly FollowUpRecord Saved=new(9,8,Request.Condition,Request.Severity,Request.Treatment,new(5,"[DEMO] recording nurse"),DateTimeOffset.UtcNow);
    private static readonly FollowUpObservation Observation=new(Shot.Id,Shot.AdministeredAt.AddMinutes(30),true,null);
    private static readonly FollowUpPendingItem Pending=new(Shot,Observation);
    private static readonly FollowUpDetail Detail=new(Shot,null,Observation);
    private static async Task<IdentityApiService> Login(Handler handler){var identity=new IdentityApiService(new HttpClient(handler){BaseAddress=new("http://test/")});Assert.True((await identity.LoginAsync("synthetic","synthetic-password")).Succeeded);return identity;}
    private static async Task<AftercareViewModel> Ready(Fake fake,IdentityApiService? identity=null){var vm=new AftercareViewModel(fake,identity??await Login(new()));await vm.LoadAsync();vm.Selected=Assert.Single(vm.Queue);await vm.LoadDetailAsync();return vm;}
    private static void Fill(AftercareViewModel vm){vm.Condition=" "+Request.Condition+" ";vm.Severity=" "+Request.Severity+" ";vm.Treatment=" "+Request.Treatment+" ";}
    [Theory]
    [InlineData("DieuDuong",true)][InlineData("Admin",false)][InlineData("BacSi",false)][InlineData("LeTan",false)][InlineData("ThuNgan",false)][InlineData("ThuKho",false)]
    public async Task ExactNurseTitleUsesExistingIdentityBoundary(string title,bool allowed)
    {
        var identity=await Login(new(){Title=title});var fake=new Fake();var vm=new AftercareViewModel(fake,identity);await vm.LoadAsync();Assert.Equal(allowed,vm.HasAccess);Assert.Equal(allowed?1:0,fake.ListCalls);
    }
    [Fact]
    public async Task RealRoutesExactThreeFieldsAndNoPostRefreshOrRetry()
    {
        var h=new Handler();var service=new AftercareApiService(await Login(h));await service.ListAsync(2," MT-0008 ");Assert.Contains("page=2&pageSize=25&search=MT-0008",h.Paths.Last());
        Assert.Equal(Detail,await service.GetAsync(8));Assert.Equal(Saved,await service.RecordAsync(8,Request));Assert.Equal("/api/v1/vaccinations/8/follow-ups",h.Paths.Last());
        using var body=JsonDocument.Parse(h.Body);Assert.Equal(new[]{"condition","severity","treatment"},body.RootElement.EnumerateObject().Select(x=>x.Name).ToArray());Assert.Equal(1,h.PostCount);Assert.Equal(0,h.RefreshCount);
    }
    [Theory]
    [InlineData(400,"validation")][InlineData(401,"invalidSession")][InlineData(403,"permissionDenied")][InlineData(409,"followUpAlreadyRecorded")][InlineData(503,"outcomeUnknown")]
    public async Task ErrorsAreSafeAndNeverReplayPost(int status,string code)
    {
        var h=new Handler{PostStatus=(HttpStatusCode)status};var service=new AftercareApiService(await Login(h));var error=await Assert.ThrowsAsync<AftercareApiException>(()=>service.RecordAsync(8,Request));Assert.Equal(code,error.Code);Assert.DoesNotContain("PRIVATE",error.Message);Assert.Equal(1,h.PostCount);Assert.Equal(0,h.RefreshCount);
    }
    [Theory]
    [InlineData("network","outcomeUnknown")][InlineData("timeout","outcomeUnknown")][InlineData("invalid201","recordedUnreadable")][InlineData("wrong201","recordedUnreadable")]
    public async Task UnknownOutcomeAndUnreadable201RequireReconciliation(string failure,string code)
    {
        var h=new Handler{Failure=failure};var service=new AftercareApiService(await Login(h));Assert.Equal(code,(await Assert.ThrowsAsync<AftercareApiException>(()=>service.RecordAsync(8,Request))).Code);Assert.Equal(1,h.PostCount);
    }
    [Theory]
    [InlineData("missingWrapper")][InlineData("wrongShot")][InlineData("wrongFollowup")][InlineData("beforeShot")]
    [InlineData("missingObservation")][InlineData("wrongMilestone")][InlineData("wrongObservationShot")][InlineData("conflictingReadiness")]
    public async Task MalformedGetsCannotProveNoRecordOrShowWrongPatient(string failure)
    {
        var h=new Handler{Failure=failure};var service=new AftercareApiService(await Login(h));await Assert.ThrowsAsync<JsonException>(()=>service.GetAsync(8));
    }
    [Fact]
    public async Task NoDefaultsExplicitThreeFieldsConfirmationCancelAndReadOnlyReopen()
    {
        var f=new Fake();var vm=await Ready(f);Assert.Equal("",vm.Condition);Assert.Equal("",vm.Severity);Assert.Equal("",vm.Treatment);Assert.False(vm.CanSave);
        vm.Condition=Request.Condition;vm.Severity=Request.Severity;Assert.False(vm.CanSave);vm.Treatment=" ";Assert.False(vm.CanSave);Fill(vm);Assert.True(vm.CanSave);Assert.False(vm.CanBrowse);
        vm.Selected=Pending with{Vaccination=Shot with{Id=99}};Assert.Equal(Pending,vm.Selected);Assert.Contains(Request.Treatment,vm.BeginConfirmation());Assert.False(vm.CanEdit);vm.Condition="blocked";Assert.Equal(" "+Request.Condition+" ",vm.Condition);
        await vm.ConfirmAsync(false);Assert.Equal(0,f.PostCalls);Assert.True(vm.CanSave);vm.BeginConfirmation();await vm.ConfirmAsync(true);Assert.True(vm.IsRecorded);Assert.False(vm.CanSave);Assert.False(vm.CanEdit);Assert.Equal(Saved,vm.Result);Assert.Equal("",vm.Condition);
        await vm.LoadAsync();vm.ResultVaccinationId="MT-0008";await vm.ReadResultAsync();Assert.Equal(Saved,vm.Result);
    }
    [Fact]
    public async Task LengthLimitsKnownErrorsAndResetRetainOrClearOnlyDraft()
    {
        var f=new Fake();var vm=await Ready(f);Fill(vm);
        vm.Condition=new string('x',101);Assert.False(vm.CanSave);Fill(vm);vm.Severity=new string('x',101);Assert.False(vm.CanSave);Fill(vm);vm.Treatment=new string('x',501);Assert.False(vm.CanSave);Fill(vm);
        f.Error="validation";vm.BeginConfirmation();await vm.ConfirmAsync(true);Assert.False(vm.HasPending);Assert.True(vm.CanEdit);Assert.Equal(" "+Request.Treatment+" ",vm.Treatment);vm.ResetDraftCommand.Execute(null);Assert.True(vm.CanBrowse);Assert.Equal("",vm.Treatment);
        f.FailGet=true;await vm.LoadDetailAsync();Assert.Null(vm.Detail);f.FailGet=false;await vm.LoadDetailAsync();Assert.Equal(Detail,vm.Detail);
        f.ListValue=[];await vm.LoadAsync();Assert.True(vm.NoResults);f.ListError=true;await vm.LoadAsync();Assert.False(vm.NoResults);Assert.True(vm.CanBrowse);
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task FrozenRequestCanRetryOnlyAfterGetAndKeepsSamePayload(bool committed)
    {
        var f=new Fake{Error="outcomeUnknown"};var vm=await Ready(f);Fill(vm);vm.BeginConfirmation();await vm.ConfirmAsync(true);Assert.True(vm.HasPending);Assert.False(vm.CanEdit);Assert.False(vm.CanBrowse);Assert.Null(vm.BeginConfirmation());
        await vm.RetryAsync();Assert.Equal(1,f.PostCalls);f.FailGet=true;await vm.ReconcileAsync();Assert.False(vm.RetryCommand.CanExecute(null));f.FailGet=false;f.Persisted=committed;await vm.ReconcileAsync();
        if(committed){Assert.True(vm.IsRecorded);Assert.False(vm.HasPending);Assert.Equal(1,f.PostCalls);}
        else{var original=f.Requests.Single();Assert.True(vm.RetryCommand.CanExecute(null));f.Error=null;await vm.RetryAsync();Assert.Same(original,f.Requests.Last());Assert.True(vm.IsRecorded);}
    }
    [Fact]
    public async Task Known201NeverRetriesEvenIfGetNullAndBusyPreventsDuplicate()
    {
        var f=new Fake{PostGate=new()};var vm=await Ready(f);Fill(vm);vm.BeginConfirmation();var saving=vm.ConfirmAsync(true);await vm.ConfirmAsync(true);Assert.Null(vm.BeginConfirmation());Assert.Equal(1,f.PostCalls);
        f.PostGate.SetException(new AftercareApiException("recordedUnreadable","safe"));await saving;await vm.ReconcileAsync();await vm.RetryAsync();Assert.True(vm.HasPending);Assert.False(vm.RetryCommand.CanExecute(null));Assert.Equal(1,f.PostCalls);
    }
    [Theory]
    [InlineData("invalidSession")][InlineData("permissionDenied")]
    public async Task DenialClearsAllPatientDataAndPending(string code)
    {
        var f=new Fake{Error=code};var vm=await Ready(f);Fill(vm);vm.BeginConfirmation();await vm.ConfirmAsync(true);Assert.Empty(vm.Queue);Assert.Null(vm.Detail);Assert.Null(vm.Result);Assert.Null(vm.Selected);Assert.Equal("",vm.Condition);Assert.Equal("",vm.Severity);Assert.Equal("",vm.Treatment);Assert.Equal("",vm.ResultVaccinationId);Assert.False(vm.HasAccess);Assert.False(vm.HasPending);
    }
    [Theory]
    [InlineData("list")][InlineData("get")][InlineData("post")][InlineData("reconcile")]
    public async Task LogoutAndNewAccountDropLateResponsesAtEveryStage(string stage)
    {
        var h=new Handler();var identity=await Login(h);var f=new Fake();var vm=await Ready(f,identity);Task operation;
        if(stage=="list"){f.ListGate=new();operation=vm.LoadAsync();}
        else if(stage=="get"){f.GetGate=new();operation=vm.LoadDetailAsync();}
        else{Fill(vm);vm.BeginConfirmation();if(stage=="post"){f.PostGate=new();operation=vm.ConfirmAsync(true);}else{f.Error="outcomeUnknown";await vm.ConfirmAsync(true);f.GetGate=new();operation=vm.ReconcileAsync();}}
        await identity.LogoutAsync();h.Title="Admin";Assert.True((await identity.LoginAsync("other","synthetic-password")).Succeeded);
        f.ListGate?.SetResult([Pending]);f.GetGate?.SetResult(new(Shot,Saved,Observation));f.PostGate?.SetResult(Saved);await operation;
        Assert.Empty(vm.Queue);Assert.Null(vm.Detail);Assert.Null(vm.Result);Assert.Null(vm.Selected);Assert.Equal("",vm.Condition);Assert.Equal("",vm.Treatment);Assert.False(vm.HasPending);Assert.False(vm.HasAccess);
    }
    [Fact]
    public async Task OldServiceCannotUseNewNursesToken()
    {
        var h=new Handler();var identity=await Login(h);var service=new AftercareApiService(identity);await identity.LogoutAsync();Assert.True((await identity.LoginAsync("next","synthetic-password")).Succeeded);
        await Assert.ThrowsAsync<OperationCanceledException>(()=>service.GetAsync(8));Assert.Equal(0,h.PostCount);
    }
    [Fact]
    public async Task WaitingMilestoneRequiresRefreshAndKeepsDraftOnRefreshFailureAndServerRejection()
    {
        var f=new Fake{CurrentObservation=Observation with{CanComplete=false,BlockReason="observationPeriodNotCompleted"}};
        var vm=await Ready(f);Fill(vm);Assert.False(vm.CanSave);Assert.Null(vm.BeginConfirmation());
        Assert.Equal("Đang theo dõi sau tiêm",vm.Detail!.Observation!.StatusLabel);
        Assert.Contains(Observation.ObservationUntil.ToLocalTime().ToString("HH:mm:ss"),vm.Detail.Observation.UntilLabel);
        f.FailGet=true;await vm.RefreshDetailAsync();Assert.False(vm.CanSave);Assert.Equal(" "+Request.Treatment+" ",vm.Treatment);
        f.FailGet=false;f.CurrentObservation=Observation;await vm.RefreshDetailAsync();Assert.True(vm.CanSave);
        Assert.True(Assert.Single(vm.Queue).Observation.CanComplete);
        Assert.Equal(" "+Request.Treatment+" ",vm.Treatment);Assert.True(vm.DetailCommand.CanExecute(null));
        f.Error="observationPeriodNotCompleted";vm.BeginConfirmation();await vm.ConfirmAsync(true);
        Assert.False(vm.CanSave);Assert.False(vm.HasPending);Assert.Equal(" "+Request.Treatment+" ",vm.Treatment);
        await vm.RefreshDetailAsync();f.Error=null;Assert.True(vm.CanSave);vm.BeginConfirmation();await vm.ConfirmAsync(true);Assert.True(vm.IsRecorded);Assert.Empty(vm.Queue);
    }
    private sealed class Fake:IAftercareService
    {
        public FollowUpObservation CurrentObservation=Observation;public int PostCalls,ListCalls;public bool Persisted,FailGet,ListError;public string? Error;public FollowUpPendingItem[] ListValue=[Pending];
        public TaskCompletionSource<FollowUpPendingItem[]>? ListGate;public TaskCompletionSource<FollowUpDetail>? GetGate;public TaskCompletionSource<FollowUpRecord>? PostGate;public List<FollowUpRequest> Requests=[];
        public Task<FollowUpPendingItem[]> ListAsync(int page,string search){ListCalls++;return ListError?Task.FromException<FollowUpPendingItem[]>(new HttpRequestException()):ListGate?.Task??Task.FromResult(ListValue);}
        public Task<FollowUpDetail> GetAsync(int id)=>FailGet?Task.FromException<FollowUpDetail>(new HttpRequestException()):GetGate?.Task??Task.FromResult(new FollowUpDetail(Shot,Persisted?Saved:null,CurrentObservation));
        public Task<FollowUpRecord> RecordAsync(int id,FollowUpRequest request){PostCalls++;Requests.Add(request);if(PostGate!=null)return PostGate.Task;if(Error!=null)throw new AftercareApiException(Error,"safe");Persisted=true;return Task.FromResult(Saved);}
    }
    private sealed class Handler:HttpMessageHandler
    {
        public string Title="DieuDuong",Failure="",Body="";public HttpStatusCode PostStatus=HttpStatusCode.Created;public int PostCount,RefreshCount;public List<string> Paths=[];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req,CancellationToken ct)
        {
            var path=req.RequestUri!.PathAndQuery;Paths.Add(path);
            if(path.EndsWith("/login")||path.EndsWith("/refresh")){if(path.EndsWith("/refresh"))RefreshCount++;return Json(new{accessToken="access",refreshToken="refresh"});}
            if(path.EndsWith("/logout"))return new(HttpStatusCode.NoContent);if(path.EndsWith("/me"))return Json(new{accountType="employee",employee=new{title=Title},permissions=Array.Empty<string>(),capabilities=new { vaccinations=Title=="DieuDuong",aftercare=Title=="DieuDuong" }});
            if(req.Method==HttpMethod.Post){PostCount++;Body=await req.Content!.ReadAsStringAsync(ct);if(Failure=="network")throw new HttpRequestException();if(Failure=="timeout")throw new TaskCanceledException();if(Failure=="invalid201")return Json(new{},HttpStatusCode.Created);if(Failure=="wrong201")return Json(Saved with{VaccinationId=99},HttpStatusCode.Created);return PostStatus==HttpStatusCode.Created?Json(Saved,PostStatus):Json(new{code="followUpAlreadyRecorded",detail="PRIVATE SQL note"},PostStatus);}
            if(path.Contains('?'))return Json(new{items=new[]{Shot},observations=new[]{Observation}});
            if(Failure=="missingObservation")return Json(new FollowUpDetail(Shot,null));
            if(Failure=="wrongMilestone")return Json(Detail with{Observation=Observation with{ObservationUntil=Observation.ObservationUntil.AddSeconds(-1)}});
            if(Failure=="wrongObservationShot")return Json(Detail with{Observation=Observation with{VaccinationId=99}});
            if(Failure=="conflictingReadiness")return Json(Detail with{Observation=Observation with{CanComplete=true,BlockReason="observationPeriodNotCompleted"}});
            if(Failure=="missingWrapper")return Json(new{vaccination=Shot});if(Failure=="wrongShot")return Json(new FollowUpDetail(Shot with{Id=99},null,Observation));
            if(Failure=="wrongFollowup")return Json(new FollowUpDetail(Shot,Saved with{VaccinationId=99},Observation));if(Failure=="beforeShot")return Json(new FollowUpDetail(Shot,Saved with{RecordedAt=Shot.AdministeredAt.AddSeconds(-1)},Observation));return Json(Detail);
        }
        private static HttpResponseMessage Json(object value,HttpStatusCode status=HttpStatusCode.OK)=>new(status){Content=JsonContent.Create(value)};
    }
}
