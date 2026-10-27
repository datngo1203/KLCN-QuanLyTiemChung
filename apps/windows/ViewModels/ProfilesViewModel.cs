using System.Collections.ObjectModel;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class ProfilesViewModel : ViewModelBase
{
    private readonly IProfilesService _service;
    private readonly bool _canWrite;
    private string _search = "", _name = "", _nationalId = "", _phone = "", _email = "";
    private string _recipientName = "", _recipientNationalId = "", _gender = "", _relation = "", _feedback = "";
    private DateTime? _birthDate;
    private CustomerRecord? _selectedCustomer;
    private RecipientRecord? _selectedRecipient;
    private bool _showErrors, _isSaving;
    public ProfilesViewModel(IProfilesService? service = null, bool canWrite = false)
    {
        _service = service ?? new ProfilesApiService();
        _canWrite = canWrite;
        Customers = []; Recipients = [];
        TimKiemKhachHangCommand = new RelayCommand(async _ => await LoadAsync(), _ => !_isSaving);
        TaoHoSoMoiCommand = new RelayCommand(_ => { ResetCustomerForm(); Feedback = "Nhập thông tin khách hàng mới."; }, _ => !_isSaving && CanEditCustomer);
        LuuKhachHangCommand = new RelayCommand(async _ => await SaveCustomer(), _ => !_isSaving && CanEditCustomer && CustomerValid);
        SaveAndNewCustomerCommand = new RelayCommand(async _ => await SaveCustomer(true), _ => !_isSaving && CanEditCustomer && SelectedCustomer is null && CustomerValid);
        ThemNguoiDuocTiemCommand = new RelayCommand(async _ => await SaveRecipient(), _ => !_isSaving && CanEditRecipient && RecipientValid);
        NewRecipientCommand = new RelayCommand(_ => { SelectedRecipient=null; RecipientName=RecipientNationalId=Gender=Relation=""; BirthDate=null; }, _=>!_isSaving && CanEditRecipient);
        DeleteCustomerCommand = new RelayCommand(async _=>await DeleteAsync(true), _=>!_isSaving && CanEditCustomer && SelectedCustomer!=null);
        DeleteRecipientCommand = new RelayCommand(async _=>await DeleteAsync(false), _=>!_isSaving && CanEditRecipient && SelectedRecipient!=null);
    }
    public RelayCommand NewRecipientCommand { get; }
    public RelayCommand DeleteCustomerCommand { get; }
    public RelayCommand DeleteRecipientCommand { get; }
    public bool CanChooseCustomer => CanEditRecipient && SelectedRecipient is null;
    public bool CanInteract => !_isSaving;
    public bool IsReadOnly => !_canWrite;
    public string CustomerFormTitle => IsReadOnly ? "Thông tin khách hàng (chỉ xem)" : SelectedCustomer is null ? "Tạo hồ sơ khách hàng" : "Thông tin khách hàng";
    public string RecipientFormTitle => IsReadOnly ? "Thông tin người được tiêm (chỉ xem)" : SelectedRecipient is null ? "Thêm người được tiêm" : "Cập nhật người được tiêm";
    public RecipientRecord? SelectedRecipient { get=>_selectedRecipient; set { if(Set(ref _selectedRecipient,value) && value!=null) { SelectedCustomer=Customers.FirstOrDefault(c=>c.Id==value.CustomerId); RecipientName=value.Name; RecipientNationalId=value.NationalId??""; BirthDate=value.BirthDate.ToDateTime(TimeOnly.MinValue); Gender=value.Gender; Relation=value.Relation; } Refresh(); } }
    public ObservableCollection<CustomerRecord> Customers { get; }
    public ObservableCollection<RecipientRecord> Recipients { get; }
    public IReadOnlyList<string> Genders { get; } = ["Nam", "Nữ", "Khác"];
    public IReadOnlyList<string> Relations { get; } = ["Bản thân", "Con", "Bố/Mẹ", "Vợ/Chồng", "Khác"];
    public RelayCommand TimKiemKhachHangCommand { get; }
    public RelayCommand TaoHoSoMoiCommand { get; }
    public RelayCommand LuuKhachHangCommand { get; }
    public RelayCommand SaveAndNewCustomerCommand { get; }
    public RelayCommand ThemNguoiDuocTiemCommand { get; }
    public bool CanEditCustomer => _canWrite;
    public bool CanEditRecipient => _canWrite;
    public string DataSourceLabel => "API / SQL thật";
    public string Search { get => _search; set { if (Set(ref _search, value)) RefreshSearch(); } }
    public IEnumerable<CustomerRecord> FilteredCustomers => Customers.Where(x => $"{x.Name} {x.NationalId} {x.Phone}".Contains(Search.Trim(), StringComparison.CurrentCultureIgnoreCase));
    public IEnumerable<RecipientRecord> FilteredRecipients => Recipients.Where(x => $"{x.Name} {x.NationalId} {Customers.FirstOrDefault(c => c.Id == x.CustomerId)?.Phone}".Contains(Search.Trim(), StringComparison.CurrentCultureIgnoreCase));
    public bool NoCustomers => !FilteredCustomers.Any();
    public bool NoRecipients => !FilteredRecipients.Any();
    public CustomerRecord? SelectedCustomer { get => _selectedCustomer; set { if (Set(ref _selectedCustomer, value)) { if (value is not null) { Name = value.Name; NationalId = value.NationalId ?? ""; Phone = value.Phone; Email = value.Email; } Refresh(); } } }
    public string Name { get => _name; set => Change(ref _name, value); }
    public string NationalId { get => _nationalId; set => Change(ref _nationalId, value); }
    public string Phone { get => _phone; set => Change(ref _phone, value); }
    public string Email { get => _email; set => Change(ref _email, value); }
    public string RecipientName { get => _recipientName; set => Change(ref _recipientName, value); }
    public string RecipientNationalId { get => _recipientNationalId; set => Change(ref _recipientNationalId, value); }
    public DateTime? BirthDate { get => _birthDate; set => Change(ref _birthDate, value); }
    public string Gender { get => _gender; set => Change(ref _gender, value); }
    public string Relation { get => _relation; set => Change(ref _relation, value); }
    public string Feedback { get => _feedback; private set => Set(ref _feedback, value); }
    public bool ShowErrors { get => _showErrors; set { if (Set(ref _showErrors, value)) Refresh(); } }
    public string CustomerError => !ShowErrors ? "" : string.IsNullOrWhiteSpace(Name) ? "Nhập họ tên." : NationalId.Length > 0 && !Regex.IsMatch(NationalId, @"^[0-9]{12}$") ? "CCCD phải có đúng 12 chữ số." : !Regex.IsMatch(Phone, @"^[0-9]{10,11}$") ? "SĐT phải có 10–11 chữ số." : !ValidEmail(Email) ? "Email không hợp lệ." : "";
    public string RecipientError => !ShowErrors ? "" : SelectedCustomer is null ? "Chọn khách hàng bảo trợ." : string.IsNullOrWhiteSpace(RecipientName) ? "Nhập họ tên người được tiêm." : BirthDate is null || BirthDate.Value.Date > DateTime.Today ? "Ngày sinh phải không muộn hơn hôm nay." : RecipientNationalId.Length > 0 && !Regex.IsMatch(RecipientNationalId, @"^[0-9]{12}$") ? "CCCD nếu nhập phải có đúng 12 chữ số." : !Genders.Contains(Gender) ? "Chọn giới tính." : !Relations.Contains(Relation) ? "Chọn quan hệ." : "";
    public bool CustomerValid => !string.IsNullOrWhiteSpace(Name) && (NationalId.Length == 0 || Regex.IsMatch(NationalId, @"^[0-9]{12}$")) && Regex.IsMatch(Phone, @"^[0-9]{10,11}$") && ValidEmail(Email);
    public bool RecipientValid => SelectedCustomer is not null && !string.IsNullOrWhiteSpace(RecipientName) && BirthDate is DateTime date && date.Date <= DateTime.Today && (RecipientNationalId.Length == 0 || Regex.IsMatch(RecipientNationalId, @"^[0-9]{12}$")) && Genders.Contains(Gender) && Relations.Contains(Relation);
    private static bool ValidEmail(string value) { try { return value.Contains('@') && new MailAddress(value).Address == value; } catch { return false; } }
    private void Change<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) { if (Set(ref field, value, propertyName)) { _showErrors = true; Refresh(); } }
    private void RefreshSearch() { OnPropertyChanged(nameof(FilteredCustomers)); OnPropertyChanged(nameof(FilteredRecipients)); OnPropertyChanged(nameof(NoCustomers)); OnPropertyChanged(nameof(NoRecipients)); }
    private void Refresh() { OnPropertyChanged(nameof(CustomerError)); OnPropertyChanged(nameof(RecipientError)); OnPropertyChanged(nameof(CanChooseCustomer)); OnPropertyChanged(nameof(CustomerFormTitle)); OnPropertyChanged(nameof(RecipientFormTitle)); LuuKhachHangCommand.NotifyCanExecuteChanged(); SaveAndNewCustomerCommand.NotifyCanExecuteChanged(); ThemNguoiDuocTiemCommand.NotifyCanExecuteChanged(); TimKiemKhachHangCommand.NotifyCanExecuteChanged(); TaoHoSoMoiCommand.NotifyCanExecuteChanged(); NewRecipientCommand.NotifyCanExecuteChanged(); DeleteCustomerCommand?.NotifyCanExecuteChanged(); DeleteRecipientCommand?.NotifyCanExecuteChanged(); OnPropertyChanged(nameof(CanInteract)); }
    private void ResetCustomerForm()
    {
        SelectedRecipient = null;
        SelectedCustomer = null;
        Name = NationalId = Phone = Email = "";
        RecipientName = RecipientNationalId = Gender = Relation = "";
        BirthDate = null;
        ShowErrors = false;
    }
    public async Task LoadAsync() {
        if (_isSaving) return;
        _isSaving=true; Refresh();
        try {
            var customers=await _service.GetCustomers(); var recipients=await _service.GetRecipients();
            SelectedRecipient=null; SelectedCustomer=null;
            Customers.Clear(); Recipients.Clear(); foreach(var c in customers) Customers.Add(c); foreach(var r in recipients) Recipients.Add(r);
            Feedback="Đã tải hồ sơ từ máy chủ."; RefreshSearch();
        } catch(Exception e) { Feedback=e.Message; }
        finally { _isSaving=false; Refresh(); }
    }
    private async Task DeleteAsync(bool customer) {
        _isSaving=true; Refresh();
        try {
            await _service.Delete(customer?"customers":"recipients",customer?SelectedCustomer!.Id:SelectedRecipient!.Id);
            if(customer) { Customers.Remove(SelectedCustomer!); SelectedCustomer=null; Name=NationalId=Phone=Email=""; }
            else { Recipients.Remove(SelectedRecipient!); SelectedRecipient=null; RecipientName=RecipientNationalId=""; BirthDate=null; }
            Feedback="Đã xóa hồ sơ."; RefreshSearch();
        } catch(Exception e) { Feedback=e.Message; }
        finally { _isSaving=false; Refresh(); }
    }
    private async Task SaveCustomer(bool createNext = false)
    {
        ShowErrors = true; if (!CustomerValid) return; _isSaving = true; Refresh();
        try { var saved = await _service.SaveCustomer(new(SelectedCustomer?.Id ?? 0, Name.Trim(), NationalId, Phone, Email.Trim())); var old = Customers.FirstOrDefault(x => x.Id == saved.Id); if (old is not null) Customers.Remove(old); Customers.Add(saved); SelectedCustomer = saved; if (createNext) ResetCustomerForm(); Feedback = createNext ? "Đã lưu khách hàng. Có thể nhập hồ sơ tiếp theo." : "Đã lưu khách hàng vào máy chủ."; RefreshSearch(); }
        catch (Exception e) { Feedback = e.Message; }
        finally { _isSaving = false; Refresh(); }
    }
    private async Task SaveRecipient()
    {
        ShowErrors = true; if (!RecipientValid) return; _isSaving = true; Refresh();
        try { var saved = await _service.SaveRecipient(new(SelectedRecipient?.Id ?? 0, SelectedCustomer!.Id, RecipientName.Trim(), string.IsNullOrWhiteSpace(RecipientNationalId) ? null : RecipientNationalId, DateOnly.FromDateTime(BirthDate!.Value), Gender, Relation)); var old=Recipients.FirstOrDefault(x=>x.Id==saved.Id); if(old!=null) Recipients.Remove(old); Recipients.Add(saved); SelectedRecipient=saved; Feedback = "Đã lưu người được tiêm vào máy chủ."; RefreshSearch(); }
        catch (Exception e) { Feedback = e.Message; }
        finally { _isSaving = false; Refresh(); }
    }
}
