using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;

namespace Blux.UI.E2E;

/// <summary>What the Chat template does in a browser, shared by the run of the published app and the run of its demo behind the docs host.</summary>
internal static class ChatChecks
{
    private static readonly Regex Interactive = new("^(Server|WebAssembly)$");

    public static Task WaitInteractive(IPage page) =>
        Assertions.Expect(page.GetByTestId("renderer")).ToHaveTextAsync(Interactive, new() { Timeout = 60_000 });

    /// <summary>Polls the address in the page (see PlaygroundTest.WaitForUrl): WaitForURLAsync can miss a navigation of the interactive router.</summary>
    public static Task WaitForUrl(IPage page, string expression) =>
        page.WaitForFunctionAsync($"() => {{ const url = new URL(location.href); return {expression}; }}", null, new() { Timeout = 15_000 });

    public static Task WaitForConversationUrl(IPage page) => WaitForUrl(page, "/\\/c\\/[0-9a-f]{32}$/.test(url.pathname)");

    private static ILocator Prompt(IPage page) => page.GetByRole(AriaRole.Textbox, new() { Name = "Message" });

    /// <summary>Types a message and sends it with Enter.</summary>
    public static async Task Send(IPage page, string text)
    {
        var prompt = Prompt(page);
        var before = await page.GetByTestId("message-user").CountAsync();
        await prompt.FillAsync(text);
        await prompt.PressAsync("Enter");
        // The send button is still there until the host answers the first round trip: wait until the question is in the list.
        await Assertions.Expect(page.GetByTestId("message-user")).ToHaveCountAsync(before + 1);
    }

    /// <summary>Waits for the answer to finish: nothing streams any more and the send button is back.</summary>
    public static async Task WaitIdle(IPage page, int timeout = 30_000)
    {
        await Assertions.Expect(page.Locator("bx-chat-prompt[data-status=idle]")).ToHaveCountAsync(1, new() { Timeout = timeout });
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Send" })).ToBeVisibleAsync(new() { Timeout = timeout });
        await Assertions.Expect(page.Locator("[data-testid=message-assistant][data-streaming=true]")).ToHaveCountAsync(0, new() { Timeout = timeout });
    }

    public static ILocator Items(IPage page) => page.GetByTestId("sidebar").GetByTestId("conversation-item");

