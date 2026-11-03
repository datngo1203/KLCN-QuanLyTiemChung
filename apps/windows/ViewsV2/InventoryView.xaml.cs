using System.Windows.Controls;
using System.Windows;
using TiemChung.Desktop.ViewModels;
namespace TiemChung.Desktop.ViewsV2;
public partial class InventoryView : UserControl
{
    private async void OpenReceiving(object sender,RoutedEventArgs e)
    {
        if(DataContext is not InventoryViewModel vm || !vm.Receiving.HasAccess)return;
        new InventoryReceivingWindow(vm.Receiving){Owner=Window.GetWindow(this)}.ShowDialog();
        if(vm.Receiving.Result is {} result && vm.CanBrowse){
            vm.SearchText=result.Received.LotNumber;await vm.LoadAsync();
            vm.Selected=vm.Batches.FirstOrDefault(l=>l.Data.LotNumber==result.Received.LotNumber);
            await vm.LoadMovementsAsync();
        }
    }
    public InventoryView()
    {
        InitializeComponent();
        Loaded += async (_, _) => {
            if (DataContext is TiemChung.Desktop.ViewModels.InventoryViewModel vm) await vm.LoadAsync();
        };
    }
}
