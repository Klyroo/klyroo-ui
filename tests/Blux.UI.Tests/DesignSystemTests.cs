using System.Text.RegularExpressions;
using Blux.UI.Theming;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tests;

public class PaletteDataTests
{
    [Fact]
    public void There_is_one_palette_per_tailwind_v4_color_with_eleven_shades()
    {
        Assert.Equal(26, BxPalette.TailwindPalettes.Count);
        Assert.All(BxPalette.TailwindPalettes, p =>
        {
            Assert.Equal(11, p.Shades.Count);
            Assert.False(p.IsBlux);
        });
        Assert.Equal(26, BxPalette.TailwindPalettes.Select(p => p.Name).Distinct().Count());
    }

    [Theory]
    [InlineData(500, "oklch(62.3% 0.214 259.815)")]
    [InlineData(50, "oklch(97% 0.014 254.604)")]
    public void Shades_are_the_tailwind_values_as_they_are(int shade, string expected)
    {
        Assert.Equal(expected, BxPalette.Blue[shade]);
    }

    [Fact]
    public void Other_known_values_match_theme_css()
    {
        Assert.Equal("oklch(97.1% 0.013 17.38)", BxPalette.Red[50]);
        Assert.Equal("oklch(37.2% 0.044 257.287)", BxPalette.Slate[700]);
        Assert.Equal("oklch(14.1% 0.005 285.823)", BxPalette.Zinc[950]);
    }

    [Theory]
    [InlineData("oklch(63.7% 0.237 25.331)", "#fb2c36")] // red-500, the sRGB value Tailwind documents
    [InlineData("oklch(62.3% 0.214 259.815)", "#2b7fff")] // blue-500
    [InlineData("oklch(100% 0 none)", "#ffffff")]
    [InlineData("oklch(0% 0 0)", "#000000")]
    public void Oklch_converts_to_srgb(string oklch, string expected)
    {
        var (r, g, b) = BxColorMath.Parse(oklch);
        var (er, eg, eb) = BxColorMath.Parse(expected);
        Assert.InRange(Math.Abs(r - er) + Math.Abs(g - eg) + Math.Abs(b - eb), 0, 6);
    }

    [Fact]
    public void A_custom_palette_needs_all_eleven_shades_with_valid_colors()
    {
        var shades = BxPalette.ShadeNumbers.ToDictionary(s => s, s => $"#{s % 256:x2}{s % 256:x2}{s % 256:x2}");
        var palette = BxPalette.Custom("brand", shades);
        Assert.Equal("brand", palette.Name);
        Assert.Equal(shades[500], palette[500]);

        shades.Remove(950);
        Assert.Throws<ArgumentException>(() => BxPalette.Custom("brand", shades));
        Assert.Throws<ArgumentException>(() => BxPalette.Custom("Brand", [.. Enumerable.Repeat("#000", 11)]));
        Assert.Throws<ArgumentException>(() => BxPalette.Custom("brand", [.. Enumerable.Repeat("red", 11)]));
        Assert.Throws<ArgumentException>(() => BxPalette.Custom("brand", "#000", "#111"));
    }

    [Fact]
    public void The_default_palette_has_no_shades()
    {
        Assert.True(BxPalette.Blux.IsBlux);
        Assert.Empty(BxPalette.Blux.Shades);
        Assert.Throws<InvalidOperationException>(() => BxPalette.Blux[500]);
    }
}

public partial class DefaultTokenTests
{
    private static string BaseCss() =>
        File.ReadAllText(Path.Combine(FindLibraryRoot(), "styles", "blux.base.css"));

    private static Dictionary<string, string> Block(string css, string selector)
    {
        var body = Regex.Match(css, selector + @"\s*\{(?<body>[^}]*)\}").Groups["body"].Value;
        return Regex.Matches(body, @"--blux-(?<name>[a-z0-9-]+):\s*(?<hex>#[0-9a-fA-F]{6})\s*;")
            .ToDictionary(m => m.Groups["name"].Value, m => m.Groups["hex"].Value);
    }

    [Fact]
    public void The_default_palette_reproduces_the_tokens_of_blux_base_css_exactly()
    {
        var css = BaseCss();
        var light = Block(css, @":root");
        var dark = Block(css, @"\.dark");

        Assert.Equal(light.OrderBy(p => p.Key), BxThemeTokens.Defaults.Light.OrderBy(p => p.Key));
        Assert.Equal(dark.OrderBy(p => p.Key), BxThemeTokens.Defaults.Dark.OrderBy(p => p.Key));
    }

