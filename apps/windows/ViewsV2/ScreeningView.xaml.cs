using System.Windows;
using System.Windows.Controls;

namespace TiemChung.Desktop.ViewsV2;

public partial class ScreeningView : UserControl
{
    public ScreeningView() => InitializeComponent();

    private void View_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool compact = e.NewSize.Width < 1100;
        Workspace.ColumnDefinitions[0].Width = compact ? new GridLength(1, GridUnitType.Star) : new GridLength(320);
        Workspace.ColumnDefinitions[1].Width = compact ? new GridLength(0) : new GridLength(20);
        Workspace.ColumnDefinitions[2].Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        DetailPanel.SetValue(Grid.ColumnProperty, compact ? 0 : 2);
        DetailPanel.SetValue(Grid.RowProperty, compact ? 1 : 0);
        DetailPanel.Margin = compact ? new Thickness(0, 16, 0, 0) : new Thickness(0);
        ClinicalColumns.ColumnDefinitions[1].Width = compact ? new GridLength(0) : new GridLength(16);
        ClinicalColumns.ColumnDefinitions[2].Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        IndicationCard.SetValue(Grid.ColumnProperty, compact ? 0 : 2);
        IndicationCard.SetValue(Grid.RowProperty, compact ? 1 : 0);
        IndicationCard.Margin = compact ? new Thickness(0, 16, 0, 0) : new Thickness(0);
    }
}
