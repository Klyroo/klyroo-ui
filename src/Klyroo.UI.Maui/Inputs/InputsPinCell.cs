namespace Klyroo.UI.Maui;

/// <summary>One box of a <see cref="KlyPinInput"/>: a square Klyroo.UI frame around an <see cref="Entry"/> that holds one character.</summary>
internal sealed class InputsPinCell : KlyFieldBase
{
    private readonly Entry _entry;

    public InputsPinCell() : this(new Entry { HorizontalTextAlignment = TextAlignment.Center, VerticalOptions = LayoutOptions.Center })
    {
    }

    private InputsPinCell(Entry entry) : base(entry)
    {
        _entry = entry;
        HorizontalOptions = LayoutOptions.Start;
        ApplyStyle();
    }

    public Entry Entry => _entry;

    /// <summary>Width and height of a box of each size: size-7 to size-11 of the web theme.</summary>
    public static double DimensionOf(KlySize size) => size switch
    {
        KlySize.Xs => 28,
        KlySize.Sm => 32,
        KlySize.Md => 36,
        KlySize.Lg => 40,
        KlySize.Xl => 44,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    protected override void StyleText(KlyThemeColor text, KlyThemeColor placeholder, double fontSize)
    {
        // The box is a square of a fixed size: the character sits in the middle, with no padding.
        Padding = new Thickness(0);
        var dimension = DimensionOf(Size);
        WidthRequest = dimension;
        HeightRequest = dimension;
        _entry.SetThemeColor(InputView.TextColorProperty, text);
        _entry.SetThemeColor(InputView.PlaceholderColorProperty, placeholder);
        _entry.FontSize = fontSize;
    }
}
