using System.Collections.ObjectModel;
using System.Globalization;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class CatalogPackageComponent(int id, string label, CatalogVaccine? vaccine, string quantity) : ViewModelBase
{
    private string _quantity = quantity;
    public int Id { get; } = id;
    public string Label { get; } = label;
    public CatalogVaccine? Vaccine { get; } = vaccine;
    public string StatusLabel => Vaccine is null ? "Chưa xác định trạng thái" : CatalogStatus.Describe(Vaccine.Status);
    public string Quantity { get => _quantity; set => Set(ref _quantity, value); }
    public int? ParsedQuantity => int.TryParse(Quantity, NumberStyles.None, CultureInfo.InvariantCulture, out var count)
        && count is >= 1 and <= 100 ? count : null;
}

public sealed partial class CatalogViewModel
{
    private CatalogVaccine[] _packageVaccines = [];
    private string _componentSearch = "";
    private CatalogVaccine? _componentCandidate;
    private readonly ObservableCollection<CatalogPackageComponent> _packageComponents = [];
    public IEnumerable<CatalogPackageComponent> PackageComponents => _packageComponents;
    public RelayCommand AddComponentCommand { get; private set; } = null!;
    public RelayCommand RemoveComponentCommand { get; private set; } = null!;
    public string ComponentSearch
    {
        get => _componentSearch;
        set { if (Set(ref _componentSearch, value)) RefreshCandidates(); }
    }
    public CatalogVaccine? ComponentCandidate
    {
        get => _componentCandidate;
        set
        {
            if (!Set(ref _componentCandidate, value)) return;
            AddComponentCommand.NotifyCanExecuteChanged();
        }
    }
    public CatalogVaccine[] ComponentCandidates { get; private set; } = [];
    public string CandidateHint => ComponentCandidates.Length == 0
        ? "Không có vắc xin phù hợp để thêm."
        : "Chọn kết quả rồi bấm Thêm. Gói đang sử dụng chỉ nhận vắc xin đang sử dụng.";
    public string ComponentSummary => _packageComponents.Count == 0 ? "Chưa có thành phần."
        : $"{_packageComponents.Count} loại vắc xin · " +
          (_packageComponents.Any(x => x.ParsedQuantity is null) ? "Kiểm tra số lượng để tính tổng mũi."
          : $"{_packageComponents.Sum(x => x.ParsedQuantity ?? 0)} mũi/liều");
    public string ComponentDiseases
    {
        get
        {
            var diseases = _packageComponents.SelectMany(x => x.Vaccine?.Diseases ?? []).DistinctBy(x => x.Id);
            if (Selected?.Package is { } package &&
                _packageComponents.Select(x => x.Id).Order().SequenceEqual(package.Items.Select(x => x.VaccineId).Order()))
                diseases = package.Diseases;
            var names = string.Join(", ", diseases.Select(x => x.Name).Order());
            return names.Length == 0 ? "Chưa có thông tin phòng bệnh." : names;
        }
    }

    private void InitializePackageCommands()
    {
        AddComponentCommand = new RelayCommand(_ => {
            if (ComponentCandidate is not { } vaccine || !CanAddComponent()) return;
            AppendComponent(vaccine.Id, vaccine.Code, vaccine.Name, vaccine, "1");
            RefreshComposition();
        }, _ => CanAddComponent());
        RemoveComponentCommand = new RelayCommand(p => {
            if (p is not CatalogPackageComponent item || !CanEdit || !_packageComponents.Remove(item)) return;
            item.PropertyChanged -= OnQuantityChanged;
            RefreshComposition();
        }, p => ShowPackage && CanEdit && p is CatalogPackageComponent item && _packageComponents.Contains(item));
    }

    private bool CanAddComponent() => ShowPackage && CanEdit && _packageComponents.Count < 100 &&
        ComponentCandidate is { } vaccine && ComponentCandidates.Contains(vaccine) &&
        (Status == "Inactive" || vaccine.Status == "Active") && _packageComponents.All(x => x.Id != vaccine.Id);

