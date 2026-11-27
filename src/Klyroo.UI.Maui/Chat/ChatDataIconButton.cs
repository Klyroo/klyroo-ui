namespace Klyroo.UI.Maui;

/// <summary>A square button with a vector icon: a native <see cref="Button"/> (touch, focus and accessibility of the platform) with a <see cref="KlyIcon"/> laid over it.</summary>
internal sealed class ChatDataIconButton : Grid
{
    private readonly KlyButton _button = new() { Block = true, Padding = new Thickness(0), Text = string.Empty };
    private readonly KlyIcon _icon = new() { InputTransparent = true, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
    private string _name = string.Empty;
    private KlyColor _color = KlyColor.Neutral;
    private ButtonVariant _variant = ButtonVariant.Ghost;
    private KlySize _iconSize = KlySize.Xs;

    public ChatDataIconButton()
    {
        Children.Add(_button);
        Children.Add(_icon);
        _button.Clicked += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);
        HorizontalOptions = LayoutOptions.Center;
        VerticalOptions = LayoutOptions.Center;
        Dimension = 40;
        Refresh();
    }

    /// <summary>Raised when the button is pressed.</summary>
    public event EventHandler? Clicked;

    /// <summary>The icon, by name.</summary>
    public string IconName { get => _name; set { _name = value; Refresh(); } }

    /// <summary>The semantic color of the button.</summary>
    public KlyColor Color { get => _color; set { _color = value; Refresh(); } }

    /// <summary>The look of the button.</summary>
    public ButtonVariant Variant { get => _variant; set { _variant = value; Refresh(); } }

    /// <summary>The size of the icon.</summary>
    public KlySize IconSize { get => _iconSize; set { _iconSize = value; Refresh(); } }

    /// <summary>Width and height of the button.</summary>
    public double Dimension
    {
        get => WidthRequest;
        set
        {
            WidthRequest = value;
            HeightRequest = value;
            _button.MinimumHeightRequest = value;
            _button.WidthRequest = value;
            _button.HeightRequest = value;
        }
    }

    /// <summary>What a screen reader says for the button.</summary>
    public string? Description
    {
        get => SemanticProperties.GetDescription(_button);
        set => SemanticProperties.SetDescription(_button, value);
    }

    /// <summary>The color of the icon, as drawn.</summary>
    public KlyThemeColor? IconColor => _icon.IconColor;

    /// <summary>Presses the button, as a tap does.</summary>
    public void SendTap() => _button.SendClicked();

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled))
        {
            _button.IsEnabled = IsEnabled;
            _icon.Opacity = IsEnabled ? 1 : 0.6;
        }
    }

    private void Refresh()
    {
        _button.Color = _color;
        _button.Variant = _variant;
        _icon.Name = _name;
        _icon.Size = _iconSize;
        _icon.IconColor = Surface.For(_variant, KlyTheme.Resolve(_color)).Text;
    }
}
