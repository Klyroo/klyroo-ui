namespace Klyroo.UI.Maui;

/// <summary>
/// Frame shared by the text fields: a border and a background from <see cref="Variant"/>, a padding and a font from <see cref="Size"/>,
/// a focus ring in <see cref="Color"/> and an error ring while <see cref="Invalid"/>. The native control inside does the editing.
/// </summary>
public abstract class KlyFieldBase : Border
{
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyFieldBase), KlyColor.Primary, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(FieldVariant), typeof(KlyFieldBase), FieldVariant.Outline, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyFieldBase), KlySize.Md, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Invalid"/>.</summary>
    public static readonly BindableProperty InvalidProperty = BindableProperty.Create(nameof(Invalid), typeof(bool), typeof(KlyFieldBase), false, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyFieldBase), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription(((KlyFieldBase)b)._control, (string?)v));

    private readonly View _control;
    private bool _focused;

    /// <summary>Creates the frame around a native control.</summary>
    /// <param name="control">The control that edits the value.</param>
    protected KlyFieldBase(View control)
    {
        _control = control;
        _control.BackgroundColor = Colors.Transparent;
        _control.Focused += (_, _) => SetFocused(true);
        _control.Unfocused += (_, _) => SetFocused(false);
        _control.HandlerChanged += (_, _) => PlatformTweaks.RemoveNativeFieldChrome(_control);
        Content = control;
        this.SetRadius(KlyTheme.RadiusOf(1));
        HorizontalOptions = LayoutOptions.Fill;
    }

    /// <summary>Semantic color of the focus ring.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Look of the field: <c>Outline</c>, <c>Soft</c>, <c>Subtle</c>, <c>Ghost</c> or <c>None</c>.</summary>
    public FieldVariant Variant { get => (FieldVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Size of the field, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Draws the error ring, in the error color, around the field.</summary>
    public bool Invalid { get => (bool)GetValue(InvalidProperty); set => SetValue(InvalidProperty, value); }

    /// <summary>What a screen reader says for the field, such as its label. The placeholder alone is not read once the field has a value.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    /// <summary>Applies the text color, the placeholder color and the font size to the native control.</summary>
    /// <param name="text">Color of the value.</param>
    /// <param name="placeholder">Color of the placeholder.</param>
    /// <param name="fontSize">Font size of the value.</param>
    protected abstract void StyleText(KlyThemeColor text, KlyThemeColor placeholder, double fontSize);

    /// <summary>Applies the style of the frame and of the native control. Derived classes call it once from their constructor, after they set up the control.</summary>
    protected void ApplyStyle()
    {
        var set = KlyTheme.Resolve(Color);
        var (background, ring) = Variant switch
        {
            FieldVariant.Outline => (KlyTheme.Background, KlyTheme.BorderAccented),
            FieldVariant.Soft => (KlyTheme.BackgroundElevated.WithAlpha(0.5f), KlyThemeColor.Transparent),
            FieldVariant.Subtle => (KlyTheme.BackgroundElevated, KlyTheme.BorderAccented),
            FieldVariant.Ghost or FieldVariant.None => (KlyThemeColor.Transparent, KlyThemeColor.Transparent),
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null),
        };
        this.SetThemeColor(BackgroundColorProperty, background);

        // The error ring wins over the focus ring. A field with no border still shows both, except the variant that asks for no ring at all.
        if (Invalid)
        {
            this.SetStroke(KlyTheme.Resolve(KlyColor.Error).Fill);
            StrokeThickness = 2;
        }
        else if (_focused && Variant != FieldVariant.None)
        {
            this.SetStroke(set.Text);
            StrokeThickness = 2;
        }
        else
        {
            this.SetStroke(ring);
            StrokeThickness = ring == KlyThemeColor.Transparent ? 0 : 1;
        }

        var (padding, fontSize) = Size switch
        {
            KlySize.Xs => (new Thickness(8, 4), 12d),
            KlySize.Sm => (new Thickness(10, 6), 12d),
            KlySize.Md => (new Thickness(10, 6), 14d),
            KlySize.Lg => (new Thickness(12, 8), 14d),
            KlySize.Xl => (new Thickness(12, 8), 16d),
            _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, null),
        };
        Padding = padding;
        StyleText(KlyTheme.Text, KlyTheme.TextDimmed, fontSize);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web field: disabled:opacity-60.
        if (propertyName == nameof(IsEnabled))
        {
            _control.IsEnabled = IsEnabled;
            Opacity = IsEnabled ? 1 : 0.6;
        }
    }

    private static void Restyle(BindableObject bindable, object oldValue, object newValue) => ((KlyFieldBase)bindable).ApplyStyle();

    private void SetFocused(bool focused)
    {
        _focused = focused;
        ApplyStyle();
    }
}
