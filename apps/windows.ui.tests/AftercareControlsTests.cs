using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using TiemChung.Desktop.ViewsV2;
using Xunit;

namespace TiemChung.Windows.Ui.Tests;

public sealed partial class ComboBoxTemplateTests
{
    [Theory]
    [InlineData(800,560)][InlineData(1200,760)]
    public async Task AftercareMilestoneBindsWaitingReadyAndPreservesDraft(int width,int height)
    {
        var identity=new IdentityApiService(new HttpClient(new AftercareLogin()){BaseAddress=new("http://test/")});
        Assert.True((await identity.LoginAsync("nurse01","synthetic-password")).Succeeded);
        await host.Run(app=>{
            var service=new AftercareBindingService();var vm=new AftercareViewModel(service,identity);
            vm.LoadAsync().GetAwaiter().GetResult();vm.Selected=Assert.Single(vm.Queue);vm.LoadDetailAsync().GetAwaiter().GetResult();
            vm.Condition="Đang theo dõi";vm.Severity="Đang ghi nhận";vm.Treatment="Tiếp tục theo dõi tại cơ sở";
            var view=new AftercareView{DataContext=vm};
            using var source=new HwndSource(new HwndSourceParameters("Aftercare milestone binding"){Width=width,Height=height,WindowStyle=0}){RootVisual=view};
            view.Measure(new Size(width,height));view.Arrange(new Rect(0,0,width,height));view.UpdateLayout();
            view.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
            var save=Assert.Single(VisualChildren<Button>(view),x=>Equals(x.Content,"Xem lại và ghi theo dõi"));
            Assert.False(save.IsEnabled);
            var text=string.Join("\n",VisualChildren<TextBlock>(view).Select(x=>x.Text));
            Assert.Contains("Đang theo dõi sau tiêm",text);Assert.Contains("Có thể hoàn tất ghi nhận sau:",text);
            service.Ready=true;vm.RefreshDetailAsync().GetAwaiter().GetResult();
            view.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);view.UpdateLayout();
            Assert.True(save.IsEnabled);
            Assert.Contains(VisualChildren<TextBlock>(view),x=>x.Text=="Đã đến mốc hoàn tất ghi nhận");
            Assert.DoesNotContain(VisualChildren<TextBlock>(view),x=>x.Text=="Đang theo dõi sau tiêm");
            Assert.Contains(VisualChildren<TextBox>(view),x=>x.Text=="Tiếp tục theo dõi tại cơ sở");
            Assert.DoesNotContain("FollowUpObservation {",string.Join("\n",VisualChildren<TextBlock>(view).Select(x=>x.Text)));
        });
    }
    private sealed class AftercareLogin:HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)=>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=request.RequestUri!.AbsolutePath.EndsWith("/login")
                ?JsonContent.Create(new{accessToken="access",refreshToken="refresh"})
                :JsonContent.Create(new{accountType="employee",employee=new{title="DieuDuong"},capabilities=new{aftercare=true}})});
    }
    private sealed class AftercareBindingService:IAftercareService
    {
        public bool Ready;
        private readonly VaccinationRecord _shot=new(1,1,1,1,new(1,"Nguyễn Test A"),new(1,"Vắc xin thử"),"LOT-01","Vị trí thử",new(1,"Điều dưỡng Test"),DateTimeOffset.UtcNow.AddMinutes(-1),"DaTiem");
        private FollowUpObservation Observation=>new(_shot.Id,_shot.AdministeredAt.AddMinutes(30),Ready,Ready?null:"observationPeriodNotCompleted");
        public Task<FollowUpPendingItem[]> ListAsync(int page,string search)=>Task.FromResult<FollowUpPendingItem[]>([new(_shot,Observation)]);
        public Task<FollowUpDetail> GetAsync(int id)=>Task.FromResult(new FollowUpDetail(_shot,null,Observation));
        public Task<FollowUpRecord> RecordAsync(int id,FollowUpRequest request)=>throw new NotSupportedException();
    }
}
