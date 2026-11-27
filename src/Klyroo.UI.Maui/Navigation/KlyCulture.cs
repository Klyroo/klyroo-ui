using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>
/// The language of the app: the culture that formats dates and numbers and picks the resources. <see cref="Set"/> applies a culture and keeps it in <see cref="Preferences"/> under <see cref="PreferenceKey"/>;
/// call <see cref="ApplyStored"/> when the app starts, before the first page shows, so the choice of the last session is the culture from the start.
/// </summary>
/// <remarks>
/// The culture of a .NET process is read from the device at start. <see cref="Apply(string)"/> changes the defaults of every thread (<see cref="CultureInfo.DefaultThreadCurrentCulture"/> and
/// <see cref="CultureInfo.DefaultThreadCurrentUICulture"/>) and the culture of the calling thread, so text that is formatted afterwards uses it; text already on screen and resource lookups
/// that were cached keep what they had until the page is created again.
/// </remarks>
public static class KlyCulture
{
    /// <summary>The <see cref="Preferences"/> key the culture name is stored under.</summary>
    public const string PreferenceKey = "klyroo-culture";

    /// <summary>The culture name the user chose in an earlier session or earlier in this one, or <c>null</c> when none was chosen.</summary>
    public static string? Stored => NavigationPreferences.Get(PreferenceKey);

    /// <summary>Applies a culture to the app and stores its name.</summary>
    /// <param name="code">A culture name such as <c>fr</c> or <c>pt-BR</c>.</param>
    /// <exception cref="ArgumentException">The runtime does not know the culture.</exception>
    public static void Set(string code)
    {
        Apply(code);
        NavigationPreferences.Set(PreferenceKey, code);
    }

    /// <summary>Applies the stored culture, if there is one and the runtime knows it. Call it from the constructor of <c>App</c>.</summary>
    public static void ApplyStored()
    {
        if (Stored is { Length: > 0 } code && TryGet(code) is { } culture) ApplyCulture(culture);
    }

    /// <summary>Applies a culture to the app without storing it.</summary>
    /// <param name="code">A culture name such as <c>fr</c> or <c>pt-BR</c>.</param>
    /// <exception cref="ArgumentException">The runtime does not know the culture.</exception>
    public static void Apply(string code) =>
        ApplyCulture(TryGet(code) ?? throw new ArgumentException($"The culture '{code}' is not known to the runtime.", nameof(code)));

    private static void ApplyCulture(CultureInfo culture)
    {
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    private static CultureInfo? TryGet(string code)
    {
        try
        {
            return CultureInfo.GetCultureInfo(code);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    internal static void ForgetStored() => NavigationPreferences.Remove(PreferenceKey);
}