    /// <summary>The greeting of a new conversation, its four suggestions, the empty list, the prompt and the model picker.</summary>
    public static async Task AssertNewConversation(IPage page)
    {
        await Assertions.Expect(page.GetByTestId("empty-conversation")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("New conversation");
        await Assertions.Expect(page.GetByTestId("suggestion")).ToHaveCountAsync(4);
        await Assertions.Expect(page.GetByTestId("conversations-empty")).ToBeVisibleAsync();
        await Assertions.Expect(Prompt(page)).ToBeVisibleAsync();
        var model = page.GetByRole(AriaRole.Combobox, new() { Name = "Model" });
        await Assertions.Expect(model).ToBeVisibleAsync();
        await Assertions.Expect(model.Locator("span[data-label]")).ToHaveAttributeAsync("data-label", "Quick (simulated)");
    }

    /// <summary>
    /// A suggestion starts a conversation: the address names it, the answer streams as Markdown (heading, table, highlighted code with a working Copy button), the list
    /// shows it under Today, and the prompt has the focus again.
    /// </summary>
    public static async Task AssertFirstConversation(IPage page, string? pathBase)
    {
        await page.GetByTestId("suggestion").First.ClickAsync();
        await WaitForConversationUrl(page);
        await Assertions.Expect(page.GetByTestId("message-user")).ToContainTextAsync("mutex and a semaphore");
        // While it arrives: marked as streaming, and the prompt offers Stop.
        await Assertions.Expect(page.Locator("[data-testid=message-assistant]")).ToBeVisibleAsync();
        await WaitIdle(page);

        var answer = page.GetByTestId("message-assistant").Last;
        await Assertions.Expect(answer.GetByRole(AriaRole.Heading, new() { Level = 2, Name = "Mutex or semaphore" })).ToBeVisibleAsync();
        await Assertions.Expect(answer.GetByRole(AriaRole.Table)).ToBeVisibleAsync();
        await Assertions.Expect(answer.Locator(".code-block[data-language=csharp] .tok-keyword").First).ToBeVisibleAsync();
        await Assertions.Expect(answer.Locator("blockquote")).ToContainTextAsync("Explain the difference");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Explain the difference between a mutex");
        await Assertions.Expect(page).ToHaveTitleAsync(new Regex(@"Explain the difference.* · Chat"));

        // The sidebar lists it under Today, as the current page, and the prompt is where the user types next.
        await Assertions.Expect(Items(page)).ToHaveCountAsync(1);
        await Assertions.Expect(Items(page).First).ToHaveAttributeAsync("aria-current", "page");
        await Assertions.Expect(page.GetByTestId("sidebar").GetByText("Today", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(Prompt(page)).ToBeFocusedAsync();

        // The Copy button of the code block copies the code and says so.
        var block = answer.Locator(".code-block").First;
        await block.GetByRole(AriaRole.Button, new() { Name = "Copy" }).ClickAsync();
        await Assertions.Expect(block.GetByRole(AriaRole.Button)).ToHaveTextAsync("Copied");
        var clipboard = await page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
        Assert.Contains("SemaphoreSlim(3)", clipboard);
        Assert.DoesNotContain("Copied", clipboard);

        // The copy button of the answer copies the Markdown of it.
        await answer.GetByTestId("message-copy").ClickAsync();
        await Assertions.Expect(page.GetByText("The answer is on the clipboard.")).ToBeVisibleAsync();
        Assert.Contains("## Mutex or semaphore", await page.EvaluateAsync<string>("() => navigator.clipboard.readText()"));

        // The text of the conversation is in the storage of this browser, under keys scoped to the path base, and nowhere else.
        var keys = await page.EvaluateAsync<string[]>("() => Object.keys(localStorage)");
        var prefix = string.IsNullOrEmpty(pathBase) ? "conversations" : "conversations." + string.Join(".", pathBase.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(s => Regex.Replace(s, "[^A-Za-z0-9_-]", "~"))) + "~~";
        Assert.Contains(keys, key => key.StartsWith(prefix + ":c:", StringComparison.Ordinal));
    }

    /// <summary>What the model writes is never run: raw html, a javascript link and an image from outside come out as text or nothing.</summary>
    public static async Task AssertAnswersAreSanitised(IPage page)
    {
        await Send(page, "<script>window.__xss = 1</script> <img src=x onerror=\"window.__xss = 2\"> [go](javascript:window.__xss=3) ![t](https://evil.example.test/p.png)");
        await Assertions.Expect(page.GetByTestId("message-user").Last).ToContainTextAsync("<script>window.__xss = 1</script>");
        await WaitIdle(page);
        var messages = page.GetByTestId("messages");
        await Assertions.Expect(page.GetByTestId("message-assistant").Last.Locator("blockquote")).ToContainTextAsync("<script>");
        Assert.Equal(0, await messages.Locator("script, img, iframe, object, embed, [onerror], a[href^=javascript]").CountAsync());
        Assert.Null(await page.EvaluateAsync<object?>("() => window.__xss ?? null"));
    }

    /// <summary>Stop keeps what arrived and lets the next question go; the answer is cut, not complete.</summary>
    public static async Task AssertStop(IPage page)
    {
        await Send(page, "Write a C# method that debounces calls");
        var stop = page.GetByRole(AriaRole.Button, new() { Name = "Stop" });
        await Assertions.Expect(stop).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("[data-testid=message-assistant][data-streaming=true]")).ToHaveCountAsync(1);
        var started = Stopwatch.StartNew();
        await Task.Delay(500);
        var atClick = (await page.GetByTestId("message-assistant").Last.InnerTextAsync()).Length;
        await stop.ClickAsync();
        var clicked = started.ElapsedMilliseconds;
        await WaitIdle(page);
        var answer = page.GetByTestId("message-assistant").Last;
        await Assertions.Expect(answer).ToContainTextAsync("debouncer");
        var text = await answer.InnerTextAsync();
        Assert.True(!text.Contains("Dispose the last source"), $"the answer was not cut ({text.Length} characters; {atClick} when Stop was pressed, {clicked} ms after the delay): {text}");
    }

    /// <summary>A model that thinks first: the thinking is a block above the answer.</summary>
    public static async Task AssertReasoning(IPage page)
    {
        var model = page.GetByRole(AriaRole.Combobox, new() { Name = "Model" });
        await model.ClickAsync();
        await page.GetByRole(AriaRole.Option, new() { Name = "Thoughtful (simulated)" }).ClickAsync();
        await Assertions.Expect(model.Locator("span[data-label]")).ToHaveAttributeAsync("data-label", "Thoughtful (simulated)");
        await Send(page, "Compare SQLite and PostgreSQL for a small app");
        await WaitIdle(page);
        var answer = page.GetByTestId("message-assistant").Last;
        await Assertions.Expect(answer.GetByTestId("message-reasoning")).ToBeVisibleAsync();
        await Assertions.Expect(answer.GetByRole(AriaRole.Heading, new() { Level = 2 })).ToContainTextAsync("SQLite or PostgreSQL");
        await answer.GetByTestId("message-regenerate").ClickAsync();
        await WaitIdle(page);
        await Assertions.Expect(page.GetByTestId("message-assistant")).ToHaveCountAsync(4);
    }

    /// <summary>Search filters the list, rename and delete work from the row and from the navbar, and a deleted conversation takes the page back to a new one.</summary>
    public static async Task AssertHistory(IPage page)
    {
        await Assertions.Expect(Items(page)).ToHaveCountAsync(1);
        await page.GetByTestId("new-conversation").ClickAsync();
        await Assertions.Expect(page.GetByTestId("empty-conversation")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("New conversation");
        await Send(page, "Draft a polite email asking for a deadline extension");
        await WaitIdle(page);
        await Assertions.Expect(Items(page)).ToHaveCountAsync(2);
        // The newest first.
        await Assertions.Expect(Items(page).First).ToContainTextAsync("Draft a polite email");

        var search = page.GetByRole(AriaRole.Textbox, new() { Name = "Search conversations" });
        await search.FillAsync("mutex");
        await Assertions.Expect(Items(page)).ToHaveCountAsync(1);
        await Assertions.Expect(Items(page).First).ToContainTextAsync("mutex");
        await search.FillAsync("zzzz nothing");
        await Assertions.Expect(page.GetByTestId("conversations-none")).ToBeVisibleAsync();
        await search.PressAsync("Escape");
        await Assertions.Expect(Items(page)).ToHaveCountAsync(2);

        // Rename from the row.
        await page.GetByTestId("sidebar").GetByTestId("conversation-menu").First.ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Rename" }).ClickAsync();
        var rename = page.GetByRole(AriaRole.Dialog, new() { Name = "Rename conversation" });
        await Assertions.Expect(rename.GetByRole(AriaRole.Textbox)).ToHaveValueAsync(new Regex("^Draft a polite email"));
        await rename.GetByRole(AriaRole.Textbox).FillAsync("  Extension\nrequest ");
        await rename.GetByTestId("rename-save").ClickAsync();
        await Assertions.Expect(rename).ToBeHiddenAsync();
        await Assertions.Expect(Items(page).First).ToHaveTextAsync("Extension request");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Extension request");

        // An empty title cannot be saved.
        await page.GetByTestId("conversation-options").ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Rename" }).ClickAsync();
        await rename.GetByRole(AriaRole.Textbox).FillAsync("   ");
        await Assertions.Expect(rename.GetByTestId("rename-save")).ToBeDisabledAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(rename).ToBeHiddenAsync();

        // Delete from the navbar, with a confirmation.
        await page.GetByTestId("conversation-options").ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Delete" }).ClickAsync();
        var confirm = page.GetByRole(AriaRole.Dialog, new() { Name = "Delete conversation?" });
        await Assertions.Expect(confirm).ToContainTextAsync("Extension request");
        await confirm.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await Assertions.Expect(Items(page)).ToHaveCountAsync(2);
        await page.GetByTestId("conversation-options").ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Delete" }).ClickAsync();
        await confirm.GetByTestId("delete-confirm").ClickAsync();
        await Assertions.Expect(confirm).ToBeHiddenAsync();
        await Assertions.Expect(Items(page)).ToHaveCountAsync(1);
        await Assertions.Expect(page.GetByTestId("empty-conversation")).ToBeVisibleAsync();
        await WaitForUrl(page, "!/\\/c\\//.test(url.pathname)");
    }

    /// <summary>The conversations are in the storage of the browser: they survive a reload, and another visitor (another browser context) sees none of them.</summary>
    public static async Task AssertPersistenceAndIsolation(IPage page, IBrowser browser, string baseUrl)
    {
        await Items(page).Filter(new() { HasText = "mutex" }).ClickAsync();
        await WaitForConversationUrl(page);
        var url = page.Url;
        await page.ReloadAsync();
        await WaitInteractive(page);
        await Assertions.Expect(page.GetByTestId("message-user").First).ToContainTextAsync("mutex and a semaphore");
        await Assertions.Expect(page.GetByTestId("message-assistant").First.GetByRole(AriaRole.Heading, new() { Level = 2 })).ToBeVisibleAsync();
        Assert.Equal(url, page.Url);
        await Assertions.Expect(Items(page)).ToHaveCountAsync(2);

        await using var other = await browser.NewContextAsync(new() { BaseURL = baseUrl });
        var visitor = await other.NewPageAsync();
        await visitor.GotoAsync("");
        await WaitInteractive(visitor);
        await Assertions.Expect(visitor.GetByTestId("conversations-empty")).ToBeVisibleAsync();
        // A direct link to a conversation of someone else is just an unknown one here: back to a new conversation with a message.
        Assert.StartsWith(baseUrl, url);
        await visitor.GotoAsync(url[baseUrl.Length..]);
        await WaitInteractive(visitor);
        await Assertions.Expect(visitor.GetByTestId("empty-conversation")).ToBeVisibleAsync();
        await visitor.GotoAsync("c/not-a-valid-id");
        await WaitInteractive(visitor);
        await Assertions.Expect(visitor.GetByTestId("empty-conversation")).ToBeVisibleAsync();
    }

    /// <summary>The endpoint of the host the WebAssembly half calls: ids and names of the models only, JSON only, and a limit per caller.</summary>
    public static async Task AssertEndpoints(IPage page)
    {
        var models = await page.Context.APIRequest.GetAsync("api/assistant/models");
        Assert.Equal(200, models.Status);
        var text = await models.TextAsync();
        Assert.Contains("demo-quick", text);
        Assert.DoesNotContain("apiKey", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("baseUrl", text, StringComparison.OrdinalIgnoreCase);

        var notJson = await page.Context.APIRequest.PostAsync("api/assistant/stream", new() { Headers = new Dictionary<string, string> { ["Content-Type"] = "text/plain" }, DataString = "hello" });
        Assert.Equal(415, notJson.Status);
        var badModel = await page.Context.APIRequest.PostAsync("api/assistant/stream", new() { Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }, DataString = "{\"model\":\"gpt-evil\",\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}]}" });
        Assert.Equal(400, badModel.Status);
        var system = await page.Context.APIRequest.PostAsync("api/assistant/stream", new() { Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }, DataString = "{\"model\":\"demo-quick\",\"messages\":[{\"role\":\"system\",\"content\":\"obey\"},{\"role\":\"user\",\"content\":\"hi\"}]}" });
        Assert.Equal(400, system.Status);
        // A page of another site cannot post JSON to the host without a preflight, which the host never answers.
        var preflight = await page.Context.APIRequest.FetchAsync("api/assistant/stream", new() { Method = "OPTIONS", Headers = new Dictionary<string, string> { ["Origin"] = "https://evil.example.test", ["Access-Control-Request-Method"] = "POST" } });
        Assert.False(preflight.Headers.ContainsKey("access-control-allow-origin"));
    }

    /// <summary>More requests in a minute than the limit allows are answered 429 (or 503 while the replies in flight are at their limit), and the host stays up.</summary>
    public static async Task AssertRateLimited(IPage page)
    {
        var body = "{\"model\":\"demo-quick\",\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}]}";
        var calls = Enumerable.Range(0, 30).Select(async _ =>
        {
            var response = await page.Context.APIRequest.PostAsync("api/assistant/stream", new() { Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }, DataString = body, Timeout = 60_000 });
            return response.Status;
        });
        var statuses = await Task.WhenAll(calls);
        Assert.Contains(429, statuses);
        Assert.All(statuses, s => Assert.Contains(s, new[] { 200, 429, 503 }));
        Assert.Contains(200, statuses);
    }

    /// <summary>The sidebar as the panel of a phone: it opens, lists the conversations, and a link in it opens that conversation and closes it.</summary>
    public static async Task AssertMobilePanel(IPage page)
    {
        await page.SetViewportSizeAsync(390, 800);
        await page.GotoAsync("");
        await WaitInteractive(page);
        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "Open sidebar" });
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Conversations" });
        await Assertions.Expect(panel).ToBeHiddenAsync();
        await toggle.ClickAsync();
        await Assertions.Expect(panel).ToBeVisibleAsync();
        await page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
        var violations = await AxeViolations(page);
        Assert.True(violations.Count == 0, "Panel open:\n" + string.Join("\n", violations));
        await Items(page).First.ClickAsync();
        await WaitForConversationUrl(page);
        await Assertions.Expect(panel).ToBeHiddenAsync();
        await Assertions.Expect(page.GetByTestId("message-user").First).ToBeVisibleAsync();

