namespace Klyroo.UI.Maui;

/// <summary>
/// The color mode of the app: light, dark, or the system setting. It sets <c>Application.Current.UserAppTheme</c>, so every Klyroo control follows it live,
/// and keeps the choice in <see cref="Preferences"/> under <see cref="PreferenceKey"/>. Call <see cref="ApplyStored"/> when the app starts, before the first page shows: it is the native
/// counterpart of the script that applies the stored mode before the first paint on the web.
/// </summary>
public static class KlyColorMode
{
    /// <summary>The <see cref="Preferences"/> key the choice is stored under: <c>system</c>, <c>light</c> or <c>dark</c>.</summary>
    public const string PreferenceKey = "klyroo-color-mode";

    private static AppTheme? _current;

    /// <summary>Raised when <see cref="Current"/> changes. Read <see cref="Current"/> or <see cref="IsDark"/> in the handler.</summary>
    public static event EventHandler? Changed;

    /// <summary>The mode the user chose: <see cref="AppTheme.Light"/>, <see cref="AppTheme.Dark"/>, or <see cref="AppTheme.Unspecified"/> to follow the system. Read from <see cref="Preferences"/> the first time.</summary>
    public static AppTheme Current => _current ??= Parse(NavigationPreferences.Get(PreferenceKey));

    /// <summary>Whether the app shows the dark theme now, whichever way it was chosen (the system setting included).</summary>
    public static bool IsDark => Application.Current?.RequestedTheme == AppTheme.Dark;

    /// <summary>Chooses a mode, applies it to the app, stores it and raises <see cref="Changed"/> when it differs from <see cref="Current"/>.</summary>
    /// <param name="theme"><see cref="AppTheme.Light"/>, <see cref="AppTheme.Dark"/>, or <see cref="AppTheme.Unspecified"/> for the system setting.</param>
    public static void Set(AppTheme theme)
    {
        var changed = Current != theme;
        _current = theme;
        NavigationPreferences.Set(PreferenceKey, Name(theme));
        Apply(theme);
        if (changed) Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>Switches between light and dark: dark when the app shows light now, light otherwise.</summary>
    public static void Toggle() => Set(IsDark ? AppTheme.Light : AppTheme.Dark);

    /// <summary>Applies the stored mode to the app. Call it from the constructor of <c>App</c>, or when the window is created, so the first page already has the right colors.</summary>
    public static void ApplyStored() => Apply(Current);

    /// <summary>Forgets what was read, so the next read goes to the preferences. For the tests, which share the process.</summary>
    internal static void Reset()
    {
        _current = null;
        NavigationPreferences.Remove(PreferenceKey);
    }

    internal static string Name(AppTheme theme) => theme switch
    {
        AppTheme.Light => "light",
        AppTheme.Dark => "dark",
        _ => "system",
    };

    private static AppTheme Parse(string? value) => value switch
    {
        "light" => AppTheme.Light,
        "dark" => AppTheme.Dark,
        _ => AppTheme.Unspecified,
    };

    private static void Apply(AppTheme theme)
    {
        if (Application.Current is { } app) app.UserAppTheme = theme;
    }
}
