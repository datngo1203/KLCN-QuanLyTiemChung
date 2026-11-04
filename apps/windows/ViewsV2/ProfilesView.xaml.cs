using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;

namespace TiemChung.Desktop.ViewsV2;

public partial class ProfilesView : UserControl
{
    public ProfilesView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is not ProfilesViewModel vm) return;
            vm.Confirm = message => MessageBox.Show(Window.GetWindow(this), message, "Hồ sơ",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
            await vm.LoadAsync();
        };
    }

    private void CustomerSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source != sender || sender is not Selector selector || DataContext is not ProfilesViewModel vm ||
            selector.SelectedItem is not CustomerRecord customer) return;
        vm.SelectedCustomer = customer;
        if (vm.SelectedCustomer != customer) selector.SetCurrentValue(Selector.SelectedItemProperty, vm.SelectedCustomer);
    }

    private void RecipientSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source != sender || sender is not Selector selector || DataContext is not ProfilesViewModel vm ||
            selector.SelectedItem is not RecipientRecord recipient) return;
        vm.SelectedRecipient = recipient;
        if (vm.SelectedRecipient != recipient) selector.SetCurrentValue(Selector.SelectedItemProperty, vm.SelectedRecipient);
    }

    private void NewCustomer_Click(object sender, RoutedEventArgs e) =>
        Dispatcher.BeginInvoke(() =>
        {
            if (DataContext is ProfilesViewModel { SelectedCustomer: null, IsDirty: false }) CustomerNameInput.Focus();
        });
}
