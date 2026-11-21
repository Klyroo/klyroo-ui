using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Blux.UI.E2E;

/// <summary>
/// What the Changelog template does in a browser, shared by the run of the published app and the run of its demo behind the docs host. Both read the
/// sample releases of the template (<c>Changelog:Source=File</c>), so nothing here needs a network: twelve releases, ten to a page.
/// </summary>
internal static class ChangelogChecks
{
    /// <summary>
    /// The list, the timeline entries, the notes (code with a copy button, links to GitHub for mentions and issue numbers, safe links), the pagination, a release
    /// page with its neighbours, and the feed. Every address is relative to the base, so it holds below a path base.
    /// </summary>
    public static async Task AssertBehaviour(IPage page, string baseUrl, bool checkClipboard)
    {
        // The list: the title, ten releases of the twelve, the newest first.
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Release notes");
        await Assertions.Expect(page).ToHaveTitleAsync("Release notes - Tern");
        var releases = page.GetByTestId("release");
        await Assertions.Expect(releases).ToHaveCountAsync(10);
        var first = releases.First;
        await Assertions.Expect(first.GetByRole(AriaRole.Heading, new() { Level = 2 })).ToHaveTextAsync("Tern 1.4.0");
        await Assertions.Expect(first.Locator("time")).ToHaveAttributeAsync("datetime", new Regex("^2026-11-12"));
        await Assertions.Expect(first).ToContainTextAsync("v1.4.0");
        await Assertions.Expect(first).ToContainTextAsync("mira-okafor");
        // A pre-release says so in words.
        await Assertions.Expect(releases.Filter(new() { HasText = "v1.0.0-rc.2 pre-release" })).ToHaveCountAsync(1);

        // The notes are headings of the right level, code, a callout and links built by the renderer.
        await Assertions.Expect(first.GetByRole(AriaRole.Heading, new() { Level = 3, Name = "Highlights" })).ToBeVisibleAsync();
        await Assertions.Expect(first.Locator("h1")).ToHaveCountAsync(0);
        await Assertions.Expect(first.Locator(".callout[data-callout=note]")).ToContainTextAsync("multipart uploads");
        var mention = first.GetByRole(AriaRole.Link, new() { Name = "@mira-okafor", Exact = true }).First;
        await Assertions.Expect(mention).ToHaveAttributeAsync("href", "https://github.com/mira-okafor");
        await Assertions.Expect(mention).ToHaveAttributeAsync("rel", "noopener noreferrer nofollow");
        await Assertions.Expect(first.GetByRole(AriaRole.Link, new() { Name = "#412", Exact = true })).ToHaveAttributeAsync("href", "https://github.com/tern-cli/tern/issues/412");
        // Nothing opens a new tab, and every link of a body that leaves the site carries the rel.
        await Assertions.Expect(page.Locator("[data-testid=releases] a[target]")).ToHaveCountAsync(0);
        Assert.True(await page.Locator("[data-testid=releases] .markdown-body a[href^='http']").CountAsync() > 5);
        Assert.Equal(0, await page.Locator("[data-testid=releases] .markdown-body a[href^='http']:not([rel='noopener noreferrer nofollow'])").CountAsync());

        // The copy button of a code block.
        var block = first.Locator(".code-block[data-language=bash]").First;
        await block.GetByRole(AriaRole.Button, new() { Name = "Copy" }).ClickAsync();
        await Assertions.Expect(block.GetByRole(AriaRole.Button)).ToHaveTextAsync("Copied");
        if (checkClipboard)
        {
            Assert.Equal("tern sync --resume", await page.EvaluateAsync<string>("() => navigator.clipboard.readText()"));
        }
        await Assertions.Expect(block.GetByRole(AriaRole.Button)).ToHaveTextAsync("Copy");

        // The header: the feed is announced to readers and linked.
        await Assertions.Expect(page.Locator("link[rel=alternate][type='application/atom+xml']")).ToHaveAttributeAsync("href", "feed.xml");
        await Assertions.Expect(page.GetByTestId("feed-link")).ToHaveAttributeAsync("href", "feed.xml");
        await Assertions.Expect(page.GetByTestId("feed-link")).ToHaveAttributeAsync("aria-label", "Atom feed of the releases");

        // Pagination: older releases are the second page, newer ones lead back.
        var pagination = page.GetByRole(AriaRole.Navigation, new() { Name = "Pages of releases" });
        await Assertions.Expect(pagination).ToContainTextAsync("Page 1 of 2");
        await Assertions.Expect(pagination.GetByRole(AriaRole.Link, new() { Name = "Newer releases" })).ToHaveCountAsync(0);
        await pagination.GetByRole(AriaRole.Link, new() { Name = "Older releases" }).ClickAsync();
        await page.WaitForURLAsync(new Regex(@"\?page=2$"));
        await Assertions.Expect(page.GetByTestId("release")).ToHaveCountAsync(2);
        await Assertions.Expect(page.GetByTestId("release").First.GetByRole(AriaRole.Heading, new() { Level = 2 })).ToHaveTextAsync("Tern 0.9.0");
        // v0.8.0 has no name and no notes: its title is its tag.
        await Assertions.Expect(page.GetByTestId("release").Nth(1).GetByRole(AriaRole.Heading, new() { Level = 2 })).ToHaveTextAsync("v0.8.0");
        var last = page.GetByRole(AriaRole.Navigation, new() { Name = "Pages of releases" });
        await Assertions.Expect(last).ToContainTextAsync("Page 2 of 2");
        await Assertions.Expect(last.GetByRole(AriaRole.Link, new() { Name = "Older releases" })).ToHaveCountAsync(0);
        await last.GetByRole(AriaRole.Link, new() { Name = "Newer releases" }).ClickAsync();
        await page.WaitForURLAsync(url => !url.Contains("page="));
        await Assertions.Expect(page.GetByTestId("release")).ToHaveCountAsync(10);

        // A release page: its own address from the tag, the notes in full, a way back, and the neighbours in time.
        await page.GetByTestId("release").First.GetByRole(AriaRole.Link, new() { Name = "Tern 1.4.0" }).ClickAsync();
        await page.WaitForURLAsync(new Regex(@"/releases/v1\.4\.0$"));
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Tern 1.4.0");
        await Assertions.Expect(page).ToHaveTitleAsync("Tern 1.4.0 - Tern");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 2, Name = "Highlights" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "View on GitHub" })).ToHaveAttributeAsync("href", "https://github.com/tern-cli/tern/releases/tag/v1.4.0");
        var neighbours = page.GetByRole(AriaRole.Navigation, new() { Name = "Other releases" });
        await Assertions.Expect(neighbours.GetByRole(AriaRole.Link)).ToHaveCountAsync(1);
        await Assertions.Expect(neighbours.GetByRole(AriaRole.Link, new() { Name = "Tern 1.3.2" })).ToHaveAttributeAsync("rel", "next");
        await neighbours.GetByRole(AriaRole.Link, new() { Name = "Tern 1.3.2" }).ClickAsync();
        await page.WaitForURLAsync(new Regex(@"/releases/v1\.3\.2$"));
        var both = page.GetByRole(AriaRole.Navigation, new() { Name = "Other releases" });
        await Assertions.Expect(both.GetByRole(AriaRole.Link, new() { Name = "Tern 1.4.0" })).ToHaveAttributeAsync("rel", "prev");
        await Assertions.Expect(both.GetByRole(AriaRole.Link, new() { Name = "Tern 1.3.1" })).ToHaveAttributeAsync("rel", "next");
        await page.GetByTestId("back").ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Release notes");

        // A release with no notes says so.
        await Go(page, "releases/v0.8.0");
        await Assertions.Expect(page.GetByText("This release has no notes.")).ToBeVisibleAsync();

        // The feed is valid Atom with absolute links to the pages of the site.
        var response = await page.APIRequest.GetAsync(new Uri(new Uri(baseUrl), "feed.xml").ToString());
        Assert.Equal(200, response.Status);
        Assert.StartsWith("application/atom+xml", response.Headers["content-type"]);
        var feed = XDocument.Parse(await response.TextAsync());
        XNamespace atom = "http://www.w3.org/2005/Atom";
        var entries = feed.Descendants(atom + "entry").ToList();
        Assert.Equal(12, entries.Count);
        Assert.All(entries, entry => Assert.StartsWith(baseUrl + "releases/", (string)entry.Element(atom + "id")!));
        Assert.Equal(baseUrl + "feed.xml", feed.Root!.Elements(atom + "link").Single(l => (string)l.Attribute("rel")! == "self").Attribute("href")!.Value);
    }

    /// <summary>Addresses that are not pages: each is a 404 (never a 500), the page being the template's own.</summary>
    public static readonly string[] MissingPages =
    [
        "releases/no-such-release", "releases/..%2f..%2fetc%2fpasswd", "releases/v1.4.0%2f", "?page=3", "?page=0", "?page=abc", "?page=-1", "?page=99999999999", "?page=1.5",
    ];

    /// <summary>A release that does not exist, or a tag that tries to leave its place, answers 404 with the template's own page; so does a page of releases that is not there.</summary>
    public static async Task AssertMissingPages(IPage page, string baseUrl, List<string> failures)
    {
        foreach (var path in MissingPages)
        {
            // Built as text: a Uri would tidy the address the way this test wants to leave it.
            var response = await page.GotoAsync(baseUrl + path);
            Assert.Equal(404, response!.Status);
            await Assertions.Expect(page.GetByText("There is nothing at this address.")).ToBeVisibleAsync();
            Assert.Equal(1, await page.Locator("base").CountAsync());
        }
        lock (failures) Assert.Equal(MissingPages.Length, failures.Count(f => f.StartsWith("404 ", StringComparison.Ordinal)));
    }

    private static async Task Go(IPage page, string path)
    {
        var baseHref = await page.Locator("base").GetAttributeAsync("href");
        await page.GotoAsync(new Uri(new Uri(page.Url), baseHref + path).ToString());
    }
}

