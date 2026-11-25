using System.Text.RegularExpressions;
using Klyroo.UI.Theming;
using Microsoft.Extensions.DependencyInjection;

namespace Klyroo.UI.Tests;

/// <summary>A palette every shade of which is the same mid gray: no text, fill or border can meet the contrast rules with it.</summary>
internal static class FlatPalette
{
    public static KlyPalette Gray { get; } = KlyPalette.Custom("flat-gray", [.. Enumerable.Repeat("#888888", 11)]);
}

public class ContrastFailureTests
{
    [Fact]
    public void A_palette_that_cannot_meet_the_rules_throws_naming_the_role_the_palette_and_the_rule()
    {
        var colors = new KlyColorOptions { Primary = FlatPalette.Gray };

        var error = Assert.Throws<InvalidOperationException>(() => KlyThemeTokens.Compute(colors));

        Assert.Contains("primary", error.Message);
        Assert.Contains("flat-gray", error.Message);
        Assert.Matches(@"\d\.\d\d:1, needs \d(\.\d+)?:1", error.Message);
        Assert.Contains("AllowLowContrast", error.Message);
    }

    [Fact]
    public void An_extra_color_and_a_neutral_palette_that_cannot_meet_the_rules_throw_too()
    {
        var extra = new KlyColorOptions();
        extra.Add("tertiary", FlatPalette.Gray);
        Assert.Contains("tertiary", Assert.Throws<InvalidOperationException>(() => KlyThemeTokens.Compute(extra)).Message);

        var neutral = new KlyColorOptions { Neutral = FlatPalette.Gray };
        var error = Assert.Throws<InvalidOperationException>(() => KlyThemeTokens.Compute(neutral));
        Assert.Contains("neutral", error.Message);
        Assert.Contains("flat-gray", error.Message);
    }

    [Fact]
    public void A_color_left_at_the_default_that_cannot_be_repaired_on_another_neutral_throws()
    {
        // Surfaces that are all the same mid gray leave the default colors no way to reach their ratios.
        var gray = KlyPalette.Custom("mid", [.. Enumerable.Repeat("#808080", 11)]);
        var error = Assert.Throws<InvalidOperationException>(() => KlyThemeTokens.Compute(new KlyColorOptions { Neutral = gray }));
        Assert.Contains("mid", error.Message);
    }

    [Fact]
    public void AllowLowContrast_keeps_the_best_effort_instead()
    {
        var colors = new KlyColorOptions { Primary = FlatPalette.Gray, Neutral = FlatPalette.Gray, AllowLowContrast = true };
        colors.Add("tertiary", FlatPalette.Gray);

        var tokens = KlyThemeTokens.Compute(colors);

        Assert.Equal("#888888", tokens.Light["primary"]);
        Assert.Contains("tertiary", tokens.ExtraColors);
    }

    [Fact]
    public void AllowLowContrast_is_off_by_default_and_read_only_once_frozen()
    {
        var colors = new KlyColorOptions();
        Assert.False(colors.AllowLowContrast);
        colors.Freeze();
        Assert.Throws<InvalidOperationException>(() => colors.AllowLowContrast = true);
    }

    private sealed class Ctx : KlyrooTestContext
    {
        public Ctx(Action<KlyrooOptions> configure) : base(configure) { }
    }

    [Fact]
    public void The_failure_surfaces_when_the_theme_stylesheet_is_first_resolved()
    {
        using var ctx = new Ctx(o => o.Colors.Primary = FlatPalette.Gray);
        Assert.ThrowsAny<Exception>(() => ctx.Render<KlyThemeStyle>());

        using var allowed = new Ctx(o => { o.Colors.Primary = FlatPalette.Gray; o.Colors.AllowLowContrast = true; });
        Assert.Contains("--klyroo-primary:#888888", allowed.Render<KlyThemeStyle>().Find("style").TextContent);
    }
}

public class OklchParsingTests
{
    private static (int, int, int) P(string css) => KlyColorMath.Parse(css);

    [Fact]
    public void A_percentage_chroma_is_scaled_so_that_100_percent_is_0_4()
    {
        Assert.Equal(P("oklch(60% 0.2 120)"), P("oklch(60% 50% 120)"));
        Assert.Equal(P("oklch(60% 0.4 40)"), P("oklch(60% 100% 40)"));
    }

    [Fact]
    public void Lightness_is_a_number_from_0_to_1_or_a_percentage()
    {
        Assert.Equal(P("oklch(70% 0.1 200)"), P("oklch(0.7 0.1 200)"));
    }

    [Theory]
    [InlineData("0.5turn")]
    [InlineData("180deg")]
    [InlineData("200grad")]
    [InlineData("3.14159265358979rad")]
    [InlineData("180")]
    public void The_hue_takes_the_css_angle_units(string hue)
    {
        var expected = P("oklch(60% 0.15 180)");
        var actual = P($"oklch(60% 0.15 {hue})");
        Assert.InRange(Math.Abs(actual.Item1 - expected.Item1) + Math.Abs(actual.Item2 - expected.Item2) + Math.Abs(actual.Item3 - expected.Item3), 0, 1);
    }

