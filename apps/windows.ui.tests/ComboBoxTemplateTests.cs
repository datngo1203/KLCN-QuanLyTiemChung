using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;

namespace TiemChung.Windows.Ui.Tests;

public sealed class ComboBoxTemplateTests
{
    [Fact]
    public void FullSurfaceHitsToggleAndChevronRendersAtSupportedDpisWithoutClipping()
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() => {
            var app = new Application();
            try {
                app.Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ThemesDictionary { Theme = Wpf.Ui.Appearance.ApplicationTheme.Light });
                app.Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ControlsDictionary());
                foreach (var file in new[] { "Colors", "Controls" })
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/TiemChung.Desktop;component/Themes/"+file+".xaml") });
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
            }
            catch (Exception e) { failure = ExceptionDispatchInfo.Capture(e); }
            finally { app.Shutdown(); }
        });
        thread.IsBackground = true; thread.SetApartmentState(ApartmentState.STA); thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "WPF template check timed out.");
        failure?.Throw();
    }
    private sealed class Selection { public int Index { get; set; } }
}
