using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using TiemChung.Contracts;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed partial class CatalogViewModel
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    private static readonly CatalogStatus[] RouteLabels = [
        new("Intramuscular", "Tiêm bắp"), new("Subcutaneous", "Tiêm dưới da"),
        new("Intradermal", "Tiêm trong da"), new("Oral", "Đường uống"),
        new("Nasal", "Đường mũi"), new("Other", "Khác")
    ];
    private CatalogType[] _diseaseOptions = [], _technologyOptions = [];
    private string _dosage = "", _volume = "", _originalRegimens = "";
    private string? _validationField;

    public CatalogTaxonomySelection DiseaseSelection { get; } = new();
    public CatalogTaxonomySelection TechnologySelection { get; } = new();
    public ObservableCollection<CatalogRegimenDraft> Regimens { get; } = [];
    public IReadOnlyList<CatalogStatus> VaccineStatuses { get; } = [new("Active", "Đang sử dụng"), new("Inactive", "Ngừng sử dụng")];
    public IReadOnlyList<CatalogStatus> VaccineRoutes { get; private set; } = RouteLabels;
    public string[] ManufacturerSuggestions { get; private set; } = [];
    public string[] OriginSuggestions { get; private set; } = [];
    public string Dosage { get => _dosage; set { if (Set(ref _dosage, value)) Refresh(); } }
    public string Volume { get => _volume; set { if (Set(ref _volume, value)) Refresh(); } }
    public string? ValidationField { get => _validationField; private set => Set(ref _validationField, value); }
    public string PriceDisplay => TryPrice(out var price) ? price.ToString("N0", Vietnamese) + " ₫" : "";
    public bool CanClearInformationDate => IsReadOnly == false && InformationDate.HasValue && Selected?.Vaccine?.InformationUpdatedAt is null;

    private void OnTaxonomyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CatalogTaxonomySelection.Selected)) Refresh();
    }

    private void ApplySuggestions(CatalogSuggestions suggestions)
    {
        VaccineRoutes = RouteLabels.Where(route => suggestions.AdministrationRoutes.Contains(route.Value)).ToArray();
        ManufacturerSuggestions = suggestions.Manufacturers;
        OriginSuggestions = suggestions.Origins;
        OnPropertyChanged(nameof(VaccineRoutes));
        OnPropertyChanged(nameof(ManufacturerSuggestions));
        OnPropertyChanged(nameof(OriginSuggestions));
    }

    public static string[] FindSuggestions(IEnumerable<string> values, string query)
    {
        var text = query.Trim();
        return values.Select(x => x.Trim())
            .Where(x => x.Length > 0 && !x.Equals(text, StringComparison.OrdinalIgnoreCase) && Matches(x, text))
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(5).ToArray();
    }

    internal static bool Matches(string value, string query) =>
        WithoutDiacritics(value).Contains(WithoutDiacritics(query), StringComparison.OrdinalIgnoreCase);

    private void ApplyVaccine(CatalogVaccine? vaccine)
    {
        Dosage = vaccine?.Dosage ?? "";
        Volume = vaccine?.Volume?.ToString("0.##", Vietnamese) ?? "";
        DiseaseSelection.Reset(ShowVaccine ? _diseaseOptions : [], vaccine?.Diseases);
        TechnologySelection.Reset(ShowVaccine ? _technologyOptions : [], vaccine?.Technologies);
        foreach (var regimen in Regimens) regimen.Changed -= Refresh;
        Replace(Regimens, (vaccine?.Regimens ?? []).Select(x => new CatalogRegimenDraft(x)));
        foreach (var regimen in Regimens) regimen.Changed += Refresh;
        _originalRegimens = JsonSerializer.Serialize(Regimens);
    }

    public CatalogRegimenDraft? AddRegimen()
    {
        if (!ShowVaccine || !CanEdit) return null;
        var regimen = new CatalogRegimenDraft();
        regimen.Changed += Refresh;
        Regimens.Add(regimen);
        Refresh();
        return regimen;
    }

    public void RemoveRegimen(CatalogRegimenDraft regimen)
    {
        if (!ShowVaccine || !CanEdit || !Regimens.Contains(regimen) ||
            (regimen.HasContent && Confirm?.Invoke("Bỏ phác đồ tham khảo và các mũi trong phác đồ này?") != true)) return;
        regimen.Changed -= Refresh;
        Regimens.Remove(regimen);
        Refresh();
    }

    public CatalogDoseDraft? AddDose(CatalogRegimenDraft regimen) =>
        ShowVaccine && CanEdit && Regimens.Contains(regimen) ? regimen.AddDose() : null;

    public void RemoveDose(CatalogRegimenDraft regimen, CatalogDoseDraft dose)
    {
        if (!ShowVaccine || !CanEdit || !Regimens.Contains(regimen) || !regimen.Doses.Contains(dose) ||
            (dose.HasContent && Confirm?.Invoke($"Bỏ mũi {dose.Order} khỏi phác đồ tham khảo?") != true)) return;
        regimen.RemoveDose(dose);
    }

    private VaccineRequest BuildVaccineRequest()
    {
        var original = Selected?.Vaccine;
        var activating = original is { Status: "Inactive" } && Status == "Active";
        ValidateVaccineText(nameof(Name), "Tên vắc xin", Name, 150, true);
        ValidateVaccineText(nameof(Description), "Mô tả", Description, 2000);
        ValidateVaccineText(nameof(Manufacturer), "Nhà sản xuất", Manufacturer, 150, true);
        ValidateVaccineText(nameof(Origin), "Xuất xứ", Origin, 100, true);
        ValidateVaccineText(nameof(Dosage), "Liều lượng", Dosage, 200);

        var diseases = DiseaseSelection.SelectedIds;
        var diseasesChanged = original is null || !SameLinks(original.Diseases, diseases);
        if (diseasesChanged && diseases.Length == 0)
            throw Invalid(nameof(DiseaseSelection), "Chọn ít nhất một phòng bệnh. Khi bỏ liên kết cuối, hãy chọn bệnh thay thế.");
        if (Status == "Active" && (diseasesChanged || activating) && !DiseaseSelection.Selected.Any(x => x.Active))
            throw Invalid(nameof(DiseaseSelection), "Vắc xin đang sử dụng cần ít nhất một phòng bệnh đang sử dụng.");
        var technologies = TechnologySelection.SelectedIds;

        var targetAudience = original is not null && original.TargetAudience is null && TargetAudience.Length == 0 ? null : TargetAudience;
        ValidateVaccineText(nameof(TargetAudience), "Đối tượng sử dụng tham khảo", targetAudience, 1000, original is null || activating || targetAudience != null);
        if ((original is null || original.AdministrationRoute is not null || activating || AdministrationRoute is not null)
            && !VaccineRoutes.Any(x => x.Value == AdministrationRoute))
            throw Invalid(nameof(AdministrationRoute), "Chọn một đường dùng trong danh sách.");
        if (ShowRouteNote) ValidateVaccineText(nameof(AdministrationRouteNote), "Mô tả đường dùng khác", AdministrationRouteNote, 500, true);

        decimal? volume = null;
        if (!string.IsNullOrWhiteSpace(Volume)) {
            // Accept comma or period as a decimal separator, never a grouping separator.
            if (!decimal.TryParse(Volume.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed)
                || parsed > 99999999.99m || decimal.Round(parsed, 2) != parsed)
                throw Invalid(nameof(Volume), "Thể tích phải từ 0 đến 99.999.999,99, tối đa hai chữ số thập phân và không có dấu phân nhóm.");
            volume = parsed;
        }
        else if (original?.Volume is not null)
            throw Invalid(nameof(Volume), "Nhập thể tích để cập nhật. Để trống sẽ giữ giá trị đã lưu.");

        if (!TryPrice(out var price)) throw Invalid(nameof(Price), PriceValidationMessage);
        ValidateVaccineText(nameof(SourceName), "Nguồn thông tin", SourceName, 250);
        ValidateVaccineUrl(nameof(SourceUrl), "URL nguồn", SourceUrl);
        var savedDate = original?.InformationUpdatedAt;
        if (savedDate.HasValue && InformationDate is null)
            throw Invalid(nameof(InformationDate), "Chọn ngày để cập nhật. Để trống sẽ giữ ngày đã lưu.");
        var informationDate = InformationDate is { } date && date != savedDate?.ToOffset(VietnamOffset).Date
            ? new DateTimeOffset(DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified), VietnamOffset)
            : savedDate;
        var regimens = JsonSerializer.Serialize(Regimens) == _originalRegimens ? null : Regimens.Select(BuildRegimen).ToArray();

        return new VaccineRequest(Name, Manufacturer, Origin, price, Code, Status,
            diseasesChanged ? diseases : null, OptionalText(Description, original?.Description), targetAudience, AdministrationRoute,
            ShowRouteNote ? AdministrationRouteNote : null, Dosage, volume,
            OptionalText(SourceName, original?.SourceName), OptionalText(SourceUrl, original?.SourceUrl),
            informationDate, regimens, original is null || !SameLinks(original.Technologies, technologies) ? technologies : null);
    }

    private VaccineRegimenRequest BuildRegimen(CatalogRegimenDraft regimen, int index)
    {
        const string field = nameof(Regimens);
        var label = $"Phác đồ {index + 1}";
        ValidateVaccineText(field, label + " — tên", regimen.Name, 150, true);
        ValidateVaccineText(field, label + " — đối tượng tham khảo", regimen.TargetAudience, 1000, true);
        ValidateVaccineText(field, label + " — tóm tắt", regimen.Summary, 1500);
        ValidateVaccineText(field, label + " — nguồn", regimen.SourceName, 250);
        ValidateVaccineUrl(field, label + " — URL nguồn", regimen.SourceUrl);
        if (regimen.Doses.Count is < 1 or > 20) throw Invalid(field, $"{label} cần từ 1 đến 20 mũi.");
        var doses = regimen.Doses.Select(dose => {
            var doseLabel = $"{label}, mũi {dose.Order}";
            ValidateVaccineText(field, doseLabel + " — thời điểm", dose.TimingLabel, 250, true);
            ValidateVaccineText(field, doseLabel + " — ghi chú", dose.Note, 500);
            var min = ReadDays(dose.MinimumIntervalDays, doseLabel + " — số ngày tối thiểu");
            var recommended = ReadDays(dose.RecommendedIntervalDays, doseLabel + " — số ngày khuyến nghị");
            if (recommended < min) throw Invalid(field, $"{doseLabel} — số ngày khuyến nghị phải từ số ngày tối thiểu trở lên.");
            int? interval = null;
            if (dose.AnchorDoseOrder.HasValue || !string.IsNullOrWhiteSpace(dose.IntervalValue) || dose.IntervalUnit is not null) {
                if (dose.AnchorDoseOrder is not int anchor || !dose.AnchorOptions.Any(option => option.Order == anchor))
                    throw Invalid(field, $"{doseLabel} — chọn một mũi neo đứng trước mũi này.");
                if (!int.TryParse(dose.IntervalValue.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value <= 0)
                    throw Invalid(field, $"{doseLabel} — khoảng cách phải là số nguyên dương.");
                if (dose.IntervalUnit is null || !dose.IntervalUnits.Any(unit => unit.Value == dose.IntervalUnit))
                    throw Invalid(field, $"{doseLabel} — chọn đơn vị khoảng cách.");
                if (min.HasValue || recommended.HasValue)
                    throw Invalid(field, $"{doseLabel} — xóa các khoảng cách ngày cũ để dùng khoảng cách theo mũi neo.");
                interval = value;
            }
            return new VaccineRegimenDoseRequest(dose.Order, dose.TimingLabel, min, recommended, dose.Note,
                dose.AnchorDoseOrder, interval, dose.IntervalUnit);
        }).ToArray();
        return new VaccineRegimenRequest(regimen.Name, regimen.TargetAudience, regimen.Summary, doses, regimen.SourceName, regimen.SourceUrl);
    }

    private int? ReadDays(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (int.TryParse(value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var days)) return days;
        throw Invalid(nameof(Regimens), $"{label} phải là số nguyên không âm.");
    }

    private void ValidateVaccineText(string field, string label, string? value, int max, bool required = false)
    {
        if (required && string.IsNullOrWhiteSpace(value)) throw Invalid(field, $"{label} là bắt buộc.");
        if (value?.Trim().Length > max) throw Invalid(field, $"{label} tối đa {max} ký tự.");
    }

    private void ValidateVaccineUrl(string field, string label, string value)
    {
        ValidateVaccineText(field, label, value, 1000);
        if (!string.IsNullOrWhiteSpace(value) &&
            (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http") ||
             string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo)))
            throw Invalid(field, $"{label} phải là địa chỉ http hoặc https hợp lệ.");
    }

    private InvalidOperationException Invalid(string field, string message)
    {
        ValidationField = field;
        return new InvalidOperationException(message);
    }

    private const string PriceValidationMessage = "Nhập giá VND từ 0 đến 9 999 999 999 999 999. Chỉ dùng chữ số và dấu cách, không nhập phần thập phân.";

    private bool TryPrice(out decimal price) =>
        decimal.TryParse(Price.Replace(" ", ""), NumberStyles.None, CultureInfo.InvariantCulture, out price) && price <= 9999999999999999m;

    private static bool SameLinks(CatalogReference[]? original, int[] selected) =>
        (original ?? []).Select(x => x.Id).Order().SequenceEqual(selected);

    private static string? OptionalText(string value, string? original) =>
        value.Length == 0 && original is null ? null : value;
}

