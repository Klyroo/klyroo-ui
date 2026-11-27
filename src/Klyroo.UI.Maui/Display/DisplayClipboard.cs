namespace Klyroo.UI.Maui;

/// <summary>Copies a text to the clipboard of the device.</summary>
internal static class DisplayClipboard
{
    /// <summary>Copies <paramref name="text"/>. Nothing happens for an empty text or when the platform has no clipboard.</summary>
    /// <param name="text">The text to copy.</param>
    /// <param name="copier">Writes the text instead of the platform; for tests.</param>
    public static void Copy(string? text, Func<string, Task>? copier = null)
    {
        if (string.IsNullOrEmpty(text)) return;
        _ = Run(text, copier ?? Clipboard.Default.SetTextAsync);
    }

    private static async Task Run(string text, Func<string, Task> copier)
    {
        try
        {
            await copier(text);
        }
        catch (Exception)
        {
            // The platform refused the clipboard (or there is none): the value stays on screen to copy by hand.
        }
    }
}
