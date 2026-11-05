using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed record EmployeeAccessRow(int AccountId, int EmployeeId, string FullName, string Username, string Title,
    string EmployeeStatus, string AccountStatus, byte[] Version);
public sealed record EmployeeRoleOption(string Title, string Label, string Access);
public sealed record EmployeeAccessResponse(EmployeeAccessRow[] Items, EmployeeRoleOption[] Roles);

public sealed class AccountViewModel : ViewModelBase
{
    private readonly IdentityApiService _identity;
    private readonly string _currentUser;
    private EmployeeAccessRow? _selected;
    private EmployeeRoleOption? _selectedRole;
    private bool _busy;
    private string _feedback = "";
    public AccountViewModel(string currentUser, IdentityApiService? identity = null)
    {
        _identity = identity ?? IdentityApiService.Current;
        _currentUser = currentUser;
        ReloadCommand = new(async _ => await LoadAsync(), _ => CanManageAccounts && !_busy);
        SaveRoleCommand = new(async _ => await SaveAsync(), _ => CanSave);
        _identity.SessionCleared += (_, _) => { Items.Clear(); Roles.Clear(); Selected = null; Feedback = "Phiên đã kết thúc. Vui lòng đăng nhập lại."; Refresh(); };
    }
    public ObservableCollection<EmployeeAccessRow> Items { get; } = [];
    public ObservableCollection<EmployeeRoleOption> Roles { get; } = [];
    public RelayCommand ReloadCommand { get; }
    public RelayCommand SaveRoleCommand { get; }
    public bool CanManageAccounts => _identity.CanManageAccounts;
    public bool CanSave => CanManageAccounts && !_busy && Selected is not null && SelectedRole is not null &&
        !string.Equals(Selected.Username, _currentUser, StringComparison.OrdinalIgnoreCase) && SelectedRole.Title != Selected.Title;
    public EmployeeAccessRow? Selected { get => _selected; set { if (Set(ref _selected, value)) { SelectedRole = Roles.FirstOrDefault(r => r.Title == value?.Title); Refresh(); } } }
    public EmployeeRoleOption? SelectedRole { get => _selectedRole; set { if (Set(ref _selectedRole, value)) Refresh(); } }
    public string Feedback { get => _feedback; private set => Set(ref _feedback, value); }
    public async Task LoadAsync()
    {
        if (!CanManageAccounts || _busy) return;
        _busy = true; Refresh();
        var revision = _identity.SessionRevision;
        try {
            using var response = await _identity.SendCatalogAsync(HttpMethod.Get, "api/v1/employee-access", expectedSessionRevision: revision);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<EmployeeAccessResponse>() ?? throw new HttpRequestException();
            if (revision != _identity.SessionRevision) return;
            Items.Clear(); Roles.Clear(); Selected = null;
            foreach (var role in data.Roles) Roles.Add(role);
            foreach (var item in data.Items) Items.Add(item);
            Feedback = Items.Count == 0 ? "Chưa có tài khoản nhân viên." : "Đã tải dữ liệu từ hệ thống.";
        } catch (OperationCanceledException) { if (revision == _identity.SessionRevision) Feedback = "Không tải được dữ liệu. Vui lòng tải lại."; }
        catch (Exception e) when (e is HttpRequestException or System.Text.Json.JsonException) {
            if (revision == _identity.SessionRevision) Feedback = "Không tải được dữ liệu. Vui lòng tải lại.";
        } finally { _busy = false; Refresh(); }
    }
    public async Task SaveAsync()
    {
        if (!CanSave) return;
        var row = Selected!; var role = SelectedRole!;
        _busy = true; Refresh();
        var revision = _identity.SessionRevision;
        var saved = false;
        try {
            using var response = await _identity.SendCatalogAsync(HttpMethod.Put, $"api/v1/employee-access/{row.AccountId}/role",
                new { title = role.Title, expectedTitle = row.Title, version = row.Version }, expectedSessionRevision: revision);
            if (revision != _identity.SessionRevision) return;
            saved = response.IsSuccessStatusCode;
            Feedback = saved ? "Đã đổi chức danh và thu hồi phiên của nhân viên. Nhân viên cần đăng nhập lại." :
                response.StatusCode == System.Net.HttpStatusCode.Conflict ? "Dữ liệu đã thay đổi. Vui lòng tải lại trước khi lưu." :
                "Không thể đổi chức danh. Vui lòng kiểm tra quyền và tải lại dữ liệu.";
        } catch (OperationCanceledException) { if (revision == _identity.SessionRevision) Feedback = "Chưa xác định kết quả. Vui lòng tải lại trước khi thử lại."; }
        catch (HttpRequestException) { if (revision == _identity.SessionRevision) Feedback = "Chưa xác định kết quả. Vui lòng tải lại trước khi thử lại."; }
        finally { _busy = false; Refresh(); }
        if (saved) { await LoadAsync(); Feedback = "Đã đổi chức danh và thu hồi phiên của nhân viên. Nhân viên cần đăng nhập lại."; }
    }
    private void Refresh() { OnPropertyChanged(nameof(CanManageAccounts)); OnPropertyChanged(nameof(CanSave)); ReloadCommand?.NotifyCanExecuteChanged(); SaveRoleCommand?.NotifyCanExecuteChanged(); }
}
