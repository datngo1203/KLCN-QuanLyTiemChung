using System.Net;
using System.Net.Http.Json;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class AppointmentClientTests
{
    [Theory][InlineData(true)][InlineData(false)]
    public async Task MidnightOnlyPrefillsHourWhenItWasExplicitlySupplied(bool supplied)
    {
        var date = DateTime.SpecifyKind(DateTime.Today.AddDays(5), DateTimeKind.Unspecified);
        var fake = new Fake { Option = Summary with { PreferredDate = DateOnly.FromDateTime(date), PreferredAt = date, PreferredTimeSpecified = supplied } };
        var vm = await Ready(fake);
        var local = new DateTimeOffset(date, TimeSpan.FromHours(7)).ToLocalTime();
        Assert.Equal(supplied ? local.Date : date, vm.AppointmentDate);
        Assert.Equal(supplied ? local.ToString("HH:mm") : "09:00", vm.Time);
        if (supplied) Assert.Contains("00:00 giờ Việt Nam", vm.Source!.PreferredDateLabel);
        else { Assert.Contains("Chưa ghi nhận giờ", vm.Source!.PreferredDateLabel); Assert.DoesNotContain("00:00", vm.Source.PreferredDateLabel); }
        vm.SelectedItem = vm.Items[0]; vm.Time = "16:20"; await vm.SaveAsync();
        Assert.Equal("16:20", fake.Requests.Single().ScheduledAt.ToLocalTime().ToString("HH:mm"));
    }

    [Fact]
    public async Task PreferredFacilityWallClockPrefillsBothFieldsAndReceptionCanChangeIt()
    {
        var preferred = DateTime.Today.AddDays(5).AddHours(14).AddMinutes(45);
        var fake = new Fake { Option = Summary with { PreferredAt = preferred, PreferredTimeSpecified = true } };
        var vm = await Ready(fake);
        var local = new DateTimeOffset(DateTime.SpecifyKind(preferred, DateTimeKind.Unspecified), TimeSpan.FromHours(7)).ToLocalTime();
        Assert.Equal(local.Date, vm.AppointmentDate); Assert.Equal(local.ToString("HH:mm"), vm.Time);
        Assert.Contains("14:45", vm.Source!.PreferredDateLabel);
        vm.SelectedItem = vm.Items[0]; vm.Time = "16:20";
        await vm.SaveAsync();
        Assert.Equal("16:20", fake.Requests.Single().ScheduledAt.ToLocalTime().ToString("HH:mm"));
        Assert.Equal(preferred, fake.Option.PreferredAt);
    }

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
        var rows=await api.GetSourcesAsync(2);Assert.Equal(Summary, Assert.Single(rows));
        Assert.Equal(Summary, source.Source);
        Assert.Contains("/api/v1/appointments/sources/7", handler.Paths);
        Assert.Contains("/api/v1/appointments/sources?page=2&pageSize=25", handler.Paths);
        var saved=await api.CreateAsync(Request);Assert.Equal(Request.ScheduledAt,saved.ScheduledAt);
        Assert.Contains("\"registrationItemId\":12",handler.Body);Assert.DoesNotContain("recipientId",handler.Body);Assert.DoesNotContain("location",handler.Body);
        Assert.Equal(Summary.Facility, saved.Location);
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
        var fake=new Fake();var vm=await Ready(fake);Assert.Equal(Summary.Recipient,vm.Source!.Recipient);Assert.False(vm.CanSave);
        vm.SelectedItem=vm.Items[0];vm.Time="bad";Assert.False(vm.CanSave);
        vm.Time="06:00";Assert.True(vm.CanSave); // No invented opening hours.
        await vm.SaveAsync();Assert.Equal(1,fake.PostCalls);Assert.False(vm.IsCreating);Assert.NotNull(vm.Detail);
        await vm.LoadAsync();vm.Selected=Assert.Single(vm.Appointments);await vm.LoadDetailAsync();Assert.NotNull(vm.Detail);
        await vm.NewAsync();Assert.Null(vm.Source);Assert.Null(vm.SelectedItem);Assert.Null(vm.Detail);
    }
    [Fact]
    public async Task DirectCodeLookupSynchronizesSelectedRegistrationAndNeverKeepsPreviousLine()
    {
        var vm=await Ready(new Fake());Fill(vm);vm.SourceCode="DK-0008";await vm.FindSourceAsync();
        Assert.Equal(8,vm.Source!.RegistrationId);Assert.Equal(8,vm.SelectedSource!.RegistrationId);Assert.Equal("DK-0008",vm.SourceCode);Assert.Null(vm.SelectedItem);Assert.False(vm.CanSave);Assert.Equal(Summary.Facility,vm.Source!.Facility);
        Assert.Equal(Summary.PreferredDate!.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), vm.AppointmentDate);
        Assert.Equal(vm.Items[0].Label,vm.Items[0].ToString());
    }

    [Theory]
    [InlineData("paid", true, "Sẵn sàng xếp lịch · Đã thanh toán")]
    [InlineData("center", true, "Sẵn sàng xếp lịch · Thanh toán tại trung tâm")]
    [InlineData("zero", true, "Sẵn sàng xếp lịch · Không phát sinh khoản thu")]
    [InlineData("unpaid", false, "Chờ thanh toán")]
    [InlineData("pending", false, "Chờ xác nhận thanh toán")]
    public async Task ReadinessAndPreferredDateDriveDraft(string code, bool ready, string label)
    {
        var fake = new Fake { Option = Summary with { CanSchedule = ready, ReadinessCode = code } };
        var vm = await Ready(fake);
        Assert.Equal(Summary.PreferredDate!.Value.ToDateTime(TimeOnly.MinValue), vm.AppointmentDate);
        Assert.Equal(label, vm.Source!.ReadinessLabel);
        Assert.Null(typeof(AppointmentViewModel).GetProperty("Location"));
        vm.SelectedItem = vm.Items[0];
        Assert.Equal(ready, vm.CanSave);
        vm.AppointmentDate = DateTime.Today.AddDays(8);
        vm.Time = "10:15";
        Assert.Equal(ready, vm.CanSave);
        await vm.SaveAsync();
        Assert.Equal(ready ? 1 : 0, fake.PostCalls);
        if (ready) Assert.Equal(DateTime.Today.AddDays(8), fake.Requests.Single().ScheduledAt.LocalDateTime.Date);
    }

    [Fact]
    public async Task ReloadChangesBlockedToReadyAndKeepsEditedTimeAndLine()
    {
        var fake = new Fake { Option = Summary with { CanSchedule = false, ReadinessCode = "unpaid" } };
        var vm = await Ready(fake);
        Fill(vm);
        var date = vm.AppointmentDate;
        Assert.False(vm.CanSave);
        fake.Option = Summary;
        vm.RefreshSourceCommand.Execute(null);
        Assert.True(vm.CanSave);
        Assert.Equal(date, vm.AppointmentDate);
        Assert.Equal(Summary, Assert.Single(vm.Sources));
    }

    [Theory]
    [InlineData("paymentDataConflict", "Dữ liệu thanh toán cần được đối chiếu trước khi xếp lịch.")]
    [InlineData("appointmentPaymentRequired", "Đăng ký chưa đủ điều kiện xếp lịch do thanh toán chưa hoàn tất hoặc đang chờ xác nhận.")]
    public async Task PaymentGateShowsBusinessWordingAndDisablesStaleDraft(string code, string message)
    {
        var handler = new Handler { PostStatus = HttpStatusCode.Conflict, ConflictCode = code };
        var identity = await Login(handler);
        var vm = new AppointmentViewModel(new AppointmentApiService(identity), identity);
        await vm.NewAsync(); vm.SelectedSource = Assert.Single(vm.Sources); await vm.LoadSourceDetailAsync();
        Fill(vm); await vm.SaveAsync();
        Assert.Equal(message, vm.Feedback);
        Assert.False(vm.CanSave);
        Assert.False(vm.HasPending);
        Assert.NotNull(vm.SelectedItem);
        Assert.True(vm.RefreshSourceCommand.CanExecute(null));
    }
    [Fact]
    public async Task FailedPostKeepsDraftAndDoubleClickDoesNotPostTwice()
    {
        var fake=new Fake {PostGate=new()};var vm=await Ready(fake);Fill(vm);var save=vm.SaveAsync();
        Assert.True(vm.IsBusy);Assert.False(vm.CanSave);await vm.SaveAsync();Assert.Equal(1,fake.PostCalls);
        fake.PostGate.SetException(new AppointmentApiException("validation","Safe message"));await save;
        Assert.Equal(Summary.Facility,vm.Source!.Facility);Assert.NotNull(vm.SelectedItem);Assert.True(vm.CanSave);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnknownOutcomeLocksExactDraftUntilGetAndRetriesOnlyIdenticalRequest(bool committed)
    {
        var fake=new Fake {Error="outcomeUnknown"};var vm=await Ready(fake);Fill(vm);await vm.SaveAsync();
        Assert.True(vm.HasPending);Assert.False(vm.CanSave);Assert.False(vm.CanEdit);await vm.NewAsync();await vm.SaveAsync();Assert.Equal(1,fake.PostCalls);
        var previous=vm.Time;vm.Time="07:00";Assert.Equal(previous,vm.Time);
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
        Assert.Empty(vm.Appointments);Assert.Null(vm.Source);Assert.False(vm.HasAccess);Assert.Null(vm.Detail);
        await identity.LoginAsync("other","synthetic");Assert.False(vm.HasAccess);
        var api=new AppointmentApiService(identity);await identity.LogoutAsync();await identity.LoginAsync("other","synthetic");
        await Assert.ThrowsAsync<AppointmentApiException>(()=>api.CreateAsync(Request));Assert.Equal(0,handler.PostCount);
        var other=new AppointmentViewModel(fake,identity);fake.ListGate=new();var loading=other.LoadAsync();await identity.LogoutAsync();fake.ListGate.SetResult([Saved]);await loading;Assert.Empty(other.Appointments);
    }
    [Fact]
    public async Task PermissionRevocationClearsDataButNetworkErrorPreservesDraft()
    {
        var fake=new Fake();var vm=await Ready(fake);Fill(vm);vm.SourceCode="7";fake.SourceError=true;var draftDate = vm.AppointmentDate; await vm.FindSourceAsync();Assert.Equal(draftDate,vm.AppointmentDate);Assert.False(vm.CanSave);
        fake.SourceError=false;await vm.FindSourceAsync();vm.SelectedItem=vm.Items[0];fake.Error="permissionDenied";await vm.SaveAsync();Assert.False(vm.HasAccess);Assert.Null(vm.Source);Assert.Empty(vm.Sources);
    }
    private static readonly DateTimeOffset When=DateTimeOffset.UtcNow.AddDays(3).ToOffset(TimeSpan.FromHours(7));
    private static readonly AppointmentRequest Request=new(7,"vaccine",12,When);
    private static readonly AppointmentSourceOption Summary = new(7, "DaDangKy", new(1,"Khách hàng thử"), new(2,"Người tiêm thử"),
        DateOnly.FromDateTime(DateTime.Today.AddDays(4)), "ThanhCong", "TrucTuyen", true, "paid", "Cơ sở VAC thử");
    private static readonly AppointmentSource Source = new(Summary,
        [new(12,"vaccine",99,"Vắc xin thử",2),new(12,"package",88,"Gói thử",1)]);
    private static readonly AppointmentRecord Saved=new(1,7,Summary.Customer,Summary.Recipient,"vaccine",12,99,"[DEMO] Vaccine",When,Summary.Facility,"DaXepLich");
    private static async Task<IdentityApiService> Login(Handler handler){var identity=new IdentityApiService(new HttpClient(handler){BaseAddress=new Uri("http://localhost/")});Assert.True((await identity.LoginAsync("synthetic","synthetic")).Succeeded);return identity;}
    private static async Task<AppointmentViewModel> Ready(Fake fake,IdentityApiService? identity=null){var vm=new AppointmentViewModel(fake,identity??await Login(new Handler()));await vm.NewAsync();vm.SelectedSource=Assert.Single(vm.Sources);await vm.LoadSourceDetailAsync();return vm;}
    private static void Fill(AppointmentViewModel vm){vm.SelectedItem=vm.Items[0];vm.AppointmentDate=When.LocalDateTime.Date;vm.Time=When.LocalDateTime.ToString("HH:mm");}
    [Theory][InlineData(false)][InlineData(true)]
    public async Task LifecycleConfirmsPersistsAndReplacesListRow(bool cancel)
    {
        var fake = new Fake { Persisted = true, Current = Saved with { Version = "AAAAAAAAAAE=", CanManage = true } };
        var vm = new AppointmentViewModel(fake, await Login(new Handler()));
        await vm.LoadAsync(); vm.Selected = vm.Appointments.Single(); await vm.LoadDetailAsync();
        Assert.True(vm.CanManage); string? preview = null;
        vm.Confirm = message => { preview = message; return false; };
        if (!cancel) { vm.BeginRescheduleCommand.Execute(null); vm.AppointmentDate = When.LocalDateTime.Date.AddDays(1); vm.Time = "10:30"; }
        await vm.MutateAsync(cancel); Assert.Equal(0, fake.MutationCalls);
        vm.Confirm = message => { preview = message; return true; };
        await vm.MutateAsync(cancel);
        Assert.Equal(1, fake.MutationCalls); Assert.NotNull(preview);
        Assert.Equal(vm.Detail, Assert.Single(vm.Appointments));
        if (cancel) { Assert.Equal("Đã hủy", vm.Detail!.StatusLabel); Assert.False(vm.CanManage); }
        else { Assert.Equal("10:30", vm.Detail!.ScheduledAt.ToLocalTime().ToString("HH:mm")); Assert.Contains("Hiện tại:", preview); Assert.Contains("Thời gian mới:", preview); }
        Assert.False(vm.NeedsMutationReload); Assert.False(vm.IsRescheduling);
    }

    [Theory][InlineData("outcomeUnknown")][InlineData("concurrencyConflict")][InlineData("clinicalAlreadyStarted")]
    public async Task LifecycleRequiresGetAfterUnknownOrStaleAndDoesNotBlindlyPostAgain(string code)
    {
        var fake = new Fake { Persisted = true, MutationError = code, Current = Saved with { Version = "AAAAAAAAAAE=", CanManage = true } };
        var vm = new AppointmentViewModel(fake, await Login(new Handler())) { Confirm = _ => true };
        await vm.LoadAsync(); vm.Selected = vm.Appointments.Single(); await vm.LoadDetailAsync();
        await vm.MutateAsync(true); Assert.True(vm.NeedsMutationReload); Assert.False(vm.CanManage);
        await vm.MutateAsync(true); Assert.Equal(1, fake.MutationCalls);
        fake.Current = fake.Current with { Status = "DaHuy", CanManage = false };
        await vm.ReconcileMutationAsync(); Assert.Equal(1, fake.MutationCalls);
        Assert.False(vm.NeedsMutationReload); Assert.Equal("Đã hủy", vm.Detail!.StatusLabel);
    }

    [Theory][InlineData("invalidSession")][InlineData("permissionDenied")]
    public async Task LifecycleClearsSessionDeniedState(string code)
    {
        var fake = new Fake { Persisted = true, MutationError = code, Current = Saved with { Version = "AAAAAAAAAAE=", CanManage = true } };
        var vm = new AppointmentViewModel(fake, await Login(new Handler())) { Confirm = _ => true };
        await vm.LoadAsync(); vm.Selected = vm.Appointments.Single(); await vm.LoadDetailAsync();
        await vm.MutateAsync(true); Assert.False(vm.HasAccess); Assert.Null(vm.Detail); Assert.Empty(vm.Appointments);
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task MutationTransportSendsOnlyVersionAndNewTimeAndNeverReplays(bool cancel)
    {
        var handler = new Handler { Failure = "network" }; var api = new AppointmentApiService(await Login(handler));
        var error = await Assert.ThrowsAsync<AppointmentApiException>(() => cancel ? api.CancelAsync(1, "AAAAAAAAAAE=") : api.RescheduleAsync(1, When, "AAAAAAAAAAE="));
        Assert.Equal("outcomeUnknown", error.Code); Assert.Equal(1, handler.PostCount); Assert.Equal(0, handler.RefreshCount);
        Assert.Contains("version", handler.Body); Assert.Equal(!cancel, handler.Body.Contains("scheduledAt"));
        Assert.DoesNotContain("registration", handler.Body); Assert.DoesNotContain("location", handler.Body);
    }

    private sealed class Fake : IAppointmentService
    {
        public AppointmentRecord Current = Saved;
        public string? MutationError; public int MutationCalls;
        public Task<AppointmentRecord> RescheduleAsync(int id, DateTimeOffset scheduledAt, string version) => Mutate(Current with { ScheduledAt = scheduledAt });
        public Task<AppointmentRecord> CancelAsync(int id, string version) => Mutate(Current with { Status = "DaHuy", CanManage = false });
        private Task<AppointmentRecord> Mutate(AppointmentRecord changed) {
            MutationCalls++; if (MutationError != null) throw new AppointmentApiException(MutationError, "Thông báo nghiệp vụ");
            Current = changed with { Version = "AAAAAAAAAAI=" }; return Task.FromResult(Current);
        }
        public AppointmentSourceOption Option = Summary;
        public int PostCalls,ListCalls;public string? Error;public bool Persisted,FailGet,SourceError;public List<AppointmentRequest> Requests=[];
        public TaskCompletionSource<AppointmentRecord>? PostGate;public TaskCompletionSource<AppointmentRecord[]>? ListGate;
        public Task<AppointmentSourceOption[]> GetSourcesAsync(int page)=>Task.FromResult(new[]{Option});
        public Task<AppointmentSource> GetSourceAsync(int id){if(SourceError)throw new HttpRequestException();return Task.FromResult(Source with { Source = Option with { RegistrationId = id, PreferredDate = Option.PreferredDate?.AddDays(id - 7) } });}
        public Task<AppointmentRecord[]> ListAsync(int page,AppointmentRequest? match=null){ListCalls++;if(FailGet)throw new HttpRequestException();if(ListGate!=null)return ListGate.Task;return Task.FromResult(Persisted?new[]{Saved with {ScheduledAt=Requests.LastOrDefault()?.ScheduledAt??When}}:Array.Empty<AppointmentRecord>());}
        public Task<AppointmentRecord> GetAsync(int id)=>Task.FromResult(Current);
        public Task<AppointmentRecord> CreateAsync(AppointmentRequest request){PostCalls++;Requests.Add(request);if(PostGate!=null)return PostGate.Task;if(Error!=null)throw new AppointmentApiException(Error,"Safe message");Persisted=true;return Task.FromResult(Saved with {ScheduledAt=request.ScheduledAt});}
    }
    private sealed class Handler : HttpMessageHandler
    {
        public string Title="LeTan",Failure="";public bool Grant=true;public int PostCount,RefreshCount;public string Body="";public List<string> Paths=[];public HttpStatusCode PostStatus=HttpStatusCode.Created;
        public string ConflictCode = "appointmentAlreadyExists";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            var path=request.RequestUri!.PathAndQuery;Paths.Add(path);
            if(path.EndsWith("/login")||path.EndsWith("/refresh")){if(path.EndsWith("/refresh"))RefreshCount++;return Json(new {accessToken="access",refreshToken="refresh"});}
            if(path.EndsWith("/logout"))return new(HttpStatusCode.NoContent);
            if(path.EndsWith("/me"))return Json(new {accountType="employee",employee=new {title=Title},permissions=Grant?new[]{"QuanLyLichHen"}:Array.Empty<string>(),capabilities=new { appointments=Title=="LeTan" && Grant }});
            if(request.Method==HttpMethod.Post){PostCount++;Body=await request.Content!.ReadAsStringAsync(ct);if(Failure=="network")throw new HttpRequestException();if(Failure=="timeout")throw new TaskCanceledException();if(Failure=="invalid201")return Json(new {},HttpStatusCode.Created);return PostStatus==HttpStatusCode.Created?Json(Saved,PostStatus):Json(new {code=PostStatus==HttpStatusCode.Conflict?ConflictCode:"validation",title="SQL token"},PostStatus);}
            if(path.Contains("appointments?"))return Json(new {items=new[]{Saved}});
            if(path.Contains("appointments/sources?"))return Json(new {items=new[]{Summary}});
            if(path.EndsWith("appointments/sources/7"))return Json(Source);
            return Json(Saved);
        }
        private static HttpResponseMessage Json(object body,HttpStatusCode status=HttpStatusCode.OK)=>new(status){Content=JsonContent.Create(body)};
    }
}
