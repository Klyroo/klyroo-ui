namespace Klyroo.UI.Maui;

/// <summary>A button that toggles between pressed and not pressed, bound to a <c>bool</c>. Tapping it flips <see cref="Value"/>.</summary>
public class KlyToggle : Border, IInputsFieldTarget
{
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(bool), typeof(KlyToggle), false, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyToggle)b).ShowValue((bool)o, (bool)v));
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyToggle), null, propertyChanged: (b, _, _) => ((KlyToggle)b).ShowLabel());
    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(View), typeof(KlyToggle), null, propertyChanged: (b, o, v) => ((KlyToggle)b).ShowIcon((View?)o, (View?)v));
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(ToggleVariant), typeof(KlyToggle), ToggleVariant.Outline, propertyChanged: (b, _, _) => ((KlyToggle)b).ApplyStyle());
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyToggle), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyToggle)b).ApplyStyle());
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyToggle), KlySize.Md, propertyChanged: (b, _, _) => ((KlyToggle)b).ApplyStyle());

    private readonly HorizontalStackLayout _row = new() { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
    private readonly Label _label = new() { FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.TailTruncation, VerticalOptions = LayoutOptions.Center, IsVisible = false };
    private readonly InputsFieldTaken _taken = new();

    /// <summary>Creates a toggle.</summary>
    public KlyToggle()
    {
        this.SetRadius(KlyTheme.RadiusOf(1));
        HorizontalOptions = LayoutOptions.Start;
        _row.Children.Add(_label);
        Content = _row;
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Press();
        GestureRecognizers.Add(tap);
        ApplyStyle();
        Describe();
    }

    /// <summary>Raised when the toggle is pressed or released, by the user or by code.</summary>
    public event EventHandler<KlyInputChangedEventArgs<bool>>? ValueChanged;

    /// <summary>Whether the toggle is pressed. Two-way bindable.</summary>
    public bool Value { get => (bool)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Text of the button.</summary>
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>A view drawn before the label, such as a <see cref="KlyIcon"/>.</summary>
    public View? Icon { get => (View?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Look of the toggle: <c>Outline</c>, <c>Soft</c> or <c>Ghost</c>.</summary>
    public ToggleVariant Variant { get => (ToggleVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Semantic color of the pressed state.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Size of the button, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>A tap flips <see cref="Value"/> unless a group handles the tap instead.</summary>
    internal bool AutoToggle { get; set; } = true;

    internal event EventHandler? Tapped;

    internal Label Text => _label;

    /// <summary>A tap by the user.</summary>
    internal void Press()
    {
        if (!IsEnabled) return;
        Tapped?.Invoke(this, EventArgs.Empty);
        if (AutoToggle) Value = !Value;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web toggle: disabled:opacity-60.
        if (propertyName == nameof(IsEnabled)) Opacity = IsEnabled ? 1 : 0.6;
    }

    void IInputsFieldTarget.ApplyField(InputsFieldState state)
    {
        _taken.TakeSize(this, SizeProperty, state.Size);
        SemanticProperties.SetHint(this, state.Hint);
    }

    private void ShowValue(bool old, bool value)
    {
        ApplyStyle();
        Describe();
        ValueChanged?.Invoke(this, new KlyInputChangedEventArgs<bool>(old, value));
    }

    private void ShowLabel()
    {
        _label.Text = Label;
        _label.IsVisible = !string.IsNullOrEmpty(Label);
        Describe();
    }

    private void ShowIcon(View? old, View? icon)
    {
        if (old is not null) _row.Children.Remove(old);
        if (icon is null) return;
        icon.VerticalOptions = LayoutOptions.Center;
        _row.Children.Insert(0, icon);
    }

    private void Describe() => SemanticProperties.SetDescription(this, string.IsNullOrEmpty(Label) ? null : Value ? Label + ", pressed" : Label);

    private void ApplyStyle()
    {
        var set = KlyTheme.Resolve(Color);
        var pressed = Value;
        var (background, text, stroke, thickness) = (Variant, pressed) switch
        {
            (ToggleVariant.Outline, false) => (KlyTheme.Background, KlyTheme.Text, KlyTheme.BorderAccented, 1d),
            (ToggleVariant.Outline, true) => (set.Fill.WithAlpha(0.15f), set.Text, set.Text.WithAlpha(0.75f), 1d),
            (ToggleVariant.Soft, false) => (KlyTheme.BackgroundElevated, KlyTheme.Text, KlyThemeColor.Transparent, 0d),
            (ToggleVariant.Soft, true) => (set.Fill.WithAlpha(0.2f), set.Text, set.Text.WithAlpha(0.75f), 1d),
            (ToggleVariant.Ghost, false) => (KlyThemeColor.Transparent, KlyTheme.TextMuted, KlyThemeColor.Transparent, 0d),
            (ToggleVariant.Ghost, true) => (set.Fill.WithAlpha(0.15f), set.Text, set.Text.WithAlpha(0.75f), 1d),
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null),
        };
        this.SetThemeColor(BackgroundColorProperty, background);
        this.SetStroke(stroke);
        StrokeThickness = thickness;
        _label.SetThemeColor(Microsoft.Maui.Controls.Label.TextColorProperty, text);

        // Padding, font and gap follow the web theme (px, py, text and gap of each size); the minimum height keeps a touch target usable, as for the button.
        var (padding, fontSize, gap, minHeight) = Size switch
        {
            KlySize.Xs => (new Thickness(8, 4), 12d, 4d, 28d),
            KlySize.Sm => (new Thickness(10, 6), 12d, 6d, 32d),
            KlySize.Md => (new Thickness(10, 6), 14d, 6d, 40d),
            KlySize.Lg => (new Thickness(12, 8), 14d, 8d, 44d),
            KlySize.Xl => (new Thickness(12, 8), 16d, 8d, 48d),
            _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, null),
        };
        Padding = padding;
        _label.FontSize = fontSize;
        _row.Spacing = gap;
        MinimumHeightRequest = minHeight;
    }
}
