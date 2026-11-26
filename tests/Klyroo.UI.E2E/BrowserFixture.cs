[assembly: AssemblyFixture(typeof(Klyroo.UI.E2E.BrowserFixture))]

namespace Klyroo.UI.E2E;

/// <summary>
/// One browser for the whole test run. Which one is decided by environment variables, so that the same tests run on a laptop and in CI:
/// <list type="bullet">
/// <item><c>KLYROO_BROWSER</c>: <c>chromium</c> (default), <c>firefox</c> or <c>webkit</c>.</item>
/// <item><c>KLYROO_BROWSER_CHANNEL</c>: a browser installed on the machine (<c>chrome</c>, <c>msedge</c>) instead of the one Playwright downloads.</item>
/// <item><c>KLYROO_BROWSER_EXECUTABLE</c>: the path of a Chromium-based browser (<c>/usr/bin/google-chrome</c>). Wins over the channel.</item>
/// </list>
/// With none of them set, the browser must have been installed for Playwright: <c>pwsh bin/Release/net10.0/playwright.ps1 install --with-deps chromium</c>.
/// </summary>
public sealed class BrowserFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;

    public IBrowser Browser { get; private set; } = default!;

    public async ValueTask InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        var browserType = (Environment.GetEnvironmentVariable("KLYROO_BROWSER") ?? "chromium") switch
        {
            "firefox" => _playwright.Firefox,
            "webkit" => _playwright.Webkit,
            _ => _playwright.Chromium,
        };
        var options = new BrowserTypeLaunchOptions();
        if (Environment.GetEnvironmentVariable("KLYROO_BROWSER_EXECUTABLE") is { Length: > 0 } executable) options.ExecutablePath = executable;
        else if (Environment.GetEnvironmentVariable("KLYROO_BROWSER_CHANNEL") is { Length: > 0 } channel) options.Channel = channel;
        try
        {
            Browser = await browserType.LaunchAsync(options);
        }
        catch (PlaywrightException e) when (e.Message.Contains("Executable doesn't exist", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "No browser for Playwright. Install one: pwsh bin/<Configuration>/net10.0/playwright.ps1 install --with-deps chromium, "
                + "or point the tests at a browser of the machine with KLYROO_BROWSER_EXECUTABLE=/usr/bin/google-chrome (or KLYROO_BROWSER_CHANNEL=chrome).", e);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Browser is not null) await Browser.DisposeAsync();
        _playwright?.Dispose();
        TemplateApp.CleanUp();
    }
}
