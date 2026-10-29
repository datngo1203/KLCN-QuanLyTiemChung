using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using TiemChung.Desktop.ViewModels;
namespace TiemChung.Desktop.ViewsV2;
public partial class PaymentView : UserControl
{
    public PaymentView()
    {
        InitializeComponent();
        Loaded += async (_, _) => {
            if (DataContext is PaymentViewModel vm && vm.SelectedRegistration is null) await vm.LoadPageAsync(vm.Page);
        };
    }
    private async void RegistrationChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is PaymentViewModel { SelectedRegistration: not null } vm
            && vm.Detail?.RegistrationId != vm.SelectedRegistration.Id) await vm.LoadDetailAsync();
    }
    private async void ConfirmPayment(object sender, RoutedEventArgs e)
    {
        if (DataContext is not PaymentViewModel vm || vm.BeginConfirmation() is not { } detail) return;
        var confirmed = MessageBox.Show(Window.GetWindow(this),
            $"{detail.Code}\nKhách hàng: {detail.Customer.FullName}\nNgười được tiêm: {detail.Recipient.FullName}\nToàn bộ số tiền phải thu: {detail.TotalDue.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))} đ\n\nChỉ ghi nhận mô phỏng phục vụ hệ thống; không xử lý hoặc xác nhận tiền thật.\n\nXác nhận ghi nhận toàn bộ một lần?",
            "Xác nhận thanh toán mô phỏng", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;
        await vm.ConfirmAsync(confirmed);
        RecordButton.BringIntoView(); RecordButton.Focus();
    }
    private void AcknowledgeRisk(object sender, RoutedEventArgs e)
    {
        if (DataContext is not PaymentViewModel { CanAcknowledgeRisk: true } vm) return;
        var id = vm.SelectedRegistration!.Id;
        if (MessageBox.Show(Window.GetWindow(this),
            $"DK-{id:0000}: GET hiện chưa thấy giao dịch. Yêu cầu trước có thể vẫn đang xử lý.\n\nBạn đã đối chiếu và chấp nhận rủi ro mở lại thao tác ghi? API vẫn bảo vệ giao dịch trùng. Thao tác này chưa gửi POST.",
            "Xác nhận rủi ro sau đối chiếu", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes
            && vm.SelectedRegistration?.Id == id) vm.AcknowledgeRisk();
    }
}
