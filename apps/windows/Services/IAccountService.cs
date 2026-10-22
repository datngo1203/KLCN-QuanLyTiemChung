using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TiemChung.Desktop.Services;

public sealed record EmployeeRecord(int Id, string Name, string CitizenId, string Phone, string Email, string Department, string JobTitle, string EmploymentStatus);

public sealed class AccountRecord : INotifyPropertyChanged
{
    private bool _isLocked;
    private HashSet<string> _additionalPermissions;
    public AccountRecord(string userName, int employeeId, string employeeName, bool isLocked, IEnumerable<string>? permissions = null)
    { UserName = userName; EmployeeId = employeeId; EmployeeName = employeeName; _isLocked = isLocked; _additionalPermissions = permissions?.ToHashSet(StringComparer.Ordinal) ?? []; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public string UserName { get; }
    public int EmployeeId { get; }
    public string EmployeeName { get; }
    public bool IsLocked { get => _isLocked; set { if (_isLocked == value) return; _isLocked = value; Changed(); Changed(nameof(Status)); Changed(nameof(IsActive)); } }
    public string Status => IsLocked ? "Đã khóa" : "Hoạt động";
    public bool IsActive => !IsLocked;
    public HashSet<string> AdditionalPermissions { get => _additionalPermissions; private set { _additionalPermissions = value; Changed(); } }
    public void SetPermissions(IEnumerable<string> values) => AdditionalPermissions = values.ToHashSet(StringComparer.Ordinal);
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public interface IAccountService
{
    bool UseSampleData { get; set; }
    IReadOnlyList<EmployeeRecord> GetEmployees();
    IReadOnlyList<AccountRecord> GetAccounts();
    IReadOnlyList<string> GetPermissionCodes();
    EmployeeRecord? SaveEmployee(EmployeeRecord employee);
    AccountRecord? CreateAccount(int employeeId, string userName);
    bool SetLocked(string userName, bool locked);
    bool ResetPassword(string userName);
    bool AssignPermissions(string userName, IReadOnlyCollection<string> codes);
}
