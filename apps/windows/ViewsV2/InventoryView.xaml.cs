using System.Windows.Controls;
namespace TiemChung.Desktop.ViewsV2;
public partial class InventoryView : UserControl
{
    public InventoryView()
    {
        InitializeComponent();
        Loaded += async (_, _) => {
            if (DataContext is TiemChung.Desktop.ViewModels.InventoryViewModel vm) await vm.LoadAsync();
        };
    }
}
