using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;

namespace Blux.UI.E2E;

/// <summary>What the Editor template does in a browser, shared by the run of the published app and the run of its demo behind the docs host.</summary>
internal static class EditorChecks
{
    private static readonly Regex Interactive = new("^(Server|WebAssembly)$");

    // A one pixel PNG for every image address of the tests: nothing goes out on the network, and nothing is reported as a failed request.
    private static readonly byte[] Pixel = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGNgYGD4DwABBAEAHnOcQAAAAABJRU5ErkJggg==");

    public static Task WaitInteractive(IPage page) =>
        Assertions.Expect(page.GetByTestId("renderer")).ToHaveTextAsync(Interactive, new() { Timeout = 60_000 });

    /// <summary>Polls the address in the page (see PlaygroundTest.WaitForUrl): WaitForURLAsync can miss a navigation of the interactive router.</summary>
    public static Task WaitForUrl(IPage page, string expression) =>
        page.WaitForFunctionAsync($"() => {{ const url = new URL(location.href); return {expression}; }}", null, new() { Timeout = 15_000 });

    public static Task WaitForDocumentUrl(IPage page) => WaitForUrl(page, "/\\/d\\/[0-9a-f]{32}$/.test(url.pathname)");

    public static Task AnswerImages(IPage page) =>
        page.RouteAsync(new Regex(@"^https?://images\.example\.test/"), route => route.FulfillAsync(new() { ContentType = "image/png", BodyBytes = Pixel }));

    public static ILocator Surface(IPage page) => page.GetByTestId("editor").Locator(".ProseMirror");

    /// <summary>The h1 of the navbar: the title of the document (the text of a document can have headings of its own, h1 included).</summary>
    public static ILocator NavTitle(IPage page) => page.Locator("header").GetByRole(AriaRole.Heading, new() { Level = 1 });

    public static ILocator Items(IPage page) => page.GetByTestId("sidebar").GetByTestId("document-item");

    public static Task WaitEditor(IPage page) =>
        page.WaitForFunctionAsync("() => { const e = document.querySelector('bx-editor'); return !!e && e.hasAttribute('data-ready') && !!e.querySelector('.ProseMirror'); }", null, new() { Timeout = 30_000 });

    /// <summary>Waits until the status bar says the last change is in the storage.</summary>
    public static Task WaitSaved(IPage page) =>
        Assertions.Expect(page.GetByTestId("save-state")).ToHaveTextAsync("Saved", new() { Timeout = 15_000 });

    /// <summary>The key prefix of the documents of the app in the local storage: scoped to the path base.</summary>
    public static string Prefix(string? pathBase) =>
        string.IsNullOrEmpty(pathBase) ? "documents" : "documents." + string.Join(".", pathBase.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(s => Regex.Replace(s, "[^A-Za-z0-9_-]", "~"))) + "~~";

    public static Task<string[]> StoredValues(IPage page, string pathBase) =>
        page.EvaluateAsync<string[]>("(prefix) => Object.keys(localStorage).filter((k) => k.startsWith(prefix + ':d:')).map((k) => localStorage.getItem(k))", Prefix(pathBase));

    /// <summary>Waits until a value of the storage of the app holds the text: the editor reports a change after a pause, so "Saved" on the screen can still be the state before it.</summary>
    public static Task WaitStored(IPage page, string? pathBase, string text) =>
        page.WaitForFunctionAsync("([prefix, text]) => Object.keys(localStorage).some((k) => k.startsWith(prefix + ':d:') && localStorage.getItem(k).includes(text))",
            new object[] { Prefix(pathBase), text }, new() { Timeout = 15_000 });

    /// <summary>Creates a document from the button of the sidebar and waits for the editor.</summary>
    public static async Task NewDocument(IPage page)
    {
        await page.GetByTestId("new-document").ClickAsync();
        await WaitForDocumentUrl(page);
        await WaitEditor(page);
    }

