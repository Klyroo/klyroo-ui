using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Klyroo.UI.Theming;

/// <summary>
/// The color tokens (<c>--klyroo-*</c>, light and dark) of a <see cref="KlyColorOptions"/>: the hand-tuned defaults of
/// <c>klyroo.base.css</c>, with every configured palette turned into tokens that keep the contrast rules of the library.
/// </summary>
internal sealed class KlyThemeTokens
{
    private const string White = "#ffffff";
    private const double TextRatio = 4.5;
    private const double IndicatorRatio = 3.0;

    // The weakest a hairline border (--klyroo-border, --klyroo-border-muted) may be on any surface: visible, not a cue by itself.
    private const double BorderRatio = 1.15;

    // The weakest outline the themes draw with the text color: ring-(--kly-color-text)/75 (checked by the outline contrast test).
    private const double OutlineAlpha = 0.75;

    // Surfaces a control or a text can sit on (the same four the contrast tests use).
    private static readonly string[] Surfaces = ["bg", "bg-overlay", "bg-muted", "bg-elevated"];

    // Solid fills lighten on hover (/90) and press (/80): the text on them must hold at those blends.
    private static readonly double[] SolidStates = [0.9, 0.8];

    private static readonly string[] Roles = ["primary", "secondary", "success", "info", "warning", "error", "neutral"];

    /// <summary>Token name (without <c>--klyroo-</c>) to value, in the order they are written.</summary>
    public IReadOnlyDictionary<string, string> Light { get; }
    public IReadOnlyDictionary<string, string> Dark { get; }

    /// <summary>The tokens that differ from the defaults, per mode: what <see cref="KlyThemeStyle"/> writes.</summary>
    public IReadOnlyDictionary<string, string> LightChanges { get; }
    public IReadOnlyDictionary<string, string> DarkChanges { get; }

    /// <summary>Extra semantic colors, in registration order of their names.</summary>
    public IReadOnlyList<string> ExtraColors { get; }

    private KlyThemeTokens(
        Dictionary<string, string> light, Dictionary<string, string> dark,
        Dictionary<string, string> lightChanges, Dictionary<string, string> darkChanges, List<string> extra)
    {
        Light = light;
        Dark = dark;
        LightChanges = lightChanges;
        DarkChanges = darkChanges;
        ExtraColors = extra;
    }

    /// <summary>The tint that soft and subtle variants put under colored text: the highest <c>bg-(--kly-color)/N</c> (N up to 50) of any theme.</summary>
    public static double SoftAlpha { get; } = HighestTintAlpha();

    private static double HighestTintAlpha() =>
        new KlyrooOptions().AllThemes
            .SelectMany(t => t.AllClassTokens())
            .Select(token => Regex.Match(token, @"(?:^|:)bg-\(--kly-color\)/(?<alpha>\d+)$"))
            .Where(m => m.Success && int.Parse(m.Groups["alpha"].Value, CultureInfo.InvariantCulture) <= 50)
            .Select(m => int.Parse(m.Groups["alpha"].Value, CultureInfo.InvariantCulture) / 100.0)
            .DefaultIfEmpty(0.10)
            .Max();

    // Lazy: the default dictionaries below are static fields declared after this member.
    private static readonly Lazy<KlyThemeTokens> DefaultTokens = new(() => Compute(new KlyColorOptions()));

    public static KlyThemeTokens Defaults => DefaultTokens.Value;

    public static KlyThemeTokens Compute(KlyColorOptions colors)
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
        if (!neutral.IsKlyroo)
        {
            foreach (var (mode, tokens, changes) in new[] { (Mode.Light, light, lightChanges), (Mode.Dark, dark, darkChanges) })
            {
                foreach (var (name, value) in NeutralFamily(neutral, mode, colors.AllowLowContrast))
                {
                    Apply(tokens, changes, name, value);
                }
            }
        }

