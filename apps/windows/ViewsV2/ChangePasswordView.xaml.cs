using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TiemChung.Desktop.ViewModels;

namespace TiemChung.Desktop.ViewsV2;

public partial class ChangePasswordView : UserControl
{
    public ChangePasswordView() => InitializeComponent();

    private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ChangePasswordViewModel vm || sender is not PasswordBox box) return;
        if (box.Name == "CurrentInput") vm.CurrentPassword = box.Password;
        else if (box.Name == "NewInput") vm.NewPassword = box.Password;
        else if (box.Name == "ConfirmInput") vm.Confirmation = box.Password;
    }

    private void CancelClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is ChangePasswordViewModel vm) vm.CancelCommand.Execute(null);
        foreach (var box in Descendants<PasswordBox>(this)) box.Clear();
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
}
