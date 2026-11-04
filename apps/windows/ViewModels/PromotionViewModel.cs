using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class PromotionViewModel : ViewModelBase
{
    private readonly IPromotionService? _service;
    private readonly bool _useApi;
    private string _name = "", _description = "", _amount = "", _feedback = "";
    private DateTime _start = DateTime.Today, _end = DateTime.Today.AddDays(7);
    private PromotionRecord? _selected;
    private bool _saving;

    public PromotionViewModel(IPromotionService? service = null, bool useApi = false)
    {
        _useApi = useApi && service is null;
        if (!_useApi)
        {
            _service = service ?? new SamplePromotionService();
            foreach (var x in _service.GetPromotions()) Promotions.Add(x);
        }

        Promotions.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoData));
        SaveCommand = new(_ => Save(), _ => CanSave);
        DeleteCommand = new(_ => Delete(), _ => CanDelete);
        NewCommand = new(_ => Clear());

        if (_useApi)
        {
            IdentityApiService.Current.SessionCleared += (_, _) => ResetForSessionChange();
        }
    }

    public ObservableCollection<PromotionRecord> Promotions { get; } = [];
    public RelayCommand SaveCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand NewCommand { get; }

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
                    Amount = value.Discount.ToString(CultureInfo.InvariantCulture);
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

    public string DataSourceLabel => _useApi
        ? "SQL Server · API v1"
        : (_service?.UseSampleData == true ? "Dữ liệu mẫu" : "Chưa có nguồn dữ liệu");

    public bool HasNoData => Promotions.Count == 0;

    public bool IsAssigned => Selected is not null && (_useApi ? Selected.Assigned : _service?.IsAssigned(Selected.Id) == true);

    public bool IsActive(PromotionRecord p) => p.Starts <= DateTime.Today && p.Ends >= DateTime.Today;
    public string Status(PromotionRecord p) => IsActive(p) ? "Đang hoạt động" : "Hết hiệu lực";

    public bool CanSave =>
        !_saving &&
        (!_useApi || IdentityApiService.Current.CanManagePromotions) &&
        !string.IsNullOrWhiteSpace(Name) &&
        (!_useApi || !string.IsNullOrWhiteSpace(Description)) &&
        End > Start &&
        decimal.TryParse(Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var a) &&
        a >= 0 &&
        decimal.Truncate(a) == a;

    public string Validation =>
        End <= Start
            ? "Thời điểm kết thúc phải sau thời điểm bắt đầu."
            : decimal.TryParse(Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var a) && a < 0
                ? "Mức ưu đãi không được âm."
                : _useApi && string.IsNullOrWhiteSpace(Description) && !string.IsNullOrWhiteSpace(Name)
                    ? "Vui lòng nhập nội dung chương trình ưu đãi."
                    : "";

    public bool CanDelete =>
        Selected is not null &&
        !IsAssigned &&
        !_saving &&
        (!_useApi || IdentityApiService.Current.CanManagePromotions);

    public async Task LoadAsync()
    {
        if (!_useApi) return;
        if (!IdentityApiService.Current.IsSignedIn)
        {
            Promotions.Clear();
            Feedback = "Vui lòng đăng nhập để xem chương trình khuyến mãi.";
            Refresh();
            return;
        }
        if (!IdentityApiService.Current.CanManagePromotions)
        {
            Promotions.Clear();
            Feedback = "Tài khoản không có quyền Quản lý khuyến mãi (QuanLyKhuyenMai).";
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
        Feedback = $"Đã tải {Promotions.Count} chương trình từ cơ sở dữ liệu.";
        Refresh();
    }

    private void Save()
    {
        if (!CanSave) return;
        if (_useApi)
        {
            _ = SaveApiAsync();
            return;
        }

        _saving = true;
        var a = decimal.Parse(Amount, CultureInfo.InvariantCulture);
        var row = new PromotionRecord(Selected?.Id ?? Promotions.Count + 1, Name, Description, Start, End, a);
        if (Selected is null) Promotions.Add(row);
        else Promotions[Promotions.IndexOf(Selected)] = row;
        Selected = row;
        Feedback = "Đã lưu thay đổi trên dữ liệu mẫu; chưa ghi chương trình thật.";
        _saving = false;
        Refresh();
    }

    private async Task SaveApiAsync()
    {
        _saving = true;
        Feedback = "Đang lưu chương trình khuyến mãi...";
        Refresh();

        var discount = decimal.Parse(Amount, CultureInfo.InvariantCulture);
        var editingId = Selected?.Id;
        var rev = IdentityApiService.Current.SessionRevision;

        var (ok, saved, error) = await PromotionApiClient.SaveAsync(
            editingId, Name, Description, Start, End, discount, rev);

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
            ? $"Đã tạo chương trình khuyến mãi #{saved.Id}."
            : $"Đã cập nhật chương trình khuyến mãi #{saved.Id}.";
        Refresh();
    }

    private void Delete()
    {
        if (!CanDelete) return;
        if (_useApi)
        {
            _ = DeleteApiAsync();
            return;
        }

        Promotions.Remove(Selected!);
        Selected = null;
        Clear();
        Feedback = "Đã xóa chương trình mẫu chưa gán đăng ký.";
        Refresh();
    }

    private async Task DeleteApiAsync()
    {
        if (Selected is null) return;
        var target = Selected;
        _saving = true;
        Feedback = $"Đang xóa chương trình #{target.Id}...";
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
        Feedback = $"Đã xóa chương trình #{target.Id} khỏi cơ sở dữ liệu.";
        Refresh();
    }

    private void Clear()
    {
        Selected = null;
        Name = Description = Amount = "";
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
        OnPropertyChanged(nameof(Validation));
        OnPropertyChanged(nameof(IsAssigned));
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(HasNoData));
        SaveCommand?.NotifyCanExecuteChanged();
        DeleteCommand?.NotifyCanExecuteChanged();
    }
}