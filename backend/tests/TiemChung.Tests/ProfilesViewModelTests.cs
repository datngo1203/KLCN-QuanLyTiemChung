using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Tests;

public sealed class ProfilesViewModelTests
{
    [Fact]
    public void NewCustomerClearsBothSelectionsAndDraftsWithoutRemovingSavedRows()
    {
        var vm = new ProfilesViewModel(canWrite: true) { Confirm = _ => true };
        var customer = new CustomerRecord(7, "[DEMO] saved", null, "0900000000", "saved@example.invalid");
        var recipient = new RecipientRecord(8, 7, "[DEMO] child", null, new(2020, 1, 1), "Nam", "Con");
        vm.Customers.Add(customer); vm.Recipients.Add(recipient);
        vm.SelectedRecipient = recipient;
        vm.NationalId = "000000000007"; vm.RecipientNationalId = "000000000008";
        var changed = new HashSet<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        vm.TaoHoSoMoiCommand.Execute(null);
        AssertBlank(vm);
        Assert.False(vm.ShowErrors);
        Assert.Equal(customer, Assert.Single(vm.Customers));
        Assert.Equal(recipient, Assert.Single(vm.Recipients));
        Assert.False(vm.DeleteCustomerCommand.CanExecute(null));
        foreach (var name in new[] { nameof(vm.Name), nameof(vm.NationalId), nameof(vm.Phone), nameof(vm.Email),
            nameof(vm.RecipientName), nameof(vm.RecipientNationalId), nameof(vm.BirthDate), nameof(vm.Gender), nameof(vm.Relation) })
            Assert.Contains(name, changed);
        Assert.DoesNotContain("Change", changed);
    }