    [Fact]
    public void The_new_tokens_exist_in_blux_css_and_in_the_tailwind_theme()
    {
        string[] tokens =
        [
            "--blux-text-toned", "--blux-text-inverted", "--blux-bg-inverted", "--blux-border-muted", "--blux-border-inverted",
            "--blux-container", "--blux-header-height",
            "--blux-radius",
        ];
        var root = FindLibraryRoot();
        foreach (var file in new[] { Path.Combine("styles", "blux.base.css"), Path.Combine("wwwroot", "blux.theme.css"), Path.Combine("wwwroot", "blux.css") })
        {
            var css = File.ReadAllText(Path.Combine(root, file));
            foreach (var token in tokens)
            {
                Assert.True(css.Contains(token + ":", StringComparison.Ordinal), $"{token} is missing from {file}");
            }
        }

        // Both modes define the mode-dependent ones.
        var theme = File.ReadAllText(Path.Combine(root, "wwwroot", "blux.theme.css"));
        foreach (var token in new[] { "text-toned", "text-inverted", "bg-inverted", "border-muted", "border-inverted" })
        {
            Assert.Equal(2, Regex.Matches(theme, $@"--blux-{token}:\s*#[0-9a-f]{{6}};").Count);
        }
    }

    [Fact]
    public void The_tailwind_theme_maps_the_semantic_utilities_to_the_runtime_tokens()
    {
        var theme = File.ReadAllText(Path.Combine(FindLibraryRoot(), "wwwroot", "blux.theme.css"));
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
            Assert.Contains($"--color-{name}: var(--blux-{name});", theme);
        }
    }

    [Theory]
    [InlineData("text-default", "color:var(--blux-text)")]
    [InlineData("text-muted", "color:var(--blux-text-muted)")]
    [InlineData("text-dimmed", "color:var(--blux-text-dimmed)")]
    [InlineData("text-toned", "color:var(--blux-text-toned)")]
    [InlineData("text-highlighted", "color:var(--blux-text-highlighted)")]
    [InlineData("text-inverted", "color:var(--blux-text-inverted)")]
    [InlineData("bg-default", "background-color:var(--blux-bg)")]
    [InlineData("bg-muted", "background-color:var(--blux-bg-muted)")]
    [InlineData("bg-elevated", "background-color:var(--blux-bg-elevated)")]
    [InlineData("bg-accented", "background-color:var(--blux-bg-accented)")]
    [InlineData("bg-inverted", "background-color:var(--blux-bg-inverted)")]
    [InlineData("border-default", "border-color:var(--blux-border)")]
    [InlineData("border-muted", "border-color:var(--blux-border-muted)")]
    [InlineData("border-accented", "border-color:var(--blux-border-accented)")]
    [InlineData("border-inverted", "border-color:var(--blux-border-inverted)")]
    [InlineData("bg-primary", "background-color:var(--blux-primary)")]
    [InlineData("border-primary", "border-color:var(--blux-primary)")]
    public void The_utilities_exist_in_a_tailwind_build_of_the_docs_site_and_follow_the_runtime_tokens(string utility, string declaration)
    {
        var site = File.ReadAllText(Path.Combine(FindLibraryRoot(), "..", "..", "playground", "Blux.Playground", "Blux.Playground", "wwwroot", "site.css"));
        Assert.Contains("." + utility + "{" + declaration, site);
    }

    internal static string FindLibraryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Blux.UI");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }
        throw new DirectoryNotFoundException("src/Blux.UI not found above the test output directory.");
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

    public static TheoryData<string> Stylesheets() => ["styles/blux.base.css", "wwwroot/blux.theme.css", "wwwroot/blux.css"];

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
                var target = suffix == "" ? "--bx-color" : "--bx-color" + suffix;
                Assert.Contains($"{target}:var(--blux-chart-{n}{suffix},var(--blux-{role}{suffix}))", css);
            }
        }
        // Nothing on :root or .dark gives the chart tokens a value: that value would be fixed there for every subtree.
        Assert.DoesNotMatch(@"--blux-chart-\d(-text|-contrast)?:", css);
    }

    [Theory]
    [MemberData(nameof(Stylesheets))]
    public void The_radius_utilities_compute_from_blux_radius_at_the_element(string file)
    {
        var css = Squash(Read(file.Split('/')));
        Assert.DoesNotMatch(@"--blux-radius-(xs|sm|md|lg|xl|2xl|3xl):", css);
        if (file.EndsWith("blux.css", StringComparison.Ordinal))
        {
            Assert.Matches(@"\.rounded-sm\{border-radius:calc\(var\(--blux-radius\)\*(\.5|0\.5)\)\}", css);
            Assert.Matches(@"\.rounded-xl\{border-radius:calc\(var\(--blux-radius\)\*1\.5\)\}", css);
        }
        else
        {
            Assert.Contains("--radius-sm:calc(var(--blux-radius)*0.5);", css);
            Assert.Contains("--radius-lg:var(--blux-radius);", css);
            Assert.Contains("--radius-3xl:calc(var(--blux-radius)*3);", css);
        }
    }

    [Fact]
    public void The_header_height_is_read_at_the_element_from_either_name()
    {
        // :root only holds the plain default, so a subtree override of --bx-header-height or --blux-header-height both apply.
        var theme = Squash(Read("styles", "blux.base.css"));
        Assert.Contains("--blux-header-height:4rem;", theme);

        var options = new BluxOptions();
        var classes = string.Join(' ', new[] { options.Header, options.Main, options.PageAside }.SelectMany(t => t.AllClassTokens()));
        Assert.Contains("h-[calc(var(--bx-header-height,var(--blux-header-height))_+_var(--bx-safe-top))]", classes);
        Assert.Contains("min-h-[calc(100dvh_-_var(--bx-header-height,var(--blux-header-height))_-_var(--bx-safe-top)_-_var(--bx-tv-inset,0px))]", classes);
        Assert.Contains("top-[calc(var(--bx-header-height,var(--blux-header-height,0px))_+_var(--bx-safe-top))]", classes);
        Assert.Contains("max-h-[calc(100vh-var(--bx-header-height,var(--blux-header-height,0px))-var(--bx-safe-top))]", classes);
        Assert.DoesNotMatch(@"(^|[\s:\-_])(h|top)-\(--blux-header-height\)|_var\(--blux-header-height\)\)", classes);
    }
}

