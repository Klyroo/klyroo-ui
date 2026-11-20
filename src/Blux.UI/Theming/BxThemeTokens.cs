using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Blux.UI.Theming;

/// <summary>
/// The color tokens (<c>--blux-*</c>, light and dark) of a <see cref="BxColorOptions"/>: the hand-tuned defaults of
/// <c>blux.base.css</c>, with every configured palette turned into tokens that keep the contrast rules of the library.
/// </summary>
internal sealed class BxThemeTokens
{
    private const string White = "#ffffff";
    private const double TextRatio = 4.5;
    private const double IndicatorRatio = 3.0;

    // The weakest outline the themes draw with the text color: ring-(--bx-color-text)/75 (checked by the outline contrast test).
    private const double OutlineAlpha = 0.75;

    // Surfaces a control or a text can sit on (the same four the contrast tests use).
    private static readonly string[] Surfaces = ["bg", "bg-overlay", "bg-muted", "bg-elevated"];

    // Solid fills lighten on hover (/90) and press (/80): the text on them must hold at those blends.
    private static readonly double[] SolidStates = [0.9, 0.8];

    private static readonly string[] Roles = ["primary", "secondary", "success", "info", "warning", "error", "neutral"];

    /// <summary>Token name (without <c>--blux-</c>) to value, in the order they are written.</summary>
    public IReadOnlyDictionary<string, string> Light { get; }
    public IReadOnlyDictionary<string, string> Dark { get; }

    /// <summary>The tokens that differ from the defaults, per mode: what <see cref="BxThemeStyle"/> writes.</summary>
    public IReadOnlyDictionary<string, string> LightChanges { get; }
    public IReadOnlyDictionary<string, string> DarkChanges { get; }

    /// <summary>Extra semantic colors, in registration order of their names.</summary>
    public IReadOnlyList<string> ExtraColors { get; }

    private BxThemeTokens(
        Dictionary<string, string> light, Dictionary<string, string> dark,
        Dictionary<string, string> lightChanges, Dictionary<string, string> darkChanges, List<string> extra)
    {
        Light = light;
        Dark = dark;
        LightChanges = lightChanges;
        DarkChanges = darkChanges;
        ExtraColors = extra;
    }

    /// <summary>The tint that soft and subtle variants put under colored text: the highest <c>bg-(--bx-color)/N</c> (N up to 50) of any theme.</summary>
    public static double SoftAlpha { get; } = HighestTintAlpha();

    private static double HighestTintAlpha() =>
        new BluxOptions().AllThemes
            .SelectMany(t => t.AllClassTokens())
            .Select(token => Regex.Match(token, @"(?:^|:)bg-\(--bx-color\)/(?<alpha>\d+)$"))
            .Where(m => m.Success && int.Parse(m.Groups["alpha"].Value, CultureInfo.InvariantCulture) <= 50)
            .Select(m => int.Parse(m.Groups["alpha"].Value, CultureInfo.InvariantCulture) / 100.0)
            .DefaultIfEmpty(0.10)
            .Max();

    // Lazy: the default dictionaries below are static fields declared after this member.
    private static readonly Lazy<BxThemeTokens> DefaultTokens = new(() => Compute(new BxColorOptions()));

    public static BxThemeTokens Defaults => DefaultTokens.Value;