/// <summary>
/// The Changelog template as a user ships it: <c>dotnet publish -c Release</c>, run in Production with the file source (no network) at the root and below a
/// path base. Started by <c>build/verify-templates.mjs</c> with <c>BLUX_TEMPLATE_PUBLISHED</c>, <c>BLUX_TEMPLATE_ASSEMBLY</c> and <c>BLUX_TEMPLATE_PROBE</c>; without them
/// the tests are skipped. The pages are static: the test fails if a WebSocket opens or a WebAssembly file is requested, or if a request fails other than the
/// deliberate 404s.
/// </summary>
public class ChangelogTemplateTests(PlaygroundServer server)
{
    private static string? Published => Environment.GetEnvironmentVariable("BLUX_TEMPLATE_PUBLISHED");
    private static string? AssemblyName => Environment.GetEnvironmentVariable("BLUX_TEMPLATE_ASSEMBLY");
    private static string? Probe => Environment.GetEnvironmentVariable("BLUX_TEMPLATE_PROBE");

    [Theory]
    [InlineData(null)]
    [InlineData("/demo/changelog")]
    public async Task Published_site_is_static_and_its_parts_work(string? pathBase)
    {
        Assert.SkipWhen(string.IsNullOrEmpty(Published) || string.IsNullOrEmpty(AssemblyName), "BLUX_TEMPLATE_PUBLISHED and BLUX_TEMPLATE_ASSEMBLY are not set.");

        var port = FreePort();
        var host = $"http://127.0.0.1:{port}";
        var directory = Path.GetFullPath(Published!);
        var start = new ProcessStartInfo("dotnet", [Path.Combine(directory, AssemblyName!), "--urls", host])
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
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

            await using var context = await server.Browser.NewContextAsync(new() { BaseURL = baseUrl });
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
            await ChangelogChecks.AssertBehaviour(page, baseUrl, checkClipboard: server.Browser.BrowserType.Name == "chromium");
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
        Assert.SkipWhen(string.IsNullOrEmpty(Published) || string.IsNullOrEmpty(AssemblyName), "BLUX_TEMPLATE_PUBLISHED and BLUX_TEMPLATE_ASSEMBLY are not set.");

        var port = FreePort();
        var host = $"http://127.0.0.1:{port}";
        var directory = Path.GetFullPath(Published!);
        var start = new ProcessStartInfo("dotnet", [Path.Combine(directory, AssemblyName!), "--urls", host])
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
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

            await using var context = await server.Browser.NewContextAsync(new() { BaseURL = host + "/" });
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
        Assert.SkipWhen(string.IsNullOrEmpty(Published) || string.IsNullOrEmpty(AssemblyName), "BLUX_TEMPLATE_PUBLISHED and BLUX_TEMPLATE_ASSEMBLY are not set.");

        var port = FreePort();
        var host = $"http://127.0.0.1:{port}";
        var directory = Path.GetFullPath(Published!);
        var start = new ProcessStartInfo("dotnet", [Path.Combine(directory, AssemblyName!), "--urls", host]) { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true };
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the template.");
        var log = new List<string>();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (log) log.Add(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (log) log.Add(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        try
        {
            await WaitUntilReady(process, host + "/", log);
            await using var context = await server.Browser.NewContextAsync(new() { BaseURL = host + "/" });
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
