using System.Windows.Controls;
using TiemChung.Desktop.ViewModels;
namespace TiemChung.Desktop.ViewsV2;
public partial class ProfilesView : UserControl {
    public ProfilesView() { InitializeComponent(); Loaded += async (_,_)=> { if(DataContext is ProfilesViewModel vm) await vm.LoadAsync(); }; }
}
