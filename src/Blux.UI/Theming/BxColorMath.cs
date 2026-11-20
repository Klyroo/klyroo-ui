using System.Globalization;

namespace Blux.UI.Theming;

/// <summary>Color conversion and WCAG 2.x contrast, on the 8-bit sRGB values that are written to the stylesheet.</summary>
internal static class BxColorMath
{
    public static (int R, int G, int B) Parse(string css)
    {
        var value = css.Trim();
        if (value.StartsWith('#'))
        {
            return ParseHex(value);
        }
        if (value.StartsWith("oklch(", StringComparison.OrdinalIgnoreCase) && value.EndsWith(')'))
        {
            return ParseOklch(value[6..^1]);
        }
        throw new FormatException($"Unsupported color '{css}': use #rgb, #rrggbb or oklch(L C H).");
    }

    private static (int, int, int) ParseHex(string hex)
    {
        var digits = hex[1..];
        if (digits.Length == 3)
        {
            digits = string.Concat(digits.Select(c => new string(c, 2)));
        }
        if (digits.Length != 6 || !digits.All(Uri.IsHexDigit))
        {
            throw new FormatException($"Invalid hex color '{hex}'.");
        }
        return (Convert.ToInt32(digits[..2], 16), Convert.ToInt32(digits[2..4], 16), Convert.ToInt32(digits[4..], 16));
    }

    private static (int, int, int) ParseOklch(string body)
    {
        var slash = body.IndexOf('/');
        if (slash >= 0)
        {
            body = body[..slash];
        }
        var parts = body.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            throw new FormatException($"Invalid oklch color 'oklch({body})'.");
        }

        static double Number(string text)
        {
            if (text.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }
            return double.Parse(text.TrimEnd('%', 'd', 'e', 'g'), CultureInfo.InvariantCulture);
        }

        var l = Number(parts[0]);
        if (parts[0].EndsWith('%'))
        {
            l /= 100;
        }
        var c = Number(parts[1]);
        var h = Number(parts[2]) * Math.PI / 180;

        var a = c * Math.Cos(h);
        var b = c * Math.Sin(h);
        var l_ = l + 0.3963377774 * a + 0.2158037573 * b;
        var m_ = l - 0.1055613458 * a - 0.0638541728 * b;
        var s_ = l - 0.0894841775 * a - 1.2914855480 * b;
        double l3 = l_ * l_ * l_, m3 = m_ * m_ * m_, s3 = s_ * s_ * s_;

        var r = 4.0767416621 * l3 - 3.3077115913 * m3 + 0.2309699292 * s3;
        var g = -1.2684380046 * l3 + 2.6097574011 * m3 - 0.3413193965 * s3;
        var bl = -0.0041960863 * l3 - 0.7034186147 * m3 + 1.7076147010 * s3;
        return (Encode(r), Encode(g), Encode(bl));
    }

    private static int Encode(double linear)
    {
        linear = Math.Clamp(linear, 0, 1);
        var srgb = linear <= 0.0031308 ? 12.92 * linear : 1.055 * Math.Pow(linear, 1 / 2.4) - 0.055;
        return (int)Math.Round(Math.Clamp(srgb, 0, 1) * 255);
    }

    /// <summary>Any supported CSS color as lower-case <c>#rrggbb</c>.</summary>
    public static string ToHex(string css)
    {
        var (r, g, b) = Parse(css);
        return $"#{r:x2}{g:x2}{b:x2}";
    }

    public static (double R, double G, double B) Rgb(string hex) =>
        (Convert.ToInt32(hex[1..3], 16), Convert.ToInt32(hex[3..5], 16), Convert.ToInt32(hex[5..7], 16));

    public static string Blend(string foreground, string background, double alpha)
    {
        var (fr, fg, fb) = Rgb(foreground);
        var (br, bg, bb) = Rgb(background);
        int Mix(double f, double b) => (int)Math.Round(f * alpha + b * (1 - alpha));
        return $"#{Mix(fr, br):x2}{Mix(fg, bg):x2}{Mix(fb, bb):x2}";
    }

    public static double Luminance(string hex)
    {
        var (r, g, b) = Rgb(hex);
        static double Channel(double c)
        {
            c /= 255;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(r) + 0.7152 * Channel(g) + 0.0722 * Channel(b);
    }

    public static double Contrast(string a, string b)
    {
        var (x, y) = (Luminance(a), Luminance(b));
        return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
    }
}
