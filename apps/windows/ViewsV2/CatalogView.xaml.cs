using System.Windows;
using System.Windows.Controls;
using TiemChung.Desktop.ViewModels;
namespace TiemChung.Desktop.ViewsV2;
public partial class CatalogView : UserControl
{
    public CatalogView() { InitializeComponent(); Loaded += LoadCatalog; }
    private async void LoadCatalog(object sender, RoutedEventArgs e) {
        if (DataContext is CatalogViewModel vm) await vm.InitializeAsync();
    }
}
