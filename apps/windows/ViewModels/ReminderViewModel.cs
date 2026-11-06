using System.Collections.ObjectModel;
using TiemChung.Desktop.Services;

namespace TiemChung.Desktop.ViewModels;

public sealed class ReminderViewModel:ViewModelBase
{
    private readonly IReminderService _service;
    private readonly IdentityApiService _identity;
    private readonly int _revision;
    private int? _id;
    private bool _busy,_pending,_committed,_reconciled,_confirming,_denied;
    private ReminderDetail? _detail;
    private string _feedback="";
    private string _search="", _queueFeedback="";
    private int _page=1;
    private AppointmentRecord? _selected;
    public event Action? Changed;
    public ReminderViewModel(IReminderService? service=null,IdentityApiService? identity=null)
    {
        _identity=identity??IdentityApiService.Current;_revision=_identity.SessionRevision;_service=service??new ReminderApiService(_identity);
        _identity.SessionCleared+=(_,_)=>{_id=null;_detail=null;_pending=false;_committed=false;_reconciled=false;_confirming=false;ClearQueue();_feedback="";Refresh();};
        LoadCommand=new(async _=>await LoadAsync(),_=>HasAccess&&!_busy&&!_confirming&&_id.HasValue);
        ConfirmCommand=new(_=>{_confirming=true;Refresh();},_=>CanRecord);
        CancelCommand=new(_=>{_confirming=false;Refresh();},_=>IsConfirming&&!_busy);
        RecordCommand=new(async _=>await RecordAsync(),_=>IsConfirming&&!_busy);
        RetryCommand=new(async _=>await PostAsync(),_=>HasPending&&!_busy&&_reconciled&&!_committed&&_detail?.CanRecord==true);
        DeliveryRetryCommand=new(async _=>await PostAsync(true),_=>HasDetail&&!_busy&&!_pending&&_detail!.Reminder?.CanRetry==true);
        SearchCommand=new(async _=>await LoadQueueAsync(),_=>CanBrowse);
        PreviousCommand=new(async _=>await LoadQueueAsync(Page-1),_=>CanBrowse&&Page>1);
        NextCommand=new(async _=>await LoadQueueAsync(Page+1),_=>CanBrowse&&Queue.Count==AppointmentApiService.PageSize);
    }
    private bool IsCurrent=>_identity.IsSignedIn&&_identity.SessionRevision==_revision;
    public bool HasAccess=>IsCurrent&&_identity.CanUseAppointments&&!_denied;
    public bool HasSelection=>HasAccess&&_id.HasValue;
    public bool HasDetail=>HasAccess&&_detail!=null;
    public bool HasPending=>HasAccess&&_pending;
    public bool BlocksNavigation=>HasAccess&&(_busy||_pending||_confirming);
    public bool CanRecord=>HasDetail&&!_busy&&!_pending&&!_confirming&&_detail!.CanRecord&&_detail.Reminder==null;
    public bool IsConfirming=>HasAccess&&_confirming;
    public bool IsBusy=>_busy;
    public bool CanBrowse=>HasAccess&&!BlocksNavigation;
    public bool NoAccess=>!HasAccess;
    public bool NoResults=>HasAccess&&!_busy&&Queue.Count==0;
    public int Page=>_page;
    public ObservableCollection<AppointmentRecord> Queue{get;}=[];
    public string Search{get=>_search;set{if(CanBrowse)Set(ref _search,value);}}
    public string QueueFeedback=>_queueFeedback;
    public AppointmentRecord? Appointment=>HasAccess?_detail?.Appointment:null;
    public AppointmentRecord? Selected
    {
        get=>_selected;
        set{if(CanBrowse&&Set(ref _selected,value))Select(value?.Id);}
    }
    public string Content=>HasDetail?(_detail!.Reminder?.Content??_detail.Preview??""):"";
    public string StatusLabel=>Result?.StatusLabel??"Chưa gửi";
    public string MaskedRecipient=>HasDetail?(_detail!.Reminder?.MaskedRecipient??_detail.MaskedRecipient??""):"";
    public string Subject=>HasDetail?(_detail!.Reminder?.Subject??_detail.Subject??""):"";
    public string DeliveryError=>Result?.ErrorLabel??"";
    public string Feedback=>_feedback;
    public ReminderRecord? Result=>HasAccess?_detail?.Reminder:null;
    public RelayCommand LoadCommand{get;}
    public RelayCommand ConfirmCommand{get;}
    public RelayCommand CancelCommand{get;}
    public RelayCommand RecordCommand{get;}
    public RelayCommand RetryCommand{get;}
    public RelayCommand DeliveryRetryCommand{get;}
    public RelayCommand SearchCommand{get;}
    public RelayCommand PreviousCommand{get;}
    public RelayCommand NextCommand{get;}
    private void ClearQueue(){Queue.Clear();_selected=null;_search="";_queueFeedback="";_page=1;OnPropertyChanged(nameof(Selected));OnPropertyChanged(nameof(Search));}
    public async Task LoadQueueAsync(int page=1)
    {
        if(!CanBrowse||page<1)return;
        _busy=true;_queueFeedback="Đang tải lịch cần nhắc…";Queue.Clear();Refresh();
        try{
            var rows=await _service.ListAsync(_search,page);if(!IsCurrent||_denied)return;
            _page=page;foreach(var row in rows)Queue.Add(row);
            _queueFeedback=rows.Length==0?"Không có lịch cần nhắc phù hợp.":"Chọn lịch để xem nội dung nhắc.";
        }catch(Exception e){Error(e);if(HasAccess)_queueFeedback="Không tải được danh sách. Hãy thử lại.";}
        finally{_busy=false;Refresh();}
    }
    public void Select(int? id)
    {
        if(BlocksNavigation||_id==id)return;
        _id=id;_detail=null;_confirming=false;_pending=false;_committed=false;_reconciled=false;_feedback="";Refresh();
    }
    private void Refresh()
    {
        foreach(var p in new[]{nameof(HasAccess),nameof(HasSelection),nameof(HasDetail),nameof(HasPending),nameof(BlocksNavigation),nameof(CanRecord),nameof(IsConfirming),nameof(IsBusy),nameof(Content),nameof(Feedback),nameof(Result),nameof(CanBrowse),nameof(NoAccess),nameof(NoResults),nameof(Page),nameof(QueueFeedback),nameof(Appointment),nameof(StatusLabel),nameof(MaskedRecipient),nameof(Subject),nameof(DeliveryError)})OnPropertyChanged(p);
        foreach(var c in new[]{LoadCommand,ConfirmCommand,CancelCommand,RecordCommand,RetryCommand,DeliveryRetryCommand,SearchCommand,PreviousCommand,NextCommand})c?.NotifyCanExecuteChanged();
        Changed?.Invoke();
    }
    private void Error(Exception e)
    {
        if(!IsCurrent)return;
        if(e is AppointmentApiException{Code:"invalidSession" or "permissionDenied"}){_denied=true;_id=null;_detail=null;_pending=false;_confirming=false;ClearQueue();}
        _feedback=e is AppointmentApiException?e.Message:"Không tải được thông tin. Hãy đối chiếu kết quả trước khi thử lại.";
    }
    public async Task LoadAsync()
    {
        if(!LoadCommand.CanExecute(null)||_id is not{} id)return;
        if(!_pending)_detail=null;
        _busy=true;_reconciled=false;_feedback="Đang đối chiếu nhắc lịch…";Refresh();
        try{
            var detail=await _service.GetAsync(id);if(!IsCurrent||_denied)return;
            _detail=detail;
            if(detail.Reminder!=null){_pending=false;_committed=false;_feedback=DeliveryFeedback(detail.Reminder);}
            else{_reconciled=true;if(_pending&&!_committed&&!detail.CanRecord)_pending=false;
                _feedback=_pending?(_committed?"Đã nhận xác nhận lưu; tiếp tục đối chiếu, không ghi lại.":"Chưa có ghi nhận. Có thể thử lại thủ công trên cùng lịch."):
                detail.CanRecord?"Xem email và người nhận trước khi xác nhận.":detail.UnavailableReason=="reminderRecipientUnavailable"?"Chưa có email đủ điều kiện nhận nhắc.":"Lịch không đủ điều kiện gửi email nhắc lịch.";}
        }catch(Exception e){Error(e);}finally{_busy=false;Refresh();}
    }
    public async Task RecordAsync(){if(RecordCommand.CanExecute(null))await PostAsync();}
    private static string DeliveryFeedback(ReminderRecord record)=>record.Status switch {
        "DaGui"=>"Đã gửi email nhắc lịch.", "GuiLoi"=>record.ErrorLabel, _=>"Đã ghi nhận yêu cầu. Email đang chờ gửi; tải lại để xem kết quả." };
    private async Task PostAsync(bool deliveryRetry=false)
    {
        if(!HasAccess||_busy||_id is not{} id)return;
        var attempts=_detail?.Reminder?.Attempts??0;
        _busy=true;_confirming=false;_pending=true;_reconciled=false;_feedback="Đang ghi nhận yêu cầu gửi email…";Refresh();
        try{
            var saved=deliveryRetry?await _service.RetryAsync(id,attempts):await _service.RecordAsync(id);if(!IsCurrent||_denied)return;
            _detail=_detail! with{Reminder=saved,Preview=null,CanRecord=false};_pending=false;_committed=false;_feedback=DeliveryFeedback(saved);
        }catch(Exception e){
            if(!IsCurrent)return;
            if(e is AppointmentApiException{Code:"recordedUnreadable"})_committed=true;
            else if(e is AppointmentApiException{Code:not("outcomeUnknown" or "reminderAlreadyRecorded")}){_pending=false;_detail=null;}
            Error(e);
        }finally{_busy=false;Refresh();}
    }
}
