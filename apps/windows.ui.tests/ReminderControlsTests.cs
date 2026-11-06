using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Threading;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using TiemChung.Desktop.ViewsV2;
using Xunit;

namespace TiemChung.Windows.Ui.Tests;

public sealed partial class ComboBoxTemplateTests
{
    [Fact]
    public async Task ReminderQueueRendersLabelsSelectsPreviewAndRetainsResultAfterRemoval()
    {
        using var http=new HttpClient(new ReminderResponses()){BaseAddress=new Uri("http://localhost")};
        var identity=new IdentityApiService(http);
        Assert.True((await identity.LoginAsync("synthetic","synthetic")).Succeeded);
        await host.Run(_=>{
            var model=new ReminderViewModel(identity:identity);
            model.LoadQueueAsync().GetAwaiter().GetResult();
            var view=new ReminderQueueView{DataContext=model};
            using var source=new HwndSource(new HwndSourceParameters("Reminder regression"){Width=1000,Height=650,WindowStyle=0}){RootVisual=view};
            view.Measure(new Size(1000,650));view.Arrange(new Rect(0,0,1000,650));view.UpdateLayout();
            var list=Assert.Single(VisualChildren<ListView>(view));
            var row=Assert.Single(model.Queue);
            Assert.Equal(row.Label,AutomationProperties.GetName((ListViewItem)list.ItemContainerGenerator.ContainerFromItem(row)));
            list.SetCurrentValue(Selector.SelectedItemProperty,row);
            view.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);view.UpdateLayout();
            Assert.Contains(VisualChildren<TextBlock>(view),t=>t.Text==ReminderResponses.Row.Label);
            Assert.Contains(VisualChildren<TextBlock>(view),t=>t.Text=="Nội dung nhắc lịch thử nghiệm");
            Assert.DoesNotContain(VisualChildren<TextBlock>(view),t=>t.Text.Contains("AppointmentRecord {")||t.Text.Contains("LogMoPhong")||t.Text.Contains("GET"));
            model.ConfirmCommand.Execute(null);Assert.False(list.IsEnabled);
            model.CancelCommand.Execute(null);Assert.Null(model.Result);
            model.ConfirmCommand.Execute(null);model.RecordAsync().GetAwaiter().GetResult();
            view.UpdateLayout();Assert.Single(model.Queue);Assert.NotNull(model.Result);
            Assert.Equal("Nội dung nhắc lịch thử nghiệm",model.Content);
            Assert.Contains(VisualChildren<TextBlock>(view),t=>t.Text=="Người ghi: Nguyễn Test B");
            Assert.False(Assert.Single(VisualChildren<Button>(view),b=>Equals(b.Content,"Gửi email nhắc lịch")).IsEnabled);
        });
    }
    private sealed class ReminderResponses:HttpMessageHandler
    {
        internal static readonly AppointmentRecord Row=new(3,1,new(1,"Nguyễn Test A"),new(2,"Nguyễn Test C"),"vaccine",1,1,"Vắc xin kiểm thử",DateTimeOffset.UtcNow.AddDays(1),"Cơ sở kiểm thử","DaXepLich");
        private bool _saved;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            var record=new ReminderRecord(7,3,"Email","ChoGui","Nội dung nhắc lịch thử nghiệm",DateTimeOffset.UtcNow,new(4,"Nguyễn Test B"),"k***@example.invalid","[VNVC] Nhắc lịch tiêm chủng LH-0003");
            var path=request.RequestUri!.AbsolutePath;
            object body;
            var status=HttpStatusCode.OK;
            if(path.EndsWith("/login"))body=new{accessToken="test",refreshToken="test"};
            else if(path.EndsWith("/me"))body=new{accountType="employee",employee=new{title="LeTan"},permissions=new[]{"QuanLyLichHen"},capabilities=new{appointments=true}};
            else if(path.EndsWith("/email-queue"))body=new{items=_saved?Array.Empty<AppointmentRecord>():new[]{Row}};
            else if(request.Method==HttpMethod.Post){_saved=true;body=record;status=HttpStatusCode.Created;}
            else body=new ReminderDetail(Row,_saved?null:record.Content,!_saved,_saved?record:null,record.MaskedRecipient,record.Subject);
            return Task.FromResult(new HttpResponseMessage(status){Content=JsonContent.Create(body)});
        }
    }
}
