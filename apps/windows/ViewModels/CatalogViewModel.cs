using System.Collections.ObjectModel;
using System.Globalization;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class CatalogViewModel : ViewModelBase
{
    private readonly ICatalogService _service;
    private string _tab = "Vắc xin", _name = "", _manufacturer = "", _origin = "", _dose = "", _volume = "", _reaction = "", _price = "", _status = "Đang kinh doanh", _feedback = "", _vaccineType="", _injectionType="";
    private CatalogVaccine? _selected;
    private bool _saving;
    public CatalogViewModel(ICatalogService? service = null) { _service = service ?? new SampleCatalogService(); Load(); Vaccines.CollectionChanged += (_,_)=>OnPropertyChanged(nameof(HasNoData)); Packages.CollectionChanged += (_,_)=>OnPropertyChanged(nameof(HasNoData)); Selected = Vaccines.FirstOrDefault(); SaveCommand = new RelayCommand(_ => Save(), _ => CanSave); NewCommand = new RelayCommand(_ => Clear()); SelectTabCommand = new RelayCommand(p => { if (p is string s) { Tab = s; Clear(); } }); }
    public ObservableCollection<CatalogVaccine> Vaccines { get; } = [];
    public ObservableCollection<CatalogPackage> Packages { get; } = [];
    public ObservableCollection<string> SelectedDiseases { get; } = [];
    public IReadOnlyList<string> Tabs { get; } = ["Loại tiêm chủng", "Loại vắc xin", "Bệnh phòng ngừa", "Vắc xin", "Gói vắc xin"];
    public IReadOnlyList<string> Statuses { get; } = ["Đang kinh doanh", "Tạm ngừng", "Hết hàng"]; public IReadOnlyList<string> VaccineTypes => _service.GetVaccineTypes(); public IReadOnlyList<string> InjectionTypes => _service.GetInjectionTypes(); public IReadOnlyList<string> Diseases => _service.GetPreventableDiseases();
    public RelayCommand SaveCommand { get; } public RelayCommand NewCommand { get; } public RelayCommand SelectTabCommand { get; }
    public string Tab { get=>_tab; set { if(Set(ref _tab,value)) NotifyAll(); } }
    public CatalogVaccine? Selected { get=>_selected; set { if(Set(ref _selected,value)) { if(value is not null) { Name=value.Name; VaccineType=value.VaccineType; InjectionType=value.InjectionType; Manufacturer=value.Manufacturer; Origin=value.Origin; Dose=value.Dose; Volume=value.Volume.ToString(CultureInfo.InvariantCulture); Reaction=value.Reactions; Price=value.Price.ToString(CultureInfo.InvariantCulture); Status=value.Status; SelectedDiseases.Clear(); foreach(var d in value.Diseases) SelectedDiseases.Add(d); } NotifyAll(); } } }
    public string VaccineType {get=>_vaccineType;set{if(Set(ref _vaccineType,value))Refresh();}} public string InjectionType {get=>_injectionType;set{if(Set(ref _injectionType,value))Refresh();}} public string Name {get=>_name;set{if(Set(ref _name,value)) Refresh();}} public string Manufacturer {get=>_manufacturer;set{if(Set(ref _manufacturer,value)) Refresh();}} public string Origin {get=>_origin;set{if(Set(ref _origin,value)) Refresh();}} public string Dose {get=>_dose;set{if(Set(ref _dose,value)) Refresh();}} public string Volume {get=>_volume;set{if(Set(ref _volume,value)) Refresh();}} public string Reaction {get=>_reaction;set{if(Set(ref _reaction,value)) Refresh();}} public string Price {get=>_price;set{if(Set(ref _price,value)) Refresh();}} public string Status {get=>_status;set{if(Set(ref _status,value)) Refresh();}}
    public string Feedback {get=>_feedback;private set=>Set(ref _feedback,value);} public string DataSourceLabel => _service.UseSampleData ? "Dữ liệu mẫu" : "Chưa có nguồn dữ liệu";
    // TODO: Bind to the authenticated permission set when the shell session exposes it.
    public bool CanEditChuyenMon => true; public bool CanEditGia => true;
    public bool ShowVaccineTab => Tab == "Vắc xin"; public bool ShowPackageTab => Tab == "Gói vắc xin"; public bool ShowLookupTab => !ShowVaccineTab && !ShowPackageTab; public bool HasNoData => _service.UseSampleData && (ShowPackageTab ? Packages.Count==0 : Vaccines.Count==0);
    public bool CanSave => !_saving && !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Manufacturer) && !string.IsNullOrWhiteSpace(Origin) && !string.IsNullOrWhiteSpace(Dose) && !string.IsNullOrWhiteSpace(Reaction) && !string.IsNullOrWhiteSpace(VaccineType) && !string.IsNullOrWhiteSpace(InjectionType) && decimal.TryParse(Volume, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) && v>0 && decimal.TryParse(Price, NumberStyles.Number, CultureInfo.InvariantCulture, out var p) && p>=0 && SelectedDiseases.Count>0;
    public string Validation => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Manufacturer) && !string.IsNullOrWhiteSpace(Origin) && !string.IsNullOrWhiteSpace(Dose) && !string.IsNullOrWhiteSpace(Reaction) && !string.IsNullOrWhiteSpace(VaccineType) && !string.IsNullOrWhiteSpace(InjectionType) && decimal.TryParse(Volume, NumberStyles.Number, CultureInfo.InvariantCulture,out var v) && v<=0 ? "Thể tích phải lớn hơn 0." : !string.IsNullOrWhiteSpace(Price) && decimal.TryParse(Price, NumberStyles.Number, CultureInfo.InvariantCulture,out var p) && p<0 ? "Giá không được âm." : SelectedDiseases.Count==0 ? "Chọn ít nhất một bệnh phòng ngừa." : "";
    public void ToggleDisease(string disease) { SetDisease(disease,!SelectedDiseases.Contains(disease)); } public void SetDisease(string disease,bool selected) { if(selected&&!SelectedDiseases.Contains(disease)) SelectedDiseases.Add(disease); else if(!selected) SelectedDiseases.Remove(disease); Refresh(); }
    private void Load(){Vaccines.Clear();foreach(var x in _service.GetVaccines())Vaccines.Add(x);Packages.Clear();foreach(var x in _service.GetPackages())Packages.Add(x);NotifyAll();}
    private void Save(){ if(!CanSave)return; _saving=true; Refresh(); try { decimal.TryParse(Volume,NumberStyles.Number,CultureInfo.InvariantCulture,out var vol);decimal.TryParse(Price,NumberStyles.Number,CultureInfo.InvariantCulture,out var price);var row=new CatalogVaccine(Selected?.Id??Vaccines.Count+1,Name, VaccineType,InjectionType,Manufacturer,Origin,Dose,vol,Reaction,price,Status,SelectedDiseases.ToArray());if(Selected is null)Vaccines.Add(row);else Vaccines[Vaccines.IndexOf(Selected)]=row;Selected=row;Feedback="Đã lưu thay đổi trên dữ liệu mẫu; chưa ghi vào hệ thống."; } finally {_saving=false;Refresh();} }
    private void Clear(){Selected=null;Name=Manufacturer=Origin=Dose=Volume=Reaction=Price=VaccineType=InjectionType="";Status="Đang kinh doanh";SelectedDiseases.Clear();Feedback="";Refresh();}
    private void Refresh(){OnPropertyChanged(nameof(Validation));OnPropertyChanged(nameof(HasNoData));SaveCommand?.NotifyCanExecuteChanged();}
    private void NotifyAll(){OnPropertyChanged(nameof(ShowVaccineTab));OnPropertyChanged(nameof(ShowPackageTab));OnPropertyChanged(nameof(ShowLookupTab));OnPropertyChanged(nameof(HasNoData));OnPropertyChanged(nameof(DataSourceLabel));Refresh();}
}