public class ThemeStyleTests : BluxTestContext
{
    public ThemeStyleTests() : base(o =>
    {
        o.Colors.Primary = BxPalette.Blue;
        o.Colors.Neutral = BxPalette.Zinc;
        o.Colors.Add("tertiary", BxPalette.Indigo);
    })
    {
    }

    [Fact]
    public void Renders_the_variables_of_the_configured_palettes_and_a_rule_for_the_extra_color()
    {
        var css = Render<BxThemeStyle>().Find("style[data-bx-theme]").TextContent;

        Assert.StartsWith(":root{--blux-", css);
        Assert.Contains(".dark{--blux-", css);
        Assert.Matches(@":root\{[^}]*--blux-primary:#[0-9a-f]{6};--blux-primary-text:#[0-9a-f]{6};--blux-primary-contrast:#[0-9a-f]{6}", css);
        Assert.Matches(@":root\{[^}]*--blux-tertiary:#[0-9a-f]{6};--blux-tertiary-text:#[0-9a-f]{6};--blux-tertiary-contrast:#[0-9a-f]{6}", css);
        Assert.Matches(@":root\{[^}]*--blux-bg-muted:#[0-9a-f]{6}", css);
        Assert.Contains("[data-color='tertiary']{--bx-color:var(--blux-tertiary);--bx-color-text:var(--blux-tertiary-text);--bx-color-contrast:var(--blux-tertiary-contrast)}", css);
        // Only the configured colors: the secondary one stays the library's.
        Assert.DoesNotContain("--blux-secondary:", css);
    }

    [Fact]
    public void The_output_is_the_same_on_every_render()
    {
        Assert.Equal(Render<BxThemeStyle>().Markup, Render<BxThemeStyle>().Markup);
    }

    [Fact]
    public void A_custom_semantic_color_renders_data_color()
    {
        var button = Render<BxButton>(p => p.Add(b => b.Color, "tertiary")).Find("button");
        Assert.Equal("tertiary", button.GetAttribute("data-color"));
    }

    [Fact]
    public void The_colors_are_read_only_once_the_style_rendered()
    {
        var options = Services.GetRequiredService<BluxOptions>();
        Render<BxThemeStyle>();
        Assert.Throws<InvalidOperationException>(() => options.Colors.Primary = BxPalette.Red);
        Assert.Throws<InvalidOperationException>(() => options.Colors.Add("other", BxPalette.Red));
    }

