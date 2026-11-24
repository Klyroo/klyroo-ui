using System.Text.RegularExpressions;
using Lux.UI.Theming;
using Microsoft.Extensions.DependencyInjection;

namespace Lux.UI.Tests;

public class PaletteDataTests
{
    [Fact]
    public void There_is_one_palette_per_tailwind_v4_color_with_eleven_shades()
    {
        Assert.Equal(26, LxPalette.TailwindPalettes.Count);
        Assert.All(LxPalette.TailwindPalettes, p =>
        {
            Assert.Equal(11, p.Shades.Count);
            Assert.False(p.IsLux);
        });
        Assert.Equal(26, LxPalette.TailwindPalettes.Select(p => p.Name).Distinct().Count());
    }

    [Theory]
    [InlineData(500, "oklch(62.3% 0.214 259.815)")]
    [InlineData(50, "oklch(97% 0.014 254.604)")]
    public void Shades_are_the_tailwind_values_as_they_are(int shade, string expected)
    {
        Assert.Equal(expected, LxPalette.Blue[shade]);
    }

    [Fact]
    public void Other_known_values_match_theme_css()
    {
        Assert.Equal("oklch(97.1% 0.013 17.38)", LxPalette.Red[50]);
        Assert.Equal("oklch(37.2% 0.044 257.287)", LxPalette.Slate[700]);
        Assert.Equal("oklch(14.1% 0.005 285.823)", LxPalette.Zinc[950]);
    }

    [Theory]
    [InlineData("oklch(63.7% 0.237 25.331)", "#fb2c36")] // red-500, the sRGB value Tailwind documents
    [InlineData("oklch(62.3% 0.214 259.815)", "#2b7fff")] // blue-500
    [InlineData("oklch(100% 0 none)", "#ffffff")]
    [InlineData("oklch(0% 0 0)", "#000000")]
    public void Oklch_converts_to_srgb(string oklch, string expected)
    {
        var (r, g, b) = LxColorMath.Parse(oklch);
        var (er, eg, eb) = LxColorMath.Parse(expected);
        Assert.InRange(Math.Abs(r - er) + Math.Abs(g - eg) + Math.Abs(b - eb), 0, 6);
    }

    [Fact]
    public void A_custom_palette_needs_all_eleven_shades_with_valid_colors()
    {
        var shades = LxPalette.ShadeNumbers.ToDictionary(s => s, s => $"#{s % 256:x2}{s % 256:x2}{s % 256:x2}");
        var palette = LxPalette.Custom("brand", shades);
        Assert.Equal("brand", palette.Name);
        Assert.Equal(shades[500], palette[500]);

        shades.Remove(950);
        Assert.Throws<ArgumentException>(() => LxPalette.Custom("brand", shades));
        Assert.Throws<ArgumentException>(() => LxPalette.Custom("Brand", [.. Enumerable.Repeat("#000", 11)]));
        Assert.Throws<ArgumentException>(() => LxPalette.Custom("brand", [.. Enumerable.Repeat("red", 11)]));
        Assert.Throws<ArgumentException>(() => LxPalette.Custom("brand", "#000", "#111"));
    }

    [Fact]
    public void The_default_palette_has_no_shades()
    {
        Assert.True(LxPalette.Lux.IsLux);
        Assert.Empty(LxPalette.Lux.Shades);
        Assert.Throws<InvalidOperationException>(() => LxPalette.Lux[500]);
    }
}

public partial class DefaultTokenTests
{
    private static string BaseCss() =>
        File.ReadAllText(Path.Combine(FindLibraryRoot(), "styles", "lux.base.css"));

    private static Dictionary<string, string> Block(string css, string selector)
    {
        var body = Regex.Match(css, selector + @"\s*\{(?<body>[^}]*)\}").Groups["body"].Value;
        return Regex.Matches(body, @"--lux-(?<name>[a-z0-9-]+):\s*(?<hex>#[0-9a-fA-F]{6})\s*;")
            .ToDictionary(m => m.Groups["name"].Value, m => m.Groups["hex"].Value);
    }

