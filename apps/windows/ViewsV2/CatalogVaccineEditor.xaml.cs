using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TiemChung.Desktop.ViewModels;

namespace TiemChung.Desktop.ViewsV2;

public partial class CatalogVaccineEditor : UserControl
{
    public CatalogVaccineEditor() => InitializeComponent();

    public void CollapseRegimens() => Regimens.IsExpanded = false;

    private void UseCalendarForDate(object sender, RoutedEventArgs e)
    {
        if (sender is DatePicker picker && picker.Template.FindName("PART_TextBox", picker) is TextBox input)
            input.IsReadOnly = true;
    }

    private void ClearNewDate(object sender, RoutedEventArgs e)
    {
        if (DataContext is CatalogViewModel { CanEdit: true, CanClearInformationDate: true } vm) {
            vm.InformationDate = null;
            InformationDate.Focus();
        }
    }

    public void FocusInvalidField(string field)
    {
        var element = FindName(field == nameof(CatalogViewModel.Name) ? nameof(VaccineNameInput) : field) as FrameworkElement;
        if (element is null) return;
        if (element == Regimens) Regimens.IsExpanded = true;
        element.BringIntoView();
        switch (element) {
            case CatalogTaxonomyPicker picker: picker.FocusSearch(); break;
            case CatalogSuggestionBox suggestions: suggestions.FocusInput(); break;
            default:
                if (!element.Focus()) element.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                break;
        }
    }

    private void AddRegimen(object sender, RoutedEventArgs e)
    {
        if (DataContext is CatalogViewModel vm && vm.AddRegimen() is { } regimen) FocusItemField(RegimenList, regimen, "RegimenNameInput");
    }

    private void RemoveRegimen(object sender, RoutedEventArgs e)
    {
        if (DataContext is CatalogViewModel vm && sender is Button { DataContext: CatalogRegimenDraft regimen }) {
            vm.RemoveRegimen(regimen);
            if (!vm.Regimens.Contains(regimen)) AddRegimenButton.Focus();
        }
    }

    private void AddDose(object sender, RoutedEventArgs e)
    {
        if (DataContext is CatalogViewModel vm && sender is Button { DataContext: CatalogRegimenDraft regimen, Tag: ItemsControl list } &&
            vm.AddDose(regimen) is { } dose) FocusItemField(list, dose, "DoseTimingInput");
    }

    private void RemoveDose(object sender, RoutedEventArgs e)
    {
        if (DataContext is CatalogViewModel vm && sender is Button { DataContext: CatalogDoseDraft dose, Tag: CatalogRegimenDraft regimen }) {
            vm.RemoveDose(regimen, dose);
            if (!regimen.Doses.Contains(dose)) FocusItemField(RegimenList, regimen, "RegimenNameInput");
        }
    }

    private static void FocusItemField(ItemsControl list, object item, string name)
    {
        list.UpdateLayout();
        if (list.ItemContainerGenerator.ContainerFromItem(item) is ContentPresenter container &&
            container.ContentTemplate?.FindName(name, container) is FrameworkElement input) {
            input.BringIntoView();
            input.Focus();
        }
    }
}
