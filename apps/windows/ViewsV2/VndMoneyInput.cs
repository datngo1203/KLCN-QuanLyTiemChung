using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TiemChung.Desktop.ViewsV2;

public sealed class VndMoneyInput : TextBox
{
    public static readonly DependencyProperty RawTextProperty = DependencyProperty.Register(
        nameof(RawText), typeof(string), typeof(VndMoneyInput),
        new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRawTextChanged));

    public string RawText
    {
        get => (string)GetValue(RawTextProperty);
        set => SetValue(RawTextProperty, value);
    }

    private bool _formatting;

    private static void OnRawTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var input = (VndMoneyInput)sender;
        if (input._formatting) return;
        input._formatting = true;
        try { input.Text = Format(input.RawText); }
        finally { input._formatting = false; }
    }

    private static string Format(string raw)
    {
        if (raw.Any(c => !char.IsAsciiDigit(c))) return raw;
        return string.Concat(raw.Select((digit, index) =>
            index > 0 && (raw.Length - index) % 3 == 0 ? " " + digit : digit.ToString()));
    }

    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        base.OnTextChanged(e);
        if (_formatting) return;
        var raw = Text.Replace(" ", "");
        var caret = Text[..Math.Min(CaretIndex, Text.Length)].Count(c => c != ' ');
        var formatted = Format(raw);
        _formatting = true;
        try {
            if (Text != formatted) {
                Text = formatted;
                var position = 0;
                while (caret > 0 && position < Text.Length)
                    if (Text[position++] != ' ') caret--;
                CaretIndex = position;
            }
            SetCurrentValue(RawTextProperty, raw);
        }
        finally { _formatting = false; }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Handled || IsReadOnly || SelectionLength != 0 || Keyboard.Modifiers != ModifierKeys.None) return;
        // Cross the presentation space so the native command deletes the neighboring digit.
        if (e.Key == Key.Back && CaretIndex > 0 && Text[CaretIndex - 1] == ' ') CaretIndex--;
        if (e.Key == Key.Delete && CaretIndex < Text.Length && Text[CaretIndex] == ' ') CaretIndex++;
    }
}
