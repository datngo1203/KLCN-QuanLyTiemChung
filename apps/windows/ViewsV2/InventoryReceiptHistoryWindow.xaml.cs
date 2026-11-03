using System.Windows;
using TiemChung.Desktop.ViewModels;

namespace TiemChung.Desktop.ViewsV2;

public partial class InventoryReceiptHistoryWindow : Window
{
    public InventoryReceiptHistoryWindow(InventoryReceiptHistoryViewModel model)
    {
        InitializeComponent();
        DataContext = model;
        Loaded += async (_, _) => {
            if (model.CanBrowse && model.Rows.Count == 0) await model.LoadAsync();
        };
    }
}
