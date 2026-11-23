using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>
/// The column panel (visibility, show and hide all, reorder by drag and by keys, pins, search, reset), resizing by pointer and by keyboard, the cookie that keeps it (read by the server at the first
/// paint, a localStorage entry in a web view), the columns that stay fixed (selection, row actions), a phone, an enhanced navigation and accessibility in light and dark,
/// on <c>/components/data-list-columns</c>.
/// </summary>
public class DataListColumnsTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private const string Cookie = "blux-dl-cols";
    private const string DemoKey = "demo-cols";
    private const string FixedKey = "demo-cols-fixed";

    private async Task OpenPage(string mode, string query = "")
    {
        await Page.GotoAsync($"/{mode}/components/data-list-columns{query}");
        await WaitForList(mode);
    }

    private async Task WaitForList(string mode)
    {
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        if (mode != "ssr")
        {
            await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
        await Assertions.Expect(List.Locator("[data-slot=skeleton-row]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
        await Assertions.Expect(Fixed.Locator("[data-slot=skeleton-row]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
    }

    private ILocator List => Page.GetByTestId("list-columns");
    private ILocator Fixed => Page.GetByTestId("list-columns-fixed");
    // Both lists have a panel in the page: the one that is open is the visible one.
    private ILocator Content => Page.Locator("[data-slot=columns-content]:visible");
    private ILocator Headers(ILocator list) => list.Locator("thead th[data-key]");

    private static Task<string[]> Keys(ILocator list) => list.Locator("thead th[data-key]").EvaluateAllAsync<string[]>("els => els.map(e => e.getAttribute('data-key'))");

    private async Task OpenPanel(ILocator list)
    {
        await list.Locator("[data-slot=columns-trigger]").ClickAsync();
        await Assertions.Expect(Content).ToBeVisibleAsync();
        // The popover moves while it opens: a pointer test measures where its items are once it has stopped.
        await Page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
    }

    private Task<string?> CookieValue(string name) => Page.Context.CookiesAsync().ContinueWith(t => t.Result.FirstOrDefault(c => c.Name == name)?.Value);

    private async Task WaitForCookie(string name, string pattern)
    {
        await Page.WaitForFunctionAsync("args => new RegExp(args.pattern).test(document.cookie.split('; ').find(c => c.startsWith(args.name + '='))?.slice(args.name.length + 1) ?? '')",
            new { name, pattern }, new() { Timeout = 10_000 });
    }

    private async Task DarkMode()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await Page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
    }

    // ---- static rendering and the first paint

    [Fact]
    public async Task Statically_rendered_page_has_no_panel_and_no_handle_and_applies_the_cookie_in_its_first_html()
    {
        await OpenPage("ssr");
        await Assertions.Expect(List.Locator("[data-slot=columns-panel]")).ToHaveCountAsync(0);
        await Assertions.Expect(List.Locator("[data-slot=col-resize]")).ToHaveCountAsync(0);
        Assert.Equal(["id", "customer", "status", "amount", "issued"], await Keys(List));

        // Hide status, pin amount to the end, put issued first, widen customer.
        await Page.Context.AddCookiesAsync([new() { Name = Cookie, Value = $"{DemoKey}.v1!oissued~id~customer~status~amount!hstatus!eamount!wcustomer*300", Url = BaseUrl }]);

        // What the browser gets before any script runs: the page is already in the columns of the user, and the response is marked private and varies on Cookie.
        var response = await Page.APIRequest.GetAsync("/ssr/components/data-list-columns");
        Assert.Equal(200, response.Status);
        Assert.Contains("private", response.Headers.GetValueOrDefault("cache-control", ""), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Cookie", response.Headers.GetValueOrDefault("vary", ""), StringComparison.OrdinalIgnoreCase);
        var html = await response.TextAsync();
        var list = html[html.IndexOf("data-testid=\"list-columns\"", StringComparison.Ordinal)..];
        list = list[..list.IndexOf("data-testid=\"list-columns-fixed\"", StringComparison.Ordinal)];
        var keys = Regex.Matches(list, "<th [^>]*data-key=\"([a-z]+)\"").Select(m => m.Groups[1].Value).ToList();
        Assert.Equal(["id", "issued", "customer", "amount"], keys);
        Assert.Matches("<col data-col-key=\"customer\" style=\"width:300px\"", list);
        Assert.DoesNotContain("data-col-key=\"status\"", list);

        await Page.ReloadAsync();
        Assert.Equal(["id", "issued", "customer", "amount"], await Keys(List));
        await Assertions.Expect(List.Locator("thead th[data-key=amount]")).ToHaveAttributeAsync("data-pinned", "end");
        await Assertions.Expect(List.Locator("thead th[data-key=id]")).ToHaveAttributeAsync("data-pinned", "start");
        // The widths hold at the first paint: the header cell is 300 px wide.
        var width = await List.Locator("thead th[data-key=customer]").EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        Assert.InRange(width, 299, 301);
    }

    [Fact]
    public async Task A_cookie_with_keys_that_are_no_column_or_markup_changes_nothing_and_cannot_hide_the_required_column()
    {
        await Page.Context.AddCookiesAsync([new() { Name = Cookie, Value = $"{DemoKey}.v1!hcustomer~'3cb'3eoops~status!wnope*5000!oevil~id", Url = BaseUrl }]);
        await OpenPage("ssr");

        Assert.Equal(["id", "customer", "amount", "issued"], (await Keys(List)).Where(k => k != "status").ToArray());
        await Assertions.Expect(List.Locator("thead th[data-key=customer]")).ToHaveCountAsync(1);
        Assert.DoesNotContain("oops", await Page.ContentAsync());
    }

    // ---- the panel

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_panel_hides_pins_resizes_remembers_in_a_cookie_and_resets(string mode)
    {
        await OpenPage(mode);
        var before = Page.Url;

        await OpenPanel(List);
        // The required column is not offered.
        await Assertions.Expect(Content.Locator("[data-slot=columns-item]")).ToHaveCountAsync(4);
        await Assertions.Expect(Content.Locator("[data-slot=columns-item][data-col-key=customer]")).ToHaveCountAsync(0);
        await Assertions.Expect(Content.Locator("[data-slot=columns-count]")).ToHaveTextAsync("5 of 5 visible");

        await Content.Locator("input[data-col-toggle=status]").UncheckAsync();
        await Assertions.Expect(List.Locator("thead th[data-key=status]")).ToHaveCountAsync(0);
        await Assertions.Expect(Content.Locator("[data-slot=columns-count]")).ToHaveTextAsync("4 of 5 visible");
        await Assertions.Expect(List.Locator("[data-slot=columns-live]")).ToHaveTextAsync("Status hidden");

        await Content.Locator("[data-col-pin-end=amount]").ClickAsync();
        await Assertions.Expect(List.Locator("thead th[data-key=amount]")).ToHaveAttributeAsync("data-pinned", "end");
        await Assertions.Expect(Content.Locator("[data-col-pin-end=amount]")).ToHaveAttributeAsync("aria-pressed", "true");

        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(Content).ToBeHiddenAsync();

        // The browser changes the width at once (a column of 150 px grows by one step) and tells .NET once.
        var handle = List.Locator("[data-slot=col-resize][data-col-key=amount]");
        await handle.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(List.Locator("colgroup col[data-col-key=amount]")).ToHaveAttributeAsync("style", "width:166px", new() { Timeout = 10_000 });
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "166");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuetext", "166 pixels");

        await WaitForCookie(Cookie, $@"^{DemoKey}\.v1!hstatus!eamount!wamount\*166$");
        var cookie = (await Page.Context.CookiesAsync()).Single(c => c.Name == Cookie);
        Assert.Equal("Lax", cookie.SameSite.ToString());
        Assert.Equal("/", cookie.Path);
        Assert.False(cookie.HttpOnly);
        Assert.InRange(cookie.Expires, DateTimeOffset.UtcNow.AddDays(360).ToUnixTimeSeconds(), DateTimeOffset.UtcNow.AddDays(366).ToUnixTimeSeconds());

        // The columns are never in the address.
        Assert.Equal(before, Page.Url);

        await Page.ReloadAsync();
        await WaitForList(mode);
        await Assertions.Expect(List.Locator("thead th[data-key=status]")).ToHaveCountAsync(0);
        await Assertions.Expect(List.Locator("thead th[data-key=amount]")).ToHaveAttributeAsync("data-pinned", "end");
        await Assertions.Expect(List.Locator("colgroup col[data-col-key=amount]")).ToHaveAttributeAsync("style", "width:166px");

        await OpenPanel(List);
        await Assertions.Expect(Content.Locator("[data-slot=columns-count]")).ToHaveTextAsync("4 of 5 visible");
        await Content.Locator("[data-slot=columns-reset]").ClickAsync();
        await Assertions.Expect(List.Locator("thead th[data-key=status]")).ToHaveCountAsync(1);
        await Assertions.Expect(List.Locator("colgroup col[data-col-key=amount]")).ToHaveAttributeAsync("style", "width:150px");
        await Assertions.Expect(List.Locator("[data-slot=columns-live]")).ToHaveTextAsync("Columns reset");
        await Page.WaitForFunctionAsync("name => !document.cookie.split('; ').some(c => c.startsWith(name + '='))", Cookie, new() { Timeout = 10_000 });
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Show_all_and_hide_all_keep_the_required_column(string mode)
    {
        await OpenPage(mode);
        await OpenPanel(List);

        await Content.Locator("[data-slot=columns-hide-all]").ClickAsync();
        // Only the required column stays.
        await Assertions.Expect(Headers(List)).ToHaveCountAsync(1);
        Assert.Equal(["customer"], await Keys(List));
        await Assertions.Expect(Content.Locator("[data-slot=columns-count]")).ToHaveTextAsync("1 of 5 visible");
        await Assertions.Expect(Content.Locator("[data-slot=columns-hide-all]")).ToBeDisabledAsync();

        await Content.Locator("[data-slot=columns-show-all]").ClickAsync();
        await Assertions.Expect(Headers(List)).ToHaveCountAsync(5);
        await Assertions.Expect(Content.Locator("[data-slot=columns-show-all]")).ToBeDisabledAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_panel_search_shows_from_its_threshold_and_filters_the_list(string mode)
    {
        await OpenPage(mode);
        await OpenPanel(List);

        // SearchThreshold is 3 in the example, the panel lists 4 columns.
        var search = Content.GetByRole(AriaRole.Searchbox, new() { Name = "Search columns…" });
        await search.FillAsync("iss");
        await Assertions.Expect(Content.Locator("[data-slot=columns-item]")).ToHaveCountAsync(1);
        await Assertions.Expect(Content.Locator("[data-slot=columns-item]").First).ToHaveAttributeAsync("data-col-key", "issued");
        await search.FillAsync("");
        await Assertions.Expect(Content.Locator("[data-slot=columns-item]")).ToHaveCountAsync(4);
    }

    // ---- the keyboard

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_whole_panel_and_the_resize_handles_work_with_the_keyboard_and_say_what_they_did(string mode)
    {
        await OpenPage(mode);

        // Open the panel from the trigger and reach its controls with Tab.
        await List.Locator("[data-slot=columns-trigger]").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Content).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Tab");
        Assert.True(await Page.EvaluateAsync<bool>("() => !!document.activeElement.closest('[data-slot=columns-content]')"), "Tab did not go into the panel");

        // Hide with Space.
        await Content.Locator("input[data-col-toggle=status]").FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await Assertions.Expect(List.Locator("thead th[data-key=status]")).ToHaveCountAsync(0);
        await Assertions.Expect(List.Locator("[data-slot=columns-live]")).ToHaveTextAsync("Status hidden");
        await Page.Keyboard.PressAsync("Space");
        await Assertions.Expect(List.Locator("thead th[data-key=status]")).ToHaveCountAsync(1);

        // Move down with Enter: the order changes, it is said, and the focus stays on that column's button.
        var down = Content.GetByRole(AriaRole.Button, new() { Name = "Move Status down" });
        await down.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(List.Locator("[data-slot=columns-live]")).ToHaveTextAsync("Status moved to position 4 of 5");
        Assert.Equal(["id", "customer", "amount", "status", "issued"], await Keys(List));
        Assert.Equal("Move Status down", await Page.EvaluateAsync<string>("() => document.activeElement.getAttribute('aria-label')"));
        // Status and Amount keep their controls in order: the panel lists id, amount, status, issued.
        await Assertions.Expect(Content.Locator("[data-slot=columns-item]").Nth(1)).ToHaveAttributeAsync("data-col-key", "amount");

        // Up again, then pin with Enter on a toggle button.
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Assertions.Expect(Content.GetByRole(AriaRole.Button, new() { Name = "Move Status up" })).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(List.Locator("[data-slot=columns-live]")).ToHaveTextAsync("Status moved to position 3 of 5");
        Assert.Equal(["id", "customer", "status", "amount", "issued"], await Keys(List));
        await Assertions.Expect(Content.GetByRole(AriaRole.Button, new() { Name = "Move Status up" })).ToBeFocusedAsync();

        var pin = Content.GetByRole(AriaRole.Button, new() { Name = "Pin Status to start" });
        await pin.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(pin).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(List.Locator("[data-slot=columns-live]")).ToHaveTextAsync("Status pinned to the start");
        await Assertions.Expect(pin).ToBeFocusedAsync();

        // The panel closes with Escape and the focus goes back to its button.
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(Content).ToBeHiddenAsync();
        await Assertions.Expect(List.Locator("[data-slot=columns-trigger]")).ToBeFocusedAsync();

        // The handle is reached with Tab from the header, has a name and limits, and the arrows resize.
        var handle = List.Locator("[data-slot=col-resize][data-col-key=status]");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-label", "Resize Status");
        await handle.FocusAsync();
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "140");
        await Page.Keyboard.PressAsync("Shift+ArrowRight");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "204");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "188");
        await Page.Keyboard.PressAsync("Home");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "64");
        await Page.Keyboard.PressAsync("End");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "900");
        await Page.Keyboard.PressAsync("Delete");
        await Assertions.Expect(handle).ToHaveAttributeAsync("aria-valuenow", "140", new() { Timeout = 10_000 });
        // The browser put the width back at once; whether .NET re-rendered the attribute or not, the column is 140 px.
        await Assertions.Expect(List.Locator("thead th[data-key=status]")).ToHaveCSSAsync("width", "140px", new() { Timeout = 10_000 });
        await Assertions.Expect(List.Locator("[data-slot=col-live]")).ToHaveTextAsync("Status: default width", new() { Timeout = 10_000 });
    }

    // ---- the pointer

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Dragging_a_handle_resizes_a_column_and_dragging_a_grip_reorders_the_columns(string mode)
    {
        await OpenPage(mode);

        var handle = List.Locator("[data-slot=col-resize][data-col-key=status]");
        await handle.ScrollIntoViewIfNeededAsync();
        var box = (await handle.BoundingBoxAsync())!;
        var th = List.Locator("thead th[data-key=status]");
        var start = await th.EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + box.Width / 2 + 40, box.Y + box.Height / 2, new() { Steps = 4 });
        // While the pointer is down the browser follows it, and nothing was reported yet.
        var during = await th.EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        Assert.InRange(during, start + 38, start + 42);
        await Assertions.Expect(handle).ToHaveAttributeAsync("data-resizing", "true");
        await Page.Mouse.UpAsync();
        await Assertions.Expect(List.Locator("colgroup col[data-col-key=status]")).ToHaveAttributeAsync("style", new Regex(@"^width:18[0-2]px$"), new() { Timeout = 10_000 });
        await WaitForCookie(Cookie, @"wstatus\*18[0-2]");

        // Double click: back to the declared width.
        await handle.DblClickAsync();
        await Assertions.Expect(List.Locator("thead th[data-key=status]")).ToHaveCSSAsync("width", "140px", new() { Timeout = 10_000 });
        await Assertions.Expect(List.Locator("colgroup col[data-col-key=status]")).ToHaveAttributeAsync("style", new Regex(@"^width: ?140px;?$"), new() { Timeout = 10_000 });

        await OpenPanel(List);
        var grip = Content.Locator("[data-col-grip=amount]");
        var target = Content.Locator("[data-slot=columns-item][data-col-key=status]");
        var from = (await grip.BoundingBoxAsync())!;
        var to = (await target.BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(from.X + from.Width / 2, to.Y + 3, new() { Steps = 6 });
        await Assertions.Expect(target).ToHaveAttributeAsync("data-drop", "before");
        await Page.Mouse.UpAsync();
        await Assertions.Expect(List.Locator("[data-slot=columns-live]")).ToHaveTextAsync("Amount moved to position 3 of 5");
        Assert.Equal(["id", "customer", "amount", "status", "issued"], await Keys(List));
        await Assertions.Expect(Content.Locator("[data-slot=columns-item]").Nth(1)).ToHaveAttributeAsync("data-col-key", "amount");
    }

    [Fact]
    public async Task A_pinned_column_sticks_to_its_edge_while_the_table_scrolls_from_the_sm_breakpoint_up()
    {
        await Page.SetViewportSizeAsync(700, 800);
        await OpenPage("server");

        var region = List.Locator("[data-slot=table-root]");
        Assert.True(await region.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth"), "the table does not overflow at 700px");
        await region.EvaluateAsync("el => { el.scrollLeft = 120; }");
        var pinned = List.Locator("thead th[data-key=id]");
        var left = await pinned.EvaluateAsync<double>("el => el.getBoundingClientRect().left");
        var rootLeft = await region.EvaluateAsync<double>("el => el.getBoundingClientRect().left + el.clientLeft");
        Assert.InRange(left, rootLeft - 1, rootLeft + 1);
        Assert.Equal("sticky", await pinned.EvaluateAsync<string>("el => getComputedStyle(el).position"));
        // The cell of a row follows, so the column keeps its place under the header.
        var cell = List.Locator("tbody tr[data-slot=row] td[data-pinned=start]").First;
        Assert.InRange(await cell.EvaluateAsync<double>("el => el.getBoundingClientRect().left"), rootLeft - 1, rootLeft + 1);
        // A pinned column does not paint over the others: the header of the next one scrolled under it.
        Assert.Equal("auto", await List.Locator("thead th[data-key=customer]").EvaluateAsync<string>("el => getComputedStyle(el).zIndex"));
    }

    // ---- selection and row actions

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_selection_and_row_actions_columns_are_not_in_the_panel_and_stay_while_the_others_move(string mode)
    {
        await OpenPage(mode);
        await OpenPanel(Fixed);

        await Assertions.Expect(Content.Locator("[data-slot=columns-item]")).ToHaveCountAsync(4);
        await Assertions.Expect(Content.Locator("[data-slot=columns-count]")).ToHaveTextAsync("4 of 4 visible");
        await Assertions.Expect(Headers(Fixed)).ToHaveCountAsync(6);

        await Content.Locator("[data-slot=columns-hide-all]").ClickAsync();
        await Assertions.Expect(Headers(Fixed)).ToHaveCountAsync(3);
        Assert.Equal(["select", "id", "actions"], await Keys(Fixed));
        // Nothing is required here: the first column stays, the last visible one cannot be hidden, and the panel says why.
        await Assertions.Expect(Content.Locator("input[data-col-toggle=id]")).ToBeDisabledAsync();
        await Assertions.Expect(Content.Locator("[data-slot=columns-hint]")).ToHaveTextAsync("At least one column stays visible.");
        await Assertions.Expect(Fixed.Locator("thead th[data-key=select] input[type=checkbox]")).ToBeVisibleAsync();
        await Content.Locator("[data-slot=columns-show-all]").ClickAsync();
        await Assertions.Expect(Headers(Fixed)).ToHaveCountAsync(6);

        await Content.Locator("[data-col-pin-start=customer]").ClickAsync();
        await Content.Locator("[data-col-pin-end=amount]").ClickAsync();
        await Assertions.Expect(Fixed.Locator("thead th[data-key=amount]")).ToHaveAttributeAsync("data-pinned", "end");
        Assert.Equal(["select", "customer", "id", "status", "amount", "actions"], await Keys(Fixed));
        await Page.Keyboard.PressAsync("Escape");

        // The fixed columns stick with the pinned ones; the actions column has a width the table knows.
        await Assertions.Expect(Fixed.Locator("thead th[data-key=select]")).ToHaveAttributeAsync("data-pinned", "start");
        await Assertions.Expect(Fixed.Locator("thead th[data-key=actions]")).ToHaveAttributeAsync("data-pinned", "end");
        await Page.WaitForFunctionAsync("() => parseFloat(getComputedStyle(document.querySelector('[data-testid=list-columns-fixed] [data-slot=table-root]')).getPropertyValue('--bx-actions-w')) > 0");

        // Ticking a row still works, the row action is still there, and a hidden column took none of them with it.
        await Fixed.Locator("tbody tr[data-slot=row]").First.Locator("input[type=checkbox]").CheckAsync();
        await Assertions.Expect(Fixed.Locator("tbody tr[data-slot=row]").First.GetByRole(AriaRole.Link, new() { Name = "Open" })).ToBeVisibleAsync();
        await WaitForCookie(Cookie, $@"^{FixedKey}\.v1!scustomer!eamount$");
    }

    // ---- a web view (MAUI)

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task In_a_web_view_the_preferences_are_kept_in_localStorage_and_no_cookie_is_written(string mode)
    {
        // The message bridge of blazor.webview.js, which is how Blux tells a web view from a browser (see HybridTests).
        var context = await NewContext(new());
        await context.AddInitScriptAsync("Object.defineProperty(window, 'external', { value: { sendMessage() {}, receiveMessage() {} }, configurable: true });");
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/{mode}/components/data-list-columns");
        await page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        await Assertions.Expect(page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        var list = page.GetByTestId("list-columns");
        await Assertions.Expect(list.Locator("[data-slot=skeleton-row]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });

        await list.Locator("[data-slot=columns-trigger]").ClickAsync();
        await page.Locator("[data-slot=columns-content]:visible input[data-col-toggle=status]").UncheckAsync();
        await Assertions.Expect(list.Locator("thead th[data-key=status]")).ToHaveCountAsync(0);

        await page.WaitForFunctionAsync("() => localStorage.getItem('blux-dl-cols') === 'demo-cols.v1!hstatus'");
        Assert.DoesNotContain(await page.Context.CookiesAsync(), c => c.Name.StartsWith("blux-dl-cols", StringComparison.Ordinal));

        // No server reads a cookie in a web view: the list asks the browser, and shows the columns of the user once it did.
        await page.ReloadAsync();
        await Assertions.Expect(page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        await Assertions.Expect(page.GetByTestId("list-columns").Locator("thead th[data-key=status]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
        await Assertions.Expect(page.GetByTestId("list-columns").Locator("thead th[data-key=amount]")).ToHaveCountAsync(1);
    }

    // ---- after an enhanced navigation

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_cookie_applies_after_an_enhanced_navigation_and_changes_still_work(string mode)
    {
        await Page.Context.AddCookiesAsync([new() { Name = Cookie, Value = $"{DemoKey}.v1!hissued!estatus", Url = BaseUrl }]);
        await Page.GotoAsync($"/{mode}/components/button");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        if (mode != "ssr") await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        await Page.EvaluateAsync("() => { window.__enhanced = new Promise((r) => { Blazor.addEventListener('enhancedload', () => setTimeout(r, 50), { once: true }); setTimeout(r, 8000); }); }");
        await Page.EvaluateAsync("address => Blazor.navigateTo(address)", $"/{mode}/components/data-list-columns");
        await Page.EvaluateAsync("() => window.__enhanced");
        await Assertions.Expect(List).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await WaitForList(mode);

        Assert.Equal(["id", "customer", "amount", "status"], await Keys(List));
        await Assertions.Expect(List.Locator("thead th[data-key=status]")).ToHaveAttributeAsync("data-pinned", "end");
        if (mode == "ssr") return;

        await OpenPanel(List);
        await Content.Locator("input[data-col-toggle=issued]").CheckAsync();
        await Assertions.Expect(List.Locator("thead th[data-key=issued]")).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("Escape");
        await WaitForCookie(Cookie, $@"^{DemoKey}\.v1!estatus$");

        // Away and back, again without a document load: the list is built again from the cookie.
        await Page.EvaluateAsync("() => { window.__enhanced = new Promise((r) => { Blazor.addEventListener('enhancedload', () => setTimeout(r, 50), { once: true }); setTimeout(r, 8000); }); }");
        await Page.EvaluateAsync("address => Blazor.navigateTo(address)", $"/{mode}/components/button");
        await Page.EvaluateAsync("() => window.__enhanced");
        await Page.EvaluateAsync("() => { window.__enhanced = new Promise((r) => { Blazor.addEventListener('enhancedload', () => setTimeout(r, 50), { once: true }); setTimeout(r, 8000); }); }");
        await Page.EvaluateAsync("address => Blazor.navigateTo(address)", $"/{mode}/components/data-list-columns");
        await Page.EvaluateAsync("() => window.__enhanced");
        await WaitForList(mode);
        Assert.Equal(["id", "customer", "amount", "issued", "status"], await Keys(List));
    }

    // ---- a phone

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task On_a_phone_the_page_fits_the_panel_has_large_targets_and_pins_do_not_stick(string mode)
    {
        await Page.SetViewportSizeAsync(375, 800);
        await OpenPage(mode);

        Assert.False(await Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth"), "the page scrolls sideways at 375px");
        // The pinned column keeps its place in the order, but scrolls with the table: it would leave no room otherwise.
        await Assertions.Expect(List.Locator("thead th[data-key=id]")).ToHaveAttributeAsync("data-pinned", "start");
        Assert.NotEqual("sticky", await List.Locator("thead th[data-key=id]").EvaluateAsync<string>("el => getComputedStyle(el).position"));
        if (mode == "ssr") return;

        // Its resize handle stays contained inside the header cell (max-sm:relative) rather than stretching over the whole table.
        var idThBox = (await List.Locator("thead th[data-key=id]").BoundingBoxAsync())!;
        var idHandleBox = (await List.Locator("[data-slot=col-resize][data-col-key=id]").BoundingBoxAsync())!;
        Assert.True(idHandleBox.Height <= idThBox.Height + 1, $"pinned resize handle height {idHandleBox.Height} exceeds header height {idThBox.Height}");

        await OpenPanel(List);
        var panel = (await Content.BoundingBoxAsync())!;
        Assert.True(panel.X >= 0 && panel.X + panel.Width <= 375.5, $"the panel is {panel.Width}px wide at x={panel.X}");
        Assert.True(panel.Y + panel.Height <= 800.5, "the panel is taller than the screen");
        // The box is 16 px, its label is the target.
        foreach (var selector in new[] { "[data-slot=columns-item][data-col-key=status] label", "[data-col-up=status]", "[data-col-down=status]", "[data-col-grip=status]" })
        {
            var target = (await Content.Locator(selector).First.BoundingBoxAsync())!;
            Assert.True(target.Width >= 24 && target.Height >= 24, $"{selector} is {target.Width}x{target.Height}");
        }
        foreach (var selector in new[] { "[data-col-up=status]", "[data-col-down=status]" })
        {
            var target = (await Content.Locator(selector).BoundingBoxAsync())!;
            Assert.True(target.Width >= 40 && target.Height >= 40, $"{selector} is {target.Width}x{target.Height} on a phone");
        }
        // The pin buttons are left out of the panel on a phone.
        await Assertions.Expect(Content.Locator("[data-col-pin-start=status]")).ToBeHiddenAsync();

        await Content.Locator("input[data-col-toggle=status]").UncheckAsync();
        await Assertions.Expect(List.Locator("thead th[data-key=status]")).ToHaveCountAsync(0);
        Assert.False(await Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth"), "the page scrolls sideways after hiding a column");
    }

    // ---- accessibility

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Columns_page_has_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await OpenPage(mode);
        await AssertNoAccessibilityViolations();
        if (mode != "ssr")
        {
            await OpenPanel(List);
            await AssertNoAccessibilityViolations();
            await Content.Locator("[data-col-pin-start=amount]").ClickAsync();
            await Content.Locator("input[data-col-toggle=issued]").UncheckAsync();
            await AssertNoAccessibilityViolations();
            await Page.Keyboard.PressAsync("Escape");
            await AssertNoAccessibilityViolations();
        }
        await DarkMode();
        await AssertNoAccessibilityViolations();
        if (mode != "ssr")
        {
            await OpenPanel(List);
            await AssertNoAccessibilityViolations();
            await Page.Keyboard.PressAsync("Escape");
            await OpenPanel(Fixed);
            await Content.Locator("[data-col-pin-end=amount]").ClickAsync();
            await AssertNoAccessibilityViolations();
        }
    }
}