    [Fact]
    public void The_default_palette_reproduces_the_tokens_of_lux_base_css_exactly()
    {
        var css = BaseCss();
        var light = Block(css, @":root");
        var dark = Block(css, @"\.dark");

        Assert.Equal(light.OrderBy(p => p.Key), LxThemeTokens.Defaults.Light.OrderBy(p => p.Key));
        Assert.Equal(dark.OrderBy(p => p.Key), LxThemeTokens.Defaults.Dark.OrderBy(p => p.Key));
    }

    [Fact]
    public void The_new_tokens_exist_in_lux_css_and_in_the_tailwind_theme()
    {
        string[] tokens =
        [
            "--lux-text-toned", "--lux-text-inverted", "--lux-bg-inverted", "--lux-border-muted", "--lux-border-inverted",
            "--lux-container", "--lux-header-height",
            "--lux-radius",
        ];
        var root = FindLibraryRoot();
        foreach (var file in new[] { Path.Combine("styles", "lux.base.css"), Path.Combine("wwwroot", "lux.theme.css"), Path.Combine("wwwroot", "lux.css") })
        {
            var css = File.ReadAllText(Path.Combine(root, file));
            foreach (var token in tokens)
            {
                Assert.True(css.Contains(token + ":", StringComparison.Ordinal), $"{token} is missing from {file}");
            }
        }

        // Both modes define the mode-dependent ones.
        var theme = File.ReadAllText(Path.Combine(root, "wwwroot", "lux.theme.css"));
        foreach (var token in new[] { "text-toned", "text-inverted", "bg-inverted", "border-muted", "border-inverted" })
        {
            Assert.Equal(2, Regex.Matches(theme, $@"--lux-{token}:\s*#[0-9a-f]{{6}};").Count);
        }
    }

    [Fact]
    public void The_tailwind_theme_maps_the_semantic_utilities_to_the_runtime_tokens()
    {
        var theme = File.ReadAllText(Path.Combine(FindLibraryRoot(), "wwwroot", "lux.theme.css"));
        Assert.Contains("@theme inline", theme);
        foreach (var name in new[] { "dimmed", "muted", "toned", "default", "highlighted", "inverted" })
        {
            Assert.Contains($"--text-color-{name}:", theme);
        }
        foreach (var name in new[] { "default", "muted", "elevated", "accented", "inverted" })
        {
            Assert.Contains($"--background-color-{name}:", theme);
        }
        foreach (var name in new[] { "default", "muted", "accented", "inverted" })
        {
            Assert.Contains($"--border-color-{name}:", theme);
        }
        foreach (var name in new[] { "primary", "secondary", "success", "info", "warning", "error", "neutral" })
        {
            Assert.Contains($"--color-{name}: var(--lux-{name});", theme);
        }
    }

    [Theory]
    [InlineData("text-default", "color:var(--lux-text)")]
    [InlineData("text-muted", "color:var(--lux-text-muted)")]
    [InlineData("text-dimmed", "color:var(--lux-text-dimmed)")]
    [InlineData("text-toned", "color:var(--lux-text-toned)")]
    [InlineData("text-highlighted", "color:var(--lux-text-highlighted)")]
    [InlineData("text-inverted", "color:var(--lux-text-inverted)")]
    [InlineData("bg-default", "background-color:var(--lux-bg)")]
    [InlineData("bg-muted", "background-color:var(--lux-bg-muted)")]
    [InlineData("bg-elevated", "background-color:var(--lux-bg-elevated)")]
    [InlineData("bg-accented", "background-color:var(--lux-bg-accented)")]
    [InlineData("bg-inverted", "background-color:var(--lux-bg-inverted)")]
    [InlineData("border-default", "border-color:var(--lux-border)")]
    [InlineData("border-muted", "border-color:var(--lux-border-muted)")]
    [InlineData("border-accented", "border-color:var(--lux-border-accented)")]
    [InlineData("border-inverted", "border-color:var(--lux-border-inverted)")]
    [InlineData("bg-primary", "background-color:var(--lux-primary)")]
    [InlineData("border-primary", "border-color:var(--lux-primary)")]
    public void The_utilities_exist_in_a_tailwind_build_of_the_docs_site_and_follow_the_runtime_tokens(string utility, string declaration)
    {
        var site = File.ReadAllText(Path.Combine(FindLibraryRoot(), "..", "..", "..", "lux.stack", "site", "Lux.Stack.Site", "wwwroot", "site.css"));
        Assert.Contains("." + utility + "{" + declaration, site);
    }