    [Fact]
    public void A_scope_receives_the_variables_for_light_and_dark_instead_of_root()
    {
        var colors = new BxColorOptions { Primary = BxPalette.Red };
        var css = Render<BxThemeStyle>(p => p.Add(s => s.Colors, colors).Add(s => s.Scope, "#preview")).Find("style").TextContent;

        Assert.StartsWith("#preview{--blux-primary:", css);
        Assert.Contains(".dark #preview,#preview.dark{--blux-primary:", css);
        Assert.DoesNotContain(":root", css);
    }

    [Fact]
    public void A_scope_must_be_a_simple_selector()
    {
        Assert.ThrowsAny<Exception>(() => Render<BxThemeStyle>(p => p.Add(s => s.Scope, "a{}b")));
    }

    [Fact]
    public void The_nonce_is_rendered_on_the_style()
    {
        var style = Render<BxThemeStyle>(p => p.Add(s => s.Nonce, "abc")).Find("style");
        Assert.Equal("abc", style.GetAttribute("nonce"));
    }
}

public class DefaultThemeStyleTests : BluxTestContext
{
    [Fact]
    public void Renders_nothing_with_the_default_colors()
    {
        Assert.Equal("", Render<BxThemeStyle>().Markup);
    }

    [Fact]
    public void Assigning_the_blux_palette_keeps_the_default()
    {
        var colors = new BxColorOptions { Primary = BxPalette.Blux };
        Assert.True(colors.IsDefault);
        Assert.Null(BxThemeTokens.Compute(colors).ToCss(colors));
    }

    [Fact]
    public void A_single_configured_color_only_changes_its_own_tokens()
    {
        var colors = new BxColorOptions { Secondary = BxPalette.Violet };
        var tokens = BxThemeTokens.Compute(colors);
        foreach (var changes in new[] { tokens.LightChanges, tokens.DarkChanges })
        {
            Assert.Contains("secondary", changes.Keys);
            Assert.All(changes.Keys, k => Assert.StartsWith("secondary", k));
        }
    }

    [Fact]
    public void Built_in_names_cannot_be_added_as_extra_colors_and_extra_palettes_need_shades()
    {
        var colors = new BxColorOptions();
        Assert.Throws<ArgumentException>(() => colors.Add("primary", BxPalette.Blue));
        Assert.Throws<ArgumentException>(() => colors.Add("Tertiary", BxPalette.Blue));
        Assert.Throws<ArgumentException>(() => colors.Add("chart-1", BxPalette.Blue));
        Assert.Throws<ArgumentException>(() => colors.Add("tertiary", BxPalette.Blux));
    }
}

public class DefaultsReachEveryComponentTests
{
    private sealed class Ctx : BluxTestContext
    {
        public Ctx(Action<BluxOptions>? configure) : base(configure) { }
    }

    [Fact]
    public void Defaults_size_reaches_the_file_upload()
    {
        using var large = new Ctx(o => o.Defaults.Size = BxSize.Xl);
        using var plain = new Ctx(null);

        static string Stable(string markup) => Regex.Replace(markup, "bx-fileupload-[0-9a-f]+", "id");
        var byDefault = Stable(large.Render<BxFileUpload>().Markup);
        Assert.Equal(Stable(plain.Render<BxFileUpload>(p => p.Add(f => f.Size, BxSize.Xl)).Markup), byDefault);
        Assert.NotEqual(Stable(plain.Render<BxFileUpload>().Markup), byDefault);
    }

    [Fact]
    public void Defaults_color_reaches_the_submit_button_of_the_chat_prompt()
    {
        using var ctx = new Ctx(o => o.Defaults.Color = BxColor.Secondary);
        Assert.Equal("secondary", ctx.Render<BxChatPromptSubmit>().Find("button").GetAttribute("data-color"));
    }

    [Fact]
    public void Defaults_color_reaches_the_badges_of_a_blog_post_and_a_changelog_version()
    {
        using var ctx = new Ctx(o => o.Defaults.Color = BxColor.Secondary);
        var post = ctx.Render<BxBlogPost>(p => p.Add(c => c.Badge, "New"));
        Assert.Contains(post.FindAll("span[data-color]"), e => e.TextContent.Trim() == "New" && e.GetAttribute("data-color") == "secondary");
        var version = ctx.Render<BxChangelogVersion>(p => p.Add(c => c.Badge, "New"));
        Assert.Contains(version.FindAll("span[data-color]"), e => e.TextContent.Trim() == "New" && e.GetAttribute("data-color") == "secondary");
    }

