using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed record CatalogStatus(string? Value, string Label)
{
    public override string ToString() => Label;
    public static string Describe(string value) => value == "Inactive" ? "Ngừng sử dụng" : "Đang sử dụng";
}
public sealed record CatalogRow(int Id, string Name, string Description = "", decimal? Price = null, CatalogVaccine? Vaccine = null, CatalogPackage? Package = null, string Code = "", string Status = "Active", CatalogType? Technology = null)
{
    public string StatusLabel => CatalogStatus.Describe(Status);
}

public sealed partial class CatalogViewModel : ViewModelBase
{
    private readonly CatalogApiService _service;
    private readonly IReadOnlyCollection<string> _permissions;
    private string _tab = "Vắc xin", _name = "", _description = "", _manufacturer = "", _origin = "", _price = "", _search = "", _feedback = "";
    private bool _busy, _codeEdited, _loadingForm, _loadFailed, _hasLoaded;
    private string _code = "", _targetAudience = "", _routeNote = "", _status = "Active", _baseline = "";
    private string? _route;
    private CatalogStatus _filter;
    private CatalogRow? _selected;
    private string _technical = "", _professional = "", _sourceName = "", _sourceUrl = "";
    private DateTime? _informationDate;

    // The view supplies the native confirmation; absent confirmation always keeps the draft/record.
    public Func<string, bool>? Confirm { get; set; }
    public CatalogViewModel(CatalogApiService? service = null, IReadOnlyCollection<string>? permissions = null)
    {
        _service = service ?? new();
        _permissions = permissions?.ToArray() ?? [];
        _filter = StatusFilters[0];
        LoadCommand = new RelayCommand(async _ => { if (MayDiscard()) await Run(() => Load(Selected?.Id)); }, _ => !_busy);
        SaveCommand = new RelayCommand(async _ => await Run(Save), _ => CanEdit);
        DeleteCommand = new RelayCommand(async _ => {
            if (Selected is { } row && Confirm?.Invoke($"Xóa {row.Code} — {row.Name}?\nThao tác này không thể hoàn tác.") == true) await Run(Delete);
        }, _ => CanEdit && Selected is not null);
        NewCommand = new RelayCommand(_ => { if (MayDiscard()) { Clear(); Feedback = "Nhập thông tin để tạo " + ResourceLabel + "."; } }, _ => CanEdit);
        RevertCommand = new RelayCommand(_ => { if (MayDiscard()) { Apply(Selected); Feedback = "Đã hoàn tác thay đổi chưa lưu."; } }, _ => !_busy && IsDirty);
        SelectTabCommand = new RelayCommand(async p => { if (p is string tab) await SelectResourceAsync(tab); }, _ => !_busy);
        DiseaseSelection.PropertyChanged += OnTaxonomyChanged;
        TechnologySelection.PropertyChanged += OnTaxonomyChanged;
        InitializePackageCommands();
        MarkClean();
    }
    public IReadOnlyList<string> Tabs { get; } = ["Phòng bệnh", "Công nghệ vắc xin", "Vắc xin", "Gói vắc xin"];
    public IReadOnlyList<CatalogStatus> StatusOptions { get; } = [new("Active", CatalogStatus.Describe("Active")), new("Inactive", CatalogStatus.Describe("Inactive"))];
    public IReadOnlyList<CatalogStatus> StatusFilters { get; } = [new(null, "Tất cả trạng thái"), new("Active", CatalogStatus.Describe("Active")), new("Inactive", CatalogStatus.Describe("Inactive"))];
    public ObservableCollection<CatalogRow> Rows { get; } = [];
    public RelayCommand LoadCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand NewCommand { get; }
    public RelayCommand RevertCommand { get; }
    public RelayCommand SelectTabCommand { get; }
    public string Tab { get => _tab; set { if (value != _tab) SelectTabCommand.Execute(value); } }
    public bool ShowVaccine => Tab == "Vắc xin";
    public bool ShowPackage => Tab == "Gói vắc xin";
    public bool ShowLookup => !ShowVaccine && !ShowPackage;
    public bool ShowPrice => !ShowLookup;
    public bool ShowTechnology => Tab == "Công nghệ vắc xin";
    public bool CanWrite => _permissions.Contains("QuanLyDanhMucChuyenMon") && (ShowLookup || _permissions.Contains("QuanLyDanhMucGia"));
    public bool CanEdit => CanInteract && CanWrite;
    public bool IsReadOnly => !CanWrite;
    public bool CanInteract => !_busy;
    public bool IsBusy => _busy;
    public bool LoadFailed => _loadFailed;
    public bool ShowEmpty => _hasLoaded && !_busy && !_loadFailed && Rows.Count == 0;
    public string EmptyMessage => string.IsNullOrWhiteSpace(Search) && StatusFilter.Value is null ? "Chưa có dữ liệu. Chọn Thêm mới để tạo danh mục." : "Không có kết quả phù hợp. Thử từ khóa hoặc trạng thái khác.";
    public string SearchHint => ShowVaccine ? "Tìm mã, tên vắc xin hoặc bệnh phòng" : "Tìm mã hoặc tên " + ResourceLabel;
    public string DataSourceLabel => "Thông tin vắc xin, phòng bệnh và gói tiêm chủng.";
    public string ResourceLabel => Resource switch { "vaccine-technologies" => "công nghệ vắc xin", "diseases" => "phòng bệnh", "packages" => "gói vắc xin", _ => "vắc xin" };
    public string FormTitle => Selected is null ? (IsReadOnly ? "Xem " : "Thêm ") + ResourceLabel : "Chi tiết " + ResourceLabel;
    public string SaveLabel => Selected is null ? "Tạo mới" : "Lưu thay đổi";
    public string EditLabel => IsDirty ? "Có thay đổi chưa lưu" : Selected is null ? (IsReadOnly ? "Chọn bản ghi trong danh sách để xem." : "Bản ghi mới") : "Đã lưu · " + Selected.Code;
    public bool CodeReadOnly => Selected is not null || IsReadOnly;
    public bool IsDirty => CanWrite && !_loadingForm && _baseline != Fingerprint();
    public string Code { get => _code; set { if (CodeReadOnly) return; _codeEdited = true; Set(ref _code, value); Refresh(); } }
    public string Name { get => _name; set {
        Set(ref _name, value);
        if (!_loadingForm && Selected is null && !_codeEdited) Set(ref _code, SuggestCode(value), nameof(Code));
        Refresh();
    } }
    public string Description { get => _description; set { Set(ref _description, value); Refresh(); } }
    public string Status { get => _status; set { Set(ref _status, value); Refresh(); } }
    public string StatusLabel => CatalogStatus.Describe(Status);
    public CatalogStatus StatusFilter { get => _filter; set { if (value is not null) Set(ref _filter, value); } }
    public string TargetAudience { get => _targetAudience; set { Set(ref _targetAudience, value); Refresh(); } }
    public string? AdministrationRoute { get => _route; set { Set(ref _route, value); OnPropertyChanged(nameof(ShowRouteNote)); Refresh(); } }
    public string AdministrationRouteNote { get => _routeNote; set { Set(ref _routeNote, value); Refresh(); } }
    public bool ShowRouteNote => AdministrationRoute == "Other";
    public string Manufacturer { get => _manufacturer; set { Set(ref _manufacturer, value); Refresh(); } }
    public string Origin { get => _origin; set { Set(ref _origin, value); Refresh(); } }
    public string Price { get => _price; set { Set(ref _price, value.Replace(" ", "")); OnPropertyChanged(nameof(PriceDisplay)); Refresh(); } }
    public string Search { get => _search; set => Set(ref _search, value); }
    public string Feedback { get => _feedback; private set => Set(ref _feedback, value); }
    public string TechnicalCharacteristics { get => _technical; set { Set(ref _technical, value); Refresh(); } }
    public string ProfessionalNotes { get => _professional; set { Set(ref _professional, value); Refresh(); } }
    public string SourceName { get => _sourceName; set { Set(ref _sourceName, value); Refresh(); } }
    public string SourceUrl { get => _sourceUrl; set { Set(ref _sourceUrl, value); Refresh(); } }
    public DateTime? InformationDate { get => _informationDate; set {
        if (Set(ref _informationDate, value)) OnPropertyChanged(nameof(CanClearInformationDate));
        Refresh();
    } }
    public CatalogRow? Selected { get => _selected; set {
        if (_busy || Equals(value, _selected)) return;
        if (!MayDiscard()) { OnPropertyChanged(nameof(Selected)); return; }
        Apply(value); Feedback = "";
    } }
    private string Resource => Tab switch { "Công nghệ vắc xin" => "vaccine-technologies", "Phòng bệnh" => "diseases", "Gói vắc xin" => "packages", _ => "vaccines" };
    public async Task SelectResourceAsync(string tab)
    {
        if (_busy || !Tabs.Contains(tab) || !MayDiscard()) { OnPropertyChanged(nameof(Tab)); return; }
        await Run(async () => {
            Set(ref _tab, tab, nameof(Tab)); Search = ""; Set(ref _filter, StatusFilters[0], nameof(StatusFilter));
            Rows.Clear(); _hasLoaded = false; _loadFailed = false; Clear(); Refresh();
            await Load();
        });
    }
    public Task InitializeAsync() => _hasLoaded ? Task.CompletedTask : Run(() => Load());
    public bool MayDiscard() => !IsDirty || Confirm?.Invoke("Bỏ các thay đổi chưa lưu của " + ResourceLabel + "?") == true;
    private async Task Run(Func<Task> action)
    {
        if (_busy) return;
        _busy = true; ValidationField = null; Feedback = ""; Refresh();
        try { await action(); }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException or FormatException or OverflowException) {
            Feedback = e switch {
                TaskCanceledException => "Yêu cầu quá thời gian. Hãy tải lại để đối chiếu trước khi gửi lại.",
                HttpRequestException { HttpRequestError: not HttpRequestError.Unknown } => "Không kết nối được dịch vụ danh mục. Kiểm tra kết nối, thử lại tải để đối chiếu trước khi gửi lại.",
                JsonException => "Phản hồi danh mục không hợp lệ. Hãy thử lại tải danh sách.",
                _ => e.Message
            };
        }
        finally { _busy = false; Refresh(); }
    }
    private async Task Load(int? selectedId = null)
    {
        _loadFailed = false;
        try {
            // Lookups need only their own endpoint; no unrelated catalog can block their CRUD.
            CatalogVaccine[] vaccines = []; CatalogType[] technologies = [], diseases = [];
            CatalogSuggestions? suggestions = null;
            if (ShowVaccine) {
                technologies = await _service.ListAsync<CatalogType>("vaccine-technologies");
                diseases = await _service.ListAsync<CatalogType>("diseases");
                suggestions = await _service.SuggestionsAsync();
            }
            if (ShowPackage) vaccines = await _service.ListAsync<CatalogVaccine>("vaccines");
            CatalogRow[] rows;
            if (ShowLookup) rows = (await _service.ListAsync<CatalogType>(Resource, Search, StatusFilter.Value)).Select(x => new CatalogRow(x.Id, x.Name, x.Description, Code: x.Code, Status: x.Status, Technology: ShowTechnology ? x : null)).ToArray();
            else if (ShowPackage) rows = (await _service.ListAsync<CatalogPackage>(Resource, Search, StatusFilter.Value)).Select(x => new CatalogRow(x.Id, x.Name, Price: x.Price, Package: x, Code: x.Code, Status: x.Status)).ToArray();
            else rows = (await _service.ListAsync<CatalogVaccine>(Resource, Search, StatusFilter.Value)).Select(x => new CatalogRow(x.Id, x.Name, Price: x.Price, Vaccine: x, Code: x.Code, Status: x.Status)).ToArray();
            _loadingForm = true;
            if (suggestions is not null) {
                _diseaseOptions = diseases;
                _technologyOptions = technologies;
                ApplySuggestions(suggestions);
            }
            if (ShowPackage) _packageVaccines = vaccines;
            Replace(Rows, rows); _hasLoaded = true;
            Apply(Rows.FirstOrDefault(x => x.Id == selectedId));
            Feedback = "";
        }
        catch { _loadFailed = true; throw; }
    }
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name)) throw Invalid(nameof(Name), "Tên danh mục là bắt buộc.");
        if (ShowLookup && Name.Trim().Length > (Resource == "diseases" ? 150 : 120)) throw new InvalidOperationException("Tên quá dài: tối đa " + (Resource == "diseases" ? 150 : 120) + " ký tự.");
        var prefix = Prefix;
        if (!Regex.IsMatch(Code, "^" + prefix + "-[A-Z0-9]+(?:-[A-Z0-9]+)*$") || Code.Length > 50) throw Invalid(nameof(Code), "Mã phải bắt đầu bằng " + prefix + "-, dùng chữ HOA ASCII, số và dấu gạch nối; tối đa 50 ký tự.");
        if (ShowLookup && Description.Length > 500) throw new InvalidOperationException("Mô tả tối đa 500 ký tự.");
        object body;
        if (ShowTechnology) body = new { Name, Description, Code, Status, TechnicalCharacteristics, ProfessionalNotes, SourceName, SourceUrl,
            InformationUpdatedAt = InformationDate is { } date ? (DateTimeOffset?)new DateTimeOffset(DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified), TimeSpan.FromHours(7)) : null };
        else if (ShowLookup) body = new { Name, Description, Code, Status };
        else if (ShowVaccine) body = BuildVaccineRequest();
        else body = BuildPackageRequest();
        var creating = Selected is null;
        CatalogRow saved;
        if (ShowLookup) {
            var value = await _service.SaveAsync<CatalogType>(Resource, Selected?.Id, body);
            saved = new(value.Id, value.Name, value.Description, Code: value.Code, Status: value.Status, Technology: ShowTechnology ? value : null);
        }
        else if (ShowVaccine) {
            var value = await _service.SaveAsync<CatalogVaccine>(Resource, Selected?.Id, body);
            saved = new(value.Id, value.Name, Price: value.Price, Vaccine: value, Code: value.Code, Status: value.Status);
        }
        else {
            var value = await _service.SaveAsync<CatalogPackage>(Resource, Selected?.Id, body);
            saved = new(value.Id, value.Name, Price: value.Price, Package: value, Code: value.Code, Status: value.Status);
        }
        // Mark the committed identity immediately, even if the subsequent reload loses the network.
        Apply(saved);
        Search = ""; Set(ref _filter, StatusFilters[0], nameof(StatusFilter));
        var success = $"Đã {(creating ? "tạo" : "cập nhật")} {ResourceLabel} {saved.Code}.";
        try { await Load(saved.Id); Feedback = success; }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException) { Feedback = success + " Chưa tải lại được danh sách. Thử lại tải; không cần tạo lại."; }
    }
    private async Task Delete()
    {
        if (Selected is not { } row) return;
        await _service.DeleteAsync(Resource, row.Id);
        Clear(); Rows.Remove(row);
        var success = $"Đã xóa {ResourceLabel} {row.Code}.";
        try { await Load(); Feedback = success; }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException) { Feedback = success + " Chưa tải lại được danh sách. Hãy thử lại tải."; }
    }
    private void Clear() => Apply(null);
    private void Apply(CatalogRow? row)
    {
        _loadingForm = true;
        ValidationField = null;
        Set(ref _selected, row, nameof(Selected)); _codeEdited = false;
        OnPropertyChanged(nameof(CanClearInformationDate));
        // A reload replaces equal-valued rows; reselect the new instance after ItemsSource cleared selection.
        OnPropertyChanged(nameof(Selected));
        Set(ref _code, row?.Code ?? "", nameof(Code)); Name = row?.Name ?? ""; Description = row?.Vaccine?.Description ?? row?.Package?.Description ?? row?.Description ?? ""; Status = row?.Status ?? "Active";
        Price = row?.Price?.ToString("0.##", CultureInfo.InvariantCulture) ?? "";
        Manufacturer = row?.Vaccine?.Manufacturer ?? ""; Origin = row?.Vaccine?.Origin ?? "";
        TargetAudience = row?.Vaccine?.TargetAudience ?? row?.Package?.TargetAudience ?? "";
        AdministrationRoute = row?.Vaccine?.AdministrationRoute; AdministrationRouteNote = row?.Vaccine?.AdministrationRouteNote ?? "";
        TechnicalCharacteristics = row?.Technology?.TechnicalCharacteristics ?? ""; ProfessionalNotes = row?.Technology?.ProfessionalNotes ?? "";
        SourceName = row?.Vaccine?.SourceName ?? row?.Technology?.SourceName ?? "";
        SourceUrl = row?.Vaccine?.SourceUrl ?? row?.Technology?.SourceUrl ?? "";
        InformationDate = (row?.Vaccine?.InformationUpdatedAt ?? row?.Technology?.InformationUpdatedAt)?.ToOffset(TimeSpan.FromHours(7)).Date;
        ApplyVaccine(row?.Vaccine);
        ApplyPackage(row?.Package);
        _loadingForm = false; MarkClean();
    }
    private string Fingerprint() => JsonSerializer.Serialize(new { Code, Name, Description, Status, Manufacturer, Origin, Price = TryPrice(out var price) ? price.ToString(CultureInfo.InvariantCulture) : Price, TargetAudience, AdministrationRoute, AdministrationRouteNote, TechnicalCharacteristics, ProfessionalNotes, SourceName, SourceUrl, InformationDate, Dosage, Volume, Diseases = DiseaseSelection.SelectedIds, Technologies = TechnologySelection.SelectedIds, Regimens, Items = PackageComponents.OrderBy(x => x.Id).Select(x => new { x.Id, Quantity = x.ParsedQuantity?.ToString(CultureInfo.InvariantCulture) ?? x.Quantity }) });
    private void MarkClean() { _baseline = Fingerprint(); Refresh(); }
    private string Prefix => Resource switch { "vaccine-technologies" => "CNVX", "diseases" => "BENH", "packages" => "GOI", _ => "VX" };
    private string SuggestCode(string name)
    {
        var ascii = WithoutDiacritics(name).ToUpperInvariant();
        var slug = Regex.Replace(ascii, "[^A-Z0-9]+", "-").Trim('-');
        var code = Prefix + "-" + slug;
        return slug.Length == 0 ? "" : code[..Math.Min(code.Length, 50)].TrimEnd('-');
    }
    private static string WithoutDiacritics(string value) => new string(value.Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).Replace('Đ', 'D').Replace('đ', 'd');
    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values) { target.Clear(); foreach (var value in values) target.Add(value); }
    private void Refresh()
    {
        foreach (var name in new[] { nameof(CodeReadOnly), nameof(StatusLabel), nameof(CanInteract), nameof(CanWrite), nameof(CanEdit), nameof(IsReadOnly), nameof(IsBusy), nameof(LoadFailed), nameof(ShowEmpty), nameof(EmptyMessage), nameof(FormTitle), nameof(SaveLabel), nameof(EditLabel), nameof(IsDirty), nameof(ShowTechnology), nameof(ShowVaccine), nameof(ShowPackage), nameof(ShowLookup), nameof(ShowPrice), nameof(SearchHint) }) OnPropertyChanged(name);
        AddComponentCommand?.NotifyCanExecuteChanged();
        RemoveComponentCommand?.NotifyCanExecuteChanged();
        foreach (var command in new[] { SaveCommand, DeleteCommand, LoadCommand, NewCommand, RevertCommand, SelectTabCommand }) command?.NotifyCanExecuteChanged();
    }
}
