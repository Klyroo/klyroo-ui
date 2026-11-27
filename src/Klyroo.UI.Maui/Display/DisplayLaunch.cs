namespace Klyroo.UI.Maui;

/// <summary>Opens the address of a link: the browser for http and https, the app registered for the scheme otherwise (<c>mailto:</c>, <c>tel:</c>).</summary>
internal static class DisplayLaunch
{
    /// <summary>
    /// Opens <paramref name="href"/>. An address that is empty or not absolute does nothing, and so does one the platform cannot open: a link has nowhere to report it,
    /// so the page that holds it keeps working.
    /// </summary>
    /// <param name="href">The address.</param>
    /// <param name="target">Where it opens.</param>
    /// <param name="opener">Opens the address instead of the platform; for tests.</param>
    public static void Open(string? href, KlyLinkTarget? target, Func<Uri, KlyLinkTarget?, Task>? opener = null)
    {
        if (!TryParse(href, out var uri)) return;
        _ = Run(uri, target, opener ?? OpenAsync);
    }

    /// <summary>
    /// Reads an address a device can open. A path such as <c>/docs</c> is not one: on Linux and macOS the framework reads it as an absolute <c>file:</c> address,
    /// and a link to a file of the device is not what a link in an interface means.
    /// </summary>
    public static bool TryParse(string? href, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Uri? uri) =>
        Uri.TryCreate(href, UriKind.Absolute, out uri) && uri.Scheme != Uri.UriSchemeFile;

    private static async Task Run(Uri uri, KlyLinkTarget? target, Func<Uri, KlyLinkTarget?, Task> opener)
    {
        try
        {
            await opener(uri, target);
        }
        catch (Exception)
        {
            // No browser, no app for the scheme, or no platform at all: see Open.
        }
    }

    private static async Task OpenAsync(Uri uri, KlyLinkTarget? target)
    {
        if (uri.Scheme is "http" or "https")
        {
            await Browser.Default.OpenAsync(uri, target is { IsBlank: true } ? BrowserLaunchMode.External : BrowserLaunchMode.SystemPreferred);
        }
        else
        {
            await Launcher.Default.OpenAsync(uri);
        }
    }
}
