using System.Windows;
using System.Windows.Controls;
using TiemChung.Desktop.ViewModels;

namespace TiemChung.Desktop.ViewsV2;

public partial class ScreeningView : UserControl
{
    public ScreeningView()
    {
        InitializeComponent();
        Loaded+=async (_,_)=>{if(DataContext is ScreeningViewModel vm && vm.Appointments.Count==0)await vm.LoadAsync();};
    }
    private async void AppointmentChanged(object sender, SelectionChangedEventArgs e)
    {
        if(DataContext is not ScreeningViewModel vm)return;
        await vm.LoadDetailAsync();
        if(vm.Detail is not null){DetailHeading.BringIntoView();DetailHeading.Focus();}
    }
    private async void ConfirmAssessment(object sender, RoutedEventArgs e)
    {
        if(DataContext is not ScreeningViewModel vm || vm.BeginConfirmation() is not {} message)return;
        var confirmed=MessageBox.Show(Window.GetWindow(this),message,"Xác nhận kết luận và chỉ định",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)==MessageBoxResult.Yes;
        await vm.ConfirmAsync(confirmed);
        DetailHeading.BringIntoView();DetailHeading.Focus();
    }
    private void View_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var compact=e.NewSize.Width<1050;
        Workspace.ColumnDefinitions[0].Width=compact?new GridLength(1,GridUnitType.Star):new GridLength(310);
        Workspace.ColumnDefinitions[1].Width=new GridLength(compact?0:20);
        Workspace.ColumnDefinitions[2].Width=compact?new GridLength(0):new GridLength(1,GridUnitType.Star);
        Grid.SetColumn(DetailPanel,compact?0:2);Grid.SetRow(DetailPanel,compact?1:0);
        DetailPanel.Margin=compact?new Thickness(0,16,0,0):new Thickness(0);
    }
}
