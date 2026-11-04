using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TiemChung.Desktop.ViewModels;

namespace TiemChung.Desktop.ViewsV2;

public partial class CatalogTaxonomyPicker : UserControl
{
    public static readonly DependencyProperty CanEditProperty =
        DependencyProperty.Register(nameof(CanEdit), typeof(bool), typeof(CatalogTaxonomyPicker));

    public bool CanEdit { get => (bool)GetValue(CanEditProperty); set => SetValue(CanEditProperty, value); }
    public CatalogTaxonomyPicker() => InitializeComponent();

    public void FocusSearch() => SearchBox.Focus();

    private void MatchChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AddButton is not null) AddButton.IsEnabled = Matches.SelectedItem is CatalogTaxonomyOption;
    }

    private void SearchKeyDown(object sender, KeyEventArgs e)
    {
        if (!CanEdit || e.Key != Key.Enter || DataContext is not CatalogTaxonomySelection selection) return;
        e.Handled = true;
        var match = Matches.SelectedItem as CatalogTaxonomyOption;
        if (match is null && selection.Results is [var only]) match = only;
        if (match is not null) Add(match);
        else if (Matches.HasItems) { Matches.Focus(); Matches.IsDropDownOpen = true; }
    }

    private void MatchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Matches.IsDropDownOpen || Matches.SelectedItem is not CatalogTaxonomyOption item) return;
        Add(item);
        e.Handled = true;
    }

    private void AddItem(object sender, RoutedEventArgs e)
    {
        if (Matches.SelectedItem is CatalogTaxonomyOption item) Add(item);
    }

    private void Add(CatalogTaxonomyOption item)
    {
        if (CanEdit && DataContext is CatalogTaxonomySelection selection && selection.Add(item.Id)) FocusSearch();
    }

    private void RemoveItem(object sender, RoutedEventArgs e)
    {
        if (!CanEdit || DataContext is not CatalogTaxonomySelection selection ||
            sender is not Button { DataContext: CatalogTaxonomyOption item }) return;
        selection.Remove(item.Id);
        FocusSearch();
    }
}
