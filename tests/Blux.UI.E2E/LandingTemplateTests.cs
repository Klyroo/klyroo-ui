using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>What the Landing template does in a browser, shared by the run of the published app and the run of its demo behind the docs host.</summary>
internal static class LandingChecks
{
    public static readonly string[] SectionIds = ["features", "how-it-works", "customers", "pricing", "faq"];

    /// <summary>
    /// The page is static HTML: every section is there, and the pieces that move work without .NET in the browser. The monthly and yearly
    /// switch is two radio buttons and CSS, the accordion and the color mode button are Blux elements, so no circuit and no WebAssembly start.
    /// </summary>
    public static async Task AssertBehaviour(IPage page)
    {
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Send an invoice");
        foreach (var id in SectionIds) await Assertions.Expect(page.Locator($"section#{id}")).ToHaveCountAsync(1);
        await Assertions.Expect(page.GetByTestId("testimonial")).ToHaveCountAsync(3);
        // Blux's browser side is loaded by blazor.web.js, static pages included.
        await page.WaitForFunctionAsync("() => !!customElements.get('bx-accordion')");

        // Pricing: monthly first, and the yearly copy is not in the layout.
        var pricing = page.GetByTestId("pricing");
        await Assertions.Expect(pricing.GetByText("$18", new() { Exact = true }).Locator("visible=true")).ToHaveCountAsync(1);
        await Assertions.Expect(pricing.GetByText("$15", new() { Exact = true }).Locator("visible=true")).ToHaveCountAsync(0);
        // Yearly, with the mouse on the label.
        await pricing.GetByText("Yearly, 2 months free").ClickAsync();
        await Assertions.Expect(pricing.GetByText("$15", new() { Exact = true }).Locator("visible=true")).ToHaveCountAsync(1);
        await Assertions.Expect(pricing.GetByText("$18", new() { Exact = true }).Locator("visible=true")).ToHaveCountAsync(0);
        await Assertions.Expect(pricing.GetByText("$180 billed once a year.").Locator("visible=true")).ToHaveCountAsync(1);
        // Back to monthly, with the keyboard: the radio group is a real one.
        await page.GetByRole(AriaRole.Radio, new() { Name = "Yearly, 2 months free" }).FocusAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(page.GetByRole(AriaRole.Radio, new() { Name = "Monthly" })).ToBeCheckedAsync();
        await Assertions.Expect(pricing.GetByText("$18", new() { Exact = true }).Locator("visible=true")).ToHaveCountAsync(1);

        // FAQ: a closed answer opens when its question is pressed.
        var question = page.GetByRole(AriaRole.Button, new() { Name = "How do reminders work?" });
        await Assertions.Expect(question).ToHaveAttributeAsync("aria-expanded", "false");
        await question.ClickAsync();
        await Assertions.Expect(question).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(page.GetByText("Reminders stop when the invoice is paid or cancelled.")).ToBeVisibleAsync();

        // Color mode.
        var dark = (await page.Locator("html").GetAttributeAsync("class") ?? "").Contains("dark");
        await page.Locator("[data-bx-color-mode=toggle]").ClickAsync();
        await Assertions.Expect(page.Locator("html")).ToHaveClassAsync(dark ? new Regex("^((?!dark).)*$") : new Regex("dark"));
    }

    /// <summary>The page has no horizontal scroll at the width of a small phone.</summary>
    public static async Task AssertNoHorizontalScroll(IPage page)
    {
        await page.SetViewportSizeAsync(375, 800);
        var sizes = await page.EvaluateAsync<int[]>("() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
        Assert.True(sizes[0] <= sizes[1], $"horizontal scroll at 375px: scrollWidth {sizes[0]} > clientWidth {sizes[1]}");
    }
}

/// <summary>
/// The Landing template as a user ships it: <c>dotnet publish -c Release</c>, run in Production at the root and below a path base. Started by
/// <c>build/verify-templates.mjs</c> with <c>BLUX_TEMPLATE_PUBLISHED</c>, <c>BLUX_TEMPLATE_ASSEMBLY</c> and <c>BLUX_TEMPLATE_PROBE</c>; without them
/// the tests are skipped. The page is static: the test fails if a WebSocket opens or a WebAssembly file is requested.
/// </summary>
public class LandingTemplateTests(PlaygroundServer server)
{
    private static string? Published => Environment.GetEnvironmentVariable("BLUX_TEMPLATE_PUBLISHED");
    private static string? AssemblyName => Environment.GetEnvironmentVariable("BLUX_TEMPLATE_ASSEMBLY");
    private static string? Probe => Environment.GetEnvironmentVariable("BLUX_TEMPLATE_PROBE");

    [Theory]
    [InlineData(null)]
    [InlineData("/demo/landing")]
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
            var failures = new List<string>();
            var runtime = new List<string>();
            var consoleErrors = new List<string>();
            context.Request += (_, request) =>
            {
                if (Regex.IsMatch(request.Url, @"\.wasm(\?|$)|/_blazor(/|\?|$)")) lock (runtime) runtime.Add(request.Url);
            };
            context.Response += (_, response) => { if (response.Status >= 400) lock (failures) failures.Add($"{response.Status} {response.Url}"); };
            var page = await context.NewPageAsync();
            page.Console += (_, message) => { if (message.Type == "error") lock (consoleErrors) consoleErrors.Add(message.Text); };
            page.PageError += (_, error) => { lock (consoleErrors) consoleErrors.Add(error); };
            page.WebSocket += (_, socket) => { lock (runtime) runtime.Add(socket.Url); };

            await page.GotoAsync("");
            await Assertions.Expect(page.Locator("base")).ToHaveAttributeAsync("href", (pathBase ?? "").TrimEnd('/') + "/");
            await LandingChecks.AssertBehaviour(page);
            await LandingChecks.AssertNoHorizontalScroll(page);

            // A page that does not exist is the template's own, below the path base too.
            var missing = await page.GotoAsync("no-such-page");
            Assert.Equal(404, missing!.Status);
            await Assertions.Expect(page.GetByText("There is nothing at this address.")).ToBeVisibleAsync();

            lock (runtime) Assert.True(runtime.Count == 0, "A static site started a circuit or loaded WebAssembly:\n" + string.Join("\n", runtime));
            lock (failures) Assert.True(failures.Count(f => !f.StartsWith("404 ", StringComparison.Ordinal)) == 0, "Failed requests:\n" + string.Join("\n", failures));
            lock (consoleErrors) Assert.True(consoleErrors.All(e => e.Contains("404")), "Console errors:\n" + string.Join("\n", consoleErrors));

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
