using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>The palette preview of the ThemeStyle page: the tokens computed in C# reach the page as computed colors.</summary>
public class DesignSystemTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private static string Rgb(string hex) =>
        $"rgb({Convert.ToInt32(hex[1..3], 16)}, {Convert.ToInt32(hex[3..5], 16)}, {Convert.ToInt32(hex[5..7], 16)})";

    /// <summary>The light fill of the primary color in the preview's style, as the browser reports a color.</summary>
    private async Task<string> PreviewPrimary()
    {
        var css = await Page.Locator("#palette-demo style").First.TextContentAsync() ?? "";
        var match = Regex.Match(css, @"^#palette-demo\{[^}]*--blux-primary:(#[0-9a-f]{6});");
        Assert.True(match.Success, css);
        return Rgb(match.Groups[1].Value);
    }

    private ILocator PrimaryButton => Page.GetByTestId("palette-primary");

    private Task<string> Background(ILocator locator) =>
        locator.EvaluateAsync<string>("e => getComputedStyle(e).backgroundColor");

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_configured_palette_gives_the_components_its_colors(string mode)
    {
        await Open(mode, "theme-style");

        var expected = await PreviewPrimary();
        Assert.NotEqual("rgb(255, 210, 63)", expected); // not the library's yellow: the palette applies
        await Assertions.Expect(PrimaryButton).ToHaveCSSAsync("background-color", expected);
        // The extra semantic color has its own rule: no hand-written CSS.
        var tertiary = await Background(Page.GetByTestId("palette-tertiary"));
        Assert.NotEqual(expected, tertiary);
        Assert.NotEqual("rgba(0, 0, 0, 0)", tertiary);
    }

    [Fact]
    public async Task The_page_outside_the_preview_keeps_the_default_colors()
    {
        await Open("ssr", "theme-style");

        var page = await Page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).getPropertyValue('--blux-primary').trim()");
        Assert.Equal("#ffd23f", page);
        var inside = await Page.GetByTestId("palette-demo").EvaluateAsync<string>("e => getComputedStyle(e).getPropertyValue('--blux-primary').trim()");
        Assert.NotEqual("#ffd23f", inside);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Choosing_a_palette_changes_the_computed_colors(string mode)
    {
        await Open(mode, "theme-style");
        var before = await Background(PrimaryButton);

        await Page.GetByTestId("palette-emerald").ClickAsync();

        await Assertions.Expect(PrimaryButton).Not.ToHaveCSSAsync("background-color", before);
        // Polled: the buttons transition their colors.
        await Assertions.Expect(PrimaryButton).ToHaveCSSAsync("background-color", await PreviewPrimary());
        await Assertions.Expect(Page.GetByTestId("palette-emerald")).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Dark_mode_uses_the_dark_tokens_of_the_palette(string mode)
    {
        await Open(mode, "theme-style");
        var light = await Background(PrimaryButton);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));

        await Assertions.Expect(PrimaryButton).Not.ToHaveCSSAsync("background-color", light);
        var css = await Page.Locator("#palette-demo style").First.TextContentAsync() ?? "";
        var dark = Regex.Match(css, @"\.dark #palette-demo,#palette-demo\.dark\{[^}]*--blux-primary:(#[0-9a-f]{6});");
        Assert.True(dark.Success, css);
        await Assertions.Expect(PrimaryButton).ToHaveCSSAsync("background-color", Rgb(dark.Groups[1].Value));
    }

    [Theory]
    [InlineData("design-system")]
    [InlineData("css-variables")]
    [InlineData("fonts")]
    [InlineData("theming")]
    public async Task Guide_has_no_accessibility_violations_in_both_color_modes(string slug)
    {
        await OpenGuide($"/docs/{slug}");
        await AssertNoAccessibilityViolations();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await Page.WaitForFunctionAsync("() => document.title.length > 0");
        await AssertNoAccessibilityViolations();
    }

    [Fact]
    public async Task A_subtree_override_of_the_primary_color_reaches_the_chart_series_inside_it()
    {
        await Open("ssr", "chart");
        var series = Page.GetByTestId("chart-line").Locator("g[data-series][data-color='chart-1']").First;
        Assert.Equal("#ffd23f", await series.EvaluateAsync<string>("e => getComputedStyle(e).getPropertyValue('--bx-color').trim()"));

        // The chart tokens are not fixed on :root: the series resolve --blux-primary at their own element.
        await Page.GetByTestId("chart-line").EvaluateAsync("e => e.style.setProperty('--blux-primary', '#ff0000')");
        Assert.Equal("#ff0000", await series.EvaluateAsync<string>("e => getComputedStyle(e).getPropertyValue('--bx-color').trim()"));

        // A global --blux-chart-1 still wins over the semantic color.
        await Page.GetByTestId("chart-line").EvaluateAsync("e => e.style.setProperty('--blux-chart-1', '#00ff00')");
        Assert.Equal("#00ff00", await series.EvaluateAsync<string>("e => getComputedStyle(e).getPropertyValue('--bx-color').trim()"));
    }

    [Fact]
    public async Task A_subtree_override_of_the_radius_resizes_the_rounded_utilities_inside_it()
    {
        await Open("ssr", "theme-style");
        var radii = await Page.EvaluateAsync<string[]>("""
            () => {
              const make = (host) => {
                const box = document.createElement('div');
                box.className = 'rounded-sm';
                host.appendChild(box);
                return box;
              };
              const outside = make(document.body);
              const wrapper = document.createElement('div');
              wrapper.style.setProperty('--blux-radius', '2rem');
              document.body.appendChild(wrapper);
              const inside = make(wrapper);
              return [getComputedStyle(outside).borderTopLeftRadius, getComputedStyle(inside).borderTopLeftRadius];
            }
            """);

        Assert.Equal("4px", radii[0]);   // 0.5rem * 0.5
        Assert.Equal("16px", radii[1]);  // 2rem * 0.5
    }

    [Fact]
    public async Task The_tailwind_utilities_follow_the_runtime_tokens_of_the_site()
    {
        await Page.GotoAsync("/docs/css-variables");
        var demo = Page.GetByTestId("utility-demo");

        await Assertions.Expect(demo.GetByText("text-muted", new() { Exact = true })).ToHaveCSSAsync("color", Rgb("#5b6165"));
        await Assertions.Expect(demo.GetByText("bg-muted").First).ToHaveCSSAsync("background-color", Rgb("#eeeeea"));
        await Assertions.Expect(demo.GetByText("bg-primary")).ToHaveCSSAsync("background-color", Rgb("#ffd23f"));

        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await Assertions.Expect(demo.GetByText("text-muted", new() { Exact = true })).ToHaveCSSAsync("color", Rgb("#8a9196"));
        await Assertions.Expect(demo.GetByText("bg-muted").First).ToHaveCSSAsync("background-color", Rgb("#111315"));
    }
}
