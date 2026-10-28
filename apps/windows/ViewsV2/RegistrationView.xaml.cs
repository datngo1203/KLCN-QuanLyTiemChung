using System.Windows.Controls;
using System.ComponentModel;
using TiemChung.Desktop.ViewModels;
namespace TiemChung.Desktop.ViewsV2;
public partial class RegistrationView : UserControl
{
    public RegistrationView()
    {
        InitializeComponent();
        Loaded += async (_, _) => {
            if (DataContext is not RegistrationViewModel vm) return;
            vm.PropertyChanged -= ViewModelChanged;
            vm.PropertyChanged += ViewModelChanged;
            if (!vm.IsCreating) await vm.LoadAsync();
        };
        Unloaded += (_, _) => { if (DataContext is RegistrationViewModel vm) vm.PropertyChanged -= ViewModelChanged; };
    }
    private void ViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RegistrationViewModel.Detail) && sender is RegistrationViewModel { Detail: not null })
            Dispatcher.BeginInvoke(new Action(() => { DetailPanel.BringIntoView(); DetailPanel.Focus(); }));
    }
    private void New_Click(object sender, System.Windows.RoutedEventArgs e) =>
        Dispatcher.BeginInvoke(new Action(() => CustomerSearchInput.Focus()));
}