    [Theory]
    [InlineData("oklch(NaN 0.1 20)")]
    [InlineData("oklch(50% NaN 20)")]
    [InlineData("oklch(50% 0.1 NaN)")]
    [InlineData("oklch(50% Infinity 20)")]
    [InlineData("oklch(50% 0.1 -Infinity)")]
    [InlineData("oklch(50% 0.1 20egg)")]
    [InlineData("oklch(50% 0.1 20dge)")]
    [InlineData("oklch(50%% 0.1 20)")]
    [InlineData("oklch(50% 0.1deg 20)")]
    [InlineData("oklch(50deg 0.1 20)")]
    [InlineData("oklch(50% 0.1 20 30)")]
    [InlineData("oklch(50% 0.1 1e400)")]
    [InlineData("oklch(50% 0.1 20xyz)")]
    public void Garbage_is_rejected(string value)
    {
        Assert.ThrowsAny<FormatException>(() => P(value));
    }

    [Theory]
    [InlineData("oklch(NaN 0.1 20)")]
    [InlineData("oklch(50% 0.1 20egg)")]
    [InlineData("oklch(50% Infinity 20)")]
    public void A_palette_rejects_it_with_an_argument_exception_naming_the_palette_and_the_value(string value)
    {
        var shades = Enumerable.Repeat("#808080", 10).Append(value).ToArray();
        var error = Assert.Throws<ArgumentException>(() => KlyPalette.Custom("brand", shades));
        Assert.Contains("brand", error.Message);
        Assert.Contains(value, error.Message);
    }

    [Fact]
    public void The_tailwind_values_still_parse_as_before()
    {
        Assert.Equal(P("#2b7fff"), P("oklch(62.3% 0.214 259.815)"), new ChannelComparer(6));
    }

    private sealed class ChannelComparer(int tolerance) : IEqualityComparer<(int, int, int)>
    {
        public bool Equals((int, int, int) x, (int, int, int) y) =>
            Math.Abs(x.Item1 - y.Item1) + Math.Abs(x.Item2 - y.Item2) + Math.Abs(x.Item3 - y.Item3) <= tolerance;

        public int GetHashCode((int, int, int) obj) => 0;
    }
}

public class ScopedPreviewTests : KlyrooTestContext
{
    private static readonly string[] AllTokens = [.. KlyThemeTokens.Defaults.Light.Keys];

    private static string Between(string css, string start, string end)
    {
        var from = css.IndexOf(start, StringComparison.Ordinal);
        Assert.True(from >= 0, $"'{start}' not found in {css[..Math.Min(css.Length, 120)]}");
        from += start.Length;
        return css[from..css.IndexOf(end, from, StringComparison.Ordinal)];
    }

    [Fact]
    public void A_scope_with_the_default_colors_writes_the_complete_token_set_in_light_and_dark()
    {
        var css = Render<KlyThemeStyle>(p => p.Add(s => s.Colors, new KlyColorOptions()).Add(s => s.Scope, "#preview")).Find("style").TextContent;

        var light = Between(css, "#preview{", "}");
        var dark = Between(css, ".dark #preview,#preview.dark{", "}");
        foreach (var token in AllTokens)
        {
            Assert.Contains($"--klyroo-{token}:{KlyThemeTokens.Defaults.Light[token]}", light);
            Assert.Contains($"--klyroo-{token}:{KlyThemeTokens.Defaults.Dark[token]}", dark);
        }
    }

    [Fact]
    public void A_scope_with_a_configured_palette_writes_every_token_too()
    {
        var colors = new KlyColorOptions { Primary = KlyPalette.Red };
        var css = Render<KlyThemeStyle>(p => p.Add(s => s.Colors, colors).Add(s => s.Scope, "#preview")).Find("style").TextContent;

        var light = Between(css, "#preview{", "}");
        var dark = Between(css, ".dark #preview,#preview.dark{", "}");
        foreach (var token in AllTokens)
        {
            Assert.Contains($"--klyroo-{token}:", light);
            Assert.Contains($"--klyroo-{token}:", dark);
        }
        Assert.Contains($"--klyroo-primary:{KlyThemeTokens.Compute(colors).Light["primary"]}", light);
    }

    [Fact]
    public void A_scope_without_colors_previews_the_options_of_the_app_completely()
    {
        var css = Render<KlyThemeStyle>(p => p.Add(s => s.Scope, ".demo")).Find("style").TextContent;
        Assert.StartsWith(".demo{--klyroo-", css);
        Assert.Contains(".dark .demo,.demo.dark{--klyroo-", css);
    }

