namespace Klyroo.UI.Maui;

/// <summary>An on/off switch with a label and an optional description: the native <see cref="Switch"/>, drawn in the semantic color. Tapping the text toggles it.</summary>
public class KlySwitch : Grid
{
    /// <summary>Identifies <see cref="IsToggled"/>.</summary>
    public static readonly BindableProperty IsToggledProperty = BindableProperty.Create(nameof(IsToggled), typeof(bool), typeof(KlySwitch), false, BindingMode.TwoWay, propertyChanged: (b, _, v) => ((KlySwitch)b).ShowToggled((bool)v));
    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(KlySwitch), null, propertyChanged: (b, _, v) => ((KlySwitch)b).ShowText(((KlySwitch)b)._label, (string?)v));
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlySwitch), null, propertyChanged: (b, _, v) => ((KlySwitch)b).ShowText(((KlySwitch)b)._description, (string?)v));
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlySwitch), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlySwitch)b).ApplyStyle());

    private readonly Switch _switch = new() { VerticalOptions = LayoutOptions.Start };
    private readonly Label _label = new() { FontSize = 14, FontAttributes = FontAttributes.Bold, IsVisible = false };
    private readonly Label _description = new() { FontSize = 14, IsVisible = false };

    /// <summary>Creates a switch.</summary>
    public KlySwitch()
    {
        ColumnSpacing = 10;
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        var texts = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center, Children = { _label, _description } };
        var toggle = new TapGestureRecognizer();
        toggle.Tapped += (_, _) =>
        {
            if (IsEnabled) IsToggled = !IsToggled;
        };
        texts.GestureRecognizers.Add(toggle);

        this.Add(_switch, 0);
        this.Add(texts, 1);
        _switch.Toggled += (_, e) => SetValue(IsToggledProperty, e.Value);
        ApplyStyle();
    }

    /// <summary>Raised when the switch turns on or off, by the user or by code.</summary>
    public event EventHandler<ToggledEventArgs>? Toggled;

    /// <summary>Whether the switch is on. Two-way bindable.</summary>
    public bool IsToggled { get => (bool)GetValue(IsToggledProperty); set => SetValue(IsToggledProperty, value); }

    /// <summary>Label of the switch.</summary>
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Text under the label.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Semantic color of the track while the switch is on.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web switch: disabled:opacity-60.
        if (propertyName == nameof(IsEnabled))
        {
            _switch.IsEnabled = IsEnabled;
            Opacity = IsEnabled ? 1 : 0.6;
        }
    }

    private void ShowToggled(bool value)
    {
        if (_switch.IsToggled != value) _switch.IsToggled = value;
        ApplyStyle();
        Toggled?.Invoke(this, new ToggledEventArgs(value));
    }

    private void ShowText(Label label, string? text)
    {
        label.Text = text;
        label.IsVisible = !string.IsNullOrEmpty(text);
    }

    private void ApplyStyle()
    {
        var set = KlyTheme.Resolve(Color);
        _switch.SetThemeColor(Switch.OnColorProperty, set.Fill);
        // The thumb is wider than the track and overhangs it, so on a dark page it must not take the dark contrast color of the fill:
        // on the colored track it is the contrast color in the light theme and white in the dark one; on the gray track it is white.
        if (IsToggled) _switch.SetThemeColor(Switch.ThumbColorProperty, new KlyThemeColor(set.Contrast.Light, Colors.White));
        else _switch.ThumbColor = Colors.White;
        _label.SetThemeColor(Label.TextColorProperty, KlyTheme.Text);
        _description.SetThemeColor(Label.TextColorProperty, KlyTheme.TextMuted);
    }
}