    internal static string FindLibraryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Lux.UI");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }
        throw new DirectoryNotFoundException("src/Lux.UI not found above the test output directory.");
    }
}

/// <summary>
/// A variable set on a subtree must reach everything inside it: an expression that reads another variable has to be evaluated
/// at the element, not once on <c>:root</c>, so the generated stylesheets must not derive tokens on <c>:root</c>.
/// </summary>
public class SubtreeOverrideTests
{
    private static string Read(params string[] path) =>
        File.ReadAllText(Path.Combine([DefaultTokenTests.FindLibraryRoot(), .. path]));

    private static string Squash(string css) => Regex.Replace(css, @"\s+", "");

    public static TheoryData<string> Stylesheets() => ["styles/lux.base.css", "wwwroot/lux.theme.css", "wwwroot/lux.css"];

    [Theory]
    [MemberData(nameof(Stylesheets))]
    public void Chart_series_resolve_at_the_element_with_the_semantic_colors_as_fallback(string file)
    {
        var css = Squash(Read(file.Split('/')));
        var pairs = new[] { ("1", "primary"), ("2", "secondary"), ("3", "info"), ("4", "error"), ("5", "success"), ("6", "warning") };
        foreach (var (n, role) in pairs)
        {
            foreach (var suffix in new[] { "", "-text", "-contrast" })
            {
                var target = suffix == "" ? "--lx-color" : "--lx-color" + suffix;
                Assert.Contains($"{target}:var(--lux-chart-{n}{suffix},var(--lux-{role}{suffix}))", css);
            }
        }
        // Nothing on :root or .dark gives the chart tokens a value: that value would be fixed there for every subtree.
        Assert.DoesNotMatch(@"--lux-chart-\d(-text|-contrast)?:", css);
    }

    [Theory]
    [MemberData(nameof(Stylesheets))]
    public void The_radius_utilities_compute_from_lux_radius_at_the_element(string file)
    {
        var css = Squash(Read(file.Split('/')));
        Assert.DoesNotMatch(@"--lux-radius-(xs|sm|md|lg|xl|2xl|3xl):", css);
        if (file.EndsWith("lux.css", StringComparison.Ordinal))
        {
            Assert.Matches(@"\.rounded-sm\{border-radius:calc\(var\(--lux-radius\)\*(\.5|0\.5)\)\}", css);
            Assert.Matches(@"\.rounded-xl\{border-radius:calc\(var\(--lux-radius\)\*1\.5\)\}", css);
        }
        else
        {
            Assert.Contains("--radius-sm:calc(var(--lux-radius)*0.5);", css);
            Assert.Contains("--radius-lg:var(--lux-radius);", css);
            Assert.Contains("--radius-3xl:calc(var(--lux-radius)*3);", css);
        }
    }

    [Fact]
    public void The_header_height_is_read_at_the_element_from_either_name()
    {
        // :root only holds the plain default, so a subtree override of --lx-header-height or --lux-header-height both apply.
        var theme = Squash(Read("styles", "lux.base.css"));
        Assert.Contains("--lux-header-height:4rem;", theme);

        var options = new LuxOptions();
        var classes = string.Join(' ', new[] { options.Header, options.Main, options.PageAside }.SelectMany(t => t.AllClassTokens()));
        Assert.Contains("h-[calc(var(--lx-header-height,var(--lux-header-height))_+_var(--lx-safe-top))]", classes);
        Assert.Contains("min-h-[calc(100dvh_-_var(--lx-header-height,var(--lux-header-height))_-_var(--lx-safe-top)_-_var(--lx-tv-inset,0px))]", classes);
        Assert.Contains("top-[calc(var(--lx-header-height,var(--lux-header-height,0px))_+_var(--lx-safe-top))]", classes);
        Assert.Contains("max-h-[calc(100vh-var(--lx-header-height,var(--lux-header-height,0px))-var(--lx-safe-top))]", classes);
        Assert.DoesNotMatch(@"(^|[\s:\-_])(h|top)-\(--lux-header-height\)|_var\(--lux-header-height\)\)", classes);
    }
}

