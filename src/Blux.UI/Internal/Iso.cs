using System.Globalization;

namespace Blux.UI.Internal;

/// <summary>ISO 8601 text of dates and times: what the pickers render, post and report.</summary>
internal static class Iso
{
    public static string? Date(DateOnly? date) => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static DateOnly? ParseDate(string? text) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    public static string? Time(TimeOnly? time) => time?.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static TimeOnly? ParseTime(string? text) =>
        TimeOnly.TryParseExact(text, ["HH:mm", "HH:mm:ss"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var time) ? time : null;
}
