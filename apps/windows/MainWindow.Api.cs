using System.Windows;
using TiemChung.Desktop.Services;
namespace TiemChung.Desktop;
public partial class MainWindow
{
    private async void CheckApi_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement button) return;
        button.IsEnabled = false;
        try
        {
            var healthy = await new TiemChungApiClient().IsHealthyAsync();
            ShowFeedback(healthy ? "API đã kết nối." : "API chưa sẵn sàng.");
        }
        catch (Exception) { ShowFeedback("Chưa kết nối API. Kiểm tra backend và địa chỉ cấu hình."); }
        finally { button.IsEnabled = true; }
    }
}
