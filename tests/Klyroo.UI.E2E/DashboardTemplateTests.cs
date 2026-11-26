using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;

namespace Klyroo.UI.E2E;

/// <summary>
/// The Dashboard template as a user ships it: <c>dotnet publish -c Release</c>, run in Production at the root and below a path base. Started by
/// <c>build/verify-templates.mjs</c> with <c>KLYROO_TEMPLATE_PUBLISHED</c>, <c>KLYROO_TEMPLATE_ASSEMBLY</c> and <c>KLYROO_TEMPLATE_PROBE</c>; without them
/// the tests are skipped. Every page is checked in the browser; no request may fail and the console stays free of errors.
/// </summary>
public class DashboardTemplateTests(BrowserFixture fixture)
{
    private static string? Probe => TemplateApp.Probe("dashboard");

    [Theory]
    [InlineData(null)]
    [InlineData("/demo/dashboard")]
    public async Task The_app_works_page_by_page(string? pathBase)
    {
        var port = FreePort();
        var host = $"http://127.0.0.1:{port}";
        var start = TemplateApp.StartInfo("dashboard", host);
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

            await using var context = await fixture.Browser.NewContextAsync(new() { BaseURL = baseUrl, ViewportSize = new() { Width = 1280, Height = 800 } });
            await context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
            var failures = new List<string>();
            var consoleErrors = new List<string>();
            context.Response += (_, response) => { if (response.Status >= 400) lock (failures) failures.Add($"{response.Status} {response.Url}"); };
            var page = await context.NewPageAsync();
            page.Console += (_, message) => { if (message.Type == "error") lock (consoleErrors) consoleErrors.Add($"{message.Text} @ {message.Location}"); };
            page.PageError += (_, error) => { lock (consoleErrors) consoleErrors.Add(error); };

            await page.GotoAsync("");
            await Assertions.Expect(page.Locator("base")).ToHaveAttributeAsync("href", (pathBase ?? "").TrimEnd('/') + "/");
            await DashboardChecks.WaitInteractive(page);
            await DashboardChecks.AssertHome(page);
            await DashboardChecks.AssertNoHorizontalScroll(page);

            await page.GotoAsync("customers");
            await DashboardChecks.WaitInteractive(page);
            await DashboardChecks.AssertCustomers(page);

            await DashboardChecks.AssertCreateMenu(page);
            await DashboardChecks.AssertSelectionFollowsTheQuery(page);
            await DashboardChecks.AssertDeleteEveryResult(page);
            await DashboardChecks.AssertStaticPosts(fixture.Browser, baseUrl);

            await page.GotoAsync("inbox");
            await DashboardChecks.WaitInteractive(page);
            await DashboardChecks.AssertInbox(page);

            await page.GotoAsync("settings");
            await DashboardChecks.WaitInteractive(page);
            await DashboardChecks.AssertSettings(page);

            await page.GotoAsync("");
            await DashboardChecks.WaitInteractive(page);
            await DashboardChecks.AssertShell(page);
            await DashboardChecks.AssertMobilePanel(page);
            await DashboardChecks.AssertColorsInThePrerender(page, fixture.Browser, baseUrl, pathBase);

            lock (failures) Assert.True(failures.Count == 0, "Failed requests:\n" + string.Join("\n", failures));
            lock (consoleErrors) Assert.True(consoleErrors.Count == 0, "Console errors:\n" + string.Join("\n", consoleErrors));

            // Accessibility: no axe violation on any page, in the color mode the run ended in (the shell check changed it, and the primary color) and in the other one.
            var mode = async () => ((await page.Locator("html").GetAttributeAsync("class") ?? "").Contains("dark") ? "dark" : "light");
            await DashboardChecks.AssertAccessible(page, await mode());
            await page.GotoAsync("");
            await DashboardChecks.WaitInteractive(page);
            await page.Locator("[data-kly-color-mode=toggle]").First.ClickAsync();
            await page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
            await DashboardChecks.AssertAccessible(page, await mode());

            // A page that does not exist is the template's own, below the path base too.
            var missingUrl = new Uri(new Uri(baseUrl), "no-such-page").ToString();
            var missing = await page.GotoAsync("no-such-page");
            Assert.Equal(404, missing!.Status);
            await Assertions.Expect(page.GetByText("There is nothing at this address.")).ToBeVisibleAsync();

            if (!string.IsNullOrEmpty(Probe))
            {
                await page.GotoAsync("");
                var sheet = await page.Locator("link[rel=stylesheet][href*='app']").First.GetAttributeAsync("href");
                Assert.NotNull(sheet);
                using var http = new HttpClient();
                var css = await http.GetStringAsync(new Uri(new Uri(baseUrl), sheet), TestContext.Current.CancellationToken);
                Assert.Contains(Probe, css);
                // The classes of Klyroo.Table (the data list) are compiled in too: it has no stylesheet of its own.
                Assert.Contains("webkit-search-cancel-button", css);
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