public sealed record CatalogTaxonomyOption(int Id, string Code, string Name, bool Active)
{
    public string Label => $"{Code} — {Name}" + (Active ? "" : " · Ngừng sử dụng");
    public string RemoveLabel => "Bỏ " + Label;
}

public sealed class CatalogTaxonomySelection : ViewModelBase
{
    private CatalogTaxonomyOption[] _options = [];
    private readonly HashSet<int> _selected = [];
    private string _query = "";
    public string Query { get => _query; set { if (Set(ref _query, value)) NotifyResults(); } }
    public CatalogTaxonomyOption[] Selected => _options.Where(x => _selected.Contains(x.Id)).ToArray();
    public int[] SelectedIds => _selected.Order().ToArray();
    public bool HasSelection => _selected.Count != 0;
    public CatalogTaxonomyOption[] Results => _options
        .Where(x => x.Active && !_selected.Contains(x.Id) && CatalogViewModel.Matches(x.Label, Query.Trim()))
        .Take(12).ToArray();
    public string ResultsHint => Results.Length == 0
        ? "Không có mục đang sử dụng phù hợp để thêm."
        : "Chọn trong kết quả hoặc gõ thêm để thu hẹp.";

    public void Reset(CatalogType[] options, CatalogReference[]? linked)
    {
        linked ??= [];
        _selected.Clear();
        _selected.UnionWith(linked.Select(x => x.Id));
        var loadedIds = options.Select(x => x.Id).ToHashSet();
        _options = options.Select(x => new CatalogTaxonomyOption(x.Id, x.Code, x.Name, x.Status == "Active"))
            .Concat(linked.Where(x => !loadedIds.Contains(x.Id)).Select(x => new CatalogTaxonomyOption(x.Id, x.Code, x.Name, false)))
            .ToArray();
        Query = "";
        NotifySelection();
    }

