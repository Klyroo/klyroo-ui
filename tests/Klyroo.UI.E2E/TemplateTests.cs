using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Klyroo.UI.E2E;

/// <summary>
/// A project template as a user ships it: the output of <c>dotnet publish -c Release</c>, run in Production (static web assets from the publish
/// folder, trimmed WebAssembly), at the root and below a path base. Started by <c>build/verify-templates.mjs</c>, which sets
/// <c>KLYROO_TEMPLATE_PUBLISHED</c> (the publish folder), <c>KLYROO_TEMPLATE_ASSEMBLY</c> (for example Starter.Web.dll) and <c>KLYROO_TEMPLATE_PROBE</c>
/// (CSS only the template's own pages produce); without them the tests are skipped.
/// </summary>
public class TemplateTests(BrowserFixture fixture)
{
    private static string? Probe => TemplateApp.Probe("starter");

    [Theory]
    [InlineData(null)]
    [InlineData("/demo/starter")]
    public async Task The_app_becomes_interactive_and_hands_off_to_WebAssembly(string? pathBase)
    {
        var port = FreePort();
        var host = $"http://127.0.0.1:{port}";
        // The runtime of the Auto mode is only picked from the cache of a published app (Development serves the framework files uncached).
        var start = await TemplateApp.PublishedStartInfoAsync("starter", host);
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
            var failures = new List<string>();
            var wasm = new List<string>();
            var consoleErrors = new List<string>();
            context.Response += (_, response) =>
            {
                lock (failures)
                {
                    if (response.Status >= 400) failures.Add($"{response.Status} {response.Url}");
                    if (response.Status == 200 && Regex.IsMatch(response.Url, @"\.wasm(\?|$)")) wasm.Add(response.Url);
                }
            };
            var page = await context.NewPageAsync();
            page.Console += (_, message) => { if (message.Type == "error") lock (consoleErrors) consoleErrors.Add(message.Text); };
            page.PageError += (_, error) => { lock (consoleErrors) consoleErrors.Add(error); };

            // First visit: the page is prerendered, then a Blazor Server circuit makes it interactive while the WebAssembly runtime downloads.
            await page.GotoAsync("");
            var counter = page.GetByTestId("counter");
            await Assertions.Expect(page.GetByTestId("renderer")).ToHaveTextAsync(new Regex("Running on (Server|WebAssembly)"), new() { Timeout = 30_000 });
            await page.GetByTestId("counter-button").ClickAsync();
            await Assertions.Expect(counter).ToHaveTextAsync("Clicked 1 times", new() { Timeout = 30_000 });

            // The skip link of the layout, on this page and below the path base, keeps the address.
            var before = page.Url;
            await page.GetByRole(AriaRole.Link, new() { Name = "Skip to content" }).FocusAsync();
            await page.Keyboard.PressAsync("Enter");
            Assert.Equal(before, page.Url);
            await Assertions.Expect(page.Locator("main#main")).ToBeFocusedAsync();

            // The runtime of the Auto mode is downloaded in the background: the framework files are served from the published folder.
            var deadline = DateTime.UtcNow.AddSeconds(90);
            while (DateTime.UtcNow < deadline)
            {
                lock (failures) if (wasm.Count > 0) break;
                await Task.Delay(250, TestContext.Current.CancellationToken);
            }
            lock (failures) Assert.True(wasm.Count > 0, "No .wasm file was served.");
            await Task.Delay(1500, TestContext.Current.CancellationToken);

            // Second visit: the runtime is cached, so Auto picks WebAssembly.
            await page.GotoAsync("");
            await Assertions.Expect(page.GetByTestId("renderer")).ToHaveTextAsync("Running on WebAssembly", new() { Timeout = 60_000 });
            await page.GetByTestId("counter-button").ClickAsync();
            await page.GetByTestId("counter-button").ClickAsync();
            await Assertions.Expect(counter).ToHaveTextAsync("Clicked 2 times", new() { Timeout = 30_000 });

            // A non-home page works below the path base too.
            await page.GotoAsync("about");
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToBeVisibleAsync();

            lock (failures) Assert.True(failures.Count == 0, "Failed requests:\n" + string.Join("\n", failures));
            lock (consoleErrors) Assert.True(consoleErrors.Count == 0, "Console errors:\n" + string.Join("\n", consoleErrors));

            if (!string.IsNullOrEmpty(Probe))
            {
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
