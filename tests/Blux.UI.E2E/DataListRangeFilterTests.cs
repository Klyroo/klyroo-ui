using System.Globalization;
using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>
/// The number and date filters of the data list on its playground page, in the four render modes. The number list ("list-number", URL prefix "num") filters the amount of 120 invoices
/// (0 to 700 by 10, a slider, a number format for the tag); the date list ("list-date", prefix "dat") filters the day they were issued (one every two days from 3 January 2026, ranges of 92 days
/// at most). Statically rendered the editors are links and GET forms, and a range is sent as two fields (f.key.from and f.key.to) that no script folds back into one key.
/// </summary>
public class DataListRangeFilterTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private async Task OpenList(string mode, string query = "")
    {
        await Page.GotoAsync($"/{mode}/components/data-list-filters{query}");
        await WaitForList(Page, mode);
    }

    private static async Task WaitForList(IPage page, string mode)
    {
        await page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        if (mode != "ssr")
        {
            await Assertions.Expect(page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
        await Assertions.Expect(page.Locator("[data-slot=skeleton-row]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
    }

    private ILocator Number => Page.GetByTestId("list-number");

    private ILocator Date => Page.GetByTestId("list-date");

    private static ILocator Rows(ILocator list) => list.Locator("tbody tr[data-slot=row]");

    private static ILocator Count(ILocator list) => list.Locator("[data-slot=result-count]");

    private static ILocator Tag(ILocator list, string name) => list.Locator("[data-slot=filter-tag] [data-slot=filter-tag-button]", new() { HasTextString = name });

    private static ILocator Tags(ILocator list) => list.Locator("[data-slot=filter-tag] [data-slot=filter-tag-button]");

    private static ILocator Editor(ILocator list, string name) => list.GetByRole(AriaRole.Dialog, new() { Name = name });

    private static ILocator AddButton(ILocator list) => list.GetByRole(AriaRole.Button, new() { Name = "Add filter" });

    private static ILocator AddPanel(ILocator list) => list.GetByRole(AriaRole.Dialog, new() { Name = "Add filter" });

    private static AriaRole Control(string mode) => mode == "ssr" ? AriaRole.Link : AriaRole.Button;

    private Task WaitForQuery(string expected) =>
        Page.WaitForURLAsync(url => Uri.UnescapeDataString(new Uri(url).Query) == expected, new() { Timeout = 15_000 });

    private static Task ToTop(ILocator trigger) => trigger.EvaluateAsync("e => e.scrollIntoView({ block: 'start' })");

    private async Task OpenTag(ILocator list, string name, string dialog)
    {
        await ToTop(Tag(list, name));
        await Tag(list, name).ClickAsync();
        await Assertions.Expect(Editor(list, dialog)).ToBeVisibleAsync();
    }

    // Opens the add filter menu and the editor of one filter: a click on its name once there is .NET to run it, its section of the menu statically rendered.
    private async Task AddFilter(ILocator list, string mode, string name)
    {
        await ToTop(AddButton(list));
        await AddButton(list).ClickAsync();
        await Assertions.Expect(AddPanel(list)).ToBeVisibleAsync();
        if (mode != "ssr") await AddPanel(list).GetByRole(AriaRole.Button, new() { Name = name, Exact = true }).ClickAsync();
    }

    // What a click on a link of the docs does: Blazor fetches the address and merges the response into the document (an enhanced navigation), or the interactive router loads the page. The wait is bounded because
    // an interactive page's router handles the address itself and the "enhancedload" event never comes.
    private async Task NavigateFromAnotherPage(string mode, string query)
    {
        await Page.GotoAsync($"/{mode}/components/button");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        // A navigation before the circuit or the runtime is up would be lost.
        if (mode != "ssr") await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        await Page.EvaluateAsync("() => { window.__enhanced = new Promise((r) => { Blazor.addEventListener('enhancedload', () => setTimeout(r, 50), { once: true }); setTimeout(r, 8000); }); }");
        await Page.EvaluateAsync("address => Blazor.navigateTo(address)", $"/{mode}/components/data-list-filters{query}");
        await Page.EvaluateAsync("() => window.__enhanced");
        // The page of the list is there (the address changes before the content, and the page left has no skeleton rows to wait for).
        await Assertions.Expect(Number).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await WaitForList(Page, mode);
    }

    // ---- number: add, type, operators

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_number_filter_is_added_from_the_menu_and_shows_as_a_tag(string mode)
    {
        await OpenList(mode);
        await AddFilter(Number, mode, "Amount");
        var field = AddPanel(Number).GetByLabel("Value for Amount");

        if (mode == "ssr")
        {
            await field.FillAsync("86.25");
            await AddPanel(Number).Locator("[data-slot=filter-editor][data-filter=amount] [data-slot=filter-form]").GetByRole(AriaRole.Button, new() { Name = "Apply" }).ClickAsync();
        }
        else
        {
            await field.FillAsync("86.25");
            await field.PressAsync("Enter");
        }

        await WaitForQuery("?num.f.amount=86.25");
        await Assertions.Expect(Tag(Number, "Amount is 86.25")).ToBeVisibleAsync();
        await Assertions.Expect(Count(Number)).ToHaveTextAsync("1–1 of 1");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Changing_the_operator_keeps_the_number_and_changes_the_rows(string mode)
    {
        await OpenList(mode, "?num.f.amount=500");
        await OpenTag(Number, "Amount is 500.00", "Edit filter Amount");

        if (mode == "ssr") await Editor(Number, "Edit filter Amount").GetByRole(AriaRole.Link, new() { Name = "is at least" }).ClickAsync();
        else await Editor(Number, "Edit filter Amount").GetByLabel("Operator for Amount").SelectOptionAsync("gte");

        await WaitForQuery("?num.f.amount.gte=500");
        await Assertions.Expect(Tag(Number, "Amount is at least 500.00")).ToBeVisibleAsync();
        await Assertions.Expect(Count(Number)).ToHaveTextAsync("1–10 of 33");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_range_of_numbers_has_a_from_and_a_to_field_and_either_end_can_be_left_open(string mode)
    {
        await OpenList(mode, "?num.f.amount.between=100..200");
        await Assertions.Expect(Tag(Number, "Amount is between 100.00 – 200.00")).ToBeVisibleAsync();
        await Assertions.Expect(Count(Number)).ToHaveTextAsync("1–10 of 21");
        await OpenTag(Number, "Amount is between 100.00 – 200.00", "Edit filter Amount");
        var panel = Editor(Number, "Edit filter Amount");

        if (mode == "ssr")
        {
            await panel.GetByLabel("Amount, to").FillAsync("150");
            await panel.GetByRole(AriaRole.Button, new() { Name = "Apply" }).ClickAsync();
            // Two fields of a GET form: no script folds them into one key, and the page reads both.
            await WaitForQuery("?num.f.amount.from=100&num.f.amount.to=150");
        }
        else
        {
            await panel.GetByLabel("Amount, to").FillAsync("150");
            await panel.GetByLabel("Amount, to").PressAsync("Enter");
            await WaitForQuery("?num.f.amount.between=100..150");
        }

        await Assertions.Expect(Tag(Number, "Amount is between 100.00 – 150.00")).ToBeVisibleAsync();
        await Assertions.Expect(Count(Number)).ToHaveTextAsync("1–9 of 9");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Moving_the_slider_applies_the_value_and_back_returns_to_the_one_before(string mode)
    {
        await OpenList(mode, "?num.f.amount=100");
        await OpenTag(Number, "Amount is 100.00", "Edit filter Amount");
        var slider = Editor(Number, "Edit filter Amount").GetByRole(AriaRole.Slider, new() { Name = "Amount, slider" });

        await slider.FocusAsync();
        await Page.Keyboard.PressAsync("End");

        await WaitForQuery("?num.f.amount=700");
        await Assertions.Expect(Tag(Number, "Amount is 700.00")).ToBeVisibleAsync();
        await Page.GoBackAsync();
        await WaitForQuery("?num.f.amount=100");
        await Assertions.Expect(Tag(Number, "Amount is 100.00")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Statically_rendered_the_number_editor_has_no_slider()
    {
        await OpenList("ssr", "?num.f.amount=100");
        await OpenTag(Number, "Amount is 100.00", "Edit filter Amount");

        await Assertions.Expect(Editor(Number, "Edit filter Amount").GetByRole(AriaRole.Slider)).ToHaveCountAsync(0);
        await Assertions.Expect(Editor(Number, "Edit filter Amount").GetByLabel("Value for Amount")).ToHaveAttributeAsync("type", "number");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_typed_address_loads_the_number_and_drops_what_does_not_parse(string mode)
    {
        await OpenList(mode, "?num.f.amount.gt=400&num.f.amount.lt=abc");

        await Assertions.Expect(Tags(Number)).ToHaveCountAsync(1);
        await Assertions.Expect(Tag(Number, "Amount is greater than 400.00")).ToBeVisibleAsync();
        await Assertions.Expect(Count(Number)).ToHaveTextAsync("1–10 of 53");
    }

    // ---- date: add, type, operators

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_day_is_added_from_the_menu_and_shows_as_a_tag(string mode)
    {
        await OpenList(mode);
        await AddFilter(Date, mode, "Issued");
        var field = AddPanel(Date).GetByLabel("Value for Issued");

        await field.FillAsync("2026-01-03");
        if (mode == "ssr") await AddPanel(Date).Locator("[data-filter=issued] [data-slot=filter-form]").GetByRole(AriaRole.Button, new() { Name = "Apply" }).ClickAsync();

        await WaitForQuery("?dat.f.issued=2026-01-03");
        await Assertions.Expect(Tag(Date, "Issued is 1/3/2026")).ToBeVisibleAsync();
        await Assertions.Expect(Count(Date)).ToHaveTextAsync("1–1 of 1");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Changing_the_operator_of_a_date_keeps_the_day_and_changes_the_rows(string mode)
    {
        await OpenList(mode, "?dat.f.issued=2026-01-11");
        await OpenTag(Date, "Issued is 1/11/2026", "Edit filter Issued");

        if (mode == "ssr") await Editor(Date, "Edit filter Issued").GetByRole(AriaRole.Link, new() { Name = "is before" }).ClickAsync();
        else await Editor(Date, "Edit filter Issued").GetByLabel("Operator for Issued").SelectOptionAsync("before");

        // Issued on 3, 5, 7 and 9 January.
        await WaitForQuery("?dat.f.issued.before=2026-01-11");
        await Assertions.Expect(Tag(Date, "Issued is before 1/11/2026")).ToBeVisibleAsync();
        await Assertions.Expect(Count(Date)).ToHaveTextAsync("1–4 of 4");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_range_of_days_filters_with_both_ends_included(string mode)
    {
        await OpenList(mode, "?dat.f.issued.between=2026-01-03..2026-01-09");

        await Assertions.Expect(Tag(Date, "Issued is between 1/3/2026 – 1/9/2026")).ToBeVisibleAsync();
        await Assertions.Expect(Count(Date)).ToHaveTextAsync("1–4 of 4");
    }

    // ---- date: presets, the range calendar

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_preset_of_a_range_writes_the_dates_of_the_period_in_the_time_zone_of_the_user(string mode)
    {
        await OpenList(mode, "?dat.f.issued.between=2026-01-03..2026-01-09");
        await OpenTag(Date, "Issued is between 1/3/2026 – 1/9/2026", "Edit filter Issued");

        await Editor(Date, "Edit filter Issued").GetByRole(Control(mode), new() { Name = "Last 7 days" }).ClickAsync();

        // Seven days ending today: the days of the browser, or in a static render the ones of UTC.
        await Page.WaitForURLAsync(new Regex(@"\?dat\.f\.issued\.between=(?!2026-01-03)\d{4}-\d{2}-\d{2}\.\.\d{4}-\d{2}-\d{2}$"), new() { Timeout = 15_000 });
        var query = Uri.UnescapeDataString(new Uri(Page.Url).Query);
        var days = query[(query.IndexOf('=') + 1)..].Split("..").Select(d => DateOnly.ParseExact(d, "yyyy-MM-dd", CultureInfo.InvariantCulture)).ToArray();
        Assert.Equal(6, days[1].DayNumber - days[0].DayNumber);
        Assert.InRange(Math.Abs(days[1].DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber), 0, 1);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_single_date_preset_applies_the_operator_it_is_listed_for(string mode)
    {
        await OpenList(mode, "?dat.f.issued.after=2026-01-03");
        await OpenTag(Date, "Issued is after 1/3/2026", "Edit filter Issued");
        var panel = Editor(Date, "Edit filter Issued");
        var presets = panel.GetByRole(AriaRole.Group, new() { Name = "Presets for Issued" }).GetByRole(Control(mode));

        await Assertions.Expect(presets).ToHaveTextAsync(["Today", "7 days ago", "Start of year"]);
        await presets.Nth(2).ClickAsync();

        // 1 January of the year it is: no invoice is issued after it before 3 January of 2026, whatever the year.
        await Page.WaitForURLAsync(new Regex(@"\?dat\.f\.issued\.after=\d{4}-01-01$"), new() { Timeout = 15_000 });
        await Assertions.Expect(Tag(Date, "Issued is after 1/1/")).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task The_range_calendar_applies_on_the_second_day_and_limits_the_range(string mode)
    {
        await OpenList(mode, "?dat.f.issued.between=2026-01-03..2026-01-09");
        await OpenTag(Date, "Issued is between 1/3/2026 – 1/9/2026", "Edit filter Issued");
        var panel = Editor(Date, "Edit filter Issued");
        var calendar = panel.Locator("bx-calendar");
        await Assertions.Expect(panel.Locator("[data-slot=filter-range-limit]")).ToHaveTextAsync("At most 92 days");

        await calendar.Locator("button[data-date='2026-01-12']").ClickAsync();

        // Nothing applies on the first click, and the calendar keeps the days within 92 days of it.
        await Assertions.Expect(calendar).ToHaveAttributeAsync("max", "2026-04-13");
        Assert.Equal("?dat.f.issued.between=2026-01-03..2026-01-09", Uri.UnescapeDataString(new Uri(Page.Url).Query));
        await calendar.Locator("button[data-date='2026-01-20']").ClickAsync();

        await WaitForQuery("?dat.f.issued.between=2026-01-12..2026-01-20");
        await Assertions.Expect(Tag(Date, "Issued is between 1/12/2026 – 1/20/2026")).ToBeVisibleAsync();
        await Assertions.Expect(Count(Date)).ToHaveTextAsync("1–4 of 4");
        await Assertions.Expect(Editor(Date, "Edit filter Issued")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Statically_rendered_a_range_of_days_is_two_date_fields_and_the_page_reads_both()
    {
        await OpenList("ssr", "?dat.f.issued.between=2026-01-03..2026-01-09");
        await OpenTag(Date, "Issued is between 1/3/2026 – 1/9/2026", "Edit filter Issued");
        var panel = Editor(Date, "Edit filter Issued");
        await Assertions.Expect(panel.GetByLabel("Issued, from")).ToHaveValueAsync("2026-01-03");

        await panel.GetByLabel("Issued, from").FillAsync("2026-01-12");
        await panel.GetByLabel("Issued, to").FillAsync("2026-01-20");
        await panel.GetByRole(AriaRole.Button, new() { Name = "Apply" }).ClickAsync();

        await WaitForQuery("?dat.f.issued.from=2026-01-12&dat.f.issued.to=2026-01-20");
        await Assertions.Expect(Tag(Date, "Issued is between 1/12/2026 – 1/20/2026")).ToBeVisibleAsync();
        await Assertions.Expect(Count(Date)).ToHaveTextAsync("1–4 of 4");
    }

    // ---- history, enhanced navigation

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Each_applied_number_is_a_history_entry_and_back_undoes_one(string mode)
    {
        await OpenList(mode, "?num.f.amount.gte=500");
        await OpenTag(Number, "Amount is at least 500.00", "Edit filter Amount");
        if (mode == "ssr") await Editor(Number, "Edit filter Amount").GetByRole(AriaRole.Link, new() { Name = "is at most" }).ClickAsync();
        else await Editor(Number, "Edit filter Amount").GetByLabel("Operator for Amount").SelectOptionAsync("lte");
        await WaitForQuery("?num.f.amount.lte=500");

        await Page.GoBackAsync();

        await WaitForQuery("?num.f.amount.gte=500");
        await Assertions.Expect(Tag(Number, "Amount is at least 500.00")).ToBeVisibleAsync();
        await Assertions.Expect(Count(Number)).ToHaveTextAsync("1–10 of 33");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task After_arriving_by_navigation_from_another_page_the_number_and_date_editors_open_and_apply(string mode)
    {
        await NavigateFromAnotherPage(mode, "?num.f.amount=86.25&dat.f.issued=2026-01-05");

        await Assertions.Expect(Tag(Number, "Amount is 86.25")).ToBeVisibleAsync();
        await Assertions.Expect(Tag(Date, "Issued is 1/5/2026")).ToBeVisibleAsync();
        await OpenTag(Date, "Issued is 1/5/2026", "Edit filter Issued");
        var field = Editor(Date, "Edit filter Issued").GetByLabel("Value for Issued");
        await field.FillAsync("2026-01-07");
        if (mode == "ssr") await Editor(Date, "Edit filter Issued").GetByRole(AriaRole.Button, new() { Name = "Apply" }).ClickAsync();

        await Page.WaitForURLAsync(url => Uri.UnescapeDataString(new Uri(url).Query).Contains("dat.f.issued=2026-01-07"), new() { Timeout = 15_000 });
        await Assertions.Expect(Tag(Date, "Issued is 1/7/2026")).ToBeVisibleAsync();
        await Assertions.Expect(Tag(Number, "Amount is 86.25")).ToBeVisibleAsync();
        await Assertions.Expect(Count(Date)).ToHaveTextAsync("1–1 of 1");
    }

    // ---- accessibility and a narrow screen

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_number_editor_has_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await OpenList(mode, "?num.f.amount.between=100..200");
        await OpenTag(Number, "Amount is between 100.00 – 200.00", "Edit filter Amount");
        await AssertNoAccessibilityViolations();
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(Editor(Number, "Edit filter Amount")).ToBeHiddenAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await Page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
        await OpenTag(Number, "Amount is between 100.00 – 200.00", "Edit filter Amount");

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_date_editor_has_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await OpenList(mode, "?dat.f.issued.between=2026-01-03..2026-01-09");
        await OpenTag(Date, "Issued is between 1/3/2026 – 1/9/2026", "Edit filter Issued");
        await AssertNoAccessibilityViolations();
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(Editor(Date, "Edit filter Issued")).ToBeHiddenAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await Page.WaitForFunctionAsync("() => document.getAnimations().length === 0");
        await OpenTag(Date, "Issued is between 1/3/2026 – 1/9/2026", "Edit filter Issued");

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_single_day_and_single_number_editors_have_no_accessibility_violations(string mode)
    {
        await OpenList(mode, "?dat.f.issued=2026-01-03&num.f.amount=100");
        await OpenTag(Date, "Issued is 1/3/2026", "Edit filter Issued");
        await AssertNoAccessibilityViolations();
        await Page.Keyboard.PressAsync("Escape");
        await OpenTag(Number, "Amount is 100.00", "Edit filter Amount");

        await AssertNoAccessibilityViolations();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task On_a_phone_the_number_and_date_editors_fit_the_screen(string mode)
    {
        await Page.SetViewportSizeAsync(375, 800);
        await OpenList(mode, "?dat.f.issued.between=2026-01-03..2026-01-09&num.f.amount.between=100..200");

        var overflow = await Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");
        Assert.False(overflow, "the page scrolls horizontally at 375px");
        await OpenTag(Date, "Issued is between 1/3/2026 – 1/9/2026", "Edit filter Issued");
        var panel = await Editor(Date, "Edit filter Issued").BoundingBoxAsync();
        Assert.NotNull(panel);
        Assert.True(panel!.X >= 0 && panel.X + panel.Width <= 375.5, $"the date editor is {panel.Width}px wide at x={panel.X}");
        await Page.Keyboard.PressAsync("Escape");
        await OpenTag(Number, "Amount is between 100.00 – 200.00", "Edit filter Amount");
        var number = await Editor(Number, "Edit filter Amount").BoundingBoxAsync();
        Assert.NotNull(number);
        Assert.True(number!.X >= 0 && number.X + number.Width <= 375.5, $"the number editor is {number.Width}px wide at x={number.X}");
    }

    // ---- the time zone cookie

    [Fact]
    public async Task The_script_leaves_the_time_zone_in_a_cookie_and_the_next_static_render_cuts_days_in_it()
    {
        var context = await NewContext(new() { TimezoneId = "Asia/Tokyo" });
        var page = await context.NewPageAsync();

        // The first visit has no cookie yet: the static render has no zone.
        await page.GotoAsync("/ssr/components/data-list");
        await page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        await Assertions.Expect(page.GetByTestId("url-zone")).ToHaveTextAsync("none");
        var cookie = await WaitForCookie(context, "blux-timezone");
        Assert.Equal("Asia/Tokyo", cookie.Value);
        Assert.Equal("/", cookie.Path);
        Assert.Equal(SameSiteAttribute.Lax, cookie.SameSite);
        Assert.True(cookie.Expires > DateTimeOffset.UtcNow.AddDays(300).ToUnixTimeSeconds());

        await page.ReloadAsync();
        await page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");

        await Assertions.Expect(page.GetByTestId("url-zone")).ToHaveTextAsync("Asia/Tokyo");
    }

    [Fact]
    public async Task A_cookie_with_an_unknown_zone_leaves_the_static_render_in_utc()
    {
        var context = await NewContext(new());
        await context.AddCookiesAsync([new() { Name = "blux-timezone", Value = "Nowhere/Land", Url = BaseUrl }]);
        var page = await context.NewPageAsync();

        await page.GotoAsync("/ssr/components/data-list");

        await Assertions.Expect(page.GetByTestId("url-zone")).ToHaveTextAsync("none");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task With_the_cookie_a_prerendered_date_filter_still_shows_the_right_rows_once_interactive(string mode)
    {
        var context = await NewContext(new() { TimezoneId = "Pacific/Kiritimati" });
        await context.AddCookiesAsync([new() { Name = "blux-timezone", Value = "Pacific/Kiritimati", Url = BaseUrl }]);
        var page = await context.NewPageAsync();

        await page.GotoAsync($"/{mode}/components/data-list-filters?dat.f.issued=2026-01-03");
        await WaitForList(page, mode);

        await Assertions.Expect(page.GetByTestId("list-date").Locator("[data-slot=result-count]")).ToHaveTextAsync("1–1 of 1");
    }

    private static async Task<BrowserContextCookiesResult> WaitForCookie(IBrowserContext context, string name)
    {
        for (var deadline = DateTime.UtcNow.AddSeconds(5); DateTime.UtcNow < deadline; await Task.Delay(50))
        {
            if ((await context.CookiesAsync()).FirstOrDefault(c => c.Name == name) is { } cookie) return cookie;
        }
        throw new TimeoutException($"No cookie named {name}.");
    }

    // ---- language

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Texts_and_dates_follow_the_language_of_the_user(string mode)
    {
        var page = await NewPage("fr", "fr");
        await page.GotoAsync($"/{mode}/components/data-list-filters?num.f.amount=86.25&dat.f.issued.between=2026-01-03..2026-01-09");
        await WaitForList(page, mode);
        var number = page.GetByTestId("list-number");
        var date = page.GetByTestId("list-date");

        await Assertions.Expect(number.Locator("[data-slot=filter-tag-button]")).ToContainTextAsync("86,25");
        await Assertions.Expect(date.Locator("[data-slot=filter-tag-button]")).ToContainTextAsync("Issued est entre 03/01/2026 – 09/01/2026");
        await ToTop(date.Locator("[data-slot=filter-tag-button]"));
        await date.Locator("[data-slot=filter-tag-button]").ClickAsync();
        await Assertions.Expect(date.GetByRole(AriaRole.Button, new() { Name = "Aujourd’hui" })).ToBeVisibleAsync();
        await Assertions.Expect(date.GetByRole(AriaRole.Button, new() { Name = "30 derniers jours" })).ToBeVisibleAsync();
    }
}
