using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;

namespace Blux.UI.E2E;

/// <summary>What the Dashboard template does in a browser, shared by the run of the published app and the run of its demo behind the docs host.</summary>
internal static class DashboardChecks
{
    private static readonly Regex Interactive = new("^(Server|WebAssembly)$");

    /// <summary>Waits until the page left its prerendered state: a click before that would go nowhere.</summary>
    public static Task WaitInteractive(IPage page) =>
        Assertions.Expect(page.GetByTestId("renderer")).ToHaveTextAsync(Interactive, new() { Timeout = 60_000 });

    /// <summary>Home: the four figures, the charts, the recent sales, and the period in the address.</summary>
    public static async Task AssertHome(IPage page)
    {
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Home");
        foreach (var stat in new[] { "revenue", "orders", "customers", "conversion" })
        {
            await Assertions.Expect(page.GetByTestId($"stat-{stat}-value")).ToHaveCountAsync(1);
            await Assertions.Expect(page.GetByTestId($"stat-{stat}-change")).ToContainTextAsync("%");
        }
        await Assertions.Expect(page.GetByTestId("stat-revenue-value")).ToHaveTextAsync(new Regex(@"^\$[\d,]+$"));
        await Assertions.Expect(page.GetByTestId("sale")).ToHaveCountAsync(8);
        await Assertions.Expect(page.Locator("[data-testid=revenue-chart] svg, [data-testid=revenue-chart]").First).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("channels-chart")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("orders-chart")).ToBeVisibleAsync();

