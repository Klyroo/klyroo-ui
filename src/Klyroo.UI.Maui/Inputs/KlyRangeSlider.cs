namespace Klyroo.UI.Maui;

/// <summary>
/// A slider with two thumbs, for a range of values. The platform has no such control, so it is drawn and moved by touch: the thumb nearest to the finger follows it.
/// The value is a list of two numbers, the lower one first; both are snapped to <see cref="Step"/> from <see cref="Min"/>.
/// </summary>
public class KlyRangeSlider : ThemedGraphicsView, IInputsFieldTarget
{
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(IReadOnlyList<double>), typeof(KlyRangeSlider), null, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyRangeSlider)b).ShowValue((IReadOnlyList<double>?)o, (IReadOnlyList<double>?)v));
    /// <summary>Identifies <see cref="Min"/>.</summary>
    public static readonly BindableProperty MinProperty = BindableProperty.Create(nameof(Min), typeof(double), typeof(KlyRangeSlider), 0d, propertyChanged: (b, _, _) => ((KlyRangeSlider)b).Invalidate());
    /// <summary>Identifies <see cref="Max"/>.</summary>
    public static readonly BindableProperty MaxProperty = BindableProperty.Create(nameof(Max), typeof(double), typeof(KlyRangeSlider), 100d, propertyChanged: (b, _, _) => ((KlyRangeSlider)b).Invalidate());
    /// <summary>Identifies <see cref="Step"/>.</summary>
    public static readonly BindableProperty StepProperty = BindableProperty.Create(nameof(Step), typeof(double), typeof(KlyRangeSlider), 1d, validateValue: (_, v) => (double)v > 0);
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlyRangeSlider), StackOrientation.Horizontal, propertyChanged: (b, _, _) => ((KlyRangeSlider)b).ApplyLayout());
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyRangeSlider), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyRangeSlider)b).Invalidate());
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyRangeSlider), KlySize.Md, propertyChanged: (b, _, _) => ((KlyRangeSlider)b).Invalidate());
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyRangeSlider), null, propertyChanged: (b, _, _) => ((KlyRangeSlider)b).Describe());

    private readonly InputsFieldTaken _taken = new();
    private int _dragging = -1;

    /// <summary>Creates a range slider from 0 to 100.</summary>
    public KlyRangeSlider()
    {
        StartInteraction += (_, e) => Begin(e.Touches[0]);
        DragInteraction += (_, e) => Drag(e.Touches[0]);
        EndInteraction += (_, _) => _dragging = -1;
        CancelInteraction += (_, _) => _dragging = -1;
        ApplyLayout();
        Describe();
    }

    /// <summary>Raised when the range changes, by the user or by code.</summary>
    public event EventHandler<KlyInputChangedEventArgs<IReadOnlyList<double>>>? ValueChanged;

    /// <summary>The lower and the upper value. When <c>null</c>, the thumbs rest on <see cref="Min"/> and <see cref="Max"/>. Two-way bindable.</summary>
    public IReadOnlyList<double>? Value { get => (IReadOnlyList<double>?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Smallest value of the track. Defaults to 0.</summary>
    public double Min { get => (double)GetValue(MinProperty); set => SetValue(MinProperty, value); }

    /// <summary>Largest value of the track. Defaults to 100.</summary>
    public double Max { get => (double)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    /// <summary>Distance between two values a thumb can take. Defaults to 1; a step of zero or less is ignored.</summary>
    public double Step { get => (double)GetValue(StepProperty); set => SetValue(StepProperty, value); }

    /// <summary><c>Horizontal</c>, with the lower value at the start, or <c>Vertical</c>, with the lower value at the bottom.</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Semantic color of the range between the thumbs and of the thumbs.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Size of the thumbs, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>What a screen reader says for the slider, such as its label.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    /// <summary>The lower value.</summary>
    public double Low => Value is { Count: > 0 } v ? v[0] : Min;

    /// <summary>The upper value.</summary>
    public double High => Value is { Count: > 1 } v ? v[1] : Max;

    /// <summary>The value under a touch: <paramref name="position"/> is the distance along the track from its start (its bottom for a vertical one).</summary>
    internal static double ValueAt(double position, double length, double thumb, double min, double max, double step)
    {
        var track = length - thumb;
        if (track <= 0 || max <= min) return min;
        var ratio = Math.Clamp((position - (thumb / 2)) / track, 0, 1);
        return KlySlider.Snap(min + (ratio * (max - min)), min, max, step);
    }

    /// <summary>Which thumb a touch takes, 0 for the lower and 1 for the upper: the nearest, and on a tie the one that can move toward the finger.</summary>
    internal static int ThumbNear(double value, double low, double high)
    {
        var toLow = Math.Abs(value - low);
        var toHigh = Math.Abs(value - high);
        if (toLow == toHigh) return value > high ? 1 : 0;
        return toLow < toHigh ? 0 : 1;
    }

    /// <summary>Moves one thumb, keeping the lower value below the upper one.</summary>
    internal void MoveThumb(int thumb, double value)
    {
        var low = Low;
        var high = High;
        if (thumb == 0) low = Math.Min(value, high);
        else high = Math.Max(value, low);
        if (low != Low || high != High || Value is null) Value = [low, high];
    }

    internal static double ThumbSize(KlySize size) => size switch
    {
        KlySize.Xs => 12,
        KlySize.Sm => 14,
        KlySize.Md => 16,
        KlySize.Lg => 20,
        KlySize.Xl => 24,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var set = KlyTheme.Resolve(Color);
        var horizontal = Orientation == StackOrientation.Horizontal;
        var length = horizontal ? dirtyRect.Width : dirtyRect.Height;
        var cross = (horizontal ? dirtyRect.Height : dirtyRect.Width) / 2;
        var thumb = (float)ThumbSize(Size);
        var radius = thumb / 2;
        var span = Max - Min;
        float PositionOf(double value) => radius + (span <= 0 ? 0 : (float)((value - Min) / span) * (length - thumb));
        RectF Along(float from, float to, float thickness) => horizontal
            ? new RectF(from, cross - (thickness / 2), to - from, thickness)
            : new RectF(cross - (thickness / 2), length - to, thickness, to - from);
        PointF Centre(float along) => horizontal ? new PointF(along, cross) : new PointF(cross, length - along);

        canvas.FillColor = KlyTheme.BackgroundAccented.Resolve();
        canvas.FillRoundedRectangle(Along(radius, length - radius, 6), 3);

        var low = PositionOf(Low);
        var high = PositionOf(High);
        canvas.FillColor = set.Fill.Resolve();
        canvas.FillRoundedRectangle(Along(low, high, 6), 3);
        canvas.StrokeColor = set.Text.Resolve();
        canvas.StrokeSize = 1;
        canvas.DrawRoundedRectangle(Along(low, high, 6), 3);

        foreach (var along in new[] { low, high })
        {
            var centre = Centre(along);
            canvas.FillColor = KlyTheme.Background.Resolve();
            canvas.FillCircle(centre, radius - 1);
            canvas.StrokeColor = set.Text.Resolve();
            canvas.StrokeSize = 2;
            canvas.DrawCircle(centre, radius - 1);
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web slider: has-disabled:opacity-60.
        if (propertyName == nameof(IsEnabled)) Opacity = IsEnabled ? 1 : 0.6;
    }

    void IInputsFieldTarget.ApplyField(InputsFieldState state)
    {
        _taken.TakeName(this, AccessibleNameProperty, state.Name);
        _taken.TakeSize(this, SizeProperty, state.Size);
        SemanticProperties.SetHint(this, state.Hint);
    }

    private void Begin(PointF touch)
    {
        if (!IsEnabled) return;
        var value = ValueAtTouch(touch);
        _dragging = ThumbNear(value, Low, High);
        MoveThumb(_dragging, value);
    }

    private void Drag(PointF touch)
    {
        if (_dragging < 0 || !IsEnabled) return;
        MoveThumb(_dragging, ValueAtTouch(touch));
    }

    private double ValueAtTouch(PointF touch)
    {
        var horizontal = Orientation == StackOrientation.Horizontal;
        var length = horizontal ? Width : Height;
        var along = horizontal ? touch.X : length - touch.Y;
        return ValueAt(along, length, ThumbSize(Size), Min, Max, Step);
    }

    private void ShowValue(IReadOnlyList<double>? old, IReadOnlyList<double>? value)
    {
        Describe();
        Invalidate();
        ValueChanged?.Invoke(this, new KlyInputChangedEventArgs<IReadOnlyList<double>>(old, value));
    }

    private void ApplyLayout()
    {
        // The touch target is at least 40 wide, whatever the size of the thumbs.
        if (Orientation == StackOrientation.Horizontal)
        {
            HeightRequest = 40;
            WidthRequest = -1;
            HorizontalOptions = LayoutOptions.Fill;
        }
        else
        {
            WidthRequest = 40;
            HeightRequest = 192;
            HorizontalOptions = LayoutOptions.Start;
        }
        Invalidate();
    }

    private void Describe()
    {
        var range = string.Create(System.Globalization.CultureInfo.CurrentCulture, $"{Low} - {High}");
        SemanticProperties.SetDescription(this, string.IsNullOrEmpty(AccessibleName) ? range : AccessibleName + ", " + range);
    }
}
