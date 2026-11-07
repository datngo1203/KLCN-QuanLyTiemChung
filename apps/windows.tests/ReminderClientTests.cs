using System.Net;
using System.Net.Http.Json;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class ReminderClientTests
{
    private static readonly AppointmentRecord Appointment=new(3,1,new(1,"[DEMO] customer"),new(2,"[DEMO] recipient"),"vaccine",1,1,"[DEMO] vaccine",DateTimeOffset.UtcNow.AddDays(1),"[DEMO] room","DaXepLich");
    private static readonly ReminderRecord Saved=new(7,3,"Email","ChoGui","[DEMO] snapshot",DateTimeOffset.UtcNow,new(4,"[DEMO] reception"),"k***@example.invalid","[VNVC] Nhắc lịch tiêm chủng LH-0003");
    private static ReminderDetail Detail(bool saved=false)=>new(Appointment,saved?null:"[DEMO] preview",!saved,saved?Saved:null,"k***@example.invalid","[VNVC] Nhắc lịch tiêm chủng LH-0003");
    private static async Task<IdentityApiService> Login(Handler handler){var identity=new IdentityApiService(new HttpClient(handler){BaseAddress=new Uri("http://localhost/")});Assert.True((await identity.LoginAsync("synthetic","synthetic")).Succeeded);return identity;}
    private static async Task<ReminderViewModel> Ready(Fake fake,IdentityApiService? identity=null){var vm=new ReminderViewModel(fake,identity??await Login(new Handler()));vm.Select(3);await vm.LoadAsync();return vm;}
    [Fact] public async Task PreviewConfirmationCancelAndReadOnlyResultUseOnePost() {
        var fake=new Fake();var vm=await Ready(fake);Assert.True(vm.CanRecord);Assert.Equal("[DEMO] preview",vm.Content);
        await vm.RecordAsync();Assert.Equal(0,fake.Posts);vm.ConfirmCommand.Execute(null);Assert.True(vm.BlocksNavigation);
        vm.CancelCommand.Execute(null);Assert.False(vm.IsConfirming);Assert.Equal(0,fake.Posts);
        vm.ConfirmCommand.Execute(null);await vm.RecordAsync();Assert.Equal(1,fake.Posts);Assert.Equal(Saved,vm.Result);Assert.False(vm.CanRecord);Assert.False(vm.HasPending);
        await vm.LoadAsync();Assert.Equal(Saved.Content,vm.Content);Assert.Equal(1,fake.Posts);
    }
    [Theory][InlineData(true)][InlineData(false)]
    public async Task UnknownOutcomeRequiresGetAndLocksSameAppointment(bool committed) {
        var fake=new Fake{Error="outcomeUnknown"};var vm=await Ready(fake);fake.Persisted=committed;vm.ConfirmCommand.Execute(null);await vm.RecordAsync();
        Assert.True(vm.HasPending);Assert.False(vm.RetryCommand.CanExecute(null));vm.Select(99);await vm.RecordAsync();Assert.Equal(1,fake.Posts);
        await vm.LoadAsync();Assert.Equal(!committed,vm.HasPending);
        if(committed){Assert.Equal(Saved,vm.Result);Assert.False(vm.RetryCommand.CanExecute(null));}
        else{Assert.True(vm.RetryCommand.CanExecute(null));fake.Error=null;vm.RetryCommand.Execute(null);await Task.Yield();Assert.Equal(new[]{3,3},fake.PostedIds);}
    }
    [Fact] public async Task Known201CannotRetryWhenGetStillHasNoRow() {
        var fake=new Fake{Error="recordedUnreadable"};var vm=await Ready(fake);vm.ConfirmCommand.Execute(null);await vm.RecordAsync();await vm.LoadAsync();
        Assert.True(vm.HasPending);Assert.False(vm.RetryCommand.CanExecute(null));Assert.Equal(1,fake.Posts);
    }
    [Theory][InlineData("GET")][InlineData("POST")]
    public async Task OldSessionDropsLateResponsesAndCannotUseNewSession(string operation) {
        var identity=await Login(new Handler());var fake=new Fake();var vm=await Ready(fake,identity);Task work;
        if(operation=="GET"){fake.GetGate=new();work=vm.LoadAsync();}else{fake.PostGate=new();vm.ConfirmCommand.Execute(null);work=vm.RecordAsync();}
        await identity.LogoutAsync();await identity.LoginAsync("other","synthetic");fake.GetGate?.SetResult(Detail());fake.PostGate?.SetResult(Saved);await work;
        Assert.False(vm.HasAccess);Assert.Null(vm.Result);Assert.Equal("",vm.Content);Assert.False(vm.HasPending);
        var handler=new Handler();identity=await Login(handler);var service=new ReminderApiService(identity);await identity.LogoutAsync();await identity.LoginAsync("other","synthetic");
        await Assert.ThrowsAsync<AppointmentApiException>(()=>service.RecordAsync(3));Assert.Equal(0,handler.Posts);
    }
    [Theory][InlineData("Admin",true)][InlineData("BacSi",true)][InlineData("LeTan",false)]
    public async Task UnauthorizedUiMakesNoCalls(string title,bool grant) {
        var fake=new Fake();var vm=new ReminderViewModel(fake,await Login(new Handler{Title=title,Grant=grant}));vm.Select(3);await vm.LoadAsync();await vm.RecordAsync();Assert.Equal(0,fake.Gets);Assert.Equal(0,fake.Posts);
    }
    [Theory][InlineData(400,"validation")][InlineData(401,"invalidSession")][InlineData(403,"permissionDenied")][InlineData(409,"reminderAlreadyRecorded")][InlineData(503,"outcomeUnknown")]
    public async Task ServicePostsOnlyEmptyObjectAndNeverReplays(int status,string code) {
        var handler=new Handler{Status=(HttpStatusCode)status};var service=new ReminderApiService(await Login(handler));
        var error=await Assert.ThrowsAsync<AppointmentApiException>(()=>service.RecordAsync(3));Assert.Equal(code,error.Code);Assert.DoesNotContain("PRIVATE",error.Message);
        Assert.Equal(1,handler.Posts);Assert.Equal("{}",handler.Body);Assert.EndsWith("/appointments/3/email-reminder",handler.LastPath);Assert.Equal(0,handler.Refreshes);
    }
    [Theory][InlineData("network","outcomeUnknown")][InlineData("bad201","recordedUnreadable")]
    public async Task LostOrUnreadableResponseRequiresGet(string failure,string code) {
        var handler=new Handler{Failure=failure};var service=new ReminderApiService(await Login(handler));Assert.Equal(code,(await Assert.ThrowsAsync<AppointmentApiException>(()=>service.RecordAsync(3))).Code);Assert.Equal(1,handler.Posts);
    }
    [Fact] public async Task FailureToLoadClearsPreviewAndPermissionFailureClearsAllData() {
        var fake=new Fake();var vm=await Ready(fake);fake.GetError="dependencyUnavailable";await vm.LoadAsync();Assert.False(vm.CanRecord);Assert.Equal("",vm.Content);
        fake.GetError=null;await vm.LoadAsync();fake.Error="permissionDenied";vm.ConfirmCommand.Execute(null);await vm.RecordAsync();Assert.False(vm.HasAccess);Assert.Equal("",vm.Content);Assert.False(vm.HasPending);
    }
    [Fact] public async Task QueueSearchSelectCancelSuccessAndRetry() {
        var fake=new Fake();var vm=new ReminderViewModel(fake,await Login(new Handler()));
        vm.Search="Nguyễn";await vm.LoadQueueAsync();Assert.Single(vm.Queue);Assert.False(vm.NoResults);
        vm.Selected=vm.Queue[0];await vm.LoadAsync();Assert.Equal(Appointment,vm.Appointment);
        vm.ConfirmCommand.Execute(null);Assert.False(vm.CanBrowse);vm.CancelCommand.Execute(null);Assert.Equal(0,fake.Posts);
        vm.ConfirmCommand.Execute(null);await vm.RecordAsync();Assert.Single(vm.Queue);Assert.Equal(Saved,vm.Result);
        fake.GetError="dependencyUnavailable";await vm.LoadQueueAsync();Assert.Empty(vm.Queue);Assert.Contains("thử lại",vm.QueueFeedback);
        fake.GetError=null;await vm.LoadQueueAsync();Assert.True(vm.NoResults);
    }
    [Fact] public async Task QueueClearsOnPermissionFailureAndLogout() {
        var fake=new Fake();var identity=await Login(new Handler());var vm=new ReminderViewModel(fake,identity);
        await vm.LoadQueueAsync();vm.Selected=vm.Queue[0];await vm.LoadAsync();fake.GetError="permissionDenied";
        await vm.LoadQueueAsync();Assert.Empty(vm.Queue);Assert.Null(vm.Selected);Assert.Null(vm.Appointment);Assert.Empty(vm.Content);Assert.False(vm.HasAccess);
        fake.GetError=null;vm=new ReminderViewModel(fake,identity);await vm.LoadQueueAsync();vm.Search="Nguyễn";
        await identity.LogoutAsync();Assert.Empty(vm.Queue);Assert.Empty(vm.Search);Assert.Null(vm.Selected);Assert.False(vm.HasAccess);
    }
    [Fact] public async Task QueueServiceEscapesSearchAndUsesPaging() {
        var handler=new Handler();var service=new ReminderApiService(await Login(handler));
        Assert.Single(await service.ListAsync(" A&B ",2));Assert.Contains("search=A%26B&page=2&pageSize=25",handler.LastPath);
    }
    [Fact] public async Task LateQueueCannotRestoreDataAfterSessionChange() {
        var fake=new Fake{ListGate=new()};var identity=await Login(new Handler());var vm=new ReminderViewModel(fake,identity);
        var work=vm.LoadQueueAsync();await identity.LogoutAsync();await identity.LoginAsync("other","synthetic");
        fake.ListGate.SetResult([Appointment]);await work;Assert.Empty(vm.Queue);Assert.False(vm.HasAccess);
    }
    [Theory][InlineData("ChoGui","Đang gửi")][InlineData("DaGui","Đã gửi")][InlineData("GuiLoi","Gửi lỗi")]
    public async Task EmailStatesAndMaskedRecipientNeverClaimDeliveredOnEnqueue(string status,string label) {
        var fake=new Fake{Persisted=true,Current=Saved with{Status=status,DeliveredAt=status=="DaGui"?DateTimeOffset.UtcNow:null,
            Attempts=1,LastErrorCode=status=="GuiLoi"?"providerUnavailable":null,CanRetry=status=="GuiLoi"}};
        var vm=await Ready(fake);Assert.Equal(label,vm.StatusLabel);Assert.Equal("k***@example.invalid",vm.MaskedRecipient);
        Assert.Equal(status=="GuiLoi",vm.DeliveryRetryCommand.CanExecute(null));
        if(status=="ChoGui")Assert.DoesNotContain("Đã gửi",vm.Feedback);
        if(status=="GuiLoi"){fake.Current=null;vm.DeliveryRetryCommand.Execute(null);await Task.Yield();Assert.Equal(1,fake.Posts);Assert.Equal("Đang gửi",vm.StatusLabel);Assert.Equal(1,fake.ExpectedAttempts);}
    }
    [Fact] public async Task UncertainDeliveryCannotRetryAndHidesRawError() {
        var fake=new Fake{Persisted=true,Current=Saved with{Status="GuiLoi",LastErrorCode="deliveryUncertain"}};
        var vm=await Ready(fake);Assert.False(vm.DeliveryRetryCommand.CanExecute(null));Assert.DoesNotContain("deliveryUncertain",vm.DeliveryError);
    }
    [Theory]
    [InlineData("TMinus48","Nhắc lịch T−48h")][InlineData("TMinus24","Nhắc lịch T−24h")]
    [InlineData("DPlus3","Liên hệ sắp xếp lại D+3")][InlineData("DPlus7","Liên hệ sắp xếp lại D+7")]
    public async Task AutomatedHistoryParsesLabelsAndSystemOrigin(string kind,string label) {
        var row=Saved with{Kind=kind,RecordedBy=null,Status="DaGui",DeliveredAt=DateTimeOffset.UtcNow};
        var handler=new Handler{DetailOverride=Detail() with{History=[row]}};
        var detail=await new ReminderApiService(await Login(handler)).GetAsync(3);
        var history=Assert.Single(detail.History!);Assert.Equal(label,history.KindLabel);Assert.Equal("Tự động bởi hệ thống",history.RecorderLabel);
        Assert.Equal("Đã gửi",history.StatusLabel);Assert.False(history.CanRetry);Assert.Contains("UTC+07",history.TimeLabel);
    }
    [Theory][InlineData("unknown")][InlineData("appointment")][InlineData("fakeEmployee")][InlineData("status")]
    public async Task InvalidAutomatedHistoryFailsClosed(string invalid) {
        var row=Saved with{Kind="DPlus3",RecordedBy=null};
        row=invalid switch {"unknown"=>row with{Kind="FutureKind"},"appointment"=>row with{AppointmentId=99},
            "fakeEmployee"=>row with{RecordedBy=new(7,"fake")},_=>row with{Status="Unknown"}};
        var handler=new Handler{DetailOverride=Detail() with{History=[row]}};
        var service=new ReminderApiService(await Login(handler));
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(()=>service.GetAsync(3));
    }
    [Fact] public async Task HistoryRetryUsesExactDeliveryAndReconcilesWithoutCreatingManualEmail() {
        var row=Saved with{Id=19,Kind="DPlus3",RecordedBy=null,Status="GuiLoi",LastErrorCode="providerUnavailable",CanRetry=true,Attempts=1};
        var fake=new Fake{DetailOverride=Detail() with{CanRecord=false,History=[row]}};
        var vm=await Ready(fake);Assert.Equal(row,Assert.Single(vm.History));Assert.True(vm.HistoryRetryCommand.CanExecute(row));
        Assert.Equal("Lịch sử email bên dưới",vm.StatusLabel);Assert.Contains("từng email",vm.Feedback);
        vm.HistoryRetryCommand.Execute(row);await Task.Yield();
        Assert.Equal(19,fake.DeliveryId);Assert.Equal(1,fake.ExpectedAttempts);Assert.Equal(1,fake.Posts);Assert.Equal(2,fake.Gets);
        Assert.False(vm.HistoryRetryCommand.CanExecute(row with{CanRetry=false}));
    }
    private sealed class Fake:IReminderService
    {
        public TaskCompletionSource<AppointmentRecord[]>? ListGate;
        public Task<AppointmentRecord[]> ListAsync(string search,int page){if(ListGate!=null)return ListGate.Task;if(GetError!=null)throw new AppointmentApiException(GetError,"Safe error");return Task.FromResult(Persisted?Array.Empty<AppointmentRecord>():new[]{Appointment});}
        public ReminderDetail? DetailOverride;
        public ReminderRecord? Current;public int ExpectedAttempts;public int? DeliveryId;
        public Task<ReminderRecord> RetryAsync(int id,int expectedAttempts,int? deliveryId=null){ExpectedAttempts=expectedAttempts;DeliveryId=deliveryId;return RecordAsync(id);}
        public int Gets,Posts;public bool Persisted;public string? Error,GetError;public List<int> PostedIds=[];
        public TaskCompletionSource<ReminderDetail>? GetGate;public TaskCompletionSource<ReminderRecord>? PostGate;
        public Task<ReminderDetail> GetAsync(int id){Gets++;if(GetGate!=null)return GetGate.Task;if(GetError!=null)throw new AppointmentApiException(GetError,"Safe error");return Task.FromResult(DetailOverride??(Current==null?Detail(Persisted):Detail(true) with{Reminder=Current}));}
        public Task<ReminderRecord> RecordAsync(int id){Posts++;PostedIds.Add(id);if(PostGate!=null)return PostGate.Task;if(Error!=null)throw new AppointmentApiException(Error,"Safe error");Persisted=true;return Task.FromResult(Saved);}
    }
    private sealed class Handler:HttpMessageHandler
    {
        public ReminderDetail? DetailOverride;
        public string Title="LeTan",Failure="",Body="",LastPath="";public bool Grant=true;public int Posts,Refreshes;public HttpStatusCode Status=HttpStatusCode.Created;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct){
            var path=request.RequestUri!.AbsolutePath;
            if(path.EndsWith("/login")||path.EndsWith("/refresh")){if(path.EndsWith("/refresh"))Refreshes++;return Json(new{accessToken="access",refreshToken="refresh"});}
            if(path.EndsWith("/me"))return Json(new{accountType="employee",employee=new{title=Title},permissions=Grant?new[]{"QuanLyLichHen"}:[],capabilities=new { appointments=Title=="LeTan" && Grant }});
            if(path.EndsWith("/logout"))return new(HttpStatusCode.NoContent);
            if(request.Method==HttpMethod.Post){Posts++;LastPath=path;Body=await request.Content!.ReadAsStringAsync(ct);if(Failure=="network")throw new HttpRequestException();if(Failure=="bad201")return Json(new{},HttpStatusCode.Created);
                return Status==HttpStatusCode.Created?Json(Saved,Status):Json(new{code=Status==HttpStatusCode.Conflict?"reminderAlreadyRecorded":"validation",detail="SQL PRIVATE"},Status);}
            if(path.EndsWith("/reminders/email-queue")){LastPath=request.RequestUri.PathAndQuery;return Json(new{items=new[]{Appointment}});}
            return Json(DetailOverride??Detail());
        }
        private static HttpResponseMessage Json(object value,HttpStatusCode status=HttpStatusCode.OK)=>new(status){Content=JsonContent.Create(value)};
    }
}
