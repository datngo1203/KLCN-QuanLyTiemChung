using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using TiemChung.Desktop.ViewsV2;
using Xunit;

namespace TiemChung.Windows.Ui.Tests;

public sealed partial class ComboBoxTemplateTests(WpfComboBoxHost host) : IClassFixture<WpfComboBoxHost>
{
    [Fact]
    public Task FullSurfaceHitsToggleAndChevronRendersAtSupportedDpisWithoutClipping() =>
        host.Run(app => {
            var combo = new ComboBox { Style = (Style)app.Resources["CE.ComboBox"], Width = 280, Height = 36,
                ItemsSource = new[] { "Active — Đang sử dụng", "Inactive — Ngừng sử dụng" }, SelectedIndex = 0 };
            // A hidden presentation source makes WPF hit testing live without desktop input.
            using var source = new HwndSource(new HwndSourceParameters("ComboBox template regression") {
                Width = 300, Height = 100, WindowStyle = 0
            }) { RootVisual = combo };
            combo.Measure(new Size(280, 36)); combo.Arrange(new Rect(0, 0, 280, 36)); combo.UpdateLayout();
            var toggle = (ToggleButton)combo.Template.FindName("DropDownToggle", combo);
            // Test real WPF hit testing at text, whitespace, edges and arrow, not XAML attributes.
            foreach (var point in new[] { new Point(4,18), new Point(80,18), new Point(220,18), new Point(264,18), new Point(140,1), new Point(140,35) }) {
                var hit = combo.InputHitTest(point) as DependencyObject;
                while (hit is not null && hit is not ToggleButton) hit = VisualTreeHelper.GetParent(hit);
                Assert.Same(toggle, hit);
            }
            Assert.False(toggle.Focusable); Assert.False(toggle.IsTabStop);
            var chevron = (Viewbox)toggle.Template.FindName("Chevron", toggle);
            var center = chevron.TranslatePoint(new Point(6,4), combo);
            var dpiScale = VisualTreeHelper.GetDpi(combo);
            Assert.InRange(Math.Abs(center.X-264),0,0.5/dpiScale.DpiScaleX+0.01);
            Assert.InRange(Math.Abs(center.Y-18),0,0.5/dpiScale.DpiScaleY+0.01);
            foreach (var dpi in new[] { 96d, 120d, 144d }) {
                var width = (int)Math.Ceiling(12*dpi/96); var height = (int)Math.Ceiling(8*dpi/96);
                var drawing = new DrawingVisual();
                var brush = new VisualBrush(chevron.Child) { ViewboxUnits = BrushMappingMode.Absolute, Viewbox = new Rect(0,0,12,8) };
                using (var context = drawing.RenderOpen()) context.DrawRectangle(brush, null, new Rect(0,0,12,8));
                var bitmap = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);
                bitmap.Render(drawing); var pixels = new byte[width*height*4]; bitmap.CopyPixels(pixels, width*4, 0);
                var ink = new List<Point>();
                for (var y=0; y<height; y++) for (var x=0; x<width; x++) if (pixels[(y*width+x)*4+3] > 0) ink.Add(new Point(x,y));
                Assert.NotEmpty(ink);
                Assert.All(ink, p => { Assert.InRange(p.X,1,width-2); Assert.InRange(p.Y,1,height-2); });
                Assert.InRange(ink.Max(p=>p.X)-ink.Min(p=>p.X)+1,Math.Floor(8*dpi/96),Math.Ceiling(10.5*dpi/96));
                Assert.InRange(ink.Max(p=>p.Y)-ink.Min(p=>p.Y)+1,Math.Floor(4*dpi/96),Math.Ceiling(6.5*dpi/96));
                Assert.InRange(Math.Abs((ink.Min(p=>p.X)+ink.Max(p=>p.X))/2-(width-1)/2d),0,1);
                Assert.InRange(Math.Abs((ink.Min(p=>p.Y)+ink.Max(p=>p.Y))/2-(height-1)/2d),0,1);
            }
            var box = (Border)combo.Template.FindName("Box", combo);
            var binding = combo.SetBinding(ComboBox.SelectedIndexProperty, new Binding(nameof(Selection.Index)) { Source = new Selection() });
            Validation.MarkInvalid(binding, new ValidationError(new ExceptionValidationRule(),binding,"Synthetic validation",null));
            Assert.Equal(app.Resources["CE.ErrorBorder"],box.BorderBrush);
            combo.IsEnabled = false;
            Assert.Equal(app.Resources["CE.DisabledBackground"],box.Background);
            Assert.Equal(app.Resources["CE.DisabledBorder"],box.BorderBrush);
            Assert.False(toggle.IsEnabled);
        });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task RegimenSelectorsRenderEarlierDosesAndLocalizedUnits(bool canEdit) => host.Run(_ => {
        var vm = new CatalogViewModel(permissions: canEdit ? ["QuanLyDanhMucChuyenMon", "QuanLyDanhMucGia"] : []);
        var regimen = new CatalogRegimenDraft(new(1, "Phác đồ thử", "Tham khảo", null, null, null, [
            new(1, 1, "Mốc đầu", null, null, null), new(2, 3, "Mốc sau", null, null, null, 1, 6, "Months")
        ]));
        vm.Regimens.Add(regimen);
        var editor = new CatalogVaccineEditor { DataContext = vm };
        ((Expander)editor.FindName("Regimens")).IsExpanded = true;
        using var source = new HwndSource(new HwndSourceParameters("Catalog regimen controls") {
            Width = 480, Height = 900, WindowStyle = 0
        }) { RootVisual = editor };
        editor.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        editor.Measure(new Size(480, double.PositiveInfinity));
        editor.Arrange(new Rect(new Point(), editor.DesiredSize));
        editor.UpdateLayout();
        var dose = regimen.Doses[1];
        var combos = VisualChildren<ComboBox>(editor).Where(combo => ReferenceEquals(combo.DataContext, dose)).ToArray();
        var anchor = Assert.Single(combos, combo => AutomationProperties.GetName(combo) == "Mũi neo tham khảo");
        var unit = Assert.Single(combos, combo => AutomationProperties.GetName(combo) == "Đơn vị khoảng cách");
        Assert.Equal("Mũi 1", Assert.IsType<CatalogDoseAnchor>(anchor.SelectedItem).Label);
        Assert.Equal("Mũi 1", anchor.SelectedItem.ToString());
        Assert.Equal("Tháng", Assert.IsType<CatalogStatus>(unit.SelectedItem).Label);
        Assert.Equal(2, anchor.Items.Count);
        Assert.Equal(5, unit.Items.Count);
        Assert.All(combos, combo => {
            Assert.Equal(canEdit, combo.IsEnabled);
            Assert.False(combo.IsEditable);
            Assert.True(combo.ActualWidth > 0);
        });
        if (canEdit) {
            unit.SelectedValue = "Years";
            Assert.Equal("Years", dose.IntervalUnit);
            anchor.SelectedIndex = 0;
            Assert.Null(dose.AnchorDoseOrder);
        }
    });

    [Theory]
    [InlineData("phòng bệnh", "BENH")]
    [InlineData("công nghệ vắc xin", "CNVX")]
    public Task TaxonomyPickerRendersBusinessLabelsInSelectionAndPopup(string group, string prefix) =>
        host.Run(_ => {
            var selection = new CatalogTaxonomySelection();
            selection.Reset([
                new CatalogType(1, "Mục thử A", "", prefix + "-A"),
                new CatalogType(2, "Mục thử B", "", prefix + "-B")
            ], null);
            var picker = new CatalogTaxonomyPicker { DataContext = selection, CanEdit = true };
            AutomationProperties.SetName(picker, group);
            using var source = new HwndSource(new HwndSourceParameters("Catalog taxonomy label regression") {
                Width = 520, Height = 260, WindowStyle = 0
            }) { RootVisual = picker };
            picker.Measure(new Size(520, 260));
            picker.Arrange(new Rect(0, 0, 520, 260));
            picker.UpdateLayout();
            var combo = Assert.IsType<ComboBox>(picker.FindName("Matches"));
            Assert.Equal("Kết quả tìm " + group, AutomationProperties.GetName(combo));

            foreach (var option in selection.Results) {
                combo.SelectedItem = option;
                combo.UpdateLayout();
                Assert.Equal(option.Label, Assert.Single(VisualChildren<TextBlock>(combo)).Text);
            }

            combo.IsDropDownOpen = true;
            try {
                var popup = Assert.IsType<Popup>(combo.Template.FindName("PART_Popup", combo));
                picker.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                popup.Child.UpdateLayout();
                foreach (var option in selection.Results) {
                    var item = Assert.IsType<ComboBoxItem>(combo.ItemContainerGenerator.ContainerFromItem(option));
                    Assert.Equal(option.Label, Assert.Single(VisualChildren<TextBlock>(item)).Text);
                }
            }
            finally { combo.IsDropDownOpen = false; }
        });

    private static IEnumerable<T> VisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) yield return match;
            foreach (var descendant in VisualChildren<T>(child)) yield return descendant;
        }
    }

    private sealed class Selection { public int Index { get; set; } }
}

