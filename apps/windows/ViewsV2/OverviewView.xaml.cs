using System.Windows;
using System.Windows.Controls;

namespace TiemChung.Desktop.ViewsV2;

public partial class OverviewView : UserControl
{
    public static readonly DependencyProperty IsCompactProperty = DependencyProperty.Register(
        nameof(IsCompact), typeof(bool), typeof(OverviewView), new PropertyMetadata(false));
    public bool IsCompact { get => (bool)GetValue(IsCompactProperty); private set => SetValue(IsCompactProperty, value); }
    public OverviewView() => InitializeComponent();
    private void View_SizeChanged(object sender, SizeChangedEventArgs e) => IsCompact = e.NewSize.Width < 1080;
}
