using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TiemChung.Desktop.ViewModels;
using TiemChung.Desktop.ViewsV2;
using Xunit;

namespace TiemChung.Windows.Ui.Tests;

public sealed partial class ComboBoxTemplateTests
{
    [Fact]
    public Task BothEditorsFormatLiveAndKeepTheBoundDraftUnformatted() =>
        host.Run(_ => {
            var vm = new CatalogViewModel(permissions: ["QuanLyDanhMucChuyenMon", "QuanLyDanhMucGia"]);
            foreach (var editor in new UserControl[] { new CatalogVaccineEditor(), new CatalogPackageEditor() }) {
                editor.DataContext = vm;
                editor.Measure(new Size(480, double.PositiveInfinity));
                editor.Arrange(new Rect(new Point(), editor.DesiredSize));
                editor.UpdateLayout();
                var price = FindMoneyInput(editor);
                Assert.NotNull(price);
                vm.Price = "300000000";
                Assert.Equal("300 000 000", price.Text);

                price.Text = "1250000";
                Assert.Equal("1 250 000", price.Text);
                Assert.Equal("1250000", vm.Price);

                price.Text = "300 000 000";
                Assert.Equal("300000000", vm.Price);
                foreach (var invalid in new[] { "1,000", "1.50", "-12", "1x" }) {
                    price.Text = invalid;
                    Assert.Equal(invalid, price.Text);
                    Assert.Equal(invalid, vm.Price);
                    Assert.Empty(vm.PriceDisplay);
                }

                price.Text = "";
                Assert.Empty(vm.Price);
            }
        });

    private static VndMoneyInput? FindMoneyInput(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) {
            var child = VisualTreeHelper.GetChild(root, i);
            if ((child as VndMoneyInput ?? FindMoneyInput(child)) is { } price) return price;
        }
        return null;
    }
}
