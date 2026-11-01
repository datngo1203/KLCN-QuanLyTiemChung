using System.Windows.Controls;
using TiemChung.Desktop.ViewModels;

namespace TiemChung.Desktop.ViewsV2;

// Register a page here; the shell XAML and MainWindow code need no further changes.
public static class NavigationRegistry
{
    public static IReadOnlyList<NavigationGroup> Groups { get; } =
    [
        new("Tổng quan", [new("A01", "Tổng quan")]),
        new("Tiếp nhận", [new("A03", "Hồ sơ khách hàng"), new("A07", "Đăng ký dịch vụ"), new("A08", "Thanh toán")]),
        new("Chuyên môn", [new("A02", "Kết luận & chỉ định"), new("A06", "Ghi nhận mũi tiêm"), new("A06F", "Theo dõi sau tiêm")]),
        new("Danh mục", [new("A04", "Danh mục"), new("A10", "Ưu đãi")]),
        new("Quản trị", [new("A09", "Lịch hẹn & lịch sử"), new("A11", "Báo cáo"), new("A12", "Nhân viên & tài khoản"), new("A14", "Đổi mật khẩu")])
    ];

    public static bool IsVisible(string id) => Groups.Any(group => group.Screens.Any(screen => screen.Id == id));

    public static UserControl CreatePage(string id, ShellViewModel shell) => id switch
    {
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