public class ThemeStyleTests : LuxTestContext
{
    public ThemeStyleTests() : base(o =>
    {
        o.Colors.Primary = LxPalette.Blue;
        o.Colors.Neutral = LxPalette.Zinc;
        o.Colors.Add("tertiary", LxPalette.Indigo);
    })
    {
    }

    [Fact]
    public void Renders_the_variables_of_the_configured_palettes_and_a_rule_for_the_extra_color()
    {
        var css = Render<LxThemeStyle>().Find("style[data-lx-theme]").TextContent;

        Assert.StartsWith(":root{--lux-", css);
        Assert.Contains(".dark{--lux-", css);
        Assert.Matches(@":root\{[^}]*--lux-primary:#[0-9a-f]{6};--lux-primary-text:#[0-9a-f]{6};--lux-primary-contrast:#[0-9a-f]{6}", css);
        Assert.Matches(@":root\{[^}]*--lux-tertiary:#[0-9a-f]{6};--lux-tertiary-text:#[0-9a-f]{6};--lux-tertiary-contrast:#[0-9a-f]{6}", css);
        Assert.Matches(@":root\{[^}]*--lux-bg-muted:#[0-9a-f]{6}", css);
        Assert.Contains("[data-color='tertiary']{--lx-color:var(--lux-tertiary);--lx-color-text:var(--lux-tertiary-text);--lx-color-contrast:var(--lux-tertiary-contrast)}", css);
        // Only the configured colors: the secondary one stays the library's.
        Assert.DoesNotContain("--lux-secondary:", css);
    }

    [Fact]
    public void The_output_is_the_same_on_every_render()
    {
        Assert.Equal(Render<LxThemeStyle>().Markup, Render<LxThemeStyle>().Markup);
    }

    [Fact]
    public void A_custom_semantic_color_renders_data_color()
    {
        var button = Render<LxButton>(p => p.Add(b => b.Color, "tertiary")).Find("button");
        Assert.Equal("tertiary", button.GetAttribute("data-color"));
    }

    [Fact]
    public void The_colors_are_read_only_once_the_style_rendered()
    {
        var options = Services.GetRequiredService<LuxOptions>();
        Render<LxThemeStyle>();
        Assert.Throws<InvalidOperationException>(() => options.Colors.Primary = LxPalette.Red);
        Assert.Throws<InvalidOperationException>(() => options.Colors.Add("other", LxPalette.Red));
    }

    [Fact]
    public void A_scope_receives_the_variables_for_light_and_dark_instead_of_root()
    {
        var colors = new LxColorOptions { Primary = LxPalette.Red };
        var css = Render<LxThemeStyle>(p => p.Add(s => s.Colors, colors).Add(s => s.Scope, "#preview")).Find("style").TextContent;

        Assert.StartsWith("#preview{--lux-primary:", css);
        Assert.Contains(".dark #preview,#preview.dark{--lux-primary:", css);
        Assert.DoesNotContain(":root", css);
    }

    [Fact]
    public void A_scope_must_be_a_simple_selector()
    {
        Assert.ThrowsAny<Exception>(() => Render<LxThemeStyle>(p => p.Add(s => s.Scope, "a{}b")));
    }

    [Fact]
    public void The_nonce_is_rendered_on_the_style()
    {
        var style = Render<LxThemeStyle>(p => p.Add(s => s.Nonce, "abc")).Find("style");
        Assert.Equal("abc", style.GetAttribute("nonce"));
    }
}