    [Fact]
    public void Defaults_color_reaches_the_buttons_of_a_pricing_table_and_of_page_links()
    {
        using var ctx = new Ctx(o => o.Defaults.Color = BxColor.Secondary);
        var table = ctx.Render<BxPricingTable>(p => p.Add(c => c.Tiers, new BxPricingTier[] { new("a", "A") { ButtonLabel = "Go", ButtonHref = "/a" } }));
        Assert.Equal("secondary", table.Find("a").GetAttribute("data-color"));

        var hero = ctx.Render<BxPageHero>(p => p.Add(c => c.Links, new[] { new PageLinkItem("Start", "/s"), new PageLinkItem("Docs", "/d") { Color = BxColor.Error } }));
        var colors = hero.FindAll("a[data-color]").Select(a => a.GetAttribute("data-color")).ToArray();
        Assert.Equal(["secondary", "error"], colors);
    }
}

public class GlobalDefaultsTests
{
    private sealed class Ctx : BluxTestContext
    {
        public Ctx(Action<BluxOptions>? configure) : base(configure) { }
    }

    [Fact]
    public void Defaults_color_applies_when_the_parameter_is_not_set_and_an_explicit_color_wins()
    {
        using var ctx = new Ctx(o => o.Defaults.Color = BxColor.Secondary);

        Assert.Equal("secondary", ctx.Render<BxButton>().Find("button").GetAttribute("data-color"));
        Assert.Equal("secondary", ctx.Render<BxBadge>().Find("span").GetAttribute("data-color"));
        Assert.Equal("primary", ctx.Render<BxButton>(p => p.Add(b => b.Color, BxColor.Primary)).Find("button").GetAttribute("data-color"));
        Assert.Equal("error", ctx.Render<BxButton>(p => p.Add(b => b.Color, BxColor.Error)).Find("button").GetAttribute("data-color"));
    }

    [Fact]
    public void Defaults_color_does_not_touch_components_that_are_neutral_by_design()
    {
        using var ctx = new Ctx(o => o.Defaults.Color = BxColor.Secondary);

        Assert.Equal("neutral", ctx.Render<BxKbd>().Find("kbd").GetAttribute("data-color"));
    }

    [Fact]
    public void Defaults_size_applies_when_the_parameter_is_not_set()
    {
        using var large = new Ctx(o => o.Defaults.Size = BxSize.Lg);
        using var plain = new Ctx(null);

        var byDefault = large.Render<BxButton>().Find("button").ClassName;
        var explicitLarge = plain.Render<BxButton>(p => p.Add(b => b.Size, BxSize.Lg)).Find("button").ClassName;
        var medium = plain.Render<BxButton>().Find("button").ClassName;

        Assert.Equal(explicitLarge, byDefault);
        Assert.NotEqual(medium, byDefault);
        Assert.Equal(medium, large.Render<BxButton>(p => p.Add(b => b.Size, BxSize.Md)).Find("button").ClassName);
    }

    [Fact]
    public void Defaults_size_reaches_the_fields()
    {
        using var large = new Ctx(o => o.Defaults.Size = BxSize.Lg);
        using var plain = new Ctx(null);

        Assert.Equal(
            plain.Render<BxInput>(p => p.Add(i => i.Size, BxSize.Lg)).Markup.Length,
            large.Render<BxInput>().Markup.Length);
    }

    [Fact]
    public void The_defaults_are_read_only_once_a_component_rendered()
    {
        using var ctx = new Ctx(o => o.Defaults.Color = BxColor.Secondary);
        var options = ctx.Services.GetRequiredService<BluxOptions>();
        ctx.Render<BxButton>();

        Assert.Throws<InvalidOperationException>(() => options.Defaults.Color = BxColor.Error);
        Assert.Throws<InvalidOperationException>(() => options.Defaults.Size = BxSize.Lg);
    }

    [Fact]
    public void Reading_a_parameter_before_the_services_are_injected_returns_the_built_in_default()
    {
        // Blazor reads parameters through reflection (DynamicComponent, tooling) on a component that has no services yet.
        Assert.Equal(BxColor.Primary, new BxButton().Color);
        Assert.Equal(BxSize.Md, new BxButton().Size);
    }

    [Fact]
    public void Without_defaults_the_components_keep_their_own()
    {
        using var ctx = new Ctx(null);
        Assert.Equal("primary", ctx.Render<BxButton>().Find("button").GetAttribute("data-color"));
    }
}
