using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>
/// The /templates gallery of the docs site and the live demos behind it: <c>/templates/{name}/demo/</c> is another app (a child process of the docs
/// host) reached through a reverse proxy, WebSockets included. The docs host starts the demos from the build output of each template, and the fixture
/// waits until they answer.
/// </summary>
public class TemplatesTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private const string DemoPath = "/templates/starter/demo/";

    private static bool IsHotReloadTooling(string url) => url.Contains("/_framework/dotnet-browser-tools/", StringComparison.Ordinal);

    /// <summary>Skips when the docs run published with no templates to run (a local run); in CI that is a failure, a test that cannot run proves nothing.</summary>
    private void RequireDemos()
    {
        if (TemplatesReady) return;
        Assert.False(Environment.GetEnvironmentVariable("CI") is { Length: > 0 }, "The template demos are not running (BLUX_PLAYGROUND_PUBLISHED is set without BLUX_TEMPLATES_PUBLISHED).");
        Assert.SkipWhen(true, "The docs run published with no BLUX_TEMPLATES_PUBLISHED folder: there is no template demo to open.");
    }

    private async Task OpenGallery(ColorScheme scheme = ColorScheme.Light)
    {
        await Page.EmulateMediaAsync(new() { ColorScheme = scheme });
        await Page.GotoAsync("templates");
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("working app");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
    }

    [Theory]
    [InlineData(ColorScheme.Light)]
    [InlineData(ColorScheme.Dark)]
    public async Task The_gallery_has_no_accessibility_violations(ColorScheme scheme)
    {
        await OpenGallery(scheme);
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(scheme == ColorScheme.Dark ? new Regex("dark") : new Regex("^((?!dark).)*$"));

        await AssertNoAccessibilityViolations();
    }

    [Fact]
    public async Task The_gallery_lists_the_ten_templates_and_only_the_starter_is_live()
    {
        await OpenGallery();

        var cards = Page.GetByTestId("template-card");
        await Assertions.Expect(cards).ToHaveCountAsync(10);
        var starter = cards.Filter(new() { Has = Page.GetByRole(AriaRole.Heading, new() { Name = "Starter" }) });
        await Assertions.Expect(starter.GetByText("Live demo")).ToBeVisibleAsync();
        await Assertions.Expect(starter.GetByRole(AriaRole.List, new() { Name = "Starter features" }).GetByRole(AriaRole.Listitem)).ToHaveCountAsync(3);
        var preview = starter.GetByTestId("template-preview");
        await Assertions.Expect(preview).ToHaveAttributeAsync("href", "templates/starter/demo/");
        // No fake link: the repositories are not published, so there is no Source link and the card says so.
        await Assertions.Expect(starter.GetByTestId("template-source")).ToHaveCountAsync(0);
        await Assertions.Expect(starter.GetByTestId("template-source-missing")).ToHaveTextAsync("Repository not published yet");

        // The light and the dark screenshot are both images of the site that load.
        foreach (var mode in new[] { "light", "dark" })
        {
            var image = starter.Locator($"img[src$='img/template-starter-{mode}.png']");
            await Assertions.Expect(image).ToHaveCountAsync(1);
            var response = await Page.APIRequest.GetAsync(new Uri(new Uri(BaseUrl + "/"), await image.GetAttributeAsync("src")).ToString());
            Assert.True(response.Ok, $"the {mode} screenshot answers {response.Status}");
        }

        // The nine others are planned: a badge, no link at all (a broken Preview or Source would be worse than none), no screenshot.
        var planned = cards.Filter(new() { HasNot = Page.GetByTestId("template-preview") });
        await Assertions.Expect(planned).ToHaveCountAsync(9);
        await Assertions.Expect(planned.GetByText("Coming soon", new() { Exact = true }).Locator("visible=true")).ToHaveCountAsync(9);
        await Assertions.Expect(planned.Locator("a")).ToHaveCountAsync(0);
        await Assertions.Expect(planned.Locator("img")).ToHaveCountAsync(0);
        await Assertions.Expect(planned.GetByTestId("template-planned")).ToHaveCountAsync(9);
    }

    [Fact]
    public async Task The_header_links_to_the_gallery()
    {
        await Page.GotoAsync("/docs");
        await Page.GetByRole(AriaRole.Navigation, new() { Name = "Main" }).GetByRole(AriaRole.Link, new() { Name = "Templates" }).ClickAsync();

        await Page.WaitForURLAsync(url => url.EndsWith("/templates"));
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("working app");
    }

    [Fact]
    public async Task Preview_opens_the_live_demo_through_the_proxy_and_it_becomes_interactive()
    {
        RequireDemos();
        var requests = new List<string>();
        var failures = new List<string>();
        var sockets = new List<string>();
        var recording = false;
        Page.Request += (_, request) => { if (recording) lock (requests) requests.Add(request.Url); };
        Page.Response += (_, response) => { if (recording && response.Status >= 400 && !IsHotReloadTooling(response.Url)) lock (failures) failures.Add($"{response.Status} {response.Url}"); };
        Page.WebSocket += (_, socket) => { lock (sockets) sockets.Add(socket.Url); };
        await OpenGallery();
        // Let the gallery settle (fonts, deferred scripts) so only what the click causes is recorded.
        await Page.EvaluateAsync("() => document.fonts.ready");
        await Task.Delay(500, TestContext.Current.CancellationToken);

        recording = true;
        await Page.GetByTestId("template-preview").ClickAsync();
        await Page.WaitForURLAsync(url => url.EndsWith(DemoPath));

        // The demo is the Starter, not a docs page: its own layout and title.
        await Assertions.Expect(Page).ToHaveTitleAsync("Starter");
        await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveTextAsync(new Regex("Running on (Server|WebAssembly)"), new() { Timeout = 30_000 });
        await Page.GetByTestId("counter-button").ClickAsync();
        await Assertions.Expect(Page.GetByTestId("counter")).ToHaveTextAsync("Clicked 1 times", new() { Timeout = 30_000 });
        await Page.GetByTestId("counter-button").ClickAsync();
        await Assertions.Expect(Page.GetByTestId("counter")).ToHaveTextAsync("Clicked 2 times", new() { Timeout = 30_000 });

        // Every request of the demo page, the framework and the circuit included, is below the path base of the demo.
        var origin = new Uri(BaseUrl).GetLeftPart(UriPartial.Authority);
        lock (requests)
        {
            // The hot reload client of the SDK (Development only) fetches its settings from the root of the origin, whatever the path base: not an
            // asset of the page, and gone in Production.
            var sameOrigin = requests
                .Where(url => url.StartsWith(origin + "/", StringComparison.Ordinal) && !IsHotReloadTooling(url))
                .ToList();
            Assert.NotEmpty(sameOrigin);
            var outside = sameOrigin.Where(url => !url.StartsWith(origin + DemoPath, StringComparison.Ordinal)).ToList();
            Assert.True(outside.Count == 0, "Requests outside " + DemoPath + ":\n" + string.Join("\n", outside));
        }
        lock (failures) Assert.True(failures.Count == 0, "Failed requests:\n" + string.Join("\n", failures));
        // The Blazor Server circuit of the first visit is a WebSocket through the proxy (a later visit may run in WebAssembly instead).
        if (await Page.GetByTestId("renderer").InnerTextAsync() == "Running on Server")
        {
            lock (sockets)
            {
                Assert.NotEmpty(sockets);
                Assert.All(sockets, url => Assert.StartsWith("ws://" + new Uri(origin).Authority + DemoPath, url));
            }
        }
    }

    [Fact]
    public async Task The_demo_answers_with_or_without_the_trailing_slash()
    {
        RequireDemos();
        var response = await Page.GotoAsync("templates/starter/demo");

        Assert.NotNull(response);
        Assert.Equal(200, response.Status);
        await Assertions.Expect(Page).ToHaveTitleAsync("Starter");
        await Assertions.Expect(Page.Locator("base")).ToHaveAttributeAsync("href", DemoPath);
    }

    [Fact]
    public async Task A_404_inside_the_demo_is_the_page_of_the_demo_not_the_one_of_the_docs()
    {
        RequireDemos();
        var response = await Page.GotoAsync("templates/starter/demo/no-such-page");

        Assert.NotNull(response);
        Assert.Equal(404, response.Status);
        // The Starter's own not-found page (its title has no site suffix, its text is its own), under its own path base.
        await Assertions.Expect(Page).ToHaveTitleAsync("Not found");
        await Assertions.Expect(Page.GetByText("There is nothing at this address.")).ToBeVisibleAsync();
        await Assertions.Expect(Page.Locator("base")).ToHaveAttributeAsync("href", DemoPath);

        // The docs keep theirs.
        var docs = await Page.GotoAsync("no-such-page");
        Assert.NotNull(docs);
        Assert.Equal(404, docs.Status);
        await Assertions.Expect(Page).ToHaveTitleAsync("Not found · blux-ui");
    }

    [Fact]
    public async Task The_language_chosen_on_the_docs_applies_to_the_demo()
    {
        RequireDemos();
        await Open("ssr", "locale-select");
        await Page.EvaluateAsync("() => { window.__beforeReload = true; }");
        await Page.GetByTestId("locale-select").GetByRole(AriaRole.Combobox, new() { Name = "Language" }).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Français" }).ClickAsync();
        await Page.WaitForFunctionAsync("() => window.__beforeReload === undefined");
        await Assertions.Expect(Page.Locator("html")).ToHaveAttributeAsync("lang", "fr");

        // One culture cookie for the origin, at the root: the proxy hands it to the demo, which reads it.
        var cookie = (await Page.Context.CookiesAsync()).Single(c => c.Name == ".AspNetCore.Culture");
        Assert.Equal("/", cookie.Path);
        await Page.GotoAsync("templates/starter/demo/");
        await Assertions.Expect(Page.Locator("html")).ToHaveAttributeAsync("lang", "fr");
        await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveTextAsync(new Regex("Running on (Server|WebAssembly)"), new() { Timeout = 30_000 });
        // The gallery of the docs and the demo agree.
        await Page.GotoAsync("templates");
        await Assertions.Expect(Page.Locator("html")).ToHaveAttributeAsync("lang", "fr");
    }

    [Fact]
    public async Task The_host_reports_every_demo_as_ready()
    {
        RequireDemos();
        var response = await Page.APIRequest.GetAsync(BaseUrl + "/_blux/templates/ready");

        Assert.True(response.Ok);
        var body = await response.TextAsync();
        Assert.Contains("\"name\":\"starter\"", body);
        Assert.Contains("\"state\":\"ready\"", body);
    }
}
