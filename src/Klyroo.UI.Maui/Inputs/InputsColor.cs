using System.Globalization;
using System.Text.RegularExpressions;

namespace Klyroo.UI.Maui;

/// <summary>Hex colors and their HSV form, for the color picker. Hue is in degrees (0 to 359), saturation and brightness from 0 to 1.</summary>
internal static partial class InputsColor
{
    [GeneratedRegex("^#?([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")]
    private static partial Regex HexPattern();

    /// <summary><c>#abc</c>, <c>abc</c> and <c>#AABBCC</c> as <c>#aabbcc</c>; <c>null</c> when the text is not a hex color.</summary>
    public static string? Normalize(string? text)
    {
        var match = HexPattern().Match((text ?? string.Empty).Trim());
        if (!match.Success) return null;
        var digits = match.Groups[1].Value;
        if (digits.Length == 3) digits = string.Concat(digits.Select(c => $"{c}{c}"));
        return "#" + digits.ToLowerInvariant();
    }

    public static (double H, double S, double V) ToHsv(string hex)
    {
        var (r, g, b) = Rgb(hex);
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;
        double hue;
        if (delta == 0) hue = 0;
        else if (max == r) hue = 60 * (((g - b) / delta % 6) + 6) % 360;
        else if (max == g) hue = (60 * (((b - r) / delta) + 2)) % 360;
        else hue = (60 * (((r - g) / delta) + 4)) % 360;
        return (Math.Min(Math.Round(hue), 359), max == 0 ? 0 : delta / max, max);
    }

    public static string ToHex(double hue, double saturation, double brightness)
    {
        var (r, g, b) = Rgb(hue, saturation, brightness);
        return string.Create(CultureInfo.InvariantCulture, $"#{(int)Math.Round(r * 255):x2}{(int)Math.Round(g * 255):x2}{(int)Math.Round(b * 255):x2}");
    }

    public static Color ToColor(string? hex) => Normalize(hex) is { } normal ? Color.FromArgb(normal) : Colors.Transparent;

    public static Color ToColor(double hue, double saturation, double brightness)
    {
        var (r, g, b) = Rgb(hue, saturation, brightness);
        return new Color((float)r, (float)g, (float)b);
    }

    private static (double R, double G, double B) Rgb(string hex)
    {
        var normal = Normalize(hex) ?? "#000000";
        return (
            int.Parse(normal.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d,
            int.Parse(normal.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d,
            int.Parse(normal.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d);
    }

    private static (double R, double G, double B) Rgb(double hue, double saturation, double brightness)
    {
        var h = ((hue % 360) + 360) % 360 / 60;
        var chroma = brightness * saturation;
        var x = chroma * (1 - Math.Abs((h % 2) - 1));
        var m = brightness - chroma;
        var (r, g, b) = (int)h switch
        {
            0 => (chroma, x, 0d),
            1 => (x, chroma, 0d),
            2 => (0d, chroma, x),
            3 => (0d, x, chroma),
            4 => (x, 0d, chroma),
            _ => (chroma, 0d, x),
        };
        return (r + m, g + m, b + m);
    }
}