    public bool Add(int id)
    {
        if (!_options.Any(x => x.Id == id && x.Active) || !_selected.Add(id)) return false;
        Query = "";
        NotifySelection();
        return true;
    }

    public void Remove(int id) { if (_selected.Remove(id)) NotifySelection(); }

    private void NotifySelection()
    {
        OnPropertyChanged(nameof(Selected));
        OnPropertyChanged(nameof(HasSelection));
        NotifyResults();
    }

    private void NotifyResults()
    {
        OnPropertyChanged(nameof(Results));
        OnPropertyChanged(nameof(ResultsHint));
    }
}

public sealed class CatalogRegimenDraft : ViewModelBase
{
    private string _name, _targetAudience, _summary, _sourceName, _sourceUrl;
    private readonly ObservableCollection<CatalogDoseDraft> _doses;
    public CatalogRegimenDraft(VaccineRegimenDto? value = null)
    {
        _name = value?.Name ?? "";
        _targetAudience = value?.TargetAudience ?? "";
        _summary = value?.Summary ?? "";
        _sourceName = value?.SourceName ?? "";
        _sourceUrl = value?.SourceUrl ?? "";
        _doses = new((value?.Doses ?? []).OrderBy(x => x.DoseOrder).Select(x => new CatalogDoseDraft(x)));
        Doses = new(_doses);
        foreach (var dose in Doses) {
            dose.UpdateAnchors(Doses, dose.AnchorDoseOrder);
            dose.PropertyChanged += DoseChanged;
        }
    }
    public event Action? Changed;
    public ReadOnlyObservableCollection<CatalogDoseDraft> Doses { get; }
    public bool CanAddDose => Doses.Count < 20;
    public bool HasContent => !string.IsNullOrWhiteSpace(Name + TargetAudience + Summary + SourceName + SourceUrl) || Doses.Any(x => x.HasContent);
    public string Name { get => _name; set => Update(ref _name, value, nameof(Name)); }
    public string TargetAudience { get => _targetAudience; set => Update(ref _targetAudience, value, nameof(TargetAudience)); }
    public string Summary { get => _summary; set => Update(ref _summary, value, nameof(Summary)); }
    public string SourceName { get => _sourceName; set => Update(ref _sourceName, value, nameof(SourceName)); }
    public string SourceUrl { get => _sourceUrl; set => Update(ref _sourceUrl, value, nameof(SourceUrl)); }