    public static BxThemeTokens Compute(BxColorOptions colors)
    {
        var light = new Dictionary<string, string>(DefaultLight, StringComparer.Ordinal);
        var dark = new Dictionary<string, string>(DefaultDark, StringComparer.Ordinal);
        var lightChanges = new Dictionary<string, string>(StringComparer.Ordinal);
        var darkChanges = new Dictionary<string, string>(StringComparer.Ordinal);

        void Apply(Dictionary<string, string> tokens, Dictionary<string, string> changes, string name, string value)
        {
            if (!tokens.TryGetValue(name, out var existing) || existing != value)
            {
                changes[name] = value;
            }
            tokens[name] = value;
        }

        // The neutral palette first: it sets the surfaces every other color is checked against.
        var neutral = colors.Neutral;
        if (!neutral.IsBlux)
        {
            foreach (var (mode, tokens, changes) in new[] { (Mode.Light, light, lightChanges), (Mode.Dark, dark, darkChanges) })
            {
                foreach (var (name, value) in NeutralFamily(neutral, mode))
                {
                    Apply(tokens, changes, name, value);
                }
            }
        }

        foreach (var role in Roles)
        {
            var palette = colors.PaletteOf(role)!;
            if (palette.IsBlux && neutral.IsBlux)
            {
                continue;
            }
            foreach (var (mode, tokens, changes) in new[] { (Mode.Light, light, lightChanges), (Mode.Dark, dark, darkChanges) })
            {
                // A color left at the Blux default was tuned for the default surfaces: on the ones of another neutral palette
                // it keeps its values where they still pass and is nudged toward more contrast where they do not.
                var triplet = palette.IsBlux
                    ? Repair(new Triplet(tokens[role], tokens[role + "-text"], tokens[role + "-contrast"]), role, mode, tokens)
                    : PickTriplet(palette, role, mode, tokens);
                Apply(tokens, changes, role, triplet.Fill);
                Apply(tokens, changes, role + "-text", triplet.Text);
                Apply(tokens, changes, role + "-contrast", triplet.Contrast);
            }
        }

        var extra = new List<string>();
        foreach (var (name, palette) in colors.Extra.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            extra.Add(name);
            foreach (var (mode, tokens, changes) in new[] { (Mode.Light, light, lightChanges), (Mode.Dark, dark, darkChanges) })
            {
                var triplet = PickTriplet(palette, name, mode, tokens);
                Apply(tokens, changes, name, triplet.Fill);
                Apply(tokens, changes, name + "-text", triplet.Text);
                Apply(tokens, changes, name + "-contrast", triplet.Contrast);
            }
        }

        return new BxThemeTokens(light, dark, lightChanges, darkChanges, extra);
    }

    private enum Mode { Light, Dark }

    private readonly record struct Triplet(string Fill, string Text, string Contrast);

    /// <summary>Indexes 0..10 ordered by distance to <paramref name="target"/>; on a tie the shade away from the page comes first.</summary>
    private static IEnumerable<int> Nearest(int target, Mode mode)
    {
        var towardContrast = mode == Mode.Light ? 1 : -1;
        for (var d = 0; d <= 10; d++)
        {
            var first = target + d * towardContrast;
            var second = target - d * towardContrast;
            if (first is >= 0 and <= 10)
            {
                yield return first;
            }
            if (d > 0 && second is >= 0 and <= 10)
            {
                yield return second;
            }
        }
    }

    /// <summary>Indexes from <paramref name="target"/> toward more contrast with the page only (darker in light, lighter in dark).</summary>
    private static IEnumerable<int> Toward(int target, Mode mode)
    {
        var step = mode == Mode.Light ? 1 : -1;
        for (var i = target; i is >= 0 and <= 10; i += step)
        {
            yield return i;
        }
    }

