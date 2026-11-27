using Microsoft.Maui.Layouts;

using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>
/// A progress bar, determinate or indeterminate, with optional steps and a percentage above it.
/// A <see cref="Value"/> of <see langword="null"/> makes the bar indeterminate: it moves across the track while the control is on screen.
/// </summary>
public class KlyProgress : VerticalStackLayout
{
    private const string SlideName = "klyroo-progress-slide";
    private const string GrowName = "klyroo-progress-grow";

    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(double?), typeof(KlyProgress), null, propertyChanged: (b, _, _) => ((KlyProgress)b).Refresh());
    /// <summary>Identifies <see cref="Max"/>.</summary>
    public static readonly BindableProperty MaxProperty = BindableProperty.Create(nameof(Max), typeof(double), typeof(KlyProgress), 100d, propertyChanged: (b, _, _) => ((KlyProgress)b).Refresh());
    /// <summary>Identifies <see cref="Steps"/>.</summary>
    public static readonly BindableProperty StepsProperty = BindableProperty.Create(nameof(Steps), typeof(IReadOnlyList<string>), typeof(KlyProgress), null, propertyChanged: (b, _, _) => ((KlyProgress)b).Refresh());
    /// <summary>Identifies <see cref="Status"/>.</summary>
    public static readonly BindableProperty StatusProperty = BindableProperty.Create(nameof(Status), typeof(bool), typeof(KlyProgress), false, propertyChanged: (b, _, _) => ((KlyProgress)b).Refresh());
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyProgress), null, propertyChanged: (b, _, _) => ((KlyProgress)b).Refresh());
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyProgress), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyProgress)b).ApplyStyle());
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyProgress), KlySize.Md, propertyChanged: (b, _, _) => ((KlyProgress)b).ApplyStyle());
    /// <summary>Identifies <see cref="Animation"/>.</summary>
    public static readonly BindableProperty AnimationProperty = BindableProperty.Create(nameof(Animation), typeof(KlyProgressAnimation), typeof(KlyProgress), KlyProgressAnimation.Carousel, propertyChanged: (b, _, _) => ((KlyProgress)b).RestartSlide());

    private readonly Label _status = DisplayParts.NewLabel(14, KlyTheme.TextMuted);
    private readonly Border _track = new() { StrokeThickness = 1, Padding = 0 };
    private readonly AbsoluteLayout _plot = new();
    private readonly Border _indicator = new() { StrokeThickness = 1, Padding = 0 };
    private readonly Grid _steps = new();
    private double _shown;

    /// <summary>Creates a progress bar.</summary>
    public KlyProgress()
    {
        Spacing = 8;
        HorizontalOptions = LayoutOptions.Fill;
        _status.HorizontalTextAlignment = TextAlignment.End;
        _track.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundElevated);
        _track.SetStroke(KlyTheme.BorderAccented);
        _track.SetRadius(9999);
        _indicator.SetRadius(9999);
        AbsoluteLayout.SetLayoutFlags(_indicator, AbsoluteLayoutFlags.SizeProportional);
        _plot.Add(_indicator);
        _track.Content = _plot;
        AutomationProperties.SetIsInAccessibleTree(_status, false);
        Add(_status);
        Add(_track);
        Add(_steps);
        HandlerChanged += (_, _) => RestartSlide();
        ApplyStyle();
        Refresh();
    }

    /// <summary>
    /// Current value between 0 and <see cref="Max"/>. <see langword="null"/> makes the bar indeterminate, for work of unknown duration.
    /// With <see cref="Steps"/> it is the index of the current step.
    /// </summary>
    public double? Value { get => (double?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Largest value (100 by default). Ignored when <see cref="Steps"/> is set: the maximum is then the index of the last step.</summary>
    public double Max { get => (double)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    /// <summary>Labels of the steps shown under the bar. <see cref="Value"/> is the index of the current step.</summary>
    public IReadOnlyList<string>? Steps { get => (IReadOnlyList<string>?)GetValue(StepsProperty); set => SetValue(StepsProperty, value); }

    /// <summary>Shows the percentage above the bar.</summary>
    public bool Status { get => (bool)GetValue(StatusProperty); set => SetValue(StatusProperty, value); }

    /// <summary>What a screen reader says for the bar. Defaults to <c>Progress</c>.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    /// <summary>Semantic color of the bar.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Size of the bar, from <c>Xs</c> (4 high) to <c>Xl</c> (16 high).</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Motion of the indeterminate bar.</summary>
    public KlyProgressAnimation Animation { get => (KlyProgressAnimation)GetValue(AnimationProperty); set => SetValue(AnimationProperty, value); }

    internal bool IsIndeterminate => Value is null;

    /// <summary>The largest value: the index of the last step with <see cref="Steps"/>, else <see cref="Max"/>.</summary>
    internal double MaxValue => Steps is { Count: > 0 } ? Math.Max(Steps.Count - 1, 1) : Math.Max(Max, double.Epsilon);

    internal double Current => Math.Clamp(Value ?? 0, 0, MaxValue);

    /// <summary>The share of the bar that is filled, from 0 to 100.</summary>
    internal double Percent => Math.Round(Current / MaxValue * 100, 1);

    /// <summary>The value as a screen reader says it: the label of the current step, or the percentage. Nothing while indeterminate.</summary>
    internal string? ValueText =>
        IsIndeterminate ? null
        : Steps is { Count: > 0 } steps ? steps[Math.Min((int)Math.Round(Current), steps.Count - 1)]
        : $"{Percent.ToString("0.##", CultureInfo.InvariantCulture)}%";

    /// <summary>The width of the filled part of the bar, as a share of the track (0 to 1), now.</summary>
    internal double ShownFraction => _shown;

    internal string StepState(int index)
    {
        var current = (int)Math.Round(Current);
        return IsIndeterminate ? "inactive" : index < current ? "completed" : index == current ? "active" : "inactive";
    }

    private void ApplyStyle()
    {
        var set = KlyTheme.Resolve(Color);
        _indicator.SetThemeColor(BackgroundColorProperty, set.Fill);
        _indicator.SetStroke(set.Text);
        var (height, statusSize) = Size switch
        {
            KlySize.Xs => (4d, 12d),
            KlySize.Sm => (6d, 12d),
            KlySize.Md => (8d, 14d),
            KlySize.Lg => (12d, 14d),
            KlySize.Xl => (16d, 16d),
            _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, null),
        };
        _track.HeightRequest = height;
        _status.FontSize = statusSize;
    }

    private void Refresh()
    {
        var indeterminate = IsIndeterminate;
        _status.IsVisible = Status && !indeterminate;
        _status.Text = $"{Percent.ToString("0.##", CultureInfo.InvariantCulture)}%";

        SemanticProperties.SetDescription(this, AccessibleName ?? "Progress");
        SemanticProperties.SetHint(this, ValueText);

        _steps.Children.Clear();
        _steps.ColumnDefinitions.Clear();
        _steps.IsVisible = Steps is { Count: > 0 };
        if (Steps is { Count: > 0 } steps)
        {
            for (var i = 0; i < steps.Count; i++)
            {
                _steps.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                var state = StepState(i);
                var label = DisplayParts.NewLabel(14, state == "active" ? KlyTheme.Resolve(Color).Text : state == "completed" ? KlyTheme.Text : KlyTheme.TextMuted, state == "active" ? FontAttributes.Bold : FontAttributes.None);
                label.Text = steps[i];
                label.HorizontalOptions = i == 0 ? LayoutOptions.Start : i == steps.Count - 1 ? LayoutOptions.End : LayoutOptions.Center;
                _steps.Add(label, i);
            }
        }

        if (indeterminate)
        {
            // The indicator is 40% of the track and slides (see RestartSlide).
            SetFraction(0.4);
        }
        else
        {
            AnimateFraction(Percent / 100);
        }
        RestartSlide();
    }

    private void SetFraction(double fraction)
    {
        _shown = fraction;
        AbsoluteLayout.SetLayoutBounds(_indicator, new Rect(0, 0, fraction, 1));
    }

    // The width moves to its new value in 200 ms (transition-[width]); a control with no native view just takes it.
    private void AnimateFraction(double target)
    {
        this.AbortAnimation(GrowName);
        if (Handler is null)
        {
            SetFraction(target);
            return;
        }
        var from = _shown;
        new Microsoft.Maui.Controls.Animation(v => SetFraction(v), from, target, Easing.CubicOut).Commit(this, GrowName, 16, 200);
    }

    private void RestartSlide()
    {
        this.AbortAnimation(SlideName);
        _indicator.TranslationX = 0;
        if (!IsIndeterminate || Handler is null) return;

        // Carousel: the bar enters from the left of the track and leaves on the right, then starts again. Swing: it goes to and fro.
        // The distances are shares of the track, read at each frame because the track has no width until it is laid out.
        var carousel = Animation == KlyProgressAnimation.Carousel;
        var slide = new Microsoft.Maui.Controls.Animation(v => _indicator.TranslationX = (carousel ? -0.4 + 1.4 * v : 0.6 * v) * _track.Width, 0, 1, Easing.SinInOut);
        if (carousel)
        {
            slide.Commit(this, SlideName, 16, 1500, repeat: () => true);
        }
        else
        {
            var swing = new Microsoft.Maui.Controls.Animation();
            swing.Add(0, 0.5, new Microsoft.Maui.Controls.Animation(v => _indicator.TranslationX = 0.6 * v * _track.Width, 0, 1, Easing.SinInOut));
            swing.Add(0.5, 1, new Microsoft.Maui.Controls.Animation(v => _indicator.TranslationX = 0.6 * v * _track.Width, 1, 0, Easing.SinInOut));
            swing.Commit(this, SlideName, 16, 2000, repeat: () => true);
        }
    }
}
