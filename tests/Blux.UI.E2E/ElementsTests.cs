using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

public class ElementsTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private static readonly string[] Slugs =
    [
        "kbd", "separator", "skeleton", "chip", "link", "icon", "container", "progress", "banner", "empty", "user", "avatar-group", "field-group",
    ];

    public static TheoryData<string, string> EverySlugInEveryMode()
    {
        var data = new TheoryData<string, string>();
        foreach (var slug in Slugs)
        {
            foreach (var mode in new[] { "ssr", "server", "wasm", "auto" }) data.Add(slug, mode);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(EverySlugInEveryMode))]
    public async Task Elements_page_has_no_accessibility_violations(string slug, string mode)
    {
        await Open(mode, slug);

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Banner_dismissal_persists_after_a_reload(string mode)
    {
        await Open(mode, "banner");
        var banner = Page.GetByTestId("banner");
        await Assertions.Expect(banner).ToBeVisibleAsync();

        await banner.GetByRole(AriaRole.Button, new() { Name = "Close" }).ClickAsync();

        await Assertions.Expect(banner).ToBeHiddenAsync();
        await Page.ReloadAsync();
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-banner')");
        await Assertions.Expect(banner).ToBeHiddenAsync();
        Assert.Equal("1", await Page.EvaluateAsync<string?>("() => localStorage.getItem('blux:banner:playground-launch')"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Link_marks_the_current_page_and_its_section_as_active(string mode)
    {
        await Open(mode, "link");

        await Assertions.Expect(Page.GetByTestId("link-current")).ToHaveAttributeAsync("aria-current", "page");
        await Assertions.Expect(Page.GetByTestId("link-section")).ToHaveAttributeAsync("aria-current", "page");
        await Assertions.Expect(Page.GetByTestId("link-section-exact")).Not.ToHaveAttributeAsync("aria-current", "page");
        await Assertions.Expect(Page.GetByTestId("link-other")).Not.ToHaveAttributeAsync("aria-current", "page");
        await Assertions.Expect(Page.GetByTestId("link-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Assertions.Expect(Page.GetByTestId("link-disabled")).Not.ToHaveAttributeAsync("href", new Regex(".*"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Progress_exposes_its_value_to_assistive_technology(string mode)
    {
        await Open(mode, "progress");

        var upload = Page.GetByRole(AriaRole.Progressbar, new() { Name = "Upload" });
        await Assertions.Expect(upload).ToHaveAttributeAsync("aria-valuenow", "45");
        var loading = Page.GetByRole(AriaRole.Progressbar, new() { Name = "Loading" });
        await Assertions.Expect(loading).Not.ToHaveAttributeAsync("aria-valuenow", new Regex(".*"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Field_group_attaches_the_buttons(string mode)
    {
        await Open(mode, "field-group");
        var group = Page.GetByRole(AriaRole.Group, new() { Name = "Text alignment" });
        var left = group.GetByRole(AriaRole.Button, new() { Name = "Left" });
        var right = group.GetByRole(AriaRole.Button, new() { Name = "Right" });

        // The first button keeps its start radius and loses its end radius; the last one is the opposite.
        Assert.NotEqual("0px", await left.EvaluateAsync<string>("e => getComputedStyle(e).borderTopLeftRadius"));
        Assert.Equal("0px", await left.EvaluateAsync<string>("e => getComputedStyle(e).borderTopRightRadius"));
        Assert.Equal("0px", await right.EvaluateAsync<string>("e => getComputedStyle(e).borderTopLeftRadius"));
        Assert.NotEqual("0px", await right.EvaluateAsync<string>("e => getComputedStyle(e).borderTopRightRadius"));
    }
}
