namespace Klyroo.UI.Maui;

/// <summary>
/// A square button that shows an icon, for the controls of the layout (menu, collapse, close). A transparent native <see cref="Button"/> lies over the icon,
/// so the click, the focus, the ripple and the accessibility are the ones of the platform, while the look follows the variant and the color of <see cref="KlyButton"/>.
/// </summary>
internal sealed class LayoutIconButton : Border
{
    private readonly KlyIcon _icon = new() { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, InputTransparent = true };
    private readonly Button _button = new()
    {
        BackgroundColor = Colors.Transparent,
        BorderWidth = 0,
        Padding = 0,
        MinimumWidthRequest = 0,
        MinimumHeightRequest = 0,
        HorizontalOptions = LayoutOptions.Fill,
        VerticalOptions = LayoutOptions.Fill,
    };

    private string _iconName = "";
    private ButtonVariant _variant = ButtonVariant.Ghost;
    private KlyColor _color = KlyColor.Neutral;
    private KlySize _size = KlySize.Md;

    public LayoutIconButton(string iconName, string label)
    {
        StrokeThickness = 0;
        Padding = 0;
        _iconName = iconName;
        _button.Clicked += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);
        Content = new Grid { Children = { _icon, _button } };
        Label = label;
        this.SetRadius(KlyTheme.RadiusOf(1));
        ApplyStyle();
    }

    public event EventHandler? Clicked;

    public string IconName { get => _iconName; set { _iconName = value; ApplyStyle(); } }

    public ButtonVariant Variant { get => _variant; set { _variant = value; ApplyStyle(); } }

    public KlyColor Color { get => _color; set { _color = value; ApplyStyle(); } }

    public KlySize Size { get => _size; set { _size = value; ApplyStyle(); } }

    /// <summary>The accessible name: the button has no text.</summary>
    public string Label
    {
        get => SemanticProperties.GetDescription(_button) ?? "";
        set => SemanticProperties.SetDescription(_button, value);
    }

    /// <summary>The side of the square, the same heights as the buttons of the library.</summary>
    public static double SideOf(KlySize size) => size switch
    {
        KlySize.Xs => 28,
        KlySize.Sm => 32,
        KlySize.Md => 40,
        KlySize.Lg => 44,
        KlySize.Xl => 48,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    /// <summary>Presses the button as a tap would.</summary>
    public void SendClicked() => _button.SendClicked();

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web button: disabled:opacity-60.
        if (propertyName == nameof(IsEnabled)) Opacity = IsEnabled ? 1 : 0.6;
    }

    private void ApplyStyle()
    {
        var surface = Surface.For(_variant, KlyTheme.Resolve(_color));
        this.Apply(surface);
        _button.SetThemeColor(Button.TextColorProperty, surface.Text);
        _icon.Name = _iconName;
        _icon.IconColor = surface.Text;
        _icon.Size = _size <= KlySize.Sm ? KlySize.Sm : KlySize.Md;
        var side = SideOf(_size);
        WidthRequest = side;
        HeightRequest = side;
    }
}
