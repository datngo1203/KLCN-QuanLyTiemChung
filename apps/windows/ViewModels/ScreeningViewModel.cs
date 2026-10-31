using System.Collections.ObjectModel;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class ClinicalVaccineOption(ClinicalVaccine vaccine, Func<bool> canEdit, Action changed) : ViewModelBase
{
    private bool _selected;
    public int Id => vaccine.Id;
    public string Name => vaccine.Name;
    public bool IsSelected { get => _selected; set { if (canEdit() && Set(ref _selected, value)) changed(); } }
    internal void Clear() { _selected=false; OnPropertyChanged(nameof(IsSelected)); }
}

public sealed class ScreeningViewModel : ViewModelBase
{
    private readonly IScreeningService _service;
    private readonly IdentityApiService _identity;
    private readonly int _revision;
    private bool _busy, _denied, _confirming, _committed, _reconciled, _loadError;
    private int _page=1, _pendingId;
    private string _search="", _feedback="", _conclusion="", _reason="";
    private AppointmentRecord? _selected;
    private ClinicalDetail? _detail;
    private ClinicalAssessment? _assessment;
    private AssessmentRequest? _pending, _confirmation;
    public ScreeningViewModel(IScreeningService? screeningService = null, IdentityApiService? identity = null)
    {
        _identity=identity??IdentityApiService.Current; _revision=_identity.SessionRevision;
        _service=screeningService??new ScreeningApiService(_identity);
        _identity.SessionCleared+=(_,_)=>ClearSessionData();
        ReloadCommand=new(async _=>await LoadAsync(1),_=>CanBrowse);
        PreviousCommand=new(async _=>await LoadAsync(Page-1),_=>CanBrowse && Page>1);
        NextCommand=new(async _=>await LoadAsync(Page+1),_=>CanBrowse && Appointments.Count==ScreeningApiService.PageSize);
        DetailCommand=new(async _=>await LoadDetailAsync(),_=>CanBrowse && Selected is not null);
        ResetDraftCommand=new(_=>{ClearDraft();Refresh();},_=>CanEdit && IsDirty);
        ReconcileCommand=new(async _=>await ReconcileAsync(),_=>HasAccess && !_busy && _pending is not null);
        RetryCommand=new(async _=>await RetryAsync(),_=>HasAccess && !_busy && _pending is not null && _reconciled && !_committed);
    }
    public ObservableCollection<AppointmentRecord> Appointments { get; }=[];
    public ObservableCollection<ClinicalVaccineOption> Vaccines { get; }=[];
    public IReadOnlyList<string> ConclusionOptions { get; }=["Đủ điều kiện","Tạm hoãn","Chống chỉ định"];
    public RelayCommand ReloadCommand { get; }
    public RelayCommand PreviousCommand { get; }
    public RelayCommand NextCommand { get; }
    public RelayCommand DetailCommand { get; }
    public RelayCommand ResetDraftCommand { get; }
    public RelayCommand ReconcileCommand { get; }
    public RelayCommand RetryCommand { get; }
    private bool IsCurrent=>_identity.IsSignedIn && _identity.SessionRevision==_revision;
    public bool HasAccess=>IsCurrent && _identity.CanUseClinical && !_denied;
    public bool NoAccess=>!HasAccess;
    public bool IsBusy=>_busy;
    public bool HasPending=>HasAccess && _pending is not null;
    public bool IsDirty=>_conclusion.Length>0 || _reason.Length>0 || Vaccines.Any(v=>v.IsSelected);
    public bool CanBrowse=>HasAccess && !_busy && !_confirming && _pending is null && !IsDirty;
    public bool CanEdit=>HasAccess && !_busy && !_confirming && _pending is null && Detail is not null && Assessment is null;
    public bool CanSelectVaccines=>CanEdit && Conclusion=="Đủ điều kiện";
    public bool IsDraft=>HasAccess && Detail is not null && Assessment is null;
    public bool IsRecorded=>HasAccess && Assessment is not null;
    public bool NoDetail=>HasAccess && Detail is null;
    public bool NoResults=>HasAccess && !_busy && !_loadError && Appointments.Count==0;
    public bool CanSave=>CanEdit && ConclusionOptions.Contains(Conclusion) && Reason.Trim().Length<=500 &&
        (Conclusion=="Đủ điều kiện" ? Vaccines.Any(v=>v.IsSelected) : !string.IsNullOrWhiteSpace(Reason) && !Vaccines.Any(v=>v.IsSelected));
    public int Page=>_page;
    public string Feedback { get=>_feedback; private set=>Set(ref _feedback,value); }
    public string SearchText { get=>_search; set { if(CanBrowse) Set(ref _search,value); } }
    public string? Conclusion { get=>string.IsNullOrEmpty(_conclusion)?null:_conclusion; set { if(CanEdit && Set(ref _conclusion,value??"")) { foreach(var v in Vaccines)v.Clear(); Refresh(); } } }
    public string Reason { get=>_reason; set { if(CanEdit && Set(ref _reason,value))Refresh(); } }
    public AppointmentRecord? Selected { get=>_selected; set { if(CanBrowse && Set(ref _selected,value)) {ClearDetail();Refresh();} } }
    public ClinicalDetail? Detail=>_detail;
    public ClinicalAssessment? Assessment=>_assessment;
    private void ClearDraft()
    {
        _conclusion="";_reason="";foreach(var v in Vaccines)v.Clear();
        OnPropertyChanged(nameof(Conclusion));OnPropertyChanged(nameof(Reason));
    }
    private void ClearDetail()
    {
        _detail=null;_assessment=null;ClearDraft();Vaccines.Clear();
        OnPropertyChanged(nameof(Detail));OnPropertyChanged(nameof(Assessment));
    }
    private void ClearSessionData()
    {
        _confirmation=null;_pending=null;_confirming=false;_committed=false;_reconciled=false;
        _search="";_selected=null;Appointments.Clear();ClearDetail();
        OnPropertyChanged(nameof(SearchText));OnPropertyChanged(nameof(Selected));
        Feedback="Phiên đã thay đổi. Đăng nhập và mở lại màn kết luận.";Refresh();
    }
    private void Error(Exception error)
    {
        if(!IsCurrent)return;
        _loadError=true;
        if(error is ClinicalApiException {Code:"invalidSession" or "permissionDenied"}){_denied=true;ClearSessionData();}
        Feedback=error is ClinicalApiException ? error.Message : "Không kết nối hoặc đọc được API. Bản nháp được giữ lại; hãy thử lại.";
    }
    private void Refresh()
    {
        foreach(var name in new[]{nameof(HasAccess),nameof(NoAccess),nameof(IsBusy),nameof(HasPending),nameof(IsDirty),nameof(CanBrowse),nameof(CanEdit),nameof(CanSelectVaccines),nameof(IsDraft),nameof(IsRecorded),nameof(NoDetail),nameof(NoResults),nameof(CanSave),nameof(Page)})OnPropertyChanged(name);
        foreach(var command in new[]{ReloadCommand,PreviousCommand,NextCommand,DetailCommand,ResetDraftCommand,ReconcileCommand,RetryCommand})command?.NotifyCanExecuteChanged();
    }
    public async Task LoadAsync(int page=1)
    {
        if(!CanBrowse || page<1)return;
        if(SearchText.Trim().Length>100){Feedback="Tìm kiếm tối đa 100 ký tự.";return;}
        _busy=true;_loadError=false;_selected=null;OnPropertyChanged(nameof(Selected));ClearDetail();Appointments.Clear();
        Feedback="Đang tải lịch hẹn từ API / SQL…";Refresh();
        try {var rows=await _service.ListAsync(page,SearchText);if(!IsCurrent || _denied)return;
            foreach(var row in rows)Appointments.Add(row);_page=page;Feedback=rows.Length==0?"Không có lịch hẹn phù hợp.":"Chọn lịch hẹn để xem và ghi kết luận. Chưa xác nhận người bệnh đã đến khám.";
        }catch(Exception e){Error(e);}finally{_busy=false;Refresh();}
    }
    public async Task LoadDetailAsync()
    {
        if(!CanBrowse || Selected is not {} selected)return;
        _busy=true;_loadError=false;ClearDetail();Feedback="Đang đọc người tiêm, dịch vụ và kết luận…";Refresh();
        try {
            var detail=await _service.GetAsync(selected.Id);
            if(!IsCurrent || _denied)return;
            var assessment=await _service.GetAssessmentAsync(selected.Id);
            if(!IsCurrent || _denied)return;
            _detail=detail;_assessment=assessment;
            foreach(var vaccine in detail.AllowedVaccines)Vaccines.Add(new(vaccine,()=>CanSelectVaccines,Refresh));
            OnPropertyChanged(nameof(Detail));OnPropertyChanged(nameof(Assessment));
            Feedback=assessment is null?"Bác sĩ chủ động chọn kết luận và vắc xin. Chưa có kết quả tiêm.":"Kết luận đã lưu, chỉ đọc; không sửa hoặc hủy trong chức năng này.";
        }catch(Exception e){Error(e);}finally{_busy=false;Refresh();}
    }
    public string? BeginConfirmation()
    {
        if(!CanSave)return null;
        _confirmation=new(_conclusion,string.IsNullOrWhiteSpace(Reason)?null:Reason.Trim(),Vaccines.Where(v=>v.IsSelected).Select(v=>v.Id).ToArray());
        _confirming=true;Refresh();
        return $"{Detail!.Appointment.Label}\n{Detail.Appointment.SourceLabel}\nKết luận: {Conclusion}\nLý do: {_confirmation.Reason}\nChỉ định: {string.Join(", ",Vaccines.Where(v=>v.IsSelected).Select(v=>v.Name))}\n\nXác nhận lưu kết luận? Sau khi lưu chỉ xem, không sửa/hủy.";
    }
    public async Task ConfirmAsync(bool confirmed)
    {
        var request=_confirmation;_confirmation=null;_confirming=false;
        if(!confirmed || request is null || !IsCurrent || _denied || Detail is null){Refresh();return;}
        _pending=request;_pendingId=Detail.Appointment.Id;_committed=false;_reconciled=false;
        await SendPendingAsync();
    }
    private void Accept(ClinicalAssessment result)
    {
        _assessment=result;_pending=null;_committed=false;_reconciled=false;ClearDraft();OnPropertyChanged(nameof(Assessment));
        Feedback="Đã đọc kết luận và chỉ định từ API / SQL. Thông tin chỉ đọc; chưa phải ghi nhận mũi tiêm.";
    }
    private async Task SendPendingAsync()
    {
        _busy=true;_reconciled=false;Feedback="Đang lưu kết luận và chỉ định…";Refresh();
        try {var result=await _service.CreateAsync(_pendingId,_pending!);if(IsCurrent && !_denied)Accept(result);}
        catch(ClinicalApiException e) when(e.Code is "outcomeUnknown" or "recordedUnreadable" or "assessmentAlreadyRecorded") {
            if(IsCurrent){_committed=e.Code=="recordedUnreadable";Feedback=e.Message;}
        }
        catch(Exception e){if(IsCurrent){if(e is ClinicalApiException)_pending=null;Error(e);}}
        finally{_busy=false;Refresh();}
    }
    public async Task ReconcileAsync()
    {
        if(!HasAccess || _busy || _pending is null)return;
        _busy=true;_reconciled=false;Feedback="Đang GET đối chiếu kết quả…";Refresh();
        try {var result=await _service.GetAssessmentAsync(_pendingId);if(!IsCurrent || _denied)return;
            if(result is not null)Accept(result);
            else{_reconciled=true;Feedback=_committed?"API đã trả 201. Tiếp tục GET, không gửi lại.":"GET chưa thấy kết luận. Có thể thử lại đúng yêu cầu cũ; API chặn phiếu trùng.";}
        }catch(Exception e){Error(e);}finally{_busy=false;Refresh();}
    }
    public async Task RetryAsync()
    {
        if(HasAccess && !_busy && _pending is not null && _reconciled && !_committed)await SendPendingAsync();
    }
}
