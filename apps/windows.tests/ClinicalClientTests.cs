using System.Net;
using System.Net.Http.Json;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class ClinicalClientTests
{
    private static readonly AppointmentRecord Appointment=new(7,3,new(1,"[DEMO] Customer"),new(2,"[DEMO] Recipient"),"package",12,9,"[DEMO] package",DateTimeOffset.UtcNow,"[DEMO] room","DaXepLich");
    private static readonly ClinicalDetail Detail=new(Appointment,[new(11,"[DEMO] vaccine A"),new(22,"[DEMO] vaccine B")]);
    private static readonly ClinicalAssessment Saved=new(1,7,3,2,new(4,"[DEMO] doctor"),DateTimeOffset.UtcNow,"Đủ điều kiện",null,null,null,[new(1,11,"[DEMO] vaccine A","DuocTiem",DateTimeOffset.UtcNow)]);
    private static readonly AssessmentRequest Request=new("Đủ điều kiện",null,[11]);
    private static async Task<IdentityApiService> Login(Handler handler){var identity=new IdentityApiService(new HttpClient(handler){BaseAddress=new("http://test/")});Assert.True((await identity.LoginAsync("synthetic","synthetic-password")).Succeeded);return identity;}
    private static async Task<ScreeningViewModel> Ready(Fake fake,IdentityApiService? identity=null){var vm=new ScreeningViewModel(fake,identity??await Login(new()));await vm.LoadAsync();vm.Selected=Assert.Single(vm.Appointments);await vm.LoadDetailAsync();return vm;}
    private static void Fill(ScreeningViewModel vm){vm.Conclusion="Đủ điều kiện";vm.Vaccines[0].IsSelected=true;}
    [Theory]
    [InlineData("BacSi",true)][InlineData("Admin",false)][InlineData("LeTan",false)][InlineData("ThuNgan",false)][InlineData("DieuDuong",false)][InlineData("ThuKho",false)]
    public async Task OnlyCurrentDoctorTitleCanBrowseAndEdit(string title,bool allowed)
    {
        var identity=await Login(new(){Title=title});var fake=new Fake();var vm=new ScreeningViewModel(fake,identity);await vm.LoadAsync();Assert.Equal(allowed,vm.HasAccess);Assert.Equal(allowed?1:0,fake.ListCalls);
    }
    [Fact]
    public async Task ServiceUsesClinicalEndpointsExactSelectedIdsAndNeverAddsActorOrVitals()
    {
        var handler=new Handler();var api=new ScreeningApiService(await Login(handler));
        await api.ListAsync(2,"LH-0007");Assert.Contains("page=2&pageSize=25&search=LH-0007",handler.Paths.Last());
        Assert.Equal(2,(await api.GetAsync(7)).AllowedVaccines.Length);Assert.Null(await api.GetAssessmentAsync(7));await api.CreateAsync(7,Request);
        Assert.Contains("/clinical/appointments/7/assessment",handler.Paths.Last());Assert.Contains("\"vaccineIds\":[11]",handler.Body);Assert.DoesNotContain("actor",handler.Body);Assert.DoesNotContain("vital",handler.Body);Assert.Equal(1,handler.PostCount);Assert.Equal(0,handler.RefreshCount);
    }
    [Theory]
    [InlineData(400,"validation")][InlineData(401,"invalidSession")][InlineData(403,"permissionDenied")][InlineData(409,"assessmentAlreadyRecorded")][InlineData(503,"outcomeUnknown")]
    public async Task PostErrorsAreSafeAndNeverAutomaticallyRetried(int status,string code)
    {
        var handler=new Handler{PostStatus=(HttpStatusCode)status};var api=new ScreeningApiService(await Login(handler));
        var e=await Assert.ThrowsAsync<ClinicalApiException>(()=>api.CreateAsync(7,Request));Assert.Equal(code,e.Code);Assert.DoesNotContain("PRIVATE",e.Message);Assert.Equal(1,handler.PostCount);Assert.Equal(0,handler.RefreshCount);
    }
    [Theory]
    [InlineData("network","outcomeUnknown")][InlineData("timeout","outcomeUnknown")][InlineData("invalid201","recordedUnreadable")]
    public async Task UncertainResponsesRequireGet(string failure,string code)
    {
        var handler=new Handler{Failure=failure};var api=new ScreeningApiService(await Login(handler));Assert.Equal(code,(await Assert.ThrowsAsync<ClinicalApiException>(()=>api.CreateAsync(7,Request))).Code);Assert.Equal(1,handler.PostCount);
    }
    [Fact]
    public async Task ExplicitConclusionAndSelectionConfirmationAndReadOnlyResult()
    {
        var fake=new Fake();var vm=await Ready(fake);Assert.Null(vm.Conclusion);Assert.All(vm.Vaccines,v=>Assert.False(v.IsSelected));Assert.False(vm.CanSave);
        vm.Conclusion="Đủ điều kiện";Assert.False(vm.CanSave);vm.Vaccines[0].IsSelected=true;Assert.True(vm.CanSave);Assert.False(vm.CanBrowse);
        Assert.NotNull(vm.BeginConfirmation());Assert.False(vm.CanEdit);vm.Reason="blocked";Assert.Equal("",vm.Reason);await vm.ConfirmAsync(false);Assert.Equal(0,fake.PostCalls);Assert.True(vm.CanSave);
        vm.BeginConfirmation();await vm.ConfirmAsync(true);Assert.True(vm.IsRecorded);Assert.False(vm.CanEdit);Assert.False(vm.CanSave);Assert.Equal(Saved,vm.Assessment);Assert.Null(vm.Conclusion);
        vm.Selected=vm.Appointments[0];await vm.LoadDetailAsync();Assert.NotNull(vm.Assessment);Assert.False(vm.CanSave);
    }
    [Theory]
    [InlineData("Tạm hoãn")][InlineData("Chống chỉ định")]
    public async Task NonEligibleRequiresReasonAndClearsPreviousVaccineSelection(string conclusion)
    {
        var vm=await Ready(new());Fill(vm);vm.Conclusion=conclusion;Assert.All(vm.Vaccines,v=>Assert.False(v.IsSelected));Assert.False(vm.CanSave);Assert.False(vm.CanSelectVaccines);
        vm.Vaccines[0].IsSelected=true;Assert.False(vm.Vaccines[0].IsSelected);vm.Reason="[DEMO] reason";Assert.True(vm.CanSave);vm.Reason=new string('x',501);Assert.False(vm.CanSave);
        vm.ResetDraftCommand.Execute(null);Assert.Null(vm.Conclusion);Assert.Equal("",vm.Reason);Assert.True(vm.CanBrowse);
    }
    [Fact]
    public async Task FailedReadCannotEnableEditingAndKnownValidationKeepsDraft()
    {
        var fake=new Fake{FailGet=true};var vm=await Ready(fake);Assert.Null(vm.Detail);Assert.False(vm.CanEdit);fake.FailGet=false;await vm.LoadDetailAsync();Fill(vm);vm.Reason="[DEMO] keep draft";
        fake.Error="validation";vm.BeginConfirmation();await vm.ConfirmAsync(true);Assert.True(vm.CanEdit);Assert.Equal("[DEMO] keep draft",vm.Reason);Assert.True(vm.Vaccines[0].IsSelected);Assert.Null(vm.Assessment);
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task UnknownOutcomeLocksDraftAndRetriesOnlyAfterSuccessfulGet(bool committed)
    {
        var fake=new Fake{Error="outcomeUnknown"};var vm=await Ready(fake);Fill(vm);vm.BeginConfirmation();await vm.ConfirmAsync(true);
        Assert.True(vm.HasPending);Assert.False(vm.CanEdit);Assert.Null(vm.BeginConfirmation());await vm.RetryAsync();Assert.Equal(1,fake.PostCalls);
        fake.FailGet=true;await vm.ReconcileAsync();Assert.False(vm.RetryCommand.CanExecute(null));fake.FailGet=false;fake.Persisted=committed;await vm.ReconcileAsync();
        if(committed){Assert.True(vm.IsRecorded);Assert.False(vm.HasPending);}
        else{Assert.True(vm.RetryCommand.CanExecute(null));var original=fake.Requests.Single();fake.Error=null;await vm.RetryAsync();Assert.Same(original,fake.Requests.Last());Assert.True(vm.IsRecorded);}
    }
    [Fact]
    public async Task Known201NeverRetriesAndDoubleSubmitIsLocked()
    {
        var fake=new Fake{PostGate=new()};var vm=await Ready(fake);Fill(vm);vm.BeginConfirmation();var save=vm.ConfirmAsync(true);await vm.ConfirmAsync(true);Assert.Null(vm.BeginConfirmation());Assert.Equal(1,fake.PostCalls);
        fake.PostGate.SetException(new ClinicalApiException("recordedUnreadable","Safe"));await save;await vm.ReconcileAsync();Assert.True(vm.HasPending);Assert.False(vm.RetryCommand.CanExecute(null));await vm.RetryAsync();Assert.Equal(1,fake.PostCalls);
    }
    [Theory]
    [InlineData("invalidSession")][InlineData("permissionDenied")]
    public async Task DenialClearsPatientAndDraft(string code)
    {
        var fake=new Fake();var vm=await Ready(fake);Fill(vm);fake.Error=code;vm.BeginConfirmation();await vm.ConfirmAsync(true);Assert.Empty(vm.Appointments);Assert.Empty(vm.Vaccines);Assert.Null(vm.Detail);Assert.Null(vm.Assessment);Assert.Equal("",vm.Reason);Assert.False(vm.HasAccess);
    }
    [Fact]
    public async Task SessionChangesClearClinicalDataAndDropLateListDetailAndPost()
    {
        foreach(var stage in new[]{"list","detail","post"}){
            var handler=new Handler();var identity=await Login(handler);var fake=new Fake();var vm=await Ready(fake,identity);Task operation;
            if(stage=="list"){fake.ListGate=new();operation=vm.LoadAsync();}
            else if(stage=="detail"){fake.DetailGate=new();operation=vm.LoadDetailAsync();}
            else{Fill(vm);vm.BeginConfirmation();fake.PostGate=new();operation=vm.ConfirmAsync(true);}
            await identity.LogoutAsync();handler.Title="LeTan";Assert.True((await identity.LoginAsync("other","synthetic-password")).Succeeded);
            fake.ListGate?.SetResult([Appointment]);fake.DetailGate?.SetResult(Detail);fake.PostGate?.SetResult(Saved);await operation;
            Assert.Empty(vm.Appointments);Assert.Empty(vm.Vaccines);Assert.Null(vm.Detail);Assert.Null(vm.Assessment);Assert.Null(vm.Conclusion);Assert.False(vm.HasAccess);
        }
    }
    private sealed class Fake:IScreeningService
    {
        public int ListCalls,PostCalls;public bool FailGet,Persisted;public string? Error;
        public TaskCompletionSource<AppointmentRecord[]>? ListGate;public TaskCompletionSource<ClinicalDetail>? DetailGate;public TaskCompletionSource<ClinicalAssessment>? PostGate;
        public List<AssessmentRequest> Requests=[];
        public Task<AppointmentRecord[]> ListAsync(int page,string search){ListCalls++;return ListGate?.Task??Task.FromResult(new[]{Appointment});}
        public Task<ClinicalDetail> GetAsync(int id)=>FailGet?Task.FromException<ClinicalDetail>(new HttpRequestException()):DetailGate?.Task??Task.FromResult(Detail);
        public Task<ClinicalAssessment?> GetAssessmentAsync(int id)=>FailGet?Task.FromException<ClinicalAssessment?>(new HttpRequestException()):Task.FromResult(Persisted?Saved:null);
        public Task<ClinicalAssessment> CreateAsync(int id,AssessmentRequest request){PostCalls++;Requests.Add(request);if(PostGate!=null)return PostGate.Task;if(Error!=null)throw new ClinicalApiException(Error,"Safe message");Persisted=true;return Task.FromResult(Saved);}
    }
    private sealed class Handler:HttpMessageHandler
    {
        public string Title="BacSi",Failure="",Body="";public int PostCount,RefreshCount;public HttpStatusCode PostStatus=HttpStatusCode.Created;public List<string> Paths=[];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            var path=request.RequestUri!.PathAndQuery;Paths.Add(path);
            if(path.EndsWith("/login")||path.EndsWith("/refresh")){if(path.EndsWith("/refresh"))RefreshCount++;return Json(new{accessToken="access",refreshToken="refresh"});}
            if(path.EndsWith("/logout"))return new(HttpStatusCode.NoContent);
            if(path.EndsWith("/me"))return Json(new{accountType="employee",employee=new{title=Title},permissions=Array.Empty<string>()});
            if(request.Method==HttpMethod.Post){PostCount++;Body=await request.Content!.ReadAsStringAsync(ct);if(Failure=="network")throw new HttpRequestException();if(Failure=="timeout")throw new TaskCanceledException();if(Failure=="invalid201")return Json(new{},HttpStatusCode.Created);return PostStatus==HttpStatusCode.Created?Json(Saved,PostStatus):Json(new{code=PostStatus==HttpStatusCode.Conflict?"assessmentAlreadyRecorded":"validation",detail="PRIVATE medical SQL"},PostStatus);}
            if(path.Contains("?"))return Json(new{items=new[]{Appointment}});
            if(path.EndsWith("/assessment"))return Json(new{assessment=(ClinicalAssessment?)null});return Json(Detail);
        }
        private static HttpResponseMessage Json(object value,HttpStatusCode status=HttpStatusCode.OK)=>new(status){Content=JsonContent.Create(value)};
    }
}
