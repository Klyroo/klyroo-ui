namespace Klyroo.UI.Maui;

/// <summary>A short label in a colored pill: a status, a count or a tag.</summary>
public class KlyBadge : Border
{
    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(KlyBadge), null, propertyChanged: (b, _, v) => ((KlyBadge)b)._label.Text = (string?)v);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyBadge), KlyColor.Primary, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(KlyVariant), typeof(KlyBadge), KlyVariant.Solid, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyBadge), KlySize.Md, propertyChanged: Restyle);

    private readonly Label _label = new() { LineBreakMode = LineBreakMode.TailTruncation, VerticalTextAlignment = TextAlignment.Center };

    /// <summary>Creates a badge.</summary>
    public KlyBadge()
    {
        Content = _label;
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        ApplyStyle();
    }

    /// <summary>Text of the badge.</summary>
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Semantic color of the badge.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Look of the badge: <c>Solid</c>, <c>Outline</c>, <c>Soft</c> or <c>Subtle</c>.</summary>
    public KlyVariant Variant { get => (KlyVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Size of the badge, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    private static void Restyle(BindableObject bindable, object oldValue, object newValue) => ((KlyBadge)bindable).ApplyStyle();

    private void ApplyStyle()
    {
        var surface = Surface.For(Variant, KlyTheme.Resolve(Color));
        this.Apply(surface);
        _label.SetThemeColor(Label.TextColorProperty, surface.Text);

        // Padding, font and radius follow the web theme: small badges have the small radius, the others the base one.
        var (padding, fontSize, radius) = Size switch
        {
            KlySize.Xs => (new Thickness(4, 2), 10d, KlyTheme.RadiusOf(0.5)),
            KlySize.Sm => (new Thickness(6, 4), 11d, KlyTheme.RadiusOf(0.5)),
            KlySize.Md => (new Thickness(8, 4), 12d, KlyTheme.RadiusOf(1)),
            KlySize.Lg => (new Thickness(8, 4), 14d, KlyTheme.RadiusOf(1)),
            KlySize.Xl => (new Thickness(10, 4), 16d, KlyTheme.RadiusOf(1)),
            _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, null),
        };
        Padding = padding;
        _label.FontSize = fontSize;
        this.SetRadius(radius);
    }
}
