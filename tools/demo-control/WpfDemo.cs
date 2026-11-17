
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Forms;

public static class DemoWpf {
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr hwnd, StringBuilder title, int capacity);
    [DllImport("user32.dll")] static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out int pid);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern bool ShowWindowAsync(IntPtr h, int mode);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr data);
    [DllImport("user32.dll")] static extern uint SendInput(uint count, Input[] inputs, int size);

    [StructLayout(LayoutKind.Sequential)] struct Input { public uint Type; public InputData Data; }
    [StructLayout(LayoutKind.Explicit)] struct InputData {
        [FieldOffset(0)] public KeyData Key;
        [FieldOffset(0)] public MouseData Mouse;
    }
    [StructLayout(LayoutKind.Sequential)] struct KeyData {
        public ushort Vk, Scan;
        public uint Flags, Time;
        public UIntPtr Extra;
    }
    [StructLayout(LayoutKind.Sequential)] struct MouseData {
        public int X, Y;
        public uint Data, Flags, Time;
        public UIntPtr Extra;
    }

    sealed class Banner : Form {
        public readonly Label TextLabel = new Label { Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleCenter };
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams {
            get { var p = base.CreateParams; p.ExStyle |= 0x08000080; return p; }
        }
        public readonly Button ReturnToMenu = new Button { Text = "Quay về menu", Dock = DockStyle.Right, Width = 115 };
        public readonly Button Pause = new Button { Text = "Pause/Resume", Dock = DockStyle.Right, Width = 105 };
        public readonly Button Stop = new Button { Text = "Stop", Dock = DockStyle.Right, Width = 60 };
        public Banner() {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = System.Drawing.Color.MidnightBlue;
            ForeColor = System.Drawing.Color.White;
            ClientSize = new System.Drawing.Size(680, 35);
            Controls.Add(TextLabel);
            Controls.Add(Stop);
            Controls.Add(Pause);
            Controls.Add(ReturnToMenu);
        }
    }

    sealed class Session {
        readonly Process wpf;
        readonly Process owner;
        readonly string username, password, customer, child;
        readonly bool rehearse, escapeTest;
        readonly string scene;
        readonly bool story;
        readonly int registrationId;
        readonly string lotNumber, receptionUsername;
        readonly string[] sceneSteps;
        int exitCode;
        readonly int minDelay, maxDelay;
        readonly Random random = new Random();
        readonly Banner banner = new Banner();
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 20 };
        readonly bool[] held = new bool[3];
        readonly int[] keys = { 0x77, 0x78, 0x1b };
        volatile bool paused, stopping, busy;
        volatile int next = 7;
        string status = "F8 #7 · Đang chuyển sang WPF";
        Task task;
        Task nativeTest;
        long lastF8, sceneReadyAt;
        volatile bool failed, finished;
        public Session(Process wpf, Process owner, string username, string password, string customer, string child,
                       int minDelay, int maxDelay, bool rehearse, bool escapeTest, string scene, int registrationId, bool story, string lotNumber, string receptionUsername) {
            this.wpf = wpf;
            this.owner = owner;
            this.username = username;
            this.password = password;
            this.customer = customer;
            this.child = child;
            this.minDelay = minDelay;
            this.maxDelay = maxDelay;
            this.rehearse = rehearse;
            this.escapeTest = escapeTest;
            this.scene = scene;
            this.story = story;
            this.registrationId = registrationId;
            this.lotNumber = lotNumber;
            this.receptionUsername = receptionUsername;
            this.sceneSteps = String.IsNullOrEmpty(scene) ? null : SceneSteps();
            banner.ReturnToMenu.Click += (sender, args) => { Print("MENU WPF"); stopping = true; };
            banner.Stop.Click += (sender, args) => { Print("MENU WPF"); stopping = true; };
            banner.Pause.Click += (sender, args) => TogglePause();
        }
        static void Print(string message) { Console.WriteLine(message); Console.Out.Flush(); }
        bool Foreground() {
            int pid;
            GetWindowThreadProcessId(GetForegroundWindow(), out pid);
            return pid == wpf.Id;
        }
        IntPtr Window() {
            wpf.Refresh();
            return wpf.MainWindowHandle;
        }
        void Activate() {
            var handle = Window();
            if (handle == IntPtr.Zero) return;
            if (IsIconic(handle)) { ShowWindowAsync(handle, 9); Thread.Sleep(350); }
            SetForegroundWindow(handle);
            if (!Foreground()) {
                // An Alt press releases the Windows foreground lock without typing into the app.
                keybd_event(0x12, 0, 0, UIntPtr.Zero);
                keybd_event(0x12, 0, 2, UIntPtr.Zero);
                SetForegroundWindow(handle);
            }
        }
        void UpdateBanner(bool foreground) {
            if (!foreground) { if (banner.Visible) banner.Hide(); return; }
            var area = Screen.FromHandle(Window()).WorkingArea;
            banner.SetDesktopLocation(area.Right - banner.Width - 16, area.Bottom - banner.Height - 16);
            banner.TextLabel.Text = (story ? Chapter() + "/10 · " : "") + status;
            if (!banner.Visible) banner.Show();
        }
        public int Run() {
            for (int i = 0; i < keys.Length; i++) held[i] = Down(keys[i]);
            Activate();
            if (!Foreground()) {
                status = "Chuyển sang WPF bằng Alt+Tab";
                Print("WAIT_FOCUS");
            } else Print("FOREGROUND #7");
            var deadline = DateTime.UtcNow.AddSeconds(20);
            timer.Tick += (sender, args) => {
                if (wpf.HasExited || (owner != null && owner.HasExited)) { stopping = true; }
                if (task != null && task.IsCompleted) {
                    busy = false;
                    if (task.IsFaulted && !stopping) {
                        failed = true;
                        Print("BLOCKED F8 #" + next + " — " + task.Exception.InnerException.Message);
                        stopping = true;
                    }
                    task = null;
                }
                if (!stopping && nativeTest != null && nativeTest.IsFaulted) {
                    failed = true;
                    stopping = true;
                    Print("BLOCKED native test — " + nativeTest.Exception.InnerException.Message);
                    nativeTest = null;
                }
                if (stopping || (finished && (!rehearse || nativeTest == null || nativeTest.IsCompleted))) {
                    if (task != null) return;
                    timer.Stop();
                    banner.Close();
                    Application.ExitThread();
                    return;
                }
                bool foreground = Foreground();
                UpdateBanner(foreground);
                if (!foreground && next == 7 && task == null && DateTime.UtcNow > deadline) {
                    failed = true;
                    Print("BLOCKED F8 #7 — WPF chưa có focus; dùng Alt+Tab và thao tác tay");
                    stopping = true;
                    return;
                }
                if (foreground && next == 7 && task == null && !paused) {
                    Start();
                    if (rehearse && sceneSteps == null && nativeTest == null) nativeTest = Task.Run((Action)Rehearse);
                }
                if (rehearse && sceneSteps != null && foreground && next >= 8 && !busy && !paused &&
                    DateTime.UtcNow.Ticks - sceneReadyAt > TimeSpan.FromSeconds(3).Ticks) Start();
                for (int i = 0; i < keys.Length; i++) {
                    bool down = Down(keys[i]);
                    bool edge = down && !held[i];
                    held[i] = down;
                    if (!edge || !foreground || Down(0x12) || Down(0x11) || Down(0x10)) continue;
                    if (i == 2) {
                        Print(sceneSteps == null ? "MANUAL WPF — Esc" : "MENU WPF");
                        stopping = true;
                        break;
                    }
                    if (i == 1) {
                        TogglePause();
                    }
                    if (i == 0 && next >= 8 && !busy && !paused &&
                        DateTime.UtcNow.Ticks - lastF8 > TimeSpan.FromMilliseconds(700).Ticks) Start();
                }
            };
            timer.Start();
            Application.Run();
            return failed ? 1 : exitCode;
        }
        void TogglePause() {
            paused = !paused;
            status = paused ? "PAUSED · F9 tiếp tục · Esc dừng" : "F8 · " + (busy ? "Đang chạy" : "READY");
            Print(paused ? "PAUSED WPF — F9" : "RESUMED WPF — F9");
        }
        static bool Down(int key) { return (GetAsyncKeyState(key) & 0x8000) != 0; }
        void Start() {
            int stage = next;
            lastF8 = DateTime.UtcNow.Ticks;
            busy = true;
            status = "RUNNING · F8 #" + stage + " · F9 tạm dừng · Esc thao tác tay";
            task = Task.Run(() => {
                if (sceneSteps != null) {
                    if (stage == 7) {
                        if (SwitchAccount()) { exitCode = 12; stopping = true; return; }
                        Login();
                        RunScene(sceneSteps[0]);
                        next = 8;
                    } else if (stage - 7 < sceneSteps.Length) {
                        RunScene(sceneSteps[stage - 7]);
                        next = stage + 1;
                    } else {
                        Print("NEXT WPF");
                        stopping = true;
                        return;
                    }
                    sceneReadyAt = DateTime.UtcNow.Ticks;
                    status = next - 7 < sceneSteps.Length ? "READ · F8 màn tiếp theo" : "READ · F8 hoàn tất cảnh";
                    return;
                }
                switch (stage) {
                    case 7: Login(); break;
                    case 8: Profiles(false); break;
                    case 9: Profiles(true); break;
                    case 10: RequireForeground(); break;
                }
                Gate();
                Print("PASS F8 #" + stage + " — WPF " + new[] { "Lễ tân", "người được tiêm lịch sử", "người được tiêm mới", "kết thúc" }[stage - 7]);
                next = stage + 1;
                status = "READY · F8 #" + next + " · F9 tạm dừng · Esc thao tác tay";
                if (stage == 10) finished = true;
            });
        }
        void Gate() {
            while (paused && !stopping) Thread.Sleep(25);
            if (stopping) throw new OperationCanceledException();
        }
        void RequireForeground() {
            Gate();
            if (!Foreground()) throw new InvalidOperationException("WPF đã mất focus");
        }
        AutomationElement Root() {
            var handle = Foreground() ? GetForegroundWindow() : Window();
            if (handle == IntPtr.Zero) return null;
            return AutomationElement.FromHandle(handle);
        }
        static AutomationElement Find(AutomationElement root, string name, ControlType type) {
            if (root == null) return null;
            Condition condition = new PropertyCondition(AutomationElement.NameProperty, name);
            if (type != null) condition = new AndCondition(condition, new PropertyCondition(AutomationElement.ControlTypeProperty, type));
            return root.FindFirst(TreeScope.Descendants, condition);
        }
        AutomationElement Await(Func<AutomationElement> get, string description, int ms = 10000) {
            var remaining = Stopwatch.StartNew();
            while (remaining.ElapsedMilliseconds < ms) {
                Gate();
                try {
                    var item = get();
                    if (item != null && item.Current.IsEnabled) return item;
                } catch (ElementNotAvailableException) {}
                Thread.Sleep(100);
            }
            throw new InvalidOperationException("Không thấy " + description);
        }
        AutomationElement Element(string name, ControlType type = null) {
            return Await(() => Find(Root(), name, type), name);
        }
        void Invoke(AutomationElement item) {
            RequireForeground();
            ((InvokePattern)item.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
        }
        void Select(AutomationElement item) {
            RequireForeground();
            object scroll;
            if (item.TryGetCurrentPattern(ScrollItemPattern.Pattern, out scroll)) ((ScrollItemPattern)scroll).ScrollIntoView();
            ((SelectionItemPattern)item.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
            item.SetFocus();
        }
        void TypeVisible(AutomationElement item, string text) {
            RequireForeground();
            item.SetFocus();
            var value = (ValuePattern)item.GetCurrentPattern(ValuePattern.Pattern);
            value.SetValue("");
            var current = new StringBuilder();
            foreach (char ch in text) {
                RequireForeground();
                value.SetValue(current.Append(ch).ToString());
                Thread.Sleep(random.Next(minDelay, maxDelay + 1));
            }
            if (value.Current.Value != text) throw new InvalidOperationException("Ô tìm kiếm không nhận đủ ký tự");
        }
        static void Key(byte key, bool release) { keybd_event(key, 0, release ? 2u : 0u, UIntPtr.Zero); }
        void TypeSecret(AutomationElement element) {
            RequireForeground();
            element.SetFocus();
            Key(0x11, false); Key(0x41, false); Key(0x41, true); Key(0x11, true);
            foreach (char ch in password) {
                RequireForeground();
                if (!element.Current.HasKeyboardFocus) throw new InvalidOperationException("Ô mật khẩu mất focus");
                var pair = new[] {
                    new Input { Type = 1, Data = new InputData { Key = new KeyData { Scan = ch, Flags = 4 } } },
                    new Input { Type = 1, Data = new InputData { Key = new KeyData { Scan = ch, Flags = 6 } } },
                };
                if (SendInput(2, pair, Marshal.SizeOf(typeof(Input))) != 2)
                    throw new InvalidOperationException("Windows không nhận ký tự mật khẩu");
                Thread.Sleep(25);
            }
        }
        bool SwitchAccount() {
            var account = Find(Root(), "Mở menu tài khoản", ControlType.Button);
            if (account == null || Find(account, username, ControlType.Text) != null) return false;
            Invoke(account);

            Invoke(Element("Đăng xuất", ControlType.Button));
            Print("SWITCH WPF");
            return true;
        }

        string Chapter() {
            switch (scene) {
                case "catalog": return "01";
                case "warehouse": return "02";
                case "promotion": return "03";
                case "profiles": return "05";
                case "payment": return "06";
                case "appointment": return "07";
                case "clinical": return "08";
                case "nurse": return "09";
                case "reports": return "10";
                default: return "";
            }
        }
        string[] SceneSteps() {
            switch (scene) {
                case "profiles": if (story) return new[] {"customer", "registration-preview", "registration"}; return registrationId > 1 ? new[] {"customer", "recipient", "registration", "newregistration"} : new[] {"customer", "recipient", "registration"};
                case "payment": return new[] {"payment"};
                case "appointment": return new[] {"appointment", "reminders"};
                case "clinical": return new[] {"clinical"};
                case "nurse": return new[] {"vaccination", "aftercare"};
                case "warehouse": return new[] {"warehouse"};
                case "catalog": return new[] {"catalog", "catalog-preview"};
                case "promotion": return new[] {"promotion"};
                case "reports": return new[] {"revenue", "doses", "statuses"};
                case "accounts": return new[] {"accounts"};
                default: throw new InvalidOperationException("Cảnh WPF không hợp lệ");
            }
        }
        void Navigate(string group, string screen, string marker) {
            var section = Find(Root(), group, ControlType.Button);
            Invoke(section ?? Element("Mở danh sách chức năng", ControlType.Button));
            var buttons = Root().FindAll(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.NameProperty, screen),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button)));
            if (buttons.Count > (group == screen ? 1 : 0)) Invoke(buttons[buttons.Count - 1]);
            Visible(marker);
        }
        AutomationElement Visible(string label, AutomationElement scope = null) {
            return Await(() => {
                var root = scope ?? Root();
                if (root == null) return null;
                foreach (AutomationElement item in root.FindAll(TreeScope.Descendants, Condition.TrueCondition)) {
                    if (item.Current.IsOffscreen) continue;
                    if (item.Current.Name.IndexOf(label, StringComparison.OrdinalIgnoreCase) >= 0) return item;
                }
                return null;
            }, label, 7000);
        }
        void Row(string list, string key) {
            var root = list == null ? Root() : Await(() => Find(Root(), list, ControlType.DataGrid) ?? Find(Root(), list, ControlType.List), list);
            var item = Visible(key, root);
            while (item != null && !item.Equals(root)) {
                object pattern;
                if (item.TryGetCurrentPattern(SelectionItemPattern.Pattern, out pattern)) {
                    RequireForeground();
                    ((SelectionItemPattern)pattern).Select();
                    return;
                }
                item = TreeWalker.ControlViewWalker.GetParent(item);
            }
            throw new InvalidOperationException("Không chọn được bản ghi " + key);
        }
        void ReadCode(string field, string id, string button, string marker) {
            TypeVisible(Element(field, ControlType.Edit), id);
            Invoke(Element(button, ControlType.Button));
            Visible(marker);
        }
        void Report(string tab, string marker) {
            Navigate("Quản trị", "Báo cáo", "Xem báo cáo");
            Invoke(Element(tab, ControlType.Button));
            Invoke(Element("Xem báo cáo", ControlType.Button));
            Visible(marker);
        }
        void CloseInventoryDialog() {
            RequireForeground();
            var handle = GetForegroundWindow();
            var buffer = new StringBuilder(260); GetWindowText(handle, buffer, buffer.Capacity);
            string title = buffer.ToString();
            if (title.StartsWith("Lịch sử tiếp nhận", StringComparison.OrdinalIgnoreCase) ||
                title.StartsWith("Nhập lô vắc xin", StringComparison.OrdinalIgnoreCase)) {
                if (!PostMessage(handle, 0x0010, IntPtr.Zero, IntPtr.Zero)) throw new InvalidOperationException("Không đóng được cửa sổ kho");
                var end = DateTime.UtcNow.AddSeconds(6);
                while (IsWindow(handle) && DateTime.UtcNow < end) { Gate(); Thread.Sleep(80); }
                if (IsWindow(handle)) throw new InvalidOperationException("Hộp thoại kho vẫn đang mở");
                Activate();
            }
        }
        void OpenInventoryDialog(string button, string marker) {
            CloseInventoryDialog();
            Invoke(Element(button, ControlType.Button));
            Await(() => {
                if (!Foreground()) return null;
                var window = AutomationElement.FromHandle(GetForegroundWindow());
                return window.Current.Name.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0 ? window : null;
            }, marker);
        }
        void ShowInventoryDialog(string button, string title, string record) {
            OpenInventoryDialog(button, title);
            if (record != null) Visible(record, AutomationElement.FromHandle(GetForegroundWindow()));
            for (int i = 0; i < 40; i++) { Gate(); Thread.Sleep(100); }
            CloseInventoryDialog();
        }
        void RunScene(string step) {
            string lot = lotNumber;
            Print("WPF step " + step);
            switch (step) {
                case "customer": Profiles(false); break;
                case "recipient": Profiles(true); break;
                case "registration-preview":
                    Navigate("Tiếp nhận", "Đăng ký dịch vụ", "Tạo đăng ký / Tạo tiếp");
                    Invoke(Element("Tạo đăng ký / Tạo tiếp", ControlType.Button));
                    Visible("Người được tiêm thuộc khách hàng"); break;
                case "registration":
                    Navigate("Tiếp nhận", "Đăng ký dịch vụ", "Danh sách / Tra cứu");
                    Invoke(Element("Danh sách / Tra cứu", ControlType.Button));
                    Row("Danh sách đăng ký", "DK-0001"); Invoke(Element("Mở chi tiết đã chọn", ControlType.Button)); Visible("Đăng ký #1"); break;
                case "payment":
                    Navigate("Tiếp nhận", "Thanh toán", "Danh sách đăng ký thanh toán");
                    Row("Danh sách đăng ký thanh toán", "DK-0001"); Visible("Biên nhận thanh toán"); break;
                case "appointment":
                    Navigate("Quản trị", "Lịch hẹn & lịch sử", "Tải lại lịch");
                    Select(Element("Lịch hẹn", ControlType.TabItem));
                    Invoke(Element("Tải lại lịch", ControlType.Button));
                    Row("Danh sách lịch hẹn", "LH-0001"); Visible("Chi tiết lịch hẹn"); break;
                case "reminders": Select(Element("Nhắc lịch", ControlType.TabItem)); Visible("Nội dung nhắc lịch"); break;
                case "clinical":
                    Navigate("Chuyên môn", "Kết luận & chỉ định", "Lịch hẹn chuyên môn");
                    Row("Lịch hẹn chuyên môn", "LH-0001"); Visible("Kết luận đã lưu"); break;
                case "vaccination":
                    Navigate("Chuyên môn", "Ghi nhận mũi tiêm", "Mã chỉ định đọc kết quả");
                    ReadCode("Mã chỉ định đọc kết quả", "CD-0001", "Tải lại kết quả", "Prevenar 13"); break;
                case "aftercare":
                    Navigate("Chuyên môn", "Theo dõi sau tiêm", "Mã mũi đọc theo dõi");
                    ReadCode("Mã mũi đọc theo dõi", "MT-0001", "Đọc kết quả theo dõi", "Prevenar 13"); break;
                case "warehouse":
                    RunScene("receiving");
                    RunScene("stock");
                    for (int i = 0; i < 30; i++) { Gate(); Thread.Sleep(100); }
                    RunScene("receipt");
                    break;
                case "stock":
                    CloseInventoryDialog();
                    Navigate("Danh mục", "Kho vắc xin", "Lô vắc xin SQL");
                    Row("Lô vắc xin SQL", lot); Invoke(Element("Xem / Thử lại biến động", ControlType.Button));
                    Visible("XuatDoTiem"); break;
                case "receipt": ShowInventoryDialog("Lịch sử tiếp nhận", "Lịch sử tiếp nhận", lot); break;
                case "receiving": Navigate("Danh mục", "Kho vắc xin", "Lô vắc xin SQL"); ShowInventoryDialog("Nhập lô vắc xin", "Nhập lô", null); break;
                case "catalog":
                    Navigate("Danh mục", "Danh mục", "Tìm theo mã hoặc tên");
                    Select(Element("Vắc xin", ControlType.ListItem));
                    Await(() => { var search = Find(Root(), "Tìm theo mã hoặc tên", ControlType.Edit); return search != null && search.Current.HelpText.Contains("bệnh phòng") ? search : null; }, "tab Vắc xin");
                    TypeVisible(Element("Tìm theo mã hoặc tên", ControlType.Edit), "Prevenar 13");
                    RequireForeground(); Key(0x0D, false); Key(0x0D, true);
                    Row("Danh sách danh mục", "Prevenar 13"); Visible("Đã lưu · VX-PREVENAR-13"); break;
                case "catalog-preview":
                    Invoke(Element("Thêm mới", ControlType.Button));
                    Visible("Thêm vắc xin"); Element("Mã vắc xin", ControlType.Edit).SetFocus(); break;
                case "promotion": Navigate("Danh mục", "Ưu đãi", "Ưu đãi chăm sóc sức khỏe gia đình"); Row(null, "Ưu đãi chăm sóc sức khỏe gia đình"); Visible("Thông tin chương trình"); Visible("Mức ưu đãi"); break;
                case "revenue": Report("Doanh thu theo tháng", "1.190.000"); break;
                case "doses": Report("Số mũi theo vắc xin", "Prevenar 13"); break;
                case "statuses": Report("Đăng ký theo trạng thái", "Đã đăng ký"); break;
                case "accounts": Navigate("Quản trị", "Nhân viên & tài khoản", receptionUsername); break;
            }
            Print("PASS WPF " + (story ? Chapter() + "/10 " : "") + "READ/PREVIEW — " + step);
        }

        void Login() {
            RequireForeground();
            var userBox = Find(Root(), "Tên đăng nhập", ControlType.Edit);
            Print("UI WPF — " + wpf.MainWindowTitle + " / login form " + (userBox != null));
            if (userBox != null) {
                TypeVisible(userBox, username);
                TypeSecret(Element("Mật khẩu", ControlType.Edit));
                Invoke(Element("Đăng nhập", ControlType.Button));
            }
            Await(() => Find(Find(Root(), "Mở menu tài khoản", ControlType.Button), username, ControlType.Text),
                "phiên có đúng vai trò yêu cầu");
        }
        void Profiles(bool recipient) {
            if (Find(Root(), "Tìm hồ sơ", ControlType.Edit) == null) {
                var nav = Find(Root(), "Tiếp nhận", ControlType.Button);
                Invoke(nav ?? Element("Mở danh sách chức năng", ControlType.Button));
                Invoke(Element("Hồ sơ khách hàng", ControlType.Button));
            }
            string tab = recipient ? "Người được tiêm" : "Khách hàng";
            string person = recipient ? child : customer;
            Select(Element(tab, ControlType.TabItem));
            TypeVisible(Element("Tìm hồ sơ", ControlType.Edit), person);
            var result = Await(() => {
                var list = Find(Root(), tab, ControlType.List);
                if (list == null) return null;
                var matches = list.FindAll(TreeScope.Descendants, new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem),
                    new PropertyCondition(AutomationElement.NameProperty, person)));
                if (matches.Count > 1) throw new InvalidOperationException("Có nhiều hồ sơ trùng tên");
                return matches.Count == 1 ? matches[0] : null;
            }, "hồ sơ " + person);
            Select(result);
            if (recipient) {
                Element("Cập nhật người được tiêm", ControlType.Text);
                Await(() => {
                    var root = Root();
                    if (root == null) return null;
                    var fields = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
                    foreach (AutomationElement field in fields) {
                        object value;
                        if (field.Current.Name != "Tìm hồ sơ" && field.TryGetCurrentPattern(ValuePattern.Pattern, out value) &&
                            ((ValuePattern)value).Current.Value == person) return field;
                    }
                    return null;
                }, "chi tiết người được tiêm");
            } else {
                Await(() => {
                    var field = Find(Root(), "Họ tên khách hàng", ControlType.Edit);
                    return field != null && ((ValuePattern)field.GetCurrentPattern(ValuePattern.Pattern)).Current.Value == person ? field : null;
                }, "chi tiết khách hàng");
            }
        }
        void SendNative(byte key, int ms = 100) {
            RequireForegroundForTest();
            Key(key, false);
            Thread.Sleep(ms);
            Key(key, true);
            Thread.Sleep(80);
        }
        void RequireForegroundForTest() {
            if (!Foreground()) throw new InvalidOperationException("WPF không có focus khi gửi phím Windows");
        }
        void TestWait(Func<bool> result, string message) {
            var until = DateTime.UtcNow.AddSeconds(20);
            while (!result() && !stopping && DateTime.UtcNow < until) Thread.Sleep(50);
            if (!result()) throw new InvalidOperationException(message);
        }
        void Rehearse() {
            TestWait(() => next == 8 && !busy, "chặng #7 chưa xong");
            Thread.Sleep(750);
            SendNative(0x78);
            TestWait(() => paused, "F9 chưa được nhận trong WPF");
            SendNative(0x77, 1100);
            if (next != 8 || busy) throw new InvalidOperationException("F8 đã chạy trong lúc pause");
            SendNative(0x78);
            TestWait(() => !paused, "F9 chưa tiếp tục");
            if (escapeTest) {
                SendNative(0x1b);
                TestWait(() => stopping, "Esc chưa được nhận trong WPF");
                if (next != 8) throw new InvalidOperationException("Esc đã chuyển chặng");
                return;
            }
            SendNative(0x77, 4000);
            TestWait(() => next == 9 && !busy, "giữ F8 không dừng đúng chặng #8");
            SendNative(0x77);
            TestWait(() => next == 10 && !busy, "F8 không mở chi tiết người được tiêm");
            Thread.Sleep(750);
            SendNative(0x77);
            TestWait(() => finished && next == 11, "F8 kết thúc không được nhận");
            Print("PASS phím Windows F8/F9 trong focus WPF");
        }
    }

    public static int Run(int ownerPid, string executable, string username, string password, string customer, string child,
                          int minDelay, int maxDelay, bool rehearse, bool escapeTest, string scene, int registrationId, bool story, string lotNumber, string receptionUsername) {
        var existing = Process.GetProcessesByName("TiemChung.Desktop")
            .Where(process => process.MainWindowHandle != IntPtr.Zero &&
                process.MainModule.FileName.StartsWith(executable, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (existing.Length != 1) { Console.WriteLine("BLOCKED WPF — cần đúng một cửa sổ WPF của repo"); return 1; }
        return new Session(existing[0], ownerPid == 0 ? null : Process.GetProcessById(ownerPid), username, password,
            customer, child, minDelay, maxDelay, rehearse, escapeTest, scene, registrationId, story, lotNumber, receptionUsername).Run();
    }
}
