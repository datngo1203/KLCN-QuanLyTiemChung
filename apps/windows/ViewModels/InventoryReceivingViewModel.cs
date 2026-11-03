using System.Collections.ObjectModel;
using TiemChung.Contracts;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class InventoryReceivingViewModel : ViewModelBase
{
    private readonly IdentityApiService _identity;
    private readonly IInventoryReceivingService _service;
    private readonly int _revision;
    private bool _busy,_denied,_checked,_confirmed201,_reconciled,_conflict,_more;
    private int _page=1;
    private string _lot="",_quantity="",_supplier="",_document="",_location="",_search="",_feedback="";
    private DateTime? _manufactured,_expires;
    private InventoryVaccineDto? _vaccine;
    private ReceiveInventoryRequest? _confirmation,_pending;
    private InventoryReceiptDto? _result;
    public InventoryReceivingViewModel(IdentityApiService identity,IInventoryReceivingService? service=null)
    {
        _identity=identity;_revision=identity.SessionRevision;_service=service??new InventoryReceivingService(identity);
        identity.SessionCleared+=(_,_)=>{Reset();Feedback="Phiên đã thay đổi. Đăng nhập và mở lại kho.";Refresh();};
        FindCommand=new(async _=>await LoadVaccinesAsync(),_=>CanEdit);
        NextCommand=new(async _=>await LoadVaccinesAsync(_page+1),_=>CanEdit&&_more);
        PreviousCommand=new(async _=>await LoadVaccinesAsync(_page-1),_=>CanEdit&&_page>1);
        ReconcileCommand=new(async _=>await ReconcileAsync(),_=>HasAccess&&!_busy&&_pending is not null);
        RetryCommand=new(async _=>await RetryAsync(),_=>CanRetry);
    }
    private bool Current=>_identity.IsSignedIn&&_identity.SessionRevision==_revision;
    public bool HasAccess=>Current&&_identity.CanReceiveInventory&&!_denied;
    public bool NoAccess=>!HasAccess;
    public bool IsBusy=>_busy;
    public bool CanEdit=>HasAccess&&!_busy&&_pending is null&&_confirmation is null&&_result is null;
    public bool CanRetry=>HasAccess&&!_busy&&_pending is not null&&_reconciled&&!_confirmed201&&!_conflict;
    public bool CanSave=>CanEdit&&Draft is not null;
    public bool CanStartNew=>HasAccess&&!_busy;
    public bool NoVaccines=>CanEdit&&Vaccines.Count==0;
    public ObservableCollection<InventoryVaccineDto> Vaccines {get;}=[];
    public RelayCommand FindCommand {get;}
    public RelayCommand NextCommand {get;}
    public RelayCommand PreviousCommand {get;}
    public RelayCommand ReconcileCommand {get;}
    public RelayCommand RetryCommand {get;}
    public string Feedback {get=>_feedback;private set=>Set(ref _feedback,value);}
    public string SearchText {get=>_search;set{if(CanEdit)Set(ref _search,value);}}
    public string LotNumber {get=>_lot;set{if(CanEdit&&Set(ref _lot,value))Refresh();}}
    public string Quantity {get=>_quantity;set{if(CanEdit&&Set(ref _quantity,value))Refresh();}}
    public string SupplierName {get=>_supplier;set{if(CanEdit&&Set(ref _supplier,value))Refresh();}}
    public string DocumentNumber {get=>_document;set{if(CanEdit&&Set(ref _document,value))Refresh();}}
    public string StorageLocation {get=>_location;set{if(CanEdit&&Set(ref _location,value))Refresh();}}
    public DateTime? ManufacturedOn {get=>_manufactured;set{if(CanEdit&&Set(ref _manufactured,value))Refresh();}}
    public DateTime? ExpiresOn {get=>_expires;set{if(CanEdit&&Set(ref _expires,value))Refresh();}}
    public InventoryVaccineDto? Vaccine {get=>_vaccine;set{if(CanEdit&&Set(ref _vaccine,value))Refresh();}}
    public bool InspectionConfirmed {get=>_checked;set{if(CanEdit&&Set(ref _checked,value))Refresh();}}
    public InventoryReceiptDto? Result=>_result;
    public bool HasResult=>_result is not null;
    public string ResultLabel=>_result is {} r?$"Đã đọc lại SQL: lô {r.Received.LotNumber} · {r.VaccineName} · nhập {r.Received.Quantity} · biến động #{r.MovementId}\nNhà cung cấp: {r.Received.SupplierName} · Chứng từ: {r.Received.DocumentNumber}\nBảo quản: {r.Received.StorageLocation}\nNgười nhập: {r.EmployeeName} · {r.RecordedAt.ToLocalTime():dd/MM/yyyy HH:mm:ss}":"";
    private static bool Text(string value,int max)=>!string.IsNullOrWhiteSpace(value)&&value.Trim().Length<=max;
    private ReceiveInventoryRequest? Draft=>Text(_lot,80)&&Text(_supplier,200)&&Text(_document,100)&&Text(_location,200)&&
        _vaccine is {Id:>0}&&_manufactured is {} m&&_expires is {} e&&m.Date<=e.Date&&int.TryParse(_quantity,out var q)&&q>0&&_checked
        ?new(_lot.Trim(),_vaccine.Id,DateOnly.FromDateTime(m),DateOnly.FromDateTime(e),q,_supplier.Trim(),_document.Trim(),_location.Trim(),true):null;
    public string Validation=>Draft is null?"Chọn vắc xin; nhập đủ số lô, NSX ≤ HSD, số lượng nguyên dương, nhà cung cấp, chứng từ, vị trí và xác nhận kiểm tra. API kiểm ngày theo UTC+7.":"";
    private void Refresh()
    {
        foreach(var name in new[]{nameof(HasAccess),nameof(NoAccess),nameof(IsBusy),nameof(CanEdit),nameof(CanSave),nameof(CanRetry),nameof(CanStartNew),nameof(NoVaccines),nameof(Validation),nameof(Result),nameof(HasResult),nameof(ResultLabel)})OnPropertyChanged(name);
        foreach(var cmd in new[]{FindCommand,NextCommand,PreviousCommand,ReconcileCommand,RetryCommand})cmd?.NotifyCanExecuteChanged();
    }
    private void Reset()
    {
        _lot=_quantity=_supplier=_document=_location=_search="";_manufactured=_expires=null;_checked=false;_vaccine=null;
        _pending=_confirmation=null;_result=null;_confirmed201=_reconciled=_conflict=_more=false;_page=1;Vaccines.Clear();
        foreach(var name in new[]{nameof(LotNumber),nameof(Quantity),nameof(SupplierName),nameof(DocumentNumber),nameof(StorageLocation),nameof(SearchText),nameof(ManufacturedOn),nameof(ExpiresOn),nameof(InspectionConfirmed),nameof(Vaccine)})OnPropertyChanged(name);
        Refresh();
    }
    public async Task StartNewAsync(){if(!CanStartNew)return;Reset();Feedback="Bản nháp mới. Chưa có dữ liệu ghi SQL.";await LoadVaccinesAsync();}
    private void Error(Exception error)
    {
        if(!Current)return;
        if(error is InventoryApiException{Code:"invalidSession" or "permissionDenied"}){_denied=true;Reset();}
        Feedback=error is InventoryApiException?error.Message:"Không kết nối hoặc đọc được API. Bản nháp được giữ; hãy thử lại.";
    }
    public async Task LoadVaccinesAsync(int page=1)
    {
        if(!CanEdit||page<1)return;if(_search.Trim().Length>100){Feedback="Tìm tối đa 100 ký tự.";return;}
        _busy=true;Feedback="Đang tìm vắc xin Active từ SQL…";Refresh();
        try{var result=await _service.VaccinesAsync(_search,page);if(!Current)return;_vaccine=null;OnPropertyChanged(nameof(Vaccine));Vaccines.Clear();foreach(var v in result.Items)Vaccines.Add(v);_more=result.HasMore;_page=page;Feedback=result.Items.Length==0?"Không có vắc xin Active phù hợp. Đổi bộ lọc hoặc thử lại.":"Chọn vắc xin từ danh sách SQL; điền thông tin theo chứng từ thực tế.";}
        catch(Exception e){Error(e);}finally{_busy=false;Refresh();}
    }
    public string? BeginConfirmation()
    {
        if(!CanSave)return null;_confirmation=Draft;Refresh();var r=_confirmation!;
        return $"Vắc xin: {Vaccine!.Name}\nLô: {r.LotNumber}\nNSX: {r.ManufacturedOn:dd/MM/yyyy} · HSD: {r.ExpiresOn:dd/MM/yyyy}\nSố lượng: {r.Quantity}\nNhà cung cấp: {r.SupplierName}\nChứng từ: {r.DocumentNumber}\nVị trí: {r.StorageLocation}\n\nĐã kiểm hàng, chứng từ, tình trạng và bảo quản. Xác nhận nhập một lô mới? Sau lưu chỉ đọc, không sửa/bù tồn tại đây.";
    }
    public async Task ConfirmAsync(bool accepted)
    {
        var request=_confirmation;_confirmation=null;
        if(!accepted||request is null||!HasAccess){Refresh();return;}
        _pending=request;_confirmed201=_conflict=_reconciled=false;await SendPending();
    }
    private async Task SendPending()
    {
        _busy=true;_reconciled=false;Feedback="Đang ghi lô, biến động và audit…";Refresh();
        try{await _service.ReceiveAsync(_pending!);if(!Current)return;_confirmed201=true;}
        catch(InventoryApiException e) when(e.Code is "outcomeUnknown" or "recordedUnreadable" or "inventoryLotExists") {
            if(Current){_confirmed201=e.Code=="recordedUnreadable";_conflict=e.Code=="inventoryLotExists";Feedback=e.Message;}
        }
        catch(InventoryApiException e){if(Current){_pending=null;Error(e);}}
        catch(Exception){if(Current)Feedback="Chưa xác định kết quả gửi. Giữ yêu cầu và GET đối chiếu trước khi thử lại.";}
        finally{_busy=false;Refresh();}
        if(Current&&_confirmed201)await ReconcileAsync();
    }
    public async Task ReconcileAsync()
    {
        if(!HasAccess||_busy||_pending is null)return;var request=_pending;_busy=true;_reconciled=false;Feedback="Đang GET đối chiếu SQL…";Refresh();
        try{var result=await _service.ReceiptAsync(request.LotNumber);if(!Current)return;
            if(result.Received!=request){_conflict=true;Feedback="Lô có thông tin tiếp nhận khác yêu cầu này. Không ghi đè/nhập lại; kiểm tra chứng từ.";}
            else{_result=result;_pending=null;Feedback="Nhập thành công; đã GET đọc lại SQL. Dùng Tải lại ở A05 để xem tồn và biến động mới.";}
        }
        catch(InventoryApiException e) when(e.Code=="resourceNotFound") {if(Current){_reconciled=true;Feedback=_confirmed201?"Đã nhận 201. Chỉ tiếp tục GET, không POST lại.":_conflict?"Số lô đã tồn tại nhưng chưa có receipt X2.1 (lô cũ/seed). Không được nhập bổ sung.":"GET chưa thấy receipt. Có thể thử lại đúng yêu cầu cũ; API chặn nhập trùng.";}}
        catch(Exception e){Error(e);}finally{_busy=false;Refresh();}
    }
    public async Task RetryAsync(){if(CanRetry)await SendPending();}
}
