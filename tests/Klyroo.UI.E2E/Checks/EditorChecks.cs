using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;

namespace Klyroo.UI.E2E;

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
        page.WaitForFunctionAsync("() => { const e = document.querySelector('kly-editor'); return !!e && e.hasAttribute('data-ready') && !!e.querySelector('.ProseMirror'); }", null, new() { Timeout = 30_000 });

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
        // What the allow-list would not keep is refused where it is typed, with the reason, and not accepted to vanish after a reload: relative addresses, bare domains, tel: and ftp:.
        var linkMessage = toolbar.Locator("[data-slot=link-form] [data-slot=form-message]");
        foreach (var refused in new[] { "javascript:window.__xss = 1", "/relative/page", "example.com", "tel:+123456", "ftp://example.com/file", "https://exa mple.com/a b", "https://user:secret@example.com/" })
        {
            await address.FillAsync(refused);
            await address.PressAsync("Enter");
            await Assertions.Expect(address).ToHaveAttributeAsync("aria-invalid", "true");
            await Assertions.Expect(linkMessage).ToHaveTextAsync("Only http, https, mailto addresses are accepted.");
            await Assertions.Expect(Surface(page).Locator("a")).ToHaveCountAsync(1);
        }
        await address.PressAsync("Escape");
        await Assertions.Expect(address).ToBeHiddenAsync();

        // An image by address: https is inserted with its alt text taken from the file name; http is refused here, because it is not what is stored.
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
        // The image field is invalid, described by a message that says which addresses are accepted, and the form stays open; nothing was inserted.
        await Assertions.Expect(image).ToHaveAttributeAsync("aria-invalid", "true");
        var imageMessage = toolbar.Locator("[data-slot=image-form] [data-slot=form-message]");
        await Assertions.Expect(imageMessage).ToHaveTextAsync("Only https addresses are accepted.");
        Assert.Equal("status", await imageMessage.GetAttributeAsync("role"));
        await Assertions.Expect(image).ToHaveAttributeAsync("aria-describedby", (await imageMessage.GetAttributeAsync("id"))!);
        await Assertions.Expect(Surface(page).Locator("img")).ToHaveCountAsync(1);
        await image.PressAsync("Escape");
        await Assertions.Expect(image).ToBeHiddenAsync();
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
        // Relative to the <base> of the app, which is not the base of the test context when the app runs behind the docs host.
        await page.GotoAsync(await page.Locator("base").GetAttributeAsync("href") + $"d/{id}");
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

    /// <summary>A browser that never gets the WebAssembly runtime: the page stays on the Server renderer, the one whose circuit refuses messages past its bound.</summary>
    private static async Task<IBrowserContext> ServerOnlyContext(IBrowser browser, string baseUrl)
    {
        var context = await browser.NewContextAsync(new() { BaseURL = baseUrl, ViewportSize = new() { Width = 1280, Height = 800 } });
        // The download never ends. Refusing it would be the same for the renderer, but a Development build answers a failed download with its error banner, which
        // sits over the buttons the tests click.
        await context.RouteAsync(new Regex(@"\.wasm(\?|$)"), _ => Task.CompletedTask);
        return context;
    }

    private static Task WaitServer(IPage page) =>
        Assertions.Expect(page.GetByTestId("renderer")).ToHaveTextAsync("Server", new() { Timeout = 60_000 });

    /// <summary>Puts documents of about <paramref name="characters"/> characters straight into the storage, the way an older visit would have left them. The newest is number 0.</summary>
    private static Task Seed(IPage page, string? pathBase, int count, int characters, string? junkKey = null) =>
        page.EvaluateAsync(@"([prefix, count, characters, junkKey]) => {
            const hex = () => Array.from(crypto.getRandomValues(new Uint8Array(16)), (b) => b.toString(16).padStart(2, '0')).join('');
            const now = Date.now();
            for (let i = 0; i < count; i++) {
                const id = hex();
                const unit = 'seedword' + i + ' lorem ipsum ';
                const html = '<h1>Seed ' + i + '</h1><p>' + unit.repeat(Math.ceil(characters / unit.length)) + '</p>';
                const when = new Date(now - i * 60000).toISOString();
                localStorage.setItem(prefix + ':d:' + id, JSON.stringify({ id, title: 'Seed ' + i, autoTitle: false, html, createdAt: when, updatedAt: when }));
            }
            if (junkKey) localStorage.setItem(prefix + ':d:' + junkKey, 'garbage');
        }", new object?[] { Prefix(pathBase), count, characters, junkKey });

    /// <summary>
    /// What the circuit of the Server renderer carries. It refuses a message over a bound that the host raises to the one the limits of a document call for: a document of 100,000 characters
    /// (far past the 32 KB default) is typed, saved and read again after a reload; a library of ninety documents is listed without being sent whole; one past the limit of characters is never sent.
    /// </summary>
    public static async Task AssertServerCircuitCarriesLargeDocuments(IBrowser browser, string baseUrl, string? pathBase)
    {
        await using var context = await ServerOnlyContext(browser, baseUrl);
        var page = await context.NewPageAsync();
        var frames = new List<int>();
        page.WebSocket += (_, socket) => socket.FrameSent += (_, frame) => { lock (frames) frames.Add(frame.Binary?.Length ?? frame.Text?.Length ?? 0); };
        await page.GotoAsync("");
        await WaitServer(page);

        // Many documents: ninety of 9,000 characters each (about 800 KB together) are listed, searched in the browser and opened one at a time.
        await Seed(page, pathBase, 90, 9000);
        await page.ReloadAsync();
        await WaitServer(page);
        await Assertions.Expect(Items(page)).ToHaveCountAsync(90, new() { Timeout = 20_000 });
        await Assertions.Expect(page.GetByTestId("storage-warning")).ToContainTextAsync("Almost full: 90 of 100 documents");
        var search = page.GetByRole(AriaRole.Textbox, new() { Name = "Search documents" });
        await search.FillAsync("seedword42 lorem");
        await Assertions.Expect(Items(page)).ToHaveCountAsync(1);
        await Assertions.Expect(Items(page).First).ToContainTextAsync("Seed 42");
        await Items(page).First.ClickAsync();
        await WaitForDocumentUrl(page);
        await WaitEditor(page);
        await Assertions.Expect(Surface(page)).ToContainTextAsync("seedword42 lorem ipsum");
        await Assertions.Expect(NavTitle(page)).ToHaveTextAsync("Seed 42");
        await search.FillAsync("");
        await Assertions.Expect(Items(page)).ToHaveCountAsync(90);

        // A large document: typed (inserted at once, like a paste) in a new document, saved, and back after a reload.
        await NewDocument(page);
        var text = string.Concat(Enumerable.Repeat("Zażółć gęślą \"jaźń\" 日本語 — ", 4000));
        Assert.InRange(text.Length, 100_000, 120_000);
        await Surface(page).ClickAsync();
        await page.Keyboard.InsertTextAsync(text);
        await WaitStored(page, pathBase, "jaźń");
        await WaitSaved(page);
        await WaitServer(page);
        var stored = (await StoredValues(page, pathBase ?? "")).Single(value => value.Contains("Zażółć", StringComparison.Ordinal));
        Assert.True(stored.Length > 100_000, $"the stored document is {stored.Length} characters");
        var url = page.Url;
        await page.ReloadAsync();
        await WaitServer(page);
        await WaitEditor(page);
        Assert.Equal(url, page.Url);
        await Assertions.Expect(Surface(page)).ToContainTextAsync("日本語");
        Assert.True((await Surface(page).InnerTextAsync()).Length > 100_000);
        await Assertions.Expect(page.GetByTestId("save-state")).ToHaveTextAsync("Saved");

        // Past the limit of characters the editor sends nothing, says so, and the stored text is the last one that fitted.
        await Surface(page).ClickAsync();
        await page.Keyboard.PressAsync("Control+End");
        lock (frames) frames.Clear();
        await page.Keyboard.InsertTextAsync(string.Concat(Enumerable.Repeat("OVERLIMIT ", 6000)));
        await Assertions.Expect(page.GetByTestId("save-state")).ToContainTextAsync("Not saved: this document is too long", new() { Timeout = 15_000 });
        await Assertions.Expect(page.GetByTestId("save-state")).ToContainTextAsync("of 150,000 characters");
        await Assertions.Expect(page.GetByTestId("editor")).ToHaveAttributeAsync("data-over-limit", "");
        await page.WaitForTimeoutAsync(1000);
        int largest;
        lock (frames) largest = frames.Count == 0 ? 0 : frames.Max();
        Assert.True(largest < 50_000, $"a message of {largest} bytes was sent while the text was over the limit");
        Assert.DoesNotContain(await StoredValues(page, pathBase ?? ""), value => value.Contains("OVERLIMIT", StringComparison.Ordinal));
        await WaitServer(page);
        // Undo takes it back within the limit: the status follows.
        await page.Keyboard.PressAsync("Control+z");
        await Assertions.Expect(page.GetByTestId("save-state")).ToHaveTextAsync("Saved", new() { Timeout = 15_000 });
        await Assertions.Expect(page.GetByTestId("editor")).Not.ToHaveAttributeAsync("data-over-limit", new Regex(".*"));
    }

    /// <summary>What the app does when the bounds are past, and tells: the repair at load, the warning near them, the removal when a new document is made.</summary>
    public static async Task AssertRemovalsAreAnnounced(IBrowser browser, string baseUrl, string? pathBase)
    {
        await using var context = await ServerOnlyContext(browser, baseUrl);
        var page = await context.NewPageAsync();
        await page.GotoAsync("");
        await WaitServer(page);

        // The storage holds 103 documents and something that is not one: reading it removes the 3 least recently used and the junk, and says so.
        await Seed(page, pathBase, 103, 600, junkKey: "not-a-document");
        await page.ReloadAsync();
        await WaitServer(page);
        var repaired = page.GetByText("Storage cleaned up", new() { Exact = true });
        await Assertions.Expect(repaired).ToBeVisibleAsync(new() { Timeout = 20_000 });
        await Assertions.Expect(page.GetByText("3 least recently used documents were removed because the storage held more than the limits allow")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("1 stored entry that was not a readable document was removed")).ToBeVisibleAsync();
        await Assertions.Expect(Items(page)).ToHaveCountAsync(100);
        var keys = await page.EvaluateAsync<string[]>("(prefix) => Object.keys(localStorage).filter((k) => k.startsWith(prefix + ':d:'))", Prefix(pathBase));
        Assert.Equal(100, keys.Length);

        // At the bound the sidebar warns before anything else goes; one more document pushes the oldest out, and a notice says how many.
        await Assertions.Expect(page.GetByTestId("storage-warning")).ToContainTextAsync("Almost full: 100 of 100 documents");
        await page.GetByTestId("new-document").ClickAsync();
        await WaitForDocumentUrl(page);
        await Assertions.Expect(page.GetByText("Older documents were removed", new() { Exact = true })).ToBeVisibleAsync(new() { Timeout = 20_000 });
        await Assertions.Expect(page.GetByText("1 least recently used document was removed: this browser keeps at most 100 documents and 1,000,000 characters in all")).ToBeVisibleAsync();
        await Assertions.Expect(Items(page)).ToHaveCountAsync(100);
        Assert.DoesNotContain(await page.EvaluateAsync<string[]>("(prefix) => Object.keys(localStorage).filter((k) => k.startsWith(prefix + ':d:'))", Prefix(pathBase)), key => key.EndsWith("not-a-document", StringComparison.Ordinal));
    }

    /// <summary>
    /// Two tabs of one browser: a change in one reaches the list and the open document of the other, a document being typed in is never replaced under the cursor, and a document deleted
    /// elsewhere is left with a notice.
    /// </summary>
    public static async Task AssertTwoTabs(IBrowser browser, string baseUrl, string? pathBase)
    {
        await using var context = await ServerOnlyContext(browser, baseUrl);
        var a = await context.NewPageAsync();
        await a.GotoAsync("");
        await WaitServer(a);
        await NewDocument(a);
        await Surface(a).ClickAsync();
        await a.Keyboard.TypeAsync("From A");
        await WaitStored(a, pathBase, "From A");
        await WaitSaved(a);
        var url = a.Url;

        var b = await context.NewPageAsync();
        await b.GotoAsync(url);
        await WaitInteractive(b);
        await WaitEditor(b);
        await Assertions.Expect(Surface(b)).ToContainTextAsync("From A");

        // B writes while A has the cursor in the text: A keeps what is on its screen, says so, and offers the other version.
        await Surface(b).ClickAsync();
        await b.Keyboard.PressAsync("Control+End");
        await b.Keyboard.TypeAsync(" and B");
        await WaitStored(b, pathBase, "and B");
        await Assertions.Expect(a.GetByTestId("external-conflict")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(a.GetByTestId("external-conflict")).ToContainTextAsync("Another tab changed this document");
        await Assertions.Expect(Surface(a)).ToHaveTextAsync("From A");
        await a.GetByTestId("external-reload").ClickAsync();
        await Assertions.Expect(Surface(a)).ToHaveTextAsync("From A and B");
        await Assertions.Expect(a.GetByTestId("external-change")).ToBeVisibleAsync();
        await Assertions.Expect(a.GetByTestId("external-conflict")).ToHaveCountAsync(0);

        // With the cursor out of the text, A takes the change as it comes.
        await a.GetByTestId("word-count").ClickAsync();
        await Surface(b).ClickAsync();
        await b.Keyboard.PressAsync("Control+End");
        await b.Keyboard.TypeAsync(" again");
        await WaitStored(b, pathBase, "and B again");
        await Assertions.Expect(Surface(a)).ToHaveTextAsync("From A and B again", new() { Timeout = 15_000 });
        await Assertions.Expect(a.GetByTestId("external-change")).ToBeVisibleAsync();

        // A document made in B is in the list of A.
        var before = await Items(a).CountAsync();
        await b.GetByTestId("new-document").ClickAsync();
        await WaitForDocumentUrl(b);
        await Assertions.Expect(Items(a)).ToHaveCountAsync(before + 1, new() { Timeout = 15_000 });

        // A document deleted in B is gone from A, which leaves it and says why.
        await b.GotoAsync(url);
        await WaitInteractive(b);
        await WaitEditor(b);
        await b.GetByRole(AriaRole.Button, new() { Name = "Delete document" }).ClickAsync();
        await b.GetByRole(AriaRole.Dialog, new() { Name = "Delete document?" }).GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await WaitForUrl(a, "!/\\/d\\//.test(url.pathname)");
        await Assertions.Expect(a.GetByText("Deleted in another tab", new() { Exact = true })).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(a.GetByText("1 document was deleted in another tab of this browser")).ToBeVisibleAsync();
        await Assertions.Expect(Items(a)).ToHaveCountAsync(before);
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
