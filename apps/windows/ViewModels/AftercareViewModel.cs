using System.Collections.ObjectModel;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class AftercareViewModel:ViewModelBase
{
    private readonly IAftercareService _service;
    private readonly IdentityApiService _identity;
    private readonly int _revision;
    private bool _busy,_denied,_confirming,_committed,_reconciled,_loadError;
    private int _page=1;
    private string _search="",_lookup="",_condition="",_severity="",_treatment="",_feedback="";
    private FollowUpPendingItem? _selected;
    private FollowUpDetail? _detail;
    private FollowUpRequest? _confirmation,_pending;
    private int _pendingId;
    public AftercareViewModel(IAftercareService? service=null,IdentityApiService? identity=null)
    {
        _identity=identity??IdentityApiService.Current;_revision=_identity.SessionRevision;_service=service??new AftercareApiService(_identity);
        _identity.SessionCleared+=(_,_)=>ClearSessionData();
        ReloadCommand=new(async _=>await LoadAsync(1),_=>CanBrowse);
        PreviousCommand=new(async _=>await LoadAsync(Page-1),_=>CanBrowse&&Page>1);
        NextCommand=new(async _=>await LoadAsync(Page+1),_=>CanBrowse&&Queue.Count==AftercareApiService.PageSize);
        DetailCommand=new(async _=>await RefreshDetailAsync(),_=>CanRefreshDetail);
        ResetDraftCommand=new(_=>{ClearDraft();Refresh();},_=>CanEdit&&IsDirty);
        ReconcileCommand=new(async _=>await ReconcileAsync(),_=>HasAccess&&!_busy&&_pending is not null);
        RetryCommand=new(async _=>await RetryAsync(),_=>HasAccess&&!_busy&&_pending is not null&&_reconciled&&!_committed&&Detail?.Observation is{CanComplete:true,BlockReason:null});
    }
    public ObservableCollection<FollowUpPendingItem> Queue{get;}=[];
    public RelayCommand ReloadCommand{get;}
    public RelayCommand PreviousCommand{get;}
    public RelayCommand NextCommand{get;}
    public RelayCommand DetailCommand{get;}
    public RelayCommand ResetDraftCommand{get;}
    public RelayCommand ReconcileCommand{get;}
    public RelayCommand RetryCommand{get;}
    private bool IsCurrent=>_identity.IsSignedIn&&_identity.SessionRevision==_revision;
    public bool HasAccess=>IsCurrent&&_identity.CanUseAftercare&&!_denied;
    public bool NoAccess=>!HasAccess;
    public bool IsBusy=>_busy;
    public bool HasPending=>HasAccess&&_pending is not null;
    public bool IsDirty=>_condition.Length>0||_severity.Length>0||_treatment.Length>0;
    public bool CanBrowse=>HasAccess&&!_busy&&!_confirming&&_pending is null&&!IsDirty;
    public bool CanEdit=>HasAccess&&!_busy&&!_confirming&&_pending is null&&Detail is{FollowUp:null};
    public bool HasDetail=>HasAccess&&Detail is not null;
    public bool NoDetail=>HasAccess&&Detail is null;
    public bool IsDraft=>HasDetail&&Result is null;
    public bool IsRecorded=>HasDetail&&Result is not null;
    public bool NoResults=>HasAccess&&!_busy&&!_loadError&&Queue.Count==0;
    private static bool Input(string value,int max)=>!string.IsNullOrWhiteSpace(value)&&value.Trim().Length<=max;
    public bool CanSave=>CanEdit&&Detail?.Observation is{CanComplete:true,BlockReason:null}&&Input(Condition,100)&&Input(Severity,100)&&Input(Treatment,500);
    public bool CanRefreshDetail=>HasAccess&&!_busy&&!_confirming&&_pending is null&&(Detail is not null||Selected is not null);
    private int LookupId=>int.TryParse(_lookup.Trim().Replace("MT-","",StringComparison.OrdinalIgnoreCase),out var id)?id:0;
    public bool CanReadResult=>CanBrowse&&LookupId>0;
    public int Page=>_page;
    public string Feedback{get=>_feedback;private set=>Set(ref _feedback,value);}
    public string SearchText{get=>_search;set{if(CanBrowse)Set(ref _search,value);}}
    public string ResultVaccinationId{get=>_lookup;set{if(CanBrowse&&Set(ref _lookup,value))Refresh();}}
    public string Condition{get=>_condition;set{if(CanEdit&&Set(ref _condition,value))Refresh();}}
    public string Severity{get=>_severity;set{if(CanEdit&&Set(ref _severity,value))Refresh();}}
    public string Treatment{get=>_treatment;set{if(CanEdit&&Set(ref _treatment,value))Refresh();}}
    public FollowUpPendingItem? Selected{get=>_selected;set{if(CanBrowse&&Set(ref _selected,value)){ClearDetail();Refresh();}}}
    public FollowUpDetail? Detail=>_detail;
    public FollowUpRecord? Result=>_detail?.FollowUp;
    private void ClearDraft(){_condition="";_severity="";_treatment="";OnPropertyChanged(nameof(Condition));OnPropertyChanged(nameof(Severity));OnPropertyChanged(nameof(Treatment));}
    private void ClearDetail(){_detail=null;ClearDraft();OnPropertyChanged(nameof(Detail));OnPropertyChanged(nameof(Result));}
    private void ClearSessionData()
    {
        _confirmation=null;_pending=null;_pendingId=0;_confirming=false;_committed=false;_reconciled=false;
        _selected=null;_search="";_lookup="";Queue.Clear();ClearDetail();OnPropertyChanged(nameof(Selected));OnPropertyChanged(nameof(SearchText));OnPropertyChanged(nameof(ResultVaccinationId));
        Feedback="Phiên đã thay đổi. Đăng nhập và mở lại khu vực theo dõi.";Refresh();
    }
    private void Error(Exception error)
    {
        if(!IsCurrent)return;_loadError=true;
        if(error is AftercareApiException{Code:"invalidSession" or "permissionDenied"}){_denied=true;ClearSessionData();}
        Feedback=error is AftercareApiException?error.Message:"Không kết nối hoặc đọc được dữ liệu. Bản nháp được giữ lại; hãy thử lại.";
    }
    private void Refresh()
    {
        foreach(var name in new[]{nameof(HasAccess),nameof(NoAccess),nameof(IsBusy),nameof(HasPending),nameof(IsDirty),nameof(CanBrowse),nameof(CanEdit),nameof(HasDetail),nameof(NoDetail),nameof(IsDraft),nameof(IsRecorded),nameof(NoResults),nameof(CanSave),nameof(CanRefreshDetail),nameof(CanReadResult),nameof(Page)})OnPropertyChanged(name);
        foreach(var command in new[]{ReloadCommand,PreviousCommand,NextCommand,DetailCommand,ResetDraftCommand,ReconcileCommand,RetryCommand})command?.NotifyCanExecuteChanged();
    }
    private void Accept(FollowUpDetail detail)
    {
        _detail=detail;OnPropertyChanged(nameof(Detail));OnPropertyChanged(nameof(Result));
        var queued=Queue.FirstOrDefault(x=>x.Vaccination.Id==detail.Vaccination.Id);
        if(queued is not null){
            if(detail.FollowUp is not null){Queue.Remove(queued);_selected=null;OnPropertyChanged(nameof(Selected));}
            else if(detail.Observation is{} observation){
                var updated=new FollowUpPendingItem(detail.Vaccination,observation);Queue[Queue.IndexOf(queued)]=updated;
                if(_selected?.Vaccination.Id==detail.Vaccination.Id){_selected=updated;OnPropertyChanged(nameof(Selected));}
            }
        }
        if(detail.FollowUp is not null){_pending=null;_pendingId=0;_committed=false;_reconciled=false;ClearDraft();_lookup=detail.Vaccination.Id.ToString();OnPropertyChanged(nameof(ResultVaccinationId));}
        Feedback=detail.FollowUp is null?"Nhập đủ ba nội dung thực tế. Chưa có bản ghi theo dõi cho mũi này.":"Đã đọc kết quả đã lưu. Chỉ đọc; việc lưu không phải quyết định cho về hoặc xác nhận an toàn.";
    }
    public async Task LoadAsync(int page=1)
    {
        if(!CanBrowse||page<1)return;if(SearchText.Trim().Length>100){Feedback="Tìm kiếm tối đa 100 ký tự.";return;}
        _busy=true;_loadError=false;_selected=null;OnPropertyChanged(nameof(Selected));ClearDetail();Queue.Clear();Feedback="Đang tải mũi chưa có theo dõi …";Refresh();
        try{var rows=await _service.ListAsync(page,SearchText);if(!IsCurrent||_denied)return;foreach(var row in rows)Queue.Add(row);_page=page;
            Feedback=rows.Length==0?"Không có mũi chưa có bản ghi theo dõi phù hợp.":"Chọn mũi để đối chiếu trước khi ghi theo dõi. Danh sách không xác nhận người tiêm còn ở cơ sở.";
        }catch(Exception e){Error(e);}finally{_busy=false;Refresh();}
    }
    public async Task LoadDetailAsync(){if(CanBrowse&&Selected is{} shot)await ReadAsync(shot.Vaccination.Id);}
    public async Task RefreshDetailAsync()
    {
        if(!CanRefreshDetail)return;
        var id=Detail?.Vaccination.Id??Selected!.Vaccination.Id;_busy=true;Refresh();
        try{var detail=await _service.GetAsync(id);if(IsCurrent&&!_denied)Accept(detail);}
        catch(Exception e){if(IsCurrent&&Detail is{} old){_detail=old with{Observation=null};OnPropertyChanged(nameof(Detail));}Error(e);}
        finally{_busy=false;Refresh();}
    }
    public async Task ReadResultAsync(){if(CanReadResult){_selected=null;OnPropertyChanged(nameof(Selected));await ReadAsync(LookupId);}}
    private async Task ReadAsync(int id)
    {
        _busy=true;_loadError=false;ClearDetail();Feedback="Đang đọc mũi và kết quả theo dõi…";Refresh();
        try{var detail=await _service.GetAsync(id);if(IsCurrent&&!_denied)Accept(detail);}catch(Exception e){Error(e);}finally{_busy=false;Refresh();}
    }
    public string? BeginConfirmation()
    {
        if(!CanSave)return null;_confirmation=new(Condition.Trim(),Severity.Trim(),Treatment.Trim());_confirming=true;Refresh();
        return $"{Detail!.Vaccination.Label}\nNgười tiêm: {Detail.Vaccination.Recipient.FullName}\nVắc xin: {Detail.Vaccination.Vaccine.Name}\nLô: {Detail.Vaccination.LotNumber}\n\nTình trạng: {_confirmation.Condition}\nMức độ: {_confirmation.Severity}\nXử trí thực tế: {_confirmation.Treatment}\n\nXác nhận lưu đúng nội dung đã ghi nhận? Sau lưu chỉ xem, không sửa/xóa. Không phải quyết định cho về hoặc xác nhận an toàn.";
    }
    public async Task ConfirmAsync(bool confirmed)
    {
        var request=_confirmation;_confirmation=null;_confirming=false;
        if(!confirmed||request is null||!IsCurrent||_denied||Detail is null){Refresh();return;}
        _pendingId=Detail.Vaccination.Id;_pending=request;_committed=false;_reconciled=false;await SendPendingAsync();
    }
    private async Task SendPendingAsync()
    {
        _busy=true;_reconciled=false;Feedback="Đang lưu theo dõi…";Refresh();
        try{var result=await _service.RecordAsync(_pendingId,_pending!);if(IsCurrent&&!_denied)Accept(new(Detail!.Vaccination,result,Detail.Observation));}
        catch(AftercareApiException e)when(e.Code is "outcomeUnknown" or "recordedUnreadable" or "followUpAlreadyRecorded"){
            if(IsCurrent){_committed=e.Code=="recordedUnreadable";Feedback=e.Message;}
        }catch(Exception e){if(IsCurrent){if(e is AftercareApiException)_pending=null;
            if(e is AftercareApiException{Code:"observationPeriodNotCompleted" or "followUpSourceConflict"}&&Detail is{} old){_detail=old with{Observation=null};OnPropertyChanged(nameof(Detail));}Error(e);}}
        finally{_busy=false;Refresh();}
    }
    public async Task ReconcileAsync()
    {
        if(!HasAccess||_busy||_pending is null)return;_busy=true;_reconciled=false;Feedback="Đang đối chiếu kết quả…";Refresh();
        try{var detail=await _service.GetAsync(_pendingId);if(!IsCurrent||_denied)return;
            if(detail.FollowUp is not null)Accept(detail);else{_detail=detail;OnPropertyChanged(nameof(Detail));_reconciled=true;Feedback=_committed?"Đã nhận xác nhận lưu. Tiếp tục đối chiếu, không gửi lại.":"Chưa thấy bản ghi. Khi đủ mốc theo dõi, có thể thử lại đúng nội dung cũ.";}
        }catch(Exception e){Error(e);}finally{_busy=false;Refresh();}
    }
    public async Task RetryAsync(){if(HasAccess&&!_busy&&_pending is not null&&_reconciled&&!_committed&&Detail?.Observation is{CanComplete:true,BlockReason:null})await SendPendingAsync();}
}
