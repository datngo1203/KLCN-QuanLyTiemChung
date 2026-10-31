using System.Windows;
using System.Windows.Controls;
using TiemChung.Desktop.ViewModels;

namespace TiemChung.Desktop.ViewsV2;

public partial class AppointmentView : UserControl
{
    public AppointmentView()
    {
        InitializeComponent();
        Loaded += async (_, _) => {
            if (DataContext is AppointmentViewModel vm && !vm.IsCreating && vm.Appointments.Count == 0) await vm.LoadAsync();
        };
    }
    private async void NewClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not AppointmentViewModel vm) return;
        await vm.NewAsync();
        if (vm.CanEdit) { SourceCodeInput.BringIntoView(); SourceCodeInput.Focus(); }
    }
    private async void SourceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is AppointmentViewModel vm) await vm.LoadSourceDetailAsync();
    }
    private async void AppointmentChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is AppointmentViewModel vm) await vm.LoadDetailAsync();
    }
}