        foreach (var role in Roles)
        {
            var palette = colors.PaletteOf(role)!;
            if (palette.IsKlyroo && neutral.IsKlyroo)
            {
                continue;
            }
            foreach (var (mode, tokens, changes) in new[] { (Mode.Light, light, lightChanges), (Mode.Dark, dark, darkChanges) })
            {
                // A color left at the Klyroo default was tuned for the default surfaces: on the ones of another neutral palette
                // it keeps its values where they still pass and is nudged toward more contrast where they do not.
                var triplet = palette.IsKlyroo
                    ? Repair(new Triplet(tokens[role], tokens[role + "-text"], tokens[role + "-contrast"]), role, mode, tokens)
                    : PickTriplet(palette, role, mode, tokens);
                Require(triplet, role, palette, neutral, mode, tokens, colors.AllowLowContrast);
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
                Require(triplet, name, palette, neutral, mode, tokens, colors.AllowLowContrast);
                Apply(tokens, changes, name, triplet.Fill);
                Apply(tokens, changes, name + "-text", triplet.Text);
                Apply(tokens, changes, name + "-contrast", triplet.Contrast);
            }
        }

        // A token changed in one mode is written in the other too: the stylesheet is unlayered, so it would otherwise leave the
        // light value in force in dark mode (and the reverse) for a token whose default the other mode kept.
        foreach (var name in lightChanges.Keys.Except(darkChanges.Keys).ToList())
        {
            darkChanges[name] = dark[name];
        }
        foreach (var name in darkChanges.Keys.Except(lightChanges.Keys).ToList())
        {
            lightChanges[name] = light[name];
        }

