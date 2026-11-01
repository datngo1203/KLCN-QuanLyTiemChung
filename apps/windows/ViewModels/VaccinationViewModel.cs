using System.Collections.ObjectModel;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class VaccinationViewModel : ViewModelBase
{
    private readonly IVaccinationService _service;
    private readonly IdentityApiService _identity;
    private readonly int _revision;
    private bool _busy,_denied,_confirming,_committed,_reconciled,_loadError;
    private int _page=1;
    private string _search="",_feedback="",_site="",_lookup="";
    private VaccinationIndication? _selected;
    private VaccinationLot? _lot;
    private VaccinationDetail? _detail;
    private VaccinationRecord? _result;
    private VaccinationRequest? _pending,_confirmation;
    public VaccinationViewModel(IVaccinationService? service=null,IdentityApiService? identity=null)
    {
        _identity=identity??IdentityApiService.Current;_revision=_identity.SessionRevision;
        _service=service??new VaccinationApiService(_identity);
        _identity.SessionCleared+=(_,_)=>ClearSessionData();
        ReloadCommand=new(async _=>await LoadAsync(1),_=>CanBrowse);
        PreviousCommand=new(async _=>await LoadAsync(Page-1),_=>CanBrowse && Page>1);
        NextCommand=new(async _=>await LoadAsync(Page+1),_=>CanBrowse && Queue.Count==VaccinationApiService.PageSize);
        DetailCommand=new(async _=>await LoadDetailAsync(),_=>CanBrowse && Selected is not null);
        ResetDraftCommand=new(_=>{ClearDraft();Refresh();},_=>CanEdit && IsDirty);
        ReconcileCommand=new(async _=>await ReconcileAsync(),_=>HasAccess && !_busy && _pending is not null);
        RetryCommand=new(async _=>await RetryAsync(),_=>HasAccess && !_busy && _pending is not null && _reconciled && !_committed);
        ReadResultCommand=new(async _=>await ReadResultAsync(),_=>CanReadResult);
    }
    public ObservableCollection<VaccinationIndication> Queue {get;}=[];
    public ObservableCollection<VaccinationLot> Lots {get;}=[];
    public RelayCommand ReloadCommand{get;}
    public RelayCommand PreviousCommand{get;}
    public RelayCommand NextCommand{get;}
    public RelayCommand DetailCommand{get;}
    public RelayCommand ResetDraftCommand{get;}
    public RelayCommand ReconcileCommand{get;}
    public RelayCommand RetryCommand{get;}
    public RelayCommand ReadResultCommand{get;}
    private bool IsCurrent=>_identity.IsSignedIn && _identity.SessionRevision==_revision;
    public bool HasAccess=>IsCurrent && _identity.CanUseVaccinations && !_denied;
    public bool NoAccess=>!HasAccess;
    public bool IsBusy=>_busy;
    public bool HasPending=>HasAccess && _pending is not null;
    public bool IsDirty=>_lot is not null || _site.Length>0;
    public bool CanBrowse=>HasAccess && !_busy && !_confirming && _pending is null && !IsDirty;
    public bool CanEdit=>HasAccess && !_busy && !_confirming && _pending is null && Detail is not null && Result is null;
    public bool IsDraft=>HasAccess && Detail is not null && Result is null;
    public bool IsRecorded=>HasAccess && Result is not null;
    public bool NoDetail=>HasAccess && Detail is null && Result is null;
    public bool HasDetail=>HasAccess && (Detail is not null || Result is not null);
    public bool NoResults=>HasAccess && !_busy && !_loadError && Queue.Count==0;
    public bool NoLots=>IsDraft && Lots.Count==0;
    public bool CanSave=>CanEdit && SelectedLot is not null && Lots.Contains(SelectedLot) && !string.IsNullOrWhiteSpace(InjectionSite) && InjectionSite.Trim().Length<=100;
    public bool CanReadResult=>CanBrowse && LookupId>0;
    public int Page=>_page;
    public string Feedback{get=>_feedback;private set=>Set(ref _feedback,value);}
    public string SearchText{get=>_search;set{if(CanBrowse)Set(ref _search,value);}}
    public string ResultIndicationId{get=>_lookup;set{if(CanBrowse && Set(ref _lookup,value))Refresh();}}
    private int LookupId=>int.TryParse(_lookup.Trim().Replace("CD-","",StringComparison.OrdinalIgnoreCase),out var id)?id:0;
    public VaccinationIndication? Selected{get=>_selected;set{if(CanBrowse && Set(ref _selected,value)){ClearDetail();Refresh();}}}
    public VaccinationLot? SelectedLot{get=>_lot;set{if(CanEdit && Set(ref _lot,value))Refresh();}}
    public string InjectionSite{get=>_site;set{if(CanEdit && Set(ref _site,value))Refresh();}}
    public VaccinationDetail? Detail=>_detail;
    public VaccinationRecord? Result=>_result;
    private void ClearDraft()
    {
        _lot=null;_site="";OnPropertyChanged(nameof(SelectedLot));OnPropertyChanged(nameof(InjectionSite));
    }
    private void ClearDetail()
    {
        _detail=null;_result=null;ClearDraft();Lots.Clear();OnPropertyChanged(nameof(Detail));OnPropertyChanged(nameof(Result));
    }
    private void ClearSessionData()
    {
        _confirmation=null;_pending=null;_confirming=false;_committed=false;_reconciled=false;
        _search="";_lookup="";_selected=null;Queue.Clear();ClearDetail();
        OnPropertyChanged(nameof(SearchText));OnPropertyChanged(nameof(ResultIndicationId));OnPropertyChanged(nameof(Selected));
        Feedback="Phiên đã thay đổi. Đăng nhập và mở lại màn ghi mũi.";Refresh();
    }
    private void Error(Exception error)
    {
        if(!IsCurrent)return;
        _loadError=true;
        if(error is VaccinationApiException{Code:"invalidSession" or "permissionDenied"}){_denied=true;ClearSessionData();}
        Feedback=error is VaccinationApiException?error.Message:"Không kết nối hoặc đọc được API. Bản nháp được giữ lại; hãy thử lại.";
    }
    private void Refresh()
    {
        foreach(var name in new[]{nameof(HasAccess),nameof(NoAccess),nameof(IsBusy),nameof(HasPending),nameof(IsDirty),nameof(CanBrowse),nameof(CanEdit),nameof(IsDraft),nameof(IsRecorded),nameof(NoDetail),nameof(HasDetail),nameof(NoResults),nameof(NoLots),nameof(CanSave),nameof(CanReadResult),nameof(Page)})OnPropertyChanged(name);
        foreach(var command in new[]{ReloadCommand,PreviousCommand,NextCommand,DetailCommand,ResetDraftCommand,ReconcileCommand,RetryCommand,ReadResultCommand})command?.NotifyCanExecuteChanged();
    }
    public async Task LoadAsync(int page=1)
    {
        if(!CanBrowse || page<1)return;
        if(SearchText.Trim().Length>100){Feedback="Tìm kiếm tối đa 100 ký tự.";return;}
        _busy=true;_loadError=false;_selected=null;OnPropertyChanged(nameof(Selected));ClearDetail();Queue.Clear();
        Feedback="Đang tải chỉ định từ API / SQL…";Refresh();
        try{var rows=await _service.ListAsync(page,SearchText);if(!IsCurrent || _denied)return;
            foreach(var row in rows)Queue.Add(row);_page=page;Feedback=rows.Length==0?"Không có chỉ định chưa ghi mũi phù hợp.":"Chọn chỉ định để đối chiếu người tiêm, thanh toán và lô. Danh sách chưa xác nhận đủ mọi điều kiện ghi.";
        }catch(Exception e){Error(e);}finally{_busy=false;Refresh();}
    }
    public async Task LoadDetailAsync()
    {
        if(!CanBrowse || Selected is not {} selected)return;
        _busy=true;_loadError=false;ClearDetail();Feedback="Đang GET đối chiếu chỉ định và mũi đã ghi…";Refresh();
        try{
            var result=await _service.GetResultAsync(selected.Id);if(!IsCurrent || _denied)return;
            if(result is not null){Accept(result);return;}
            var detail=await _service.GetAsync(selected.Id);if(!IsCurrent || _denied)return;
            _detail=detail;foreach(var lot in detail.Lots)Lots.Add(lot);OnPropertyChanged(nameof(Detail));
            Feedback=Lots.Count==0?"Không có lô còn hạn, còn tồn và phù hợp từ SQL. Chưa thể ghi mũi.":"Chọn lô và nhập vị trí thực tế. Chỉ xác nhận sau khi đã thực hiện tiêm theo chỉ định bác sĩ.";
        }catch(Exception e){Error(e);}finally{_busy=false;Refresh();}
    }
    public string? BeginConfirmation()
    {
        if(!CanSave)return null;
        _confirmation=new(Detail!.Indication.Id,SelectedLot!.Number,InjectionSite.Trim());_confirming=true;Refresh();
        return $"{Detail.Indication.Label}\n{Detail.Indication.SourceLabel}\nVắc xin: {Detail.Indication.Vaccine.Name}\nLô: {_confirmation.LotNumber}\nVị trí: {_confirmation.InjectionSite}\n\nXác nhận đã thực hiện mũi tiêm này? Ghi nhận sẽ trừ 1 tồn SQL. Sau khi lưu chỉ xem, không sửa/xóa. Chưa phải hoàn tất theo dõi sau tiêm.";
    }
    public async Task ConfirmAsync(bool confirmed)
    {
        var request=_confirmation;_confirmation=null;_confirming=false;
        if(!confirmed || request is null || !IsCurrent || _denied || Detail is null){Refresh();return;}
        _pending=request;_committed=false;_reconciled=false;await SendPendingAsync();
    }
    private void Accept(VaccinationRecord result)
    {
        _result=result;_pending=null;_committed=false;_reconciled=false;ClearDraft();Lots.Clear();
        _lookup=result.IndicationId.ToString();OnPropertyChanged(nameof(ResultIndicationId));OnPropertyChanged(nameof(Result));
        Feedback="Đã đọc mũi ghi nhận từ API / SQL. Chỉ đọc; chưa ghi theo dõi sau tiêm hoặc xác nhận cho về.";
    }
    private async Task SendPendingAsync()
    {
        _busy=true;_reconciled=false;Feedback="Đang ghi mũi, trừ tồn và ghi biến động…";Refresh();
        try{var result=await _service.RecordAsync(_pending!);if(IsCurrent && !_denied)Accept(result);}
        catch(VaccinationApiException e) when(e.Code is "outcomeUnknown" or "recordedUnreadable" or "vaccinationAlreadyRecorded") {
            if(IsCurrent){_committed=e.Code=="recordedUnreadable";Feedback=e.Message;}
        }
        catch(Exception e){if(IsCurrent){if(e is VaccinationApiException)_pending=null;Error(e);}}
        finally{_busy=false;Refresh();}
    }
    public async Task ReconcileAsync()
    {
        if(!HasAccess || _busy || _pending is null)return;
        _busy=true;_reconciled=false;Feedback="Đang GET đối chiếu mũi đã ghi…";Refresh();
        try{var result=await _service.GetResultAsync(_pending.IndicationId);if(!IsCurrent || _denied)return;
            if(result is not null)Accept(result);
            else{_reconciled=true;Feedback=_committed?"API đã trả 201. Tiếp tục GET, không POST lại.":"GET chưa thấy mũi. Có thể thử lại đúng yêu cầu cũ; API chặn ghi trùng và trừ tồn hai lần.";}
        }catch(Exception e){Error(e);}finally{_busy=false;Refresh();}
    }
    public async Task RetryAsync()
    {
        if(HasAccess && !_busy && _pending is not null && _reconciled && !_committed)await SendPendingAsync();
    }
    public async Task ReadResultAsync()
    {
        if(!CanBrowse || LookupId<=0)return;
        var id=LookupId;_busy=true;_loadError=false;_selected=null;OnPropertyChanged(nameof(Selected));ClearDetail();
        Feedback="Đang đọc lại mũi theo mã chỉ định…";Refresh();
        try{var result=await _service.GetResultAsync(id);if(!IsCurrent || _denied)return;
            if(result is not null)Accept(result);else Feedback="Chỉ định chưa có mũi ghi nhận. Tìm trong danh sách chỉ định để tiếp tục.";
        }catch(Exception e){Error(e);}finally{_busy=false;Refresh();}
    }
}
