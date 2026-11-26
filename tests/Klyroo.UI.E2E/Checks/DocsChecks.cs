using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Klyroo.UI.E2E;

/// <summary>What the Docs template does in a browser, shared by the run of the published app and the run of its demo behind the docs host.</summary>
internal static class DocsChecks
{
    /// <summary>
    /// The home page lists the sections; a page of the content has its navigation with the current page marked, its table of contents whose links
    /// reach their headings, a callout, a table, previous and next links; the search finds a page by a word of its text; the copy button copies.
    /// Everything runs in the browser from HTML (no circuit, no WebAssembly), and every address is relative to the base, so it holds below a path base.
    /// </summary>
    public static async Task AssertBehaviour(IPage page, bool checkClipboard)
    {
        // Home: the intro and one card per section of the content.
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Keep a folder in step");
        await Assertions.Expect(page.GetByTestId("sections").GetByRole(AriaRole.Listitem).Filter(new() { Has = page.GetByRole(AriaRole.Heading, new() { Level = 3 }) })).ToHaveCountAsync(3);
        await page.WaitForFunctionAsync("() => !!customElements.get('kly-content-toc') && !!customElements.get('kly-popover')");

        // A page of the content, by its link on the home page.
        await page.GetByTestId("sections").GetByRole(AriaRole.Link, new() { Name = "Installation", Exact = true }).ClickAsync();
        await page.WaitForUrlMatchingAsync("/getting-started/installation$");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Installation");
        await Assertions.Expect(page).ToHaveTitleAsync("Installation - Tern");

        // Navigation: the tree of the folders, with the current page marked and the other groups closed.
        var nav = page.GetByRole(AriaRole.Navigation, new() { Name = "Documentation", Exact = true });
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Installation", Exact = true })).ToHaveAttributeAsync("aria-current", "page");
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Introduction", Exact = true })).Not.ToHaveAttributeAsync("aria-current", "page");
        await Assertions.Expect(nav.GetByRole(AriaRole.Button, new() { Name = "Getting started" })).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(nav.GetByRole(AriaRole.Button, new() { Name = "Guides" })).ToHaveAttributeAsync("aria-expanded", "false");

        // Table of contents: the h2 and h3, each link reaches the heading with its id and stays on the page.
        var toc = page.GetByRole(AriaRole.Navigation, new() { Name = "On this page" }).Filter(new() { Has = page.Locator("a") }).Locator("visible=true");
        await Assertions.Expect(toc.GetByRole(AriaRole.Link)).ToHaveCountAsync(5);
        var link = toc.GetByRole(AriaRole.Link, new() { Name = "Download the archive" });
        var href = await link.GetAttributeAsync("href");
        Assert.EndsWith("getting-started/installation#download-the-archive", href);
        await link.ClickAsync();
        await page.WaitForUrlMatchingAsync("/getting-started/installation#download-the-archive$");
        var heading = page.Locator("h2#download-the-archive");
        await Assertions.Expect(heading).ToBeInViewportAsync();
        await Assertions.Expect(link).ToHaveAttributeAsync("aria-current", "location");

        // The heading is not under the sticky header.
        var top = await heading.EvaluateAsync<double>("e => e.getBoundingClientRect().top");
        var header = await page.Locator("header").First.EvaluateAsync<double>("e => e.getBoundingClientRect().bottom");
        Assert.True(top >= header - 1, $"the heading starts at {top}px, under the header that ends at {header}px");

        // Callout, table and the highlighted code of a page.
        await Assertions.Expect(page.Locator(".callout[data-callout=warning]")).ToContainTextAsync("Check the checksum");
        await Assertions.Expect(page.Locator(".code-block[data-language=bash] .tok-command").First).ToBeVisibleAsync();

        // Copy button: the code goes to the clipboard and the button says so.
        var block = page.Locator(".code-block[data-language=bash]").First;
        await block.GetByRole(AriaRole.Button, new() { Name = "Copy" }).ClickAsync();
        await Assertions.Expect(block.GetByRole(AriaRole.Button)).ToHaveTextAsync("Copied");
        await Assertions.Expect(block.GetByRole(AriaRole.Status)).ToHaveTextAsync("Copied");
        if (checkClipboard)
        {
            var copied = await page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
            Assert.Equal("brew install tern", copied);
        }
        await Assertions.Expect(block.GetByRole(AriaRole.Button)).ToHaveTextAsync("Copy");

        // Previous and next, in the order of the navigation.
        await Assertions.Expect(page.Locator("a[rel=prev]")).ToContainTextAsync("Introduction");
        await Assertions.Expect(page.Locator("a[rel=next]")).ToContainTextAsync("Quick start");

        // A table scrolls in its own box.
        await Go(page, "reference/commands");
        await Assertions.Expect(page.Locator(".table-scroll table").First).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".table-scroll").First).ToHaveAttributeAsync("tabindex", "0");

        // Search: the index is not in the pages. It is a file the browser fetches the first time the search opens, once.
        var indexRequests = new List<string>();
        void OnRequest(object? sender, IRequest request)
        {
            if (request.Url.EndsWith("/search-index.json", StringComparison.Ordinal)) lock (indexRequests) indexRequests.Add(request.Url);
        }
        page.Request += OnRequest;
        await Go(page, "");
        await page.WaitForFunctionAsync("() => !!customElements.get('kly-popover')");
        // The page has loaded and its script has run (the proxy of the docs host keeps a connection open, so the network never goes idle): give a fetch time to show.
        await Task.Delay(1000);
        lock (indexRequests) Assert.Empty(indexRequests);
        await page.GetByRole(AriaRole.Button, new() { Name = "Search" }).Locator("visible=true").First.ClickAsync();
        var search = page.GetByRole(AriaRole.Dialog);
        await Assertions.Expect(search).ToBeVisibleAsync();
        await search.GetByRole(AriaRole.Combobox).FillAsync("systemd");
        var result = search.GetByRole(AriaRole.Option).Filter(new() { HasText = "Scheduling" });
        await Assertions.Expect(result.First).ToBeVisibleAsync();
        await result.First.ClickAsync();
        await page.WaitForUrlMatchingAsync("/guides/scheduling$");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Scheduling");
        page.Request -= OnRequest;
        lock (indexRequests) Assert.Single(indexRequests);
    }

    /// <summary>
    /// The search index is a file of its own, not part of the pages: a page holds no result of the search, is a third of what it was with the index inside
    /// (211 KB), and the file is sent with an ETag that answers 304.
    /// </summary>
    public static async Task AssertIndexIsNotInThePages(IPage page, string baseUrl)
    {
        var response = await page.APIRequest.GetAsync(new Uri(new Uri(baseUrl), "getting-started/quick-start").ToString());
        var html = await response.TextAsync();
        Assert.DoesNotContain("data-search=", html);
        Assert.Contains("data-kly-content-search-index=\"search-index.json\"", html);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(html) < 100_000, $"the page is {System.Text.Encoding.UTF8.GetByteCount(html)} bytes: the search index is in it");

        var index = await page.APIRequest.GetAsync(new Uri(new Uri(baseUrl), "search-index.json").ToString());
        Assert.Equal(200, index.Status);
        Assert.StartsWith("application/json", index.Headers["content-type"]);
        var etag = index.Headers["etag"];
        var again = await page.APIRequest.GetAsync(new Uri(new Uri(baseUrl), "search-index.json").ToString(), new() { Headers = new Dictionary<string, string> { ["If-None-Match"] = etag } });
        Assert.Equal(304, again.Status);
    }

    /// <summary>Goes to a page of the template, from the base address of the page being shown (the root of the app, or its path base).</summary>
    private static async Task Go(IPage page, string path)
    {
        var baseHref = await page.Locator("base").GetAttributeAsync("href");
        await page.GotoAsync(new Uri(new Uri(page.Url), baseHref + path).ToString());
    }

    /// <summary>A page that is not in the content answers 404 with the template's own page, and so does a draft.</summary>
    public static async Task AssertMissingPages(IPage page, string baseUrl, List<string> failures)
    {
        foreach (var path in new[] { "no-such-page", "guides/webhooks" })
        {
            var url = new Uri(new Uri(baseUrl), path).ToString();
            var response = await page.GotoAsync(path);
            Assert.Equal(404, response!.Status);
            await Assertions.Expect(page.GetByText("There is nothing at this address.")).ToBeVisibleAsync();
            lock (failures) Assert.Contains($"404 {url}", failures);
        }
    }

    /// <summary>The color mode button flips the class of the page, and the code block stays readable in both modes (axe checks the colors elsewhere).</summary>
    public static async Task AssertColorMode(IPage page)
    {
        var dark = (await page.Locator("html").GetAttributeAsync("class") ?? "").Contains("dark");
        await page.Locator("[data-kly-color-mode=toggle]").Locator("visible=true").First.ClickAsync();
        await Assertions.Expect(page.Locator("html")).ToHaveClassAsync(dark ? new Regex("^((?!dark).)*$") : new Regex("dark"));
    }

    /// <summary>The pages with the widest content have no horizontal scroll at the width of a small phone.</summary>
    public static async Task AssertNoHorizontalScroll(IPage page, params string[] paths)
    {
        await page.SetViewportSizeAsync(375, 800);
        foreach (var path in paths)
        {
            await page.GotoAsync(path);
            await page.WaitForFunctionAsync("() => !!customElements.get('kly-popover')");
            var sizes = await page.EvaluateAsync<int[]>("() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
            Assert.True(sizes[0] <= sizes[1], $"horizontal scroll at 375px on '{path}': scrollWidth {sizes[0]} > clientWidth {sizes[1]}");
        }
    }
}
