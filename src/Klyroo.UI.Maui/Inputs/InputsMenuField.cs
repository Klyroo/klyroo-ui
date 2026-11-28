namespace Klyroo.UI.Maui;

/// <summary>The field of an input menu: the native <see cref="Entry"/> in a Klyroo.UI frame, with a chevron at its end that opens the suggestions.</summary>
internal sealed class InputsMenuField : KlyFieldBase
{
    private readonly Entry _entry;
    private readonly InputsIconButton _chevron = new(KlyIconNames.ChevronDown, "Suggestions", KlySize.Md, 36, 40) { Dimmed = 0.6 };

    public InputsMenuField() : this(new Entry())
    {
    }

    private InputsMenuField(Entry entry) : base(entry)
    {
        _entry = entry;
        _entry.VerticalOptions = LayoutOptions.Center;
        _chevron.IconColor = KlyTheme.TextDimmed;
        Content = null;
        var grid = new Grid { ColumnSpacing = 4 };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.Add(_entry, 0);
        grid.Add(_chevron, 1);
        Content = grid;
        ApplyStyle();
    }

    public Entry Entry => _entry;

    public InputsIconButton Chevron => _chevron;

    protected override void StyleText(KlyThemeColor text, KlyThemeColor placeholder, double fontSize)
    {
        _entry.SetThemeColor(InputView.TextColorProperty, text);
        _entry.SetThemeColor(InputView.PlaceholderColorProperty, placeholder);
        _entry.FontSize = fontSize;
    }
}
