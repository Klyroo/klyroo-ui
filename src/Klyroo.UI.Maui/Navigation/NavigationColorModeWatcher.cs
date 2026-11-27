namespace Klyroo.UI.Maui;

/// <summary>
/// Calls an action when the color mode changes, while a control is on screen: through <see cref="KlyColorMode.Changed"/> and through the theme of the app (the system
/// setting can flip it). It subscribes when the control gets a handler and lets go when it loses it, so a control that is gone is not kept alive.
/// </summary>
internal sealed class NavigationColorModeWatcher
{
    private readonly Action _sync;
    private Application? _app;

    public NavigationColorModeWatcher(VisualElement owner, Action sync)
    {
        _sync = sync;
        owner.HandlerChanged += (_, _) => Update(owner.Handler is not null);
    }

    private void Update(bool attached)
    {
        KlyColorMode.Changed -= OnChanged;
        if (_app is not null) _app.RequestedThemeChanged -= OnTheme;
        _app = null;
        if (!attached) return;

        KlyColorMode.Changed += OnChanged;
        if (Application.Current is { } app)
        {
            _app = app;
            app.RequestedThemeChanged += OnTheme;
        }
        _sync();
    }

    private void OnChanged(object? sender, EventArgs e) => _sync();

    private void OnTheme(object? sender, AppThemeChangedEventArgs e) => _sync();
}
