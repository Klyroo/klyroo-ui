using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>Blux.UI.Tools on the playground: the placeholder component proves the package, its icons, its texts and its second JS initializer.</summary>
public class ToolsTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private ILocator Empty => Page.GetByTestId("empty-default");

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_empty_state_renders_its_default_texts_and_the_icon_of_the_package(string mode)
    {
        await Open(mode, "data-list-empty");

        await Assertions.Expect(Empty.Locator("[data-slot=title]")).ToHaveTextAsync("No results");
        await Assertions.Expect(Empty.Locator("[data-slot=description]")).ToHaveTextAsync("Try changing your search or your filters.");
        var icon = Empty.Locator("svg");
        await Assertions.Expect(icon).ToHaveCountAsync(1);
        await Assertions.Expect(icon.Locator("path").First).ToBeAttachedAsync();
        await Assertions.Expect(icon).Not.ToHaveAttributeAsync("data-bx-missing-icon", new Regex(".*"));
        await Assertions.Expect(Page.GetByTestId("package-note")).ToContainTextAsync("dotnet add package Blux.UI.Tools --prerelease");
        await Assertions.Expect(Page.GetByTestId("empty-actions").GetByRole(AriaRole.Button, new() { Name = "Clear filters" })).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_second_javascript_initializer_loads_next_to_the_one_of_Blux_UI(string mode)
    {
        await Open(mode, "data-list-empty");

        // Both initializers merge into the same global, whichever runs first.
        await Page.WaitForFunctionAsync("() => !!globalThis.Blux?.tools && typeof globalThis.Blux.setColorMode === 'function'");
        var response = await Page.APIRequest.GetAsync("/_content/Blux.UI.Tools/Blux.UI.Tools.lib.module.js");
        Assert.True(response.Ok, $"the initializer is not served: {response.Status}");
    }

    [Fact]
    public async Task The_second_initializer_also_runs_when_the_page_looks_like_a_web_view()
    {
        // The message bridge blazor.webview.js uses, as in HybridTests: the initializer exports of a web view (beforeStart, afterStarted) run.
        var context = await NewContext(new());
        await context.AddInitScriptAsync("Object.defineProperty(window, 'external', { value: { sendMessage() {}, receiveMessage() {} }, configurable: true });");
        var page = await context.NewPageAsync();
        await Open(page, "server", "data-list-empty");

        await page.WaitForFunctionAsync("() => !!globalThis.Blux?.tools && typeof globalThis.Blux.setColorMode === 'function'");
        await Assertions.Expect(page.GetByTestId("empty-default").Locator("svg path").First).ToBeAttachedAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_texts_follow_the_language_of_the_user(string mode)
    {
        var page = await NewPage("fr", "fr");
        await Open(page, mode, "data-list-empty");

        await Assertions.Expect(page.GetByTestId("empty-default").Locator("[data-slot=title]")).ToHaveTextAsync("Aucun résultat");
        await Assertions.Expect(page.GetByTestId("empty-default").Locator("[data-slot=description]")).ToHaveTextAsync("Essayez de modifier votre recherche ou vos filtres.");
    }

    [Fact]
    public async Task The_stylesheets_of_the_package_are_served()
    {
        await Open("ssr", "data-list-empty");

        foreach (var asset in new[] { "blux-tools.css", "blux-tools.theme.css" })
        {
            var response = await Page.APIRequest.GetAsync($"/_content/Blux.UI.Tools/{asset}");
            Assert.True(response.Ok, $"{asset}: {response.Status}");
        }
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_empty_state_has_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await Open(mode, "data-list-empty");
        await Assertions.Expect(Empty.Locator("[data-slot=title]")).ToBeVisibleAsync();

        await AssertNoAccessibilityViolations();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        // A color transition still running would be measured half way by the contrast rule.
        await Page.WaitForFunctionAsync("() => document.getAnimations().length === 0");

        await AssertNoAccessibilityViolations();
    }
}
