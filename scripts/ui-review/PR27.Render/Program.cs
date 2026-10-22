using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TiemChung.Desktop;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using TiemChung.Desktop.ViewsV2;
using ShellWindow = TiemChung.Desktop.ViewsV2.MainWindow;


class Review
{
 static string output = Path.GetFullPath("docs/ui-review/pr27-replacement");
 static int checks;
 static void Check(bool value, string name) { if(!value) throw new Exception(name); Console.WriteLine("PASS " + name); checks++; }
 static void Pump() { var frame=new DispatcherFrame(); var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(120)}; timer.Tick+=(s,e)=>{timer.Stop();frame.Continue=false;}; timer.Start(); Dispatcher.PushFrame(frame); }
 static void Capture(FrameworkElement view, string name, int width=1366, int height=768)
 {
  if(view is Window w){ ((FrameworkElement)w.Content).Width=double.NaN; ((FrameworkElement)w.Content).Height=double.NaN; w.Width=width; w.Height=height; w.Measure(new Size(width,height)); w.Arrange(new Rect(0,0,width,height)); w.UpdateLayout(); view=(FrameworkElement)w.Content; } view.Width=width; view.Height=height; view.Measure(new Size(width,height)); view.Arrange(new Rect(0,0,width,height)); view.UpdateLayout(); Pump();
  var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32); bitmap.Render(view);
  var encoder=new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
  using(var file=File.Create(Path.Combine(output,name+".png"))) encoder.Save(file);
  Check(new FileInfo(Path.Combine(output,name+".png")).Length < 500*1024, name+" render <500KB");
 }
 [STAThread] static void Main()
 {
  Directory.CreateDirectory(output);
  var app=new Application(); app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
  app.Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ThemesDictionary());
  app.Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ControlsDictionary());
  foreach(var file in new[]{"Colors","Typography","Controls","Buttons","Cards","BadgesAlerts","Navigation"}) app.Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("/TiemChung.Desktop;component/Themes/"+file+".xaml",UriKind.Relative)});
  var shell=new ShellViewModel("reviewer"); var window=new ShellWindow(shell); window.Show(); Pump();
  foreach(var id in new[]{"A01","A02","A04","A05","A07","A11","A12","A14"})
  {
   if(id=="A14") shell.OpenPasswordCommand.Execute(null);
   else shell.NavigateCommand.Execute(new NavigationItem(id,id));
   Capture(window,id+"-normal");
   Check(shell.SelectedScreen.Id==id,id+" navigation");
  }
  var login=new LoginWindow(new LoginViewModel(new SampleAuthService())); Capture(login,"login-normal"); login.Close();
  shell.NavigateCommand.Execute(new NavigationItem("A02","A02")); var s=shell.Screening;
  s.SelectEncounterCommand.Execute(s.Queue[0]); s.PulseText="80"; s.SystolicText="120"; s.DiastolicText="80"; s.TemperatureText="37";
  s.AllergyHistory="PHẢN VỆ"; s.Conclusion="Tạm hoãn"; s.DeferReason="Bản nháp minh họa";
  Check(s.HasDangerousAllergySuggestion && !s.IsDangerousAllergy && s.Conclusion=="Tạm hoãn","A02 hint never sets flag/conclusion");
  Check(s.CanSave,"A02 valid draft"); Capture(window,"A02-hint");
  s.IsDangerousAllergy=true; Check(!s.CanSave,"A02 manual flag blocks save");
  s.ConfirmSafetyCommand.Execute(null); Check(!s.CanSave,"A02 first confirmation");
  s.ConfirmSafetyCommand.Execute(null); Check(s.CanSave,"A02 second confirmation");
  s.AllergyHistory="phù mạch"; Check(!s.CanSave && s.SafetyConfirmationStep==0,"A02 history edit resets confirmation");
  s.PulseText="bad"; Check(!s.CanSave,"A02 invalid pulse"); Capture(window,"A02-error");
  var off=new ScreeningViewModel(new SampleScreeningService{UseSampleData=false}); off.AllergyHistory="phản vệ";
  Check(off.Queue.Count==0 && !off.HasDangerousAllergySuggestion,"A02 sample off no keyword logic");
  Capture(new ScreeningView{DataContext=off},"A02-empty");
  shell.NavigateCommand.Execute(new NavigationItem("A12","A12")); var a=shell.Accounts;
  Check(a.IsCurrentAccount && !a.CanAssignPermissions && !a.CanToggleLock && !a.CanResetPassword,"A12 cannot modify current account security");
  a.SelectedAccount=a.Accounts.First(x=>x.UserName!="reviewer"); Check(a.CanAssignPermissions,"A12 other sample account permissions");
  window.UpdateLayout(); Pump(); var tabs=Find<TabControl>((DependencyObject)shell.CurrentView).First(); tabs.SelectedIndex=2; Pump(); Capture(window,"A12-permissions");
  a.CitizenId="bad"; Check(!a.CanSaveEmployee,"A12 invalid citizen id"); tabs.SelectedIndex=0; Capture(window,"A12-error");
  a.UseSampleData=false; Check(a.HasNoAccounts && a.HasNoPermissions && !a.CanAssignPermissions,"A12 sample off"); Capture(window,"A12-empty");
  shell.ToggleProfileCommand.Execute(null); Capture(window,"A13-no-source");
  a.UseSampleData=true; Check(a.CurrentSessionJobTitle.Contains("Bác sĩ"),"A13 sample job title"); Capture(window,"A13-sample");
  shell.OpenPasswordCommand.Execute(null); Check(!shell.IsProfileOpen && shell.SelectedScreen.Id=="A14","A13 opens A14 and closes overlay");
  shell.ChangePassword.CurrentPassword="old"; shell.ChangePassword.NewPassword="newvalue"; shell.ChangePassword.Confirmation="different";
  Check(!shell.ChangePassword.CanChangePassword,"A14 mismatch"); Capture(window,"A14-error");
  shell.ChangePassword.CurrentPassword="sai"; shell.ChangePassword.NewPassword="review-only"; shell.ChangePassword.Confirmation="review-only"; shell.ChangePassword.ChangePasswordCommand.Execute(null); Pump(); Check(shell.ChangePassword.HasError,"A14 sample failure feedback"); Capture(window,"A14-service-error"); var passwordOff=new ChangePasswordViewModel(new SampleChangePasswordService{UseSampleData=false}); passwordOff.CurrentPassword="old"; passwordOff.NewPassword="review-only"; passwordOff.Confirmation="review-only"; passwordOff.ChangePasswordCommand.Execute(null); Pump(); Check(passwordOff.HasError,"A14 no source rejects operation"); Capture(new ChangePasswordView{DataContext=passwordOff},"A14-no-source"); shell.ChangePassword.CancelCommand.Execute(null); Check(shell.ChangePassword.NewPassword=="","A14 cancel clears secret"); Capture(window,"A14-empty");
  Capture(window,"shell-narrow",980,620); Console.WriteLine($"WIDTH requested={window.Width} actual={window.ActualWidth} compact={window.IsCompact}"); Check(window.IsCompact,"980 DIP compact navigation");
  shell.ToggleNavigationCommand.Execute(null); Capture(window,"shell-drawer",980,620);
  shell.NavigateCommand.Execute(new NavigationItem("A12","A12")); Check(!shell.IsNavigationOpen && shell.SelectedScreen.Id=="A12","drawer navigation closes panel");
  Capture(window,"A12-narrow",980,620);
  bool signedOut=false; shell.SignOutRequested+=(s,e)=>signedOut=true;
  // Avoid the real login dialog in the render host; verify command on a separate shell.
  var session=new ShellViewModel("before"); session.SignOutRequested+=(s,e)=>signedOut=true;
  session.ToggleProfileCommand.Execute(null); session.SignOutCommand.Execute(null); Check(signedOut && !session.IsProfileOpen,"signout event closes overlay");
  session.ChangePassword.CurrentPassword="old"; var oldAccounts=session.Accounts; session.BeginLocalSession("after");
  Check(session.CurrentUserDisplayName=="after" && oldAccounts!=session.Accounts && session.ChangePassword.CurrentPassword=="" && session.SelectedScreen.Id=="A01","new session resets identity/password/navigation");
  window.Close(); Console.WriteLine($"TOTAL {checks} PASS; offscreen 96-DPI render, no physical keyboard/DPI/E2E claims"); app.Shutdown();
 }
 static IEnumerable<T> Find<T>(DependencyObject root) where T:DependencyObject
 { for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var c=VisualTreeHelper.GetChild(root,i);if(c is T item)yield return item;foreach(var child in Find<T>(c))yield return child;} }
}
