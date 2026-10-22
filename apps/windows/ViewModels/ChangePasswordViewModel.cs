using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class ChangePasswordViewModel : ViewModelBase
{
    private readonly IChangePasswordService _service;
    private string _currentPassword = "", _newPassword = "", _confirmation = "";
    private string _feedback = "", _error = "";
    private bool _isBusy;

    public ChangePasswordViewModel(IChangePasswordService? service = null)
    {
        _service = service ?? new SampleChangePasswordService();
        ChangePasswordCommand = new RelayCommand(async _ => await ChangePasswordAsync(), _ => CanChangePassword);
        CancelCommand = new RelayCommand(_ => Clear());
    }

    public string CurrentPassword { get => _currentPassword; set { if (Set(ref _currentPassword, value)) InputChanged(); } }
    public string NewPassword { get => _newPassword; set { if (Set(ref _newPassword, value)) InputChanged(); } }
    public string Confirmation { get => _confirmation; set { if (Set(ref _confirmation, value)) InputChanged(); } }
    public string Feedback { get => _feedback; private set => Set(ref _feedback, value); }
    public string Error { get => _error; private set { if (Set(ref _error, value)) OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrWhiteSpace(Error);
    public bool IsBusy { get => _isBusy; private set { if (Set(ref _isBusy, value)) { OnPropertyChanged(nameof(IsNotBusy)); NotifyCanChange(); } } }
    public bool IsNotBusy => !IsBusy;
    public bool CanChangePassword => !IsBusy && !string.IsNullOrEmpty(CurrentPassword) && !string.IsNullOrEmpty(NewPassword)
        && !string.IsNullOrEmpty(Confirmation) && NewPassword == Confirmation && NewPassword != CurrentPassword && NewPassword.Length >= 6;
    public RelayCommand ChangePasswordCommand { get; }
    public RelayCommand CancelCommand { get; }

    private async Task ChangePasswordAsync()
    {
        if (!CanChangePassword) return;
        Error = ""; Feedback = "Đang kiểm tra…"; IsBusy = true;
        try
        {
            var result = await _service.ChangePasswordAsync(CurrentPassword, NewPassword);
            Feedback = result.Succeeded ? result.Message : "";
            Error = result.Succeeded ? "" : result.Message;
        }
        catch (Exception)
        {
            Feedback = ""; Error = "Không kết nối được dịch vụ. Kiểm tra mạng rồi thử lại.";
        }
        finally { IsBusy = false; }
    }

    private void Clear()
    {
        CurrentPassword = NewPassword = Confirmation = "";
        Error = Feedback = "";
    }

    private void InputChanged()
    {
        if (!IsBusy) { Error = ""; Feedback = ""; }
        NotifyCanChange();
    }

    private void NotifyCanChange()
    {
        OnPropertyChanged(nameof(CanChangePassword));
        ChangePasswordCommand?.NotifyCanExecuteChanged();
    }
}
