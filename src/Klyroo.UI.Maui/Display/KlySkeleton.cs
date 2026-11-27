namespace Klyroo.UI.Maui;

/// <summary>
/// A placeholder that shows where content is loading: a gray block that pulses while it is on screen. It is hidden from assistive technology:
/// announce the loading once for the whole region instead. Size it with <see cref="VisualElement.WidthRequest"/> and <see cref="VisualElement.HeightRequest"/> (16 high by default).
/// </summary>
public class KlySkeleton : Border
{
    private const string PulseName = "klyroo-skeleton-pulse";

    /// <summary>Identifies <see cref="Circle"/>.</summary>
    public static readonly BindableProperty CircleProperty = BindableProperty.Create(nameof(Circle), typeof(bool), typeof(KlySkeleton), false, propertyChanged: (b, _, _) => ((KlySkeleton)b).ApplyStyle());

    /// <summary>Creates a skeleton.</summary>
    public KlySkeleton()
    {
        StrokeThickness = 0;
        HeightRequest = 16;
        HorizontalOptions = LayoutOptions.Fill;
        AutomationProperties.SetIsInAccessibleTree(this, false);
        this.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundAccented);
        ApplyStyle();
        HandlerChanged += (_, _) =>
        {
            if (Handler is null) StopPulse();
            else StartPulse();
        };
    }

    /// <summary>Draws a circle instead of a rounded block, for the placeholder of an avatar: give it the same width and height.</summary>
    public bool Circle { get => (bool)GetValue(CircleProperty); set => SetValue(CircleProperty, value); }

    /// <summary>Whether the pulse runs: only while the skeleton has a native view, so a skeleton that left the page costs nothing.</summary>
    internal bool IsPulsing { get; private set; }

    private void ApplyStyle() => this.SetRadius(Circle ? 9999 : KlyTheme.RadiusOf(1));

    private void StartPulse()
    {
        if (IsPulsing) return;
        IsPulsing = true;
        // The same pulse as the web skeleton (animate-pulse): the opacity goes down to 50% and back every two seconds.
        var pulse = new Animation();
        pulse.Add(0, 0.5, new Animation(v => Opacity = v, 1, 0.5, Easing.CubicInOut));
        pulse.Add(0.5, 1, new Animation(v => Opacity = v, 0.5, 1, Easing.CubicInOut));
        pulse.Commit(this, PulseName, 16, 2000, repeat: () => true);
    }

    private void StopPulse()
    {
        if (!IsPulsing) return;
        IsPulsing = false;
        this.AbortAnimation(PulseName);
        Opacity = 1;
    }
}