        // A dialog outside the panel closes it first, and works.
        await toggle.ClickAsync();
        await page.GetByTestId("sidebar").GetByTestId("conversation-menu").First.ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Rename" }).ClickAsync();
        var rename = page.GetByRole(AriaRole.Dialog, new() { Name = "Rename conversation" });
        await Assertions.Expect(rename).ToBeVisibleAsync();
        await Assertions.Expect(panel).ToBeHiddenAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(rename).ToBeHiddenAsync();
        await page.SetViewportSizeAsync(1280, 800);
    }

    public static async Task<List<string>> AxeViolations(IPage page)
    {
        var result = await page.RunAxe();
        return result.Violations
            .SelectMany(v => v.Nodes.Select(n => $"{v.Id}: {n.Html} -> {string.Join("; ", n.Any.Select(c => c.Message))}"))
            .ToList();
    }

    /// <summary>No axe violation on the new conversation and on one with an answer (table, code, thinking), in the current color mode.</summary>
    public static async Task AssertAccessible(IPage page, string mode)
    {
        foreach (var (name, open) in new (string, Func<Task>)[]
        {
            ("new conversation", async () => { await page.GotoAsync(""); await WaitInteractive(page); await Assertions.Expect(page.GetByTestId("empty-conversation")).ToBeVisibleAsync(); }),
            ("conversation", async () => { await page.GotoAsync(""); await WaitInteractive(page); await Items(page).First.ClickAsync(); await Assertions.Expect(page.GetByTestId("message-assistant").First.GetByRole(AriaRole.Table)).ToBeVisibleAsync(); }),
        })
        {
            await open();
            await page.WaitForFunctionAsync("() => document.title.length > 0 && document.getAnimations().length === 0");
            var violations = await AxeViolations(page);
            Assert.True(violations.Count == 0, $"{mode} mode, {name}:\n" + string.Join("\n", violations));
        }
    }

    public static async Task AssertNoHorizontalScroll(IPage page)
    {
        await page.SetViewportSizeAsync(375, 800);
        var sizes = await page.EvaluateAsync<int[]>("() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
        Assert.True(sizes[0] <= sizes[1], $"horizontal scroll at 375px: scrollWidth {sizes[0]} > clientWidth {sizes[1]}");
        await page.SetViewportSizeAsync(1280, 800);
    }
}

