using System.Windows;
using System.Windows.Controls;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;

namespace TiemChung.Desktop.ViewsV2;

public partial class AppointmentView : UserControl
{
    public AppointmentView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => {
            if (DataContext is AppointmentViewModel model) model.Confirm = message =>
                MessageBox.Show(Window.GetWindow(this), message, "Xác nhận thay đổi lịch", MessageBoxButton.YesNo,
                    MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
        };
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
        if (DataContext is AppointmentViewModel { CanEdit: true } vm &&
            e.AddedItems.Count == 1 && e.AddedItems[0] is AppointmentSourceOption source) {
            vm.SelectedSource = source;
            await vm.LoadSourceDetailAsync();
        }
    }
    private async void AppointmentChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is AppointmentViewModel vm) await vm.LoadDetailAsync();
    }
    private void PanelVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is ScrollViewer { IsVisible: true } panel) panel.ScrollToTop();
    }
}
