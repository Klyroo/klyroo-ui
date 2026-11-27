namespace Klyroo.UI.Maui;

/// <summary>A slider with one thumb: the native <see cref="Slider"/> in the colors of Klyroo.UI, with limits and a step. The value is snapped to <see cref="Step"/> from <see cref="Min"/>.</summary>
public class KlySlider : ContentView, IInputsFieldTarget
{
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(double), typeof(KlySlider), 0d, BindingMode.TwoWay, coerceValue: (b, v) => ((KlySlider)b).Snap((double)v), propertyChanged: (b, o, v) => ((KlySlider)b).ShowValue((double)o, (double)v));
    /// <summary>Identifies <see cref="Min"/>.</summary>
    public static readonly BindableProperty MinProperty = BindableProperty.Create(nameof(Min), typeof(double), typeof(KlySlider), 0d, propertyChanged: (b, _, _) => ((KlySlider)b).ShowRange());
    /// <summary>Identifies <see cref="Max"/>.</summary>
    public static readonly BindableProperty MaxProperty = BindableProperty.Create(nameof(Max), typeof(double), typeof(KlySlider), 100d, propertyChanged: (b, _, _) => ((KlySlider)b).ShowRange());
    /// <summary>Identifies <see cref="Step"/>.</summary>
    public static readonly BindableProperty StepProperty = BindableProperty.Create(nameof(Step), typeof(double), typeof(KlySlider), 1d, validateValue: (_, v) => (double)v > 0, propertyChanged: (b, _, _) => ((KlySlider)b).Resnap());
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlySlider), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlySlider)b).ApplyStyle());
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlySlider), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription(((KlySlider)b)._slider, (string?)v));

    private readonly Slider _slider = new() { MinimumHeightRequest = 40, VerticalOptions = LayoutOptions.Center };
    private readonly InputsFieldTaken _taken = new();

    /// <summary>Creates a slider from 0 to 100.</summary>
    public KlySlider()
    {
        _slider.Minimum = 0;
        _slider.Maximum = 100;
        _slider.ValueChanged += (_, e) => Value = e.NewValue;
        HorizontalOptions = LayoutOptions.Fill;
        Content = _slider;
        ApplyStyle();
    }

    /// <summary>Raised when the value changes, by the user or by code.</summary>
    public event EventHandler<ValueChangedEventArgs>? ValueChanged;

    /// <summary>The value, between <see cref="Min"/> and <see cref="Max"/> and a multiple of <see cref="Step"/> from <see cref="Min"/>. Two-way bindable.</summary>
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Smallest value of the track. Defaults to 0.</summary>
    public double Min { get => (double)GetValue(MinProperty); set => SetValue(MinProperty, value); }

    /// <summary>Largest value of the track. Defaults to 100.</summary>
    public double Max { get => (double)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    /// <summary>Distance between two values the thumb can take. Defaults to 1; a step of zero or less is ignored.</summary>
    public double Step { get => (double)GetValue(StepProperty); set => SetValue(StepProperty, value); }

    /// <summary>Semantic color of the filled part of the track and of the thumb.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>What a screen reader says for the slider, such as its label.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    internal Slider Native => _slider;

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web slider: has-disabled:opacity-60.
        if (propertyName == nameof(IsEnabled))
        {
            _slider.IsEnabled = IsEnabled;
            Opacity = IsEnabled ? 1 : 0.6;
        }
    }

    void IInputsFieldTarget.ApplyField(InputsFieldState state)
    {
        _taken.TakeName(this, AccessibleNameProperty, state.Name);
        SemanticProperties.SetHint(_slider, state.Hint);
    }

    /// <summary>The value limited to the track and moved to the nearest step.</summary>
    internal static double Snap(double value, double min, double max, double step)
    {
        if (max <= min) return min;
        var snapped = min + (Math.Round((value - min) / step) * step);
        // A step that does not divide the track ends at the largest multiple below Max, as a range input does.
        if (snapped > max) snapped -= step;
        return Math.Round(Math.Clamp(snapped, min, max), 10);
    }

    private double Snap(double value) => Snap(value, Min, Max, Step);

    private void ShowRange()
    {
        // The native slider throws when its minimum passes its maximum, so the limit that moves away from the other goes first.
        if (Max > _slider.Maximum)
        {
            _slider.Maximum = Max;
            _slider.Minimum = Min;
        }
        else
        {
            _slider.Minimum = Min;
            _slider.Maximum = Max;
        }
        Resnap();
        if (_slider.Value != Value) _slider.Value = Value;
    }

    /// <summary>Moves the value to the nearest step inside the track, after the track or the step changed.</summary>
    private void Resnap()
    {
        var snapped = Snap(Value);
        if (snapped != Value) Value = snapped;
    }

    private void ShowValue(double old, double value)
    {
        if (_slider.Value != value) _slider.Value = value;
        ValueChanged?.Invoke(this, new ValueChangedEventArgs(old, value));
    }

    private void ApplyStyle()
    {
        var set = KlyTheme.Resolve(Color);
        // The text color, not the fill: a light fill such as the lime primary has no contrast against the page.
        _slider.SetThemeColor(Slider.MinimumTrackColorProperty, set.Text);
        _slider.SetThemeColor(Slider.ThumbColorProperty, set.Text);
        _slider.SetThemeColor(Slider.MaximumTrackColorProperty, KlyTheme.BackgroundAccented);
    }
}