        // A shorter period: a link, so the address keeps it, and the figures change.
        var before = await page.GetByTestId("stat-revenue-value").TextContentAsync();
        await page.GetByTestId("period-week").ClickAsync();
        await page.WaitForURLAsync(new Regex(@"period=week"));
        await Assertions.Expect(page.GetByTestId("period-week")).ToHaveAttributeAsync("aria-current", "true");
        await Assertions.Expect(page.GetByTestId("stat-revenue-value")).Not.ToHaveTextAsync(before!);
    }

    /// <summary>The value of a cookie of the app by its name at the root (below a path base the name carries a slug of it).</summary>
    public static async Task<string?> CookieValue(IPage page, string name) =>
        (await page.Context.CookiesAsync()).FirstOrDefault(c => c.Name == name || c.Name.StartsWith(name + ".", StringComparison.Ordinal))?.Value;

    /// <summary>
    /// The color choice is a cookie that the server reads while it renders: after a choice, the HTML of the first response (what the browser paints before any script runs)
    /// already carries the style of the chosen colors, and a value that is not an offered palette is ignored. The cookie is for the path of the app, SameSite=Lax and long-lived.
    /// </summary>
    public static async Task AssertColorsInThePrerender(IPage page, IBrowser browser, string baseUrl, string? pathBase)
    {
        var cookies = (await page.Context.CookiesAsync()).Where(c => c.Name.StartsWith("dashboard-primary", StringComparison.Ordinal) || c.Name.StartsWith("dashboard-neutral", StringComparison.Ordinal)).ToList();
        Assert.Equal(2, cookies.Count);
        Assert.All(cookies, c =>
        {
            Assert.Equal(string.IsNullOrEmpty(pathBase) ? "/" : pathBase, c.Path);
            Assert.Equal(SameSiteAttribute.Lax, c.SameSite);
            Assert.True(c.Expires > DateTimeOffset.UtcNow.AddDays(300).ToUnixTimeSeconds(), "the cookie lasts more than 300 days");
        });
        if (!string.IsNullOrEmpty(pathBase)) Assert.All(cookies, c => Assert.Contains("~~", c.Name));

        // The response of the server, before any script: it has the style of the chosen colors (the same as the live page has after the choice).
        var live = await page.EvaluateAsync<string>("() => [...document.querySelectorAll('style[data-bx-theme]')].map(e => e.textContent).join('')");
        var html = await (await page.Context.APIRequest.GetAsync("customers")).TextAsync();
        Assert.Contains("data-bx-theme", html);
        var themed = System.Text.RegularExpressions.Regex.Matches(html, "<style data-bx-theme[^>]*>(.*?)</style>", RegexOptions.Singleline).Select(m => m.Groups[1].Value).ToArray();
        Assert.Contains(live, themed);

        // The interactive render that follows the prerender keeps the colors (the prerender hands them over), and so does a reload.
        var primary = () => page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).getPropertyValue('--blux-primary').trim()");
        var chosen = await primary();
        await page.ReloadAsync();
        Assert.Equal(chosen, await primary());
        await WaitInteractive(page);
        Assert.Equal(chosen, await primary());
        Assert.Equal(live, await page.EvaluateAsync<string>("() => [...document.querySelectorAll('style[data-bx-theme]')].map(e => e.textContent).join('')"));

        // A visitor with no cookie, and one with an edited cookie, get the default colors.
        foreach (var value in new[] { null, "<script>", "red" })
        {
            await using var plain = await browser.NewContextAsync(new() { BaseURL = baseUrl, JavaScriptEnabled = false });
            if (value is not null)
            {
                await plain.AddCookiesAsync(cookies.Select(c => new Microsoft.Playwright.Cookie { Name = c.Name, Value = value, Domain = c.Domain, Path = c.Path }));
            }
            var defaults = await (await plain.APIRequest.GetAsync("customers")).TextAsync();
            Assert.DoesNotContain(live, System.Text.RegularExpressions.Regex.Matches(defaults, "<style data-bx-theme[^>]*>(.*?)</style>", RegexOptions.Singleline).Select(m => m.Groups[1].Value));
        }
    }

    /// <summary>The sidebar, the command palette, the notifications and the appearance dialog: the parts of the shell.</summary>
    public static async Task AssertShell(IPage page)
    {
        // The sidebar is in the page once: the column of the wide layout, the off-canvas panel of the narrow one.
        var nav = page.GetByTestId("sidebar").GetByRole(AriaRole.Navigation, new() { Name = "Pages" });
        await Assertions.Expect(nav.GetByRole(AriaRole.Link)).ToHaveCountAsync(4);

        // Command palette: the shortcut opens it, a link command navigates.
        await page.Keyboard.PressAsync("Control+k");
        var palette = page.GetByRole(AriaRole.Dialog);
        await Assertions.Expect(palette.GetByRole(AriaRole.Combobox)).ToBeVisibleAsync();
        await palette.GetByRole(AriaRole.Combobox).FillAsync("security");
        await palette.GetByRole(AriaRole.Option, new() { Name = "Security" }).ClickAsync();
        await page.WaitForURLAsync(new Regex("settings/security$"));
        await Assertions.Expect(page.GetByTestId("settings-security")).ToBeVisibleAsync();

        // Notifications: the bell opens the slideover, which lists them and marks them read.
        await page.GetByTestId("notifications-button").ClickAsync();
        var notifications = page.GetByTestId("notifications");
        await Assertions.Expect(notifications.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(5);
        await Assertions.Expect(notifications.Locator("li[data-unread=true]")).ToHaveCountAsync(3);
        await page.GetByTestId("mark-read").ClickAsync();
        await Assertions.Expect(notifications.Locator("li[data-unread=true]")).ToHaveCountAsync(0);
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(notifications).Not.ToBeVisibleAsync();

        // Appearance: the user menu opens the dialog and the primary color follows the choice (and is remembered).
        var primary = () => page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).getPropertyValue('--blux-primary').trim()");
        var before = await primary();
        await page.GetByTestId("sidebar").GetByTestId("user-menu").ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Appearance" }).ClickAsync();
        await page.GetByTestId("palette-emerald").ClickAsync();
        await Assertions.Expect(page.GetByTestId("palette-emerald")).ToHaveAttributeAsync("aria-pressed", "true");
        Assert.NotEqual(before, await primary());
        Assert.Equal("emerald", await CookieValue(page, "dashboard-primary"));
        // The neutral color drives the gray of the text, the backgrounds and the borders.
        var grays = () => page.EvaluateAsync<string>("() => ['--blux-text', '--blux-bg-muted', '--blux-border'].map(n => getComputedStyle(document.documentElement).getPropertyValue(n).trim()).join('|')");
        var grayBefore = await grays();
        await page.GetByTestId("neutral-zinc").ClickAsync();
        await Assertions.Expect(page.GetByTestId("neutral-zinc")).ToHaveAttributeAsync("aria-pressed", "true");
        Assert.NotEqual(grayBefore, await grays());
        Assert.Equal("zinc", await CookieValue(page, "dashboard-neutral"));
        // The primary color is still the one that was chosen.
        await Assertions.Expect(page.GetByTestId("palette-emerald")).ToHaveAttributeAsync("aria-pressed", "true");
        await page.Keyboard.PressAsync("Escape");

        // Color mode.
        var dark = (await page.Locator("html").GetAttributeAsync("class") ?? "").Contains("dark");
        await page.Locator("[data-bx-color-mode=toggle]").First.ClickAsync();
        await Assertions.Expect(page.Locator("html")).ToHaveClassAsync(dark ? new Regex("^((?!dark).)*$") : new Regex("dark"));
    }

    private static async Task<List<string>> AxeViolations(IPage page)
    {
        var result = await page.RunAxe();
        return result.Violations
            .SelectMany(v => v.Nodes.Select(n => $"{v.Id}: {n.Html} -> {string.Join("; ", n.Any.Select(c => c.Message))}"))
            .ToList();
    }

    /// <summary>
    /// The sidebar as the off-canvas panel of a phone: it opens from the navbar button, passes axe open (in the current color mode and the other one), and a modal that lives
    /// outside it (the appearance dialog of the layout, opened from the user menu of the panel) closes it first, works, and gives the focus back to the button when it closes.
    /// </summary>
    public static async Task AssertMobilePanel(IPage page)
    {
        await page.SetViewportSizeAsync(390, 800);
        await page.GotoAsync("");
        await WaitInteractive(page);
        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "Open sidebar" });
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Menu" });
        await Assertions.Expect(panel).ToBeHiddenAsync();

        await toggle.ClickAsync();

        await Assertions.Expect(panel).ToBeVisibleAsync();
        await page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
        var light = await AxeViolations(page);
        Assert.True(light.Count == 0, "Panel open:\n" + string.Join("\n", light));
        var dark = (await page.Locator("html").GetAttributeAsync("class") ?? "").Contains("dark");
        await page.EvaluateAsync("d => document.documentElement.classList.toggle('dark', !d)", dark);
        await page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
        var other = await AxeViolations(page);
        Assert.True(other.Count == 0, "Panel open, other color mode:\n" + string.Join("\n", other));
        await page.EvaluateAsync("d => document.documentElement.classList.toggle('dark', d)", dark);

        await page.GetByTestId("user-menu").ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Appearance" }).ClickAsync();

        var appearance = page.GetByRole(AriaRole.Dialog, new() { Name = "Appearance" });
        await Assertions.Expect(appearance).ToBeVisibleAsync();
        await Assertions.Expect(panel).ToBeHiddenAsync();
        await appearance.GetByTestId("palette-emerald").ClickAsync();
        await Assertions.Expect(page.GetByTestId("palette-emerald")).ToHaveAttributeAsync("aria-pressed", "true");
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(appearance).ToBeHiddenAsync();
        await Assertions.Expect(toggle).ToBeFocusedAsync();
        await page.SetViewportSizeAsync(1280, 800);
    }

    /// <summary>The customers table: search, sort, a filter, pagination, selection, row actions and the new customer form with its remote country select.</summary>
    public static async Task AssertCustomers(IPage page)
    {
        var rows = page.Locator("table tbody tr[data-slot=row]");
        await Assertions.Expect(rows).ToHaveCountAsync(10);
        await Assertions.Expect(page.GetByText("1–10 of 240")).ToBeVisibleAsync();

        // Sort: a click on a header sorts, and the state is in the address.
        await page.GetByRole(AriaRole.Button, new() { Name = "Sort by Orders, ascending" }).ClickAsync();
        await page.WaitForURLAsync(new Regex(@"[?&]s="));
        var orders = (await rows.Locator("td:nth-child(7)").AllTextContentsAsync()).Select(t => int.Parse(t.Trim())).ToArray();
        Assert.Equal(orders.Order(), orders);

        // Pagination.
        await page.GetByLabel("Page 2", new() { Exact = true }).ClickAsync();
        await Assertions.Expect(page.GetByText("11–20 of 240")).ToBeVisibleAsync();

        // Search goes back to page 1 and narrows the table.
        await page.GetByRole(AriaRole.Searchbox).FillAsync("Foxglove");
        await Assertions.Expect(rows.First).ToContainTextAsync("Foxglove", new() { Timeout = 15_000 });
        var count = await rows.CountAsync();
        Assert.InRange(count, 1, 10);
        Assert.All(await rows.AllTextContentsAsync(), text => Assert.Contains("Foxglove", text));
        await page.GetByRole(AriaRole.Searchbox).FillAsync("");
        await Assertions.Expect(page.GetByText("of 240")).ToBeVisibleAsync();

        // A text filter from the Add filter menu: it is a tag, it is in the address and it narrows the rows.
        await page.GetByRole(AriaRole.Button, new() { Name = "Add filter" }).ClickAsync();
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Add filter" });
        await panel.GetByRole(AriaRole.Button, new() { Name = "Company", Exact = true }).ClickAsync();
        await panel.GetByLabel("Value for Company").PressSequentiallyAsync("Kestrel", new() { Delay = 20 });
        await page.WaitForURLAsync(new Regex(@"f\.company=Kestrel"));
        await Assertions.Expect(page.Locator("[data-slot=filter-tag-button]", new() { HasTextString = "Company contains Kestrel" })).ToBeVisibleAsync();
        await Assertions.Expect(rows.First).ToContainTextAsync("Kestrel");
        Assert.All(await rows.AllTextContentsAsync(), text => Assert.Contains("Kestrel", text));
        await page.GetByRole(AriaRole.Button, new() { Name = "Clear filters" }).ClickAsync();
        await Assertions.Expect(page.GetByText("of 240")).ToBeVisibleAsync();

        // Selection: the page, then one row less. The mixed state of the header box says "some".
        await page.Locator("input[data-select=all]").ClickAsync();
        await Assertions.Expect(page.Locator("[data-slot=selection-status]")).ToHaveTextAsync("10 selected");
        await rows.First.GetByRole(AriaRole.Checkbox).UncheckAsync();
        await Assertions.Expect(page.Locator("[data-slot=selection-status]")).ToHaveTextAsync("9 selected");
        await Assertions.Expect(page.Locator("input[data-select=all]")).ToHaveAttributeAsync("data-indeterminate", new Regex(".*"));
        // The selection survives the next page.
        await page.GetByLabel("Page 2", new() { Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("[data-slot=selection-status]")).ToHaveTextAsync("9 selected");
        // Delete the selection, after a confirmation.
        await page.Locator("[data-slot=selection-bar] [data-action=delete]").ClickAsync();
        await page.Locator("[data-slot=action-dialog] [data-slot=action-confirm]").ClickAsync();
        await Assertions.Expect(page.GetByText("of 231")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("[data-slot=selection-bar]")).ToHaveCountAsync(0);

        // Row action: delete one customer.
        await rows.First.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Actions for") }).ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Delete customer" }).ClickAsync();
        await Assertions.Expect(page.GetByText("of 230")).ToBeVisibleAsync();

        // New customer: the form validates, the country is searched in a remote source, and the customer is in the table.
        await page.GetByTestId("new-customer-button").ClickAsync();
        var form = page.GetByTestId("new-customer");
        await form.GetByTestId("new-create").ClickAsync();
        await Assertions.Expect(form.GetByText("The name is required.")).ToBeVisibleAsync();
        await form.GetByTestId("new-name").FillAsync("Zora Quentin");
        await form.GetByTestId("new-email").FillAsync("zora@quentin.example");
        await form.GetByTestId("new-create").ClickAsync();
        await Assertions.Expect(form.GetByText("Pick a country.")).ToBeVisibleAsync();
        await form.GetByTestId("new-country").ClickAsync();
        await page.GetByRole(AriaRole.Searchbox).Last.FillAsync("Fin");
        await page.GetByRole(AriaRole.Option, new() { Name = "Finland" }).ClickAsync();
        await form.GetByTestId("new-create").ClickAsync();
        await Assertions.Expect(form).Not.ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Searchbox).FillAsync("Zora Quentin");
        await Assertions.Expect(rows).ToHaveCountAsync(1, new() { Timeout = 15_000 });
        await Assertions.Expect(rows.First).ToContainTextAsync("Finland");
    }

    /// <summary>
    /// The "New customer" entry of the create menu opens the form from any page, also from the customers page itself, and leaves no <c>new</c> parameter behind:
    /// a reload or Back does not open the form again.
    /// </summary>
    public static async Task AssertCreateMenu(IPage page)
    {
        var form = page.GetByTestId("new-customer");
        async Task ChooseNewCustomer()
        {
            await page.GetByTestId("create-button").ClickAsync();
            await page.GetByRole(AriaRole.Menuitem, new() { Name = "New customer" }).ClickAsync();
        }

        // From another page: the customers page opens with the form, and its address is clean.
        await page.GotoAsync("");
        await WaitInteractive(page);
        await ChooseNewCustomer();
        await page.WaitForURLAsync(new Regex("customers$"));
        await Assertions.Expect(form).ToBeVisibleAsync();
        Assert.DoesNotContain("new=", page.Url);
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(form).Not.ToBeVisibleAsync();
        // A reload does not open it again.
        await page.ReloadAsync();
        await WaitInteractive(page);
        await Assertions.Expect(form).Not.ToBeVisibleAsync();

        // From the customers page itself, more than once.
        for (var i = 0; i < 2; i++)
        {
            await ChooseNewCustomer();
            await Assertions.Expect(form).ToBeVisibleAsync();
            Assert.DoesNotContain("new=", page.Url);
            await page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(form).Not.ToBeVisibleAsync();
        }

        // Back does not open it either: the address was replaced, not added.
        await page.GoBackAsync();
        await WaitInteractive(page);
        await page.GoForwardAsync();
        await WaitInteractive(page);
        await Assertions.Expect(form).Not.ToBeVisibleAsync();
    }

    /// <summary>
    /// The selection follows the query: it survives a page change but only the selected rows that the search or the filters still match stay selected, and the count
    /// and the confirmation say so. The announcement of the count is a live region that is always in the page.
    /// </summary>
    public static async Task AssertSelectionFollowsTheQuery(IPage page)
    {
        await page.GotoAsync("customers");
        await WaitInteractive(page);
        var rows = page.Locator("table tbody tr[data-slot=row]");
        await Assertions.Expect(rows).ToHaveCountAsync(10);

        // Nothing selected: the live region is there, with no text.
        var live = page.Locator("[data-slot=selection-status]");
        await Assertions.Expect(live).ToHaveAttributeAsync("role", "status");
        await Assertions.Expect(live).ToHaveTextAsync("");
        await Assertions.Expect(page.Locator("[data-slot=selection-bar]")).ToHaveCountAsync(0);

        await page.Locator("input[data-select=all]").ClickAsync();
        await Assertions.Expect(live).ToHaveTextAsync("10 selected");
        var companies = (await rows.Locator("td:nth-child(3)").AllTextContentsAsync()).Select(t => t.Trim()).ToArray();
        var company = companies[0];
        var expected = companies.Count(c => c == company);
        Assert.InRange(expected, 1, 9);

        // A search that leaves only some of them: the others are no longer selected.
        await page.GetByRole(AriaRole.Searchbox).FillAsync(company);
        await Assertions.Expect(live).ToHaveTextAsync($"{expected} selected", new() { Timeout = 15_000 });
        Assert.All(await rows.AllTextContentsAsync(), text => Assert.Contains(company, text));

        // The confirmation counts the pruned selection, and the deletion reaches only those rows.
        await page.Locator("[data-slot=selection-bar] [data-action=delete]").ClickAsync();
        await Assertions.Expect(page.Locator("[data-slot=action-dialog]")).ToContainTextAsync($"{expected} customers will be removed");
        await page.Locator("[data-slot=action-dialog] [data-slot=action-confirm]").ClickAsync();
        await Assertions.Expect(live).ToHaveTextAsync("");
        await page.GetByRole(AriaRole.Searchbox).FillAsync("");
        await Assertions.Expect(page.GetByText($"of {240 - expected}")).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // A filter prunes too, and a page change does not.
        await page.Locator("input[data-select=all]").ClickAsync();
        await Assertions.Expect(live).ToHaveTextAsync("10 selected");
        await page.GetByLabel("Page 2", new() { Exact = true }).ClickAsync();
        await Assertions.Expect(live).ToHaveTextAsync("10 selected");
        await page.GetByRole(AriaRole.Searchbox).FillAsync("zzzzqq");
        await Assertions.Expect(live).ToHaveTextAsync("", new() { Timeout = 15_000 });
        await Assertions.Expect(page.Locator("[data-slot=selection-bar]")).ToHaveCountAsync(0);
    }

    /// <summary>
    /// "Select all N results" on the Delete action: two rows ticked, then every result of a search; the confirmation says the real count, the deletion removes exactly the matching rows,
    /// and the keyboard focus lands on the select-all box of the table instead of falling to the page when the Delete button leaves with the bar.
    /// </summary>
    public static async Task AssertDeleteEveryResult(IPage page)
    {
        await page.GotoAsync("customers");
        await WaitInteractive(page);
        var rows = page.Locator("table tbody tr[data-slot=row]");
        await Assertions.Expect(rows).ToHaveCountAsync(10);
        var total = await page.Locator("[data-slot=result-count]").First.TextContentAsync();
        var before = int.Parse(Regex.Match(total!, @"of ([\d,]+)").Groups[1].Value.Replace(",", ""));

        // A search that matches more than a page.
        await page.GetByRole(AriaRole.Searchbox).FillAsync("an");
        await Assertions.Expect(page.Locator("[data-slot=result-count]").First).Not.ToHaveTextAsync(total!, new() { Timeout = 15_000 });
        var matches = int.Parse(Regex.Match((await page.Locator("[data-slot=result-count]").First.TextContentAsync())!, @"of ([\d,]+)").Groups[1].Value.Replace(",", ""));
        Assert.InRange(matches, 12, before - 1);

        await rows.Nth(0).GetByRole(AriaRole.Checkbox).CheckAsync();
        await rows.Nth(1).GetByRole(AriaRole.Checkbox).CheckAsync();
        await Assertions.Expect(page.Locator("[data-slot=selection-status]")).ToHaveTextAsync("2 selected");
        await page.GetByRole(AriaRole.Button, new() { Name = $"Select all {matches} results" }).ClickAsync();
        await Assertions.Expect(page.Locator("[data-slot=selection-status]")).ToHaveTextAsync($"All {matches} results selected");

        // With the keyboard: Delete, then the confirm button.
        var delete = page.Locator("[data-slot=selection-bar] [data-action=delete]");
        await delete.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        var dialog = page.Locator("[data-slot=action-dialog]");
        await Assertions.Expect(dialog).ToContainTextAsync($"{matches} customers will be removed");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Assertions.Expect(page.Locator("[data-slot=selection-bar]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
        await Assertions.Expect(page.Locator("[data-slot=selection-status]")).ToHaveTextAsync("");
        // The focus is not lost to the page: with nothing left to select it is on the table region of the list (the select-all box is disabled).
        await Assertions.Expect(page.Locator("[data-slot=table-root]").First).ToBeFocusedAsync(new() { Timeout = 15_000 });
        // Exactly the matching rows are gone: the search is empty now, and the others remain.
        await Assertions.Expect(page.Locator("[data-slot=result-count]").First).ToHaveTextAsync("No results for “an”", new() { Timeout = 15_000 });
        await page.GetByRole(AriaRole.Searchbox).FillAsync("");
        await Assertions.Expect(page.GetByText($"of {before - matches}")).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    /// <summary>
    /// Forms posted before the page is interactive (a native submit, here with JavaScript off): the server validates the posted model and answers with the page and
    /// its message, not with an error. Needs the antiforgery cookie that the first request sets.
    /// </summary>
    public static async Task AssertStaticPosts(IBrowser browser, string baseUrl)
    {
        await using var context = await browser.NewContextAsync(new() { BaseURL = baseUrl, JavaScriptEnabled = false });
        var failures = new List<string>();
        context.Response += (_, response) => { if (response.Status >= 400) lock (failures) failures.Add($"{response.Status} {response.Url}"); };
        var page = await context.NewPageAsync();

        // Profile: invalid, then valid.
        await page.GotoAsync("settings");
        await page.GetByTestId("profile-name").FillAsync("");
        await page.GetByTestId("profile-save").ClickAsync();
        await Assertions.Expect(page.GetByText("The name is required.")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("profile-status")).ToHaveTextAsync("");
        await page.GetByTestId("profile-name").FillAsync("Maren A.");
        await page.GetByTestId("profile-save").ClickAsync();
        await Assertions.Expect(page.GetByTestId("profile-status")).ToHaveTextAsync("Profile saved.");
        await Assertions.Expect(page.GetByTestId("profile-name")).ToHaveValueAsync("Maren A.");

        // Password: the rules of the model apply.
        await page.GotoAsync("settings/security");
        await page.GetByTestId("password-current").FillAsync("old-secret");
        await page.GetByTestId("password-new").FillAsync("short");
        await page.GetByTestId("password-confirm").FillAsync("different");
        await page.GetByTestId("password-save").ClickAsync();
        await Assertions.Expect(page.GetByText("Use at least 8 characters.")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("The two passwords differ.")).ToBeVisibleAsync();
        await page.GetByTestId("password-current").FillAsync("old-secret");
        await page.GetByTestId("password-new").FillAsync("a-longer-secret");
        await page.GetByTestId("password-confirm").FillAsync("a-longer-secret");
        await page.GetByTestId("password-save").ClickAsync();
        await Assertions.Expect(page.GetByTestId("password-status")).ToHaveTextAsync("Password updated.");

        // A synthetic click on the submit button (the slideover is closed without a script, so the button cannot be pressed), then the page the post answers with.
        async Task Submit()
        {
            var answer = page.WaitForResponseAsync(r => r.Request.Method == "POST");
            await page.GetByTestId("new-create").DispatchEventAsync("click");
            await answer;
            await page.WaitForLoadStateAsync(LoadState.Load);
        }

        // New customer: the form is in a slideover that only a script opens, so the controls are filled where they are. The server validates what was posted.
        await page.GotoAsync("customers");
        await Submit();
        await Assertions.Expect(page.GetByText("The name is required.")).ToHaveCountAsync(1);
        // The controls are not visible, so they cannot be typed in: their values are set directly.
        await page.GetByTestId("new-name").EvaluateAsync("e => { e.value = 'Zora Quentin'; }");
        await page.GetByTestId("new-email").EvaluateAsync("e => { e.value = 'zora@quentin.example'; }");
        await Submit();
        await Assertions.Expect(page.GetByText("The name is required.")).ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByText("Pick a country.")).ToHaveCountAsync(1);

        lock (failures) Assert.True(failures.Count == 0, "Failed requests:\n" + string.Join("\n", failures));
    }

    /// <summary>The inbox: a message opens in the pane (and is read), the filter, the archive, and one panel at a time on a phone.</summary>
    public static async Task AssertInbox(IPage page)
    {
        await Assertions.Expect(page.GetByTestId("mail-list").GetByRole(AriaRole.Listitem)).ToHaveCountAsync(12);
        await Assertions.Expect(page.GetByTestId("mail-empty")).ToBeVisibleAsync();
        var unread = await page.GetByTestId("unread-count").TextContentAsync();
        Assert.Equal("3 unread", unread);

        await page.GetByTestId("mail-1").ClickAsync();
        await page.WaitForURLAsync(new Regex("inbox/1$"));
        await Assertions.Expect(page.GetByTestId("mail-article").GetByRole(AriaRole.Heading, new() { Level = 2 })).ToHaveTextAsync("Renewal for the Business plan");
        await Assertions.Expect(page.GetByTestId("unread-count")).ToHaveTextAsync("2 unread");

        // Reply.
        await page.GetByTestId("mail-reply").FillAsync("Thanks, a quote is on its way.");
        await page.GetByTestId("mail-send").ClickAsync();
        await Assertions.Expect(page.GetByText("Reply sent")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("mail-reply")).ToHaveValueAsync("");

        // Unread filter, then archive: the message leaves the list and the address goes back to it.
        await page.GetByTestId("filter-unread").ClickAsync();
        await Assertions.Expect(page.GetByTestId("mail-list").GetByRole(AriaRole.Listitem)).ToHaveCountAsync(2);
        await page.GetByTestId("filter-all").ClickAsync();
        await page.GetByTestId("mail-5").ClickAsync();
        await page.GetByTestId("mail-archive").ClickAsync();
        await Assertions.Expect(page.GetByTestId("mail-list").GetByRole(AriaRole.Listitem)).ToHaveCountAsync(11);
        await Assertions.Expect(page.GetByTestId("mail-empty")).ToBeVisibleAsync();

        // A phone shows one panel at a time: the message with a way back, then the list.
        await page.SetViewportSizeAsync(390, 800);
        await Assertions.Expect(page.GetByTestId("mail-list-panel")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("mail-detail")).Not.ToBeVisibleAsync();
        await page.GetByTestId("mail-2").ClickAsync();
        await Assertions.Expect(page.GetByTestId("mail-detail")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("mail-list-panel")).Not.ToBeVisibleAsync();
        await page.GetByTestId("mail-back").ClickAsync();
        await Assertions.Expect(page.GetByTestId("mail-list-panel")).ToBeVisibleAsync();
        // The sidebar is off-canvas there: its toggle opens it.
        await page.SetViewportSizeAsync(1280, 800);
    }

    /// <summary>The four settings pages: forms that validate and save, the members with an invitation from a remote directory, the switches, the sessions.</summary>
    public static async Task AssertSettings(IPage page)
    {
        await Assertions.Expect(page.GetByTestId("settings-general")).ToBeVisibleAsync();
        await page.GetByTestId("profile-name").FillAsync("");
        await page.GetByTestId("profile-save").ClickAsync();
        await Assertions.Expect(page.GetByText("The name is required.")).ToBeVisibleAsync();
        await page.GetByTestId("profile-name").FillAsync("Maren A.");
        // The field reports its value on change: wait for the message to go before pressing Save.
        await page.GetByTestId("profile-name").PressAsync("Tab");
        await Assertions.Expect(page.GetByText("The name is required.")).ToHaveCountAsync(0);
        await page.GetByTestId("profile-save").ClickAsync();
        await Assertions.Expect(page.GetByText("Profile saved", new() { Exact = true })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Navigation, new() { Name = "Settings sections" }).GetByRole(AriaRole.Link, new() { Name = "Members" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("settings-members")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("member")).ToHaveCountAsync(6);
        await page.GetByTestId("member-filter").FillAsync("tobias");
        await Assertions.Expect(page.GetByTestId("member")).ToHaveCountAsync(1);
        await page.GetByTestId("member-filter").FillAsync("");
        await page.GetByTestId("invite-button").ClickAsync();
        var dialog = page.GetByTestId("invite-dialog");
        await dialog.GetByTestId("invite-people").ClickAsync();
        await page.GetByRole(AriaRole.Searchbox).Last.FillAsync("Alma");
        await page.GetByRole(AriaRole.Option).First.ClickAsync();
        await page.Keyboard.PressAsync("Escape");
        await dialog.GetByTestId("invite-send").ClickAsync();
        await Assertions.Expect(page.GetByTestId("member")).ToHaveCountAsync(7);
        await Assertions.Expect(page.GetByTestId("member-count")).ToHaveTextAsync("7 people");

        await page.GetByRole(AriaRole.Navigation, new() { Name = "Settings sections" }).GetByRole(AriaRole.Link, new() { Name = "Notifications" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("settings-notifications")).ToBeVisibleAsync();
        await page.GetByTestId("channel-email").ClickAsync();
        await page.GetByTestId("notification-save").ClickAsync();
        await Assertions.Expect(page.GetByText("No channel is on")).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Navigation, new() { Name = "Settings sections" }).GetByRole(AriaRole.Link, new() { Name = "Security" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("settings-security")).ToBeVisibleAsync();
        await page.GetByTestId("password-current").FillAsync("old-secret");
        await page.GetByTestId("password-new").FillAsync("short");
        await page.GetByTestId("password-confirm").FillAsync("different");
        await page.GetByTestId("password-save").ClickAsync();
        await Assertions.Expect(page.GetByText("Use at least 8 characters.")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("The two passwords differ.")).ToBeVisibleAsync();
        await page.GetByTestId("password-new").FillAsync("a-longer-secret");
        await page.GetByTestId("password-confirm").FillAsync("a-longer-secret");
        await page.GetByTestId("password-confirm").PressAsync("Tab");
        await Assertions.Expect(page.GetByText("The two passwords differ.")).ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByText("Use at least 8 characters.")).ToHaveCountAsync(0);
        await page.GetByTestId("password-save").ClickAsync();
        await Assertions.Expect(page.GetByText("Password updated", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTestId("session")).ToHaveCountAsync(3);
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign out Safari on iPhone" }).ClickAsync();
        await Assertions.Expect(page.GetByTestId("session")).ToHaveCountAsync(2);
    }

    /// <summary>Every page of the app, scanned with axe in the current color mode, once it is interactive and nothing moves.</summary>
    public static async Task AssertAccessible(IPage page, string mode)
    {
        foreach (var path in new[] { "", "customers", "inbox/2", "settings", "settings/members", "settings/notifications", "settings/security" })
        {
            await page.GotoAsync(path);
            await WaitInteractive(page);
            await page.WaitForFunctionAsync("() => document.title.length > 0 && document.getAnimations().length === 0");
            var result = await page.RunAxe();
            var violations = result.Violations
                .SelectMany(v => v.Nodes.Select(n => $"{v.Id}: {n.Html} -> {string.Join("; ", n.Any.Select(c => c.Message))}"))
                .ToList();
            Assert.True(violations.Count == 0, $"{mode} mode, /{path}:\n" + string.Join("\n", violations));
        }
    }

    /// <summary>The page has no horizontal scroll at the width of a small phone.</summary>
    public static async Task AssertNoHorizontalScroll(IPage page)
    {
        await page.SetViewportSizeAsync(375, 800);
        var sizes = await page.EvaluateAsync<int[]>("() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
        Assert.True(sizes[0] <= sizes[1], $"horizontal scroll at 375px: scrollWidth {sizes[0]} > clientWidth {sizes[1]}");
        await page.SetViewportSizeAsync(1280, 800);
    }
}

/// <summary>
/// The Dashboard template as a user ships it: <c>dotnet publish -c Release</c>, run in Production at the root and below a path base. Started by
/// <c>build/verify-templates.mjs</c> with <c>BLUX_TEMPLATE_PUBLISHED</c>, <c>BLUX_TEMPLATE_ASSEMBLY</c> and <c>BLUX_TEMPLATE_PROBE</c>; without them
/// the tests are skipped. Every page is checked in the browser; no request may fail and the console stays free of errors.
/// </summary>
public class DashboardTemplateTests(PlaygroundServer server)
{
    private static string? Published => Environment.GetEnvironmentVariable("BLUX_TEMPLATE_PUBLISHED");
    private static string? AssemblyName => Environment.GetEnvironmentVariable("BLUX_TEMPLATE_ASSEMBLY");
    private static string? Probe => Environment.GetEnvironmentVariable("BLUX_TEMPLATE_PROBE");

    [Theory]
    [InlineData(null)]
    [InlineData("/demo/dashboard")]
    public async Task Published_app_works_page_by_page(string? pathBase)
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

            await using var context = await server.Browser.NewContextAsync(new() { BaseURL = baseUrl, ViewportSize = new() { Width = 1280, Height = 800 } });
            await context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
            var failures = new List<string>();
            var consoleErrors = new List<string>();
            context.Response += (_, response) => { if (response.Status >= 400) lock (failures) failures.Add($"{response.Status} {response.Url}"); };
            var page = await context.NewPageAsync();
            page.Console += (_, message) => { if (message.Type == "error") lock (consoleErrors) consoleErrors.Add($"{message.Text} @ {message.Location}"); };
            page.PageError += (_, error) => { lock (consoleErrors) consoleErrors.Add(error); };

            await page.GotoAsync("");
            await Assertions.Expect(page.Locator("base")).ToHaveAttributeAsync("href", (pathBase ?? "").TrimEnd('/') + "/");
            await DashboardChecks.WaitInteractive(page);
            await DashboardChecks.AssertHome(page);
            await DashboardChecks.AssertNoHorizontalScroll(page);

            await page.GotoAsync("customers");
            await DashboardChecks.WaitInteractive(page);
            await DashboardChecks.AssertCustomers(page);

            await DashboardChecks.AssertCreateMenu(page);
            await DashboardChecks.AssertSelectionFollowsTheQuery(page);
            await DashboardChecks.AssertDeleteEveryResult(page);
            await DashboardChecks.AssertStaticPosts(server.Browser, baseUrl);

            await page.GotoAsync("inbox");
            await DashboardChecks.WaitInteractive(page);
            await DashboardChecks.AssertInbox(page);

            await page.GotoAsync("settings");
            await DashboardChecks.WaitInteractive(page);
            await DashboardChecks.AssertSettings(page);

            await page.GotoAsync("");
            await DashboardChecks.WaitInteractive(page);
            await DashboardChecks.AssertShell(page);
            await DashboardChecks.AssertMobilePanel(page);
            await DashboardChecks.AssertColorsInThePrerender(page, server.Browser, baseUrl, pathBase);

            lock (failures) Assert.True(failures.Count == 0, "Failed requests:\n" + string.Join("\n", failures));
            lock (consoleErrors) Assert.True(consoleErrors.Count == 0, "Console errors:\n" + string.Join("\n", consoleErrors));

            // Accessibility: no axe violation on any page, in the color mode the run ended in (the shell check changed it, and the primary color) and in the other one.
            var mode = async () => ((await page.Locator("html").GetAttributeAsync("class") ?? "").Contains("dark") ? "dark" : "light");
            await DashboardChecks.AssertAccessible(page, await mode());
            await page.GotoAsync("");
            await DashboardChecks.WaitInteractive(page);
            await page.Locator("[data-bx-color-mode=toggle]").First.ClickAsync();
            await page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
            await DashboardChecks.AssertAccessible(page, await mode());

            // A page that does not exist is the template's own, below the path base too.
            var missingUrl = new Uri(new Uri(baseUrl), "no-such-page").ToString();
            var missing = await page.GotoAsync("no-such-page");
            Assert.Equal(404, missing!.Status);
            await Assertions.Expect(page.GetByText("There is nothing at this address.")).ToBeVisibleAsync();

            if (!string.IsNullOrEmpty(Probe))
            {
                await page.GotoAsync("");
                var sheet = await page.Locator("link[rel=stylesheet][href*='app']").First.GetAttributeAsync("href");
                Assert.NotNull(sheet);
                using var http = new HttpClient();
                var css = await http.GetStringAsync(new Uri(new Uri(baseUrl), sheet), TestContext.Current.CancellationToken);
                Assert.Contains(Probe, css);
                // The classes of Blux.UI.Tools (the data list) are compiled in too: it has no stylesheet of its own.
                Assert.Contains("webkit-search-cancel-button", css);
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