public sealed class WpfComboBoxHost : IDisposable
{
    private readonly TaskCompletionSource<Application> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Thread _thread;

    public WpfComboBoxHost()
    {
        _thread = new Thread(() => {
            try {
                var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                app.Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ThemesDictionary { Theme = Wpf.Ui.Appearance.ApplicationTheme.Light });
                app.Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ControlsDictionary());
                foreach (var file in new[] { "Colors", "Typography", "Controls", "Buttons", "Cards", "BadgesAlerts", "Navigation" })
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary {
                        Source = new Uri($"pack://application:,,,/TiemChung.Desktop;component/Themes/{file}.xaml")
                    });
                _ready.SetResult(app);
                Dispatcher.Run();
            }
            catch (Exception e) { _ready.TrySetException(e); }
        }) { IsBackground = true };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    public async Task Run(Action<Application> check)
    {
        var app = await _ready.Task.WaitAsync(TimeSpan.FromSeconds(30));
        await app.Dispatcher.InvokeAsync(() => check(app)).Task.WaitAsync(TimeSpan.FromSeconds(30));
    }

    public void Dispose()
    {
        if (_ready.Task.IsCompletedSuccessfully)
            _ready.Task.Result.Dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
        _thread.Join(TimeSpan.FromSeconds(30));
    }
}
