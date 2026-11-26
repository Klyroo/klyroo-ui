using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Klyroo.UI.E2E;

/// <summary>What the SaaS template does in a browser, shared by the run of the published app and the run of its demo behind the docs host.</summary>
internal static class SaasChecks
{
    /// <summary>
    /// The home page, the pricing page with its switch and table, the blog with its tags and an article, the documentation, the changelog and the search
    /// of all three: every piece runs in the browser from HTML, with no circuit and no WebAssembly, and every address is relative to the base.
    /// </summary>
    public static async Task AssertBehaviour(IPage page)
    {
        // Home.
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Know when a scheduled job stops running");
        foreach (var id in new[] { "features", "how-it-works", "customers", "blog" }) await Assertions.Expect(page.Locator($"section#{id}")).ToHaveCountAsync(1);
        await Assertions.Expect(page.GetByTestId("testimonial")).ToHaveCountAsync(3);
        await Assertions.Expect(page.GetByTestId("step")).ToHaveCountAsync(3);
        await Assertions.Expect(page.GetByTestId("latest-post")).ToHaveCountAsync(3);
        await Assertions.Expect(page.Locator("#how-it-works .code-block[data-language=bash]")).ToHaveCountAsync(1);
        await page.WaitForFunctionAsync("() => !!customElements.get('kly-popover') && !!customElements.get('kly-accordion')");
        // The newest post leads the three.
        await Assertions.Expect(page.GetByTestId("latest-post").First).ToContainTextAsync("Tideline 2");

        // Pricing, from the header.
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Main" }).GetByRole(AriaRole.Link, new() { Name = "Pricing" }).First.ClickAsync();
        await page.WaitForUrlMatchingAsync("/pricing$");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Pay for the monitors you use");
        var pricing = page.GetByTestId("pricing");
        await Assertions.Expect(pricing.GetByText("$29", new() { Exact = true }).Locator("visible=true")).ToHaveCountAsync(1);
        await Assertions.Expect(pricing.GetByText("$24", new() { Exact = true }).Locator("visible=true")).ToHaveCountAsync(0);
        await pricing.GetByText("Yearly, 2 months free").ClickAsync();
        await Assertions.Expect(pricing.GetByText("$24", new() { Exact = true }).Locator("visible=true")).ToHaveCountAsync(1);
        await Assertions.Expect(pricing.GetByText("$29", new() { Exact = true }).Locator("visible=true")).ToHaveCountAsync(0);
        await Assertions.Expect(pricing.GetByText("$288 billed once a year.").Locator("visible=true")).ToHaveCountAsync(1);
        await page.GetByRole(AriaRole.Radio, new() { Name = "Yearly, 2 months free" }).FocusAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(page.GetByRole(AriaRole.Radio, new() { Name = "Monthly" })).ToBeCheckedAsync();
        await Assertions.Expect(pricing.GetByText("$29", new() { Exact = true }).Locator("visible=true")).ToHaveCountAsync(1);
        // The comparison table: a row, with a value per plan.
        var compare = page.GetByTestId("compare");
        await Assertions.Expect(compare.GetByRole(AriaRole.Row, new() { Name = "Users 1 10 50" })).ToBeVisibleAsync();
        await Assertions.Expect(compare.GetByRole(AriaRole.Columnheader)).ToHaveCountAsync(4);
        // FAQ.
        var question = page.GetByRole(AriaRole.Button, new() { Name = "What counts as a monitor?" });
        await Assertions.Expect(question).ToHaveAttributeAsync("aria-expanded", "false");
        await question.ClickAsync();
        await Assertions.Expect(question).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(page.GetByText("One monitor is one job or one schedule")).ToBeVisibleAsync();

        // Blog: five posts, the newest large, tags that filter, an article.
        await Go(page, "blog");
        await Assertions.Expect(page.GetByTestId("post")).ToHaveCountAsync(5);
        await page.GetByRole(AriaRole.Link, new() { Name = "engineering (2)" }).ClickAsync();
        await page.WaitForUrlMatchingAsync(@"/blog\?tag=engineering$");
        await Assertions.Expect(page.GetByTestId("post")).ToHaveCountAsync(2);
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "engineering (2)" })).ToHaveAttributeAsync("aria-current", "page");
        await Go(page, "blog?tag=nothing");
        await Assertions.Expect(page.GetByText("No posts with this tag")).ToBeVisibleAsync();
        await Go(page, "blog");
        await page.GetByRole(AriaRole.Link, new() { Name = "Grace periods without the guesswork" }).ClickAsync();
        await page.WaitForUrlMatchingAsync("/blog/grace-periods-without-the-guesswork$");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Grace periods without the guesswork");
        await Assertions.Expect(page).ToHaveTitleAsync("Grace periods without the guesswork - Tideline");
        var article = page.GetByTestId("article");
        await Assertions.Expect(article.GetByText("Camila Duarte")).ToBeVisibleAsync();
        await Assertions.Expect(article.GetByText("min read")).ToBeVisibleAsync();
        await Assertions.Expect(article.Locator("time")).ToHaveAttributeAsync("datetime", "2026-09-30");
        await Assertions.Expect(page.GetByTestId("more-post")).ToHaveCountAsync(2);
        // The cover loads, and the link in the text reaches a page of the documentation.
        await Assertions.Expect(article.Locator("img")).ToHaveCountAsync(1);
        await page.WaitForFunctionAsync("() => { const i = document.querySelector('[data-testid=article] img'); return i && i.complete && i.naturalWidth > 0; }");
        await article.GetByRole(AriaRole.Link, new() { Name = "guide on grace periods" }).ClickAsync();
        await page.WaitForUrlMatchingAsync("/docs/guides/grace-periods$");

        // Documentation: sections, the navigation of docs only, the table of contents and the previous and next links of docs only.
        await Go(page, "docs");
        await Assertions.Expect(page.GetByTestId("sections").GetByRole(AriaRole.Listitem).Filter(new() { Has = page.GetByRole(AriaRole.Heading, new() { Level = 2 }) })).ToHaveCountAsync(3);
        await page.GetByTestId("sections").GetByRole(AriaRole.Link, new() { Name = "Concepts", Exact = true }).ClickAsync();
        await page.WaitForUrlMatchingAsync("/docs/getting-started/concepts$");
        var nav = page.GetByRole(AriaRole.Navigation, new() { Name = "Documentation", Exact = true });
        await Assertions.Expect(nav.GetByRole(AriaRole.Link, new() { Name = "Concepts", Exact = true })).ToHaveAttributeAsync("aria-current", "page");
        await Assertions.Expect(nav.Locator("a")).ToHaveCountAsync(10);
        await Assertions.Expect(page.Locator("a[rel=prev]")).ToContainTextAsync("Quick start");
        await Assertions.Expect(page.Locator("a[rel=next]")).ToContainTextAsync("Monitor a cron job");
        var toc = page.GetByRole(AriaRole.Navigation, new() { Name = "On this page" }).Filter(new() { Has = page.Locator("a") }).Locator("visible=true");
        await Assertions.Expect(toc.GetByRole(AriaRole.Link)).ToHaveCountAsync(5);
        // The last page of the docs has no next: the posts and versions that follow it in the whole site are not its neighbours.
        await Go(page, "docs/reference/limits");
        await Assertions.Expect(page.Locator("a[rel=next]")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator("a[rel=prev]")).ToContainTextAsync("Command line");

        // Changelog: the versions newest first, each with a page of its own.
        await Go(page, "changelog");
        await Assertions.Expect(page.GetByTestId("release")).ToHaveCountAsync(5);
        await Assertions.Expect(page.GetByTestId("release").First).ToContainTextAsync("2.0");
        await page.GetByTestId("release").First.GetByRole(AriaRole.Link, new() { Name = "2.0" }).ClickAsync();
        await page.WaitForUrlMatchingAsync("/changelog/2-0$");
        await Assertions.Expect(page.GetByTestId("release-page")).ToContainTextAsync("Public status pages");

        // Dates: the visible date is the one of the culture, the datetime attribute is not; the page of a version says the same as the list.
        await Assertions.Expect(page.GetByTestId("release-page").Locator("time")).ToHaveTextAsync("November 12, 2026");
        await Assertions.Expect(page.GetByTestId("release-page").Locator("time")).ToHaveAttributeAsync("datetime", "2026-11-12");

        // Plain pages: a Markdown page outside the docs, the blog and the changelog, reached from the footer, with a link to a sibling page.
        await Go(page, "");
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Footer" }).GetByRole(AriaRole.Link, new() { Name = "Terms of service" }).ClickAsync();
        await page.WaitForUrlMatchingAsync("/legal/terms$");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Terms of service");
        await Assertions.Expect(page).ToHaveTitleAsync("Terms of service - Tideline");
        await page.GetByTestId("plain-page").GetByRole(AriaRole.Link, new() { Name = "privacy policy" }).ClickAsync();
        await page.WaitForUrlMatchingAsync("/legal/privacy$");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Privacy policy");

        // Search: one index for the docs, the blog and the changelog, fetched the first time the search opens.
        await Go(page, "");
        await page.WaitForFunctionAsync("() => !!customElements.get('kly-popover')");
        await page.GetByRole(AriaRole.Button, new() { Name = "Search" }).Locator("visible=true").First.ClickAsync();
        var search = page.GetByRole(AriaRole.Dialog);
        await Assertions.Expect(search).ToBeVisibleAsync();
        await search.GetByRole(AriaRole.Combobox).FillAsync("audit log");
        var post = search.GetByRole(AriaRole.Option).Filter(new() { HasText = "Roles and an audit log" });
        await Assertions.Expect(post.First).ToBeVisibleAsync();
        await post.First.ClickAsync();
        await page.WaitForUrlMatchingAsync("/blog/roles-and-an-audit-log-for-your-team$");
    }

    /// <summary>
    /// Sign in and sign up post to the server and render the answer: no circuit. An empty form shows what is missing, a short password is refused,
    /// a valid form says that it is valid and that nothing was stored, and the password is not sent back in the page. Changes the state of the page.
    /// </summary>
    public static async Task AssertAuthPages(IPage page)
    {
        await Go(page, "login");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Sign in");
        // No "this is a template" note on the page: the message after a valid submit is the only one, and the README says the rest.
        await Assertions.Expect(page.GetByTestId("auth-note")).ToHaveCountAsync(0);
        // Both "Continue with" buttons say that they are demos.
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Continue with GitHub (demo)" })).ToHaveAttributeAsync("href", "https://example.com/auth/github");
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Continue with Google (demo)" })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await Assertions.Expect(page.Locator("input[aria-invalid=true]")).ToHaveCountAsync(2);
        await Assertions.Expect(page.GetByTestId("auth-result")).ToHaveCountAsync(0);

        await page.GetByLabel("Email").FillAsync("not-an-email");
        await page.GetByLabel("Password", new() { Exact = true }).FillAsync("hunter2hunter2");
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Enter a valid email address.")).ToBeVisibleAsync();

        await page.GetByLabel("Email").FillAsync("ana@company.test");
        await page.GetByLabel("Password", new() { Exact = true }).FillAsync("hunter2hunter2");
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        var result = page.GetByTestId("auth-result");
        await Assertions.Expect(result).ToContainTextAsync("ana@company.test");
        await Assertions.Expect(result).ToContainTextAsync("nothing was checked and nothing was stored");
        // Nothing was stored: no cookie but the antiforgery one, and the password is not in the page.
        Assert.DoesNotContain("hunter2hunter2", await page.ContentAsync());
        Assert.All(await page.Context.CookiesAsync(), c => Assert.Matches("Antiforgery|Culture|klyroo-", c.Name));

        // Forgot password: reached from the sign-in page, checks the email, sends nothing.
        await Go(page, "login");
        await page.GetByRole(AriaRole.Link, new() { Name = "Forgot your password?" }).ClickAsync();
        await page.WaitForUrlMatchingAsync("/forgot-password$");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Reset your password");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send the link" }).ClickAsync();
        await Assertions.Expect(page.Locator("input[aria-invalid=true]")).ToHaveCountAsync(1);
        await page.GetByLabel("Email").FillAsync("ana@company.test");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send the link" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("auth-result")).ToContainTextAsync("nothing was sent and nothing was stored");

        await Go(page, "signup");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Create your account");
        await page.GetByLabel("Full name").FillAsync("Ana Torres");
        await page.GetByLabel("Work email").FillAsync("ana@company.test");
        await page.GetByLabel("Password", new() { Exact = true }).FillAsync("short");
        await page.GetByRole(AriaRole.Checkbox).CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Use at least 8 characters.")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("auth-result")).ToHaveCountAsync(0);
        await page.GetByLabel("Password", new() { Exact = true }).FillAsync("a longer password");
        await page.GetByRole(AriaRole.Checkbox).CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("auth-result")).ToContainTextAsync("An account would be created for ana@company.test");
    }

    /// <summary>In Windows High Contrast the checked billing option must still look different from the other one.</summary>
    public static async Task AssertForcedColors(IPage page)
    {
        await Go(page, "pricing");
        await page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });
        try
        {
            var monthly = page.Locator("label[for=billing-monthly]");
            var yearly = page.Locator("label[for=billing-yearly]");
            await page.GetByRole(AriaRole.Radio, new() { Name = "Monthly" }).CheckAsync(new() { Force = true });
            var checkedFill = await monthly.EvaluateAsync<string>("e => getComputedStyle(e).backgroundColor");
            var otherFill = await yearly.EvaluateAsync<string>("e => getComputedStyle(e).backgroundColor");
            Assert.NotEqual(otherFill, checkedFill);
        }
        finally
        {
            await page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.None });
        }
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