    internal CatalogDoseDraft? AddDose()
    {
        if (!CanAddDose) return null;
        var dose = new CatalogDoseDraft();
        dose.PropertyChanged += DoseChanged;
        _doses.Add(dose);
        NotifyDoseStructure();
        return dose;
    }

    internal void RemoveDose(CatalogDoseDraft dose)
    {
        dose.PropertyChanged -= DoseChanged;
        _doses.Remove(dose);
        NotifyDoseStructure();
    }

    private void NotifyDoseStructure()
    {
        // Map the old order before renumbering so an anchor keeps pointing to the same dose.
        var orders = _doses.Select((dose, index) => (dose.Order, NewOrder: index + 1)).ToDictionary(x => x.Order, x => x.NewOrder);
        var anchors = _doses.Select(dose => dose.AnchorDoseOrder is int anchor && orders.TryGetValue(anchor, out var order)
            ? (int?)order : null).ToArray();
        for (var i = 0; i < _doses.Count; i++) _doses[i].Order = i + 1;
        for (var i = 0; i < _doses.Count; i++) _doses[i].UpdateAnchors(Doses, anchors[i]);
        OnPropertyChanged(nameof(CanAddDose));
        Changed?.Invoke();
    }

    private void Update(ref string field, string value, string property)
    {
        if (Set(ref field, value, property)) Changed?.Invoke();
    }

