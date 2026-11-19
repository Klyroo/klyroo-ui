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
        "modal", "popover", "tooltip", "dropdown-menu", "toast", "tabs", "accordion", "table",
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

    protected IPage Page { get; private set; } = default!;
    private IBrowserContext _context = default!;

    public async ValueTask InitializeAsync()
    {
        _context = await server.Browser.NewContextAsync(new() { BaseURL = server.BaseUrl });
        Page = await _context.NewPageAsync();
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    /// <summary>Opens a page and waits until it can be used in its mode.</summary>
    protected async Task Open(string mode, string component)
    {
        await Page.GotoAsync($"/{mode}/components/{component}");
        // Blux's browser side is loaded by blazor.web.js in every mode, static included.
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        if (mode != "ssr")
        {
            await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
    }

    protected async Task AssertNoAccessibilityViolations()
    {
        var result = await Page.RunAxe();
        var violations = result.Violations
            .SelectMany(v => v.Nodes.Select(n => $"{v.Id}: {n.Html} -> {string.Join("; ", n.Any.Select(c => c.Message))}"))
            .ToList();
        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }
}