public class DefaultThemeStyleTests : LuxTestContext
{
    [Fact]
    public void Renders_nothing_with_the_default_colors()
    {
        Assert.Equal("", Render<LxThemeStyle>().Markup);
    }

    [Fact]
    public void Assigning_the_lux_palette_keeps_the_default()
    {
        var colors = new LxColorOptions { Primary = LxPalette.Lux };
        Assert.True(colors.IsDefault);
        Assert.Null(LxThemeTokens.Compute(colors).ToCss(colors));
    }

    [Fact]
    public void A_single_configured_color_only_changes_its_own_tokens()
    {
        var colors = new LxColorOptions { Secondary = LxPalette.Violet };
        var tokens = LxThemeTokens.Compute(colors);
        foreach (var changes in new[] { tokens.LightChanges, tokens.DarkChanges })
        {
            Assert.Contains("secondary", changes.Keys);
            Assert.All(changes.Keys, k => Assert.StartsWith("secondary", k));
        }
    }

    [Fact]
    public void Built_in_names_cannot_be_added_as_extra_colors_and_extra_palettes_need_shades()
    {
        var colors = new LxColorOptions();
        Assert.Throws<ArgumentException>(() => colors.Add("primary", LxPalette.Blue));
        Assert.Throws<ArgumentException>(() => colors.Add("Tertiary", LxPalette.Blue));
        Assert.Throws<ArgumentException>(() => colors.Add("chart-1", LxPalette.Blue));
        Assert.Throws<ArgumentException>(() => colors.Add("tertiary", LxPalette.Lux));
    }
}

public class DefaultsReachEveryComponentTests
{
    private sealed class Ctx : LuxTestContext
    {
        public Ctx(Action<LuxOptions>? configure) : base(configure) { }
    }

    [Fact]
    public void Defaults_size_reaches_the_file_upload()
    {
        using var large = new Ctx(o => o.Defaults.Size = LxSize.Xl);
        using var plain = new Ctx(null);

        static string Stable(string markup) => Regex.Replace(markup, "lx-fileupload-[0-9a-f]+", "id");
        var byDefault = Stable(large.Render<LxFileUpload>().Markup);
        Assert.Equal(Stable(plain.Render<LxFileUpload>(p => p.Add(f => f.Size, LxSize.Xl)).Markup), byDefault);
        Assert.NotEqual(Stable(plain.Render<LxFileUpload>().Markup), byDefault);
    }

    [Fact]
    public void Defaults_color_reaches_the_submit_button_of_the_chat_prompt()
    {
        using var ctx = new Ctx(o => o.Defaults.Color = LxColor.Secondary);
        Assert.Equal("secondary", ctx.Render<LxChatPromptSubmit>().Find("button").GetAttribute("data-color"));
    }

    [Fact]
    public void Defaults_color_reaches_the_badges_of_a_blog_post_and_a_changelog_version()
    {
        using var ctx = new Ctx(o => o.Defaults.Color = LxColor.Secondary);
        var post = ctx.Render<LxBlogPost>(p => p.Add(c => c.Badge, "New"));
        Assert.Contains(post.FindAll("span[data-color]"), e => e.TextContent.Trim() == "New" && e.GetAttribute("data-color") == "secondary");
        var version = ctx.Render<LxChangelogVersion>(p => p.Add(c => c.Badge, "New"));
        Assert.Contains(version.FindAll("span[data-color]"), e => e.TextContent.Trim() == "New" && e.GetAttribute("data-color") == "secondary");
    }

