namespace Klyroo.UI.Maui;

/// <summary>Raises the link events of the page controls and opens external addresses.</summary>
internal static class PagesLinks
{
    public static PageLinkEventArgs Raise(object sender, EventHandler<PageLinkEventArgs>? handler, string? href, object? item)
    {
        var args = new PageLinkEventArgs(href, item);
        handler?.Invoke(sender, args);
        return args;
    }

    /// <summary>Raises the event, then opens the address with the system when nobody handled it and it leaves the app.</summary>
    public static void Follow(object sender, EventHandler<PageLinkEventArgs>? handler, string? href, object? item)
    {
        if (Raise(sender, handler, href, item).Handled) return;
        if (IsExternal(href, out var uri)) _ = Open(uri);
    }

    public static bool IsExternal(string? href, out Uri uri)
    {
        uri = null!;
        if (!Uri.TryCreate(href, UriKind.Absolute, out var parsed)) return false;
        if (parsed.Scheme is not ("http" or "https" or "mailto" or "tel")) return false;
        uri = parsed;
        return true;
    }

    private static async Task Open(Uri uri)
    {
        try
        {
            await Launcher.Default.OpenAsync(uri);
        }
        catch (Exception)
        {
            // No app on the device can open the address: the link does nothing, like a link to an unknown scheme.
        }
    }
}
