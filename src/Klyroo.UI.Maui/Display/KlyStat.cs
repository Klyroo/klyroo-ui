using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>
/// A highlighted number with a title, a unit, a delta badge, a sparkline and a caption. <typeparamref name="TValue"/> is a built-in number type
/// (<see cref="sbyte"/> to <see cref="ulong"/>, <see cref="float"/>, <see cref="double"/>, <see cref="decimal"/>) or its nullable form, which shows a dash when it has no value.
/// Another type throws an <see cref="InvalidOperationException"/> that lists the supported ones.
/// </summary>
/// <typeparam name="TValue">The number type of the value and of its change.</typeparam>
public class KlyStat<TValue> : KlyBlockBase
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyStat<TValue>), null, propertyChanged: Update);
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(TValue), typeof(KlyStat<TValue>), default(TValue), propertyChanged: Update);
    /// <summary>Identifies <see cref="Format"/>.</summary>
    public static readonly BindableProperty FormatProperty = BindableProperty.Create(nameof(Format), typeof(string), typeof(KlyStat<TValue>), "#,0.##", propertyChanged: Update);
    /// <summary>Identifies <see cref="Culture"/>.</summary>
    public static readonly BindableProperty CultureProperty = BindableProperty.Create(nameof(Culture), typeof(CultureInfo), typeof(KlyStat<TValue>), null, propertyChanged: Update);
    /// <summary>Identifies <see cref="Unit"/>.</summary>
    public static readonly BindableProperty UnitProperty = BindableProperty.Create(nameof(Unit), typeof(string), typeof(KlyStat<TValue>), null, propertyChanged: Update);
    /// <summary>Identifies <see cref="Delta"/>.</summary>
    public static readonly BindableProperty DeltaProperty = BindableProperty.Create(nameof(Delta), typeof(TValue), typeof(KlyStat<TValue>), default(TValue), propertyChanged: Update);
    /// <summary>Identifies <see cref="DeltaFormat"/>.</summary>
    public static readonly BindableProperty DeltaFormatProperty = BindableProperty.Create(nameof(DeltaFormat), typeof(string), typeof(KlyStat<TValue>), "+#,0.##;-#,0.##;0", propertyChanged: Update);
    /// <summary>Identifies <see cref="DeltaUnit"/>.</summary>
    public static readonly BindableProperty DeltaUnitProperty = BindableProperty.Create(nameof(DeltaUnit), typeof(string), typeof(KlyStat<TValue>), null, propertyChanged: Update);
    /// <summary>Identifies <see cref="DeltaDirection"/>.</summary>
    public static readonly BindableProperty DeltaDirectionProperty = BindableProperty.Create(nameof(DeltaDirection), typeof(KlyDeltaDirection?), typeof(KlyStat<TValue>), null, propertyChanged: Update);
    /// <summary>Identifies <see cref="Trend"/>.</summary>
    public static readonly BindableProperty TrendProperty = BindableProperty.Create(nameof(Trend), typeof(IReadOnlyList<double>), typeof(KlyStat<TValue>), null, propertyChanged: Update);
    /// <summary>Identifies <see cref="Caption"/>.</summary>
    public static readonly BindableProperty CaptionProperty = BindableProperty.Create(nameof(Caption), typeof(string), typeof(KlyStat<TValue>), null, propertyChanged: Update);

    private readonly Label _title = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
    private readonly Label _value = DisplayParts.NewLabel(24, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.NoWrap);
    private readonly Label _unit = DisplayParts.NewLabel(12, KlyTheme.TextMuted, lineBreak: LineBreakMode.NoWrap);
    private readonly KlyBadge _delta = new() { Variant = KlyVariant.Subtle, Size = KlySize.Sm, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.End, IsVisible = false };
    private readonly DisplaySparkline _sparkline = new() { IsVisible = false };
    private readonly Label _caption = DisplayParts.NewLabel(12, KlyTheme.TextMuted);

    /// <summary>Creates a stat.</summary>
    public KlyStat() : base(KlySurface.Muted, 8)
    {
        DisplayNumber.EnsureSupported(typeof(TValue));
        _title.IsVisible = false;
        _unit.IsVisible = false;
        _unit.VerticalOptions = LayoutOptions.End;
        _unit.Margin = new Thickness(0, 0, 0, 3);
        _caption.IsVisible = false;

        var valueGroup = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center, Children = { _value, _unit } };
        var row = new Grid { ColumnSpacing = 8 };
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        row.Add(valueGroup, 0);
        row.Add(_delta, 1);

        Body.Add(_title);
        Body.Add(row);
        Body.Add(_sparkline);
        Body.Add(_caption);
        Refresh();
    }

    /// <summary>The label of the value.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>The value, formatted with <see cref="Format"/> and <see cref="Culture"/>. A dash when it is a nullable type with no value, or not finite.</summary>
    public TValue Value { get => (TValue)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>The .NET numeric format string of <see cref="Value"/> (<c>"#,0.##"</c> by default; <c>"C0"</c> shows the currency of <see cref="Culture"/>, <c>"P1"</c> a percentage).</summary>
    public string Format { get => (string)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>The culture that formats the numbers. Defaults to the culture of the current thread.</summary>
    public CultureInfo? Culture { get => (CultureInfo?)GetValue(CultureProperty); set => SetValue(CultureProperty, value); }

    /// <summary>The unit shown beside the value, as text.</summary>
    public string? Unit { get => (string?)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }

    /// <summary>The change shown as a badge beside the value. Its direction comes from its sign unless <see cref="DeltaDirection"/> is set. With a nullable type, no value means no badge; with a plain number type the default, zero, shows a flat badge.</summary>
    public TValue Delta { get => (TValue)GetValue(DeltaProperty); set => SetValue(DeltaProperty, value); }

    /// <summary>The format string of <see cref="Delta"/> (<c>"+#,0.##;-#,0.##;0"</c> by default, which always shows the sign).</summary>
    public string DeltaFormat { get => (string)GetValue(DeltaFormatProperty); set => SetValue(DeltaFormatProperty, value); }

    /// <summary>Text written after <see cref="Delta"/>, such as <c>"%"</c>.</summary>
    public string? DeltaUnit { get => (string?)GetValue(DeltaUnitProperty); set => SetValue(DeltaUnitProperty, value); }

    /// <summary>
    /// The direction of <see cref="Delta"/>, which only chooses the color of its badge: <see cref="KlyDeltaDirection.Up"/> (success), <see cref="KlyDeltaDirection.Down"/> (error) or <see cref="KlyDeltaDirection.Flat"/> (neutral).
    /// When <see langword="null"/>, it comes from the sign of <see cref="Delta"/>. Set it for a change where a drop is good news (a cost, an error rate).
    /// </summary>
    public KlyDeltaDirection? DeltaDirection { get => (KlyDeltaDirection?)GetValue(DeltaDirectionProperty); set => SetValue(DeltaDirectionProperty, value); }

    /// <summary>The series of the sparkline, drawn from two points or more.</summary>
    public IReadOnlyList<double>? Trend { get => (IReadOnlyList<double>?)GetValue(TrendProperty); set => SetValue(TrendProperty, value); }

    /// <summary>A line of smaller text under the value.</summary>
    public string? Caption { get => (string?)GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }

    /// <summary>The delta badge, for tests.</summary>
    internal KlyBadge DeltaBadge => _delta;

    internal DisplaySparkline Sparkline => _sparkline;

    internal string ValueText => DisplayNumber.Display(Value, Format, Culture ?? CultureInfo.CurrentCulture) ?? "—";

    internal string? DeltaText => DisplayNumber.Display(Delta, DeltaFormat, Culture ?? CultureInfo.CurrentCulture) is { } text ? text + DeltaUnit : null;

    internal KlyDeltaDirection EffectiveDirection => DeltaDirection ?? DisplayNumber.Sign(Delta) switch
    {
        > 0 => KlyDeltaDirection.Up,
        < 0 => KlyDeltaDirection.Down,
        _ => KlyDeltaDirection.Flat,
    };

    private static void Update(BindableObject bindable, object oldValue, object newValue) => ((KlyStat<TValue>)bindable).Refresh();

    private void Refresh()
    {
        DisplayParts.Show(_title, Title);
        _value.Text = ValueText;
        DisplayParts.Show(_unit, Unit);
        DisplayParts.Show(_caption, Caption);

        var delta = DeltaText;
        _delta.IsVisible = delta is not null;
        if (delta is not null)
        {
            _delta.Text = delta;
            _delta.Color = EffectiveDirection switch
            {
                KlyDeltaDirection.Down => KlyColor.Error,
                KlyDeltaDirection.Flat => KlyColor.Neutral,
                _ => KlyColor.Success,
            };
        }

        _sparkline.IsVisible = Trend is { Count: >= 2 };
        _sparkline.Points = Trend ?? [];
    }
}
