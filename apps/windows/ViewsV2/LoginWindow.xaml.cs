using System.Windows;
using TiemChung.Desktop.ViewModels;

namespace TiemChung.Desktop.ViewsV2;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.LoginSucceeded += LoginSucceeded;
        PasswordInput.Password = viewModel.Password;
    }

    public string UserName => _viewModel.UserName;

    private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.Password = PasswordInput.Password;

    private void LoginSucceeded(object? sender, EventArgs e) => DialogResult = true;
}
