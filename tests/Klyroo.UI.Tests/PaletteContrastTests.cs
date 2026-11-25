using System.Text.RegularExpressions;

namespace Klyroo.UI.Tests;

/// <summary>
/// Guards the palette in <c>klyroo.base.css</c> against contrast regressions (WCAG 2.x relative luminance), in both themes:
/// solid fills used as the only cue of a state (switch track, checkbox, progress, slider) reach 3:1 against the page
/// surfaces (1.4.11), text on a fill reaches 4.5:1, and the text color reaches 4.5:1 on the page and on soft backgrounds.
/// </summary>
public partial class PaletteContrastTests
{
    private static readonly string[] Colors = ["primary", "secondary", "success", "info", "warning", "error", "neutral"];

    // Surfaces a control can sit on.
    private static readonly string[] Surfaces = ["bg", "bg-overlay", "bg-muted", "bg-elevated"];

    // The soft/subtle variants tint the surface with the color at 10%; hover, pressed and active states go higher. The
    // guard uses the highest tint any theme puts under text (derived from the classes, see Highest_tint_*).
    private static readonly double SoftAlpha = HighestTintAlpha();

    private static readonly Dictionary<string, Dictionary<string, string>> Themes = Load();

    public static TheoryData<string, string> ThemeAndColor()
    {
        var data = new TheoryData<string, string>();
        foreach (var theme in new[] { "light", "dark" })
        {
            foreach (var color in Colors)
            {
                data.Add(theme, color);
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(ThemeAndColor))]
    public void Solid_fill_reaches_3_to_1_against_the_page_surfaces_or_is_outlined_by_the_text_color(string theme, string color)
    {
        // The signal yellow is too light to reach 3:1 as a fill in the light theme; its indicators are outlined with
        // the -text color instead (checked:ring-(--kly-color-text) and friends), which must then reach 3:1.
        var indicator = theme == "light" && color == "primary" ? Token(theme, color + "-text") : Token(theme, color);

        foreach (var surface in Surfaces)
        {
            var ratio = Contrast(indicator, Token(theme, surface));
            Assert.True(ratio >= 3.0, $"{theme} {color}: indicator {indicator} on {surface} is {ratio:0.00}:1, needs 3:1");
        }
    }

    [Theory]
    [MemberData(nameof(ThemeAndColor))]
    public void Text_on_the_fill_reaches_4_5_to_1(string theme, string color)
    {
        var ratio = Contrast(Token(theme, color + "-contrast"), Token(theme, color));
        Assert.True(ratio >= 4.5, $"{theme} {color}: contrast on fill is {ratio:0.00}:1, needs 4.5:1");
    }

    [Theory]
    [MemberData(nameof(ThemeAndColor))]
    public void Color_text_reaches_4_5_to_1_on_the_page_and_on_soft_backgrounds(string theme, string color)
    {
        var text = Token(theme, color + "-text");
        foreach (var surface in Surfaces)
        {
            var background = Token(theme, surface);
            var plain = Contrast(text, background);
            Assert.True(plain >= 4.5, $"{theme} {color}: text {text} on {surface} is {plain:0.00}:1, needs 4.5:1");

            var soft = Contrast(text, Blend(Token(theme, color), background, SoftAlpha));
            Assert.True(soft >= 4.5, $"{theme} {color}: text {text} on soft {surface} is {soft:0.00}:1, needs 4.5:1");
        }
    }

    private static double HighestTintAlpha() =>
        new KlyrooOptions().AllThemes
            .SelectMany(t => t.AllClassTokens())
            .Select(token => Regex.Match(token, @"(?:^|:)bg-\(--kly-color\)/(?<alpha>\d+)$"))
            .Where(m => m.Success && int.Parse(m.Groups["alpha"].Value) <= 50)
            .Select(m => int.Parse(m.Groups["alpha"].Value) / 100.0)
            .DefaultIfEmpty(0.10)
            .Max();

    [Fact]
    public void Highest_tint_under_text_is_known_and_covered()
    {
        // Solid hover/active use /80 and /90 of the fill, so only tints up to 50% count. A new, higher tint fails the
        // text-on-soft-background test below until the -text tokens are darkened for it.
        Assert.InRange(SoftAlpha, 0.20, 0.50);
    }

    private static string Token(string theme, string name) => Themes[theme][name];

    private static Dictionary<string, Dictionary<string, string>> Load()
    {
        var css = File.ReadAllText(Path.Combine(FindLibraryRoot(), "styles", "klyroo.base.css"));
        return new()
        {
            ["light"] = Parse(Regex.Match(css, @":root\s*\{(?<body>[^}]*)\}").Groups["body"].Value),
            ["dark"] = Parse(Regex.Match(css, @"\.dark\s*\{(?<body>[^}]*)\}").Groups["body"].Value),
        };
    }

    private static Dictionary<string, string> Parse(string body) =>
        TokenPattern().Matches(body).ToDictionary(m => m.Groups["name"].Value, m => m.Groups["hex"].Value);

    [GeneratedRegex(@"--klyroo-(?<name>[a-z-]+):\s*(?<hex>#[0-9a-fA-F]{6})\s*;")]
    private static partial Regex TokenPattern();

    private static (double R, double G, double B) Rgb(string hex) =>
        (Convert.ToInt32(hex[1..3], 16), Convert.ToInt32(hex[3..5], 16), Convert.ToInt32(hex[5..7], 16));

    private static string Blend(string foreground, string background, double alpha)
    {
        var (fr, fg, fb) = Rgb(foreground);
        var (br, bg, bb) = Rgb(background);
        int Mix(double f, double b) => (int)Math.Round(f * alpha + b * (1 - alpha));
        return $"#{Mix(fr, br):x2}{Mix(fg, bg):x2}{Mix(fb, bb):x2}";
    }

    private static double Luminance(string hex)
    {
        var (r, g, b) = Rgb(hex);
        static double Channel(double c)
        {
            c /= 255;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(r) + 0.7152 * Channel(g) + 0.0722 * Channel(b);
    }

    private static double Contrast(string a, string b)
    {
        var (x, y) = (Luminance(a), Luminance(b));
        return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
    }

    private static string FindLibraryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Klyroo.UI");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }
        throw new DirectoryNotFoundException("src/Klyroo.UI not found above the test output directory.");
    }
}
