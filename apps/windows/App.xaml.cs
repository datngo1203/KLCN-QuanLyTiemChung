using System.Windows;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;

namespace TiemChung.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var login = new ViewsV2.LoginWindow(new LoginViewModel(IdentityApiService.Current));
        if (login.ShowDialog() != true) { Shutdown(); return; }

        var shell = new ViewsV2.MainWindow(new ShellViewModel(login.UserName));
        MainWindow = shell;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        shell.Show();
    }
}