    private void RefreshCandidates()
    {
        if (!ShowPackage) return;
        var term = WithoutDiacritics(ComponentSearch.Trim());
        bool Matches(string value) => WithoutDiacritics(value).Contains(term, StringComparison.OrdinalIgnoreCase);
        var selectedIds = _packageComponents.Select(x => x.Id).ToHashSet();
        ComponentCandidates = _packageVaccines.Where(x => !selectedIds.Contains(x.Id) &&
            (Matches(x.Code) || Matches(x.Name) || Matches(x.Manufacturer) ||
             (x.Diseases ?? []).Any(d => Matches(d.Name)))).OrderBy(x => x.Name).ToArray();
        ComponentCandidate = null;
        OnPropertyChanged(nameof(ComponentCandidates));
        OnPropertyChanged(nameof(CandidateHint));
    }

    private void ApplyPackage(CatalogPackage? package)
    {
        foreach (var item in _packageComponents) item.PropertyChanged -= OnQuantityChanged;
        _packageComponents.Clear();
        foreach (var item in package?.Items ?? []) {
            var vaccine = _packageVaccines.FirstOrDefault(x => x.Id == item.VaccineId);
            AppendComponent(item.VaccineId, item.VaccineCode ?? vaccine?.Code ?? "",
                item.VaccineName ?? vaccine?.Name ?? $"Vắc xin #{item.VaccineId}", vaccine,
                item.Quantity.ToString(CultureInfo.InvariantCulture));
        }
        Set(ref _componentSearch, "", nameof(ComponentSearch));
        RefreshComposition();
    }

    private void AppendComponent(int id, string code, string name, CatalogVaccine? vaccine, string quantity)
    {
        var item = new CatalogPackageComponent(id, string.IsNullOrEmpty(code) ? name : $"{code} — {name}", vaccine, quantity);
        item.PropertyChanged += OnQuantityChanged;
        _packageComponents.Add(item);
    }

    private void OnQuantityChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        OnPropertyChanged(nameof(ComponentSummary));
        Refresh();
    }

    private void RefreshComposition()
    {
        RefreshCandidates();
        OnPropertyChanged(nameof(ComponentSummary));
        OnPropertyChanged(nameof(ComponentDiseases));
        Refresh();
    }

    private object BuildPackageRequest()
    {
        ValidateVaccineText(nameof(Name), "Tên gói", Name, 150, true);
        ValidateVaccineText(nameof(Description), "Mô tả", Description, 2000);
        var targetAudience = Selected is not null && Selected.Package?.TargetAudience is null && TargetAudience.Length == 0
            ? null : TargetAudience;
        ValidateVaccineText(nameof(TargetAudience), "Đối tượng tham khảo", targetAudience, 1000, Selected is null || targetAudience is not null);
        if (!TryPrice(out var price)) throw Invalid(nameof(Price), PriceValidationMessage);
        if (_packageComponents.Count is < 1 or > 100)
            throw Invalid(nameof(PackageComponents), "Chọn từ 1 đến 100 loại vắc xin.");
        var items = new List<CatalogPackageItem>();
        foreach (var component in _packageComponents) {
            if (component.ParsedQuantity is not { } quantity)
                throw Invalid(nameof(PackageComponents), $"{component.Label}: số lượng phải là số nguyên từ 1 đến 100, không có dấu phân nhóm.");
            if (Status == "Active" && component.Vaccine?.Status != "Active")
                throw Invalid(nameof(PackageComponents), $"{component.Label}: {component.StatusLabel}. Gói đang sử dụng chỉ được chứa vắc xin đang sử dụng.");
            items.Add(new CatalogPackageItem(component.Id, quantity));
        }
        var description = Selected is not null && Selected.Package?.Description is null && Description.Length == 0
            ? null : Description;
        return new { Name, Code, Status, Description = description, TargetAudience = targetAudience, Price = price,
            Items = items.Select(x => new { x.VaccineId, x.Quantity }).ToArray() };
    }
}
