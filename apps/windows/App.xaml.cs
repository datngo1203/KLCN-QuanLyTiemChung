using System.Windows;
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
        // Preserve the existing local login behavior; no auth or service logic is changed.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var login = new DemoLoginWindow();
        if (login.ShowDialog() != true) { Shutdown(); return; }

        var shell = new ViewsV2.MainWindow(new ShellViewModel(login.UserName));
        MainWindow = shell;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        shell.Show();
    }
}