    private static Triplet PickTriplet(BxPalette palette, string role, Mode mode, Dictionary<string, string> tokens)
    {
        var surfaces = Surfaces.Select(s => tokens[s]).ToArray();
        var isNeutral = role == "neutral";
        // Nuxt UI uses 500 in light and 400 in dark; the neutral color is its inverted surface (dark in light, light in dark).
        var fillTarget = isNeutral ? (mode == Mode.Light ? 9 : 1) : (mode == Mode.Light ? 5 : 4);
        // The signal-yellow rule: the light primary fill may be too light for 3:1, its -text color then outlines the indicators.
        var fillMayBeFaint = mode == Mode.Light && role == "primary";

        var textTarget = isNeutral ? (mode == Mode.Light ? 8 : 2) : (mode == Mode.Light ? 6 : 4);

        // The first fill (nearest to Nuxt's choice) whose rules hold and that leaves a text color that holds too.
        Triplet? best = null;
        var bestScore = double.MinValue;
        foreach (var index in Nearest(fillTarget, mode))
        {
            var fill = palette.Hex(index);
            foreach (var contrast in new[] { White, palette.Hex(10) })
            {
                var fillScore = FillScore(fill, contrast, surfaces, fillMayBeFaint);
                var (text, textScore) = PickText(palette, textTarget, mode, fill, surfaces);
                var score = Math.Min(fillScore, textScore);
                if (score >= 0)
                {
                    return new Triplet(fill, text, contrast);
                }
                if (score > bestScore)
                {
                    (best, bestScore) = (new Triplet(fill, text, contrast), score);
                }
            }
        }
        return best!.Value;
    }

    /// <summary>The shade nearest to <paramref name="target"/> that passes as text on the surfaces and on the fill's tint, with its score.</summary>
    private static (string Text, double Score) PickText(BxPalette palette, int target, Mode mode, string fill, string[] surfaces)
    {
        (string, double) best = ("", double.MinValue);
        foreach (var index in Nearest(target, mode))
        {
            var candidate = palette.Hex(index);
            var score = TextScore(candidate, fill, surfaces);
            if (score >= 0)
            {
                return (candidate, score);
            }
            if (score > best.Item2)
            {
                best = (candidate, score);
            }
        }
        return best;
    }

    private static Triplet Repair(Triplet t, string role, Mode mode, Dictionary<string, string> tokens)
    {
        var surfaces = Surfaces.Select(s => tokens[s]).ToArray();
        var extreme = mode == Mode.Light ? "#000000" : White;
        var faint = mode == Mode.Light && role == "primary";

        var fill = t.Fill;
        for (var step = 0; step <= 50 && FillScore(fill, t.Contrast, surfaces, faint) < 0; step++)
        {
            fill = BxColorMath.Blend(extreme, t.Fill, step * 0.02);
        }

        var text = t.Text;
        for (var step = 0; step <= 50 && TextScore(text, fill, surfaces) < 0; step++)
        {
            text = BxColorMath.Blend(extreme, t.Text, step * 0.02);
        }
        return t with { Fill = fill, Text = text };
    }

    /// <summary>Smallest margin over the rules of a fill with its contrast color; negative when one fails.</summary>
    private static double FillScore(string fill, string contrast, string[] surfaces, bool fillMayBeFaint)
    {
        var margin = BxColorMath.Contrast(contrast, fill) / TextRatio - 1;
        foreach (var surface in surfaces)
        {
            foreach (var alpha in SolidStates)
            {
                margin = Math.Min(margin, BxColorMath.Contrast(contrast, BxColorMath.Blend(fill, surface, alpha)) / TextRatio - 1);
            }
            if (!fillMayBeFaint)
            {
                margin = Math.Min(margin, BxColorMath.Contrast(fill, surface) / IndicatorRatio - 1);
            }
        }
        return margin;
    }

    /// <summary>Smallest margin of the text color on the page surfaces and on the soft tint of the fill.</summary>
    private static double TextScore(string text, string fill, string[] surfaces)
    {
        var margin = double.MaxValue;
        foreach (var surface in surfaces)
        {
            margin = Math.Min(margin, BxColorMath.Contrast(text, surface) / TextRatio - 1);
            margin = Math.Min(margin, BxColorMath.Contrast(text, BxColorMath.Blend(fill, surface, SoftAlpha)) / TextRatio - 1);
            margin = Math.Min(margin, BxColorMath.Contrast(BxColorMath.Blend(text, surface, OutlineAlpha), surface) / IndicatorRatio - 1);
        }
        return margin;
    }

