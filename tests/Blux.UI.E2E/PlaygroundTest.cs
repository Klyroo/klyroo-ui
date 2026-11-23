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
        "modal", "popover", "tooltip", "dropdown-menu", "toast", "tabs", "accordion", "table", "data-list", "data-list-filters", "data-list-filter-panel", "data-list-facets", "data-list-selection", "data-list-empty",
        "infinite-scroll", "select-menu", "input-menu", "listbox", "command-palette",
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
        await ThrottleCpu(Page);
        Trace(Page);
    }

    private readonly List<string> _trace = [];
    private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();

    /// <summary>
    /// BLUX_E2E_TRACE=folder keeps, for each failed test, what its page did (navigations, console, errors, failed requests, circuit frames)
    /// in a file of that folder: the first thing to read when a timeout shows up once in a full run.
    /// </summary>
    private void Trace(IPage page)
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BLUX_E2E_TRACE"))) return;
        void Add(string what) { lock (_trace) _trace.Add($"{_clock.ElapsedMilliseconds,7} {what}"); }
        page.FrameNavigated += (_, f) => { if (f == page.MainFrame) Add("navigated " + f.Url); };
        page.Console += (_, m) => Add($"console.{m.Type} {m.Text}");
        page.PageError += (_, e) => Add("pageerror " + e);
        page.RequestFailed += (_, r) => Add($"requestfailed {r.Url} {r.Failure}");
        page.Response += (_, r) => { if (r.Status >= 400) Add($"response {r.Status} {r.Url}"); };
        page.Request += (_, r) => { if (r.Url.Contains("_framework/dotnet.native") || r.Url.EndsWith(".wasm") || r.Url.Contains("_blazor/negotiate") || r.Url.Contains("blazor.web.js")) Add($"request {r.Url}"); };
        page.WebSocket += (_, ws) =>
        {
            Add("websocket opened " + ws.Url);
            ws.FrameSent += (_, f) => Add($"ws> {Shorten(f.Text)}");
            ws.FrameReceived += (_, f) => Add($"ws< {Shorten(f.Text)}");
            ws.Close += (_, _) => Add("websocket closed");
        };
    }

    private static string Shorten(string? text) => text is null ? "(binary)" : text.Length > 160 ? text[..160] : text;

    private void SaveTrace()
    {
        var folder = Environment.GetEnvironmentVariable("BLUX_E2E_TRACE");
        if (string.IsNullOrEmpty(folder) || TestContext.Current.TestState?.Result != TestResult.Failed) return;
        Directory.CreateDirectory(folder);
        var name = string.Concat(TestContext.Current.Test?.TestDisplayName.Select(c => char.IsLetterOrDigit(c) ? c : '_') ?? "test");
        lock (_trace) File.WriteAllLines(Path.Combine(folder, name + ".txt"), _trace);
    }

    /// <summary>
    /// BLUX_E2E_CPU_THROTTLE=4 slows the page's CPU (Chromium only) by that factor, the way a loaded CI machine does: a timing
    /// assumption of a test shows up deterministically instead of once in a full run.
    /// </summary>
    protected static async Task ThrottleCpu(IPage page)
    {
        if (!double.TryParse(Environment.GetEnvironmentVariable("BLUX_E2E_CPU_THROTTLE"), System.Globalization.CultureInfo.InvariantCulture, out var rate) || rate <= 1) return;
        var session = await page.Context.NewCDPSessionAsync(page);
        await session.SendAsync("Emulation.setCPUThrottlingRate", new Dictionary<string, object> { ["rate"] = rate });
    }

    public async ValueTask DisposeAsync()
    {
        SaveTrace();
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

    /// <summary>
    /// Holds back the render batches the server sends over the circuit of the page by <paramref name="milliseconds"/> (in order), and lets the other messages through at once.
    /// This is what a loaded server does for real: a batch is sent when the browser acknowledged the previous one, while a JavaScript call a component makes right after
    /// rendering goes out immediately, so the call can reach the browser before the DOM it was meant to follow. Call it before the page opens a circuit.
    /// Returns what waits until every batch held back so far was delivered and the browser has run its next frame, so a test can look at the page right after a late render.
    /// </summary>
    protected static async Task<Func<Task>> DelayRenderBatches(IPage page, int milliseconds)
    {
        var marker = System.Text.Encoding.ASCII.GetBytes("JS.RenderBatch");
        // The focus the router's FocusOnNavigate gives the h1 after the first interactive render is a JavaScript call like the others: held in the same order.
        var focus = System.Text.Encoding.ASCII.GetBytes("focusBySelector");
        Task tail = Task.CompletedTask;
        await page.RouteWebSocketAsync(new System.Text.RegularExpressions.Regex("/_blazor"), route =>
        {
            var server = route.ConnectToServer();
            server.OnMessage(frame =>
            {
                var binary = frame.Binary;
                if (binary is not null && (binary.AsSpan().IndexOf(marker) >= 0 || binary.AsSpan().IndexOf(focus) >= 0))
                {
                    lock (marker)
                    {
                        tail = tail.ContinueWith(async _ =>
                        {
                            await Task.Delay(milliseconds);
                            route.Send(binary);
                        }, TaskScheduler.Default).Unwrap();
                    }
                }
                else if (binary is not null) route.Send(binary);
                else route.Send(frame.Text!);
            });
        });
        return async () =>
        {
            Task pending;
            lock (marker) pending = tail;
            await pending;
            await page.EvaluateAsync("() => new Promise((resolve) => requestAnimationFrame(() => setTimeout(resolve, 0)))");
        };
    }

    /// <summary>
    /// Waits until the address of the page satisfies a JavaScript expression of <c>url</c> (a <c>URL</c>), checked in the page, polled until it
    /// holds. <c>Page.WaitForURLAsync</c> is not used for an address that changes without a load (pushState, an enhanced navigation, the
    /// interactive router): it reads the address the client last heard about, then starts listening for the next navigation, and a navigation that
    /// lands between the two is lost, so the wait ends on a timeout although the address is the expected one.
    /// </summary>
    protected Task WaitForUrl(string expression, float timeout = 15_000) => Page.WaitForUrlAsync(expression, timeout);

    /// <summary>Waits until the query of the address, decoded (what a user shares: the comma of a list is written %2C or not), is exactly <paramref name="expected"/>.</summary>
    protected Task WaitForQuery(string expected, float timeout = 15_000) =>
        Page.WaitForFunctionAsync("expected => decodeURIComponent(location.search) === expected", expected, new() { Timeout = timeout });

    /// <summary>Waits until the query of the address, as written, is exactly <paramref name="expected"/>.</summary>
    protected Task WaitForRawQuery(string expected, float timeout = 15_000) =>
        Page.WaitForFunctionAsync("expected => location.search === expected", expected, new() { Timeout = timeout });

    /// <summary>Waits until the decoded query of the address contains <paramref name="part"/>.</summary>
    protected Task WaitForQueryContaining(string part, float timeout = 15_000) =>
        Page.WaitForFunctionAsync("part => decodeURIComponent(location.search).includes(part)", part, new() { Timeout = timeout });

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
            // The router's FocusOnNavigate moves the focus to the h1 with a JavaScript call that follows the first interactive render
            // (the app's own, a few tens of milliseconds later, or much later on a loaded server). A field focused before it loses the
            // focus, and the keys typed next go to the heading. The page is settled once the h1 has the focus.
            await page.WaitForFunctionAsync("() => !document.querySelector('h1') || document.activeElement === document.querySelector('h1')", null, new() { Timeout = 30_000 });
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

/// <summary>
/// Waits for the address of a page the way a test needs it for an enhanced or same-document navigation: polled in the page, never
/// by <c>WaitForURLAsync</c> (see <see cref="PlaygroundTest.WaitForUrl"/>). <c>WaitForURLAsync</c> stays only where a full document load is awaited.
/// </summary>
internal static class PageUrlWaits
{
    /// <summary>Until a JavaScript expression of <c>url</c> (a <c>URL</c>) holds.</summary>
    public static Task WaitForUrlAsync(this IPage page, string expression, float timeout = 15_000) =>
        page.WaitForFunctionAsync($"() => {{ const url = new URL(location.href); return {expression}; }}", null, new() { Timeout = timeout });

    /// <summary>Until the full address matches a JavaScript regular expression (the same text Playwright's regex wait tests).</summary>
    public static Task WaitForUrlMatchingAsync(this IPage page, string pattern, float timeout = 15_000) =>
        page.WaitForFunctionAsync("pattern => new RegExp(pattern).test(location.href)", pattern, new() { Timeout = timeout });

    /// <summary>Until the full address ends with <paramref name="suffix"/>.</summary>
    public static Task WaitForUrlEndingWithAsync(this IPage page, string suffix, float timeout = 15_000) =>
        page.WaitForFunctionAsync("suffix => location.href.endsWith(suffix)", suffix, new() { Timeout = timeout });
}
