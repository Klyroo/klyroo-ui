using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Klyroo.UI.E2E;

/// <summary>What the Portfolio template does in a browser, shared by the run of the published app and the run of its demo behind the docs host.</summary>
internal static class PortfolioChecks
{
    /// <summary>
    /// The home page, the work list and a project page, the about page with its timeline, the writing list with its tags and an article, the speaking
    /// page, a plain page and the search: every piece runs in the browser from HTML, with no circuit and no WebAssembly, and every address is relative to the base.
    /// </summary>
    public static async Task AssertBehaviour(IPage page)
    {
        // Home.
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Interfaces that are quick to learn and hard to break");
        foreach (var id in new[] { "work", "skills", "writing" }) await Assertions.Expect(page.Locator($"section#{id}")).ToHaveCountAsync(1);
        await Assertions.Expect(page.GetByTestId("hero-card")).ToContainTextAsync("Booking projects from February 2027");
        await Assertions.Expect(page.GetByTestId("project")).ToHaveCountAsync(3);
        await Assertions.Expect(page.GetByTestId("project").First).ToContainTextAsync("Harbor Atlas");
        await Assertions.Expect(page.GetByTestId("latest-article")).ToHaveCountAsync(3);
        await Assertions.Expect(page.GetByTestId("latest-article").First).ToContainTextAsync("A focus ring you can see");
        await page.WaitForFunctionAsync("() => !!customElements.get('kly-popover')");
        // The social icons of the hero card are real icons, not the missing icon placeholder.
        await Assertions.Expect(page.Locator("[data-kly-missing-icon]")).ToHaveCountAsync(0);

        // Work, from the header.
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Main" }).GetByRole(AriaRole.Link, new() { Name = "Work" }).First.ClickAsync();
        await page.WaitForUrlMatchingAsync("/work$");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Work");
        await Assertions.Expect(page.GetByTestId("project")).ToHaveCountAsync(5);
        await page.GetByRole(AriaRole.Link, new() { Name = "Quire", Exact = true }).ClickAsync();
        await page.WaitForUrlMatchingAsync("/work/quire$");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Quire");
        await Assertions.Expect(page).ToHaveTitleAsync("Quire - Ines Calloway");
        var facts = page.GetByTestId("project-facts");
        await Assertions.Expect(facts).ToContainTextAsync("2024");
        await Assertions.Expect(facts).ToContainTextAsync("Meridian Credit");
        await Assertions.Expect(facts).ToContainTextAsync("Design tokens");
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Case study" })).ToHaveAttributeAsync("href", "https://example.com/quire/case-study");
        // A link of the front matter to a page of the site is relative to the base, so it stays below a path base.
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "How I audit" })).ToHaveAttributeAsync("href", "writing/the-audit-i-run-before-writing-a-token");
        var project = page.GetByTestId("project-page");
        await Assertions.Expect(project.Locator("img")).ToHaveCountAsync(1);
        await page.WaitForFunctionAsync("() => { const i = document.querySelector('[data-testid=project-page] img'); return i && i.complete && i.naturalWidth > 0; }");
        await Assertions.Expect(project.Locator(".code-block[data-language=json]")).ToHaveCountAsync(1);
        await Assertions.Expect(page.GetByTestId("project")).ToHaveCountAsync(2);
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Breadcrumb" }).GetByRole(AriaRole.Link, new() { Name = "Work" }).ClickAsync();
        await page.WaitForUrlMatchingAsync("/work$");

        // About: the text, the timeline, the skills.
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Main" }).GetByRole(AriaRole.Link, new() { Name = "About" }).First.ClickAsync();
        await page.WaitForUrlMatchingAsync("/about$");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("About");
        await Assertions.Expect(page.GetByTestId("about-text")).ToContainTextAsync("I am Ines, a design engineer.");
        var timeline = page.GetByTestId("experience");
        await Assertions.Expect(timeline.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(4);
        await Assertions.Expect(timeline.GetByRole(AriaRole.Listitem).First).ToContainTextAsync("Independent design engineer, Calloway Studio");
        await Assertions.Expect(timeline.GetByRole(AriaRole.Listitem).First).ToContainTextAsync("2024 to now");
        await Assertions.Expect(page.GetByTestId("skill-group")).ToHaveCountAsync(3);

        // Writing: five articles, the newest large, tags that filter, an article.
        await Go(page, "writing");
        await Assertions.Expect(page.GetByTestId("article-card")).ToHaveCountAsync(5);
        await page.GetByRole(AriaRole.Link, new() { Name = "css (2)" }).ClickAsync();
        await page.WaitForUrlMatchingAsync(@"/writing\?tag=css$");
        await Assertions.Expect(page.GetByTestId("article-card")).ToHaveCountAsync(2);
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "css (2)" })).ToHaveAttributeAsync("aria-current", "page");
        await Go(page, "writing?tag=nothing");
        await Assertions.Expect(page.GetByText("No articles with this tag")).ToBeVisibleAsync();
        await Go(page, "writing");
        await page.GetByRole(AriaRole.Link, new() { Name = "Spacing scales with fewer steps" }).ClickAsync();
        await page.WaitForUrlMatchingAsync("/writing/spacing-scales-with-fewer-steps$");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Spacing scales with fewer steps");
        await Assertions.Expect(page).ToHaveTitleAsync("Spacing scales with fewer steps - Ines Calloway");
        var article = page.GetByTestId("article");
        await Assertions.Expect(article.GetByText("min read")).ToBeVisibleAsync();
        await Assertions.Expect(article.Locator("time")).ToHaveAttributeAsync("datetime", "2026-05-20");
        await Assertions.Expect(article.Locator("time")).ToHaveTextAsync("May 20, 2026");
        await Assertions.Expect(page.GetByTestId("more-article")).ToHaveCountAsync(2);
        await Assertions.Expect(article.Locator(".code-block")).ToHaveCountAsync(1);
        await page.WaitForFunctionAsync("() => { const i = document.querySelector('[data-testid=article] img'); return i && i.complete && i.naturalWidth > 0; }");
        // The link in the text of an article reaches a project.
        await Go(page, "writing/what-an-offline-first-app-owes-its-users");
        await page.GetByTestId("article").GetByRole(AriaRole.Link, new() { Name = "Fieldnote" }).ClickAsync();
        await page.WaitForUrlMatchingAsync("/work/fieldnote$");

        // Speaking: the years, the talks, the materials.
        await Go(page, "");
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Main" }).GetByRole(AriaRole.Link, new() { Name = "Speaking" }).First.ClickAsync();
        await page.WaitForUrlMatchingAsync("/speaking$");
        await Assertions.Expect(page.GetByTestId("talk-year")).ToHaveCountAsync(2);
        await Assertions.Expect(page.GetByTestId("talk")).ToHaveCountAsync(5);
        await Assertions.Expect(page.GetByTestId("talk").First).ToContainTextAsync("Focus you can see on every background");
        await Assertions.Expect(page.GetByTestId("talk").First.Locator("time")).ToHaveTextAsync("October 9, 2026");
        await Assertions.Expect(page.GetByTestId("talk").First.Locator("time")).ToHaveAttributeAsync("datetime", "2026-10-09");
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Slides of Focus you can see on every background" })).ToHaveAttributeAsync("href", "https://example.com/ines/slides/focus");

        // Plain pages: a Markdown page outside work and writing, reached from the footer.
        await Go(page, "");
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Footer" }).GetByRole(AriaRole.Link, new() { Name = "Uses" }).ClickAsync();
        await page.WaitForUrlMatchingAsync("/uses$");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Uses");
        await Assertions.Expect(page).ToHaveTitleAsync("Uses - Ines Calloway");
        await Assertions.Expect(page.GetByTestId("plain-page").GetByRole(AriaRole.Table)).ToBeVisibleAsync();

        // Search: one index for the projects, the articles and the text pages, fetched the first time the search opens.
        await Go(page, "");
        await page.WaitForFunctionAsync("() => !!customElements.get('kly-popover')");
        await page.GetByRole(AriaRole.Button, new() { Name = "Search" }).Locator("visible=true").First.ClickAsync();
        var search = page.GetByRole(AriaRole.Dialog);
        await Assertions.Expect(search).ToBeVisibleAsync();
        await search.GetByRole(AriaRole.Combobox).FillAsync("offline");
        var hit = search.GetByRole(AriaRole.Option).Filter(new() { HasText = "What an offline-first app owes" });
        await Assertions.Expect(hit.First).ToBeVisibleAsync();
        await hit.First.ClickAsync();
        await page.WaitForUrlMatchingAsync("/writing/what-an-offline-first-app-owes-its-users$");
    }

    /// <summary>
    /// The contact form posts to the server and renders the answer: no circuit. An empty form says what is missing, a bad address is refused, a
    /// valid form says that it is valid and that nothing was sent, and the message is not sent back in the page. Changes the state of the page.
    /// </summary>
    public static async Task AssertContactForm(IPage page)
    {
        await Go(page, "contact");
        var form = page.GetByTestId("contact-form");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Contact");
        await Assertions.Expect(page.GetByTestId("contact-result")).ToHaveCountAsync(0);
        await page.GetByRole(AriaRole.Button, new() { Name = "Send the message" }).ClickAsync();
        foreach (var error in new[] { "Enter your name.", "Enter your email address.", "Pick what this is about.", "Write a message." })
        {
            await Assertions.Expect(page.GetByText(error)).ToBeVisibleAsync();
        }
        await Assertions.Expect(page.GetByTestId("contact-result")).ToHaveCountAsync(0);

        await form.GetByLabel("Name").FillAsync("Ana Torres");
        await form.GetByLabel("Email").FillAsync("not-an-email");
        await form.GetByLabel("Message").FillAsync("Too short");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send the message" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Enter a valid email address.")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Write between 10 and 2000 characters.")).ToBeVisibleAsync();
        // What was typed is still in the form after a refused post.
        await Assertions.Expect(form.GetByLabel("Name")).ToHaveValueAsync("Ana Torres");
        await Assertions.Expect(page.GetByTestId("contact-result")).ToHaveCountAsync(0);

        await form.GetByLabel("Email").FillAsync("ana@company.test");
        await form.GetByLabel("Message").FillAsync("I would like to talk about a design system for our product.");
        await form.GetByRole(AriaRole.Radio, new() { Name = "A project" }).CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Send the message" }).ClickAsync();
        // Post/Redirect/Get: the answer to the post is a redirect to a page that a reload loads again without posting.
        await page.WaitForUrlMatchingAsync(@"/contact\?sent=1$");
        var result = page.GetByTestId("contact-result");
        await Assertions.Expect(result).ToContainTextAsync("nothing was sent and nothing was stored");
        // Nothing was stored: no cookie but the antiforgery one, the form is empty and the message is not in the page.
        Assert.DoesNotContain("design system for our product", await page.ContentAsync());
        await Assertions.Expect(form.GetByLabel("Name")).ToHaveValueAsync("");
        Assert.All(await page.Context.CookiesAsync(), c => Assert.Matches("Antiforgery|Culture|klyroo-", c.Name));
        var reload = await page.ReloadAsync();
        Assert.Equal("GET", reload!.Request.Method);
        await Assertions.Expect(page.GetByTestId("contact-result")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("[aria-invalid=true]")).ToHaveCountAsync(0);
        // The form itself, without the query, shows no confirmation.
        await Go(page, "contact");
        await Assertions.Expect(page.GetByTestId("contact-result")).ToHaveCountAsync(0);
    }

    /// <summary>
    /// The form works with JavaScript off, as every form of a static site has to: the topic is a group of radio buttons that the browser posts, the answer
    /// to an empty form carries the errors, and a valid form redirects to the confirmation.
    /// </summary>
    public static async Task AssertContactFormWithoutScript(IBrowser browser, string contactUrl)
    {
        await using var context = await browser.NewContextAsync(new() { JavaScriptEnabled = false });
        var page = await context.NewPageAsync();
        await page.GotoAsync(contactUrl);
        var form = page.GetByTestId("contact-form");

        await page.GetByRole(AriaRole.Button, new() { Name = "Send the message" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Pick what this is about.")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Enter your name.")).ToBeVisibleAsync();

        await form.GetByLabel("Name").FillAsync("Ana Torres");
        await form.GetByLabel("Email").FillAsync("ana@company.test");
        await form.GetByLabel("Message").FillAsync("A message sent with no script at all.");
        await form.GetByRole(AriaRole.Radio, new() { Name = "A talk or a workshop" }).CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Send the message" }).ClickAsync();

        await page.WaitForUrlMatchingAsync(@"/contact\?sent=1$");
        await Assertions.Expect(page.GetByTestId("contact-result")).ToContainTextAsync("nothing was sent and nothing was stored");
    }

    /// <summary>The color mode button flips the class of the page.</summary>
    public static async Task AssertColorMode(IPage page)
    {
        var dark = (await page.Locator("html").GetAttributeAsync("class") ?? "").Contains("dark");
        await page.Locator("[data-kly-color-mode=toggle]").Locator("visible=true").First.ClickAsync();
        await Assertions.Expect(page.Locator("html")).ToHaveClassAsync(dark ? new Regex("^((?!dark).)*$") : new Regex("dark"));
    }

    /// <summary>The widest pages have no horizontal scroll at the width of a small phone.</summary>
    public static async Task AssertNoHorizontalScroll(IPage page, params string[] paths)
    {
        await page.SetViewportSizeAsync(375, 800);
        foreach (var path in paths)
        {
            await Go(page, path);
            await page.WaitForFunctionAsync("() => !!customElements.get('kly-popover')");
            var sizes = await page.EvaluateAsync<int[]>("() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
            Assert.True(sizes[0] <= sizes[1], $"horizontal scroll at 375px on '{path}': scrollWidth {sizes[0]} > clientWidth {sizes[1]}");
        }
    }

    /// <summary>Goes to a page of the template, from the base address of the page being shown (the root of the app, or its path base).</summary>
    public static async Task Go(IPage page, string path)
    {
        var baseHref = await page.Locator("base").GetAttributeAsync("href");
        await page.GotoAsync(new Uri(new Uri(page.Url), baseHref + path).ToString());
    }
}
