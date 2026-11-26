using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Klyroo.UI.E2E;

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
        await page.WaitForUrlMatchingAsync(@"\?page=2$");
        await Assertions.Expect(page.GetByTestId("release")).ToHaveCountAsync(2);
        await Assertions.Expect(page.GetByTestId("release").First.GetByRole(AriaRole.Heading, new() { Level = 2 })).ToHaveTextAsync("Tern 0.9.0");
        // v0.8.0 has no name and no notes: its title is its tag.
        await Assertions.Expect(page.GetByTestId("release").Nth(1).GetByRole(AriaRole.Heading, new() { Level = 2 })).ToHaveTextAsync("v0.8.0");
        var last = page.GetByRole(AriaRole.Navigation, new() { Name = "Pages of releases" });
        await Assertions.Expect(last).ToContainTextAsync("Page 2 of 2");
        await Assertions.Expect(last.GetByRole(AriaRole.Link, new() { Name = "Older releases" })).ToHaveCountAsync(0);
        await last.GetByRole(AriaRole.Link, new() { Name = "Newer releases" }).ClickAsync();
        await page.WaitForUrlAsync("!url.search.includes('page=')");
        await Assertions.Expect(page.GetByTestId("release")).ToHaveCountAsync(10);

        // A release page: its own address from the tag, the notes in full, a way back, and the neighbours in time.
        await page.GetByTestId("release").First.GetByRole(AriaRole.Link, new() { Name = "Tern 1.4.0" }).ClickAsync();
        await page.WaitForUrlMatchingAsync(@"/releases/v1\.4\.0$");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Tern 1.4.0");
        await Assertions.Expect(page).ToHaveTitleAsync("Tern 1.4.0 - Tern");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 2, Name = "Highlights" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "View on GitHub" })).ToHaveAttributeAsync("href", "https://github.com/tern-cli/tern/releases/tag/v1.4.0");
        var neighbours = page.GetByRole(AriaRole.Navigation, new() { Name = "Other releases" });
        await Assertions.Expect(neighbours.GetByRole(AriaRole.Link)).ToHaveCountAsync(1);
        await Assertions.Expect(neighbours.GetByRole(AriaRole.Link, new() { Name = "Tern 1.3.2" })).ToHaveAttributeAsync("rel", "next");
        await neighbours.GetByRole(AriaRole.Link, new() { Name = "Tern 1.3.2" }).ClickAsync();
        await page.WaitForUrlMatchingAsync(@"/releases/v1\.3\.2$");
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
