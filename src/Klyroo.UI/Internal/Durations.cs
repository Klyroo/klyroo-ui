using System.Globalization;

namespace Klyroo.UI.Internal;

/// <summary>Number words that need no culture data: an app may run with invariant globalization.</summary>
internal static class Durations
{
    /// <summary>A length in hours and minutes, without culture data: <c>1 h 30 min</c>, <c>45 min</c>, <c>2 h</c>. The units are abbreviations, so they do not inflect; the separators are no-break spaces, so a narrow column never puts a number and its unit on two lines.</summary>
    public static string Format(int totalMinutes, string hour, string minute, string unitSeparator = " ", string partSeparator = " ")
    {
        var minutes = Math.Max(0, totalMinutes);
        var hours = minutes / 60;
        var rest = minutes % 60;
        var invariant = CultureInfo.InvariantCulture;
        if (hours == 0) return string.Create(invariant, $"{rest}{unitSeparator}{minute}");
        if (rest == 0) return string.Create(invariant, $"{hours}{unitSeparator}{hour}");
        return string.Create(invariant, $"{hours}{unitSeparator}{hour}{partSeparator}{rest}{unitSeparator}{minute}");
    }

    /// <summary>Russian plural forms: one (1, 21, 31...), few (2-4, 22-24...), many (the rest, 11-14 included).</summary>
    public static string Russian(int n, string one, string few, string many)
    {
        var mod10 = Math.Abs(n) % 10;
        var mod100 = Math.Abs(n) % 100;
        if (mod10 == 1 && mod100 != 11) return one;
        return mod10 is >= 2 and <= 4 && mod100 is < 12 or > 14 ? few : many;
    }

    /// <summary>Arabic plural forms after a number: the singular for 1, 2 and 11-99 (the counted noun stays singular), the plural for 0 and 3-10.</summary>
    public static string Arabic(int n, string singular, string plural)
    {
        var mod100 = Math.Abs(n) % 100;
        return mod100 is >= 3 and <= 10 || n == 0 ? plural : singular;
    }
}