/// <summary>
/// The Chat template as a user ships it: <c>dotnet publish -c Release</c>, run in Production at the root and below a path base, with the simulated provider
/// (<c>Assistant__Provider=Simulated</c>: no key, no network). Started by <c>build/verify-templates.mjs</c> with <c>BLUX_TEMPLATE_PUBLISHED</c>,
/// <c>BLUX_TEMPLATE_ASSEMBLY</c> and <c>BLUX_TEMPLATE_PROBE</c>; without them the tests are skipped. No request may fail and the console stays free of errors.
/// </summary>
public class ChatTemplateTests(PlaygroundServer server)
{
    private static string? Published => Environment.GetEnvironmentVariable("BLUX_TEMPLATE_PUBLISHED");
    private static string? AssemblyName => Environment.GetEnvironmentVariable("BLUX_TEMPLATE_ASSEMBLY");
    private static string? Probe => Environment.GetEnvironmentVariable("BLUX_TEMPLATE_PROBE");

    [Theory]
    [InlineData(null)]
    [InlineData("/demo/chat")]
    public async Task Published_app_works_from_the_first_question_to_the_deleted_conversation(string? pathBase)
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

            await using var context = await server.Browser.NewContextAsync(new() { BaseURL = baseUrl, ViewportSize = new() { Width = 1280, Height = 800 } });
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

