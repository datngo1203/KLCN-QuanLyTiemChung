using System.Windows.Controls;
using TiemChung.Desktop.ViewModels;
namespace TiemChung.Desktop.ViewsV2;
public partial class CatalogView : UserControl
{
 public CatalogView()=>InitializeComponent();
 private void Disease_Checked(object sender,System.Windows.RoutedEventArgs e)=>SetDisease(sender,true);
 private void Disease_Unchecked(object sender,System.Windows.RoutedEventArgs e)=>SetDisease(sender,false);
 private static void SetDisease(object sender,bool selected){if(sender is CheckBox{Content:string disease,Tag:CatalogViewModel vm})vm.SetDisease(disease,selected);}
}
