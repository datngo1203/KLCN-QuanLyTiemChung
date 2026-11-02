using System.IO;
using System.Windows;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;

namespace TiemChung.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        File.AppendAllText("app_startup.log", $"Args: [{string.Join(", ", e.Args)}]\n");

        if (e.Args.Length > 0 && (e.Args[0] == "--capture-report" || e.Args[0] == "--capture-report-doses" || e.Args[0] == "--capture-report-status"))
        {
            var isDoses = e.Args[0] == "--capture-report-doses";
            var isStatus = e.Args[0] == "--capture-report-status";
            var outputPath = e.Args.Length > 1 ? e.Args[1] : (isStatus ? "report_a11_s7_3_screenshot.png" : (isDoses ? "report_a11_doses_screenshot.png" : "report_a11_revenue_screenshot.png"));
            var auth = await IdentityApiService.Current.LoginAsync("demo.admin", "TiemChung@2026");
            if (!auth.Succeeded)
            {
                File.WriteAllText("capture_error.txt", $"Login failed: {auth.Message}");
                Shutdown(1);
                return;
            }

            var vm = new ShellViewModel("demo.admin");
            var group = vm.NavigationGroups.First(g => g.Screens.Any(s => s.Id == "A11"));
            vm.SelectGroupCommand.Execute(group);
            var item = group.Screens.First(s => s.Id == "A11");
            vm.NavigateCommand.Execute(item);

            if (isStatus)
            {
                vm.Reports.SelectedTab = "RegistrationStatus";
            }
            else if (isDoses)
            {
                vm.Reports.SelectedTab = "VaccineDoses";
            }
            await vm.Reports.GenerateAsync();

            var window = new ViewsV2.MainWindow(vm);
            window.Width = 1280;
            window.Height = 880;
            window.Show();

            await Task.Delay(1500);
            window.UpdateLayout();

            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(1280, 880, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Render(window);

            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
            using (var stream = File.Create(outputPath))
            {
                encoder.Save(stream);
            }

            window.Close();
            Shutdown(0);
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var login = new ViewsV2.LoginWindow(new LoginViewModel(IdentityApiService.Current));
        if (login.ShowDialog() != true) { Shutdown(); return; }

        var shell = new ViewsV2.MainWindow(new ShellViewModel(login.UserName));
        MainWindow = shell;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        shell.Show();
    }
}