    [Fact]
    public async Task SaveAndNewCreatesConsecutivelyAndNeverOverwritesASelectedCustomer()
    {
        var service = new ProfilesService { Pending = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var vm = new ProfilesViewModel(service, true);
        var old = new CustomerRecord(7, "[DEMO] unchanged", null, "0900000000", "saved@example.invalid");
        vm.Customers.Add(old); vm.SelectedCustomer = old;
        Assert.False(vm.SaveAndNewCustomerCommand.CanExecute(null));
        vm.SaveAndNewCustomerCommand.Execute(null);
        Assert.Empty(service.Writes);
        vm.TaoHoSoMoiCommand.Execute(null); Fill(vm, "[DEMO] first");
        vm.SaveAndNewCustomerCommand.Execute(null);
        Assert.False(vm.CanInteract);
        Assert.False(vm.SaveAndNewCustomerCommand.CanExecute(null));
        Assert.False(vm.LuuKhachHangCommand.CanExecute(null));
        Assert.False(vm.TaoHoSoMoiCommand.CanExecute(null));
        vm.SaveAndNewCustomerCommand.Execute(null);
        service.Pending.SetResult();
        await Wait(vm);
        AssertBlank(vm);
        Assert.Contains("Đã lưu khách hàng", vm.Feedback);
        service.Pending = null;
        Fill(vm, "[DEMO] second"); vm.SaveAndNewCustomerCommand.Execute(null); await Wait(vm);
        AssertBlank(vm);
        Assert.Equal(3, vm.Customers.Count);
        Assert.Equal(old, vm.Customers.Single(c => c.Id == 7));
        Assert.Equal(2, service.Writes.Count);
        Assert.All(service.Writes, c => Assert.Equal(0, c.Id));
    }

    [Fact]
    public async Task ReadOnlyCanLoadAndSelectButAllWriteCommandsAreDisabled()
    {
        var service = new ProfilesService();
        var vm = new ProfilesViewModel(service);
        await vm.LoadAsync();
        vm.SelectedCustomer = Assert.Single(vm.Customers);
        Assert.Equal("[DEMO] existing", vm.Name);
        Assert.True(vm.IsReadOnly); Assert.False(vm.CanChooseCustomer);
        foreach (var command in new[] { vm.TaoHoSoMoiCommand, vm.NewRecipientCommand, vm.LuuKhachHangCommand,
            vm.SaveAndNewCustomerCommand, vm.ThemNguoiDuocTiemCommand, vm.DeleteCustomerCommand, vm.DeleteRecipientCommand }) {
            Assert.False(command.CanExecute(null)); command.Execute(null);
        }
        Assert.Empty(service.Writes);
        Assert.True(vm.TimKiemKhachHangCommand.CanExecute(null));
    }

    [Fact]
    public async Task FailedSaveAndNewPreservesDraftForRetry()
    {
        var service = new ProfilesService { Fail = true };
        var vm = new ProfilesViewModel(service, true);
        Fill(vm, "[DEMO] draft");
        vm.SaveAndNewCustomerCommand.Execute(null); await Wait(vm);
        Assert.Equal("[DEMO] draft", vm.Name);
        Assert.Equal("Không lưu được hồ sơ.", vm.Feedback);
        Assert.Null(vm.SelectedCustomer);
        Assert.True(vm.IsDirty);
        Assert.True(vm.SaveAndNewCustomerCommand.CanExecute(null));
    }

    [Fact]
    public async Task RejectingContextSwitchRetainsBothDraftsAndMakesNoReload()
    {
        var service = new ProfilesService();
        var vm = new ProfilesViewModel(service, true);
        await vm.LoadAsync();
        var first = Assert.Single(vm.Customers);
        var second = first with { Id = 8, Name = "Second" };
        var child = new RecipientRecord(10, first.Id, "Child", null, new(2020, 1, 1), "Nam", "Con");
        var other = child with { Id = 11, CustomerId = second.Id };
        vm.Customers.Add(second);
        vm.Recipients.Add(child);
        vm.Recipients.Add(other);
        vm.SelectedRecipient = child;
        Assert.False(vm.IsDirty);
        vm.Name = "Draft customer";
        vm.RecipientName = "Draft recipient";
        var asked = 0;
        vm.Confirm = _ => { asked++; return false; };
        vm.SelectedCustomer = second;
        vm.SelectedRecipient = other;
        vm.NewRecipientCommand.Execute(null);
        vm.TaoHoSoMoiCommand.Execute(null);
        vm.Search = "Second";
        await vm.LoadAsync();
        Assert.Equal(5, asked);
        Assert.Equal(1, service.CustomerReads);
        Assert.Equal(first, vm.SelectedCustomer);
        Assert.Equal(child, vm.SelectedRecipient);
        Assert.Equal("Draft customer", vm.Name);
        Assert.Equal("Draft recipient", vm.RecipientName);
        vm.Confirm = _ => true;
        vm.SelectedRecipient = other;
        Assert.Equal(second, vm.SelectedCustomer);
        Assert.Equal(other, vm.SelectedRecipient);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public async Task DuplicateSuggestionsAllowChoosingExistingOrConfirmingCreate()
    {
        var existing = new CustomerRecord(7, "[DEMO] existing", null, "0900000000", "existing@example.invalid", "Chị", new(1990, 1, 1));
        var service = new ProfilesService { Existing = existing };
        var vm = new ProfilesViewModel(service, true);
        Fill(vm, "  [demo] existing ");
        vm.Confirm = message => { Assert.Contains("Có thể hồ sơ này đã tồn tại", message); return false; };
        vm.LuuKhachHangCommand.Execute(null);
        await Wait(vm);
        Assert.Empty(service.Writes);
        var match = Assert.Single(vm.CustomerMatches);
        Assert.Contains("Khớp thêm SĐT hoặc email", match.Label);
        vm.Confirm = _ => true;
        vm.UseExistingProfileCommand.Execute(match.Profile);
        Assert.Equal(existing, vm.SelectedCustomer);
        Assert.False(vm.IsDirty);
        vm.TaoHoSoMoiCommand.Execute(null);
        Fill(vm, existing.Name);
        vm.LuuKhachHangCommand.Execute(null);
        await Wait(vm);
        Assert.Single(service.Writes);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public async Task DeleteCancelAndReferencedFailurePreserveRowSelectionAndDraft()
    {
        var service = new ProfilesService { FailDelete = true };
        var vm = new ProfilesViewModel(service, true);
        await vm.LoadAsync();
        var row = Assert.Single(vm.Customers);
        vm.SelectedCustomer = row;
        vm.Name = "Draft";
        vm.Confirm = message =>
        {
            Assert.Contains(row.Name, message);
            Assert.Contains("không thể hoàn tác", message);
            return false;
        };
        vm.DeleteCustomerCommand.Execute(null);
        Assert.Equal(0, service.Deletes);
        vm.Confirm = _ => true;
        vm.DeleteCustomerCommand.Execute(null);
        await Wait(vm);
        Assert.Equal("Hồ sơ đang được sử dụng, không thể xóa.", vm.Feedback);
        Assert.Equal(row, Assert.Single(vm.Customers));
        Assert.Equal(row, vm.SelectedCustomer);
        Assert.Equal("Draft", vm.Name);
        Assert.True(vm.IsDirty);
        service.FailDelete = false;
        vm.DeleteCustomerCommand.Execute(null);
        await Wait(vm);
        Assert.Empty(vm.Customers);
        AssertBlank(vm);
        Assert.False(vm.IsDirty);
        Assert.Equal(2, service.Deletes);
    }

    private static void Fill(ProfilesViewModel vm, string name) { vm.Name = name; vm.Phone = "0900000000"; vm.Email = "demo@example.invalid"; vm.Salutation = "Chị"; vm.CustomerBirthDate = new(1990, 1, 1); }
    private static void AssertBlank(ProfilesViewModel vm)
    {
        Assert.Null(vm.SelectedCustomer); Assert.Null(vm.SelectedRecipient);
        Assert.All(new[] { vm.Name, vm.NationalId, vm.Phone, vm.Email, vm.RecipientName, vm.RecipientNationalId, vm.Gender, vm.Relation }, s => Assert.Equal("", s));
        Assert.Null(vm.BirthDate);
    }
    private static async Task Wait(ProfilesViewModel vm)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!vm.CanInteract) await Task.Delay(10, timeout.Token);
    }
    private sealed class ProfilesService : IProfilesService
    {
        public List<CustomerRecord> Writes { get; } = [];
        public TaskCompletionSource? Pending { get; set; }
        public bool Fail { get; set; }
        public bool FailDelete { get; set; }
        public int Deletes { get; private set; }
        public int CustomerReads { get; private set; }
        public CustomerRecord Existing { get; init; } = new(7, "[DEMO] existing", null, "0900000000", "existing@example.invalid");
        public Task<IReadOnlyList<CustomerRecord>> GetCustomers(string search = "")
        {
            CustomerReads++;
            return Task.FromResult<IReadOnlyList<CustomerRecord>>([Existing]);
        }
        public Task<IReadOnlyList<RecipientRecord>> GetRecipients(string search = "", int? customerId = null) => Task.FromResult<IReadOnlyList<RecipientRecord>>([]);
        public async Task<CustomerRecord> SaveCustomer(CustomerRecord customer) {
            if (Pending is not null) await Pending.Task;
            if (Fail) throw new InvalidOperationException("Không lưu được hồ sơ.");
            Writes.Add(customer); return customer with { Id = 10 + Writes.Count };
        }
        public Task<RecipientRecord> SaveRecipient(RecipientRecord recipient) => throw new NotSupportedException();
        public Task Delete(string resource, int id)
        {
            Deletes++;
            if (FailDelete) throw new InvalidOperationException("Hồ sơ đang được sử dụng, không thể xóa.");
            return Task.CompletedTask;
        }
    }
}
