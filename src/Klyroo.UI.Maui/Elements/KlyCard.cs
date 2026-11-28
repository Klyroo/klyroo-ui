namespace Klyroo.UI.Maui;

/// <summary>A container with the Klyroo.UI card look. Put any view in <see cref="Border.Content"/>; the padding is 16 and can be changed with <see cref="Border.Padding"/>.</summary>
public class KlyCard : Border
{
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(CardVariant), typeof(KlyCard), CardVariant.Outline, propertyChanged: (b, _, _) => ((KlyCard)b).ApplyStyle());

    /// <summary>Creates a card.</summary>
    public KlyCard()
    {
        Padding = new Thickness(16);
        this.SetRadius(KlyTheme.RadiusOf(2));
        ApplyStyle();
    }

    /// <summary>Look of the card: <c>Outline</c>, <c>Soft</c> or <c>Subtle</c>.</summary>
    public CardVariant Variant { get => (CardVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    private void ApplyStyle()
    {
        var tint = KlyTheme.BackgroundElevated.WithAlpha(0.5f);
        this.Apply(Variant switch
        {
            CardVariant.Outline => new Surface(KlyTheme.Background, KlyTheme.Border, 1, KlyTheme.Text),
            CardVariant.Soft => new Surface(tint, KlyThemeColor.Transparent, 0, KlyTheme.Text),
            CardVariant.Subtle => new Surface(tint, KlyTheme.Border, 1, KlyTheme.Text),
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null),
        });
    }
}