    /// <summary>
    /// The text, background and border tokens from a neutral palette, with Nuxt UI's mapping (text 700, muted 500, bg white,
    /// bg-muted 50, border 200 … in light), moved toward more contrast where a rule needs it.
    /// </summary>
    private static IEnumerable<KeyValuePair<string, string>> NeutralFamily(BxPalette p, Mode mode)
    {
        var light = mode == Mode.Light;
        var result = new List<KeyValuePair<string, string>>();
        void Add(string name, string value) => result.Add(new(name, value));

        // Surfaces. Dark mode goes one step below Nuxt UI's 900 page so the elevations keep four distinct steps.
        var bg = light ? White : p.Hex(10);
        var bgMuted = p.Hex(light ? 0 : 9);
        var bgElevated = p.Hex(light ? 1 : 8);
        var bgAccented = p.Hex(light ? 2 : 7);
        var bgOverlay = light ? White : p.Hex(9);
        var bgInverted = light ? p.Hex(9) : White;
        string[] surfaces = [bg, bgOverlay, bgMuted, bgElevated];

        string PickText(int lightTarget, int darkTarget, double ratio, string[]? against = null)
        {
            var targets = against ?? surfaces;
            var candidates = Toward(light ? lightTarget : darkTarget, mode).Select(p.Hex).ToList();
            return candidates.FirstOrDefault(c => targets.All(s => BxColorMath.Contrast(c, s) >= ratio))
                   ?? candidates[^1];
        }

        Add("text", PickText(7, 2, TextRatio));
        Add("text-highlighted", light ? PickText(9, 9, TextRatio) : White);
        Add("text-toned", PickText(6, 3, TextRatio));
        Add("text-muted", PickText(5, 4, TextRatio));
        // Placeholders and hints: as legible as the default theme's, which is slightly under 4.5:1.
        Add("text-dimmed", PickText(4, 5, 4.0));
        Add("text-inverted", light ? White : p.Hex(10));

        Add("bg", bg);
        Add("bg-muted", bgMuted);
        Add("bg-elevated", bgElevated);
        Add("bg-accented", bgAccented);
        Add("bg-overlay", bgOverlay);
        Add("bg-inverted", bgInverted);

        Add("border", p.Hex(light ? 2 : 9));
        Add("border-muted", p.Hex(light ? 2 : 8));
        // The boundary of form controls: 3:1 on the page, and against a white thumb for the switch track.
        var accented = Toward(light ? 3 : 7, mode).Select(p.Hex)
            .FirstOrDefault(c => BxColorMath.Contrast(c, bg) >= IndicatorRatio && BxColorMath.Contrast(c, White) >= IndicatorRatio)
            ?? p.Hex(light ? 10 : 0);
        Add("border-accented", accented);
        Add("border-inverted", bgInverted);
        Add("control", accented);
        return result;
    }

    /// <summary>The stylesheet of the configuration, or null when it equals the defaults of <c>blux.css</c>.</summary>
    /// <param name="colors">The options these tokens were computed from.</param>
    /// <param name="scope">The selector the light tokens apply to, <c>:root</c> for the page; the dark ones go to <c>.dark scope</c> and <c>scope.dark</c>.</param>
    public string? ToCss(BxColorOptions colors, string scope = ":root")
    {
        if (colors.IsDefault)
        {
            return null;
        }

        var css = new StringBuilder();
        Block(css, scope, LightChanges);
        Block(css, scope == ":root" ? ".dark" : $".dark {scope},{scope}.dark", DarkChanges);
        foreach (var name in ExtraColors)
        {
            css.Append($"[data-color='{name}']{{--bx-color:var(--blux-{name});--bx-color-text:var(--blux-{name}-text);--bx-color-contrast:var(--blux-{name}-contrast)}}");
        }
        return css.Length == 0 ? null : css.ToString();
    }

