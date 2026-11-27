namespace Klyroo.UI.Maui;

/// <summary>
/// A button that switches the app between light and dark (<see cref="KlyColorMode.Toggle"/>). It shows a moon in the light theme and a sun in the dark one.
/// Without a <see cref="Label"/> it is a square icon button and needs an <see cref="AccessibleName"/>.
/// </summary>
public class KlyColorModeButton : ContentView
{
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyColorModeButton), KlyColor.Neutral, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(ButtonVariant), typeof(KlyColorModeButton), ButtonVariant.Ghost, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyColorModeButton), KlySize.Md, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyColorModeButton), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyColorModeButton), "Toggle dark mode", propertyChanged: Restyle);

    private readonly NavigationButton _button = new();
    private readonly KlyIcon _moon = new() { Name = KlyIconNames.Dark };
    private readonly KlyIcon _sun = new() { Name = KlyIconNames.Light };
    private readonly Label _label = new() { LineBreakMode = LineBreakMode.TailTruncation, VerticalTextAlignment = TextAlignment.Center, IsVisible = false };

    /// <summary>Creates a color mode button.</summary>
    public KlyColorModeButton()
    {
        // The moon shows in the light theme and the sun in the dark one, whoever changes the theme.
        _moon.SetAppTheme(IsVisibleProperty, true, false);
        _sun.SetAppTheme(IsVisibleProperty, false, true);
        var row = new HorizontalStackLayout { Spacing = 6, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, Children = { _moon, _sun, _label } };
        _button.Face.Content = row;
        _button.Face.SetRadius(KlyTheme.RadiusOf(1));
        _button.Clicked += (_, _) => KlyColorMode.Toggle();
        Content = _button;
        HorizontalOptions = LayoutOptions.Start;
        ApplyStyle();
    }

    /// <summary>Semantic color of the button. Defaults to neutral.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Look of the button: <c>Solid</c>, <c>Outline</c>, <c>Soft</c>, <c>Subtle</c>, <c>Ghost</c> or <c>Link</c>. Defaults to <c>Ghost</c>.</summary>
    public ButtonVariant Variant { get => (ButtonVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Size of the button, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Visible text next to the icon. Without it the button is icon-only and needs <see cref="AccessibleName"/>.</summary>
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>What a screen reader says for an icon-only button. Defaults to "Toggle dark mode".</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    internal NavigationButton Button => _button;

    internal KlyIcon Moon => _moon;

    internal KlyIcon Sun => _sun;

    internal Label TextLabel => _label;

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled)) _button.IsEnabled = IsEnabled;
    }

    private static void Restyle(BindableObject bindable, object oldValue, object newValue) => ((KlyColorModeButton)bindable).ApplyStyle();

    private void ApplyStyle()
    {
        var surface = Surface.For(Variant, KlyTheme.Resolve(Color));
        _button.Face.Apply(surface);
        _moon.IconColor = surface.Text;
        _sun.IconColor = surface.Text;
        _moon.Size = Size;
        _sun.Size = Size;
        _label.SetThemeColor(Microsoft.Maui.Controls.Label.TextColorProperty, surface.Text);
        _label.Text = Label;
        _label.IsVisible = !string.IsNullOrEmpty(Label);

        // Padding, font and height follow the web button: the same table as KlyButton. An icon-only button is square.
        var (padding, fontSize, height) = Size switch
        {
            KlySize.Xs => (new Thickness(8, 4), 12d, 28d),
            KlySize.Sm => (new Thickness(10, 6), 12d, 32d),
            KlySize.Md => (new Thickness(10, 6), 14d, 40d),
            KlySize.Lg => (new Thickness(12, 8), 14d, 44d),
            KlySize.Xl => (new Thickness(12, 8), 16d, 48d),
            _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, null),
        };
        _button.Face.Padding = padding;
        _button.Face.MinimumHeightRequest = height;
        _label.FontSize = fontSize;
        var iconOnly = string.IsNullOrEmpty(Label);
        _button.Face.WidthRequest = iconOnly ? height : -1;
        _button.Face.HeightRequest = iconOnly ? height : -1;
        SemanticProperties.SetDescription(_button, iconOnly ? AccessibleName : null);
    }
}
