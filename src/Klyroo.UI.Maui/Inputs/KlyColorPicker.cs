namespace Klyroo.UI.Maui;

/// <summary>
/// A color picker with a saturation and brightness area, a hue bar and a hex field. It binds to a hex string such as <c>#3b5900</c>.
/// Touch the area or the bar to choose; type in the field and press return, or leave it, to set a hex value.
/// </summary>
public class KlyColorPicker : VerticalStackLayout, IInputsFieldTarget
{
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(string), typeof(KlyColorPicker), null, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyColorPicker)b).ShowValue((string?)o, (string?)v));
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(FieldVariant), typeof(KlyColorPicker), FieldVariant.Outline, propertyChanged: (b, _, v) => ((KlyColorPicker)b)._hex.Variant = (FieldVariant)v);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyColorPicker), KlyColor.Primary, propertyChanged: (b, _, v) => ((KlyColorPicker)b)._hex.Color = (KlyColor)v);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyColorPicker), KlySize.Md, propertyChanged: (b, _, v) => ((KlyColorPicker)b)._hex.Size = (KlySize)v);
    /// <summary>Identifies <see cref="Invalid"/>.</summary>
    public static readonly BindableProperty InvalidProperty = BindableProperty.Create(nameof(Invalid), typeof(bool), typeof(KlyColorPicker), false, propertyChanged: (b, _, v) => ((KlyColorPicker)b)._hex.Invalid = (bool)v);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyColorPicker), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyColorPicker)b, (string?)v));

    private readonly InputsColorArea _area = new();
    private readonly InputsHueBar _hue = new();
    private readonly Border _swatch = new() { WidthRequest = 32, HeightRequest = 32, StrokeThickness = 1, VerticalOptions = LayoutOptions.Center };
    private readonly KlyInput _hex = new() { Placeholder = "#000000", MaxLength = 7, AccessibleName = "Hex color", HorizontalOptions = LayoutOptions.Fill };
    private readonly InputsFieldTaken _taken = new();
    private double _h;
    private double _s;
    private double _v;
    private bool _picking;

    /// <summary>Creates a color picker.</summary>
    public KlyColorPicker()
    {
        Spacing = 12;
        MaximumWidthRequest = 256;
        HorizontalOptions = LayoutOptions.Fill;
        _swatch.SetRadius(KlyTheme.RadiusOf(1));
        _swatch.SetStroke(KlyTheme.BorderAccented);
        SemanticProperties.SetDescription(_area, "Saturation and brightness");
        SemanticProperties.SetDescription(_hue, "Hue");
        SemanticProperties.SetDescription(_swatch, "Selected color");

        var preview = new Grid { ColumnSpacing = 8 };
        preview.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        preview.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        preview.Add(_swatch, 0);
        preview.Add(_hex, 1);
        Children.Add(_area);
        Children.Add(_hue);
        Children.Add(preview);

        _area.Moved += (_, e) => Pick(_h, e.Saturation, e.Brightness);
        _hue.Moved += (_, hue) => Pick(hue, _s, _v);
        var entry = (Entry)_hex.Content!;
        entry.Completed += (_, _) => CommitText();
        entry.Unfocused += (_, _) => CommitText();
        Show();
    }

    /// <summary>Raised when the color changes, by the user or by code.</summary>
    public event EventHandler<KlyInputChangedEventArgs<string>>? ValueChanged;

    /// <summary>The color as <c>#rrggbb</c> (a <c>#rgb</c> value is accepted and read as <c>#rrggbb</c>), or <c>null</c> while none is chosen. Two-way bindable.</summary>
    public string? Value { get => (string?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Look of the hex field, as a <see cref="FieldVariant"/>.</summary>
    public FieldVariant Variant { get => (FieldVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Semantic color of the focus ring of the hex field.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Size of the hex field, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Draws the error ring, in the error color, around the hex field.</summary>
    public bool Invalid { get => (bool)GetValue(InvalidProperty); set => SetValue(InvalidProperty, value); }

    /// <summary>What a screen reader says for the picker, such as its label.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    internal InputsColorArea Area => _area;

    internal InputsHueBar HueBar => _hue;

    internal Border Swatch => _swatch;

    internal KlyInput HexField => _hex;

    /// <summary>Reads the hex field, as when it loses focus: a valid color becomes the value, any other text is replaced by the current color.</summary>
    internal void CommitText()
    {
        var hex = InputsColor.Normalize(_hex.Text);
        if (hex is null || hex == InputsColor.Normalize(Value)) Show();
        else Value = hex;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled))
        {
            _area.IsEnabled = IsEnabled;
            _hue.IsEnabled = IsEnabled;
            _hex.IsEnabled = IsEnabled;
            Opacity = IsEnabled ? 1 : 0.6;
        }
    }

    void IInputsFieldTarget.ApplyField(InputsFieldState state)
    {
        _taken.TakeInvalid(this, InvalidProperty, state.Invalid);
        _taken.TakeName(this, AccessibleNameProperty, state.Name);
        _taken.TakeSize(this, SizeProperty, state.Size);
        SemanticProperties.SetHint(this, state.Hint);
    }

    private void Pick(double hue, double saturation, double brightness)
    {
        _picking = true;
        (_h, _s, _v) = (hue, saturation, brightness);
        var hex = InputsColor.ToHex(hue, saturation, brightness);
        if (hex != InputsColor.Normalize(Value)) Value = hex;
        _picking = false;
        Show();
    }

    private void ShowValue(string? old, string? value)
    {
        // A color chosen on the area keeps its hue even when it is gray; one set by code takes its hue from the color.
        if (!_picking && InputsColor.Normalize(value) is { } hex) (_h, _s, _v) = InputsColor.ToHsv(hex);
        Show();
        ValueChanged?.Invoke(this, new KlyInputChangedEventArgs<string>(old, value));
    }

    private void Show()
    {
        var hex = InputsColor.Normalize(Value);
        _area.Show(_h, _s, _v);
        _hue.Show(_h);
        _swatch.BackgroundColor = InputsColor.ToColor(hex);
        if (_hex.Text != (hex ?? string.Empty)) _hex.Text = hex ?? string.Empty;
    }
}
