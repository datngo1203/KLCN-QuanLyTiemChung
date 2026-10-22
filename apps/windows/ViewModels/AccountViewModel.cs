using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class PermissionSelection(string code) : INotifyPropertyChanged
{
    private bool _isSelected;
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Code { get; } = code;
    public bool IsSelected { get => _isSelected; set { if (_isSelected == value) return; _isSelected = value; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); } }
}

public sealed class AccountViewModel : ViewModelBase
{
    private readonly IAccountService _service;
    private readonly string _currentUser;
    private EmployeeRecord? _selectedEmployee;
    private EmployeeRecord? _employeeForAccount;
    private AccountRecord? _selectedAccount;
    private string _employeeName = "", _citizenId = "", _phone = "", _email = "", _department = "", _jobTitle = "", _employmentStatus = "Đang làm việc";
    private string _userName = "", _feedback = "";

    public AccountViewModel(string currentUser, IAccountService? service = null)
    {
        _currentUser = currentUser;
        _service = service ?? new SampleAccountService(currentUser);
        Employees.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoEmployees));
        Accounts.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoAccounts));
        PermissionOptions.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoPermissions));
        Reload();
        SaveEmployeeCommand = new(_ => SaveEmployee(), _ => CanSaveEmployee);
        NewEmployeeCommand = new(_ => ClearEmployee());
        CreateAccountCommand = new(_ => CreateAccount(), _ => CanCreateAccount);
        ToggleLockCommand = new(_ => ToggleLock(), _ => CanToggleLock);
        ResetPasswordCommand = new(_ => ResetPassword(), _ => CanResetPassword);
        AssignPermissionsCommand = new(_ => AssignPermissions(), _ => CanAssignPermissions);
        SelectedEmployee = Employees.FirstOrDefault();
        SelectedAccount = Accounts.FirstOrDefault(x => x.UserName.Equals(_currentUser, StringComparison.OrdinalIgnoreCase));
        EmployeeForAccount = Employees.FirstOrDefault(x => !Accounts.Any(a => a.EmployeeId == x.Id));
    }

    public ObservableCollection<EmployeeRecord> Employees { get; } = [];
    public ObservableCollection<AccountRecord> Accounts { get; } = [];
    public ObservableCollection<PermissionSelection> PermissionOptions { get; } = [];
    public IReadOnlyList<string> Departments { get; } = ["Y tế", "Lễ tân", "Kế toán", "Kho", "Ban giám đốc"];
    public IReadOnlyList<string> JobTitles { get; } = ["BacSi", "DieuDuong", "ThuNgan", "LeTan", "ThuKho", "Admin"];
    public IReadOnlyList<string> EmploymentStatuses { get; } = ["Đang làm việc", "Nghỉ việc", "Tạm nghỉ"];
    public RelayCommand SaveEmployeeCommand { get; }
    public RelayCommand NewEmployeeCommand { get; }
    public RelayCommand CreateAccountCommand { get; }
    public RelayCommand ToggleLockCommand { get; }
    public RelayCommand ResetPasswordCommand { get; }
    public RelayCommand AssignPermissionsCommand { get; }

    public string DataSourceLabel => _service.UseSampleData ? "Dữ liệu mẫu" : "Chưa có nguồn dữ liệu";
    public string CurrentSessionJobTitle
    {
        get
        {
            if (!_service.UseSampleData) return "Chưa có chức danh từ dịch vụ xác thực.";
            var account = Accounts.FirstOrDefault(x => x.UserName.Equals(_currentUser, StringComparison.OrdinalIgnoreCase));
            var employee = account is null ? null : Employees.FirstOrDefault(x => x.Id == account.EmployeeId);
            var title = employee?.JobTitle switch
            {
                "BacSi" => "Bác sĩ",
                "DieuDuong" => "Điều dưỡng",
                "ThuNgan" => "Thu ngân",
                "LeTan" => "Lễ tân",
                "ThuKho" => "Thủ kho",
                "Admin" => "Quản trị viên",
                _ => null
            };
            return title is null ? "Chưa có chức danh trong dữ liệu mẫu." : $"{title} (dữ liệu mẫu)";
        }
    }
    public string CurrentSessionPermissionDisplay
    {
        get
        {
            if (!_service.UseSampleData) return "Chưa có quyền được cung cấp từ dịch vụ xác thực.";
            var account = Accounts.FirstOrDefault(x => x.UserName.Equals(_currentUser, StringComparison.OrdinalIgnoreCase));
            if (account is null) return "Chưa có dữ liệu quyền trong phiên mẫu.";
            return account.AdditionalPermissions.Count == 0
                ? "Chưa có quyền xác thực; dữ liệu mẫu không cấp quyền bổ sung cho phiên này."
                : $"Quyền bổ sung mẫu: {string.Join(", ", account.AdditionalPermissions)}. Quyền xác thực chưa được tải.";
        }
    }
    // TODO: Bind account-management permission from the authenticated shell session when available.
    public bool CanManageAccounts => true;
    public bool HasNoEmployees => Employees.Count == 0;
    public bool HasNoAccounts => Accounts.Count == 0;
    public bool HasNoPermissions => PermissionOptions.Count == 0;
    public string Feedback { get => _feedback; private set => Set(ref _feedback, value); }

    public bool UseSampleData
    {
        get => _service.UseSampleData;
        set
        {
            if (_service.UseSampleData == value) return;
            _service.UseSampleData = value;
            Reload();
            OnPropertyChanged(nameof(DataSourceLabel));
            OnPropertyChanged(nameof(CurrentSessionJobTitle));
            OnPropertyChanged(nameof(CurrentSessionPermissionDisplay));
        }
    }

    public EmployeeRecord? SelectedEmployee
    {
        get => _selectedEmployee;
        set
        {
            if (!Set(ref _selectedEmployee, value)) return;
            if (value is not null)
            { EmployeeName = value.Name; CitizenId = value.CitizenId; Phone = value.Phone; Email = value.Email; Department = value.Department; JobTitle = value.JobTitle; EmploymentStatus = value.EmploymentStatus; }
            OnPropertyChanged(nameof(IsNewEmployee));
            RefreshCommands();
        }
    }
    public EmployeeRecord? EmployeeForAccount { get => _employeeForAccount; set { if (Set(ref _employeeForAccount, value)) RefreshCommands(); } }
    public AccountRecord? SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            if (!Set(ref _selectedAccount, value)) return;
            foreach (var option in PermissionOptions) option.IsSelected = value?.AdditionalPermissions.Contains(option.Code) == true;
            OnPropertyChanged(nameof(IsCurrentAccount)); OnPropertyChanged(nameof(CanEditPermissions)); OnPropertyChanged(nameof(ToggleLockLabel)); OnPropertyChanged(nameof(HasSelectedAccount));
            RefreshCommands();
        }
    }
    public string EmployeeName { get => _employeeName; set { if (Set(ref _employeeName, value)) RefreshCommands(); } }
    public string CitizenId { get => _citizenId; set { if (Set(ref _citizenId, value)) RefreshCommands(); } }
    public string Phone { get => _phone; set { if (Set(ref _phone, value)) RefreshCommands(); } }
    public string Email { get => _email; set { if (Set(ref _email, value)) RefreshCommands(); } }
    public string Department { get => _department; set { if (Set(ref _department, value)) RefreshCommands(); } }
    public string JobTitle { get => _jobTitle; set { if (Set(ref _jobTitle, value)) RefreshCommands(); } }
    public string EmploymentStatus { get => _employmentStatus; set { if (Set(ref _employmentStatus, value)) RefreshCommands(); } }
    public string UserName { get => _userName; set { if (Set(ref _userName, value)) RefreshCommands(); } }
    public bool IsNewEmployee => SelectedEmployee is null;
    public bool IsCurrentAccount => SelectedAccount?.UserName.Equals(_currentUser, StringComparison.OrdinalIgnoreCase) == true;
    public bool CanEditPermissions => SelectedAccount is not null && !IsCurrentAccount;
    public bool HasSelectedAccount => SelectedAccount is not null;
    public string ToggleLockLabel => SelectedAccount?.IsLocked == true ? "Mở khóa" : "Khóa tài khoản";
    public string CitizenIdError => string.IsNullOrEmpty(CitizenId) || CitizenId.Length != 12 || CitizenId.Any(c => !char.IsAsciiDigit(c)) ? "CCCD phải gồm đúng 12 chữ số." : Employees.Any(x => x.CitizenId == CitizenId && x.Id != SelectedEmployee?.Id) ? "CCCD đã có trong danh sách mẫu." : "";
    public string UserNameError => string.IsNullOrWhiteSpace(UserName) ? "" : UserName.Length < 4 ? "Tên đăng nhập phải có ít nhất 4 ký tự." : UserName.Any(c => c > 127 || char.IsWhiteSpace(c)) ? "Tên đăng nhập không dấu và không có khoảng trắng." : Accounts.Any(x => x.UserName.Equals(UserName, StringComparison.OrdinalIgnoreCase)) ? "Tên đăng nhập đã tồn tại." : "";
    public bool CanSaveEmployee => CanManageAccounts && !string.IsNullOrWhiteSpace(EmployeeName) && string.IsNullOrEmpty(CitizenIdError) && !string.IsNullOrWhiteSpace(Phone) && !string.IsNullOrWhiteSpace(Email) && Departments.Contains(Department) && JobTitles.Contains(JobTitle) && EmploymentStatuses.Contains(EmploymentStatus);
    public bool CanCreateAccount => CanManageAccounts && EmployeeForAccount is not null && !string.IsNullOrWhiteSpace(UserName) && string.IsNullOrEmpty(UserNameError);
    public bool CanToggleLock => CanManageAccounts && SelectedAccount is not null && !IsCurrentAccount;
    public bool CanResetPassword => CanManageAccounts && SelectedAccount is not null && !IsCurrentAccount;
    public bool CanAssignPermissions => CanManageAccounts && SelectedAccount is not null && !IsCurrentAccount && PermissionOptions.Count > 0;

    private void Reload()
    {
        Employees.Clear(); Accounts.Clear(); PermissionOptions.Clear();
        foreach (var item in _service.GetEmployees()) Employees.Add(item);
        foreach (var item in _service.GetAccounts()) Accounts.Add(item);
        foreach (var code in _service.GetPermissionCodes())
        {
            var option = new PermissionSelection(code);
            option.PropertyChanged += (_, _) => AssignPermissionsCommand?.NotifyCanExecuteChanged();
            PermissionOptions.Add(option);
        }
        OnPropertyChanged(nameof(HasNoEmployees)); OnPropertyChanged(nameof(HasNoAccounts)); OnPropertyChanged(nameof(HasNoPermissions));
        OnPropertyChanged(nameof(CurrentSessionJobTitle)); OnPropertyChanged(nameof(CurrentSessionPermissionDisplay));
        SelectedEmployee = null; SelectedAccount = null; EmployeeForAccount = Employees.FirstOrDefault(x => !Accounts.Any(a => a.EmployeeId == x.Id));
        ClearEmployee();
        UserName = "";
        RefreshCommands();
    }

    private void SaveEmployee()
    {
        if (!CanSaveEmployee) return;
        var draft = new EmployeeRecord(SelectedEmployee?.Id ?? 0, EmployeeName.Trim(), CitizenId, Phone.Trim(), Email.Trim(), Department, JobTitle, EmploymentStatus);
        var saved = _service.SaveEmployee(draft);
        if (saved is null) { Feedback = "Chưa có dịch vụ nhân viên; không ghi dữ liệu."; return; }
        if (SelectedEmployee is null) Employees.Add(saved);
        else Employees[Employees.IndexOf(SelectedEmployee)] = saved;
        SelectedEmployee = saved;
        EmployeeForAccount ??= Employees.FirstOrDefault(x => !Accounts.Any(a => a.EmployeeId == x.Id));
        Feedback = "Đã lưu nhân viên trên dữ liệu mẫu.";
    }
    private void ClearEmployee() { SelectedEmployee = null; EmployeeName = CitizenId = Phone = Email = Department = JobTitle = ""; EmploymentStatus = "Đang làm việc"; }
    private void CreateAccount()
    {
        if (!CanCreateAccount || EmployeeForAccount is null) return;
        var account = _service.CreateAccount(EmployeeForAccount.Id, UserName.Trim());
        if (account is null) { Feedback = "Không thể tạo tài khoản mẫu cho nhân viên này."; return; }
        Accounts.Add(account); SelectedAccount = account; EmployeeForAccount = Employees.FirstOrDefault(x => !Accounts.Any(a => a.EmployeeId == x.Id)); UserName = "";
        Feedback = "Đã tạo tài khoản trên dữ liệu mẫu; chưa tạo thông tin xác thực thật.";
    }
    private void ToggleLock()
    {
        if (!CanToggleLock || SelectedAccount is null) return;
        var locked = !SelectedAccount.IsLocked;
        if (!_service.SetLocked(SelectedAccount.UserName, locked)) { Feedback = "Không thể thay đổi trạng thái tài khoản hiện tại."; return; }
        SelectedAccount.IsLocked = locked; OnPropertyChanged(nameof(ToggleLockLabel));
        Feedback = locked ? "Đã khóa tài khoản mẫu." : "Đã mở khóa tài khoản mẫu.";
    }
    private void ResetPassword()
    {
        if (!CanResetPassword || SelectedAccount is null) return;
        Feedback = _service.ResetPassword(SelectedAccount.UserName) ? "Đã minh họa đặt lại mật khẩu; chưa có dịch vụ xác thực thật." : "Không thể đặt lại mật khẩu tài khoản này.";
    }
    private void AssignPermissions()
    {
        if (!CanAssignPermissions || SelectedAccount is null) return;
        var codes = PermissionOptions.Where(x => x.IsSelected).Select(x => x.Code).ToArray();
        if (!_service.AssignPermissions(SelectedAccount.UserName, codes)) { Feedback = "Không thể gán quyền cho tài khoản hiện tại."; return; }
        SelectedAccount.SetPermissions(codes);
        Feedback = "Đã cập nhật quyền mẫu; chưa có dữ liệu quyền xác thực từ máy chủ.";
    }
    private void RefreshCommands()
    {
        OnPropertyChanged(nameof(CitizenIdError)); OnPropertyChanged(nameof(UserNameError));
        OnPropertyChanged(nameof(CanSaveEmployee)); OnPropertyChanged(nameof(CanCreateAccount)); OnPropertyChanged(nameof(CanToggleLock)); OnPropertyChanged(nameof(CanResetPassword)); OnPropertyChanged(nameof(CanAssignPermissions));
        SaveEmployeeCommand?.NotifyCanExecuteChanged(); NewEmployeeCommand?.NotifyCanExecuteChanged(); CreateAccountCommand?.NotifyCanExecuteChanged(); ToggleLockCommand?.NotifyCanExecuteChanged(); ResetPasswordCommand?.NotifyCanExecuteChanged(); AssignPermissionsCommand?.NotifyCanExecuteChanged();
    }
}
