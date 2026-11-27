namespace Klyroo.UI.Maui;

/// <summary>
/// A tap target: a transparent grid of at least 40 by 40 around a visual that can be smaller, so a small indicator or page number stays easy to hit.
/// The visual is a <see cref="Border"/> the control styles; a disabled button dims like the other controls.
/// </summary>
internal sealed class NavigationButton : Grid
{
    /// <summary>The smallest side of the tap target.</summary>
    public const double MinTarget = 40;

    /// <summary>Creates a tap target.</summary>
    public NavigationButton(double minimum = MinTarget)
    {
        MinimumWidthRequest = minimum;
        MinimumHeightRequest = minimum;
        BackgroundColor = Colors.Transparent;
        Face = new Border { StrokeThickness = 0, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        Children.Add(Face);
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Press();
        GestureRecognizers.Add(tap);
    }

    /// <summary>The drawn part: put the content in it and style it.</summary>
    public Border Face { get; }

    /// <summary>Raised when the user taps an enabled button.</summary>
    public event EventHandler? Clicked;

    /// <summary>Taps the button, as the user does.</summary>
    public void Press()
    {
        if (IsEnabled) Clicked?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web controls: disabled:opacity-60.
        if (propertyName == nameof(IsEnabled)) Opacity = IsEnabled ? 1 : 0.6;
    }
}
