using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TiemChung.Desktop.ViewModels;

namespace TiemChung.Desktop.ViewsV2;

public partial class AftercareView : UserControl
{
    public AftercareView()
    {
        InitializeComponent();
        Loaded+=async (_,_)=>{if(DataContext is AftercareViewModel vm && vm.Queue.Count==0)await vm.LoadAsync();};
    }
    private async void ShotChanged(object sender,SelectionChangedEventArgs e)
    {
        if(DataContext is not AftercareViewModel vm)return;
        await vm.LoadDetailAsync();
        if(vm.Detail is not null){DetailHeading.BringIntoView();DetailHeading.Focus();}
    }
    private async void ConfirmFollowUp(object sender,RoutedEventArgs e)
    {
        if(DataContext is not AftercareViewModel vm || vm.BeginConfirmation() is not {} message)return;
        var confirmed=MessageBox.Show(Window.GetWindow(this),message,"Xác nhận nội dung theo dõi sau tiêm",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)==MessageBoxResult.Yes;
        await vm.ConfirmAsync(confirmed);
        DetailHeading.BringIntoView();DetailHeading.Focus();
    }
    private async void ReadResult(object sender,RoutedEventArgs e)
    {
        if(DataContext is not AftercareViewModel vm || !vm.CanReadResult)return;
        await vm.ReadResultAsync();
        if(vm.Detail is not null){DetailHeading.BringIntoView();DetailHeading.Focus();}
        else{PageFeedback.BringIntoView();PageFeedback.Focus();}
    }
    private static ScrollViewer? ListScroll(DependencyObject node)
    {
        if(node is ScrollViewer scroll)return scroll;
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)
            if(ListScroll(VisualTreeHelper.GetChild(node,i)) is {} child)return child;
        return null;
    }
    private void Queue_MouseWheel(object sender,MouseWheelEventArgs e)
    {
        // A short list (or its edge) must not trap wheel input above the rest of the form.
        var scroll=ListScroll((DependencyObject)sender);
        if(scroll is not null && (e.Delta>0?scroll.VerticalOffset<=0:scroll.VerticalOffset>=scroll.ScrollableHeight)) {
            PageScroll.ScrollToVerticalOffset(PageScroll.VerticalOffset-e.Delta);e.Handled=true;
        }
    }
    private void View_SizeChanged(object sender,SizeChangedEventArgs e)
    {
        var compact=e.NewSize.Width<1050;
        Workspace.ColumnDefinitions[0].Width=compact?new GridLength(1,GridUnitType.Star):new GridLength(320);
        Workspace.ColumnDefinitions[1].Width=new GridLength(compact?0:20);
        Workspace.ColumnDefinitions[2].Width=compact?new GridLength(0):new GridLength(1,GridUnitType.Star);
        Grid.SetColumn(DetailPanel,compact?0:2);Grid.SetRow(DetailPanel,compact?1:0);
        DetailPanel.Margin=compact?new Thickness(0,16,0,0):new Thickness(0);
    }
}
