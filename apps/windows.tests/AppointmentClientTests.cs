using System.Net;
using System.Net.Http.Json;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class AppointmentClientTests
{
    [Theory]
    [InlineData("LeTan",true,true)]
    [InlineData("LeTan",false,false)]
    [InlineData("Admin",true,false)]
    [InlineData("ThuNgan",true,false)]
    [InlineData("BacSi",true,false)]
    [InlineData("DieuDuong",true,false)]
    public async Task CurrentTitleAndPermissionGateAllCalls(string title,bool grant,bool allowed)
    {
        var handler=new Handler {Title=title,Grant=grant};var identity=await Login(handler);var fake=new Fake();var vm=new AppointmentViewModel(fake,identity);
        await vm.LoadAsync();await vm.NewAsync();Assert.Equal(allowed,vm.HasAccess);Assert.Equal(allowed?1:0,fake.ListCalls);Assert.Equal(allowed,vm.IsCreating);
    }

    [Fact]
    public async Task ServiceUsesLineIdOffsetAndExactReconciliationFilter()
    {
        var handler=new Handler();var api=new AppointmentApiService(await Login(handler));
        var source=await api.GetSourceAsync(7);Assert.Equal(12,source.Items[0].Id);Assert.Equal(99,source.Items[0].ServiceId);
        var rows=await api.GetSourcesAsync(2);Assert.Single(rows);
        var saved=await api.CreateAsync(Request);Assert.Equal(Request.ScheduledAt,saved.ScheduledAt);
        Assert.Contains("\"registrationItemId\":12",handler.Body);Assert.DoesNotContain("recipientId",handler.Body);
        await api.ListAsync(1,Request);Assert.Contains("registrationItemId=12",handler.Paths.Last());Assert.Contains("scheduledAt=",handler.Paths.Last());
        Assert.Equal(1,handler.PostCount);Assert.Equal(0,handler.RefreshCount);
    }
    [Theory]
    [InlineData(400,"validation")]
    [InlineData(401,"invalidSession")]
    [InlineData(403,"permissionDenied")]
    [InlineData(409,"appointmentAlreadyExists")]
    [InlineData(503,"outcomeUnknown")]
    [InlineData(500,"outcomeUnknown")]
    public async Task PostIsNeverReplayedAndErrorsNeverExposeServerText(int status,string expected)
    {
        var handler=new Handler {PostStatus=(HttpStatusCode)status};var api=new AppointmentApiService(await Login(handler));
        var error=await Assert.ThrowsAsync<AppointmentApiException>(()=>api.CreateAsync(Request));Assert.Equal(expected,error.Code);Assert.DoesNotContain("SQL token",error.Message);Assert.Equal(1,handler.PostCount);Assert.Equal(0,handler.RefreshCount);
    }
    [Theory]
    [InlineData("network","outcomeUnknown")]
    [InlineData("timeout","outcomeUnknown")]
    [InlineData("invalid201","recordedUnreadable")]
    public async Task TransportAndUnreadableSuccessRequireGet(string failure,string expected)
    {
        var handler=new Handler {Failure=failure};var api=new AppointmentApiService(await Login(handler));
        Assert.Equal(expected,(await Assert.ThrowsAsync<AppointmentApiException>(()=>api.CreateAsync(Request))).Code);Assert.Equal(1,handler.PostCount);
    }
    [Fact]
    public async Task CreateModesValidationReadbackAndDraftResetWorkWithoutSample()
    {
        var fake=new Fake();var vm=await Ready(fake);Assert.Equal(Source.Recipient,vm.Source!.Recipient);Assert.False(vm.CanSave);
        vm.SelectedItem=vm.Items[0];vm.Time="bad";vm.Location="[DEMO] room";Assert.False(vm.CanSave);
        vm.Time="06:00";Assert.True(vm.CanSave); // No invented opening hours.
        await vm.SaveAsync();Assert.Equal(1,fake.PostCalls);Assert.False(vm.IsCreating);Assert.NotNull(vm.Detail);
        await vm.LoadAsync();vm.Selected=Assert.Single(vm.Appointments);await vm.LoadDetailAsync();Assert.NotNull(vm.Detail);
        await vm.NewAsync();Assert.Null(vm.Source);Assert.Null(vm.SelectedItem);Assert.Equal("",vm.Location);Assert.Null(vm.Detail);
    }
    [Fact]
    public async Task DirectCodeLookupSynchronizesSelectedRegistrationAndNeverKeepsPreviousLine()
    {
        var vm=await Ready(new Fake());Fill(vm);vm.SourceCode="DK-0008";await vm.FindSourceAsync();
        Assert.Equal(8,vm.Source!.Id);Assert.Equal(8,vm.SelectedSource!.Id);Assert.Equal("DK-0008",vm.SourceCode);Assert.Null(vm.SelectedItem);Assert.False(vm.CanSave);Assert.Equal("[DEMO] room",vm.Location);
        Assert.Equal(vm.Items[0].Label,vm.Items[0].ToString());Assert.Equal(vm.SelectedSource.Display,vm.SelectedSource.ToString());
    }
    [Fact]
    public async Task FailedPostKeepsDraftAndDoubleClickDoesNotPostTwice()
    {
        var fake=new Fake {PostGate=new()};var vm=await Ready(fake);Fill(vm);var save=vm.SaveAsync();
        Assert.True(vm.IsBusy);Assert.False(vm.CanSave);await vm.SaveAsync();Assert.Equal(1,fake.PostCalls);
        fake.PostGate.SetException(new AppointmentApiException("validation","Safe message"));await save;
        Assert.Equal("[DEMO] room",vm.Location);Assert.NotNull(vm.SelectedItem);Assert.True(vm.CanSave);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnknownOutcomeLocksExactDraftUntilGetAndRetriesOnlyIdenticalRequest(bool committed)
    {
        var fake=new Fake {Error="outcomeUnknown"};var vm=await Ready(fake);Fill(vm);await vm.SaveAsync();
        Assert.True(vm.HasPending);Assert.False(vm.CanSave);Assert.False(vm.CanEdit);await vm.NewAsync();await vm.SaveAsync();Assert.Equal(1,fake.PostCalls);
        var previous=vm.Location;vm.Location="other";Assert.Equal(previous,vm.Location);
        fake.FailGet=true;await vm.ReconcileAsync();Assert.False(vm.RetryCommand.CanExecute(null));
        fake.FailGet=false;fake.Persisted=committed;await vm.ReconcileAsync();Assert.Equal(!committed,vm.HasPending);
        if(committed){Assert.NotNull(vm.Detail);Assert.False(vm.RetryCommand.CanExecute(null));}
        else{Assert.True(vm.RetryCommand.CanExecute(null));var original=fake.Requests.Single();fake.Error=null;await vm.RetryAsync();Assert.Equal(original,fake.Requests.Last());Assert.Equal(2,fake.PostCalls);}
    }
    [Fact]
    public async Task Known201NeverRetriesEvenWhenGetDoesNotSeeRowYet()
    {
        var fake=new Fake {Error="recordedUnreadable"};var vm=await Ready(fake);Fill(vm);await vm.SaveAsync();await vm.ReconcileAsync();Assert.True(vm.HasPending);Assert.False(vm.RetryCommand.CanExecute(null));Assert.Equal(1,fake.PostCalls);
    }
    [Fact]
    public async Task SessionChangeClearsDraftAndDropsLateReadAndPost()
    {
        var handler=new Handler();var identity=await Login(handler);var fake=new Fake();var vm=await Ready(fake,identity);Fill(vm);
        fake.PostGate=new();var saving=vm.SaveAsync();await identity.LogoutAsync();fake.PostGate.SetResult(Saved);await saving;
        Assert.Empty(vm.Appointments);Assert.Null(vm.Source);Assert.Equal("",vm.Location);Assert.False(vm.HasAccess);Assert.Null(vm.Detail);
        await identity.LoginAsync("other","synthetic");Assert.False(vm.HasAccess);
        var api=new AppointmentApiService(identity);await identity.LogoutAsync();await identity.LoginAsync("other","synthetic");
        await Assert.ThrowsAsync<AppointmentApiException>(()=>api.CreateAsync(Request));Assert.Equal(0,handler.PostCount);
        var other=new AppointmentViewModel(fake,identity);fake.ListGate=new();var loading=other.LoadAsync();await identity.LogoutAsync();fake.ListGate.SetResult([Saved]);await loading;Assert.Empty(other.Appointments);
    }
    [Fact]
    public async Task PermissionRevocationClearsDataButNetworkErrorPreservesDraft()
    {
        var fake=new Fake();var vm=await Ready(fake);Fill(vm);vm.SourceCode="7";fake.SourceError=true;await vm.FindSourceAsync();Assert.Equal("[DEMO] room",vm.Location);
        fake.SourceError=false;await vm.FindSourceAsync();vm.SelectedItem=vm.Items[0];fake.Error="permissionDenied";await vm.SaveAsync();Assert.False(vm.HasAccess);Assert.Null(vm.Source);Assert.Empty(vm.Sources);Assert.Equal("",vm.Location);
    }
    private static readonly DateTimeOffset When=DateTimeOffset.UtcNow.AddDays(3).ToOffset(TimeSpan.FromHours(7));
    private static readonly AppointmentRequest Request=new(7,"vaccine",12,When,"[DEMO] room");
    private static readonly AppointmentSource Source=new(7,"DaDangKy",new(1,"[DEMO] Customer"),new(2,"[DEMO] Recipient"),[new(12,"vaccine",99,"[DEMO] Vaccine",2),new(12,"package",88,"[DEMO] Package",1)]);
    private static readonly AppointmentSourceOption Summary=new(7,Source.Customer,Source.Recipient);
    private static readonly AppointmentRecord Saved=new(1,7,Source.Customer,Source.Recipient,"vaccine",12,99,"[DEMO] Vaccine",When,"[DEMO] room","DaXepLich");
    private static async Task<IdentityApiService> Login(Handler handler){var identity=new IdentityApiService(new HttpClient(handler){BaseAddress=new Uri("http://localhost/")});Assert.True((await identity.LoginAsync("synthetic","synthetic")).Succeeded);return identity;}
    private static async Task<AppointmentViewModel> Ready(Fake fake,IdentityApiService? identity=null){var vm=new AppointmentViewModel(fake,identity??await Login(new Handler()));await vm.NewAsync();vm.SelectedSource=Summary;await vm.LoadSourceDetailAsync();return vm;}
    private static void Fill(AppointmentViewModel vm){vm.SelectedItem=vm.Items[0];vm.AppointmentDate=When.LocalDateTime.Date;vm.Time=When.LocalDateTime.ToString("HH:mm");vm.Location="[DEMO] room";}
    private sealed class Fake : IAppointmentService
    {
        public int PostCalls,ListCalls;public string? Error;public bool Persisted,FailGet,SourceError;public List<AppointmentRequest> Requests=[];
        public TaskCompletionSource<AppointmentRecord>? PostGate;public TaskCompletionSource<AppointmentRecord[]>? ListGate;
        public Task<AppointmentSourceOption[]> GetSourcesAsync(int page)=>Task.FromResult(new[]{Summary});
        public Task<AppointmentSource> GetSourceAsync(int id){if(SourceError)throw new HttpRequestException();return Task.FromResult(Source with {Id=id});}
        public Task<AppointmentRecord[]> ListAsync(int page,AppointmentRequest? match=null){ListCalls++;if(FailGet)throw new HttpRequestException();if(ListGate!=null)return ListGate.Task;return Task.FromResult(Persisted?new[]{Saved with {ScheduledAt=Requests.LastOrDefault()?.ScheduledAt??When}}:Array.Empty<AppointmentRecord>());}
        public Task<AppointmentRecord> GetAsync(int id)=>Task.FromResult(Saved);
        public Task<AppointmentRecord> CreateAsync(AppointmentRequest request){PostCalls++;Requests.Add(request);if(PostGate!=null)return PostGate.Task;if(Error!=null)throw new AppointmentApiException(Error,"Safe message");Persisted=true;return Task.FromResult(Saved with {ScheduledAt=request.ScheduledAt});}
    }
    private sealed class Handler : HttpMessageHandler
    {
        public string Title="LeTan",Failure="";public bool Grant=true;public int PostCount,RefreshCount;public string Body="";public List<string> Paths=[];public HttpStatusCode PostStatus=HttpStatusCode.Created;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            var path=request.RequestUri!.PathAndQuery;Paths.Add(path);
            if(path.EndsWith("/login")||path.EndsWith("/refresh")){if(path.EndsWith("/refresh"))RefreshCount++;return Json(new {accessToken="access",refreshToken="refresh"});}
            if(path.EndsWith("/logout"))return new(HttpStatusCode.NoContent);
            if(path.EndsWith("/me"))return Json(new {accountType="employee",employee=new {title=Title},permissions=Grant?new[]{"QuanLyLichHen"}:Array.Empty<string>()});
            if(request.Method==HttpMethod.Post){PostCount++;Body=await request.Content!.ReadAsStringAsync(ct);if(Failure=="network")throw new HttpRequestException();if(Failure=="timeout")throw new TaskCanceledException();if(Failure=="invalid201")return Json(new {},HttpStatusCode.Created);return PostStatus==HttpStatusCode.Created?Json(Saved,PostStatus):Json(new {code=PostStatus==HttpStatusCode.Conflict?"appointmentAlreadyExists":"validation",title="SQL token"},PostStatus);}
            if(path.Contains("appointments?"))return Json(new {items=new[]{Saved}});
            if(path.Contains("registrations?"))return Json(new {items=new[]{Summary}});
            if(path.EndsWith("registrations/7"))return Json(Source);
            return Json(Saved);
        }
        private static HttpResponseMessage Json(object body,HttpStatusCode status=HttpStatusCode.OK)=>new(status){Content=JsonContent.Create(body)};
    }
}
