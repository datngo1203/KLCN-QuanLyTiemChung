using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using TiemChung.Desktop.ViewModels;
using TiemChung.Desktop.ViewsV2;
using Xunit;

namespace TiemChung.Windows.Ui.Tests;

public sealed partial class ComboBoxTemplateTests
{
    [Fact]
    public Task RegistrationDatePickerBindsReadOnlyMinimumAndEditableSelection() =>
        host.Run(_ => {
            var model = new RegistrationViewModel();
            var view = new RegistrationView { DataContext = model };
            using var source = new HwndSource(new HwndSourceParameters("Registration date regression") {
                Width = 850, Height = 650, WindowStyle = 0
            }) { RootVisual = view };
            view.Measure(new Size(850, 650));
            view.Arrange(new Rect(0, 0, 850, 650));
            view.UpdateLayout();
            var picker = Assert.IsType<DatePicker>(view.FindName("PreferredDateInput"));
            Assert.Equal(model.VietnamToday, picker.DisplayDateStart);
            var date = model.VietnamToday.AddDays(1);
            picker.SetCurrentValue(DatePicker.SelectedDateProperty, date);
            Assert.Equal(date, model.PreferredDate);
        });
}
