namespace Klyroo.UI.Maui;

/// <summary>The closed state of a select menu: a Klyroo.UI frame around a label, the icon of the chosen item and a chevron. Tapping it raises <see cref="Tapped"/>.</summary>
internal sealed class InputsPickerField : KlyFieldBase
{
    private readonly Label _label;
    private readonly KlyIcon _leading = new() { VerticalOptions = LayoutOptions.Center, IsVisible = false, IconColor = KlyTheme.TextMuted };
    private readonly KlyIcon _trailing;
    private bool _placeholder = true;
    private KlyThemeColor _textColor = KlyTheme.Text;
    private KlyThemeColor _placeholderColor = KlyTheme.TextDimmed;

    public InputsPickerField(string trailingIcon) : this(new Label { LineBreakMode = LineBreakMode.TailTruncation, VerticalOptions = LayoutOptions.Center }, trailingIcon)
    {
    }

    private InputsPickerField(Label label, string trailingIcon) : base(label)
    {
        _label = label;
        _trailing = new KlyIcon { Name = trailingIcon, VerticalOptions = LayoutOptions.Center, IconColor = KlyTheme.TextDimmed };
        Content = null;
        var grid = new Grid { ColumnSpacing = 6, MinimumHeightRequest = 28 };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.Add(_leading, 0);
        grid.Add(_label, 1);
        grid.Add(_trailing, 2);
        Content = grid;
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            if (IsEnabled) Tapped?.Invoke(this, EventArgs.Empty);
        };
        GestureRecognizers.Add(tap);
        ApplyStyle();
    }

    public event EventHandler? Tapped;

    public Label Label => _label;

    public KlyIcon Leading => _leading;

    public KlyIcon Trailing => _trailing;

    public void ShowText(string? text, bool isPlaceholder)
    {
        _label.Text = text;
        _placeholder = isPlaceholder;
        _label.SetThemeColor(Label.TextColorProperty, _placeholder ? _placeholderColor : _textColor);
    }

    public void ShowIcon(string? icon)
    {
        _leading.IsVisible = !string.IsNullOrEmpty(icon);
        if (icon is not null) _leading.Name = icon;
    }

    protected override void StyleText(KlyThemeColor text, KlyThemeColor placeholder, double fontSize)
    {
        _textColor = text;
        _placeholderColor = placeholder;
        _label.SetThemeColor(Label.TextColorProperty, _placeholder ? placeholder : text);
        _label.FontSize = fontSize;
        var icon = Size;
        _leading.Size = icon;
        _trailing.Size = icon;
    }
}
