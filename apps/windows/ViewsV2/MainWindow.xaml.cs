using System.ComponentModel;
using System.Windows;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;

namespace TiemChung.Desktop.ViewsV2;

public partial class MainWindow : Window
{
    public static readonly DependencyProperty IsCompactProperty = DependencyProperty.Register(
        nameof(IsCompact), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));
    public bool IsCompact { get => (bool)GetValue(IsCompactProperty); private set => SetValue(IsCompactProperty, value); }
    private ShellViewModel _viewModel;
    private IdentityApiService _identity = IdentityApiService.Current;

    public MainWindow() : this(new ShellViewModel("")) { }

    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.SignOutRequested += SignOutRequested;
        _identity.PasswordChanged += PasswordChanged;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        SourceInitialized += (_, _) =>
        {
            var work = SystemParameters.WorkArea;
            MinWidth = Math.Min(MinWidth, work.Width);
            MinHeight = Math.Min(MinHeight, work.Height);
            Width = Math.Min(Width, work.Width);
            Height = Math.Min(Height, work.Height);
        };
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        IsCompact = e.NewSize.Width < 1240;
        if (!IsCompact && DataContext is ShellViewModel vm) vm.IsNavigationOpen = false;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShellViewModel.IsSearchOpen) && _viewModel.IsSearchOpen)
            Dispatcher.BeginInvoke(new Action(() => { FunctionSearch.Focus(); FunctionSearch.SelectAll(); }));
    }

    private bool _signingOut;
    private async void SignOutRequested(object? sender, EventArgs e)
    {
        if (_signingOut) return;
        _signingOut = true;
        IsEnabled = false;
        try { await _identity.LogoutAsync(); }
        catch (Exception) {
            MessageBox.Show("Chưa đăng xuất được khỏi API. Kiểm tra kết nối và thử lại.", "Đăng xuất");
            _signingOut = false;
            IsEnabled = true;
            return;
        }
        ShowLogin();
        _signingOut = false;
        IsEnabled = true;
    }

    private void PasswordChanged(object? sender, EventArgs e) => Dispatcher.BeginInvoke(new Action(() => {
        if (_signingOut) return;
        _signingOut = true;
        try { ShowLogin(); }
        finally { _signingOut = false; }
    }));

    private void ShowLogin()
    {
        Hide();
        // Discard the entire old shell and its service session, including pending loads.
        DataContext = null;
        _viewModel.SignOutRequested -= SignOutRequested;
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        _identity.PasswordChanged -= PasswordChanged;
        IdentityApiService.StartNewSession();
        _identity = IdentityApiService.Current;
        var login = new LoginWindow(new LoginViewModel(IdentityApiService.Current));
        if (login.ShowDialog() == true) {
            _viewModel = new ShellViewModel(login.UserName);
            DataContext = _viewModel;
            _viewModel.SignOutRequested += SignOutRequested;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _identity.PasswordChanged += PasswordChanged;
            Show();
        }
        else Close();
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        _viewModel.SignOutRequested -= SignOutRequested;
        _identity.PasswordChanged -= PasswordChanged;
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }
}
