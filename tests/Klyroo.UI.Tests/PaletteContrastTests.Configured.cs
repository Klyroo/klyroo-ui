using System.Text.RegularExpressions;
using Klyroo.UI.Theming;

namespace Klyroo.UI.Tests;

/// <summary>
/// The same contrast rules as above, applied to the tokens computed for any configured palette: every Tailwind palette as
/// the primary color and as the neutral one, in both themes, and the default tokens compiled into klyroo.css.
/// </summary>
public partial class PaletteContrastTests
{
    // The weakest a hairline border may be against a surface it can sit on: visible, not a cue by itself.
    internal const double BorderRatio = 1.15;

    private static readonly string[] AccentRoles = ["primary", "secondary", "success", "info", "warning", "error"];

    public static TheoryData<string> PaletteNames()
    {
        var data = new TheoryData<string>();
        foreach (var palette in KlyPalette.TailwindPalettes)
        {
            data.Add(palette.Name);
        }
        return data;
    }

    private static KlyPalette ByName(string name) => KlyPalette.TailwindPalettes.Single(p => p.Name == name);

    /// <summary>Every broken rule of a light and a dark token set, as readable messages.</summary>
    internal static List<string> Violations(IReadOnlyDictionary<string, string> light, IReadOnlyDictionary<string, string> dark)
    {
        var broken = new List<string>();
        void Check(bool ok, string message)
        {
            if (!ok)
            {
                broken.Add(message);
            }
        }

        foreach (var (theme, t) in new[] { ("light", light), ("dark", dark) })
        {
            foreach (var color in Colors)
            {
                var fill = t[color];
                var contrast = t[color + "-contrast"];
                var text = t[color + "-text"];

                var indicator = theme == "light" && color == "primary" ? text : fill;
                foreach (var surface in Surfaces)
                {
                    var ratio = Contrast(indicator, t[surface]);
                    Check(ratio >= 3.0, $"{theme} {color}: indicator {indicator} on {surface} is {ratio:0.00}:1, needs 3:1");
                    var plain = Contrast(text, t[surface]);
                    Check(plain >= 4.5, $"{theme} {color}: text {text} on {surface} is {plain:0.00}:1, needs 4.5:1");
                    var soft = Contrast(text, Blend(fill, t[surface], SoftAlpha));
                    Check(soft >= 4.5, $"{theme} {color}: text {text} on soft {surface} is {soft:0.00}:1, needs 4.5:1");
                    foreach (var alpha in new[] { 0.9, 0.8 })
                    {
                        var state = Contrast(contrast, Blend(fill, t[surface], alpha));
                        Check(state >= 4.5, $"{theme} {color}: {contrast} on the fill at {alpha:P0} over {surface} is {state:0.00}:1, needs 4.5:1");
                    }
                }

                var onFill = Contrast(contrast, fill);
                Check(onFill >= 4.5, $"{theme} {color}: contrast {contrast} on fill {fill} is {onFill:0.00}:1, needs 4.5:1");
            }

            foreach (var token in new[] { "text", "text-highlighted", "text-toned", "text-muted" })
            {
                foreach (var surface in Surfaces)
                {
                    var ratio = Contrast(t[token], t[surface]);
                    Check(ratio >= 4.5, $"{theme}: {token} on {surface} is {ratio:0.00}:1, needs 4.5:1");
                }
            }
            foreach (var surface in Surfaces)
            {
                var dimmed = Contrast(t["text-dimmed"], t[surface]);
                Check(dimmed >= 3.0, $"{theme}: text-dimmed on {surface} is {dimmed:0.00}:1, needs 3:1");
            }
            foreach (var token in new[] { "border-accented", "control" })
            {
                var ratio = Contrast(t[token], t["bg"]);
                Check(ratio >= 3.0, $"{theme}: {token} on bg is {ratio:0.00}:1, needs 3:1 (control boundaries, WCAG 1.4.11)");
            }
            foreach (var token in new[] { "border", "border-muted" })
            {
                foreach (var surface in Surfaces)
                {
                    var ratio = Contrast(t[token], t[surface]);
                    Check(ratio >= BorderRatio, $"{theme}: {token} on {surface} is {ratio:0.00}:1, needs {BorderRatio}:1 to stay visible");
                }
            }
            var inverted = Contrast(t["text-inverted"], t["bg-inverted"]);
            Check(inverted >= 4.5, $"{theme}: text-inverted on bg-inverted is {inverted:0.00}:1, needs 4.5:1");
            var tab = Contrast(t["text-muted"], t["bg-elevated"]);
            Check(tab >= 3.0, $"{theme}: text-muted on bg-elevated is {tab:0.00}:1, needs 3:1 (selected pill tab ring)");
        }

        broken.AddRange(OutlineViolations(light, dark));
        return broken;
    }