        return new KlyThemeTokens(light, dark, lightChanges, darkChanges, extra);
    }

    /// <summary>Throws when the tokens picked for a color still break a rule, unless the low contrast is allowed.</summary>
    private static void Require(Triplet t, string role, KlyPalette palette, KlyPalette neutral, Mode mode, Dictionary<string, string> tokens, bool allowLowContrast)
    {
        if (allowLowContrast)
        {
            return;
        }
        var faint = mode == Mode.Light && role == "primary";
        var rule = FailingRule(t, faint, tokens);
        if (rule is null)
        {
            return;
        }
        var surfaces = !neutral.IsKlyroo && role != "neutral" ? $" on the surfaces of the neutral palette '{neutral.Name}'" : "";
        throw new InvalidOperationException(
            $"The '{role}' color cannot meet the contrast rules with the palette '{palette.Name}'{surfaces} in {ModeName(mode)} mode: {rule}. " +
            "Choose a palette with more range, or set Colors.AllowLowContrast = true to keep the best combination found.");
    }

    private static string ModeName(Mode mode) => mode == Mode.Light ? "light" : "dark";

    private static string Ratio(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Needed(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>The first rule the triplet breaks, as a sentence with the measured and the required ratio; null when all hold.</summary>
    private static string? FailingRule(Triplet t, bool fillMayBeFaint, Dictionary<string, string> tokens)
    {
        var onFill = KlyColorMath.Contrast(t.Contrast, t.Fill);
        if (onFill < TextRatio)
        {
            return $"the text {t.Contrast} on the fill {t.Fill} is {Ratio(onFill)}:1, needs {Needed(TextRatio)}:1";
        }
        foreach (var name in Surfaces)
        {
            var surface = tokens[name];
            foreach (var alpha in SolidStates)
            {
                var state = KlyColorMath.Contrast(t.Contrast, KlyColorMath.Blend(t.Fill, surface, alpha));
                if (state < TextRatio)
                {
                    return $"the text {t.Contrast} on the fill {t.Fill} at {alpha:P0} over {name} ({surface}) is {Ratio(state)}:1, needs {Needed(TextRatio)}:1";
                }
            }
            var indicator = KlyColorMath.Contrast(t.Fill, surface);
            if (!fillMayBeFaint && indicator < IndicatorRatio)
            {
                return $"the fill {t.Fill} on {name} ({surface}) is {Ratio(indicator)}:1, needs {Needed(IndicatorRatio)}:1";
            }
            var plain = KlyColorMath.Contrast(t.Text, surface);
            if (plain < TextRatio)
            {
                return $"the text color {t.Text} on {name} ({surface}) is {Ratio(plain)}:1, needs {Needed(TextRatio)}:1";
            }
            var soft = KlyColorMath.Contrast(t.Text, KlyColorMath.Blend(t.Fill, surface, SoftAlpha));
            if (soft < TextRatio)
            {
                return $"the text color {t.Text} on a soft tint of the fill over {name} ({surface}) is {Ratio(soft)}:1, needs {Needed(TextRatio)}:1";
            }
            var outline = KlyColorMath.Contrast(KlyColorMath.Blend(t.Text, surface, OutlineAlpha), surface);
            if (outline < IndicatorRatio)
            {
                return $"the outline in the text color {t.Text} at {OutlineAlpha:P0} on {name} ({surface}) is {Ratio(outline)}:1, needs {Needed(IndicatorRatio)}:1";
            }
        }
        return null;
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

    private static Triplet PickTriplet(KlyPalette palette, string role, Mode mode, Dictionary<string, string> tokens)
    {
        var surfaces = Surfaces.Select(s => tokens[s]).ToArray();
        var isNeutral = role == "neutral";
        // Uses 500 in light and 400 in dark; the neutral color is its inverted surface (dark in light, light in dark).
        var fillTarget = isNeutral ? (mode == Mode.Light ? 9 : 1) : (mode == Mode.Light ? 5 : 4);
        // The signal-yellow rule: the light primary fill may be too light for 3:1, its -text color then outlines the indicators.
        var fillMayBeFaint = mode == Mode.Light && role == "primary";

        var textTarget = isNeutral ? (mode == Mode.Light ? 8 : 2) : (mode == Mode.Light ? 6 : 4);

        // The first fill (nearest to the target shade) whose rules hold and that leaves a text color that holds too.
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
    private static (string Text, double Score) PickText(KlyPalette palette, int target, Mode mode, string fill, string[] surfaces)
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
            fill = KlyColorMath.Blend(extreme, t.Fill, step * 0.02);
        }

        var text = t.Text;
        for (var step = 0; step <= 50 && TextScore(text, fill, surfaces) < 0; step++)
        {
            text = KlyColorMath.Blend(extreme, t.Text, step * 0.02);
        }
        return t with { Fill = fill, Text = text };
    }

    /// <summary>Smallest margin over the rules of a fill with its contrast color; negative when one fails.</summary>
    private static double FillScore(string fill, string contrast, string[] surfaces, bool fillMayBeFaint)
    {
        var margin = KlyColorMath.Contrast(contrast, fill) / TextRatio - 1;
        foreach (var surface in surfaces)
        {
            foreach (var alpha in SolidStates)
            {
                margin = Math.Min(margin, KlyColorMath.Contrast(contrast, KlyColorMath.Blend(fill, surface, alpha)) / TextRatio - 1);
            }
            if (!fillMayBeFaint)
            {
                margin = Math.Min(margin, KlyColorMath.Contrast(fill, surface) / IndicatorRatio - 1);
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
            margin = Math.Min(margin, KlyColorMath.Contrast(text, surface) / TextRatio - 1);
            margin = Math.Min(margin, KlyColorMath.Contrast(text, KlyColorMath.Blend(fill, surface, SoftAlpha)) / TextRatio - 1);
            margin = Math.Min(margin, KlyColorMath.Contrast(KlyColorMath.Blend(text, surface, OutlineAlpha), surface) / IndicatorRatio - 1);
        }
        return margin;
    }

    /// <summary>
    /// The text, background and border tokens derived from a neutral palette (text 700, muted 500, bg white,
    /// bg-muted 50, border 200 … in light), moved toward more contrast where a rule needs it.
    /// </summary>
    private static IEnumerable<KeyValuePair<string, string>> NeutralFamily(KlyPalette p, Mode mode, bool allowLowContrast)
    {
        var light = mode == Mode.Light;
        var result = new List<KeyValuePair<string, string>>();
        void Add(string name, string value) => result.Add(new(name, value));

        // Surfaces. Dark mode steps down from 900 so the elevations keep four distinct steps.
        var bg = light ? White : p.Hex(10);
        var bgMuted = p.Hex(light ? 0 : 9);
        var bgElevated = p.Hex(light ? 1 : 8);
        var bgAccented = p.Hex(light ? 2 : 7);
        var bgOverlay = light ? White : p.Hex(9);
        var bgInverted = light ? p.Hex(9) : White;
        string[] surfaces = [bg, bgOverlay, bgMuted, bgElevated];

        // The shade nearest to the target, toward more contrast, that reaches the ratio on every surface; none is a broken rule.
        string Nearest(string token, IEnumerable<string> candidates, double ratio, string against)
        {
            var list = candidates.ToList();
            var found = list.FirstOrDefault(c => surfaces.All(s => KlyColorMath.Contrast(c, s) >= ratio));
            if (found is not null)
            {
                return found;
            }
            var last = list[^1];
            if (!allowLowContrast)
            {
                var worst = Surfaces.Select((name, i) => (name, i)).MinBy(x => KlyColorMath.Contrast(last, surfaces[x.i]));
                throw new InvalidOperationException(
                    $"The 'neutral' color cannot meet the contrast rules with the palette '{p.Name}' in {ModeName(mode)} mode: " +
                    $"{token} ({last}) on {worst.name} ({surfaces[worst.i]}) is {Ratio(KlyColorMath.Contrast(last, surfaces[worst.i]))}:1, needs {Needed(ratio)}:1 {against}. " +
                    "Choose a palette with more range, or set Colors.AllowLowContrast = true to keep the best combination found.");
            }
            return last;
        }

        string PickText(string token, int lightTarget, int darkTarget, double ratio) =>
            Nearest(token, Toward(light ? lightTarget : darkTarget, mode).Select(p.Hex), ratio, "");

        Add("text", PickText("text", 7, 2, TextRatio));
        Add("text-highlighted", light ? PickText("text-highlighted", 9, 9, TextRatio) : White);
        Add("text-toned", PickText("text-toned", 6, 3, TextRatio));
        Add("text-muted", PickText("text-muted", 5, 4, TextRatio));
        // Placeholders and hints are text like the rest (WCAG 1.4.3): 4.5:1 on every surface, so a group label or a hint in 11px passes too.
        Add("text-dimmed", PickText("text-dimmed", 4, 5, TextRatio));
        Add("text-inverted", light ? White : p.Hex(10));

        Add("bg", bg);
        Add("bg-muted", bgMuted);
        Add("bg-elevated", bgElevated);
        Add("bg-accented", bgAccented);
        Add("bg-overlay", bgOverlay);
        Add("bg-inverted", bgInverted);

        // Hairlines: starts at 200 (light) and 800 (dark), moved toward more contrast until they
        // stay visible on every surface (overlay and muted are 900 and elevated is 800 here, so 800 itself would vanish).
        Add("border", PickText("border", 2, 8, BorderRatio));
        Add("border-muted", PickText("border-muted", 2, 7, BorderRatio));
        // The boundary of form controls: 3:1 on the page, and against a white thumb for the switch track.
        var accented = Toward(light ? 3 : 7, mode).Select(p.Hex)
            .FirstOrDefault(c => KlyColorMath.Contrast(c, bg) >= IndicatorRatio && KlyColorMath.Contrast(c, White) >= IndicatorRatio);
        if (accented is null)
        {
            // No shade is 3:1 on the page and on a white thumb: the extreme one, which keeps the boundary visible on the page.
            accented = p.Hex(light ? 10 : 0);
            var onPage = KlyColorMath.Contrast(accented, bg);
            if (!allowLowContrast && onPage < IndicatorRatio)
            {
                throw new InvalidOperationException(
                    $"The 'neutral' color cannot meet the contrast rules with the palette '{p.Name}' in {ModeName(mode)} mode: " +
                    $"border-accented ({accented}) on bg ({bg}) is {Ratio(onPage)}:1, needs {Needed(IndicatorRatio)}:1. " +
                    "Choose a palette with more range, or set Colors.AllowLowContrast = true to keep the best combination found.");
            }
        }
        Add("border-accented", accented);
        Add("border-inverted", bgInverted);
        Add("control", accented);
        return result;
    }

    /// <summary>
    /// The stylesheet of the configuration. For the page (<c>:root</c>), only what differs from the defaults of <c>klyroo.css</c>,
    /// or null when nothing does. For another scope, every token in both modes: the scope has to look the same wherever it sits,
    /// whatever the page around it configured.
    /// </summary>
    /// <param name="colors">The options these tokens were computed from.</param>
    /// <param name="scope">The selector the light tokens apply to, <c>:root</c> for the page; the dark ones go to <c>.dark scope</c> and <c>scope.dark</c>.</param>
    public string? ToCss(KlyColorOptions colors, string scope = ":root")
    {
        var page = scope == ":root";
        if (page && colors.IsDefault)
        {
            return null;
        }

        var css = new StringBuilder();
        Block(css, scope, page ? LightChanges : Light);
        Block(css, page ? ".dark" : $".dark {scope},{scope}.dark", page ? DarkChanges : Dark);
        foreach (var name in ExtraColors)
        {
            css.Append($"[data-color='{name}']{{--kly-color:var(--klyroo-{name});--kly-color-text:var(--klyroo-{name}-text);--kly-color-contrast:var(--klyroo-{name}-contrast)}}");
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
            css.Append("--klyroo-").Append(name).Append(':').Append(value).Append(';');
        }
        css.Length--; // the last semicolon
        css.Append('}');
    }

    // Keep in sync with klyroo.base.css (a test compares them).
    private static readonly Dictionary<string, string> DefaultLight = new(StringComparer.Ordinal)
    {
        ["primary"] = "#b6ff00", ["primary-text"] = "#3b5900", ["primary-contrast"] = "#0b0f14",
        ["secondary"] = "#1a5e68", ["secondary-text"] = "#1a5e68", ["secondary-contrast"] = "#ffffff",
        ["success"] = "#146439", ["success-text"] = "#136238", ["success-contrast"] = "#ffffff",
        ["info"] = "#2555a0", ["info-text"] = "#23549e", ["info-contrast"] = "#ffffff",
        ["warning"] = "#d86010", ["warning-text"] = "#9c3d08", ["warning-contrast"] = "#1a0a00",
        ["error"] = "#a53125", ["error-text"] = "#9d2c1a", ["error-contrast"] = "#ffffff",
        ["neutral"] = "#0b0f14", ["neutral-text"] = "#1f2937", ["neutral-contrast"] = "#ffffff",
        ["text"] = "#1f2937", ["text-highlighted"] = "#0b0f14", ["text-toned"] = "#374151", ["text-muted"] = "#4b5563",
        ["text-dimmed"] = "#626b77", ["text-inverted"] = "#ffffff",
        ["bg"] = "#ffffff", ["bg-muted"] = "#f7f8fa", ["bg-elevated"] = "#f1f3f5", ["bg-accented"] = "#e5e7eb",
        ["bg-overlay"] = "#ffffff", ["bg-inverted"] = "#0b0f14",
        ["border"] = "#d4d8de", ["border-muted"] = "#d4d8de", ["border-accented"] = "#6b7280", ["border-inverted"] = "#0b0f14",
        ["control"] = "#6b7280",
    };

    private static readonly Dictionary<string, string> DefaultDark = new(StringComparer.Ordinal)
    {
        ["primary"] = "#b6ff00", ["primary-text"] = "#b6ff00", ["primary-contrast"] = "#0b0f14",
        ["secondary"] = "#64c3cf", ["secondary-text"] = "#64c3cf", ["secondary-contrast"] = "#06181b",
        ["success"] = "#7bd88f", ["success-text"] = "#7bd88f", ["success-contrast"] = "#08180c",
        ["info"] = "#7fb2ff", ["info-text"] = "#7fb2ff", ["info-contrast"] = "#07101f",
        ["warning"] = "#ff8a4c", ["warning-text"] = "#ff8a4c", ["warning-contrast"] = "#1a0a00",
        ["error"] = "#ff6b4a", ["error-text"] = "#ff7759", ["error-contrast"] = "#1f0700",
        ["neutral"] = "#e5e7eb", ["neutral-text"] = "#e5e7eb", ["neutral-contrast"] = "#0b0f14",
        ["text"] = "#e5e7eb", ["text-highlighted"] = "#ffffff", ["text-toned"] = "#cbd5e1", ["text-muted"] = "#9ca3af",
        ["text-dimmed"] = "#808999", ["text-inverted"] = "#0b0f14",
        ["bg"] = "#0b0f14", ["bg-muted"] = "#11161d", ["bg-elevated"] = "#171e27", ["bg-accented"] = "#222b36",
        ["bg-overlay"] = "#121820", ["bg-inverted"] = "#e5e7eb",
        ["border"] = "#242e3b", ["border-muted"] = "#2b3645", ["border-accented"] = "#647080", ["border-inverted"] = "#e5e7eb",
        ["control"] = "#647080",
    };
}
