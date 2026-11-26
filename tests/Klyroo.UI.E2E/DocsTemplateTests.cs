using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Klyroo.UI.E2E;

/// <summary>
/// The Docs template as a user ships it: <c>dotnet publish -c Release</c>, run in Production at the root and below a path base. Started by
/// <c>build/verify-templates.mjs</c> with <c>KLYROO_TEMPLATE_PUBLISHED</c>, <c>KLYROO_TEMPLATE_ASSEMBLY</c> and <c>KLYROO_TEMPLATE_PROBE</c>; without them
/// the tests are skipped. The pages are static: the test fails if a WebSocket opens or a WebAssembly file is requested.
/// </summary>
public class DocsTemplateTests(BrowserFixture fixture)
{
    private static string? Probe => TemplateApp.Probe("docs");

    [Theory]
    [InlineData(null)]
    [InlineData("/demo/docs")]
    public async Task The_site_is_static_and_its_parts_work(string? pathBase)
    {
        var port = FreePort();
        var host = $"http://127.0.0.1:{port}";
        var start = TemplateApp.StartInfo("docs", host);
        if (pathBase is not null) start.Environment["PathBase"] = pathBase;
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the template.");
        var log = new List<string>();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (log) log.Add(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (log) log.Add(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        try
        {
            var baseUrl = host + (pathBase ?? "").TrimEnd('/') + "/";
            await WaitUntilReady(process, baseUrl, log);

            await using var context = await fixture.Browser.NewContextAsync(new() { BaseURL = baseUrl });
            try
            {
                await context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"], new() { Origin = host });
            }
            catch (PlaywrightException)
            {
                // Only Chromium has these permissions; the other browsers copy on a click without one.
            }
            var failures = new List<string>();
            var runtime = new List<string>();
            var consoleErrors = new List<string>();
            context.Request += (_, request) =>
            {
                if (Regex.IsMatch(request.Url, @"\.wasm(\?|$)|/_blazor(/|\?|$)")) lock (runtime) runtime.Add(request.Url);
            };
            context.Response += (_, response) => { if (response.Status >= 400) lock (failures) failures.Add($"{response.Status} {response.Url}"); };
            var page = await context.NewPageAsync();
            page.Console += (_, message) => { if (message.Type == "error") lock (consoleErrors) consoleErrors.Add($"{message.Text} @ {message.Location}"); };
            page.PageError += (_, error) => { lock (consoleErrors) consoleErrors.Add(error); };
            page.WebSocket += (_, socket) => { lock (runtime) runtime.Add(socket.Url); };

            await page.GotoAsync("");
            await Assertions.Expect(page.Locator("base")).ToHaveAttributeAsync("href", (pathBase ?? "").TrimEnd('/') + "/");
            await DocsChecks.AssertBehaviour(page, checkClipboard: fixture.Browser.BrowserType.Name == "chromium");
            await DocsChecks.AssertIndexIsNotInThePages(page, baseUrl);
            await DocsChecks.AssertColorMode(page);
            await DocsChecks.AssertNoHorizontalScroll(page, "", "reference/commands", "getting-started/quick-start", "guides/configuration");

            // Nothing failed and nothing was logged so far: the stylesheet, the fonts, the script and every link of the pages were found below the
            // path base.
            lock (failures) Assert.True(failures.Count == 0, "Failed requests:\n" + string.Join("\n", failures));
            lock (consoleErrors) Assert.True(consoleErrors.Count == 0, "Console errors:\n" + string.Join("\n", consoleErrors));

            // Pages that do not exist: the 404s are the only failed requests there may be.
            await page.SetViewportSizeAsync(1280, 800);
            await DocsChecks.AssertMissingPages(page, baseUrl, failures);
            lock (failures) Assert.Equal(2, failures.Count);
            lock (consoleErrors) Assert.All(consoleErrors, e => Assert.Matches("no-such-page|webhooks", e));

            lock (runtime) Assert.True(runtime.Count == 0, "A static site started a circuit or loaded WebAssembly:\n" + string.Join("\n", runtime));

            if (!string.IsNullOrEmpty(Probe))
            {
                await page.GotoAsync("");
                var sheet = await page.Locator("link[rel=stylesheet][href*='app']").First.GetAttributeAsync("href");
                Assert.NotNull(sheet);
                using var http = new HttpClient();
                var css = await http.GetStringAsync(new Uri(new Uri(baseUrl), sheet), TestContext.Current.CancellationToken);
                Assert.Contains(Probe, css);
            }
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(TestContext.Current.CancellationToken);
            }
        }
    }

    private static async Task WaitUntilReady(Process process, string url, List<string> log)
    {
        using var http = new HttpClient();
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                string output;
                lock (log) output = string.Join("\n", log);
                throw new InvalidOperationException($"The template exited with code {process.ExitCode}:\n{output}");
            }
            try
            {
                if ((await http.GetAsync(url)).StatusCode == HttpStatusCode.OK) return;
            }
            catch (HttpRequestException)
            {
                // Not listening yet.
            }
            await Task.Delay(250);
        }
        throw new TimeoutException($"The template did not answer at {url} within 60 seconds.");
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
