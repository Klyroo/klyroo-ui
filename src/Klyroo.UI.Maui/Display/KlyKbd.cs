namespace Klyroo.UI.Maui;

/// <summary>A keyboard key cap, to show a shortcut: <c>Ctrl</c>, <c>K</c>.</summary>
public class KlyKbd : Border
{
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(string), typeof(KlyKbd), null, propertyChanged: (b, _, v) => ((KlyKbd)b)._label.Text = (string?)v);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyKbd), KlyColor.Neutral, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(KlyVariant), typeof(KlyKbd), KlyVariant.Outline, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyKbd), KlySize.Md, propertyChanged: Restyle);

    private readonly Label _label = new() { HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center, LineBreakMode = LineBreakMode.NoWrap };

    /// <summary>Creates a key cap.</summary>
    public KlyKbd()
    {
        Content = _label;
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        Padding = new Thickness(4, 0);
        this.SetRadius(KlyTheme.RadiusOf(0.5));
        ApplyStyle();
    }

    /// <summary>The key to show, <c>Ctrl</c> or <c>K</c> for example.</summary>
    public string? Value { get => (string?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Semantic color of the key cap. Neutral by default.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Look of the key cap: <c>Solid</c>, <c>Outline</c> (the default), <c>Soft</c> or <c>Subtle</c>.</summary>
    public KlyVariant Variant { get => (KlyVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Size of the key cap, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    private static void Restyle(BindableObject bindable, object oldValue, object newValue) => ((KlyKbd)bindable).ApplyStyle();

    private void ApplyStyle()
    {
        var surface = Surface.For(Variant, KlyTheme.Resolve(Color));
        this.Apply(surface);
        _label.SetThemeColor(Label.TextColorProperty, surface.Text);

        // Height, minimum width and font follow the web theme (h-4 to h-7).
        var (height, fontSize) = Size switch
        {
            KlySize.Xs => (16d, 10d),
            KlySize.Sm => (16d, 11d),
            KlySize.Md => (20d, 12d),
            KlySize.Lg => (24d, 14d),
            KlySize.Xl => (28d, 16d),
            _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, null),
        };
        HeightRequest = height;
        MinimumWidthRequest = height;
        _label.FontSize = fontSize;
    }
}
