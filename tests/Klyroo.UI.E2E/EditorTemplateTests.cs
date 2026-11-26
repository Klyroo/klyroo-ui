using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;

namespace Klyroo.UI.E2E;

/// <summary>
/// The Editor template as a user ships it: <c>dotnet publish -c Release</c>, run in Production at the root and below a path base. Started by
/// <c>build/verify-templates.mjs</c> with <c>KLYROO_TEMPLATE_PUBLISHED</c>, <c>KLYROO_TEMPLATE_ASSEMBLY</c> and <c>KLYROO_TEMPLATE_PROBE</c>; without them the tests are skipped.
/// No request may fail and the console stays free of errors.
/// </summary>
public class EditorTemplateTests(BrowserFixture fixture)
{
    private static string? Probe => TemplateApp.Probe("editor");

    [Theory]
    [InlineData(null)]
    [InlineData("/demo/editor")]
    public async Task The_app_works_from_the_first_document_to_the_deleted_one(string? pathBase)
    {
        var host = $"http://127.0.0.1:{FreePort()}";
        var start = TemplateApp.StartInfo("editor", host);
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
            var failures = new List<string>();
            var wasm = new List<string>();
            var consoleErrors = new List<string>();
            context.Response += (_, response) =>
            {
                lock (failures)
                {
                    if (response.Status >= 400 && !response.Url.EndsWith("/no-such-page", StringComparison.Ordinal)) failures.Add($"{response.Status} {response.Url}");
                    if (response.Status == 200 && Regex.IsMatch(response.Url, @"\.wasm(\?|$)")) wasm.Add(response.Url);
                }
            };
            var page = await context.NewPageAsync();
            page.Console += (_, message) => { if (message.Type == "error" && !message.Text.Contains("status of 404", StringComparison.Ordinal)) lock (consoleErrors) consoleErrors.Add($"{message.Text} @ {message.Location}"); };
            page.PageError += (_, error) => { lock (consoleErrors) consoleErrors.Add(error); };

            // First visit: prerendered, then a circuit makes it interactive (the Server renderer).
            await page.GotoAsync("");
            await Assertions.Expect(page.Locator("base")).ToHaveAttributeAsync("href", (pathBase ?? "").TrimEnd('/') + "/");
            await EditorChecks.Tour(page, fixture.Browser, baseUrl, pathBase);
            // The Server renderer carries large documents and many of them, the removals are announced and two tabs follow each other (each in a browser that never gets the WebAssembly runtime).
            await EditorChecks.AssertServerCircuitCarriesLargeDocuments(fixture.Browser, baseUrl, pathBase);
            await EditorChecks.AssertRemovalsAreAnnounced(fixture.Browser, baseUrl, pathBase);
            await EditorChecks.AssertTwoTabs(fixture.Browser, baseUrl, pathBase);

            // The WebAssembly runtime downloads in the background. Once it has, the page comes back as WebAssembly and the documents are still there.
            var deadline = DateTime.UtcNow.AddSeconds(90);
            while (DateTime.UtcNow < deadline)
            {
                lock (failures) if (wasm.Count > 0) break;
                await Task.Delay(250, TestContext.Current.CancellationToken);
            }
            lock (failures) Assert.True(wasm.Count > 0, "No .wasm file was served.");
            await Task.Delay(1500, TestContext.Current.CancellationToken);
            await page.GotoAsync("");
            await Assertions.Expect(page.GetByTestId("renderer")).ToHaveTextAsync("WebAssembly", new() { Timeout = 60_000 });
            await Assertions.Expect(EditorChecks.Items(page).First).ToBeVisibleAsync();
            var before = await EditorChecks.Items(page).CountAsync();
            await EditorChecks.NewDocument(page);
            await EditorChecks.Surface(page).ClickAsync();
            await page.Keyboard.TypeAsync("Written by WebAssembly");
            await EditorChecks.WaitStored(page, pathBase, "Written by WebAssembly");
            await Assertions.Expect(EditorChecks.Items(page)).ToHaveCountAsync(before + 1);
            await page.ReloadAsync();
            await Assertions.Expect(page.GetByTestId("renderer")).ToHaveTextAsync(new Regex("^(Server|WebAssembly)$"), new() { Timeout = 60_000 });
            await EditorChecks.WaitEditor(page);
            await Assertions.Expect(EditorChecks.Surface(page)).ToContainTextAsync("Written by WebAssembly");

            // Another tab of the same browser, with the runtime already downloaded (so WebAssembly too): what it writes reaches this one, which is not being typed in.
            var second = await context.NewPageAsync();
            await second.GotoAsync(page.Url);
            await Assertions.Expect(second.GetByTestId("renderer")).ToHaveTextAsync("WebAssembly", new() { Timeout = 60_000 });
            await EditorChecks.WaitEditor(second);
            await EditorChecks.Surface(second).ClickAsync();
            await second.Keyboard.PressAsync("Control+End");
            await second.Keyboard.TypeAsync(" and a second tab");
            await Assertions.Expect(EditorChecks.Surface(page)).ToContainTextAsync("and a second tab", new() { Timeout = 15_000 });
            await Assertions.Expect(page.GetByTestId("external-change")).ToBeVisibleAsync();
            await second.CloseAsync();

            await EditorChecks.AssertNoHorizontalScroll(page);
            await EditorChecks.AssertMobilePanel(page);

            // Accessibility in the color mode of the run and in the other one.
            var mode = async () => ((await page.Locator("html").GetAttributeAsync("class") ?? "").Contains("dark") ? "dark" : "light");
            await EditorChecks.AssertAccessible(page, await mode());
            await page.Locator("[data-kly-color-mode=toggle]").First.ClickAsync();
            await page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
            await EditorChecks.AssertAccessible(page, await mode());

            // A page that does not exist is the template's own, below the path base too.
            var missing = await page.GotoAsync("no-such-page");
            Assert.Equal(404, missing!.Status);
            await Assertions.Expect(page.GetByText("There is nothing at this address.")).ToBeVisibleAsync();

            lock (failures) Assert.True(failures.Count == 0, "Failed requests:\n" + string.Join("\n", failures));
            lock (consoleErrors) Assert.True(consoleErrors.Count == 0, "Console errors:\n" + string.Join("\n", consoleErrors));

            if (!string.IsNullOrEmpty(Probe))
            {
                await page.GotoAsync("");
                var sheet = await page.Locator("link[rel=stylesheet][href*='app']").First.GetAttributeAsync("href");
                Assert.NotNull(sheet);
                using var http = new HttpClient();
                var css = await http.GetStringAsync(new Uri(new Uri(baseUrl), sheet), TestContext.Current.CancellationToken);
                Assert.Contains(Probe, css);
                // The task items and images of the editor are styled by the library, from the classes the build compiled.
                Assert.Contains("data-type=taskList", css.Replace("\\", "").Replace("\"", ""));
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
