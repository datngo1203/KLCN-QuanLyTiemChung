using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TiemChung.Desktop.ViewModels;

namespace TiemChung.Desktop.ViewsV2;

public partial class CatalogSuggestionBox : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string),
        typeof(CatalogSuggestionBox), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty SuggestionsProperty = DependencyProperty.Register(nameof(Suggestions), typeof(string[]),
        typeof(CatalogSuggestionBox), new PropertyMetadata(null, (d, _) => ((CatalogSuggestionBox)d).UpdateChoices()));
    public static readonly DependencyProperty MaxLengthProperty = DependencyProperty.Register(nameof(MaxLength), typeof(int),
        typeof(CatalogSuggestionBox));

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string[]? Suggestions { get => (string[]?)GetValue(SuggestionsProperty); set => SetValue(SuggestionsProperty, value); }
    public int MaxLength { get => (int)GetValue(MaxLengthProperty); set => SetValue(MaxLengthProperty, value); }

    public CatalogSuggestionBox() => InitializeComponent();

    public void FocusInput() => Input.Focus();

    private void InputChanged(object sender, TextChangedEventArgs e) => UpdateChoices();
    private void FocusChanged(object sender, DependencyPropertyChangedEventArgs e) => UpdateChoices();
    private void EnabledChanged(object sender, DependencyPropertyChangedEventArgs e) => UpdateChoices();

    private void UpdateChoices()
    {
        if (Choices is null || NoMatch is null) return;
        HideChoices();
        if (!IsEnabled || !IsKeyboardFocusWithin || string.IsNullOrWhiteSpace(Input.Text)) return;
        var suggestions = CatalogViewModel.FindSuggestions(Suggestions ?? [], Input.Text);
        Choices.ItemsSource = suggestions;
        Choices.Visibility = suggestions.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        NoMatch.Visibility = suggestions.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void HideChoices()
    {
        Choices.Visibility = Visibility.Collapsed;
        NoMatch.Visibility = Visibility.Collapsed;
    }

    private void ChooseByMouse(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source &&
            ItemsControl.ContainerFromElement(Choices, source) is ListBoxItem { Content: string value }) Choose(value);
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && (Choices.IsVisible || NoMatch.IsVisible)) {
            Input.Focus();
            HideChoices();
        }
        else if (e.Key == Key.Down && Input.IsKeyboardFocused && Choices.IsVisible &&
                 Choices.ItemContainerGenerator.ContainerFromIndex(0) is ListBoxItem first) {
            Choices.SelectedIndex = 0;
            first.Focus();
        }
        else if (e.Key == Key.Enter && Choices.IsKeyboardFocusWithin && Choices.SelectedItem is string value) Choose(value);
        else return;
        e.Handled = true;
    }

    private void Choose(string value)
    {
        SetCurrentValue(TextProperty, value);
        Input.Focus();
        Input.CaretIndex = Input.Text.Length;
        HideChoices();
    }
}