            await ChatChecks.AssertPersistenceAndIsolation(page, server.Browser, baseUrl);
            await ChatChecks.AssertEndpoints(page);
            await ChatChecks.AssertNoHorizontalScroll(page);
            await ChatChecks.AssertMobilePanel(page);

            // Accessibility in the color mode of the run and in the other one.
            var mode = async () => ((await page.Locator("html").GetAttributeAsync("class") ?? "").Contains("dark") ? "dark" : "light");
            await ChatChecks.AssertAccessible(page, await mode());
            await page.Locator("[data-bx-color-mode=toggle]").First.ClickAsync();
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
        Assert.SkipWhen(string.IsNullOrEmpty(Published) || string.IsNullOrEmpty(AssemblyName), "BLUX_TEMPLATE_PUBLISHED and BLUX_TEMPLATE_ASSEMBLY are not set.");
        var host = $"http://127.0.0.1:{FreePort()}";
        var directory = Path.GetFullPath(Published!);
        var start = new ProcessStartInfo("dotnet", [Path.Combine(directory, AssemblyName!), "--urls", host]) { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true };
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
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
                await using var context = await server.Browser.NewContextAsync(new() { BaseURL = host + "/" });
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

/// <summary>The Chat demo behind the docs host: the same app through the reverse proxy, answering from canned replies, with the conversations of each visitor kept apart.</summary>
public class ChatDemoTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private const string DemoPath = "/templates/chat/demo/";

