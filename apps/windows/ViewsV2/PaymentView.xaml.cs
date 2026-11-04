using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using TiemChung.Contracts;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
namespace TiemChung.Desktop.ViewsV2;
public partial class PaymentView : UserControl
{
    public PaymentView()
    {
        InitializeComponent();
        Loaded += async (_, _) => {
            if (DataContext is not PaymentViewModel vm) return;
            vm.ConfirmDiscard = message => MessageBox.Show(Window.GetWindow(this), message, "Bản nháp thanh toán",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
            if (vm.SelectedRegistration is null) await vm.LoadPageAsync(vm.Page);
        };
    }
    private async void RegistrationChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source != sender || sender is not ListBox list || DataContext is not PaymentViewModel vm ||
            list.SelectedItem is not PaymentRegistration row || vm.SelectedRegistration?.Id == row.Id) return;
        vm.SelectedRegistration = row;
        if (vm.SelectedRegistration != row) {
            list.SetCurrentValue(Selector.SelectedItemProperty, vm.SelectedRegistration);
            return;
        }
        if (vm.Detail?.RegistrationId != row.Id) await vm.LoadDetailAsync();
    }
    private async void ConfirmPayment(object sender, RoutedEventArgs e)
    {
        if (DataContext is not PaymentViewModel vm || vm.BeginConfirmation() is not { } detail) return;
        var confirmed = MessageBox.Show(Window.GetWindow(this),
            $"{detail.Code} · {detail.Recipient.FullName}\n{vm.ReviewText}\n\nTổng phải thu: {PaymentDraft.Money(detail.TotalDue)}\n" +
            "Thanh toán mô phỏng phục vụ khóa luận.\n\n" + (detail.IsPending ? "Xác nhận đã nhận khoản chuyển này?" : "Ghi nhận thanh toán toàn bộ một lần?"),
            detail.IsPending ? "Xác nhận chuyển khoản mô phỏng" : "Xem lại thanh toán mô phỏng",
            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
        await vm.ConfirmAsync(confirmed);
        if (vm.Detail?.HasReceipt == true) { Receipt.BringIntoView(); Receipt.Focus(); }
        else if (sender is Button { IsEnabled: true, IsVisible: true } button) button.Focus();
    }
    private void AcknowledgeRisk(object sender, RoutedEventArgs e)
    {
        if (DataContext is not PaymentViewModel { CanAcknowledgeRisk: true } vm) return;
        var id = vm.SelectedRegistration!.Id;
        if (MessageBox.Show(Window.GetWindow(this),
            $"DK-{id:0000}: GET chưa thấy thanh toán thành công. Yêu cầu trước có thể vẫn đang xử lý.\n\nXác nhận đã đối chiếu để mở lại thao tác?",
            "Xác nhận rủi ro sau đối chiếu", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes
            && vm.SelectedRegistration?.Id == id) vm.AcknowledgeRisk();
    }

    private void FormatCardNumber(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox input || input.Text.Any(c => c != ' ' && !char.IsAsciiDigit(c))) return;
        var digits = CounterPaymentRules.CardDigits(input.Text);
        if (digits.Length > 19) return;
        var formatted = string.Join(" ", digits.Chunk(4).Select(group => new string(group)));
        if (input.Text == formatted) return;
        var digitsBeforeCaret = input.Text[..Math.Min(input.CaretIndex, input.Text.Length)].Count(char.IsAsciiDigit);
        input.SetCurrentValue(TextBox.TextProperty, formatted);
        input.CaretIndex = digitsBeforeCaret + Math.Max(0, digitsBeforeCaret - 1) / 4;
    }

    private void CardNumberKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox input || input.SelectionLength != 0 || Keyboard.Modifiers != ModifierKeys.None) return;
        var caret = input.CaretIndex;
        if (e.Key == Key.Back && caret > 0 && input.Text[caret - 1] == ' ') input.CaretIndex--;
        if (e.Key == Key.Delete && caret < input.Text.Length && input.Text[caret] == ' ') input.CaretIndex++;
    }
}
