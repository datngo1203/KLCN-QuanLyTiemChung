using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class CatalogPackageChoice : ViewModelBase
{
    private bool _included;
    private string _quantity = "1";
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public bool Included { get => _included; set => Set(ref _included,value); }
    public string Quantity { get => _quantity; set => Set(ref _quantity,value); }
}
public sealed record CatalogRow(int Id, string Name, string Description = "", decimal? Price = null, CatalogVaccine? Vaccine = null, CatalogPackage? Package = null);

public sealed class CatalogViewModel : ViewModelBase
{
    private readonly CatalogApiService _service;
    private readonly IReadOnlyCollection<string> _permissions;
    private string _tab = "Vắc xin", _name = "", _description = "", _manufacturer = "", _origin = "", _price = "", _search = "", _feedback = "";
    private bool _busy;
    private CatalogRow? _selected;
    private CatalogType? _vaccineType, _injectionType;
    public CatalogViewModel(CatalogApiService? service = null, IReadOnlyCollection<string>? permissions = null) {
        _service = service ?? new();
        _permissions = permissions?.ToArray() ?? [];
        LoadCommand = new RelayCommand(async _ => await Run(Load), _ => !_busy);
        SaveCommand = new RelayCommand(async _ => await Run(Save), _ => !_busy && CanWrite && !string.IsNullOrWhiteSpace(Name));
        DeleteCommand = new RelayCommand(async _ => await Run(Delete), _ => !_busy && CanWrite && Selected is not null);
        NewCommand = new RelayCommand(_ => Clear(), _ => !_busy && CanWrite);
        SelectTabCommand = new RelayCommand(async p => { if (p is string tab) { if (Tab != tab) Rows.Clear(); Tab=tab; Clear(); await Run(Load); } }, _ => !_busy);
    }
    public IReadOnlyList<string> Tabs { get; } = ["Loại tiêm chủng", "Loại vắc xin", "Vắc xin", "Gói vắc xin"];
    public ObservableCollection<CatalogRow> Rows { get; } = [];
    public ObservableCollection<CatalogType> VaccineTypes { get; } = [];
    public ObservableCollection<CatalogType> InjectionTypes { get; } = [];
    public ObservableCollection<CatalogPackageChoice> PackageChoices { get; } = [];
    public RelayCommand LoadCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand NewCommand { get; }
    public RelayCommand SelectTabCommand { get; }
    public string Tab { get => _tab; private set { Set(ref _tab,value); OnPropertyChanged(nameof(ShowVaccine)); OnPropertyChanged(nameof(ShowPackage)); OnPropertyChanged(nameof(ShowLookup)); OnPropertyChanged(nameof(ShowPrice)); } }
    public bool ShowVaccine => Tab == "Vắc xin";
    public bool ShowPackage => Tab == "Gói vắc xin";
    public bool ShowLookup => !ShowVaccine && !ShowPackage;
    public bool ShowPrice => !ShowLookup;
    public bool CanWrite => _permissions.Contains("QuanLyDanhMucChuyenMon") && (ShowLookup || _permissions.Contains("QuanLyDanhMucGia"));
    public bool CanEdit => CanInteract && CanWrite;
    public bool CanInteract => !_busy;
    public string DataSourceLabel => "API / SQL thật · [DEMO] là dữ liệu giả";
    public string Name { get => _name; set { Set(ref _name,value); Refresh(); } }
    public string Description { get => _description; set => Set(ref _description,value); }
    public string Manufacturer { get => _manufacturer; set => Set(ref _manufacturer,value); }
    public string Origin { get => _origin; set => Set(ref _origin,value); }
    public string Price { get => _price; set => Set(ref _price,value); }
    public string Search { get => _search; set => Set(ref _search,value); }
    public string Feedback { get => _feedback; private set => Set(ref _feedback,value); }
    public CatalogType? VaccineType { get => _vaccineType; set => Set(ref _vaccineType,value); }
    public CatalogType? InjectionType { get => _injectionType; set => Set(ref _injectionType,value); }
    public CatalogRow? Selected { get => _selected; set {
        Set(ref _selected,value);
        if (value is not null) {
            Name=value.Name; Description=value.Description; Price=value.Price?.ToString("0.##",CultureInfo.InvariantCulture) ?? "";
            Manufacturer=value.Vaccine?.Manufacturer ?? ""; Origin=value.Vaccine?.Origin ?? "";
            VaccineType=VaccineTypes.FirstOrDefault(x => x.Id == value.Vaccine?.VaccineTypeId);
            InjectionType=InjectionTypes.FirstOrDefault(x => x.Id == value.Vaccine?.InjectionTypeId);
            foreach (var choice in PackageChoices) {
                var item=value.Package?.Items.FirstOrDefault(x => x.VaccineId == choice.Id);
                choice.Included=item is not null; choice.Quantity=(item?.Quantity ?? 1).ToString(CultureInfo.InvariantCulture);
            }
        }
        Refresh();
    } }
    private string Resource => Tab switch { "Loại tiêm chủng" => "injection-types", "Loại vắc xin" => "vaccine-types", "Gói vắc xin" => "packages", _ => "vaccines" };
    public Task InitializeAsync() => Run(Load);
    private async Task Run(Func<Task> action) {
        if (_busy) return;
        _busy=true; Refresh(); Feedback="Đang xử lý…";
        try { await action(); Feedback="Đã tải/lưu qua API và SQL."; }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException or FormatException or OverflowException) { Feedback=e.Message; }
        finally { _busy=false; Refresh(); }
    }
    private async Task Load() {
        // Fetch first so a failed reload never clears the visible data or current edit.
        var types=await _service.ListAsync<CatalogType>("vaccine-types");
        var injections=await _service.ListAsync<CatalogType>("injection-types");
        var vaccines=await _service.ListAsync<CatalogVaccine>("vaccines");
        CatalogRow[] rows;
        if (ShowLookup) rows=(await _service.ListAsync<CatalogType>(Resource,Search)).Select(x => new CatalogRow(x.Id,x.Name,x.Description)).ToArray();
        else if (ShowPackage) rows=(await _service.ListAsync<CatalogPackage>(Resource,Search)).Select(x => new CatalogRow(x.Id,x.Name,Price:x.Price,Package:x)).ToArray();
        else rows=(await _service.ListAsync<CatalogVaccine>(Resource,Search)).Select(x => new CatalogRow(x.Id,x.Name,Price:x.Price,Vaccine:x)).ToArray();
        VaccineTypes.Clear(); foreach (var x in types) VaccineTypes.Add(x);
        InjectionTypes.Clear(); foreach (var x in injections) InjectionTypes.Add(x);
        PackageChoices.Clear(); foreach (var x in vaccines) PackageChoices.Add(new() { Id=x.Id, Name=x.Name });
        Rows.Clear(); foreach (var x in rows) Rows.Add(x);
        Clear();
    }
    private async Task Save() {
        object body;
        if (ShowLookup) body=new { Name, Description };
        else {
            if (!decimal.TryParse(Price,NumberStyles.None,CultureInfo.InvariantCulture,out var price) || price < 0) throw new InvalidOperationException("Giá phải là số nguyên VND không âm, không dấu phân cách.");
            if (ShowVaccine) body=new { Name, Manufacturer, Origin, Price=price, VaccineTypeId=VaccineType?.Id ?? 0, InjectionTypeId=InjectionType?.Id ?? 0 };
            else {
                var items=PackageChoices.Where(x => x.Included).Select(x => new CatalogPackageItem(x.Id,int.Parse(x.Quantity,CultureInfo.InvariantCulture))).ToArray();
                body=new { Name, Price=price, Items=items };
            }
        }
        await _service.SaveAsync(Resource,Selected?.Id,body); await Load();
    }
    private async Task Delete() { if (Selected is null) return; await _service.DeleteAsync(Resource,Selected.Id); await Load(); }
    private void Clear() {
        Selected=null; Name=Description=Manufacturer=Origin=Price=""; VaccineType=InjectionType=null;
        foreach (var choice in PackageChoices) { choice.Included=false; choice.Quantity="1"; }
    }
    private void Refresh() { OnPropertyChanged(nameof(CanInteract)); OnPropertyChanged(nameof(CanWrite)); OnPropertyChanged(nameof(CanEdit)); SaveCommand?.NotifyCanExecuteChanged(); DeleteCommand?.NotifyCanExecuteChanged(); LoadCommand?.NotifyCanExecuteChanged(); NewCommand?.NotifyCanExecuteChanged(); SelectTabCommand?.NotifyCanExecuteChanged(); }
}
