using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;

namespace Blux.UI.E2E;

public abstract class PlaygroundTest(PlaygroundServer server) : IAsyncLifetime
{
    public static TheoryData<string> AllModes => new("ssr", "server", "wasm", "auto");
    public static TheoryData<string> InteractiveModes => new("server", "wasm", "auto");

    public static readonly string[] Components =
    [
        "button", "badge", "avatar", "card", "alert", "input", "textarea", "checkbox", "switch", "radio-group", "select", "form-field",
        "modal", "popover", "tooltip", "dropdown-menu", "toast", "tabs", "accordion", "table", "data-list", "data-list-empty",
    ];

    public static TheoryData<string, string> EveryPageInEveryMode()
    {
        var data = new TheoryData<string, string>();
        foreach (var component in Components)
        {
            foreach (var mode in new[] { "ssr", "server", "wasm", "auto" }) data.Add(component, mode);
        }
        return data;
    }

    protected string BaseUrl => server.BaseUrl;

    /// <summary>True when the docs host started its template demos and every one answers.</summary>
    protected bool TemplatesReady => server.TemplatesReady;

    /// <summary>chromium, firefox or webkit.</summary>
    protected string BrowserName => server.Browser.BrowserType.Name;

    protected IPage Page { get; private set; } = default!;
    private IBrowserContext _context = default!;

    public async ValueTask InitializeAsync()
    {
        _context = await server.Browser.NewContextAsync(new() { BaseURL = server.BaseUrl });
        Page = await _context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var extra in _extraContexts) await extra.DisposeAsync();
        await _context.DisposeAsync();
    }

    private readonly List<IBrowserContext> _extraContexts = [];

    /// <summary>A browser context with its own options, disposed with the test.</summary>
    protected async Task<IBrowserContext> NewContext(BrowserNewContextOptions options)
    {
        options.BaseURL ??= server.BaseUrl;
        var context = await server.Browser.NewContextAsync(options);
        _extraContexts.Add(context);
        return context;
    }

    /// <summary>A page of a browser that reports an Apple platform, where the "mod" of a shortcut is the command key.</summary>
    protected async Task<IPage> NewApplePage()
    {
        var context = await server.Browser.NewContextAsync(new() { BaseURL = server.BaseUrl });
        _extraContexts.Add(context);
        await context.AddInitScriptAsync("Object.defineProperty(navigator, 'platform', { get: () => 'MacIntel' });");
        return await context.NewPageAsync();
    }

    /// <summary>
    /// Waits until the browser context holds a cookie with that value. A <c>document.cookie</c> write reaches the
    /// context's cookie store asynchronously (Chromium's network process), so reading it once can see the old value.
    /// </summary>
    protected async Task ExpectCookie(string name, string expected)
    {
        string? actual = null;
        for (var deadline = DateTime.UtcNow.AddSeconds(5); DateTime.UtcNow < deadline; await Task.Delay(50))
        {
            actual = (await Page.Context.CookiesAsync()).FirstOrDefault(c => c.Name == name)?.Value;
            if (actual == expected) return;
        }
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// A page of a browser whose language is <paramref name="browserLocale"/> (what the WebAssembly runtime starts from) and
    /// that sends <paramref name="acceptLanguage"/> to the server, with no culture cookie.
    /// </summary>
    protected async Task<IPage> NewPage(string acceptLanguage, string browserLocale)
    {
        var context = await server.Browser.NewContextAsync(new()
        {
            BaseURL = server.BaseUrl,
            ExtraHTTPHeaders = new Dictionary<string, string> { ["Accept-Language"] = acceptLanguage },
        });
        _extraContexts.Add(context);
        // The language of the browser, which the WebAssembly runtime starts from (a Locale option would replace the header).
        await context.AddInitScriptAsync($"Object.defineProperty(navigator, 'language', {{ get: () => '{browserLocale}' }}); Object.defineProperty(navigator, 'languages', {{ get: () => ['{browserLocale}'] }});");
        return await context.NewPageAsync();
    }

    /// <summary>Opens a page and waits until it can be used in its mode.</summary>
    protected Task Open(string mode, string component) => Open(Page, mode, component);

    protected static async Task Open(IPage page, string mode, string component)
    {
        await page.GotoAsync($"/{mode}/components/{component}");
        // Blux's browser side is loaded by blazor.web.js in every mode, static included.
        await page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        if (mode != "ssr")
        {
            await Assertions.Expect(page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
    }

    /// <summary>
    /// Opens a guide page (/docs/...). Guides run in Auto: the head outlet renders the title again once the page is
    /// interactive, so a scan made before that can see an empty title. Waits for the interactive render and the title.
    /// </summary>
    protected async Task OpenGuide(string path)
    {
        await Page.GotoAsync(path);
        await Assertions.Expect(Page.Locator("main#main")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover') && document.title.length > 0");
    }

    protected async Task AssertNoAccessibilityViolations()
    {
        // The head outlet renders the title again when a page turns interactive (WebAssembly can do it late on a busy
        // machine): a scan in that window reports an empty <title> the page does not really have.
        await Page.WaitForFunctionAsync("() => document.title.length > 0");
        var result = await Page.RunAxe();
        var violations = result.Violations
            .SelectMany(v => v.Nodes.Select(n => $"{v.Id}: {n.Html} -> {string.Join("; ", n.Any.Select(c => c.Message))}"))
            .ToList();
        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }
}
