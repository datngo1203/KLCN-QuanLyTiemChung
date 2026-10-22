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
    private readonly ShellViewModel _viewModel;

    public MainWindow() : this(new ShellViewModel("")) { }

    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.SignOutRequested += SignOutRequested;
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

    private void SignOutRequested(object? sender, EventArgs e)
    {
        Hide();
        var login = new LoginWindow(new LoginViewModel(new SampleAuthService()));
        if (login.ShowDialog() == true) { _viewModel.BeginLocalSession(login.UserName); Show(); }
        else Close();
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        _viewModel.SignOutRequested -= SignOutRequested;
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }
}
