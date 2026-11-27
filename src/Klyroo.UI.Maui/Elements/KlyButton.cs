namespace Klyroo.UI.Maui;

/// <summary>
/// Button: the native MAUI <see cref="Button"/> (ripple, focus and accessibility of the platform) drawn with the Klyroo.UI colors, sizes and radius.
/// Use <see cref="Button.Text"/>, <see cref="Button.ImageSource"/>, <see cref="Button.Command"/> and <see cref="Button.Clicked"/> as on any button.
/// </summary>
public class KlyButton : Button
{
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyButton), KlyColor.Primary, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(ButtonVariant), typeof(KlyButton), ButtonVariant.Solid, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyButton), KlySize.Md, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Block"/>.</summary>
    public static readonly BindableProperty BlockProperty = BindableProperty.Create(nameof(Block), typeof(bool), typeof(KlyButton), false, propertyChanged: Restyle);

    /// <summary>Creates a button.</summary>
    public KlyButton()
    {
        TextTransform = TextTransform.None;
        ApplyStyle();
    }

    /// <summary>Semantic color of the button: a built-in <see cref="KlyColor"/> or one registered with <see cref="KlyTheme.Register"/>.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Look of the button: <c>Solid</c>, <c>Outline</c>, <c>Soft</c>, <c>Subtle</c>, <c>Ghost</c> or <c>Link</c>.</summary>
    public ButtonVariant Variant { get => (ButtonVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Size of the button, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Full width. Without it the button is as wide as its content.</summary>
    public bool Block { get => (bool)GetValue(BlockProperty); set => SetValue(BlockProperty, value); }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web button: disabled:opacity-60.
        if (propertyName == nameof(IsEnabled)) Opacity = IsEnabled ? 1 : 0.6;
    }

    private static void Restyle(BindableObject bindable, object oldValue, object newValue) => ((KlyButton)bindable).ApplyStyle();

    private void ApplyStyle()
    {
        var surface = Surface.For(Variant, KlyTheme.Resolve(Color));
        this.SetThemeColor(BackgroundColorProperty, surface.Background);
        this.SetThemeColor(TextColorProperty, surface.Text);
        this.SetThemeColor(BorderColorProperty, surface.Stroke);
        BorderWidth = surface.StrokeThickness;
        CornerRadius = (int)KlyTheme.RadiusOf(1);

        // Padding and font follow the web theme (px/py/text of each size). The minimum height keeps a touch target usable.
        (Padding, FontSize, MinimumHeightRequest) = Size switch
        {
            KlySize.Xs => (new Thickness(8, 4), 12d, 28d),
            KlySize.Sm => (new Thickness(10, 6), 12d, 32d),
            KlySize.Md => (new Thickness(10, 6), 14d, 40d),
            KlySize.Lg => (new Thickness(12, 8), 14d, 44d),
            KlySize.Xl => (new Thickness(12, 8), 16d, 48d),
            _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, null),
        };

        HorizontalOptions = Block ? LayoutOptions.Fill : LayoutOptions.Start;
    }
}
