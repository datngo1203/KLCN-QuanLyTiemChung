namespace TiemChung.Desktop.Services;

// TODO: Replace with reviewed Identity endpoints; this fixture stores no passwords and enforces no real authorization.
public sealed class SampleAccountService : IAccountService
{
    private readonly string _currentUser;
    private readonly List<EmployeeRecord> _employees =
    [
        new(1, "Nhân viên mẫu A", "000000000001", "0000000001", "nhanvien-a@example.test", "Y tế", "BacSi", "Đang làm việc"),
        new(2, "Nhân viên mẫu B", "000000000002", "0000000002", "nhanvien-b@example.test", "Lễ tân", "LeTan", "Đang làm việc"),
        new(3, "Nhân viên mẫu C", "000000000003", "0000000003", "nhanvien-c@example.test", "Kho", "ThuKho", "Tạm nghỉ"),
        new(4, "Nhân viên mẫu D", "000000000004", "0000000004", "nhanvien-d@example.test", "Kế toán", "ThuNgan", "Đang làm việc")
    ];
    private readonly List<AccountRecord> _accounts = [];
    private static readonly string[] PermissionCodes = [
        "XemTongQuan", "QuanLyLichHen", "GuiNhacLich", "QuanLyKhuyenMai", "XemBaoCao", "XemThanhToan",
        "GhiThanhToan", "QuanLyTaiKhoan", "QuanLyDanhMucChuyenMon", "QuanLyDanhMucGia", "XemKho", "XemHoSo"
    ];

    public SampleAccountService(string currentUser)
    {
        _currentUser = currentUser;
        _accounts.Add(new(currentUser, 1, _employees[0].Name, false));
        _accounts.Add(new("taikhoan.mau", 2, _employees[1].Name, false, ["XemTongQuan", "XemBaoCao"]));
        _accounts.Add(new("khoamautest", 3, _employees[2].Name, true));
    }

    public bool UseSampleData { get; set; } = true;

    public IReadOnlyList<EmployeeRecord> GetEmployees() => UseSampleData ? _employees.ToArray() : [];
    public IReadOnlyList<AccountRecord> GetAccounts() => UseSampleData ? _accounts.ToArray() : [];
    public IReadOnlyList<string> GetPermissionCodes() => UseSampleData ? PermissionCodes : [];
    public EmployeeRecord? SaveEmployee(EmployeeRecord employee)
    {
        if (!UseSampleData) return null;
        var index = _employees.FindIndex(x => x.Id == employee.Id);
        if (index < 0) { employee = employee with { Id = _employees.Count == 0 ? 1 : _employees.Max(x => x.Id) + 1 }; _employees.Add(employee); }
        else _employees[index] = employee;
        return employee;
    }
    public AccountRecord? CreateAccount(int employeeId, string userName)
    {
        if (!UseSampleData || _accounts.Any(x => x.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase)) || _accounts.Any(x => x.EmployeeId == employeeId)) return null;
        var employee = _employees.FirstOrDefault(x => x.Id == employeeId);
        if (employee is null) return null;
        var account = new AccountRecord(userName, employeeId, employee.Name, false);
        _accounts.Add(account);
        return account;
    }
    public bool SetLocked(string userName, bool locked)
    {
        var account = _accounts.FirstOrDefault(x => x.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
        if (!UseSampleData || account is null || account.UserName.Equals(_currentUser, StringComparison.OrdinalIgnoreCase)) return false;
        account.IsLocked = locked;
        return true;
    }
    public bool ResetPassword(string userName) => UseSampleData && _accounts.Any(x => x.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase) && !x.UserName.Equals(_currentUser, StringComparison.OrdinalIgnoreCase));
    public bool AssignPermissions(string userName, IReadOnlyCollection<string> codes)
    {
        var account = _accounts.FirstOrDefault(x => x.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
        if (!UseSampleData || account is null || account.UserName.Equals(_currentUser, StringComparison.OrdinalIgnoreCase) || codes.Any(x => !PermissionCodes.Contains(x, StringComparer.Ordinal))) return false;
        account.SetPermissions(codes);
        return true;
    }
}