    /// <summary>The outline and subtle borders of the themes (ring/border of the color, with their alpha) against the surfaces.</summary>
    private static IEnumerable<string> OutlineViolations(IReadOnlyDictionary<string, string> light, IReadOnlyDictionary<string, string> dark)
    {
        var seen = new HashSet<string>();
        foreach (var theme in AllThemes)
        {
            if (!theme.Variants.TryGetValue("variant", out var variants))
            {
                continue;
            }
            foreach (var look in new[] { "outline", "subtle" })
            {
                if (!variants.TryGetValue(look, out var slots))
                {
                    continue;
                }
                foreach (var (slot, classes) in slots)
                {
                    foreach (var (prefixes, utility) in Tokens(classes))
                    {
                        var m = ColorRing.Match(utility);
                        if (!m.Success || prefixes.Any(TransientStates.Contains) || !seen.Add(string.Join(':', prefixes.Append(utility))))
                        {
                            continue;
                        }
                        var alpha = m.Groups["alpha"].Success ? int.Parse(m.Groups["alpha"].Value) / 100.0 : 1.0;
                        var suffix = m.Groups["text"].Success ? "-text" : "";
                        foreach (var (name, t) in new[] { ("light", light), ("dark", dark) })
                        {
                            foreach (var color in Colors)
                            {
                                foreach (var surface in Surfaces)
                                {
                                    var border = Blend(t[color + suffix], t[surface], alpha);
                                    var ratio = Contrast(border, t[surface]);
                                    if (ratio < 3.0)
                                    {
                                        yield return $"{theme.Name}.{slot} {look}: '{string.Join(':', prefixes.Append(utility))}' gives {ratio:0.00}:1 on {surface} for {name} {color}, needs 3:1";
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    [Fact]
    public void The_default_tokens_of_klyroo_css_pass_every_rule()
    {
        var broken = Violations(Themes["light"], Themes["dark"]);
        Assert.True(broken.Count == 0, string.Join(Environment.NewLine, broken.Take(20)));
    }

    [Theory]
    [MemberData(nameof(PaletteNames))]
    public void Every_tailwind_palette_as_primary_passes_every_rule(string name)
    {
        var colors = new KlyColorOptions { Primary = ByName(name) };
        var tokens = KlyThemeTokens.Compute(colors);
        var broken = Violations(tokens.Light, tokens.Dark);
        Assert.True(broken.Count == 0, $"primary = {name}:{Environment.NewLine}{string.Join(Environment.NewLine, broken.Take(20))}");
    }

    [Theory]
    [MemberData(nameof(PaletteNames))]
    public void Every_tailwind_palette_as_neutral_passes_every_rule(string name)
    {
        var colors = new KlyColorOptions { Neutral = ByName(name) };
        var tokens = KlyThemeTokens.Compute(colors);
        var broken = Violations(tokens.Light, tokens.Dark);
        Assert.True(broken.Count == 0, $"neutral = {name}:{Environment.NewLine}{string.Join(Environment.NewLine, broken.Take(20))}");
    }

    [Theory]
    [MemberData(nameof(PaletteNames))]
    public void Every_tailwind_palette_passes_as_each_semantic_color_on_the_default_and_on_a_neutral_palette(string name)
    {
        foreach (var neutral in new[] { KlyPalette.Klyroo, KlyPalette.Zinc, KlyPalette.Stone })
        {
            foreach (var role in AccentRoles)
            {
                var colors = new KlyColorOptions { Neutral = neutral };
                switch (role)
                {
                    case "primary": colors.Primary = ByName(name); break;
                    case "secondary": colors.Secondary = ByName(name); break;
                    case "success": colors.Success = ByName(name); break;
                    case "info": colors.Info = ByName(name); break;
                    case "warning": colors.Warning = ByName(name); break;
                    default: colors.Error = ByName(name); break;
                }
                var tokens = KlyThemeTokens.Compute(colors);
                var broken = Violations(tokens.Light, tokens.Dark);
                Assert.True(broken.Count == 0, $"{role} = {name}, neutral = {neutral}:{Environment.NewLine}{string.Join(Environment.NewLine, broken.Take(20))}");
            }
        }
    }

    [Theory]
    [InlineData("slate")]
    [InlineData("gray")]
    [InlineData("zinc")]
    [InlineData("neutral")]
    [InlineData("stone")]
    public void The_initials_of_an_avatar_are_readable_on_its_fill_under_every_gray_scale(string name)
    {
        // The avatar fallback is text on bg-accented, a fill the surface rules do not list: muted text was 3.98:1 on it in dark mode with the zinc neutrals.
        var tokens = KlyThemeTokens.Compute(new KlyColorOptions { Neutral = ByName(name) });
        foreach (var (theme, t) in new[] { ("light", tokens.Light), ("dark", tokens.Dark) })
        {
            var ratio = Contrast(t["text"], t["bg-accented"]);
            Assert.True(ratio >= 4.5, $"neutral = {name}, {theme}: text on bg-accented is {ratio:0.00}:1, needs 4.5:1");
        }
    }

    [Fact]
    public void An_extra_semantic_color_gets_tokens_that_pass_the_color_rules()
    {
        var colors = new KlyColorOptions();
        foreach (var palette in KlyPalette.TailwindPalettes)
        {
            colors.Add("extra-" + palette.Name, palette);
        }
        var tokens = KlyThemeTokens.Compute(colors);
        foreach (var (theme, t) in new[] { ("light", tokens.Light), ("dark", tokens.Dark) })
        {
            foreach (var name in tokens.ExtraColors)
            {
                var fill = t[name];
                Assert.True(Contrast(t[name + "-contrast"], fill) >= 4.5, $"{theme} {name}: contrast on fill");
                foreach (var surface in Surfaces)
                {
                    Assert.True(Contrast(t[name + "-text"], t[surface]) >= 4.5, $"{theme} {name}: text on {surface}");
                    Assert.True(Contrast(t[name + "-text"], Blend(fill, t[surface], SoftAlpha)) >= 4.5, $"{theme} {name}: text on soft {surface}");
                    foreach (var alpha in new[] { 0.9, 0.8 })
                    {
                        Assert.True(Contrast(t[name + "-contrast"], Blend(fill, t[surface], alpha)) >= 4.5, $"{theme} {name}: contrast on fill at {alpha} over {surface}");
                    }
                }
            }
        }
    }

    [Fact]
    public void The_soft_tint_the_tokens_are_computed_for_is_the_one_the_themes_use()
    {
        Assert.Equal(SoftAlpha, KlyThemeTokens.SoftAlpha);
    }

    [Fact]
    public void Text_on_solid_hover_and_active_fills_reaches_4_5_to_1_over_every_surface()
    {
        // bg-(--kly-color)/90 and /80 let the surface show through: the text on them is checked against the blended fill.
        foreach (var theme in new[] { "light", "dark" })
        {
            foreach (var color in Colors)
            {
                foreach (var surface in Surfaces)
                {
                    foreach (var alpha in new[] { 0.9, 0.8 })
                    {
                        var ratio = Contrast(Token(theme, color + "-contrast"), Blend(Token(theme, color), Token(theme, surface), alpha));
                        Assert.True(ratio >= 4.5, $"{theme} {color}: text on the fill at {alpha:P0} over {surface} is {ratio:0.00}:1, needs 4.5:1");
                    }
                }
            }
        }
    }

    [Fact]
    public void Solid_hover_and_active_use_the_fill_at_90_and_80_percent_only()
    {
        // The two alphas above are the ones the themes use for solid fills; another one needs its own check.
        var alphas = AllThemes.SelectMany(t => t.AllClassTokens())
            .Select(token => Regex.Match(token, @"(?:^|:)bg-\(--kly-color\)/(?<alpha>\d+)$"))
            .Where(m => m.Success && int.Parse(m.Groups["alpha"].Value) > 50)
            .Select(m => int.Parse(m.Groups["alpha"].Value))
            .Distinct()
            .Order()
            .ToList();
        Assert.Equal([80, 90], alphas);
    }
}
