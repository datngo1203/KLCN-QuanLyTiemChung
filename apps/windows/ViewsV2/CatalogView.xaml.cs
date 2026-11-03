using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TiemChung.Desktop.ViewModels;

namespace TiemChung.Desktop.ViewsV2;

public partial class CatalogView : UserControl
{
    private bool _selectingResource, _selectingRow;
    public CatalogView() { InitializeComponent(); Loaded += LoadCatalog; }
    private async void LoadCatalog(object sender, RoutedEventArgs e)
    {
        if (DataContext is CatalogViewModel vm) {
            vm.Confirm = message => MessageBox.Show(Window.GetWindow(this), message, "Xác nhận danh mục", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
            PriceColumn.Visibility = vm.ShowPrice ? Visibility.Visible : Visibility.Collapsed;
            await vm.InitializeAsync();
        }
    }
    // Commit selection only after the native discard dialog has returned. A deferred callback
    // can run inside MessageBox's nested dispatcher and leave a rejected resource highlighted.
    private async void RestoreResourceSelection(object sender, SelectionChangedEventArgs e)
    {
        if (_selectingResource || DataContext is not CatalogViewModel vm) return;
        _selectingResource = true;
        try {
            var changed = ResourcesList.SelectedItem is string tab && tab != vm.Tab;
            if (changed) await vm.SelectResourceAsync((string)ResourcesList.SelectedItem);

            ResourcesList.SetCurrentValue(ListBox.SelectedItemProperty, vm.Tab);
            PriceColumn.Visibility = vm.ShowPrice ? Visibility.Visible : Visibility.Collapsed;
            if (!vm.IsDirty) DetailScroll?.ScrollToTop();
            if (changed && ResourcesList.ItemContainerGenerator.ContainerFromItem(vm.Tab) is ListBoxItem item) item.Focus();
        }
        finally { _selectingResource = false; }
    }
    private void RestoreRowSelection(object sender, SelectionChangedEventArgs e)
    {
        if (_selectingRow || DataContext is not CatalogViewModel vm || vm.IsBusy) return;
        _selectingRow = true;
        try {
            vm.Selected = CatalogRows.SelectedItem as CatalogRow;
            CatalogRows.SetCurrentValue(DataGrid.SelectedItemProperty, vm.Selected);
            if (!vm.IsDirty) DetailScroll.ScrollToTop();
        }
        finally { _selectingRow = false; }
    }
    private void OpenFocusedRow(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Space) || Keyboard.Modifiers != ModifierKeys.None ||
            DataContext is not CatalogViewModel vm || vm.IsBusy || CatalogRows.CurrentItem is not CatalogRow row) return;
        vm.Selected = row;
        CatalogRows.SetCurrentValue(DataGrid.SelectedItemProperty, vm.Selected);
        e.Handled = true;
    }
}
