namespace Klyroo.UI.Maui;

/// <summary>A star rating, bound to a <c>double</c> from 0 (nothing chosen) to <see cref="Max"/>, with half steps and a read-only mode. Tapping a star chooses it; with <see cref="AllowHalf"/> the left half of a star chooses half a star less.</summary>
public class KlyInputRating : Border, IInputsFieldTarget
{
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(double), typeof(KlyInputRating), 0d, BindingMode.TwoWay, coerceValue: (b, v) => ((KlyInputRating)b).Snap((double)v), propertyChanged: (b, o, v) => ((KlyInputRating)b).ShowValue((double)o, (double)v));
    /// <summary>Identifies <see cref="Max"/>.</summary>
    public static readonly BindableProperty MaxProperty = BindableProperty.Create(nameof(Max), typeof(int), typeof(KlyInputRating), 5, validateValue: (_, v) => (int)v >= 1, propertyChanged: (b, _, _) => ((KlyInputRating)b).Rebuild());
    /// <summary>Identifies <see cref="AllowHalf"/>.</summary>
    public static readonly BindableProperty AllowHalfProperty = BindableProperty.Create(nameof(AllowHalf), typeof(bool), typeof(KlyInputRating), false, propertyChanged: (b, _, _) => ((KlyInputRating)b).Rebuild());
    /// <summary>Identifies <see cref="ReadOnly"/>.</summary>
    public static readonly BindableProperty ReadOnlyProperty = BindableProperty.Create(nameof(ReadOnly), typeof(bool), typeof(KlyInputRating), false, propertyChanged: (b, _, _) => ((KlyInputRating)b).Describe());
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyInputRating), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyInputRating)b).Rebuild());
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyInputRating), KlySize.Md, propertyChanged: (b, _, _) => ((KlyInputRating)b).Rebuild());
    /// <summary>Identifies <see cref="Invalid"/>.</summary>
    public static readonly BindableProperty InvalidProperty = BindableProperty.Create(nameof(Invalid), typeof(bool), typeof(KlyInputRating), false, propertyChanged: (b, _, _) => ((KlyInputRating)b).ApplyStyle());
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyInputRating), null, propertyChanged: (b, _, _) => ((KlyInputRating)b).Describe());

    private readonly HorizontalStackLayout _stars = new() { Spacing = 0 };
    private readonly InputsFieldTaken _taken = new();

    /// <summary>Creates a rating of five stars.</summary>
    public KlyInputRating()
    {
        HorizontalOptions = LayoutOptions.Start;
        Padding = new Thickness(2);
        this.SetRadius(4);
        Content = _stars;
        Rebuild();
        ApplyStyle();
    }

    /// <summary>Raised when the rating changes, by the user or by code.</summary>
    public event EventHandler<KlyInputChangedEventArgs<double>>? ValueChanged;

    /// <summary>The rating, from 0 to <see cref="Max"/> in steps of 1, or of 0.5 with <see cref="AllowHalf"/>. Two-way bindable.</summary>
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Number of stars. Defaults to 5.</summary>
    public int Max { get => (int)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    /// <summary>Lets a star be chosen by halves (0.5, 1, 1.5...).</summary>
    public bool AllowHalf { get => (bool)GetValue(AllowHalfProperty); set => SetValue(AllowHalfProperty, value); }

    /// <summary>Shows the value without letting it change.</summary>
    public bool ReadOnly { get => (bool)GetValue(ReadOnlyProperty); set => SetValue(ReadOnlyProperty, value); }

    /// <summary>Semantic color of the filled stars.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Size of the stars, from <c>Xs</c> (16) to <c>Xl</c> (32).</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Draws the error outline, in the error color, around the stars.</summary>
    public bool Invalid { get => (bool)GetValue(InvalidProperty); set => SetValue(InvalidProperty, value); }

    /// <summary>What a screen reader says for the rating, such as its label, before the value.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    internal IReadOnlyList<InputsStar> Stars => [.. _stars.Children.OfType<InputsStar>()];

    internal static double DimensionOf(KlySize size) => size switch
    {
        KlySize.Xs => 16,
        KlySize.Sm => 20,
        KlySize.Md => 24,
        KlySize.Lg => 28,
        KlySize.Xl => 32,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    /// <summary>A tap on a star: <paramref name="index"/> counts from 1 and <paramref name="left"/> says the tap fell on its left half.</summary>
    internal void Tap(int index, bool left)
    {
        if (!IsEnabled || ReadOnly) return;
        Value = AllowHalf && left ? index - 0.5 : index;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web rating: has-disabled:opacity-60.
        if (propertyName == nameof(IsEnabled)) Opacity = IsEnabled ? 1 : 0.6;
    }

    void IInputsFieldTarget.ApplyField(InputsFieldState state)
    {
        _taken.TakeInvalid(this, InvalidProperty, state.Invalid);
        _taken.TakeName(this, AccessibleNameProperty, state.Name);
        _taken.TakeSize(this, SizeProperty, state.Size);
        SemanticProperties.SetHint(this, state.Hint);
    }

    private double Snap(double value)
    {
        var step = AllowHalf ? 0.5 : 1;
        return Math.Clamp(Math.Round(value / step) * step, 0, Max);
    }

    private void ShowValue(double old, double value)
    {
        Paint();
        Describe();
        ValueChanged?.Invoke(this, new KlyInputChangedEventArgs<double>(old, value));
    }

    private void Paint()
    {
        var index = 1;
        foreach (var star in _stars.Children.OfType<InputsStar>())
        {
            star.Filled = Value >= index ? 1 : Value >= index - 0.5 ? 0.5 : 0;
            index++;
        }
    }

    private void Describe()
    {
        var text = string.Create(System.Globalization.CultureInfo.CurrentCulture, $"{Value} out of {Max}");
        SemanticProperties.SetDescription(this, string.IsNullOrEmpty(AccessibleName) ? text : AccessibleName + ", " + text);
    }

    private void ApplyStyle()
    {
        this.SetStroke(Invalid ? KlyTheme.Resolve(KlyColor.Error).Fill : KlyThemeColor.Transparent);
        StrokeThickness = Invalid ? 2 : 0;
    }

    private void Rebuild()
    {
        _stars.Children.Clear();
        var dimension = DimensionOf(Size);
        for (var i = 1; i <= Max; i++)
        {
            var star = new InputsStar { Color = Color, Dimension = dimension };
            var index = i;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, e) =>
            {
                var position = e.GetPosition(star);
                Tap(index, position is { } point && point.X < dimension / 2);
            };
            star.GestureRecognizers.Add(tap);
            _stars.Children.Add(star);
        }
        var snapped = Snap(Value);
        if (snapped != Value) Value = snapped;
        Paint();
        Describe();
    }
}
