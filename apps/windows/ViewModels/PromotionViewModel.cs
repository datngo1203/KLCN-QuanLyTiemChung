using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class PromotionViewModel : ViewModelBase
{
    private string _name = "", _description = "", _amount = "", _feedback = "";
    private DateTime _start = DateTime.Today, _end = DateTime.Today.AddDays(7);
    private PromotionRecord? _selected;
    private bool _saving;
    private bool _enabled = true;
    public bool Enabled { get => _enabled; set => Set(ref _enabled, value); }
    public bool CanEdit => !_saving && IdentityApiService.Current.Permissions.Contains("QuanLyKhuyenMai");

    public PromotionViewModel()
    {
        Promotions.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoData));
        SaveCommand = new(async _ => await SaveApiAsync(), _ => CanSave);
        DeleteCommand = new(async _ => await DeleteApiAsync(), _ => CanDelete);
        NewCommand = new(_ => Clear(), _ => CanEdit);
        ReloadCommand = new(async _ => await LoadAsync(), _ => !_saving);

        IdentityApiService.Current.SessionCleared += (_, _) => ResetForSessionChange();
    }

    public ObservableCollection<PromotionRecord> Promotions { get; } = [];
    public RelayCommand SaveCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand NewCommand { get; }
    public RelayCommand ReloadCommand { get; }

    public PromotionRecord? Selected
    {
        get => _selected;
        set
        {
            if (Set(ref _selected, value))
            {
                if (value is not null)
                {
                    Name = value.Name;
                    Description = value.Description;
                    Start = value.Starts;
                    End = value.Ends;
                    Amount = value.Discount.ToString("0", CultureInfo.InvariantCulture);
                    Enabled = value.Enabled;
                }
                Refresh();
            }
        }
    }

    public string Name { get => _name; set { if (Set(ref _name, value)) Refresh(); } }
    public string Description { get => _description; set { if (Set(ref _description, value)) Refresh(); } }
    public DateTime Start { get => _start; set { if (Set(ref _start, value)) Refresh(); } }
    public DateTime End { get => _end; set { if (Set(ref _end, value)) Refresh(); } }
    public string Amount { get => _amount; set { if (Set(ref _amount, value)) Refresh(); } }
    public string Feedback { get => _feedback; private set => Set(ref _feedback, value); }

    public bool HasNoData => Promotions.Count == 0 && !_saving;
    public bool IsAssigned => Selected?.Assigned == true;

    public bool CanSave =>
        CanEdit &&
        Name.Trim().Length is >= 1 and <= 150 &&
        Description.Trim().Length is >= 1 and <= 500 &&
        End >= Start && End < DateTime.MaxValue.Date &&
        decimal.TryParse(Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var a) &&
        a is >= 0 and <= TiemChung.Contracts.CounterPaymentRules.MaxMoney &&
        decimal.Truncate(a) == a;

    public string Validation =>
        End < Start || End >= DateTime.MaxValue.Date
            ? "Ngày kết thúc phải bằng hoặc sau ngày bắt đầu và nằm trong phạm vi hợp lệ."
            : decimal.TryParse(Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var a) && a < 0
                ? "Mức ưu đãi không được âm."
                : string.IsNullOrWhiteSpace(Description) && !string.IsNullOrWhiteSpace(Name)
                    ? "Vui lòng nhập nội dung chương trình ưu đãi."
                    : "";

    public bool CanDelete =>
        Selected is not null &&
        !IsAssigned &&
        !_saving &&
        IdentityApiService.Current.Permissions.Contains("QuanLyKhuyenMai");

    public async Task LoadAsync()
    {
        if (_saving) return;
        if (!IdentityApiService.Current.IsSignedIn)
        {
            Promotions.Clear();
            Feedback = "Vui lòng đăng nhập để xem chương trình khuyến mãi.";
            Refresh();
            return;
        }
        if (!IdentityApiService.Current.Permissions.Contains("QuanLyKhuyenMai"))
        {
            Promotions.Clear();
            Feedback = "Tài khoản không có quyền Quản lý khuyến mãi.";
            Refresh();
            return;
        }

        _saving = true;
        Feedback = "Đang tải danh sách khuyến mãi từ máy chủ...";
        Refresh();

        var rev = IdentityApiService.Current.SessionRevision;
        var (ok, items, error) = await PromotionApiClient.LoadAsync(rev);
        if (rev != IdentityApiService.Current.SessionRevision) return;

        _saving = false;
        if (!ok)
        {
            Feedback = error;
            Refresh();
            return;
        }

        var selectedId = Selected?.Id;
        Promotions.Clear();
        foreach (var item in items) Promotions.Add(item);
        Selected = selectedId.HasValue ? Promotions.FirstOrDefault(p => p.Id == selectedId.Value) : null;
        Feedback = $"Đã tải {Promotions.Count} chương trình.";
        Refresh();
    }

    private async Task SaveApiAsync()
    {
        if (!CanSave) return;
        _saving = true;
        Feedback = "Đang lưu chương trình khuyến mãi...";
        Refresh();

        var discount = decimal.Parse(Amount, CultureInfo.InvariantCulture);
        var editingId = Selected?.Id;
        var rev = IdentityApiService.Current.SessionRevision;

        var (ok, saved, error) = await PromotionApiClient.SaveAsync(
            editingId, Name, Description, Start, End, discount, Enabled, rev);

        if (rev != IdentityApiService.Current.SessionRevision) return;
        _saving = false;

        if (!ok || saved is null)
        {
            Feedback = error;
            Refresh();
            return;
        }

        if (editingId is null)
        {
            Promotions.Insert(0, saved);
        }
        else
        {
            var existing = Promotions.FirstOrDefault(p => p.Id == editingId.Value);
            if (existing is not null)
                Promotions[Promotions.IndexOf(existing)] = saved;
            else
                Promotions.Insert(0, saved);
        }

        Selected = saved;
        Feedback = editingId is null
            ? "Đã tạo chương trình khuyến mãi."
            : "Đã cập nhật chương trình khuyến mãi.";
        Refresh();
    }

    private async Task DeleteApiAsync()
    {
        if (Selected is null || !CanDelete) return;
        var target = Selected;
        _saving = true;
        Feedback = "Đang xóa chương trình…";
        Refresh();

        var rev = IdentityApiService.Current.SessionRevision;
        var (ok, error) = await PromotionApiClient.DeleteAsync(target.Id, rev);
        if (rev != IdentityApiService.Current.SessionRevision) return;

        _saving = false;
        if (!ok)
        {
            Feedback = error;
            Refresh();
            return;
        }

        Promotions.Remove(target);
        Selected = null;
        Clear();
        Feedback = "Đã xóa chương trình.";
        Refresh();
    }

    private void Clear()
    {
        Selected = null;
        Name = Description = Amount = "";
        Enabled = true;
        Start = DateTime.Today;
        End = DateTime.Today.AddDays(7);
        Refresh();
    }

    private void ResetForSessionChange()
    {
        _saving = false;
        Promotions.Clear();
        Clear();
        Feedback = "";
    }

    private void Refresh()
    {
        OnPropertyChanged(nameof(CanEdit));
        NewCommand?.NotifyCanExecuteChanged();
        ReloadCommand?.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(Validation));
        OnPropertyChanged(nameof(IsAssigned));
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(HasNoData));
        SaveCommand?.NotifyCanExecuteChanged();
        DeleteCommand?.NotifyCanExecuteChanged();
    }
}