    [Fact]
    public void Without_a_scope_the_page_still_renders_only_what_differs()
    {
        Assert.Equal("", Render<KlyThemeStyle>().Markup);
        var colors = new KlyColorOptions { Secondary = KlyPalette.Violet };
        var css = Render<KlyThemeStyle>(p => p.Add(s => s.Colors, colors)).Find("style").TextContent;
        Assert.DoesNotContain("--klyroo-primary:", css);
    }

    [Theory]
    [InlineData("#preview\n")]
    [InlineData(".demo\n")]
    [InlineData(":root\n")]
    [InlineData("#a b")]
    [InlineData("a{}b")]
    public void The_scope_must_be_a_simple_selector_without_a_trailing_newline(string scope)
    {
        Assert.ThrowsAny<Exception>(() => Render<KlyThemeStyle>(p => p.Add(s => s.Scope, scope)));
    }
}

public class LightDarkSymmetryTests
{
    private static IEnumerable<KlyColorOptions> Configurations()
    {
        foreach (var neutral in KlyPalette.TailwindPalettes)
        {
            yield return new KlyColorOptions { Neutral = neutral };
            yield return new KlyColorOptions { Neutral = neutral, Primary = KlyPalette.Blue };
        }
        foreach (var palette in KlyPalette.TailwindPalettes)
        {
            yield return new KlyColorOptions { Primary = palette };
            yield return new KlyColorOptions { Error = palette, Warning = palette };
        }
    }

    [Fact]
    public void Every_token_changed_in_one_mode_is_written_in_the_other_too()
    {
        var checkedConfigurations = 0;
        foreach (var colors in Configurations())
        {
            var tokens = KlyThemeTokens.Compute(colors);
            Assert.Equal(tokens.LightChanges.Keys.Order(StringComparer.Ordinal), tokens.DarkChanges.Keys.Order(StringComparer.Ordinal));
            foreach (var (name, value) in tokens.DarkChanges)
            {
                Assert.Equal(tokens.Dark[name], value);
            }
            foreach (var (name, value) in tokens.LightChanges)
            {
                Assert.Equal(tokens.Light[name], value);
            }
            checkedConfigurations++;
        }
        Assert.True(checkedConfigurations > 100);
    }

    [Fact]
    public void The_rendered_root_and_dark_blocks_declare_the_same_tokens()
    {
        foreach (var colors in Configurations())
        {
            var css = KlyThemeTokens.Compute(colors).ToCss(colors)!;
            var root = Regex.Match(css, @"^:root\{(?<b>[^}]*)\}").Groups["b"].Value;
            var dark = Regex.Match(css, @"\.dark\{(?<b>[^}]*)\}").Groups["b"].Value;
            string[] Names(string block) => [.. Regex.Matches(block, @"--klyroo-([a-z0-9-]+):").Select(m => m.Groups[1].Value).Order(StringComparer.Ordinal)];
            Assert.Equal(Names(root), Names(dark));
        }
    }
}

public class ThemeCssTests
{
    private sealed class Ctx : KlyrooTestContext
    {
        public Ctx(Action<KlyrooOptions>? configure) : base(configure) { }
    }

    [Fact]
    public void Concurrent_first_resolutions_share_one_result_and_freeze_the_colors()
    {
        using var ctx = new Ctx(o => o.Colors.Primary = KlyPalette.Blue);
        var options = ctx.Services.GetRequiredService<KlyrooOptions>();

        var results = Enumerable.Range(0, 16).AsParallel().WithDegreeOfParallelism(8).Select(_ => options.ThemeCss).ToArray();

        Assert.NotNull(results[0]);
        Assert.All(results, r => Assert.Same(results[0], r));
        Assert.Throws<InvalidOperationException>(() => options.Colors.Primary = KlyPalette.Red);
    }

    [Fact]
    public async Task The_stylesheet_does_not_lock_on_the_public_colors_object()
    {
        var token = Xunit.TestContext.Current.CancellationToken;
        using var ctx = new Ctx(o => o.Colors.Primary = KlyPalette.Blue);
        var options = ctx.Services.GetRequiredService<KlyrooOptions>();

        // Someone holding a lock on the public object must not block the resolution (or deadlock with it).
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var holder = Task.Run(() =>
        {
            lock (options.Colors)
            {
                entered.Set();
                release.Wait(token);
            }
        }, token);
        entered.Wait(token);

        var resolution = Task.Run(() => options.ThemeCss, token);
        var finished = await Task.WhenAny(resolution, Task.Delay(TimeSpan.FromSeconds(5), token));
        release.Set();
        await holder;

        Assert.Same(resolution, finished);
        Assert.NotNull(await resolution);
    }

    [Fact]
    public void Default_colors_resolve_to_null()
    {
        using var ctx = new Ctx(null);
        Assert.Null(ctx.Services.GetRequiredService<KlyrooOptions>().ThemeCss);
    }
}
