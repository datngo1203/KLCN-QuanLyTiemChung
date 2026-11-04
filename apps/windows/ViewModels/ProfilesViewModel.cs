using System.Collections.ObjectModel;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TiemChung.Contracts;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class ProfilesViewModel : ViewModelBase
{
    private readonly IProfilesService _service;
    private readonly bool _canWrite;
    private string _search = "", _name = "", _nationalId = "", _phone = "", _email = "", _salutation = "";
    private string _recipientName = "", _recipientNationalId = "", _gender = "", _relation = "", _feedback = "";
    private DateTime? _birthDate, _customerBirthDate;
    private CustomerRecord? _selectedCustomer;
    private RecipientRecord? _selectedRecipient;
    private bool _showErrors, _isSaving;

    public ProfilesViewModel(IProfilesService? service = null, bool canWrite = false)
    {
        _service = service ?? new ProfilesApiService();
        _canWrite = canWrite;
        TimKiemKhachHangCommand = new RelayCommand(async _ => await LoadAsync(), _ => CanInteract);
        TaoHoSoMoiCommand = new RelayCommand(_ =>
        {
            if (!CanDiscard()) return;
            LoadSelection(null, null);
            Feedback = "Nhập thông tin khách hàng mới.";
        }, _ => CanInteract && CanEditCustomer);
        LuuKhachHangCommand = new RelayCommand(async _ => await SaveCustomer(), _ => CanInteract && CanEditCustomer && CustomerValid);
        SaveAndNewCustomerCommand = new RelayCommand(async _ => await SaveCustomer(true),
            _ => CanInteract && CanEditCustomer && SelectedCustomer is null && CustomerValid);
        ThemNguoiDuocTiemCommand = new RelayCommand(async _ => await SaveRecipient(), _ => CanInteract && CanEditRecipient && RecipientValid);
        NewRecipientCommand = new RelayCommand(_ =>
        {
            if (!CanDiscard()) return;
            LoadSelection(SelectedCustomer, null);
            Feedback = "Nhập thông tin người được tiêm mới.";
        }, _ => CanInteract && CanEditRecipient);
        DeleteCustomerCommand = new RelayCommand(async _ => await DeleteAsync(true),
            _ => CanInteract && CanEditCustomer && SelectedCustomer is not null);
        DeleteRecipientCommand = new RelayCommand(async _ => await DeleteAsync(false),
            _ => CanInteract && CanEditRecipient && SelectedRecipient is not null);
        UseExistingProfileCommand = new RelayCommand(UseExistingProfile, _ => CanInteract);
    }

    public Func<string, bool>? Confirm { get; set; }
    public ObservableCollection<CustomerRecord> Customers { get; } = [];
    public ObservableCollection<RecipientRecord> Recipients { get; } = [];
    public ObservableCollection<ProfileMatch> CustomerMatches { get; } = [];
    public ObservableCollection<ProfileMatch> RecipientMatches { get; } = [];
    public sealed record ProfileMatch(string Label, object Profile);

    public RelayCommand NewRecipientCommand { get; }
    public RelayCommand DeleteCustomerCommand { get; }
    public RelayCommand DeleteRecipientCommand { get; }
    public RelayCommand UseExistingProfileCommand { get; }
    public RelayCommand TimKiemKhachHangCommand { get; }
    public RelayCommand TaoHoSoMoiCommand { get; }
    public RelayCommand LuuKhachHangCommand { get; }
    public RelayCommand SaveAndNewCustomerCommand { get; }
    public RelayCommand ThemNguoiDuocTiemCommand { get; }

    public bool CanChooseCustomer => CanEditRecipient && SelectedRecipient is null;
    public bool CanInteract => !_isSaving;
    public bool IsReadOnly => !_canWrite;
    public bool CanEditCustomer => _canWrite;
    public bool CanEditRecipient => _canWrite;
    public string DataSourceLabel => "API / SQL thật";
    public IReadOnlyList<string> Genders { get; } = ["Nam", "Nữ", "Khác"];
    public IReadOnlyList<string> Relations => ProfileRules.Relationships;
    public IReadOnlyList<string> Salutations => ProfileRules.Salutations;
    public string CustomerFormTitle => IsReadOnly ? "Thông tin khách hàng (chỉ xem)" : SelectedCustomer is null ? "Tạo hồ sơ khách hàng" : "Thông tin khách hàng";
    public string RecipientFormTitle => IsReadOnly ? "Thông tin người được tiêm (chỉ xem)" : SelectedRecipient is null ? "Thêm người được tiêm" : "Cập nhật người được tiêm";

    public string Search { get => _search; set { if (Set(ref _search, value)) RefreshSearch(); } }
    public IEnumerable<CustomerRecord> FilteredCustomers => Customers.Where(x => MatchesSearch($"{x.Name} {x.NationalId} {x.Phone}"));
    public IEnumerable<RecipientRecord> FilteredRecipients => Recipients.Where(x => MatchesSearch(
        $"{x.Name} {x.NationalId} {Customers.FirstOrDefault(c => c.Id == x.CustomerId)?.Phone}"));
    private bool MatchesSearch(string text) => text.Contains(Search.Trim(), StringComparison.CurrentCultureIgnoreCase);
    public bool NoCustomers => !FilteredCustomers.Any();
    public bool NoRecipients => !FilteredRecipients.Any();

    public CustomerRecord? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (value != _selectedCustomer && CanInteract && CanDiscard())
                LoadSelection(value, null);
        }
    }

    public RecipientRecord? SelectedRecipient
    {
        get => _selectedRecipient;
        set
        {
            if (value == _selectedRecipient || !CanInteract || !CanDiscard()) return;
            var owner = value is null ? SelectedCustomer : Customers.FirstOrDefault(c => c.Id == value.CustomerId);
            LoadSelection(owner, value);
        }
    }

    public string Name { get => _name; set => Change(ref _name, value); }
    public string NationalId { get => _nationalId; set => Change(ref _nationalId, value); }
    public string Phone { get => _phone; set => Change(ref _phone, value); }
    public string Email { get => _email; set => Change(ref _email, value); }
    public string Salutation { get => _salutation; set => Change(ref _salutation, value ?? ""); }
    public DateTime? CustomerBirthDate { get => _customerBirthDate; set => Change(ref _customerBirthDate, value); }
    public string RecipientName { get => _recipientName; set => Change(ref _recipientName, value); }
    public string RecipientNationalId { get => _recipientNationalId; set => Change(ref _recipientNationalId, value); }
    public DateTime? BirthDate { get => _birthDate; set => Change(ref _birthDate, value); }
    public string Gender { get => _gender; set => Change(ref _gender, value ?? ""); }
    public string Relation { get => _relation; set => Change(ref _relation, value ?? ""); }
    public string Feedback { get => _feedback; private set => Set(ref _feedback, value); }
    public bool ShowErrors { get => _showErrors; private set => Set(ref _showErrors, value); }

    private CustomerRecord CustomerDraft => new(SelectedCustomer?.Id ?? 0, Name, EmptyAsNull(NationalId), Phone, Email,
        EmptyAsNull(Salutation), ToDate(CustomerBirthDate));
    private RecipientRecord RecipientDraft => new(SelectedRecipient?.Id ?? 0, SelectedCustomer?.Id ?? 0, RecipientName,
        EmptyAsNull(RecipientNationalId), ToDate(BirthDate) ?? default, Gender, Relation);
    public bool IsDirty => _canWrite &&
        (CustomerDraft != (SelectedCustomer ?? new(0, "", null, "", "")) ||
         RecipientDraft != (SelectedRecipient ?? new(0, SelectedCustomer?.Id ?? 0, "", null, default, "", "")));
    public bool MissingSalutation => SelectedCustomer is not null && Salutation.Length == 0;
    public bool MissingCustomerBirthDate => SelectedCustomer is not null && CustomerBirthDate is null;
    public string LegacyRelation => SelectedRecipient is not null && !Relations.Contains(Relation) ? Relation : "";
    public bool HasLegacyRelation => LegacyRelation.Length > 0;

    public string CustomerError => ShowErrors ? ValidateCustomer() : "";
    public string RecipientError => ShowErrors ? ValidateRecipient() : "";
    public bool CustomerValid => ValidateCustomer().Length == 0;
    public bool RecipientValid => ValidateRecipient().Length == 0;

    private string ValidateCustomer()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Trim().Length > 150) return "Nhập họ tên tối đa 150 ký tự.";
        if (!Salutations.Contains(Salutation)) return "Chọn danh xưng.";
        if (!ValidBirthDate(CustomerBirthDate)) return "Nhập ngày sinh không muộn hơn hôm nay tại Việt Nam.";
        if (!ValidIdentity(NationalId)) return "CCCD phải có đúng 12 chữ số.";
        if (!Regex.IsMatch(Phone, @"^[0-9]{10,11}$")) return "SĐT phải có 10–11 chữ số.";
        if (!MailAddress.TryCreate(Email.Trim(), out var address) || address.Address != Email.Trim()) return "Email không hợp lệ.";
        return "";
    }

    private string ValidateRecipient()
    {
        if (SelectedCustomer is null) return "Chọn khách hàng bảo trợ.";
        if (string.IsNullOrWhiteSpace(RecipientName) || RecipientName.Trim().Length > 150) return "Nhập họ tên tối đa 150 ký tự.";
        if (ToDate(BirthDate) is not DateOnly dob || !ValidBirthDate(BirthDate)) return "Nhập ngày sinh không muộn hơn hôm nay tại Việt Nam.";
        if (!ValidIdentity(RecipientNationalId)) return "CCCD phải có đúng 12 chữ số.";
        if (!Genders.Contains(Gender)) return "Chọn giới tính.";
        if (!Relations.Contains(Relation)) return "Chọn quan hệ.";
        if (!ProfileRules.BirthDatesAgree(Relation, dob, SelectedCustomer.BirthDate))
            return "Ngày sinh không phù hợp với quan hệ của người được tiêm đối với khách hàng.";
        return "";
    }

    private static bool ValidIdentity(string value) => value.Length == 0 || Regex.IsMatch(value, @"^[0-9]{12}$");
    private static bool ValidBirthDate(DateTime? date) => ToDate(date) is DateOnly dob &&
        dob != DateOnly.MinValue && dob <= ProfileRules.Today(DateTimeOffset.UtcNow);
    private static DateOnly? ToDate(DateTime? date) => date is DateTime value ? DateOnly.FromDateTime(value) : null;
    private static string? EmptyAsNull(string value) => value.Length == 0 ? null : value;
    private static bool SameText(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private bool CanDiscard() => !IsDirty || Confirm?.Invoke("Bỏ thay đổi chưa lưu trong hồ sơ?") == true;

    private void Change<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!_canWrite || !Set(ref field, value, propertyName)) return;
        CustomerMatches.Clear();
        RecipientMatches.Clear();
        ShowErrors = true;
        Refresh();
    }

    private void RefreshSearch()
    {
        OnPropertyChanged(nameof(FilteredCustomers));
        OnPropertyChanged(nameof(FilteredRecipients));
        OnPropertyChanged(nameof(NoCustomers));
        OnPropertyChanged(nameof(NoRecipients));
    }

    private void Refresh()
    {
        OnPropertyChanged(nameof(CustomerError));
        OnPropertyChanged(nameof(RecipientError));
        OnPropertyChanged(nameof(CanChooseCustomer));
        OnPropertyChanged(nameof(CustomerFormTitle));
        OnPropertyChanged(nameof(RecipientFormTitle));
        OnPropertyChanged(nameof(MissingSalutation));
        OnPropertyChanged(nameof(MissingCustomerBirthDate));
        OnPropertyChanged(nameof(LegacyRelation));
        OnPropertyChanged(nameof(HasLegacyRelation));
        OnPropertyChanged(nameof(CanInteract));
        foreach (var command in new[] { LuuKhachHangCommand, SaveAndNewCustomerCommand, ThemNguoiDuocTiemCommand,
            TimKiemKhachHangCommand, TaoHoSoMoiCommand, NewRecipientCommand, DeleteCustomerCommand,
            DeleteRecipientCommand, UseExistingProfileCommand })
            command.NotifyCanExecuteChanged();
    }

    private void LoadSelection(CustomerRecord? customer, RecipientRecord? recipient)
    {
        _selectedCustomer = customer;
        LoadCustomerDraft(customer);
        LoadRecipient(recipient);
        OnPropertyChanged(nameof(SelectedCustomer));
    }

    private void LoadCustomerDraft(CustomerRecord? customer)
    {
        Set(ref _name, customer?.Name ?? "", nameof(Name));
        Set(ref _nationalId, customer?.NationalId ?? "", nameof(NationalId));
        Set(ref _phone, customer?.Phone ?? "", nameof(Phone));
        Set(ref _email, customer?.Email ?? "", nameof(Email));
        Set(ref _salutation, customer?.Salutation ?? "", nameof(Salutation));
        Set(ref _customerBirthDate, customer?.BirthDate?.ToDateTime(TimeOnly.MinValue), nameof(CustomerBirthDate));
        CustomerMatches.Clear();
    }

    private void LoadRecipient(RecipientRecord? recipient)
    {
        _selectedRecipient = recipient;
        Set(ref _recipientName, recipient?.Name ?? "", nameof(RecipientName));
        Set(ref _recipientNationalId, recipient?.NationalId ?? "", nameof(RecipientNationalId));
        Set(ref _gender, recipient?.Gender ?? "", nameof(Gender));
        Set(ref _relation, recipient?.Relation ?? "", nameof(Relation));
        Set(ref _birthDate, recipient?.BirthDate.ToDateTime(TimeOnly.MinValue), nameof(BirthDate));
        RecipientMatches.Clear();
        ShowErrors = false;
        OnPropertyChanged(nameof(SelectedRecipient));
        Refresh();
    }

    public async Task LoadAsync()
    {
        if (!CanInteract || !CanDiscard()) return;
        await RunAsync(async () =>
        {
            var customers = await _service.GetCustomers();
            var recipients = await _service.GetRecipients();
            LoadSelection(null, null);
            Customers.Clear();
            Recipients.Clear();
            foreach (var customer in customers) Customers.Add(customer);
            foreach (var recipient in recipients) Recipients.Add(recipient);
            Feedback = "Đã tải hồ sơ từ máy chủ.";
            RefreshSearch();
        });
    }

    private async Task DeleteAsync(bool customer)
    {
        var id = customer ? SelectedCustomer!.Id : SelectedRecipient!.Id;
        var name = customer ? SelectedCustomer!.Name : SelectedRecipient!.Name;
        if (Confirm?.Invoke($"Xóa hồ sơ “{name}”? Thao tác này không thể hoàn tác.") != true) return;
        await RunAsync(async () =>
        {
            await _service.Delete(customer ? "customers" : "recipients", id);
            if (customer)
            {
                Customers.Remove(SelectedCustomer!);
                LoadSelection(null, null);
            }
            else
            {
                Recipients.Remove(SelectedRecipient!);
                LoadRecipient(null);
            }
            Feedback = "Đã xóa hồ sơ.";
            RefreshSearch();
        });
    }

    private async Task SaveCustomer(bool createNext = false)
    {
        var draft = CustomerDraft with { Name = Name.Trim(), Email = Email.Trim() };
        await RunAsync(async () =>
        {
            if (draft.Id == 0 && !await ConfirmCustomerCreation(draft)) return;
            var saved = await _service.SaveCustomer(draft);
            Store(Customers, saved, c => c.Id == saved.Id);
            if (createNext) LoadSelection(null, null);
            else
            {
                _selectedCustomer = saved;
                LoadCustomerDraft(saved);
                OnPropertyChanged(nameof(SelectedCustomer));
            }
            Feedback = createNext ? "Đã lưu khách hàng. Có thể nhập hồ sơ tiếp theo." : "Đã lưu khách hàng vào máy chủ.";
            RefreshSearch();
        });
    }

    private async Task SaveRecipient()
    {
        var draft = RecipientDraft with { Name = RecipientName.Trim() };
        await RunAsync(async () =>
        {
            if (draft.Id == 0 && !await ConfirmRecipientCreation(draft)) return;
            var saved = await _service.SaveRecipient(draft);
            Store(Recipients, saved, r => r.Id == saved.Id);
            LoadRecipient(saved);
            Feedback = "Đã lưu người được tiêm vào máy chủ.";
            RefreshSearch();
        });
    }

    private async Task<bool> ConfirmCustomerCreation(CustomerRecord draft)
    {
        var matches = (await _service.GetCustomers(draft.Name))
            .Where(c => c.BirthDate == draft.BirthDate && SameText(c.Name, draft.Name))
            .Select(c => new
            {
                Profile = c,
                ContactMatches = SameText(c.Phone, draft.Phone) || SameText(c.Email, draft.Email)
            }).OrderByDescending(c => c.ContactMatches)
            .Select(c => new ProfileMatch($"{c.Profile.Name} · #{c.Profile.Id}" +
                (c.ContactMatches ? " · Khớp thêm SĐT hoặc email" : ""), c.Profile));
        return ConfirmCreation(CustomerMatches, matches);
    }

    private async Task<bool> ConfirmRecipientCreation(RecipientRecord draft)
    {
        var matches = (await _service.GetRecipients(customerId: draft.CustomerId))
            .Where(r => r.CustomerId == draft.CustomerId &&
                ((r.BirthDate == draft.BirthDate && SameText(r.Name, draft.Name)) ||
                    (draft.NationalId is not null && SameText(r.NationalId, draft.NationalId))))
            .Select(r => new ProfileMatch($"{r.Name} · {r.BirthDate:dd/MM/yyyy} · #{r.Id}", r));
        return ConfirmCreation(RecipientMatches, matches);
    }

    private bool ConfirmCreation(ObservableCollection<ProfileMatch> list, IEnumerable<ProfileMatch> matches)
    {
        list.Clear();
        foreach (var match in matches) list.Add(match);
        if (list.Count == 0) return true;
        Feedback = "Có thể hồ sơ này đã tồn tại.";
        return Confirm?.Invoke("Có thể hồ sơ này đã tồn tại. Chọn Có để tiếp tục tạo, hoặc Không để xem và chọn hồ sơ có sẵn.") == true;
    }

    private void UseExistingProfile(object? match)
    {
        if (!CanDiscard()) return;
        Search = "";
        if (match is CustomerRecord customer)
        {
            Store(Customers, customer, c => c.Id == customer.Id);
            LoadSelection(customer, null);
        }
        else if (match is RecipientRecord recipient)
        {
            Store(Recipients, recipient, r => r.Id == recipient.Id);
            LoadSelection(SelectedCustomer, recipient);
        }
        Feedback = "Đã chọn hồ sơ có sẵn.";
        RefreshSearch();
    }

    private static void Store<T>(ObservableCollection<T> items, T item, Func<T, bool> matches) where T : class
    {
        var previous = items.FirstOrDefault(matches);
        if (previous is null) items.Add(item);
        else items[items.IndexOf(previous)] = item;
    }

    private async Task RunAsync(Func<Task> operation)
    {
        _isSaving = true;
        Refresh();
        try { await operation(); }
        catch (Exception e) { Feedback = e.Message; }
        finally { _isSaving = false; Refresh(); }
    }
}
