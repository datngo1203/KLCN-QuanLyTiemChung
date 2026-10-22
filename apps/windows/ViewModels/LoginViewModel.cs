using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private string _userName = "";
    private string _password = "";
    private string _errorMessage = "";
    private string _statusMessage = "";
    private bool _isBusy;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
        LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => CanLogin);
        ExitCommand = new RelayCommand(_ => App.Current.Shutdown());
    }

    public event EventHandler? LoginSucceeded;
    public string UserName { get => _userName; set { if (Set(ref _userName, value)) { RefreshCanLogin(); ErrorMessage = ""; } } }
    public string Password { get => _password; set { if (Set(ref _password, value)) { RefreshCanLogin(); ErrorMessage = ""; } } }
    public string ErrorMessage { get => _errorMessage; private set { if (Set(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError)); } }
    public string StatusMessage { get => _statusMessage; private set => Set(ref _statusMessage, value); }
    public bool IsBusy { get => _isBusy; private set { if (Set(ref _isBusy, value)) { OnPropertyChanged(nameof(IsNotBusy)); RefreshCanLogin(); } } }
    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool CanLogin => !IsBusy && UserName.Length >= 3 && UserName == UserName.Trim() && !string.IsNullOrWhiteSpace(Password);
    public RelayCommand LoginCommand { get; }
    public RelayCommand ExitCommand { get; }

    private async Task LoginAsync()
    {
        StatusMessage = "Đang xác thực…";
        IsBusy = true;
        try
        {
            var result = await _authService.LoginAsync(UserName, Password);
            if (!result.Succeeded) { StatusMessage = ""; ErrorMessage = result.Message; }
            else
            {
                StatusMessage = result.Message;
                UserName = result.UserName;
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Không kết nối được dịch vụ xác thực. Kiểm tra mạng rồi thử lại.";
        }
        finally { IsBusy = false; }
    }

    private void RefreshCanLogin()
    {
        OnPropertyChanged(nameof(CanLogin));
        LoginCommand?.NotifyCanExecuteChanged();
    }
}
