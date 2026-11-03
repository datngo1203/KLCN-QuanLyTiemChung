using System.Windows;
using System.Windows.Controls;
using TiemChung.Desktop.ViewModels;

namespace TiemChung.Desktop.ViewsV2;

public partial class InventoryReceivingWindow : Window
{
    private InventoryReceivingViewModel Model => (InventoryReceivingViewModel)DataContext;
    private void InvalidDate(object sender,DatePickerDateValidationErrorEventArgs e)
    {
        e.ThrowException=false;
        if(ReferenceEquals(sender,ManufacturedInput))Model.ManufacturedOn=null;
        else Model.ExpiresOn=null;
    }
    public InventoryReceivingWindow(InventoryReceivingViewModel model)
    {
        InitializeComponent();DataContext=model;
        Loaded+=async (_,_)=>{if(model.CanEdit&&model.Vaccines.Count==0)await model.LoadVaccinesAsync();};
        Closing+=(_,e)=>{if(model.IsBusy)e.Cancel=true;};
    }
    private async void ConfirmReceipt(object sender,RoutedEventArgs e)
    {
        if(Model.BeginConfirmation() is not {} message)return;
        var accepted=MessageBox.Show(this,message,"Xác nhận nhập lô mới",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)==MessageBoxResult.Yes;
        await Model.ConfirmAsync(accepted);
        if(Model.Result is not null){ReceiptResult.BringIntoView();ReceiptResult.Focus();}
    }
    private async void NewDraft(object sender,RoutedEventArgs e)
    {
        if(MessageBox.Show(this,"Bỏ bản nháp/kết quả đang xem và tạo bản nháp mới? Nếu kết quả trước chưa xác định, nên GET đối chiếu trước. Thao tác này không xóa dữ liệu SQL.","Tạo bản nháp mới",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)!=MessageBoxResult.Yes)return;
        await Model.StartNewAsync();VaccineSearch.Focus();
    }
}
