using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;

namespace Klyroo.UI.E2E;

/// <summary>
/// The Chat template as a user ships it: <c>dotnet publish -c Release</c>, run in Production at the root and below a path base, with the simulated provider
/// (<c>Assistant__Provider=Simulated</c>: no key, no network). Started by <c>build/verify-templates.mjs</c> with <c>KLYROO_TEMPLATE_PUBLISHED</c>,
/// <c>KLYROO_TEMPLATE_ASSEMBLY</c> and <c>KLYROO_TEMPLATE_PROBE</c>; without them the tests are skipped. No request may fail and the console stays free of errors.
/// </summary>
public class ChatTemplateTests(BrowserFixture fixture)
{
    private static string? Probe => TemplateApp.Probe("chat");

    [Theory]
    [InlineData(null)]
    [InlineData("/demo/chat")]
    public async Task The_app_works_from_the_first_question_to_the_deleted_conversation(string? pathBase)
    {
        var port = FreePort();
        var host = $"http://127.0.0.1:{port}";
        var start = TemplateApp.StartInfo("chat", host);
        start.Environment["Assistant__Provider"] = "Simulated";
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
            var wasm = new List<string>();
            var consoleErrors = new List<string>();
            context.Response += (_, response) =>
            {
                lock (failures)
                {
                    // 429 and 503 are what the rate limit test asks for; 4xx of the API checks are asked for on purpose.
                    if (response.Status >= 400 && !response.Url.Contains("/api/assistant/stream", StringComparison.Ordinal) && !response.Url.EndsWith("/no-such-page", StringComparison.Ordinal)) failures.Add($"{response.Status} {response.Url}");
                    if (response.Status == 200 && Regex.IsMatch(response.Url, @"\.wasm(\?|$)")) wasm.Add(response.Url);
                }
            };
            var page = await context.NewPageAsync();
            page.Console += (_, message) => { if (message.Type == "error" && !message.Text.Contains("status of 4", StringComparison.Ordinal)) lock (consoleErrors) consoleErrors.Add($"{message.Text} @ {message.Location}"); };
            page.PageError += (_, error) => { lock (consoleErrors) consoleErrors.Add(error); };

            // First visit: prerendered, then a circuit makes it interactive (the Server renderer: the reply comes from the provider in the host itself).
            await page.GotoAsync("");
            await Assertions.Expect(page.Locator("base")).ToHaveAttributeAsync("href", (pathBase ?? "").TrimEnd('/') + "/");
            await ChatChecks.WaitInteractive(page);
            await ChatChecks.AssertNewConversation(page);
            await ChatChecks.AssertFirstConversation(page, pathBase);
            await ChatChecks.AssertAnswersAreSanitised(page);
            await ChatChecks.AssertStop(page);
            await ChatChecks.AssertReasoning(page);
            await ChatChecks.AssertHistory(page);

            // The WebAssembly runtime downloads in the background. Once it has, the page comes back as WebAssembly, and the replies come from the endpoint of the host.
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
            await Assertions.Expect(ChatChecks.Items(page)).ToHaveCountAsync(1);
            var remote = page.WaitForRequestAsync(r => r.Url.EndsWith("api/assistant/stream", StringComparison.Ordinal) && r.Method == "POST");
            await ChatChecks.Send(page, "Compare SQLite and PostgreSQL for a small app");
            await remote;
            await ChatChecks.WaitIdle(page);
            await Assertions.Expect(page.GetByTestId("message-assistant").Last.GetByRole(AriaRole.Table)).ToBeVisibleAsync();

            await ChatChecks.AssertPersistenceAndIsolation(page, fixture.Browser, baseUrl);
            await ChatChecks.AssertEndpoints(page);
            await ChatChecks.AssertNoHorizontalScroll(page);
            await ChatChecks.AssertMobilePanel(page);

            // Accessibility in the color mode of the run and in the other one.
            var mode = async () => ((await page.Locator("html").GetAttributeAsync("class") ?? "").Contains("dark") ? "dark" : "light");
            await ChatChecks.AssertAccessible(page, await mode());
            await page.Locator("[data-kly-color-mode=toggle]").First.ClickAsync();
            await page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
            await ChatChecks.AssertAccessible(page, await mode());

            // A page that does not exist is the template's own, below the path base too.
            var missing = await page.GotoAsync("no-such-page");
            Assert.Equal(404, missing!.Status);
            await Assertions.Expect(page.GetByText("There is nothing at this address.")).ToBeVisibleAsync();

            await ChatChecks.AssertRateLimited(page);

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
                // The styles of an answer are in the stylesheet the build compiled.
                Assert.Contains(".code-block", css);
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
    /// In the Server render mode the provider runs in the circuit, which has no request of its own: it is limited as the address of the connection that opened it. Three
    /// visitors from this machine share a budget of two answers a minute, whatever their circuits, and the third is refused.
    /// </summary>
    [Fact]
    public async Task Circuits_of_one_address_share_their_limits()
    {
        var host = $"http://127.0.0.1:{FreePort()}";
        var start = TemplateApp.StartInfo("chat", host);
        start.Environment["Assistant__Provider"] = "Simulated";
        start.Environment["Assistant__RequestsPerMinute"] = "2";
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the template.");
        var log = new List<string>();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (log) log.Add(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (log) log.Add(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        try
        {
            await WaitUntilReady(process, host + "/", log);
            var answered = new List<bool>();
            for (var visitor = 0; visitor < 3; visitor++)
            {
                await using var context = await fixture.Browser.NewContextAsync(new() { BaseURL = host + "/" });
                var page = await context.NewPageAsync();
                await page.GotoAsync("");
                await ChatChecks.WaitInteractive(page);
                await ChatChecks.Send(page, "hello " + visitor);
                var refused = page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Too many requests" });
                var reply = page.GetByTestId("message-assistant");
                await Assertions.Expect(reply.Or(refused).First).ToBeVisibleAsync(new() { Timeout = 15_000 });
                answered.Add(await reply.CountAsync() > 0);
            }
            Assert.Equal([true, true, false], answered);
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