    private void DoseChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => Changed?.Invoke();
}

public sealed class CatalogDoseDraft : ViewModelBase
{
    private static readonly CatalogStatus[] Units = [
        new(null, "Chọn đơn vị"), new("Days", "Ngày"), new("Weeks", "Tuần"), new("Months", "Tháng"), new("Years", "Năm")
    ];
    private int _order;
    private int? _anchorDoseOrder;
    private string? _intervalUnit;
    private string _timingLabel, _minimumIntervalDays, _recommendedIntervalDays, _note, _intervalValue;
    public CatalogDoseDraft(VaccineRegimenDoseDto? value = null)
    {
        _order = value?.DoseOrder ?? 0;
        _timingLabel = value?.TimingLabel ?? "";
        _minimumIntervalDays = value?.MinimumIntervalDays?.ToString(CultureInfo.InvariantCulture) ?? "";
        _recommendedIntervalDays = value?.RecommendedIntervalDays?.ToString(CultureInfo.InvariantCulture) ?? "";
        _anchorDoseOrder = value?.AnchorDoseOrder;
        _intervalValue = value?.IntervalValue?.ToString(CultureInfo.InvariantCulture) ?? "";
        _intervalUnit = value?.IntervalUnit;
        _note = value?.Note ?? "";
    }
    public bool HasContent => AnchorDoseOrder.HasValue || IntervalUnit is not null ||
        !string.IsNullOrWhiteSpace(TimingLabel + MinimumIntervalDays + RecommendedIntervalDays + Note + IntervalValue);
    public int Order { get => _order; internal set => Set(ref _order, value); }
    public string TimingLabel { get => _timingLabel; set => Set(ref _timingLabel, value); }
    public string MinimumIntervalDays { get => _minimumIntervalDays; set => Set(ref _minimumIntervalDays, value); }
    public string RecommendedIntervalDays { get => _recommendedIntervalDays; set => Set(ref _recommendedIntervalDays, value); }
    public int? AnchorDoseOrder { get => _anchorDoseOrder; set => Set(ref _anchorDoseOrder, value); }
    public string IntervalValue { get => _intervalValue; set => Set(ref _intervalValue, value); }
    public string? IntervalUnit { get => _intervalUnit; set => Set(ref _intervalUnit, value); }
    [JsonIgnore] public IReadOnlyList<CatalogStatus> IntervalUnits => Units;
    [JsonIgnore] public IReadOnlyList<CatalogDoseAnchor> AnchorOptions { get; private set; } = [];
    public string Note { get => _note; set => Set(ref _note, value); }

    internal void UpdateAnchors(IEnumerable<CatalogDoseDraft> doses, int? anchor)
    {
        AnchorOptions = [new(null, "Không dùng mũi neo"), .. doses.Where(d => d.Order < Order)
            .Select(d => new CatalogDoseAnchor(d.Order, $"Mũi {d.Order}"))];
        OnPropertyChanged(nameof(AnchorOptions));
        _anchorDoseOrder = anchor;
        OnPropertyChanged(nameof(AnchorDoseOrder));
    }
}

public sealed record CatalogDoseAnchor(int? Order, string Label);
