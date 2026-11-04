using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using TiemChung.Desktop.ViewsV2;
using Xunit;

namespace TiemChung.Windows.Ui.Tests;

public sealed partial class ComboBoxTemplateTests
{
    [Fact]
    public Task ProfileControlsShowLegacyValuesAndRestoreSelectionWhenDiscardIsCanceled() =>
        host.Run(_ =>
        {
            var vm = new ProfilesViewModel(new ProfileControlsFixture(), canWrite: true);
            var view = new ProfilesView { DataContext = vm };
            using var source = new HwndSource(new HwndSourceParameters("RF03 profile controls") {
                Width = 1080, Height = 720, WindowStyle = 0
            }) { RootVisual = view };
            view.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            view.UpdateLayout();
            vm.Confirm = _ => false;
            var list = Assert.Single(VisualChildren<ListBox>(view));
            Assert.Equal(2, list.Items.Count);
            list.SelectedIndex = 0;
            Assert.True(vm.MissingSalutation);
            Assert.True(vm.MissingCustomerBirthDate);
            var salutation = Assert.Single(VisualChildren<ComboBox>(view), c => AutomationProperties.GetName(c) == "Danh xưng khách hàng");
            Assert.Equal(new[] { "Ông", "Bà", "Anh", "Chị", "Khác" }, salutation.Items.Cast<string>().ToArray());
            vm.Name = "Bản nháp";
            list.SelectedIndex = 1;
            Assert.Equal(0, list.SelectedIndex);
            Assert.Equal("Bản nháp", vm.Name);
            Assert.True(vm.IsDirty);
            vm.Confirm = _ => true;
            list.SelectedIndex = 1;
            Assert.Equal("Hồ sơ mới", vm.Name);
            Assert.Equal("Chị", salutation.SelectedItem);
            Assert.False(vm.IsDirty);
            var tabs = Assert.Single(VisualChildren<TabControl>(view));
            tabs.SelectedIndex = 1;
            view.UpdateLayout();
            var relation = Assert.Single(VisualChildren<ComboBox>(view), c => AutomationProperties.GetName(c) == "Quan hệ với khách hàng");
            Assert.Equal(new[] { "Bản thân", "Con", "Anh", "Chị", "Em", "Cha", "Mẹ", "Vợ", "Chồng", "Ông", "Bà", "Cùng hộ khẩu" },
                relation.Items.Cast<string>().ToArray());
        });

    private sealed class ProfileControlsFixture : IProfilesService
    {
        public Task<IReadOnlyList<CustomerRecord>> GetCustomers(string search = "") => Task.FromResult<IReadOnlyList<CustomerRecord>>(
            [new(1, "Hồ sơ cũ", null, "0900000000", "legacy@example.invalid"),
             new(2, "Hồ sơ mới", null, "0900000001", "current@example.invalid", "Chị", new(1990, 1, 1))]);
        public Task<IReadOnlyList<RecipientRecord>> GetRecipients(string search = "", int? customerId = null) =>
            Task.FromResult<IReadOnlyList<RecipientRecord>>([]);
        public Task<CustomerRecord> SaveCustomer(CustomerRecord customer) => throw new NotSupportedException();
        public Task<RecipientRecord> SaveRecipient(RecipientRecord recipient) => throw new NotSupportedException();
        public Task Delete(string resource, int id) => throw new NotSupportedException();
    }
}