    [Fact]
    public void Defaults_color_reaches_the_buttons_of_a_pricing_table_and_of_page_links()
    {
        using var ctx = new Ctx(o => o.Defaults.Color = LxColor.Secondary);
        var table = ctx.Render<LxPricingTable>(p => p.Add(c => c.Tiers, new LxPricingTier[] { new("a", "A") { ButtonLabel = "Go", ButtonHref = "/a" } }));
        Assert.Equal("secondary", table.Find("a").GetAttribute("data-color"));

        var hero = ctx.Render<LxPageHero>(p => p.Add(c => c.Links, new[] { new PageLinkItem("Start", "/s"), new PageLinkItem("Docs", "/d") { Color = LxColor.Error } }));
        var colors = hero.FindAll("a[data-color]").Select(a => a.GetAttribute("data-color")).ToArray();
        Assert.Equal(["secondary", "error"], colors);
    }
}

public class GlobalDefaultsTests
{
    private sealed class Ctx : LuxTestContext
    {
        public Ctx(Action<LuxOptions>? configure) : base(configure) { }
    }

    [Fact]
    public void Defaults_color_applies_when_the_parameter_is_not_set_and_an_explicit_color_wins()
    {
        using var ctx = new Ctx(o => o.Defaults.Color = LxColor.Secondary);

        Assert.Equal("secondary", ctx.Render<LxButton>().Find("button").GetAttribute("data-color"));
        Assert.Equal("secondary", ctx.Render<LxBadge>().Find("span").GetAttribute("data-color"));
        Assert.Equal("primary", ctx.Render<LxButton>(p => p.Add(b => b.Color, LxColor.Primary)).Find("button").GetAttribute("data-color"));
        Assert.Equal("error", ctx.Render<LxButton>(p => p.Add(b => b.Color, LxColor.Error)).Find("button").GetAttribute("data-color"));
    }

    [Fact]
    public void Defaults_color_does_not_touch_components_that_are_neutral_by_design()
    {
        using var ctx = new Ctx(o => o.Defaults.Color = LxColor.Secondary);

        Assert.Equal("neutral", ctx.Render<LxKbd>().Find("kbd").GetAttribute("data-color"));
    }

    [Fact]
    public void Defaults_size_applies_when_the_parameter_is_not_set()
    {
        using var large = new Ctx(o => o.Defaults.Size = LxSize.Lg);
        using var plain = new Ctx(null);

        var byDefault = large.Render<LxButton>().Find("button").ClassName;
        var explicitLarge = plain.Render<LxButton>(p => p.Add(b => b.Size, LxSize.Lg)).Find("button").ClassName;
        var medium = plain.Render<LxButton>().Find("button").ClassName;

        Assert.Equal(explicitLarge, byDefault);
        Assert.NotEqual(medium, byDefault);
        Assert.Equal(medium, large.Render<LxButton>(p => p.Add(b => b.Size, LxSize.Md)).Find("button").ClassName);
    }

    [Fact]
    public void Defaults_size_reaches_the_fields()
    {
        using var large = new Ctx(o => o.Defaults.Size = LxSize.Lg);
        using var plain = new Ctx(null);

        Assert.Equal(
            plain.Render<LxInput>(p => p.Add(i => i.Size, LxSize.Lg)).Markup.Length,
            large.Render<LxInput>().Markup.Length);
    }

    [Fact]
    public void The_defaults_are_read_only_once_a_component_rendered()
    {
        using var ctx = new Ctx(o => o.Defaults.Color = LxColor.Secondary);
        var options = ctx.Services.GetRequiredService<LuxOptions>();
        ctx.Render<LxButton>();

        Assert.Throws<InvalidOperationException>(() => options.Defaults.Color = LxColor.Error);
        Assert.Throws<InvalidOperationException>(() => options.Defaults.Size = LxSize.Lg);
    }

    [Fact]
    public void Reading_a_parameter_before_the_services_are_injected_returns_the_built_in_default()
    {
        // Blazor reads parameters through reflection (DynamicComponent, tooling) on a component that has no services yet.
        Assert.Equal(LxColor.Primary, new LxButton().Color);
        Assert.Equal(LxSize.Md, new LxButton().Size);
    }

    [Fact]
    public void Without_defaults_the_components_keep_their_own()
    {
        using var ctx = new Ctx(null);
        Assert.Equal("primary", ctx.Render<LxButton>().Find("button").GetAttribute("data-color"));
    }
}
