using System.Windows.Controls;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;

namespace TiemChung.Desktop.ViewsV2;

public partial class ReminderQueueView : UserControl
{
    public ReminderQueueView()
    {
        InitializeComponent();
        Loaded += async (_, _) => { if (DataContext is ReminderViewModel vm) await vm.LoadQueueAsync(); };
    }
    private async void QueueSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ReminderViewModel { CanBrowse: true } vm &&
            e.AddedItems.Count == 1 && e.AddedItems[0] is AppointmentRecord row) {
            vm.Selected = row;
            await vm.LoadAsync();
        }
    }
}
