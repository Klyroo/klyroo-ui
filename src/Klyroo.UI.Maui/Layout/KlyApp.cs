namespace Klyroo.UI.Maui;

/// <summary>
/// The root of an app: the page background of the theme, the safe area of the device (notch, status bar, gesture bar, keyboard) and the color mode.
/// Place it once as the content of the page, around the header, the main area and the footer. Set <see cref="VisualElement.FlowDirection"/> for a right-to-left language.
/// </summary>
public class KlyApp : ContentView
{
    /// <summary>Identifies <see cref="SafeArea"/>.</summary>
    public static readonly BindableProperty SafeAreaProperty = BindableProperty.Create(nameof(SafeArea), typeof(bool), typeof(KlyApp), true, propertyChanged: (b, _, _) => ((KlyApp)b).ApplySafeArea());
    /// <summary>Identifies <see cref="ColorMode"/>.</summary>
    public static readonly BindableProperty ColorModeProperty = BindableProperty.Create(nameof(ColorMode), typeof(AppTheme), typeof(KlyApp), AppTheme.Unspecified, propertyChanged: (b, _, _) => ((KlyApp)b).ApplyColorMode());

    /// <summary>Creates the root of an app.</summary>
    public KlyApp()
    {
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Fill;
        this.SetThemeColor(BackgroundColorProperty, KlyTheme.Background);
        // A mode set before the application existed is applied once the app shows the control. The default leaves the theme of the app alone.
        Loaded += (_, _) =>
        {
            if (ColorMode != AppTheme.Unspecified) ApplyColorMode();
        };
        ApplySafeArea();
    }

    /// <summary>
    /// Keeps the content out of the system bars, the display cutout and the soft keyboard (default). Turn it off for a full-bleed app that handles the insets itself.
    /// The other layout controls leave the safe area to this one, so it is applied once.
    /// </summary>
    public bool SafeArea { get => (bool)GetValue(SafeAreaProperty); set => SetValue(SafeAreaProperty, value); }

    /// <summary>
    /// The color mode of the whole app: <c>Light</c> or <c>Dark</c> forces it, <c>Unspecified</c> (default) follows the system.
    /// It sets <c>Application.UserAppTheme</c>, so every Klyroo control switches live.
    /// </summary>
    public AppTheme ColorMode { get => (AppTheme)GetValue(ColorModeProperty); set => SetValue(ColorModeProperty, value); }

    private void ApplySafeArea() => SafeAreaEdges = SafeArea ? SafeAreaEdges.All : SafeAreaEdges.None;

    private void ApplyColorMode()
    {
        if (Application.Current is { } app && app.UserAppTheme != ColorMode) app.UserAppTheme = ColorMode;
    }
}
