using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UiButton = Wpf.Ui.Controls.Button;

namespace TiemChung.Desktop;

public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    private const string Pending = "Cần xác nhận theo đề cương";
    private readonly DispatcherTimer _feedbackTimer = new() { Interval = TimeSpan.FromSeconds(3) };
    private readonly List<FrameworkElement> _activeDemoRows = [];
    private string _currentScreen = "A01";
    private string _catalogTab = "Loại tiêm chủng";
    private string _clientTab = "Khách hàng";
    private string _accountTab = "Tài khoản";
    private string _demoUser = "admin";
    private bool _demoPaymentPaid;
    private bool _demoReminderSent;
    private bool _demoAccountEnabled = true;
    private readonly Dictionary<string, string[]> _demoRowEdits = [];
    private readonly Dictionary<string, List<string[]>> _addedDemoRows = [];

    private static readonly (string Id, string Title, string Subtitle)[] Screens =
    [
        ("A01", "Tổng quan", "Tổng hợp hoạt động của trung tâm"),
        ("A02", "Khám sàng lọc và chỉ định", "Tiếp nhận, tra cứu hồ sơ và ghi nhận kết quả khám"),
        ("A03", "Khách hàng & Người được tiêm", "Quản lý hồ sơ khách hàng và người được tiêm"),
        ("A04", "Danh mục tiêm chủng", "Loại tiêm chủng, vắc xin và gói dịch vụ"),
        ("A05", "Nhập kho vắc xin", "Theo dõi lô vắc xin, tồn kho và hạn sử dụng"),
        ("A06", "Thực hiện tiêm chủng", "Ghi nhận giao diện thực hiện tiêm"),
        ("A07", "Quản lý đăng ký dịch vụ", "Tiếp nhận và theo dõi đăng ký dịch vụ"),
        ("A08", "Thanh toán dịch vụ", "Theo dõi phiếu và phương thức thanh toán"),
        ("A09", "Lịch tiêm & Nhắc lịch", "Lịch hẹn và nhắc lịch"),
        ("A10", "Khuyến mãi & Ưu đãi", "Quản lý chương trình ưu đãi"),
        ("A11", "Thống kê & Báo cáo", "Tổng hợp hoạt động "),
        ("A12", "Tài khoản & Phân quyền", "Danh sách tài khoản và vai trò minh họa"),
        ("A13", "Đăng nhập nội bộ", "Mô phỏng cục bộ · dữ liệu minh họa"),
        ("A14", "Đổi mật khẩu", "Biểu mẫu minh họa · không thay đổi mật khẩu thật")
    ];

    public MainWindow()
    {
        InitializeComponent();
        _feedbackTimer.Tick += (_, _) =>
        {
            FeedbackBanner.Visibility = Visibility.Collapsed;
            _feedbackTimer.Stop();
        };
        Hide();
        Dispatcher.BeginInvoke(() =>
        {
            var login = new ViewsV2.LoginWindow(new ViewModels.LoginViewModel(new Services.SampleAuthService()));
            if (login.ShowDialog() == true)
            {
                _demoUser = login.UserName;
                Show();
                RenderScreen("A01");
            }
            else Close();
        }, DispatcherPriority.ApplicationIdle);
    }

    private void Navigate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not UiButton button || button.Tag is not string screenId) return;
        RenderScreen(screenId);
        foreach (var item in NavigationStack.Children.OfType<UiButton>())
        {
            var selected = Equals(item.Tag, screenId);
            item.Background = selected ? Brush("#E9E7FF") : Brushes.Transparent;
            item.Foreground = selected ? Brush("#5148B8") : Brush("#44546A");
        }
    }

    private void Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (NavigationStack is null || MenuSearch is null) return;
        var query = MenuSearch.Text.Trim();
        foreach (var button in NavigationStack.Children.OfType<UiButton>())
            button.Visibility = string.IsNullOrWhiteSpace(query) || button.Content?.ToString()?.Contains(query, StringComparison.CurrentCultureIgnoreCase) == true
                ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Notification_Click(object sender, RoutedEventArgs e) => ShowFeedback($"Thông báo: {Pending}.");

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        var login = new ViewsV2.LoginWindow(new ViewModels.LoginViewModel(new Services.SampleAuthService()));
        if (login.ShowDialog() == true)
        {
            _demoUser = login.UserName;
            Show();
            ShowFeedback("Đăng nhập thành công.");
        }
        else Close();
    }

    private void RenderScreen(string id)
    {
        _currentScreen = id;
        _activeDemoRows.Clear();
        var screen = Screens.FirstOrDefault(item => item.Id == id);
        var title = screen.Title ?? "Giao diện";
        PageTitle.Text = title;
        BreadcrumbText.Text = $"HỆ THỐNG / {id} / {title.ToUpperInvariant()}";
        ScreenContent.Children.Clear();
        AddIntro(screen.Subtitle ?? Pending, false);

        switch (id)
        {
            case "A01": RenderDashboard(); break;
            case "A02": RenderScreening(); break;
            case "A03": RenderClients(); break;
            case "A04": RenderCatalog(); break;
            case "A06": RenderVaccination(); break;
            case "A07": RenderRegistrations(); break;
            case "A10": RenderPromotions(); break;
            case "A05": RenderInventory(); break;
            case "A08": RenderPayments(); break;
            case "A09": RenderSchedule(); break;
            case "A11": RenderReports(); break;
            case "A12": RenderAccounts(); break;
            case "A13": RenderLoginInfo(); break;
            case "A14": RenderPasswordForm(); break;
            default: RenderWaitingScreen(); break;
        }
    }

    private void AddIntro(string subtitle, bool isWaiting)
    {
        if (!IsDemoDisclaimer(subtitle))
            AddText(subtitle, 13, "#72869A", 0, 0, 0, 17);
    }

    private void RenderDashboard()
    {
        AddSection("Tổng quan hoạt động", "Tổng hợp hoạt động theo kỳ", () =>
        {
            var stats = new UniformGrid { Columns = 4, Margin = new Thickness(0, 8, 0, 0) };
            foreach (var label in new[] { "LƯỢT ĐĂNG KÝ", "LỊCH HẸN", "LƯỢT TIÊM", "THANH TOÁN" })
                stats.Children.Add(StatTile(label));
            return stats;
        });
        AddSection("Lịch hẹn và trạng thái xử lý", "Lịch hẹn trong ngày", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(TableHeader("MÃ / HỒ SƠ", "DỊCH VỤ", "THỜI GIAN", "TRẠNG THÁI"));
            stack.Children.Add(TablePlaceholder());
            stack.Children.Add(ActionRow("Xuất bảng minh họa", () => ShowFeedback("Đã mô phỏng thao tác xuất bảng; không tạo tệp thật."), "Tiếp nhận nhanh", () => OpenDemoEditor("Tiếp nhận nhanh (minh họa)")));
            return stack;
        });
        AddSection("Tình trạng xử lý", "Số lượng lượt trong từng trạng thái", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(ProcessRow("18", "Chờ khám sàng lọc", "Phòng khám minh họa · thời gian chờ cần xác nhận."));
            stack.Children.Add(ProcessRow("07", "Chờ thanh toán", "Tại quầy thu ngân · số lượt minh họa."));
            stack.Children.Add(ProcessRow("12", "Chờ tiêm vắc xin", "Khu vực tiêm minh họa · số bàn cần xác nhận."));
            stack.Children.Add(ProcessRow("09", "Theo dõi sau tiêm", "Thời gian theo dõi cần bổ sung theo đề cương."));
            return stack;
        });
        AddSection("Lịch tiêm sắp đến", "Lịch tiêm sắp đến", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(ProcessRow("16/10", "Hồ sơ mẫu Lê An Nhiên", "Vắc xin mẫu A · 09:00 · chưa nhắc (mẫu)."));
            stack.Children.Add(ProcessRow("17/10", "Hồ sơ mẫu Trần Gia Bảo", "Gói mẫu B · 10:30 · đã nhắc thử."));
            stack.Children.Add(ActionRow("Xem danh sách nhắc hẹn", () => NavigateTo("A09"), "Gửi nhắc thử", () => ShowFeedback("Đã mô phỏng gửi nhắc lịch; không gửi thông báo thật.")));
            return stack;
        });
    }

    private void RenderScreening()
    {
        AddSection("Hàng đợi khám sàng lọc", "Hồ sơ và kết quả khám", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(ActionRow("Tất cả", () => ShowFeedback("Đang hiển thị các lượt khám minh họa."), "Làm mới hàng đợi", () => ShowFeedback("Đã làm mới hàng đợi minh họa.")));
            stack.Children.Add(TableHeader("THÔNG TIN LƯỢT", "HỒ SƠ", "TRẠNG THÁI", "THAO TÁC"));
            stack.Children.Add(TablePlaceholder());
            return stack;
        });
        AddSection("Ghi nhận kết quả khám sàng lọc", "Kết quả dưới đây chỉ thay đổi trạng thái giao diện; tiêu chí y tế cần xác nhận", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(PendingBox("Người được tiêm: Lê An Nhiên (hồ sơ mẫu) · Khách hàng/giám hộ: Trần Minh An (mẫu) · Đăng ký ban đầu: Vắc xin mẫu A. Dữ liệu này chỉ minh họa, không phải hồ sơ y tế."));
            stack.Children.Add(PendingBox("Lịch sử mẫu: chưa có bản ghi trước đó trong hồ sơ demo. Phác đồ, biểu mẫu khám và tiêu chí chuyên môn cần xác nhận theo đề cương."));
            stack.Children.Add(AddText("GHI NHẬN THÔNG TIN KHÁM SÀNG LỌC", 10, "#49647A", 0, 0, 0, 7, true));
            var note = new TextBox { Height = 76, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 12), BorderBrush = Brush("#DDE6ED"), Background = Brushes.White, ToolTip = "Ghi chú minh họa; không phải thông tin y tế thật." };

            stack.Children.Add(AddText("QUYẾT ĐỊNH CHO LƯỢT HIỆN TẠI · TIÊU CHÍ: CẦN XÁC NHẬN", 10, "#49647A", 0, 0, 0, 7, true));
            var decisions = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            var decisionGroup = $"screening-{Guid.NewGuid():N}";
            foreach (var label in new[] { "Chỉ định tiêm", "Tạm hoãn", "Không chỉ định" })
                decisions.Children.Add(new RadioButton { Content = label, GroupName = decisionGroup, Margin = new Thickness(0, 3, 0, 3), Foreground = Brush("#434655"), FontSize = 11 });
            stack.Children.Add(decisions);
            stack.Children.Add(ActionRow("Mở biểu mẫu chi tiết", () => OpenDemoEditor("Biểu mẫu khám sàng lọc (minh họa)"), "Lưu kết quả giao diện", () =>
            {
                var selected = decisions.Children.OfType<RadioButton>().FirstOrDefault(option => option.IsChecked == true);
                if (selected is null) ShowFeedback("Chọn một trạng thái để thử phản hồi trên giao diện.");
                else ShowFeedback($"Đã chọn “{selected.Content}” cho lượt hiện tại. Không ghi hồ sơ y tế thật.");
            }));
            return stack;
        });
    }

    private void RenderClients()
    {
        AddSection("Hồ sơ khách hàng và người được tiêm", "Hồ sơ giả lập · thông tin chi tiết hiển thị trong giao diện này", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(ActionRow(_clientTab, () => { _clientTab = "Khách hàng"; RenderScreen(_currentScreen); }, _clientTab == "Khách hàng" ? "Người được tiêm" : "Khách hàng", () => { _clientTab = "Người được tiêm"; RenderScreen(_currentScreen); }));
            stack.Children.Add(SearchAction("Tìm kiếm hồ sơ minh họa", "Thêm mới", () => OpenDemoEditor($"Thêm {_clientTab.ToLowerInvariant()}")));
            stack.Children.Add(TableHeader("THÔNG TIN HỒ SƠ", "TRẠNG THÁI", "CẬP NHẬT", "THAO TÁC"));
            stack.Children.Add(TablePlaceholder());
            stack.Children.Add(ActionRow("Xem hồ sơ", () => ShowFeedback("Đang xem hồ sơ Nguyễn An Nhiên."), "Chỉnh sửa", () => OpenDemoEditor($"Chỉnh sửa {_clientTab.ToLowerInvariant()}")));
            return stack;
        });
    }

    private void RenderCatalog()
    {
        AddSection("Danh mục dịch vụ", "Thông tin dịch vụ", () =>
        {
            var stack = new StackPanel();
            var otherTabs = new[] { "Loại tiêm chủng", "Vắc xin", "Gói vắc xin" }.Where(tab => tab != _catalogTab).ToArray();
            stack.Children.Add(ActionRow(_catalogTab, () => { _catalogTab = "Loại tiêm chủng"; RenderScreen(_currentScreen); }, otherTabs[0], () => { _catalogTab = otherTabs[0]; RenderScreen(_currentScreen); }, otherTabs[1], () => { _catalogTab = otherTabs[1]; RenderScreen(_currentScreen); }));
            stack.Children.Add(SearchAction($"Tìm trong {_catalogTab}", "Thêm mới", () => OpenDemoEditor($"Thêm {_catalogTab.ToLowerInvariant()}")));
            stack.Children.Add(TableHeader("THÔNG TIN DANH MỤC", "MÔ TẢ", "TRẠNG THÁI", "THAO TÁC"));
            stack.Children.Add(TablePlaceholder());
            stack.Children.Add(ActionRow("Xem", () => ShowFeedback("Đang xem mục danh mục."), "Chỉnh sửa", () => OpenDemoEditor($"Chỉnh sửa {_catalogTab.ToLowerInvariant()}")));
            return stack;
        });
    }

    private void RenderVaccination()
    {
        AddSection("Quy trình giao diện", "Các bước tham khảo từ artboard Stitch", () =>
        {
            var stack = new StackPanel();
            foreach (var (number, label, detail) in new[]
            {
                ("01", "Tiếp nhận lượt tiêm", "Chỉ nhận lượt có kết quả khám sàng lọc: Chỉ định tiêm (minh họa)."),
                ("02", "Đối chiếu và kiểm tra", "Đối chiếu người được tiêm, kết quả chỉ định và vắc xin; kiểm tra vắc xin."),
                ("03", "Thực hiện tiêm và ghi nhận", "Ví dụ bản ghi: VX-DEMO-01 · mũi số 1 · 15/10/2026 (không phải hồ sơ y tế). Trường chính thức cần xác nhận theo đề cương."),
                ("04", "Theo dõi và hướng dẫn", "Thời lượng và tiêu chí xử lý sau tiêm cần xác nhận theo đề cương.")
            })
                stack.Children.Add(ProcessRow(number, label, detail));
            return stack;
        });
        AddSection("Lượt tiêm", "Danh sách lượt tiêm", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(SearchAction("Tìm lượt tiêm", "Làm mới", () => ShowFeedback("Đã làm mới trạng thái giao diện.")));
            stack.Children.Add(TableHeader("THÔNG TIN LƯỢT TIÊM", "DỊCH VỤ", "TRẠNG THÁI", "THAO TÁC"));
            stack.Children.Add(TablePlaceholder());
            stack.Children.Add(ActionRow("Xem", () => ShowFeedback("Đang xem lượt LH-DEMO-002."), "Ghi nhận", () => OpenDemoEditor("Ghi nhận lượt tiêm (minh họa)")));
            return stack;
        });
    }

    private void RenderRegistrations()
    {
        AddSection("Quy trình tiếp nhận", "Nội dung theo khung 5 bước trong Stitch; trường chi tiết đang chờ đề cương", () =>
        {
            var stack = new StackPanel();
            foreach (var (number, label, detail) in new[]
            {
                ("01", "Tiếp nhận yêu cầu đăng ký", "Lễ tân tiếp nhận, không quyết định điều kiện y tế."),
                ("02", "Xác định đối tượng", "Khách hàng và người được tiêm có thể là cùng hoặc khác người."),
                ("03", "Xác định dịch vụ và thời gian", "Dịch vụ mẫu và thời gian dự kiến; chi tiết cần xác nhận."),
                ("04", "Đối chiếu và hoàn thiện hồ sơ", "Trường hồ sơ còn thiếu sẽ được bổ sung theo đề cương."),
                ("05", "Hoàn tất đăng ký", "Trạng thái đăng ký chi tiết cần xác nhận theo đề cương.")
            })
                stack.Children.Add(ProcessRow(number, label, detail));
            return stack;
        });
        AddSection("Danh sách đăng ký", "Danh sách đăng ký", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(SearchAction("Tìm đăng ký", "Tiếp nhận đăng ký", () => OpenDemoEditor("Tiếp nhận đăng ký (minh họa)")));
            stack.Children.Add(TableHeader("THÔNG TIN ĐĂNG KÝ", "DỊCH VỤ", "THỜI GIAN", "THAO TÁC"));
            stack.Children.Add(TablePlaceholder());
            stack.Children.Add(ActionRow("Xem", () => ShowFeedback($"Chi tiết đăng ký: {Pending}."), "Chỉnh sửa", () => ShowFeedback($"Chỉnh sửa đăng ký: {Pending}.")));
            return stack;
        });
    }

    private void RenderPromotions()
    {
        AddSection("Chương trình ưu đãi", "Điều kiện áp dụng và đối tượng ưu đãi", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(SearchAction("Tìm chương trình", "Thêm chương trình", () => OpenDemoEditor("Thêm chương trình ưu đãi")));
            stack.Children.Add(ActionRow("Tất cả trạng thái", () => ShowFeedback("Đang hiển thị chương trình ưu đãi mẫu."), "Làm mới", () => ShowFeedback("Đã làm mới danh sách mẫu.")));
            stack.Children.Add(TableHeader("THÔNG TIN CHƯƠNG TRÌNH", "ĐIỀU KIỆN", "TRẠNG THÁI", "THAO TÁC"));
            stack.Children.Add(TablePlaceholder());
            stack.Children.Add(ActionRow("Xem", () => ShowFeedback("Ưu đãi THANG10 · giảm minh họa, cần xác nhận điều kiện."), "Chỉnh sửa", () => OpenDemoEditor("Chỉnh sửa chương trình ưu đãi")));
            return stack;
        });
    }

    private void RenderInventory()
    {
        AddSection("Quy trình nhận lô", "Theo nội dung artboard A05 · trường phiếu và tiêu chí kiểm tra cần xác nhận", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(ProcessRow("01", "Tiếp nhận", "Nhận lô vắc xin và chứng từ giao nhận từ nhà cung cấp."));
            stack.Children.Add(ProcessRow("02", "Đối chiếu và kiểm tra", "Đối chiếu thông tin, kiểm tra tình trạng lô."));
            stack.Children.Add(ProcessRow("03", "Xử lý theo kết quả", "Lô phù hợp: xác nhận và lập phiếu nhập. Chưa phù hợp: đối chiếu lại với nhà cung cấp."));
            stack.Children.Add(PendingBox("Ví dụ phiếu: nhà cung cấp DEMO · lô VX-DEMO-01 · 24 liều · hạn 10/2027. Cấu trúc phiếu và tiêu chí kiểm tra cần xác nhận theo đề cương."));
            return stack;
        });
        AddSection("Tồn kho theo lô", "Tồn kho theo lô", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(SearchAction("Tìm tên vắc xin hoặc mã lô", "Nhập lô minh họa", () => OpenDemoEditor("Phiếu nhập kho (minh họa)")));
            stack.Children.Add(TableHeader("VẮC XIN / MÃ LÔ", "HẠN DÙNG", "SỐ LƯỢNG MẪU", "TRẠNG THÁI"));
            stack.Children.Add(TablePlaceholder());
            stack.Children.Add(ActionRow("Xem lô", () => ShowFeedback("Đang xem lô VX-DEMO-01."), "Điều chỉnh tồn", () => OpenDemoEditor("Điều chỉnh tồn kho (minh họa)")));
            return stack;
        });
    }

    private void RenderPayments()
    {
        AddSection("Quy trình thanh toán", "Theo artboard A08 · không giả định mọi lượt tiêm thanh toán cùng thời điểm", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(ProcessRow("01", "Tiếp nhận và đối chiếu", "Thu ngân đối chiếu thông tin đăng ký tiêm."));
            stack.Children.Add(ProcessRow("02", "Xác định giá trị", "Giá trị theo dịch vụ; giá cụ thể cần bổ sung theo đề cương."));
            stack.Children.Add(ProcessRow("03", "Thực hiện giao dịch", "Phương thức thanh toán cần bổ sung theo đề cương."));
            stack.Children.Add(ProcessRow("04", "Xác nhận và ghi nhận", "Ghi nhận trạng thái tương ứng trên giao dịch minh họa."));
            return stack;
        });
        AddSection("Phiếu thanh toán", "Giá và trạng thái đều là dữ liệu giả; không phát sinh giao dịch", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(SearchAction("Tìm mã phiếu hoặc khách hàng", "Tạo phiếu mẫu", () => OpenDemoEditor("Tạo phiếu thanh toán (minh họa)")));
            stack.Children.Add(ActionRow("Tất cả", () => ShowFeedback("Bộ lọc: tất cả phiếu thanh toán mẫu."), "Chưa thanh toán", () => ShowFeedback("Bộ lọc: 2 phiếu chờ trong dữ liệu minh họa."), "Đã thanh toán", () => ShowFeedback("Bộ lọc: 1 phiếu đã thanh toán trong dữ liệu minh họa.")));
            stack.Children.Add(TableHeader("MÃ PHIẾU / KHÁCH HÀNG", "NỘI DUNG", "SỐ TIỀN MẪU", "TRẠNG THÁI"));
            stack.Children.Add(TablePlaceholder());
            stack.Children.Add(ActionRow("Xem phiếu", () => ShowFeedback("Phiếu TT-DEMO-001 · mức phí minh họa."), "Xác nhận trên giao diện", ToggleDemoPayment));
            return stack;
        });
    }

    private void RenderSchedule()
    {
        AddSection("Quy trình quản lý lịch và nhắc lịch", "Nhân viên CSKH không xác định mũi tiếp theo; cần tra cứu lịch sử và phác đồ chuyên môn", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(ProcessRow("01", "Tra cứu và xác định", "Tra cứu lịch sử tiêm/phác đồ để xác định mũi tiếp theo theo quy trình chuyên môn."));
            stack.Children.Add(ProcessRow("02", "Ghi nhận lịch hẹn", "Ghi nhận thời gian dự kiến vào giao diện lịch minh họa."));
            stack.Children.Add(ProcessRow("03", "Gửi nhắc và cập nhật", "Mô phỏng nhắc lịch; cập nhật nếu lịch hẹn thay đổi."));
            stack.Children.Add(ProcessRow("04", "Đến lịch và đối chiếu", "Đã thực hiện: cập nhật lịch sử. Chưa thực hiện: tiếp tục theo dõi."));
            stack.Children.Add(PendingBox("Ví dụ lịch: LH-DEMO-001 · 09:00 · 16/10/2026 · nhắc trạng thái “Chưa gửi (demo)”. Chu kỳ, kênh và thời điểm nhắc cần xác nhận theo đề cương."));
            return stack;
        });
        AddSection("Lịch hẹn trong ngày", "Khung giờ và quy tắc nhắc lịch là giá trị minh họa, cần xác nhận", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(ActionRow("‹ Ngày trước", () => ShowFeedback("Đã chuyển ngày xem lịch (minh họa)."), "Hôm nay · 15/10/2026", () => ShowFeedback("Đang xem lịch minh họa ngày 15/10/2026."), "Ngày sau ›", () => ShowFeedback("Đã chuyển ngày xem lịch (minh họa).")));
            stack.Children.Add(SearchAction("Tìm tên hoặc mã lịch hẹn", "Tạo lịch mẫu", () => OpenDemoEditor("Tạo lịch hẹn (minh họa)")));
            stack.Children.Add(TableHeader("GIỜ / MÃ LỊCH", "NGƯỜI ĐƯỢC TIÊM", "DỊCH VỤ MẪU", "NHẮC LỊCH"));
            stack.Children.Add(TablePlaceholder());
            stack.Children.Add(ActionRow("Xem lịch", () => ShowFeedback("Chi tiết lịch LH-DEMO-001."), "Gửi nhắc thử", ToggleDemoReminder));
            return stack;
        });
    }

    private void RenderReports()
    {
        AddSection("Báo cáo hoạt động", "Mọi số liệu, chỉ số và cách tính bên dưới chỉ để minh họa giao diện", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(ActionRow("Hôm nay", () => ShowFeedback("Đã lọc báo cáo mẫu theo hôm nay."), "7 ngày", () => ShowFeedback("Đã lọc báo cáo mẫu theo 7 ngày."), "Tháng này", () => ShowFeedback("Đã lọc báo cáo mẫu theo tháng.")));
            var stats = new UniformGrid { Columns = 3, Margin = new Thickness(0, 8, 0, 12) };
            foreach (var (label, value) in new[] { ("Lượt đăng ký", "42"), ("Lịch hẹn", "36"), ("Đã hoàn tất", "28") }) stats.Children.Add(StatTile(label, value));
            stack.Children.Add(stats);
            stack.Children.Add(PendingBox("Ví dụ chỉ số: số lượt tiếp nhận trong kỳ. Công thức và biểu mẫu báo cáo chính thức cần xác nhận theo đề cương."));
            stack.Children.Add(ActionRow("Xuất bảng mẫu", () => ShowFeedback("Đã tạo phản hồi xuất bảng minh họa; không tạo tệp thật."), "In báo cáo", () => ShowFeedback("Bản in báo cáo chỉ là thao tác minh họa.")));
            return stack;
        });
    }

    private void RenderAccounts()
    {
        AddSection("Tài khoản nội bộ", "Tài khoản và phân quyền giả lập · ma trận quyền cần xác nhận theo đề cương", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(ActionRow(_accountTab, () => { _accountTab = "Tài khoản"; RenderScreen(_currentScreen); }, _accountTab == "Tài khoản" ? "Phân quyền" : "Tài khoản", () => { _accountTab = "Phân quyền"; RenderScreen(_currentScreen); }));
            stack.Children.Add(SearchAction("Tìm tài khoản", "Thêm tài khoản mẫu", () => OpenDemoEditor("Thêm tài khoản nội bộ (minh họa)")));
            stack.Children.Add(TableHeader("TÀI KHOẢN", "VAI TRÒ MẪU", "PHẠM VI MẪU", "TRẠNG THÁI"));
            stack.Children.Add(TablePlaceholder());
            stack.Children.Add(ActionRow("Xem quyền", () => ShowFeedback("Ma trận quyền đang ở mức minh họa, cần xác nhận theo đề cương."), "Đổi trạng thái", ToggleDemoAccount));
            return stack;
        });
    }

    private void RenderLoginInfo()
    {
        AddSection("Phiên đăng nhập demo", "Đăng nhập cục bộ để trình diễn điều hướng; không dùng dịch vụ xác thực", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(PendingBox($"Tài khoản hiện tại: {_demoUser}. Bộ dữ liệu đang dùng là dữ liệu minh họa."));
            stack.Children.Add(ActionRow("Đăng xuất", () => Logout_Click(this, new RoutedEventArgs()), "Thông tin đăng nhập demo", () => ShowFeedback("Thông tin đăng nhập demo không còn được hiển thị trong giao diện này.")));
            return stack;
        });
    }

    private void RenderPasswordForm()
    {
        AddSection("Đổi mật khẩu", "Biểu mẫu tham khảo; không thay đổi thông tin đăng nhập demo", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(PendingBox("Quy tắc xác thực hiển thị để minh họa và cần xác nhận theo đề cương. Các giá trị nhập vào không được lưu."));
            var current = AddPasswordField(stack, "Mật khẩu hiện tại");
            var next = AddPasswordField(stack, "Mật khẩu mới");
            var confirm = AddPasswordField(stack, "Xác nhận mật khẩu mới");
            stack.Children.Add(ActionRow("Xóa trường", () => { current.Clear(); next.Clear(); confirm.Clear(); ShowFeedback("Đã xóa nội dung các trường trong biểu mẫu."); }, "Xác nhận đổi mật khẩu", () =>
            {
                if (string.IsNullOrWhiteSpace(current.Password) || string.IsNullOrWhiteSpace(next.Password)) ShowFeedback("Nhập các trường minh họa trước khi kiểm tra biểu mẫu.");
                else if (next.Password != confirm.Password) ShowFeedback("Mật khẩu mới và phần xác nhận chưa khớp.");
                else ShowFeedback("Biểu mẫu demo hợp lệ. Mật khẩu đăng nhập không thay đổi.");
            }));
            return stack;
        });
    }

    private PasswordBox AddPasswordField(Panel panel, string label)
    {
        panel.Children.Add(AddText(label, 10, "#49647A", 0, 0, 0, 5, true));
        var input = new PasswordBox { Height = 36, Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 12), BorderBrush = Brush("#DDE6ED"), Background = Brushes.White, VerticalContentAlignment = VerticalAlignment.Center };
        panel.Children.Add(input);
        return input;
    }

    private void RenderWaitingScreen()
    {
        AddSection("Thông tin màn hình", "", () =>
        {
            var stack = new StackPanel();
            stack.Children.Add(PendingBox("Khu vực mẫu giữ chỗ cho thông tin chưa có căn cứ. Ví dụ giá trị, trường dữ liệu và quy tắc cần xác nhận theo đề cương."));
            stack.Children.Add(ActionRow("Xem trạng thái chờ", () => ShowFeedback($"{PageTitle.Text}: {Pending}."), "Về tổng quan", () => NavigateTo("A01")));
            return stack;
        });
    }

    private Border StatTile(string label)
    {
        var tone = StatTone(label);
        var stack = new StackPanel();
        stack.Children.Add(AddText(label, 9, "#71869A", 0, 0, 0, 10, true));
        var value = _currentScreen switch { "A01" => label switch { "LƯỢT ĐĂNG KÝ" => "128", "LỊCH HẸN" => "95", "LƯỢT TIÊM" => "64", _ => "58.450.000 đ" }, _ => "0" };
        stack.Children.Add(AddText(value, 15, tone.Accent, 0, 0, 0, 0, true));
        return new Border { Background = Brush(tone.Tint), BorderBrush = Brush(tone.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Padding = new Thickness(13), Margin = new Thickness(0, 0, 8, 0), Child = stack };
    }

    private Border StatTile(string label, string value)
    {
        var tone = StatTone(label);
        var stack = new StackPanel();
        stack.Children.Add(AddText(label, 9, "#71869A", 0, 0, 0, 10, true));
        stack.Children.Add(AddText(value, 15, tone.Accent, 0, 0, 0, 0, true));
        return new Border { Background = Brush(tone.Tint), BorderBrush = Brush(tone.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Padding = new Thickness(13), Margin = new Thickness(0, 0, 8, 0), Child = stack };
    }

    private static (string Accent, string Tint, string Border) StatTone(string label) => label switch
    {
        "LƯỢT ĐĂNG KÝ" or "Lượt đăng ký" => ("#6257C9", "#F1EFFF", "#E2DFFF"),
        "LỊCH HẸN" or "Lịch hẹn" => ("#168875", "#EAF8F3", "#D5EEE6"),
        "LƯỢT TIÊM" or "Đã hoàn tất" => ("#2778C4", "#EAF4FF", "#D8E9FA"),
        "THANH TOÁN" => ("#B36D19", "#FFF4E7", "#F3E1C8"),
        _ => ("#6257C9", "#F1EFFF", "#E2DFFF")
    };

    private void AddSection(string title, string subtitle, Func<UIElement> contentFactory)
    {
        var panel = new StackPanel();
        panel.Children.Add(AddText(title, 15, "#214462", 0, 0, 0, 5, true));
        if (!string.IsNullOrWhiteSpace(subtitle) && !IsDemoDisclaimer(subtitle))
            panel.Children.Add(AddText(subtitle, 11, "#8091A1", 0, 0, 0, 14));
        panel.Children.Add(contentFactory());
        var card = new Border { Background = Brushes.White, BorderBrush = Brush("#E5EBF0"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(13), Padding = new Thickness(20), Margin = new Thickness(0, 0, 0, 14), Child = panel };
        ScreenContent.Children.Add(card);
    }

    private void AddNotice(string text, bool muted)
    {
        var banner = new Border { Background = Brush(muted ? "#FFF8E9" : "#EDF6FC"), BorderBrush = Brush(muted ? "#F1E3C1" : "#DDECF7"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Padding = new Thickness(13, 10, 13, 10), Margin = new Thickness(0, 0, 0, 16) };
        banner.Child = AddText($"ⓘ  {text}", 11, muted ? "#8A6D31" : "#3B6F95", 0, 0, 0, 0);
        ScreenContent.Children.Add(banner);
    }

    private Border PendingBox(string detail) => new() { Visibility = Visibility.Collapsed };

    private UIElement TableHeader(params string[] labels)
    {
        var grid = new UniformGrid { Columns = labels.Length, Margin = new Thickness(0, 9, 0, 0) };
        foreach (var label in labels)
        {
            var cell = new Border { Background = Brush("#F5F8FA"), Padding = new Thickness(9, 11, 9, 11) };
            cell.Child = AddText(label, 9, "#6F8294", 0, 0, 0, 0, true);
            grid.Children.Add(cell);
        }
        return grid;
    }

    private UIElement TablePlaceholder()
    {
        var rowsByScreen = _currentScreen switch
        {
            "A01" => new[] { ("LH-DEMO-001", "Lê An Nhiên · khách mẫu", "Vắc xin mẫu A", "Chờ khám"), ("LH-DEMO-002", "Trần Gia Bảo · khách mẫu", "Gói mẫu B", "Chờ tiêm"), ("LH-DEMO-003", "Phạm Minh Khoa · khách mẫu", "Vắc xin mẫu C", "Hoàn tất") },
            "A02" => new[] { ("LH-DEMO-001 · 08:15", "Lê An Nhiên · hồ sơ mẫu", "Khám theo lượt", "Đang khám"), ("LH-DEMO-004 · 08:30", "Trần Gia Bảo · hồ sơ mẫu", "Khám theo lượt", "Chờ khám"), ("LH-DEMO-005 · 09:00", "Phạm Minh Khoa · hồ sơ mẫu", "Khám theo lượt", "Chờ khám") },
            "A03" => _clientTab == "Khách hàng"
                ? new[] { ("KH-DEMO-001 · Lê An Nhiên", "0900 000 101", "15/10/2026", "Đang hoạt động"), ("KH-DEMO-002 · Trần Gia Bảo", "0900 000 102", "14/10/2026", "Đang hoạt động"), ("KH-DEMO-003 · Phạm Minh Khoa", "0900 000 103", "12/10/2026", "Chờ xác nhận") }
                : new[] { ("DT-DEMO-001 · Lê An Nhiên", "Người thân · mẫu", "Hồ sơ minh họa", "Đang hoạt động"), ("DT-DEMO-002 · Trần Gia Bảo", "Chính khách hàng", "Hồ sơ minh họa", "Đang hoạt động"), ("DT-DEMO-003 · Phạm Minh Khoa", "Người thân · mẫu", "Hồ sơ minh họa", "Chờ xác nhận") },
            "A04" => _catalogTab switch
            {
                "Vắc xin" => new[] { ("Vắc xin mẫu A", "Mô tả minh họa", "450.000 đ · mẫu", "Đang hiển thị") , ("Vắc xin mẫu B", "Mô tả minh họa", "620.000 đ · mẫu", "Đang hiển thị") },
                "Gói vắc xin" => new[] { ("Gói mẫu Gia đình", "Nhóm dịch vụ minh họa", "1.200.000 đ · mẫu", "Đang hiển thị") , ("Gói mẫu Cơ bản", "Nhóm dịch vụ minh họa", "850.000 đ · mẫu", "Tạm ẩn") },
                _ => new[] { ("Loại tiêm mẫu A", "Mô tả minh họa", "Dữ liệu mẫu", "Đang hiển thị"), ("Loại tiêm mẫu B", "Mô tả minh họa", "Dữ liệu mẫu", "Đang hiển thị") }
            },
            "A05" => new[] { ("VX-DEMO-01 · Vắc xin mẫu A", "31/12/2026 · mẫu", "24 liều · mẫu", "Còn hàng"), ("VX-DEMO-02 · Vắc xin mẫu B", "30/04/2027 · mẫu", "8 liều · mẫu", "Sắp hết") },
            "A06" => new[] { ("LH-DEMO-002 · Trần Gia Bảo", "Vắc xin mẫu B", "Mũi minh họa", "Chờ thực hiện"), ("LH-DEMO-003 · Phạm Minh Khoa", "Vắc xin mẫu C", "Mũi minh họa", "Theo dõi mẫu") },
            "A07" => new[] { ("DK-DEMO-001 · Lê An Nhiên", "Vắc xin mẫu A", "15/10/2026 · mẫu", "Chờ xác nhận"), ("DK-DEMO-002 · Trần Gia Bảo", "Gói mẫu B", "16/10/2026 · mẫu", "Đã tiếp nhận"), ("DK-DEMO-003 · Phạm Minh Khoa", "Vắc xin mẫu C", "17/10/2026 · mẫu", "Đang xử lý") },
            "A08" => new[] { ("TT-DEMO-001 · Lê An Nhiên", "Vắc xin mẫu A", "450.000 đ · mẫu", "Chờ thanh toán"), ("TT-DEMO-002 · Trần Gia Bảo", "Gói mẫu B", "1.200.000 đ · mẫu", "Đã thanh toán"), ("TT-DEMO-003 · Phạm Minh Khoa", "Vắc xin mẫu C", "620.000 đ · mẫu", "Chờ thanh toán") },
            "A09" => new[] { ("08:15 · LH-DEMO-001", "Lê An Nhiên · mẫu", "Vắc xin mẫu A", "Chưa nhắc"), ("09:30 · LH-DEMO-002", "Trần Gia Bảo · mẫu", "Gói mẫu B", "Đã nhắc thử"), ("10:00 · LH-DEMO-003", "Phạm Minh Khoa · mẫu", "Vắc xin mẫu C", "Chưa nhắc") },
            "A10" => new[] { ("KM-DEMO-01 · THANG10", "Giảm 10% · mẫu", "01–31/10/2026 · mẫu", "Đang áp dụng"), ("KM-DEMO-02 · CHAOHE", "Giảm 5% · mẫu", "01–30/11/2026 · mẫu", "Bản nháp") },
            "A12" => _accountTab == "Tài khoản"
                ? new[] { ("admin · tài khoản mẫu", "Quản trị viên · mẫu", "Cơ sở demo", "Đang hoạt động"), ("nv-demo-01", "Nhân viên tiếp nhận · mẫu", "Cơ sở demo", "Đang hoạt động"), ("bs-demo-01", "Nhân viên chuyên môn · mẫu", "Cơ sở demo", "Tạm khóa") }
                : new[] { ("Quản trị viên · mẫu", "Tài khoản / danh mục", "Quyền demo", "Minh họa"), ("Tiếp nhận · mẫu", "Hồ sơ / đăng ký", "Quyền demo", "Minh họa"), ("Chuyên môn · mẫu", "Khám / tiêm", "Quyền demo", "Minh họa") },
            _ => new[] { ("Mã mẫu", "Bản ghi minh họa", "Chi tiết minh họa", "Trạng thái mẫu") }
        };

        var dataRows = rowsByScreen.Select(item => new[] { item.Item1, item.Item2, item.Item3, item.Item4 }).ToList();
        if (_demoRowEdits.TryGetValue(_currentScreen, out var editedRow) && dataRows.Count > 0) dataRows[0] = editedRow;
        if (_currentScreen == "A08" && dataRows.Count > 0) dataRows[0][3] = _demoPaymentPaid ? "Đã thanh toán demo" : "Chờ thanh toán · mẫu";
        if (_currentScreen == "A09" && dataRows.Count > 0) dataRows[0][3] = _demoReminderSent ? "Đã nhắc thử" : "Chưa nhắc · mẫu";
        if (_currentScreen == "A12" && _accountTab == "Tài khoản" && dataRows.Count > 0) dataRows[0][3] = _demoAccountEnabled ? "Đang hoạt động · mẫu" : "Tạm khóa · mẫu";
        if (_addedDemoRows.TryGetValue(_currentScreen, out var addedRows)) dataRows.AddRange(addedRows);

        var rows = new StackPanel();
        foreach (var values in dataRows)
        {
            var grid = new UniformGrid { Columns = 4 };
            foreach (var value in values)
            {
                var cell = new Border { Background = Brushes.White, BorderBrush = Brush("#E9EEF2"), BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(9, 12, 7, 12) };
                cell.Child = AddText(value, 10, "#49647A", 0, 0, 0, 0);
                grid.Children.Add(cell);
            }
            grid.Tag = string.Join(" ", values);
            _activeDemoRows.Add(grid);
            rows.Children.Add(grid);
        }
                return rows;
    }

    private UIElement ProcessRow(string number, string title, string detail)
    {
        var tone = ProcessTone(number, title);
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(43) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var badge = new Border { Width = 31, Height = 31, CornerRadius = new CornerRadius(9), Background = Brush(tone.Accent), Child = AddText(number, 10, "#FFFFFF", 0, 0, 0, 0, true), VerticalAlignment = VerticalAlignment.Top };
        Grid.SetColumn(badge, 0);
        var details = new StackPanel();
        details.Children.Add(AddText(title, 12, "#29445D", 0, 0, 0, 4, true));
        if (!IsDemoDisclaimer(detail)) details.Children.Add(AddText(detail, 10, "#6F8294", 0, 0, 0, 0));
        Grid.SetColumn(details, 1);
        grid.Children.Add(badge);
        grid.Children.Add(details);
        var border = new Border { Background = Brush(tone.Tint), BorderBrush = Brush(tone.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Padding = new Thickness(11), Margin = new Thickness(0, 0, 0, 5), Child = grid };
        return border;
    }

    private static (string Accent, string Tint, string Border) ProcessTone(string number, string title)
    {
        if (title.Contains("thanh toán", StringComparison.OrdinalIgnoreCase)) return ("#BE741C", "#FFF5E9", "#F3E2CC");
        if (title.Contains("khám", StringComparison.OrdinalIgnoreCase)) return ("#7259CB", "#F4F0FF", "#E5DDF8");
        if (title.Contains("theo dõi", StringComparison.OrdinalIgnoreCase)) return ("#168875", "#ECF8F3", "#D7EEE6");
        if (title.Contains("tiêm", StringComparison.OrdinalIgnoreCase)) return ("#2879C5", "#EDF6FF", "#D8EAF9");
        if (number.StartsWith("16", StringComparison.Ordinal)) return ("#168875", "#ECF8F3", "#D7EEE6");
        if (number.StartsWith("17", StringComparison.Ordinal)) return ("#BE741C", "#FFF5E9", "#F3E2CC");

        return number switch
        {
            "01" => ("#6257C9", "#F4F0FF", "#E5DDF8"),
            "02" => ("#168875", "#ECF8F3", "#D7EEE6"),
            "03" => ("#BE741C", "#FFF5E9", "#F3E2CC"),
            "04" => ("#2879C5", "#EDF6FF", "#D8EAF9"),
            _ => ("#2879C5", "#EDF6FF", "#D8EAF9")
        };
    }

    private static bool IsDemoDisclaimer(string text) =>
        text.Contains("minh họa", StringComparison.OrdinalIgnoreCase)
        || text.Contains("cần xác nhận", StringComparison.OrdinalIgnoreCase)
        || text.Contains("cần bổ sung", StringComparison.OrdinalIgnoreCase)
        || text.Contains("chờ đề cương", StringComparison.OrdinalIgnoreCase)
        || text.Contains("dữ liệu giả", StringComparison.OrdinalIgnoreCase)
        || text.Contains("giả lập", StringComparison.OrdinalIgnoreCase);

    private UIElement SearchAction(string placeholder, string actionLabel, Action action)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 11) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var search = new TextBox { Height = 36, VerticalContentAlignment = VerticalAlignment.Center, Padding = new Thickness(10, 4, 10, 4), BorderBrush = Brush("#DDE6ED"), Background = Brush("#FAFCFD"), ToolTip = placeholder };
        var watermark = AddText(placeholder, 11, "#93A2AF", 11, 0, 8, 0);
        watermark.VerticalAlignment = VerticalAlignment.Center;
        watermark.IsHitTestVisible = false;
        search.TextChanged += (_, _) =>
        {
            watermark.Visibility = string.IsNullOrEmpty(search.Text) ? Visibility.Visible : Visibility.Collapsed;
            foreach (var row in _activeDemoRows)
                row.Visibility = string.IsNullOrWhiteSpace(search.Text) || row.Tag?.ToString()?.Contains(search.Text, StringComparison.CurrentCultureIgnoreCase) == true ? Visibility.Visible : Visibility.Collapsed;
        };
        search.PreviewKeyDown += (_, args) =>
        {
            if (args.Key != Key.Enter) return;
            args.Handled = true;
            ShowFeedback($"Đã lọc dữ liệu minh họa theo “{search.Text}”.");
        };
        var searchHost = new Grid();
        searchHost.Children.Add(search);
        searchHost.Children.Add(watermark);
        grid.Children.Add(searchHost);
        var button = MakeButton(actionLabel, action, true);
        button.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetColumn(button, 1);
        grid.Children.Add(button);
        return grid;
    }

    private UIElement ActionRow(string firstLabel, Action firstAction, string secondLabel, Action secondAction)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 11, 0, 0) };
        panel.Children.Add(MakeButton(firstLabel, firstAction));
        var second = MakeButton(secondLabel, secondAction, true);
        second.Margin = new Thickness(8, 0, 0, 0);
        panel.Children.Add(second);
        return panel;
    }

    private UIElement ActionRow(string firstLabel, Action firstAction, string secondLabel, Action secondAction, string thirdLabel, Action thirdAction)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 8) };
        foreach (var (label, action) in new[] { (firstLabel, firstAction), (secondLabel, secondAction), (thirdLabel, thirdAction) })
        {
            var button = MakeButton(label, action, label != firstLabel);
            button.Margin = new Thickness(0, 0, 7, 0);
            panel.Children.Add(button);
        }
        return panel;
    }

    private UiButton MakeButton(string label, Action action, bool secondary = false)
    {
        var button = new UiButton
        {
            Content = label,
            Padding = new Thickness(13, 7, 13, 7),
            MinHeight = 34,
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Background = Brush(secondary ? "#F1F5F9" : "#2563EB"),
            Foreground = secondary ? Brush("#3A6588") : Brushes.White,
            BorderBrush = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        button.Click += (_, _) => action();
        return button;
    }

    private void OpenDemoEditor(string title)
    {
        var editor = new Window
        {
            Title = title.Replace(" (minh họa)", ""),
            FontFamily = new FontFamily("Segoe UI"),
            Width = 520,
            Height = 570,
            MinHeight = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Background = Brush("#F5F8FB"),
            ResizeMode = ResizeMode.NoResize
        };
        var panel = new StackPanel { Margin = new Thickness(26) };
        panel.Children.Add(AddText(title.Replace(" (minh họa)", ""), 20, "#193A58", 0, 0, 0, 5, true));
        var editorFields = new List<TextBox>();
        foreach (var (label, hint) in new[] { ("Tên / nội dung", "Vắc xin A"), ("Mã tham chiếu", "VX-001"), ("Ngày / thời gian", "15/10/2026"), ("Ghi chú", "Ghi chú") })
        {
            panel.Children.Add(AddText(label, 10, "#49647A", 0, 0, 0, 5, true));
            var field = new TextBox { Text = hint, Height = 36, Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 12), BorderBrush = Brush("#DDE6ED"), Background = Brushes.White, VerticalContentAlignment = VerticalAlignment.Center };
            editorFields.Add(field);
            panel.Children.Add(field);
        }
        panel.Children.Add(PendingBox("Các giá trị đang là dữ liệu minh họa; tên trường và quy tắc cần xác nhận theo đề cương."));
        panel.Children.Add(ActionRow("Hủy", editor.Close, "Áp dụng trên giao diện", () =>
        {
            var values = editorFields.Select(field => field.Text.Trim()).ToArray();
            if (title.StartsWith("Chỉnh sửa", StringComparison.OrdinalIgnoreCase))
                _demoRowEdits[_currentScreen] = values;
            else
            {
                if (!_addedDemoRows.TryGetValue(_currentScreen, out var rows)) _addedDemoRows[_currentScreen] = rows = [];
                rows.Add(values);
            }
            editor.Close();
            RenderScreen(_currentScreen);
            ShowFeedback("Đã cập nhật danh sách.");
        }));
        editor.Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        editor.ShowDialog();
    }

    private TextBlock AddText(string value, double size, string color, double left, double top, double right, double bottom, bool bold = false) =>
        new() { Text = value, FontSize = size, FontFamily = new FontFamily("Segoe UI"), Foreground = Brush(color), Margin = new Thickness(left, top, right, bottom), FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, TextWrapping = TextWrapping.Wrap };

    private void NavigateTo(string id)
    {
        RenderScreen(id);
        foreach (var button in NavigationStack.Children.OfType<UiButton>())
        {
            var selected = Equals(button.Tag, id);
            button.Background = selected ? Brush("#DCE9FF") : Brushes.Transparent;
            button.Foreground = selected ? Brush("#004AC6") : Brush("#434655");
        }
    }

    private void ShowFeedback(string text)
    {
        FeedbackText.Text = text;
        FeedbackBanner.Visibility = Visibility.Visible;
        _feedbackTimer.Stop();
        _feedbackTimer.Start();
    }

    private void ToggleDemoPayment()
    {
        _demoPaymentPaid = !_demoPaymentPaid;
        RenderScreen("A08");
        ShowFeedback(_demoPaymentPaid ? "Đã đổi trạng thái phiếu mẫu trong giao diện; không có giao dịch thật." : "Phiếu mẫu đã đặt lại về trạng thái chờ.");
    }

    private void ToggleDemoReminder()
    {
        _demoReminderSent = !_demoReminderSent;
        RenderScreen("A09");
        ShowFeedback(_demoReminderSent ? "Đã đánh dấu nhắc thử trong giao diện; không gửi tin nhắn thật." : "Đã đặt lại trạng thái nhắc của lịch mẫu.");
    }

    private void ToggleDemoAccount()
    {
        _demoAccountEnabled = !_demoAccountEnabled;
        RenderScreen("A12");
        ShowFeedback(_demoAccountEnabled ? "Tài khoản mẫu đang hoạt động trong giao diện." : "Tài khoản mẫu đã chuyển sang tạm khóa trong giao diện.");
    }

    private static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