    private static void Block(StringBuilder css, string selector, IReadOnlyDictionary<string, string> tokens)
    {
        if (tokens.Count == 0)
        {
            return;
        }
        css.Append(selector).Append('{');
        foreach (var (name, value) in tokens)
        {
            css.Append("--blux-").Append(name).Append(':').Append(value).Append(';');
        }
        css.Length--; // the last semicolon
        css.Append('}');
    }

    // Keep in sync with blux.base.css (a test compares them).
    private static readonly Dictionary<string, string> DefaultLight = new(StringComparer.Ordinal)
    {
        ["primary"] = "#ffd23f", ["primary-text"] = "#7a5800", ["primary-contrast"] = "#151100",
        ["secondary"] = "#1a5e68", ["secondary-text"] = "#1a5e68", ["secondary-contrast"] = "#ffffff",
        ["success"] = "#146439", ["success-text"] = "#136238", ["success-contrast"] = "#ffffff",
        ["info"] = "#2555a0", ["info-text"] = "#23549e", ["info-contrast"] = "#ffffff",
        ["warning"] = "#d86010", ["warning-text"] = "#9c3d08", ["warning-contrast"] = "#1a0a00",
        ["error"] = "#a53125", ["error-text"] = "#9d2c1a", ["error-contrast"] = "#ffffff",
        ["neutral"] = "#0b0c0d", ["neutral-text"] = "#23272a", ["neutral-contrast"] = "#ffffff",
        ["text"] = "#23272a", ["text-highlighted"] = "#0b0c0d", ["text-toned"] = "#43484b", ["text-muted"] = "#5b6165",
        ["text-dimmed"] = "#73797d", ["text-inverted"] = "#ffffff",
        ["bg"] = "#f5f5f2", ["bg-muted"] = "#eeeeea", ["bg-elevated"] = "#e9e9e4", ["bg-accented"] = "#dfdfda",
        ["bg-overlay"] = "#ffffff", ["bg-inverted"] = "#0b0c0d",
        ["border"] = "#dcdcd7", ["border-muted"] = "#dcdcd7", ["border-accented"] = "#85898c", ["border-inverted"] = "#0b0c0d",
        ["control"] = "#85898c",
    };

    private static readonly Dictionary<string, string> DefaultDark = new(StringComparer.Ordinal)
    {
        ["primary"] = "#ffd23f", ["primary-text"] = "#ffd23f", ["primary-contrast"] = "#151100",
        ["secondary"] = "#64c3cf", ["secondary-text"] = "#64c3cf", ["secondary-contrast"] = "#06181b",
        ["success"] = "#7bd88f", ["success-text"] = "#7bd88f", ["success-contrast"] = "#08180c",
        ["info"] = "#7fb2ff", ["info-text"] = "#7fb2ff", ["info-contrast"] = "#07101f",
        ["warning"] = "#ff8a4c", ["warning-text"] = "#ff8a4c", ["warning-contrast"] = "#1a0a00",
        ["error"] = "#ff6b4a", ["error-text"] = "#ff6e4d", ["error-contrast"] = "#1f0700",
        ["neutral"] = "#edefef", ["neutral-text"] = "#d5d9db", ["neutral-contrast"] = "#0a0b0c",
        ["text"] = "#d5d9db", ["text-highlighted"] = "#f2f4f4", ["text-toned"] = "#aeb4b8", ["text-muted"] = "#8a9196",
        ["text-dimmed"] = "#6f767b", ["text-inverted"] = "#0a0b0c",
        ["bg"] = "#0a0b0c", ["bg-muted"] = "#111315", ["bg-elevated"] = "#1a1d20", ["bg-accented"] = "#23272a",
        ["bg-overlay"] = "#131618", ["bg-inverted"] = "#edefef",
        ["border"] = "#202427", ["border-muted"] = "#2e3337", ["border-accented"] = "#5c6369", ["border-inverted"] = "#edefef",
        ["control"] = "#5c6369",
    };
}
