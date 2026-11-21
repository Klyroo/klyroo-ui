namespace Blux.UI.E2E;

/// <summary>A second playground, served below the path base <c>/sub</c> (<c>PathBase=/sub</c>: UsePathBase and a matching base href), started once for the class.</summary>
public sealed class PathBaseFixture(PlaygroundServer server) : IAsyncLifetime
{
    public const string PathBase = "/sub";

    public PlaygroundServer Server { get; } = server;
    public PlaygroundInstance Instance { get; private set; } = default!;

    public async ValueTask InitializeAsync() => Instance = await Server.StartAtPathBaseAsync(PathBase);

    public ValueTask DisposeAsync() => Instance is null ? ValueTask.CompletedTask : Instance.DisposeAsync();
}

/// <summary>
/// Apps built with Blux.UI work below a path base, next to other apps of the same origin: everything the pages ask for stays under
/// <c>/sub/</c>, the cookies the library writes are scoped to it, and the images load.
/// </summary>
public class PathBaseTests(PathBaseFixture fixture) : IClassFixture<PathBaseFixture>, IAsyncLifetime
{
    private IBrowserContext _context = default!;
    private readonly List<string> _requests = [];
    private readonly List<string> _sockets = [];
    private readonly List<string> _failures = [];

    private IPage Page { get; set; } = default!;
    private string Host => fixture.Instance.Host;

    public async ValueTask InitializeAsync()
    {
        _context = await fixture.Server.Browser.NewContextAsync(new() { BaseURL = fixture.Instance.BaseUrl });
        _context.Request += (_, request) => { lock (_requests) _requests.Add(request.Url); };
        _context.Response += (_, response) =>
        {
            if (response.Status >= 400 && !IsSdkTooling(response.Url)) lock (_failures) _failures.Add($"{response.Status} {response.Url}");
        };
        Page = await _context.NewPageAsync();
        Page.WebSocket += (_, socket) => { lock (_sockets) _sockets.Add(socket.Url); };
    }

    public ValueTask DisposeAsync() => _context.DisposeAsync();

    private async Task Open(string mode, string component)
    {
        await Page.GotoAsync($"{mode}/components/{component}");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        if (mode != "ssr")
        {
            await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
    }

    [Fact]
    public async Task Skip_link_keeps_the_address_and_focuses_main_below_the_path_base()
    {
        await Open("ssr", "button");

        // "#main" resolves against <base href="/sub/">: without the handler of the Blux JS it would load the home page of the app.
        var skip = Page.GetByRole(AriaRole.Link, new() { Name = "Skip to content" });
        await skip.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Assertions.Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/sub/ssr/components/button$"));
        await Assertions.Expect(Page.Locator("main#main")).ToBeFocusedAsync();
    }

    /// <summary>Injected by the SDK in Development (browser refresh and hot reload tooling, from the <c>dotnet run</c> environment), not by the app or the library.</summary>
    private static bool IsSdkTooling(string pathOrUrl) => pathOrUrl.Contains("/_framework/dotnet-browser-tools/", StringComparison.Ordinal);

    /// <summary>Every request of the page to its own origin, as a path with its query.</summary>
    private List<string> SameOriginPaths()
    {
        lock (_requests)
        {
            return _requests
                .Where(url => url.StartsWith(Host + "/", StringComparison.Ordinal))
                .Select(url => url[Host.Length..])
                .Where(path => !IsSdkTooling(path))
                .ToList();
        }
    }

    private void AssertEverythingStaysBelowThePathBase()
    {
        var paths = SameOriginPaths();
        Assert.NotEmpty(paths);
        var outside = paths.Where(p => p != "/sub" && !p.StartsWith("/sub/", StringComparison.Ordinal)).ToList();
        Assert.True(outside.Count == 0, "Requests outside the path base:\n" + string.Join("\n", outside));
        // Not a request of the page, but the circuit of Blazor Server connects through one too.
        List<string> sockets;
        lock (_sockets) sockets = [.. _sockets];
        Assert.All(sockets, url => Assert.StartsWith("ws://127.0.0.1:", url));
        Assert.All(sockets, url => Assert.Contains("/sub/", new Uri(url).AbsolutePath + "/"));
        List<string> failures;
        lock (_failures) failures = [.. _failures];
        Assert.True(failures.Count == 0, "Failed responses:\n" + string.Join("\n", failures));
    }

    private async Task<List<Cookie>> CookiesNamed(string prefix)
    {
        List<Cookie> found = [];
        for (var deadline = DateTime.UtcNow.AddSeconds(5); DateTime.UtcNow < deadline; await Task.Delay(50))
        {
            found = (await _context.CookiesAsync()).Where(c => c.Name.StartsWith(prefix, StringComparison.Ordinal)).Select(c => new Cookie(c.Name, c.Path)).ToList();
            if (found.Count > 0) return found;
        }
        return found;
    }

    private sealed record Cookie(string Name, string Path);

    [Theory]
    [InlineData("ssr")]
    [InlineData("server")]
    [InlineData("wasm")]
    [InlineData("auto")]
    public async Task A_page_in_every_render_mode_asks_only_for_urls_below_the_path_base(string mode)
    {
        await Open(mode, "button");

        // The page is the one of the app, not another app's or the root's.
        await Assertions.Expect(Page).ToHaveURLAsync($"{Host}/sub/{mode}/components/button");
        Assert.Equal($"{Host}/sub/", await Page.EvaluateAsync<string>("() => document.baseURI"));
        AssertEverythingStaysBelowThePathBase();
    }

    [Fact]
    public async Task A_WebAssembly_page_loads_its_runtime_and_stays_interactive_below_the_path_base()
    {
        await Open("wasm", "dashboard-group");

        var paths = SameOriginPaths();
        Assert.Contains(paths, p => p.StartsWith("/sub/_framework/", StringComparison.Ordinal) && p.Contains(".wasm", StringComparison.Ordinal));
        Assert.Contains(paths, p => p.StartsWith("/sub/_content/Blux.UI/", StringComparison.Ordinal) || p.StartsWith("/sub/_framework/blazor.web", StringComparison.Ordinal));
        AssertEverythingStaysBelowThePathBase();
    }

    [Theory]
    [InlineData("ssr")]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task The_dashboard_sidebar_cookie_is_scoped_to_the_path_base_and_survives_a_reload(string mode)
    {
        await Open(mode, "dashboard-group");
        var sidebar = Page.GetByTestId("dashboard-sidebar");
        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "expanded");

        await Page.GetByTestId("dashboard-collapse").ClickAsync();

        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "collapsed");
        var cookies = await CookiesNamed("blux-sidebar");
        Assert.NotEmpty(cookies);
        // Scoped by path and by name: a server that reads it keeps the last cookie of a name, and the one of a root app would come after.
        Assert.All(cookies, c => Assert.Equal("/sub", c.Path));
        Assert.All(cookies, c => Assert.StartsWith("blux-sidebar.sub~~playground-dashboard", c.Name));
        Assert.Contains(cookies, c => c.Name == "blux-sidebar.sub~~playground-dashboard");

