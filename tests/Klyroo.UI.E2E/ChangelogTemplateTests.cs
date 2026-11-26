using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Klyroo.UI.E2E;

/// <summary>
/// The Changelog template as a user ships it: <c>dotnet publish -c Release</c>, run in Production with the file source (no network) at the root and below a
/// path base. Started by <c>build/verify-templates.mjs</c> with <c>KLYROO_TEMPLATE_PUBLISHED</c>, <c>KLYROO_TEMPLATE_ASSEMBLY</c> and <c>KLYROO_TEMPLATE_PROBE</c>; without them
/// the tests are skipped. The pages are static: the test fails if a WebSocket opens or a WebAssembly file is requested, or if a request fails other than the
/// deliberate 404s.
/// </summary>
public class ChangelogTemplateTests(BrowserFixture fixture)
{
    private static string? Probe => TemplateApp.Probe("changelog");

    [Theory]
    [InlineData(null)]
    [InlineData("/demo/changelog")]
    public async Task The_site_is_static_and_its_parts_work(string? pathBase)
    {
        var port = FreePort();
        var host = $"http://127.0.0.1:{port}";
        var start = TemplateApp.StartInfo("changelog", host);
        // The sample releases: this run has no network, and the feed and the links to GitHub need a repository to point at.
        start.Environment["Changelog__Source"] = "File";
        start.Environment["Changelog__Repository"] = "tern-cli/tern";
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
            var outside = new List<string>();
            context.Request += (_, request) =>
            {
                if (Regex.IsMatch(request.Url, @"\.wasm(\?|$)|/_blazor(/|\?|$)")) lock (runtime) runtime.Add(request.Url);
                if (request.Url.StartsWith(host, StringComparison.Ordinal) && !request.Url.StartsWith(baseUrl, StringComparison.Ordinal)) lock (outside) outside.Add(request.Url);
            };
            context.Response += (_, response) => { if (response.Status >= 400) lock (failures) failures.Add($"{response.Status} {response.Url}"); };
            var page = await context.NewPageAsync();
            page.Console += (_, message) => { if (message.Type == "error") lock (consoleErrors) consoleErrors.Add($"{message.Text} @ {message.Location}"); };
            page.PageError += (_, error) => { lock (consoleErrors) consoleErrors.Add(error); };
            page.WebSocket += (_, socket) => { lock (runtime) runtime.Add(socket.Url); };

            await page.GotoAsync("");
            await Assertions.Expect(page.Locator("base")).ToHaveAttributeAsync("href", (pathBase ?? "").TrimEnd('/') + "/");
            await ChangelogChecks.AssertBehaviour(page, baseUrl, checkClipboard: fixture.Browser.BrowserType.Name == "chromium");
            await page.GotoAsync("");
            await DocsChecks.AssertColorMode(page);
            await DocsChecks.AssertNoHorizontalScroll(page, "", "?page=2", "releases/v1.4.0", "releases/v1.3.0", "releases/v1.3.1");

            // Nothing failed and nothing was logged so far: the stylesheet, the fonts, the script and every link of the pages were found below the path base.
            lock (failures) Assert.True(failures.Count == 0, "Failed requests:\n" + string.Join("\n", failures));
            lock (consoleErrors) Assert.True(consoleErrors.Count == 0, "Console errors:\n" + string.Join("\n", consoleErrors));
            lock (outside) Assert.True(outside.Count == 0, "Requests outside the path base:\n" + string.Join("\n", outside));

            // Pages that do not exist: the 404s are the only failed requests there may be.
            await page.SetViewportSizeAsync(1280, 800);
            await ChangelogChecks.AssertMissingPages(page, baseUrl, failures);
            lock (failures) Assert.Equal(ChangelogChecks.MissingPages.Length, failures.Count);
            lock (consoleErrors) Assert.All(consoleErrors, e => Assert.Matches("404|Not Found|Failed to load resource", e));

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

    /// <summary>
    /// A site that has no releases to show (no repository, a file that is not there) still starts and answers: every page is a 503 with a page that says so
    /// and no setting name in it (those are for the log), the feed is a 503 too, a release address is never a crash, and nothing fails in the browser but the 503s.
    /// </summary>
    [Theory]
    [InlineData("GitHub", "")]
    [InlineData("File", "data/no-such-file.json")]
    public async Task A_site_with_no_releases_to_show_answers_503_with_a_friendly_page_and_does_not_crash(string source, string file)
    {
        var port = FreePort();
        var host = $"http://127.0.0.1:{port}";
        // What a visitor of a deployed site sees: in Development the page spells out the missing setting for the developer.
        var start = await TemplateApp.PublishedStartInfoAsync("changelog", host);
        start.Environment["Changelog__Source"] = source;
        start.Environment["Changelog__Repository"] = source == "GitHub" ? "" : "tern-cli/tern";
        if (file.Length > 0) start.Environment["Changelog__File"] = file;
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the template.");
        var log = new List<string>();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (log) log.Add(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (log) log.Add(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        try
        {
            // Not ready by the usual test (a 200): wait for any answer.
            using var http = new HttpClient();
            var deadline = DateTime.UtcNow.AddSeconds(60);
            while (true)
            {
                Assert.False(process.HasExited, "The template exited:\n" + string.Join("\n", log));
                Assert.True(DateTime.UtcNow < deadline, "The template did not answer.");
                try
                {
                    await http.GetAsync(host + "/", TestContext.Current.CancellationToken);
                    break;
                }
                catch (HttpRequestException)
                {
                    await Task.Delay(250, TestContext.Current.CancellationToken);
                }
            }

            await using var context = await fixture.Browser.NewContextAsync(new() { BaseURL = host + "/" });
            var page = await context.NewPageAsync();
            foreach (var path in new[] { "", "releases/v1.4.0", "?page=2" })
            {
                var response = await page.GotoAsync(path);
                Assert.Equal(503, response!.Status);
                Assert.Equal("300", response.Headers["retry-after"]);
                await Assertions.Expect(page.GetByTestId("releases-unavailable")).ToBeVisibleAsync();
                await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "The release notes are not available right now", Level = 2 })).ToBeVisibleAsync();
                // A visitor is told nothing about the settings.
                var text = await page.Locator("main").InnerTextAsync();
                Assert.DoesNotContain("Changelog:", text);
                Assert.DoesNotContain("appsettings", text);
            }
            var feed = await page.APIRequest.GetAsync(host + "/feed.xml");
            Assert.Equal(503, feed.Status);
            Assert.Equal("300", feed.Headers["retry-after"]);

            // The site is up and says why in its log, once: no stack trace, no secret.
            string output;
            lock (log) output = string.Join("\n", log);
            Assert.DoesNotContain("Unhandled exception", output);
            Assert.DoesNotContain("   at ", output);
            Assert.Contains(source == "GitHub" ? "Changelog:Repository is not set" : "could not be read", output);
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

    /// <summary>Nothing set at all: the sample releases are shown, and the page says they are made up.</summary>
    [Fact]
    public async Task With_no_repository_and_no_source_the_sample_releases_are_shown_with_a_notice()
    {
        var port = FreePort();
        var host = $"http://127.0.0.1:{port}";
        var start = TemplateApp.StartInfo("changelog", host);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the template.");
        var log = new List<string>();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (log) log.Add(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (log) log.Add(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        try
        {
            await WaitUntilReady(process, host + "/", log);
            await using var context = await fixture.Browser.NewContextAsync(new() { BaseURL = host + "/" });
            var page = await context.NewPageAsync();
            foreach (var path in new[] { "", "releases/v1.4.0" })
            {
                var response = await page.GotoAsync(path);
                Assert.Equal(200, response!.Status);
                await Assertions.Expect(page.GetByTestId("sample-notice")).ToContainTextAsync("Set Changelog:Repository");
            }
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Tern 1.4.0");
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
