using System.Collections.ObjectModel;
using System.Net.Mail;
using System.Text.RegularExpressions;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class ProfilesViewModel : ViewModelBase
{
    private readonly IProfilesService _service;
    private string _search = "", _name = "", _nationalId = "", _phone = "", _email = "";
    private string _recipientName = "", _recipientNationalId = "", _gender = "", _relation = "", _feedback = "";
    private DateTime? _birthDate;
    private CustomerRecord? _selectedCustomer;
    private bool _showErrors, _isSaving;
    public ProfilesViewModel(IProfilesService? service = null)
    {
        _service = service ?? new SampleProfilesService();
        Customers = new(_service.GetCustomers()); Recipients = new(_service.GetRecipients());
        TimKiemKhachHangCommand = new RelayCommand(_ => RefreshSearch());
        TaoHoSoMoiCommand = new RelayCommand(_ => { SelectedCustomer = null; Name = NationalId = Phone = Email = ""; ShowErrors = false; Feedback = "Nhập thông tin khách hàng mới."; });
        LuuKhachHangCommand = new RelayCommand(_ => SaveCustomer(), _ => !_isSaving && CanEditCustomer && CustomerValid);
        ThemNguoiDuocTiemCommand = new RelayCommand(_ => SaveRecipient(), _ => !_isSaving && CanEditRecipient && RecipientValid);
    }
    public ObservableCollection<CustomerRecord> Customers { get; }
    public ObservableCollection<RecipientRecord> Recipients { get; }
    public IReadOnlyList<string> Genders { get; } = ["Nam", "Nữ", "Khác"];
    public IReadOnlyList<string> Relations { get; } = ["Bản thân", "Con", "Bố/Mẹ", "Vợ/Chồng", "Khác"];
    public RelayCommand TimKiemKhachHangCommand { get; }
    public RelayCommand TaoHoSoMoiCommand { get; }
    public RelayCommand LuuKhachHangCommand { get; }
    public RelayCommand ThemNguoiDuocTiemCommand { get; }
    // TODO: Resolve reviewed permissions from the authenticated shell session.
    public bool CanEditCustomer => true;
    public bool CanEditRecipient => true;
    public string DataSourceLabel => _service.UseSampleData ? "Dữ liệu mẫu" : "Chưa có nguồn dữ liệu";
    public string Search { get => _search; set { if (Set(ref _search, value)) RefreshSearch(); } }
    public IEnumerable<CustomerRecord> FilteredCustomers => Customers.Where(x => $"{x.Name} {x.NationalId} {x.Phone}".Contains(Search.Trim(), StringComparison.CurrentCultureIgnoreCase));
    public IEnumerable<RecipientRecord> FilteredRecipients => Recipients.Where(x => $"{x.Name} {x.NationalId} {Customers.FirstOrDefault(c => c.Id == x.CustomerId)?.Phone}".Contains(Search.Trim(), StringComparison.CurrentCultureIgnoreCase));
    public bool NoCustomers => !FilteredCustomers.Any();
    public bool NoRecipients => !FilteredRecipients.Any();
    public CustomerRecord? SelectedCustomer { get => _selectedCustomer; set { if (Set(ref _selectedCustomer, value)) { if (value is not null) { Name = value.Name; NationalId = value.NationalId; Phone = value.Phone; Email = value.Email; } Refresh(); } } }
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
    public string CustomerError => !ShowErrors ? "" : string.IsNullOrWhiteSpace(Name) ? "Nhập họ tên." : !Regex.IsMatch(NationalId, @"^\d{12}$") ? "CCCD phải có đúng 12 chữ số." : _service.NationalIdExists(NationalId, SelectedCustomer?.Id) ? "CCCD đã tồn tại." : !Regex.IsMatch(Phone, @"^\d{10,11}$") ? "SĐT phải có 10–11 chữ số." : !ValidEmail(Email) ? "Email không hợp lệ." : "";
    public string RecipientError => !ShowErrors ? "" : SelectedCustomer is null ? "Chọn khách hàng bảo trợ." : string.IsNullOrWhiteSpace(RecipientName) ? "Nhập họ tên người được tiêm." : BirthDate is null || BirthDate.Value.Date > DateTime.Today ? "Ngày sinh phải không muộn hơn hôm nay." : RecipientNationalId.Length > 0 && !Regex.IsMatch(RecipientNationalId, @"^\d{12}$") ? "CCCD nếu nhập phải có đúng 12 chữ số." : RecipientNationalId.Length > 0 && _service.NationalIdExists(RecipientNationalId) ? "CCCD đã tồn tại." : !Genders.Contains(Gender) ? "Chọn giới tính." : !Relations.Contains(Relation) ? "Chọn quan hệ." : "";
    public bool IsDependentChild => BirthDate is DateTime date && date.Date <= DateTime.Today && date.Date > DateTime.Today.AddYears(-14) && string.IsNullOrWhiteSpace(RecipientNationalId);
    public bool CustomerValid => !string.IsNullOrWhiteSpace(Name) && Regex.IsMatch(NationalId, @"^\d{12}$") && !_service.NationalIdExists(NationalId, SelectedCustomer?.Id) && Regex.IsMatch(Phone, @"^\d{10,11}$") && ValidEmail(Email);
    public bool RecipientValid => SelectedCustomer is not null && !string.IsNullOrWhiteSpace(RecipientName) && BirthDate is DateTime date && date.Date <= DateTime.Today && (RecipientNationalId.Length == 0 || Regex.IsMatch(RecipientNationalId, @"^\d{12}$") && !_service.NationalIdExists(RecipientNationalId)) && Genders.Contains(Gender) && Relations.Contains(Relation);
    private static bool ValidEmail(string value) { try { return value.Contains('@') && new MailAddress(value).Address == value; } catch { return false; } }
    private void Change<T>(ref T field, T value) { if (Set(ref field, value)) { _showErrors = true; Refresh(); } }
    private void RefreshSearch() { OnPropertyChanged(nameof(FilteredCustomers)); OnPropertyChanged(nameof(FilteredRecipients)); OnPropertyChanged(nameof(NoCustomers)); OnPropertyChanged(nameof(NoRecipients)); }
    private void Refresh() { OnPropertyChanged(nameof(CustomerError)); OnPropertyChanged(nameof(RecipientError)); OnPropertyChanged(nameof(IsDependentChild)); LuuKhachHangCommand.NotifyCanExecuteChanged(); ThemNguoiDuocTiemCommand.NotifyCanExecuteChanged(); }
    private void SaveCustomer()
    {
        ShowErrors = true; if (!CustomerValid) return; _isSaving = true; Refresh();
        try { var saved = _service.SaveCustomer(new(SelectedCustomer?.Id ?? 0, Name.Trim(), NationalId, Phone, Email.Trim())); var old = Customers.FirstOrDefault(x => x.Id == saved.Id); if (old is not null) Customers.Remove(old); Customers.Add(saved); SelectedCustomer = saved; Feedback = "Đã lưu trên dữ liệu mẫu; chưa ghi lên máy chủ."; RefreshSearch(); }
        catch (Exception e) { Feedback = e.Message; }
        finally { _isSaving = false; Refresh(); }
    }
    private void SaveRecipient()
    {
        ShowErrors = true; if (!RecipientValid) return; _isSaving = true; Refresh();
        try { Recipients.Add(_service.SaveRecipient(new(0, SelectedCustomer!.Id, RecipientName.Trim(), string.IsNullOrWhiteSpace(RecipientNationalId) ? null : RecipientNationalId, DateOnly.FromDateTime(BirthDate!.Value), Gender, Relation))); Feedback = "Đã thêm người được tiêm trên dữ liệu mẫu."; RefreshSearch(); }
        catch (Exception e) { Feedback = e.Message; }
        finally { _isSaving = false; Refresh(); }
    }
}
