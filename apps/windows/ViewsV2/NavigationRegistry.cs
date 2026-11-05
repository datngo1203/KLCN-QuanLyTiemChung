using System.Windows.Controls;
using TiemChung.Desktop.ViewModels;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewsV2;

// Register a page here; the shell XAML and MainWindow code need no further changes.
public static class NavigationRegistry
{
    public static IReadOnlyList<NavigationGroup> Groups { get; } =
    [
        new("Tổng quan", [new("A01", "Tổng quan", "overview")]),
        new("Tiếp nhận", [new("A03", "Hồ sơ khách hàng", "profilesCreate"), new("A07", "Đăng ký dịch vụ", "registrations"), new("A08", "Thanh toán", "paymentsRead")]),
        new("Chuyên môn", [new("A02", "Kết luận & chỉ định", "screening"), new("A06", "Ghi nhận mũi tiêm", "vaccinations"), new("A06F", "Theo dõi sau tiêm", "aftercare")]),
        new("Danh mục", [new("A04", "Danh mục", "catalog"), new("A05", "Kho vắc xin", "inventoryRead"), new("A10", "Ưu đãi", "promotions")]),
        new("Quản trị", [new("A09", "Lịch hẹn & lịch sử", "appointments"), new("A11", "Báo cáo", "reports"), new("A12", "Nhân viên & tài khoản", "accounts"), new("A14", "Đổi mật khẩu", "changePassword")])
    ];

    public static IReadOnlyList<NavigationGroup> VisibleGroups(IdentityApiService identity) => Groups
        .Select(group => new NavigationGroup(group.Title, group.Screens.Where(screen => identity.HasCapability(screen.RequiredCapability)).ToArray()))
        .Where(group => group.Screens.Count > 0).ToArray();

    public static bool IsVisible(string id) => VisibleGroups(IdentityApiService.Current).Any(group => group.Screens.Any(screen => screen.Id == id));

    public static UserControl CreatePage(string id, ShellViewModel shell)
    {
        if (!shell.CanAccess(id)) throw new UnauthorizedAccessException("Bạn không có quyền mở màn hình này.");
        return id switch {

        "A01" => new OverviewView { DataContext = shell.Overview },
        "A02" => new ScreeningView { DataContext = shell.Screening },
        "A03" => new ProfilesView { DataContext = shell.Profiles },
        "A07" => new RegistrationView { DataContext = shell.Registration },
        "A08" => new PaymentView { DataContext = shell.Payment },
        "A04" => new CatalogView { DataContext = shell.Catalog },
        "A05" => new InventoryView { DataContext = shell.Inventory },
        "A10" => new PromotionView { DataContext = shell.Promotion },
        "A06" => new VaccinationView { DataContext = shell.Vaccination },
        "A06F" => new AftercareView { DataContext = shell.Aftercare },
        "A09" => new AppointmentView { DataContext = shell.Appointments },
        "A11" => new ReportView { DataContext = shell.Reports },
        "A12" => new AccountView { DataContext = shell.Accounts },
        "A14" => new ChangePasswordView { DataContext = shell.ChangePassword },
        _ => new PlaceholderView { DataContext = new PlaceholderViewModel(Groups.SelectMany(g => g.Screens).First(s => s.Id == id)) }
        };
    }
}