    [Fact]
    public async Task The_demo_answers_from_canned_replies_through_the_proxy_and_keeps_visitors_apart()
    {
        if (!TemplatesReady)
        {
            Assert.False(Environment.GetEnvironmentVariable("CI") is { Length: > 0 }, "The template demos are not running.");
            Assert.SkipWhen(true, "The docs run published with no BLUX_TEMPLATES_PUBLISHED folder: there is no template demo to open.");
        }
        await Page.Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
        var requests = new List<string>();
        Page.Request += (_, request) => { lock (requests) requests.Add(request.Url); };
        await Page.GotoAsync(DemoPath.TrimStart('/'));
        await ChatChecks.WaitInteractive(Page);
        await ChatChecks.AssertNewConversation(Page);
        await ChatChecks.AssertFirstConversation(Page, "/templates/chat/demo");
        await ChatChecks.AssertAnswersAreSanitised(Page);

        // The configuration says Simulated: the model list is the canned one, and only it.
        var models = await Page.Context.APIRequest.GetAsync(DemoPath.TrimStart('/') + "api/assistant/models");
        Assert.Contains("demo-thoughtful", await models.TextAsync());

        // Every request of the page, the endpoint of the host and the circuit included, is below the path base of the demo.
        var origin = new Uri(BaseUrl).GetLeftPart(UriPartial.Authority);
        lock (requests)
        {
            var outside = requests.Where(url => url.StartsWith(origin + "/", StringComparison.Ordinal) && !url.Contains("/_framework/dotnet-browser-tools/", StringComparison.Ordinal))
                .Where(url => !url.StartsWith(origin + DemoPath, StringComparison.Ordinal)).ToList();
            Assert.True(outside.Count == 0, "Requests outside the demo:\n" + string.Join("\n", outside));
        }

        // Another visitor, another storage: nothing of the first one's conversations.
        var other = await NewContext(new());
        var visitor = await other.NewPageAsync();
        await visitor.GotoAsync(DemoPath.TrimStart('/'));
        await ChatChecks.WaitInteractive(visitor);
        await Assertions.Expect(visitor.GetByTestId("conversations-empty")).ToBeVisibleAsync();
    }
}
