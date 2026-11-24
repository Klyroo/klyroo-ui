using System.Globalization;

namespace Lux.UI.Internal;

/// <summary>A size in bytes as a short text in the language and culture of a locale: "512 B", "1,5 Ko", "2 MB".</summary>
internal static class FileSizeText
{
    public static string Format(long bytes, LxLocale locale, CultureInfo? culture = null)
    {
        culture ??= locale.Culture;
        var messages = locale.Messages;
        string[] templates = [messages.FileSizeBytes, messages.FileSizeKilobytes, messages.FileSizeMegabytes, messages.FileSizeGigabytes];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < templates.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        var number = Math.Round(size, 1).ToString(culture);
        return string.Format(culture, templates[unit], number);
    }
}
