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
    public event Action? Changed;
    public ReminderViewModel(IReminderService? service=null,IdentityApiService? identity=null)
    {
        _identity=identity??IdentityApiService.Current;_revision=_identity.SessionRevision;_service=service??new ReminderApiService(_identity);
        _identity.SessionCleared+=(_,_)=>{_id=null;_detail=null;_pending=false;_committed=false;_reconciled=false;_confirming=false;Refresh();};
        LoadCommand=new(async _=>await LoadAsync(),_=>HasAccess&&!_busy&&!_confirming&&_id.HasValue);
        ConfirmCommand=new(_=>{_confirming=true;Refresh();},_=>CanRecord);
        CancelCommand=new(_=>{_confirming=false;Refresh();},_=>IsConfirming&&!_busy);
        RecordCommand=new(async _=>await RecordAsync(),_=>IsConfirming&&!_busy);
        RetryCommand=new(async _=>await PostAsync(),_=>HasPending&&!_busy&&_reconciled&&!_committed);
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
    public string Content=>HasDetail?(_detail!.Reminder?.Content??_detail.Preview??"Không có nội dung mô phỏng đã lưu hoặc lịch không đủ điều kiện ghi mới."):"";
    public string Feedback=>_feedback;
    public ReminderRecord? Result=>HasAccess?_detail?.Reminder:null;
    public RelayCommand LoadCommand{get;}
    public RelayCommand ConfirmCommand{get;}
    public RelayCommand CancelCommand{get;}
    public RelayCommand RecordCommand{get;}
    public RelayCommand RetryCommand{get;}
    public void Select(int? id)
    {
        if(BlocksNavigation||_id==id)return;
        _id=id;_detail=null;_confirming=false;_pending=false;_committed=false;_reconciled=false;_feedback="";Refresh();
    }
    private void Refresh()
    {
        foreach(var p in new[]{nameof(HasAccess),nameof(HasSelection),nameof(HasDetail),nameof(HasPending),nameof(BlocksNavigation),nameof(CanRecord),nameof(IsConfirming),nameof(IsBusy),nameof(Content),nameof(Feedback),nameof(Result)})OnPropertyChanged(p);
        foreach(var c in new[]{LoadCommand,ConfirmCommand,CancelCommand,RecordCommand,RetryCommand})c?.NotifyCanExecuteChanged();
        Changed?.Invoke();
    }
    private void Error(Exception e)
    {
        if(!IsCurrent)return;
        if(e is AppointmentApiException{Code:"invalidSession" or "permissionDenied"}){_denied=true;_detail=null;_pending=false;_confirming=false;}
        _feedback=e is AppointmentApiException?e.Message:"Không đọc được API. Hãy GET đối chiếu; không gửi lại tự động.";
    }
    public async Task LoadAsync()
    {
        if(!LoadCommand.CanExecute(null)||_id is not{} id)return;
        if(!_pending)_detail=null;
        _busy=true;_reconciled=false;_feedback="Đang GET nhắc lịch…";Refresh();
        try{
            var detail=await _service.GetAsync(id);if(!IsCurrent||_denied)return;
            _detail=detail;
            if(detail.Reminder!=null){_pending=false;_committed=false;_feedback="Đã ghi log mô phỏng; chưa gửi email hoặc SMS.";}
            else{_reconciled=true;_feedback=_pending?(_committed?"Đã nhận 201; tiếp tục GET, không POST lại.":"GET chưa có log. Có thể thử lại thủ công trên cùng lịch."):
                detail.CanRecord?"Chưa có log. Xem nội dung trước khi xác nhận.":"Lịch không đủ điều kiện ghi nhắc tham dự.";}
        }catch(Exception e){Error(e);}finally{_busy=false;Refresh();}
    }
    public async Task RecordAsync(){if(RecordCommand.CanExecute(null))await PostAsync();}
    private async Task PostAsync()
    {
        if(!HasAccess||_busy||_id is not{} id)return;
        _busy=true;_confirming=false;_pending=true;_reconciled=false;_feedback="Đang ghi log mô phỏng…";Refresh();
        try{
            var saved=await _service.RecordAsync(id);if(!IsCurrent||_denied)return;
            _detail=_detail! with{Reminder=saved,Preview=null,CanRecord=false};_pending=false;_committed=false;_feedback="Đã ghi log mô phỏng. Nội dung chưa được gửi đến khách hàng.";
        }catch(Exception e){
            if(!IsCurrent)return;
            if(e is AppointmentApiException{Code:"recordedUnreadable"})_committed=true;
            else if(e is AppointmentApiException{Code:not("outcomeUnknown" or "reminderAlreadyRecorded")}){_pending=false;_detail=null;}
            Error(e);
        }finally{_busy=false;Refresh();}
    }
}