        await Page.ReloadAsync();
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        await Assertions.Expect(Page.GetByTestId("dashboard-sidebar")).ToHaveAttributeAsync("data-state", "collapsed");
        AssertEverythingStaysBelowThePathBase();
    }

    [Theory]
    [InlineData("ssr")]
    [InlineData("wasm")]
    public async Task The_sidebar_cookie_of_an_app_at_the_root_is_neither_read_nor_overwritten(string mode)
    {
        // An app at the root of the origin with the same sidebar id: its cookie is at "/" under the plain name, collapsed.
        await _context.AddCookiesAsync([new Microsoft.Playwright.Cookie { Name = "blux-sidebar-playground-dashboard", Value = "1", Domain = "127.0.0.1", Path = "/" }]);
        await Open(mode, "dashboard-group");
        var sidebar = Page.GetByTestId("dashboard-sidebar");
        // The browser sends that cookie to /sub too, but this app reads its own name only.
        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "expanded");

        await Page.GetByTestId("dashboard-collapse").ClickAsync();
        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "collapsed");
        await ExpectCookieValue("blux-sidebar.sub~~playground-dashboard", "/sub", "1");
        await Page.GetByTestId("dashboard-collapse").ClickAsync();
        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "expanded");
        await ExpectCookieValue("blux-sidebar.sub~~playground-dashboard", "/sub", "0");

        var root = (await _context.CookiesAsync()).Single(c => c.Name == "blux-sidebar-playground-dashboard");
        Assert.Equal("/", root.Path);
        Assert.Equal("1", root.Value);
    }

    private async Task ExpectCookieValue(string name, string path, string expected)
    {
        string? actual = null;
        for (var deadline = DateTime.UtcNow.AddSeconds(5); DateTime.UtcNow < deadline; await Task.Delay(50))
        {
            actual = (await _context.CookiesAsync()).FirstOrDefault(c => c.Name == name && c.Path == path)?.Value;
            if (actual == expected) return;
        }
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task The_collapsed_state_survives_a_reload_even_when_a_root_cookie_says_otherwise()
    {
        await Open("ssr", "dashboard-group");
        await Page.GetByTestId("dashboard-collapse").ClickAsync();
        await ExpectCookieValue("blux-sidebar.sub~~playground-dashboard", "/sub", "1");
        await _context.AddCookiesAsync([new Microsoft.Playwright.Cookie { Name = "blux-sidebar-playground-dashboard", Value = "0", Domain = "127.0.0.1", Path = "/" }]);

        await Page.ReloadAsync();
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");

        await Assertions.Expect(Page.GetByTestId("dashboard-sidebar")).ToHaveAttributeAsync("data-state", "collapsed");
    }

    [Fact]
    public async Task Banner_dismissals_are_prefixed_with_the_path_base_but_the_color_mode_stays_shared()
    {
        await Open("wasm", "banner");
        await Page.GetByTestId("banner").GetByRole(AriaRole.Button, new() { Name = "Close" }).ClickAsync();
        await Page.EvaluateAsync("() => Blux.setColorMode('dark')");

        var keys = await Page.EvaluateAsync<string[]>("() => Object.keys(localStorage)");
        Assert.Contains("/sub:blux:banner:playground-launch", keys);
        Assert.DoesNotContain("blux:banner:playground-launch", keys);
        // One color mode for every app of the origin.
        Assert.Contains("blux-color-mode", keys);
        Assert.DoesNotContain("/sub:blux-color-mode", keys);
        await Page.ReloadAsync();
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-banner')");
        await Assertions.Expect(Page.GetByTestId("banner")).ToBeHiddenAsync();
    }

    [Theory]
    [InlineData("ssr")]
    [InlineData("server")]
    [InlineData("wasm")]
    [InlineData("auto")]
    public async Task The_culture_cookie_stays_one_for_the_origin_at_the_root_path_and_the_server_reads_it(string mode)
    {
        await Open(mode, "locale-select");
        await Page.EvaluateAsync("() => { window.__beforeReload = true; }");

        await Page.GetByTestId("locale-select").GetByRole(AriaRole.Combobox, new() { Name = "Language" }).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Français" }).ClickAsync();

        await Page.WaitForFunctionAsync("() => window.__beforeReload === undefined");
        var cookie = (await _context.CookiesAsync()).Single(c => c.Name == ".AspNetCore.Culture");
        Assert.Equal("c=fr|uic=fr", Uri.UnescapeDataString(cookie.Value));
        // One culture for the whole origin: a language chosen here applies to the app at the root and the other way round.
        Assert.Equal("/", cookie.Path);
        if (mode != "ssr")
        {
            await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
        await Assertions.Expect(Page.GetByTestId("locale-resolved")).ToHaveTextAsync("fr");
        AssertEverythingStaysBelowThePathBase();
    }

    [Fact]
    public async Task A_culture_chosen_at_the_root_applies_below_the_path_base()
    {
        // What a language chosen in the docs at "/" does to the demos below /sub: the cookie is at "/" and the server reads it.
        await _context.AddCookiesAsync([new Microsoft.Playwright.Cookie { Name = ".AspNetCore.Culture", Value = Uri.EscapeDataString("c=fr|uic=fr"), Domain = "127.0.0.1", Path = "/" }]);

        await Open("ssr", "locale-select");

        await Assertions.Expect(Page.GetByTestId("locale-resolved")).ToHaveTextAsync("fr");
    }

    [Theory]
    [InlineData("ssr")]
    [InlineData("server")]
    [InlineData("wasm")]
    [InlineData("auto")]
    public async Task Images_come_from_the_optimizer_of_this_app_and_load(string mode)
    {
        await Open(mode, "image");
        foreach (var id in new[] { "image-basic", "image-cover", "image-contain", "image-responsive", "image-placeholder", "image-eager" })
        {
            await Page.GetByTestId(id).ScrollIntoViewIfNeededAsync();
            await Page.WaitForFunctionAsync("id => { const i = document.querySelector(`[data-testid=${id}]`); return i && i.complete && i.naturalWidth > 0; }", id, new() { Timeout = 30_000 });
        }

        var source = await Page.GetByTestId("image-basic").EvaluateAsync<string>("i => i.currentSrc");
        Assert.StartsWith($"{Host}/sub/_blux/img?src=%2Fimg%2Fsample-gradient.png", source);
        Assert.Contains(SameOriginPaths(), p => p.StartsWith("/sub/_blux/img?", StringComparison.Ordinal));
        Assert.Equal(200, (await _context.APIRequest.GetAsync("_blux/img?src=/img/sample-waves.png&w=64&f=webp")).Status);
        AssertEverythingStaysBelowThePathBase();
    }

    [Fact]
    public async Task The_home_page_and_its_unoptimized_images_stay_below_the_path_base()
    {
        await Page.GotoAsync("");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        await Page.EvaluateAsync("() => document.querySelectorAll('img[loading=lazy]').forEach(i => i.loading = 'eager')");
        await Page.WaitForFunctionAsync("() => [...document.images].every(i => i.complete && i.naturalWidth > 0)", null, new() { Timeout = 30_000 });

        Assert.Contains(SameOriginPaths(), p => p.StartsWith("/sub/img/platform-", StringComparison.Ordinal));
        AssertEverythingStaysBelowThePathBase();
    }
}