    /// <summary>The home page names what to do, and the list has nothing in it.</summary>
    public static async Task AssertEmptyHome(IPage page)
    {
        await Assertions.Expect(page.GetByTestId("empty-state")).ToBeVisibleAsync();
        await Assertions.Expect(NavTitle(page)).ToHaveTextAsync("Editor");
        await Assertions.Expect(page.GetByTestId("documents-empty")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("home-new")).ToBeEnabledAsync();
    }

    /// <summary>
    /// A new document: typing is saved on its own, the title follows the first line, the word count and the list follow, and the text is in the storage of this browser
    /// under keys scoped to the path base, and nowhere else.
    /// </summary>
    public static async Task AssertWritingAndAutosave(IPage page, string? pathBase)
    {
        await NewDocument(page);
        await Assertions.Expect(NavTitle(page)).ToHaveTextAsync("Untitled");
        await Assertions.Expect(page.GetByTestId("word-count")).ToHaveTextAsync("0 words, 0 characters");
        await Assertions.Expect(Items(page)).ToHaveCountAsync(1);

        await Surface(page).ClickAsync();
        await page.Keyboard.TypeAsync("Trip plan");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync("Book the train and pack light.");
        await WaitSaved(page);
        await Assertions.Expect(page.GetByTestId("word-count")).ToHaveTextAsync("8 words, 39 characters");
        await Assertions.Expect(NavTitle(page)).ToHaveTextAsync("Trip plan");
        await Assertions.Expect(Items(page).First).ToHaveTextAsync("Trip plan");
        await Assertions.Expect(Items(page).First).ToHaveAttributeAsync("aria-current", "page");
        await Assertions.Expect(page.GetByTestId("sidebar").GetByText("Today", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page).ToHaveTitleAsync(new Regex(@"Trip plan · Editor"));

        var keys = await page.EvaluateAsync<string[]>("() => Object.keys(localStorage)");
        Assert.Contains(keys, key => key.StartsWith(Prefix(pathBase) + ":d:", StringComparison.Ordinal));
        var stored = await StoredValues(page, pathBase ?? "");
        Assert.Single(stored);
        Assert.Contains("Book the train", stored[0]);
    }

    /// <summary>The toolbar formats, the floating menu follows a selection, the slash menu inserts blocks and a task can be checked.</summary>
    public static async Task AssertFormatting(IPage page)
    {
        var toolbar = page.GetByRole(AriaRole.Toolbar, new() { Name = "Formatting" });
        await Surface(page).ClickAsync();
        await page.Keyboard.PressAsync("Control+End");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync("emphasis");
        await page.Keyboard.PressAsync("Shift+Home");

        // The floating menu shows over a selection and formats it.
        var bubble = page.GetByRole(AriaRole.Toolbar, new() { Name = "Format the selection" });
        await Assertions.Expect(bubble).ToBeVisibleAsync();
        await bubble.GetByRole(AriaRole.Button, new() { Name = "Bold" }).ClickAsync();
        await Assertions.Expect(Surface(page).Locator("strong")).ToHaveTextAsync("emphasis");
        // The toolbar above the text shows the state of the selection.
        await Assertions.Expect(toolbar.GetByRole(AriaRole.Button, new() { Name = "Bold" })).ToHaveAttributeAsync("aria-pressed", "true");
        await toolbar.GetByRole(AriaRole.Button, new() { Name = "Underline" }).ClickAsync();
        await Assertions.Expect(Surface(page).Locator("u")).ToHaveTextAsync("emphasis");

        // The slash menu: a heading, then a task list.
        await Surface(page).ClickAsync();
        await page.Keyboard.PressAsync("Control+End");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync("/h2");
        await Assertions.Expect(page.GetByRole(AriaRole.Option, new() { Name = "Heading 2" })).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync("Next steps");
        await Assertions.Expect(Surface(page).Locator("h2")).ToHaveTextAsync("Next steps");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync("/task");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync("Buy tickets");
        var task = Surface(page).Locator("ul[data-type=taskList] li[data-checked]");
        await Assertions.Expect(task).ToHaveCountAsync(1);
        await Assertions.Expect(task).ToHaveAttributeAsync("data-checked", "false");
        await task.GetByRole(AriaRole.Checkbox).CheckAsync();
        await Assertions.Expect(task).ToHaveAttributeAsync("data-checked", "true");
        await Assertions.Expect(task.GetByRole(AriaRole.Checkbox)).ToHaveAccessibleNameAsync("Task: Buy tickets");

        // Lists, a quote and a code block from the toolbar buttons.
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("Enter");
        await toolbar.GetByRole(AriaRole.Button, new() { Name = "Bulleted list" }).ClickAsync();
        await page.Keyboard.TypeAsync("first");
        await Assertions.Expect(Surface(page).Locator("ul:not([data-type]) li")).ToHaveTextAsync("first");
        await WaitSaved(page);
    }

    /// <summary>A link takes http, https and mailto addresses and refuses the others; an image takes an address in the form of the toolbar.</summary>
    public static async Task AssertLinksAndImages(IPage page, string? pathBase)
    {
        await AnswerImages(page);
        var toolbar = page.GetByRole(AriaRole.Toolbar, new() { Name = "Formatting" });
        await Surface(page).ClickAsync();
        await page.Keyboard.PressAsync("Control+End");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync("a safe link");
        await page.Keyboard.PressAsync("Shift+Home");
        await toolbar.GetByRole(AriaRole.Button, new() { Name = "Link" }).ClickAsync();
        var address = toolbar.GetByRole(AriaRole.Textbox, new() { Name = "Link address" });
        await address.FillAsync("https://example.com/docs");
        await address.PressAsync("Enter");
        await Assertions.Expect(Surface(page).Locator("a[href='https://example.com/docs']")).ToHaveTextAsync("a safe link");
        await Assertions.Expect(Surface(page).Locator("a")).ToHaveAttributeAsync("rel", "noopener noreferrer nofollow");

        // An address that is not allowed is not made a link, by the editor or by what is stored.
        await Surface(page).ClickAsync();
        await page.Keyboard.PressAsync("Control+End");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync("a bad link");
        await page.Keyboard.PressAsync("Shift+Home");
        await toolbar.GetByRole(AriaRole.Button, new() { Name = "Link" }).ClickAsync();
        await address.FillAsync("javascript:window.__xss = 1");
        await address.PressAsync("Enter");
        await Assertions.Expect(Surface(page).Locator("a[href^=javascript]")).ToHaveCountAsync(0);

        // An image by address: https is inserted with its alt text taken from the file name, http is kept out of what is stored.
        await Surface(page).ClickAsync();
        await page.Keyboard.PressAsync("Control+End");
        await page.Keyboard.PressAsync("Enter");
        await toolbar.GetByRole(AriaRole.Button, new() { Name = "Image" }).ClickAsync();
        var image = toolbar.GetByRole(AriaRole.Textbox, new() { Name = "Image address" });
        await image.FillAsync("not an address");
        await image.PressAsync("Enter");
        await Assertions.Expect(image).ToHaveAttributeAsync("aria-invalid", "true");
        await image.FillAsync("https://images.example.test/team-photo.png");
        await image.PressAsync("Enter");
        var picture = Surface(page).Locator("img[src='https://images.example.test/team-photo.png']");
        await Assertions.Expect(picture).ToHaveAttributeAsync("alt", "team photo");
        await Assertions.Expect(picture).ToHaveAttributeAsync("referrerpolicy", "no-referrer");
        await WaitStored(page, pathBase, "team-photo.png");

        await Surface(page).ClickAsync();
        await page.Keyboard.PressAsync("Control+End");
        await page.Keyboard.PressAsync("Enter");
        await toolbar.GetByRole(AriaRole.Button, new() { Name = "Image" }).ClickAsync();
        await image.FillAsync("http://images.example.test/insecure.png");
        await image.PressAsync("Enter");
        await Assertions.Expect(Surface(page).Locator("img")).ToHaveCountAsync(2);
        await Surface(page).ClickAsync();
        await page.Keyboard.PressAsync("Control+End");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.TypeAsync("after the images");
        await WaitStored(page, pathBase, "after the images");
        var stored = (await StoredValues(page, pathBase ?? "")).Single(value => value.Contains("after the images", StringComparison.Ordinal));
        Assert.Contains("https://images.example.test/team-photo.png", stored);
        Assert.DoesNotContain("insecure.png", stored);
        Assert.DoesNotContain("javascript", stored);
        Assert.Contains("https://example.com/docs", stored);
    }

    /// <summary>Pasted HTML and tampered storage are rebuilt from the allow-list: nothing runs, nothing outside it is shown or kept.</summary>
    public static async Task AssertContentIsSanitised(IPage page, string? pathBase)
    {
        await NewDocument(page);
        await Surface(page).ClickAsync();
        // Pasting HTML takes what the editor knows (text, bold, a link) and nothing else.
        await page.EvaluateAsync(@"() => {
            const data = new DataTransfer();
            data.setData('text/html', '<p>pasted <b>bold</b> <a href=""javascript:window.__xss=1"">bad</a> <a href=""https://example.com/ok"">ok</a></p><img src=x onerror=""window.__xss=2""><script>window.__xss=3</script><iframe src=""https://evil.example.test""></iframe>');
            data.setData('text/plain', 'pasted bold bad ok');
            document.querySelector('.ProseMirror').dispatchEvent(new ClipboardEvent('paste', { clipboardData: data, bubbles: true, cancelable: true }));
        }");
        await Assertions.Expect(Surface(page)).ToContainTextAsync("pasted bold bad ok");
        Assert.Equal(0, await Surface(page).Locator("script, iframe, img[onerror], a[href^=javascript], [onerror]").CountAsync());
        await Assertions.Expect(Surface(page).Locator("a[href='https://example.com/ok']")).ToHaveCountAsync(1);
        await WaitStored(page, pathBase, "example.com/ok");
        var stored = (await StoredValues(page, pathBase ?? "")).Single(value => value.Contains("example.com/ok", StringComparison.Ordinal));
        Assert.DoesNotContain("javascript", stored);
        Assert.DoesNotContain("onerror", stored);
        Assert.DoesNotContain("<script", stored);
        Assert.DoesNotContain("<iframe", stored);

        // A document written by hand into the storage (or by an older version) is read through the same allow-list.
        var id = "abcdef0123456789abcdef0123456789";
        var oversized = "0123456789abcdef0123456789abcdee";
        await page.EvaluateAsync(@"([prefix, id, oversized]) => {
            const html = '<h1>Tampered</h1><p onclick=""window.__xss=4"">text</p><img src=x onerror=""window.__xss=5""><script>window.__xss=6</script><a href=""javascript:window.__xss=7"">l</a><img src=""http://insecure.test/a.png"">';
            localStorage.setItem(prefix + ':d:' + id, JSON.stringify({ id, title: 'Tampered', html, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() }));
            localStorage.setItem(prefix + ':d:' + oversized, JSON.stringify({ id: oversized, title: 'Big', html: '<p>' + 'a'.repeat(2000000) + '</p>' }));
            localStorage.setItem(prefix + ':d:not-an-id', 'garbage');
        }", new[] { Prefix(pathBase), id, oversized });
        await page.GotoAsync($"d/{id}");
        await WaitInteractive(page);
        await WaitEditor(page);
        await Assertions.Expect(Surface(page).Locator("h1")).ToHaveTextAsync("Tampered");
        Assert.Equal(0, await page.GetByTestId("editor-area").Locator("script, iframe, [onerror], [onclick], a[href^=javascript], img[src^='http:']").CountAsync());
        Assert.Null(await page.EvaluateAsync<object?>("() => window.__xss ?? null"));
        // What is not a document, or is too large to be one, is removed from the storage when the app reads it.
        var keys = await page.EvaluateAsync<string[]>("(prefix) => Object.keys(localStorage).filter((k) => k.startsWith(prefix + ':d:'))", Prefix(pathBase));
        Assert.DoesNotContain(keys, key => key.EndsWith(oversized, StringComparison.Ordinal) || key.EndsWith("not-an-id", StringComparison.Ordinal));
        Assert.Contains(keys, key => key.EndsWith(id, StringComparison.Ordinal));
    }

    /// <summary>Search, rename and delete from the list; a deleted document leaves the page it was on.</summary>
    public static async Task AssertListActions(IPage page)
    {
        await page.GotoAsync("");
        await WaitInteractive(page);
        await Assertions.Expect(Items(page).First).ToBeVisibleAsync();
        var count = await Items(page).CountAsync();
        Assert.True(count >= 2, "the tests before this one left at least two documents");

        var search = page.GetByRole(AriaRole.Textbox, new() { Name = "Search documents" });
        await search.FillAsync("train");
        await Assertions.Expect(Items(page)).ToHaveCountAsync(1);
        await Assertions.Expect(Items(page).First).ToContainTextAsync("Trip plan");
        await search.FillAsync("no-such-text-anywhere");
        await Assertions.Expect(page.GetByTestId("documents-none")).ToBeVisibleAsync();
        await search.FillAsync("");
        await Assertions.Expect(Items(page)).ToHaveCountAsync(count);

        // Rename from the menu of a row: the title stops following the text.
        await Items(page).Filter(new() { HasText = "Trip plan" }).ClickAsync();
        await WaitForDocumentUrl(page);
        await WaitEditor(page);
        await page.GetByTestId("sidebar").GetByRole(AriaRole.Button, new() { Name = "Options for Trip plan" }).ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Rename" }).ClickAsync();
        var rename = page.GetByRole(AriaRole.Dialog, new() { Name = "Rename document" });
        await Assertions.Expect(rename).ToBeVisibleAsync();
        await rename.GetByRole(AriaRole.Textbox, new() { Name = "Title" }).FillAsync("Holiday notes");
        await rename.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await Assertions.Expect(rename).ToBeHiddenAsync();
        await Assertions.Expect(NavTitle(page)).ToHaveTextAsync("Holiday notes");
        await Surface(page).ClickAsync();
        await page.Keyboard.PressAsync("Control+Home");
        await page.Keyboard.TypeAsync("More ");
        await WaitSaved(page);
        await Assertions.Expect(NavTitle(page)).ToHaveTextAsync("Holiday notes");

        // Delete from the navbar: a confirmation, then the home page.
        await page.GetByRole(AriaRole.Button, new() { Name = "Delete document" }).ClickAsync();
        var confirm = page.GetByRole(AriaRole.Dialog, new() { Name = "Delete document?" });
        await Assertions.Expect(confirm).ToContainTextAsync("Holiday notes");
        await confirm.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await Assertions.Expect(confirm).ToBeHiddenAsync();
        Assert.Equal(count, await Items(page).CountAsync());
        await page.GetByRole(AriaRole.Button, new() { Name = "Delete document" }).ClickAsync();
        await confirm.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await WaitForUrl(page, "!/\\/d\\//.test(url.pathname)");
        await Assertions.Expect(page.GetByTestId("empty-state")).ToBeVisibleAsync();
        await Assertions.Expect(Items(page)).ToHaveCountAsync(count - 1);
    }

    /// <summary>A reload brings the documents back, another visitor sees none of them and a link to one is an unknown document.</summary>
    public static async Task AssertPersistenceAndIsolation(IPage page, IBrowser browser, string baseUrl)
    {
        await Items(page).First.ClickAsync();
        await WaitForDocumentUrl(page);
        var url = page.Url;
        var text = await Surface(page).InnerTextAsync();
        await page.ReloadAsync();
        await WaitInteractive(page);
        await WaitEditor(page);
        Assert.Equal(url, page.Url);
        Assert.Equal(text, await Surface(page).InnerTextAsync());

        await using var other = await browser.NewContextAsync(new() { BaseURL = baseUrl });
        var visitor = await other.NewPageAsync();
        await visitor.GotoAsync("");
        await WaitInteractive(visitor);
        await Assertions.Expect(visitor.GetByTestId("documents-empty")).ToBeVisibleAsync();
        Assert.StartsWith(baseUrl, url);
        await visitor.GotoAsync(url[baseUrl.Length..]);
        await WaitInteractive(visitor);
        await Assertions.Expect(visitor.GetByTestId("document-missing")).ToBeVisibleAsync();
        await visitor.GotoAsync("d/not-a-valid-id");
        await WaitInteractive(visitor);
        await Assertions.Expect(visitor.GetByTestId("document-missing")).ToBeVisibleAsync();
    }

    /// <summary>The guide of the home page is a document with every kind of block, and opening it again opens the same one.</summary>
    public static async Task AssertGuide(IPage page)
    {
        await page.GotoAsync("");
        await WaitInteractive(page);
        await page.GetByTestId("home-guide").ClickAsync();
        await WaitForDocumentUrl(page);
        await WaitEditor(page);
        var url = page.Url;
        await Assertions.Expect(NavTitle(page)).ToHaveTextAsync("Getting started");
        await Assertions.Expect(Surface(page).Locator("h1")).ToHaveTextAsync("Getting started");
        await Assertions.Expect(Surface(page).Locator("ul[data-type=taskList] li[data-checked]")).ToHaveCountAsync(3);
        await Assertions.Expect(Surface(page).Locator("blockquote")).ToBeVisibleAsync();
        await Assertions.Expect(Surface(page).Locator("pre code")).ToBeVisibleAsync();
        await page.GotoAsync("");
        await WaitInteractive(page);
        await page.GetByTestId("home-guide").ClickAsync();
        await WaitForDocumentUrl(page);
        Assert.Equal(url, page.Url);
    }

    public static async Task<List<string>> AxeViolations(IPage page)
    {
        var result = await page.RunAxe();
        return result.Violations
            .SelectMany(v => v.Nodes.Select(n => $"{v.Id}: {n.Html} -> {string.Join("; ", n.Any.Select(c => c.Message))}"))
            .ToList();
    }

    /// <summary>No axe violation on the home page and on a document, in the current color mode.</summary>
    public static async Task AssertAccessible(IPage page, string mode)
    {
        foreach (var (name, open) in new (string, Func<Task>)[]
        {
            ("home", async () => { await page.GotoAsync(""); await WaitInteractive(page); await Assertions.Expect(page.GetByTestId("empty-state")).ToBeVisibleAsync(); }),
            ("document", async () => { await page.GotoAsync(""); await WaitInteractive(page); await Items(page).First.ClickAsync(); await WaitEditor(page); }),
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

    /// <summary>On a phone the list is a panel that opens over the page and closes when a document is chosen; the dialogs open from it.</summary>
    public static async Task AssertMobilePanel(IPage page)
    {
        await page.SetViewportSizeAsync(390, 800);
        await page.GotoAsync("");
        await WaitInteractive(page);
        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "Open sidebar" });
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Documents" });
        await Assertions.Expect(panel).ToBeHiddenAsync();
        await toggle.ClickAsync();
        await Assertions.Expect(panel).ToBeVisibleAsync();
        await page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
        var violations = await AxeViolations(page);
        Assert.True(violations.Count == 0, "Panel open:\n" + string.Join("\n", violations));
        await Items(page).First.ClickAsync();
        await WaitForDocumentUrl(page);
        await Assertions.Expect(panel).ToBeHiddenAsync();
        await WaitEditor(page);

        await toggle.ClickAsync();
        await page.GetByTestId("sidebar").GetByTestId("document-menu").First.ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Rename" }).ClickAsync();
        var rename = page.GetByRole(AriaRole.Dialog, new() { Name = "Rename document" });
        await Assertions.Expect(rename).ToBeVisibleAsync();
        await Assertions.Expect(panel).ToBeHiddenAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(rename).ToBeHiddenAsync();
        await page.SetViewportSizeAsync(1280, 800);
    }

    /// <summary>The whole tour, in the order the documents it makes are needed.</summary>
    public static async Task Tour(IPage page, IBrowser browser, string baseUrl, string? pathBase)
    {
        await page.GotoAsync("");
        await WaitInteractive(page);
        await AssertEmptyHome(page);
        await AssertWritingAndAutosave(page, pathBase);
        await AssertFormatting(page);
        await AssertLinksAndImages(page, pathBase);
        await AssertContentIsSanitised(page, pathBase);
        await AssertGuide(page);
        await AssertListActions(page);
        await AssertPersistenceAndIsolation(page, browser, baseUrl);
    }
}

/// <summary>
/// The Editor template as a user ships it: <c>dotnet publish -c Release</c>, run in Production at the root and below a path base. Started by
/// <c>build/verify-templates.mjs</c> with <c>BLUX_TEMPLATE_PUBLISHED</c>, <c>BLUX_TEMPLATE_ASSEMBLY</c> and <c>BLUX_TEMPLATE_PROBE</c>; without them the tests are skipped.
/// No request may fail and the console stays free of errors.
/// </summary>
public class EditorTemplateTests(PlaygroundServer server)
{
    private static string? Published => Environment.GetEnvironmentVariable("BLUX_TEMPLATE_PUBLISHED");
    private static string? AssemblyName => Environment.GetEnvironmentVariable("BLUX_TEMPLATE_ASSEMBLY");
    private static string? Probe => Environment.GetEnvironmentVariable("BLUX_TEMPLATE_PROBE");

    [Theory]
    [InlineData(null)]
    [InlineData("/demo/editor")]
    public async Task Published_app_works_from_the_first_document_to_the_deleted_one(string? pathBase)
    {
        Assert.SkipWhen(string.IsNullOrEmpty(Published) || string.IsNullOrEmpty(AssemblyName), "BLUX_TEMPLATE_PUBLISHED and BLUX_TEMPLATE_ASSEMBLY are not set.");

        var host = $"http://127.0.0.1:{FreePort()}";
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

            await using var context = await server.Browser.NewContextAsync(new() { BaseURL = baseUrl, ViewportSize = new() { Width = 1280, Height = 800 } });
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
            await EditorChecks.Tour(page, server.Browser, baseUrl, pathBase);

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

            await EditorChecks.AssertNoHorizontalScroll(page);
            await EditorChecks.AssertMobilePanel(page);

            // Accessibility in the color mode of the run and in the other one.
            var mode = async () => ((await page.Locator("html").GetAttributeAsync("class") ?? "").Contains("dark") ? "dark" : "light");
            await EditorChecks.AssertAccessible(page, await mode());
            await page.Locator("[data-bx-color-mode=toggle]").First.ClickAsync();
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

/// <summary>The Editor demo behind the docs host: the same app through the reverse proxy, with the documents of each visitor kept apart.</summary>
public class EditorDemoTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private const string DemoPath = "/templates/editor/demo/";

    [Fact]
    public async Task The_demo_works_through_the_proxy_and_keeps_visitors_apart()
    {
        if (!TemplatesReady)
        {
            Assert.False(Environment.GetEnvironmentVariable("CI") is { Length: > 0 }, "The template demos are not running.");
            Assert.SkipWhen(true, "The docs run published with no BLUX_TEMPLATES_PUBLISHED folder: there is no template demo to open.");
        }
        var requests = new List<string>();
        Page.Request += (_, request) => { lock (requests) requests.Add(request.Url); };
        await Page.GotoAsync(DemoPath.TrimStart('/'));
        await EditorChecks.WaitInteractive(Page);
        await EditorChecks.AssertEmptyHome(Page);
        await EditorChecks.AssertWritingAndAutosave(Page, "/templates/editor/demo");
        await EditorChecks.AssertContentIsSanitised(Page, "/templates/editor/demo");

        var origin = new Uri(BaseUrl).GetLeftPart(UriPartial.Authority);
        lock (requests)
        {
            var outside = requests.Where(url => url.StartsWith(origin + "/", StringComparison.Ordinal) && !url.Contains("/_framework/dotnet-browser-tools/", StringComparison.Ordinal))
                .Where(url => !url.StartsWith(origin + DemoPath, StringComparison.Ordinal)).ToList();
            Assert.True(outside.Count == 0, "Requests outside the demo:\n" + string.Join("\n", outside));
        }

        // Another visitor, another storage: nothing of the first one's documents.
        var other = await NewContext(new());
        var visitor = await other.NewPageAsync();
        await visitor.GotoAsync(DemoPath.TrimStart('/'));
        await EditorChecks.WaitInteractive(visitor);
        await Assertions.Expect(visitor.GetByTestId("documents-empty")).ToBeVisibleAsync();
    }
}
