using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Blux.UI.Tools.Query;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

/// <summary>
/// The number and date filters of the data list: their tags in the culture of the user, the operators, the number field and slider, the day, the range calendar, the times, the presets in the time zone
/// of the user, the history, the Submit apply mode, a list with its state bound instead of in the URL, and static rendering as links and GET forms.
/// </summary>
public partial class DataListRangeFilterTests : BluxToolsTestContext
{
    // 12:00 UTC on 21 November 2026: Saturday the 21st in Paris, Sunday the 22nd in Auckland.
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 12, 0, 0, TimeSpan.Zero);

    public DataListRangeFilterTests() : base(configureServices: s => s.AddSingleton<TimeProvider>(new FixedClock(Now)))
    {
    }

    private BunitNavigationManager Navigation => (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();

    private static string Query(NavigationManager navigation) => Uri.UnescapeDataString(new Uri(navigation.Uri).Query);

    [GeneratedRegex(@"[\s  ]+")]
    private static partial Regex Spaces();

    private static string Text(IElement element) => Spaces().Replace(element.TextContent, " ").Trim();

    private IRenderedComponent<DataListRangeHarness> RenderList(string? url = null, Action<ComponentParameterCollectionBuilder<DataListRangeHarness>>? configure = null, string? zone = null)
    {
        if (zone is not null) JSInterop.Setup<string?>("Blux.tools.timeZone").SetResult(zone);
        if (url is not null) Navigation.NavigateTo(url);
        return Render<DataListRangeHarness>(p => configure?.Invoke(p));
    }

    private static string[] Tags(IRenderedComponent<DataListRangeHarness> cut) =>
        [.. cut.FindAll("li[data-slot=filter-tag] [data-slot=filter-tag-button]").Select(Text)];

    private static IElement Item(IRenderedComponent<DataListRangeHarness> cut, string key) => cut.Find($"li[data-slot=filter-tag][data-filter={key}]");

    private static string[] Operators(IRenderedComponent<DataListRangeHarness> cut, string key) =>
        [.. Item(cut, key).QuerySelectorAll("select[data-slot=filter-operator] option").Select(o => o.TextContent)];

    // The element of a Blux.UI field (the custom element that wraps the input with this name): what the script of Blux.UI reports through.
    private static IElement Field(IElement scope, string label) => scope.QuerySelector($"[aria-label='{label}']")!.ParentElement!;

    private static Task Report(IElement element, string value) =>
        element.TriggerEventAsync("onbxchange", new BxChangeEventArgs { Value = value, Sequence = int.Parse(element.GetAttribute("data-bx-seq") ?? "0") + 1 });

    private static Task WaitForQuery(NavigationManager navigation, string expected) =>
        Task.Run(async () =>
        {
            for (var i = 0; i < 100 && Query(navigation) != expected; i++) await Task.Delay(20);
            Assert.Equal(expected, Query(navigation));
        });

    private static string Count(IRenderedComponent<DataListRangeHarness> cut) => Text(cut.Find("[data-testid=count]"));

    // The culture of the user for the length of a test.
    private static IDisposable Culture(string name)
    {
        var (culture, ui) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo(name);
        return new Restore(() => (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (culture, ui));
    }

    private sealed class Restore(Action undo) : IDisposable
    {
        public void Dispose() => undo();
    }

    // ---- the tags

    [Fact]
    public void A_number_tag_writes_the_numbers_with_the_group_separator_of_the_culture()
    {
        using var _ = Culture("en-US");
        var cut = RenderList("/t?f.amount.gt=120&f.weight.between=1250.5..3000");

        Assert.Equal(["Amount is greater than 120", "Weight is between 1,250.5 – 3,000"], Tags(cut));
    }

    [Fact]
    public void A_number_tag_follows_the_culture_and_the_format_of_the_filter()
    {
        using var culture = Culture("fr-FR");
        var schema = DataListSchema.For<Order>(s => s
            .RowKey(o => o.Id)
            .Field(o => o.Id, f => f.Sortable())
            .Field(o => o.Amount, f => f.Filter(DataListFilter.Number().WithLabel("Montant").WithFormat("C2")))
            .Field(o => o.Weight, f => f.Filter(DataListFilter.Number().WithLabel("Poids"))));
        var cut = RenderList("/t?f.amount.gte=1250.5&f.weight=1250.5", p => p.Add(c => c.Schema, schema));

        Assert.Equal(["Montant est au moins 1 250,50 €", "Poids est 1 250,5"], Tags(cut));
    }

    [Fact]
    public void A_date_tag_writes_the_day_and_the_range_in_the_culture_and_an_open_end_stays_open()
    {
        using var _ = Culture("en-US");
        var cut = RenderList("/t?f.day=2026-11-05&f.created.between=2026-11-03T00:00:00Z..");

        Assert.Equal(["Day is 11/5/2026", "Created is between 11/3/2026 12:00 AM –"], Tags(cut));
    }

    [Fact]
    public void A_date_range_tag_follows_the_order_of_the_culture()
    {
        using var _ = Culture("fr-FR");
        var cut = RenderList("/t?f.day.between=2026-11-03..2026-11-09");

        Assert.Equal(["Day est entre 03/11/2026 – 09/11/2026"], Tags(cut));
    }

    [Fact]
    public void A_moment_shows_in_the_time_zone_of_the_user()
    {
        using var _ = Culture("en-US");
        var cut = RenderList("/t?f.created.after=2026-11-05T10:30:00Z", zone: "Pacific/Auckland");

        // 10:30 UTC is 23:30 in Auckland (UTC+13 in November).
        Assert.Equal(["Created is after 11/5/2026 11:30 PM"], Tags(cut));
    }

    // ---- the operators

    [Fact]
    public void A_number_filter_offers_the_seven_operators_and_a_date_filter_five()
    {
        var cut = RenderList("/t?f.amount=10&f.day=2026-11-05");

        Assert.Equal(["is", "is not", "is greater than", "is at least", "is less than", "is at most", "is between"], Operators(cut, "amount"));
        Assert.Equal(["is", "is not", "is before", "is after", "is between"], Operators(cut, "day"));
    }

    [Fact]
    public async Task Changing_the_operator_keeps_the_number_and_a_range_starts_with_it()
    {
        var cut = RenderList("/t?f.amount=150");

        await Item(cut, "amount").QuerySelector("select")!.ChangeAsync(new ChangeEventArgs { Value = "gte" });
        await WaitForQuery(Navigation, "?f.amount.gte=150");
        await Item(cut, "amount").QuerySelector("select")!.ChangeAsync(new ChangeEventArgs { Value = "between" });
        await WaitForQuery(Navigation, "?f.amount.between=150..");
        cut.WaitForAssertion(() => Assert.Equal(["Amount is between 150 –"], Tags(cut)));

        await Item(cut, "amount").QuerySelector("select")!.ChangeAsync(new ChangeEventArgs { Value = "lt" });
        await WaitForQuery(Navigation, "?f.amount.lt=150");
    }

    [Fact]
    public async Task Changing_the_operator_of_a_date_keeps_the_day_and_a_range_starts_with_it()
    {
        var cut = RenderList("/t?f.day=2026-11-05");

        await Item(cut, "day").QuerySelector("select")!.ChangeAsync(new ChangeEventArgs { Value = "after" });
        await WaitForQuery(Navigation, "?f.day.after=2026-11-05");
        await Item(cut, "day").QuerySelector("select")!.ChangeAsync(new ChangeEventArgs { Value = "between" });
        await WaitForQuery(Navigation, "?f.day.between=2026-11-05..");
    }

    // ---- the number editor

    [Fact]
    public async Task Typing_a_number_applies_it_with_a_history_entry_on_the_first_page_and_emptying_the_field_takes_the_filter_off()
    {
        var cut = RenderList("/t?f.weight=2&p=2");
        var before = Navigation.History.Count;

        await Report(Field(Item(cut, "weight"), "Value for Weight"), "7.5");

        await WaitForQuery(Navigation, "?f.weight=7.5");
        Assert.Equal(before + 1, Navigation.History.Count);
        Assert.False(Navigation.History.First().Options.ReplaceHistoryEntry);
        cut.WaitForAssertion(() => Assert.Equal(["Weight is 7.5"], Tags(cut)));
        cut.WaitForAssertion(() => Assert.Equal("1–1 of 1", Count(cut)));

        await Report(Field(Item(cut, "weight"), "Value for Weight"), "");
        await WaitForQuery(Navigation, "");
        cut.WaitForAssertion(() => Assert.Empty(Tags(cut)));
    }

    [Fact]
    public void The_number_field_takes_the_limits_and_the_step_of_the_filter_and_an_amount_with_a_slider_shows_both()
    {
        var cut = RenderList("/t?f.amount=150&f.weight=2");

        var amount = Field(Item(cut, "amount"), "Value for Amount");
        Assert.Equal(("0", "500", "10"), (amount.GetAttribute("min"), amount.GetAttribute("max"), amount.GetAttribute("step")));
        Assert.Equal("150", amount.GetAttribute("value"));
        Assert.Equal("150", Item(cut, "amount").QuerySelector("bx-slider")!.GetAttribute("data-values"));
        // No limits, no slider: the field has the default step.
        Assert.Empty(Item(cut, "weight").QuerySelectorAll("bx-slider"));
        Assert.Equal("1", Field(Item(cut, "weight"), "Value for Weight").GetAttribute("step"));
    }

    [Fact]
    public async Task Moving_the_slider_applies_the_value()
    {
        var cut = RenderList("/t?f.amount=150");

        await Report(Item(cut, "amount").QuerySelector("bx-slider")!, "250");

        await WaitForQuery(Navigation, "?f.amount=250");
        cut.WaitForAssertion(() => Assert.Equal("1–1 of 1", Count(cut)));
    }

    [Fact]
    public async Task A_range_has_a_from_and_a_to_field_and_either_end_can_stay_open()
    {
        var cut = RenderList("/t?f.weight.between=2..");
        var item = Item(cut, "weight");
        Assert.Equal("From", Text(item.QuerySelectorAll("[data-slot=filter-range] span")[0]));

        await Report(Field(item, "Weight, to"), "4");
        await WaitForQuery(Navigation, "?f.weight.between=2..4");
        cut.WaitForAssertion(() => Assert.Equal(["Weight is between 2 – 4"], Tags(cut)));
        cut.WaitForAssertion(() => Assert.Equal("1–5 of 5", Count(cut)));

        await Report(Field(Item(cut, "weight"), "Weight, from"), "");
        await WaitForQuery(Navigation, "?f.weight.between=..4");
    }

    [Fact]
    public async Task A_range_with_a_slider_shows_the_two_ends_on_it_and_moving_them_applies_both()
    {
        var cut = RenderList("/t?f.amount.between=100..200");

        var slider = Item(cut, "amount").QuerySelector("bx-slider[role=group]")!;
        Assert.Equal(2, slider.QuerySelectorAll("input[type=range]").Length);

        Assert.Equal("100\u001f200", slider.GetAttribute("data-values"));
        await Report(slider, "50\u001f300");
        await WaitForQuery(Navigation, "?f.amount.between=50..300");
    }

    // ---- the date editor

    [Fact]
    public void The_presets_depend_on_the_operator()
    {
        var cut = RenderList("/t?f.day=2026-11-05&f.created.after=2026-11-05T00:00:00Z");
        string[] Presets(string key) => [.. Item(cut, key).QuerySelectorAll("button[data-slot=filter-preset]").Select(Text)];

        Assert.Equal(["Today", "Yesterday", "Start of month"], Presets("day"));
        Assert.Equal(["Today", "7 days ago", "Start of year"], Presets("created"));
    }

    [Fact]
    public async Task The_presets_of_the_other_operators_and_of_a_range()
    {
        var cut = RenderList("/t?f.day.before=2026-11-05&f.created.between=2026-11-03T00:00:00Z..2026-11-04T00:00:00Z");

        Assert.Equal(["Today", "7 days ago", "Start of month"], Item(cut, "day").QuerySelectorAll("button[data-slot=filter-preset]").Select(Text));
        Assert.Equal(["Today", "Last 7 days", "Last 30 days", "This month", "Last month", "Year to date"], Item(cut, "created").QuerySelectorAll("button[data-slot=filter-preset]").Select(Text));
        await Task.CompletedTask;
    }

    [Fact]
    public async Task A_preset_is_computed_in_the_time_zone_of_the_user_and_applies_at_once()
    {
        var cut = RenderList("/t?f.day=2026-11-05", zone: "Pacific/Auckland");

        await Item(cut, "day").QuerySelectorAll("button[data-slot=filter-preset]").First(b => Text(b) == "Yesterday").ClickAsync();

        // 12:00 UTC is already Sunday the 22nd in Auckland: yesterday is Saturday the 21st.
        await WaitForQuery(Navigation, "?f.day=2026-11-21");
    }

    [Fact]
    public async Task A_preset_in_utc_when_the_browser_gave_no_zone()
    {
        var cut = RenderList("/t?f.day=2026-11-05");

        await Item(cut, "day").QuerySelectorAll("button[data-slot=filter-preset]").First(b => Text(b) == "Start of month").ClickAsync();

        await WaitForQuery(Navigation, "?f.day=2026-11-01");
    }

    [Fact]
    public async Task A_date_typed_in_the_field_applies_and_clearing_it_takes_the_filter_off()
    {
        var cut = RenderList("/t?f.day=2026-11-05");
        var input = Item(cut, "day").QuerySelector("input[aria-label='Value for Day']")!;
        Assert.Equal("2026-11-05", input.GetAttribute("value"));

        await input.ChangeAsync(new ChangeEventArgs { Value = "2026-11-09" });
        await WaitForQuery(Navigation, "?f.day=2026-11-09");
        cut.WaitForAssertion(() => Assert.Equal("1–1 of 1", Count(cut)));

        await Item(cut, "day").QuerySelector("input[aria-label='Value for Day']")!.ChangeAsync(new ChangeEventArgs { Value = "" });
        await WaitForQuery(Navigation, "");
    }

    [Fact]
    public async Task A_range_calendar_applies_when_the_second_day_is_picked_and_not_on_the_first()
    {
        var cut = RenderList("/t?f.day.between=2026-11-03..2026-11-04");
        var calendar = Item(cut, "day").QuerySelector("bx-calendar")!;
        Assert.Equal("range", calendar.GetAttribute("mode"));
        Assert.Equal("2026-11-03/2026-11-04", calendar.GetAttribute("value"));
        var before = Navigation.History.Count;

        await Report(calendar, "2026-11-10/");
        Assert.Equal("?f.day.between=2026-11-03..2026-11-04", Query(Navigation));
        Assert.Equal(before, Navigation.History.Count);
        await Report(Item(cut, "day").QuerySelector("bx-calendar")!, "2026-11-10/2026-11-12");

        await WaitForQuery(Navigation, "?f.day.between=2026-11-10..2026-11-12");
        Assert.Equal(before + 1, Navigation.History.Count);
        cut.WaitForAssertion(() => Assert.Equal("1–3 of 3", Count(cut)));
        cut.WaitForAssertion(() => Assert.Equal("11/10/2026 – 11/12/2026", Text(Item(cut, "day").QuerySelector("[data-slot=filter-range-text]")!)));
    }

    [Fact]
    public async Task The_range_of_a_filter_with_a_limit_cannot_pass_it_and_the_calendar_says_so()
    {
        var cut = RenderList("/t?f.day.between=2026-11-03..2026-11-04");
        Assert.Equal("At most 10 days", Text(Item(cut, "day").QuerySelector("[data-slot=filter-range-limit]")!));

        // The first click leaves only the days within the limit of it selectable.
        await Report(Item(cut, "day").QuerySelector("bx-calendar")!, "2026-11-10/");
        var picking = Item(cut, "day").QuerySelector("bx-calendar")!;
        Assert.Equal(("2026-11-01", "2026-11-19"), (picking.GetAttribute("min"), picking.GetAttribute("max")));

        // A range that gets past it anyway (a script, a stale click) ends on the last allowed day.
        await Report(picking, "2026-11-10/2026-11-28");
        await WaitForQuery(Navigation, "?f.day.between=2026-11-10..2026-11-19");
    }

    [Fact]
    public async Task A_preset_range_that_is_longer_than_the_limit_keeps_the_end_and_moves_the_start()
    {
        var cut = RenderList("/t?f.day.between=2026-11-03..2026-11-04");

        await Item(cut, "day").QuerySelectorAll("button[data-slot=filter-preset]").First(b => Text(b) == "Last 30 days").ClickAsync();

        // Today is the 21st in UTC: ten days ending today.
        await WaitForQuery(Navigation, "?f.day.between=2026-11-12..2026-11-21");
    }

    [Fact]
    public async Task A_range_preset_without_a_limit_covers_the_whole_period()
    {
        var cut = RenderList("/t?f.created.between=2026-11-03T00:00:00Z..2026-11-04T00:00:00Z");

        await Item(cut, "created").QuerySelectorAll("button[data-slot=filter-preset]").First(b => Text(b) == "Last month").ClickAsync();

        // Whole October, from the start of its first day to the last minute of its last one (in UTC: no zone yet).
        await WaitForQuery(Navigation, "?f.created.between=2026-10-01T00:00:00.0000000+00:00..2026-10-31T23:59:00.0000000+00:00");
    }

    [Fact]
    public async Task A_date_exact_to_the_minute_has_a_time_field_and_the_instant_carries_the_offset_of_the_zone()
    {
        var cut = RenderList("/t?f.created.after=2026-11-05T10:30:00Z", zone: "Europe/Paris");
        var item = Item(cut, "created");
        // 10:30 UTC is 11:30 in Paris in November.
        Assert.Equal("2026-11-05", item.QuerySelector("input[aria-label='Value for Created']")!.GetAttribute("value"));
        Assert.Equal("11", item.QuerySelector("bx-input-time [data-segment=hour]")!.GetAttribute("data-label"));
        Assert.Equal("30", item.QuerySelector("bx-input-time [data-segment=minute]")!.GetAttribute("data-label"));

        await Report(item.QuerySelector("bx-input-time")!, "14:45");

        await WaitForQuery(Navigation, "?f.created.after=2026-11-05T14:45:00.0000000+01:00");
        cut.WaitForAssertion(() => Assert.Equal(["Created is after 11/5/2026 2:45 PM"], Tags(cut)));
    }

    [Fact]
    public async Task A_date_picked_without_a_time_means_the_start_of_the_day_and_a_day_that_keeps_the_time_it_had_keeps_it()
    {
        var cut = RenderList(null, p => p.Add(c => c.ShowAdd, true));
        cut.Find("[data-slot=filter-menu-item][data-filter=created]").Click();
        var panel = cut.Find("[data-slot=add-filter-popover] [popover]");

        await panel.QuerySelector("input[aria-label='Value for Created']")!.ChangeAsync(new ChangeEventArgs { Value = "2026-11-06" });
        await WaitForQuery(Navigation, "?f.created=2026-11-06T00:00:00.0000000+00:00");

        await Report(Item(cut, "created").QuerySelector("bx-input-time")!, "10:30");
        await WaitForQuery(Navigation, "?f.created=2026-11-06T10:30:00.0000000+00:00");
        await Item(cut, "created").QuerySelector("input[aria-label='Value for Created']")!.ChangeAsync(new ChangeEventArgs { Value = "2026-11-07" });
        await WaitForQuery(Navigation, "?f.created=2026-11-07T10:30:00.0000000+00:00");
    }

    [Fact]
    public async Task A_filter_exact_to_the_second_applies_and_shows_its_tag_but_has_no_editor()
    {
        var schema = DataListSchema.For<Order>(s => s
            .RowKey(o => o.Id)
            .Field(o => o.Id, f => f.Sortable())
            .Field(o => o.Created, f => f.Filter(DataListFilter.Date(DateGranularity.Second).WithLabel("Created"))));
        var cut = RenderList("/t?f.created.after=2026-11-05T10:30:00Z", p => p.Add(c => c.Schema, schema).Add(c => c.ShowAdd, true));

        Assert.Single(Tags(cut));
        Assert.Empty(cut.FindAll("li[data-slot=filter-tag] [popover]"));
        Assert.Empty(cut.FindAll("[data-slot=filter-menu-item]"));
        await Task.CompletedTask;
    }

    // ---- the Submit mode, the bound state and the history

    [Fact]
    public async Task In_submit_mode_a_number_waits_for_apply()
    {
        var cut = RenderList("/t?f.weight=2", p => p.Add(c => c.ApplyMode, DataListApplyMode.Submit));
        var before = Navigation.History.Count;

        await Report(Field(Item(cut, "weight"), "Value for Weight"), "9");
        Assert.Equal("?f.weight=2", Query(Navigation));
        Item(cut, "weight").QuerySelector("button[data-slot=filter-apply]")!.Click();

        await WaitForQuery(Navigation, "?f.weight=9");
        Assert.Equal(before + 1, Navigation.History.Count);
    }

    [Fact]
    public async Task In_submit_mode_a_preset_and_a_range_wait_for_apply_and_cancel_drops_them()
    {
        var cut = RenderList("/t?f.day=2026-11-05", p => p.Add(c => c.ApplyMode, DataListApplyMode.Submit));

        await Item(cut, "day").QuerySelectorAll("button[data-slot=filter-preset]").First(b => Text(b) == "Today").ClickAsync();
        Assert.Equal("?f.day=2026-11-05", Query(Navigation));
        Item(cut, "day").QuerySelector("button[data-slot=filter-cancel]")!.Click();
        cut.WaitForAssertion(() => Assert.Equal("2026-11-05", Item(cut, "day").QuerySelector("input[aria-label='Value for Day']")!.GetAttribute("value")));

        await Item(cut, "day").QuerySelectorAll("button[data-slot=filter-preset]").First(b => Text(b) == "Today").ClickAsync();
        Item(cut, "day").QuerySelector("button[data-slot=filter-apply]")!.Click();
        await WaitForQuery(Navigation, "?f.day=2026-11-21");
    }

    [Fact]
    public async Task A_list_with_its_state_bound_and_no_url_applies_number_and_date_filters_to_the_state()
    {
        DataListState? last = null;
        var cut = RenderList(null, p => p
            .Add(c => c.UrlSync, false)
            .Add(c => c.State, RangeData.Schema.DefaultState with { Filters = [new FilterRule("weight", FilterOperator.Gte, new FilterValue.Number(2))] })
            .Add(c => c.StateChanged, EventCallback.Factory.Create<DataListState>(this, s => last = s)));
        cut.WaitForAssertion(() => Assert.Equal(["Weight is at least 2"], Tags(cut)));

        await Report(Field(Item(cut, "weight"), "Value for Weight"), "4");

        cut.WaitForAssertion(() => Assert.Equal(new FilterRule("weight", FilterOperator.Gte, new FilterValue.Number(4)), last!.Filters.Single()));
        await Item(cut, "weight").QuerySelector("select")!.ChangeAsync(new ChangeEventArgs { Value = "between" });
        cut.WaitForAssertion(() => Assert.Equal(new FilterRule("weight", FilterOperator.Between, new FilterValue.Range(new FilterValue.Number(4), null)), last!.Filters.Single()));
    }

    [Fact]
    public async Task Back_restores_the_previous_number_into_the_editor()
    {
        var cut = RenderList("/t?f.weight=2");
        await Report(Field(Item(cut, "weight"), "Value for Weight"), "6");
        await WaitForQuery(Navigation, "?f.weight=6");

        Navigation.NavigateTo("/t?f.weight=3");

        cut.WaitForAssertion(() => Assert.Equal(["Weight is 3"], Tags(cut)));
        Assert.Equal("3", Field(Item(cut, "weight"), "Value for Weight").GetAttribute("value"));
    }

    [Fact]
    public void A_typed_url_loads_number_and_date_filters_and_drops_what_does_not_fit()
    {
        var cut = RenderList("/t?f.amount.gt=x&f.weight=2&f.day=2026-13-40&f.day.after=2026-11-05&f.weight.between=5");

        Assert.Equal(["Weight is 2", "Day is after 11/5/2026"], Tags(cut).Select(t => t.Replace("11/05/2026", "11/5/2026")));
    }

    // ---- add filter

    [Fact]
    public void The_add_filter_menu_lists_the_number_and_date_filters()
    {
        var cut = RenderList(null, p => p.Add(c => c.ShowAdd, true));

        Assert.Equal(["Amount", "Weight", "Day", "Created"], cut.FindAll("[data-slot=filter-menu-item]").Select(Text).ToArray());
    }

    [Fact]
    public async Task Picking_a_number_filter_in_the_menu_opens_its_editor_and_the_first_value_adds_the_filter()
    {
        var cut = RenderList(null, p => p.Add(c => c.ShowAdd, true));

        cut.Find("[data-slot=filter-menu-item][data-filter=weight]").Click();
        var panel = cut.Find("[data-slot=add-filter-popover] [popover]");
        await Report(Field(panel, "Value for Weight"), "3");

        await WaitForQuery(Navigation, "?f.weight=3");
        cut.WaitForAssertion(() => Assert.Equal(["Weight is 3"], Tags(cut)));
    }

    [Fact]
    public void A_date_editor_gets_the_wide_popover_and_the_others_the_narrow_one()
    {
        var cut = RenderList("/t?f.day=2026-11-05&f.weight=2");

        Assert.Contains("w-80", Item(cut, "day").QuerySelector("[popover]")!.ClassName);
        Assert.DoesNotContain("w-80", Item(cut, "weight").QuerySelector("[popover]")!.ClassName);
    }

    // ---- static rendering

    private sealed class StaticContext : BluxToolsTestContext
    {
        public StaticContext() : base(interactive: false, configureServices: s => s.AddSingleton<TimeProvider>(new FixedClock(Now))) { }

        public NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();
    }

    private static System.Collections.Specialized.NameValueCollection QueryOf(string href) => System.Web.HttpUtility.ParseQueryString(new Uri("http://x" + href).Query);

    private static (string Name, string? Value)[] Hidden(IElement form) =>
        [.. form.QuerySelectorAll("input[type=hidden]").Select(i => (i.GetAttribute("name")!, i.GetAttribute("value")))];

    [Fact]
    public void Static_rendering_edits_a_number_with_a_get_form_that_names_the_operator_and_keeps_the_other_parameters()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.amount.gte=150&f.day=2026-11-05&p=2&utm_source=mail");

        var cut = context.Render<DataListRangeHarness>();

        var form = cut.Find("li[data-filter=amount] form[data-slot=filter-form]");
        Assert.Equal("get", form.GetAttribute("method"));
        var input = form.QuerySelector("input[type=number]")!;
        Assert.Equal("f.amount.gte", input.GetAttribute("name"));
        Assert.Equal("150", input.GetAttribute("value"));
        Assert.Equal(("0", "500", "any"), (input.GetAttribute("min"), input.GetAttribute("max"), input.GetAttribute("step")));
        var hidden = Hidden(form).ToDictionary(h => h.Name, h => h.Value);
        Assert.Equal("2026-11-05", hidden["f.day"]);
        Assert.Equal("mail", hidden["utm_source"]);
        Assert.DoesNotContain("p", hidden.Keys);
        Assert.DoesNotContain("f.amount.gte", hidden.Keys);
        Assert.Equal("Apply", Text(form.QuerySelector("button[type=submit]")!));
        // The slider needs a script and .NET: a field only.
        Assert.Empty(cut.FindAll("li[data-filter=amount] bx-slider"));
    }

    [Fact]
    public void Static_rendering_sends_the_two_ends_of_a_range_as_a_from_field_and_a_to_field()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.weight.between=2..4");

        var cut = context.Render<DataListRangeHarness>();

        var fields = cut.FindAll("li[data-filter=weight] form input[type=number]");
        Assert.Equal(["f.weight.from", "f.weight.to"], fields.Select(f => f.GetAttribute("name")));
        Assert.Equal(["2", "4"], fields.Select(f => f.GetAttribute("value")));
        Assert.Equal(["Weight, from", "Weight, to"], fields.Select(f => f.GetAttribute("aria-label")));
    }

    [Fact]
    public void Static_rendering_switches_a_number_to_a_range_with_a_link_that_keeps_the_value_as_its_start()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.amount.gte=150");

        var cut = context.Render<DataListRangeHarness>();

        var links = cut.FindAll("li[data-filter=amount] [data-slot=filter-operators] a");
        Assert.Equal(["is", "is not", "is greater than", "is at least", "is less than", "is at most", "is between"], links.Select(Text));
        Assert.Equal("true", links[3].GetAttribute("aria-current"));
        Assert.Equal("150..", QueryOf(links[6].GetAttribute("href")!)["f.amount.between"]);
        Assert.Equal("150", QueryOf(links[0].GetAttribute("href")!)["f.amount"]);
    }

    [Fact]
    public void Static_rendering_lists_the_presets_as_links_that_write_the_day_and_a_form_with_a_date_field()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.day=2026-11-05&p=3");

        var cut = context.Render<DataListRangeHarness>();

        var presets = cut.FindAll("li[data-filter=day] [data-slot=filter-presets] a");
        Assert.Equal(["Today", "Yesterday", "Start of month"], presets.Select(Text));
        Assert.Equal(["2026-11-21", "2026-11-20", "2026-11-01"], presets.Select(a => QueryOf(a.GetAttribute("href")!)["f.day"]));
        Assert.All(presets, a => Assert.Null(QueryOf(a.GetAttribute("href")!)["p"]));
        var input = cut.Find("li[data-filter=day] form input[type=date]");
        Assert.Equal(("f.day", "2026-11-05"), (input.GetAttribute("name"), input.GetAttribute("value")));
    }

    [Fact]
    public void Static_rendering_of_a_date_range_has_range_presets_and_two_date_fields()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.day.between=2026-11-03..2026-11-09");

        var cut = context.Render<DataListRangeHarness>();

        var presets = cut.FindAll("li[data-filter=day] [data-slot=filter-presets] a");
        Assert.Equal(6, presets.Count);
        Assert.Equal("2026-11-15..2026-11-21", QueryOf(presets[1].GetAttribute("href")!)["f.day.between"]);
        var fields = cut.FindAll("li[data-filter=day] form input[type=date]");
        Assert.Equal(["f.day.from", "f.day.to"], fields.Select(f => f.GetAttribute("name")));
        Assert.Equal(["2026-11-03", "2026-11-09"], fields.Select(f => f.GetAttribute("value")));
    }

    [Fact]
    public void Static_rendering_of_a_date_exact_to_the_minute_uses_a_date_time_field_in_utc()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.created.after=2026-11-05T10:30:00Z");

        var cut = context.Render<DataListRangeHarness>();

        var input = cut.Find("li[data-filter=created] form input[type=datetime-local]");
        Assert.Equal(("f.created.after", "2026-11-05T10:30", "60"), (input.GetAttribute("name"), input.GetAttribute("value"), input.GetAttribute("step")));
    }

    [Fact]
    public void Static_rendering_names_the_url_key_in_the_range_fields()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?tk.f.weight.between=2..4&other.f.weight=9");

        var cut = context.Render<DataListRangeHarness>(p => p.Add(c => c.UrlKey, "tk"));

        var form = cut.Find("li[data-filter=weight] form");
        Assert.Equal(["tk.f.weight.from", "tk.f.weight.to"], form.QuerySelectorAll("input[type=number]").Select(i => i.GetAttribute("name")));
        Assert.Equal("9", Hidden(form).ToDictionary(h => h.Name, h => h.Value)["other.f.weight"]);
    }

    [Fact]
    public void Static_rendering_lists_number_and_date_editors_in_the_add_menu_with_the_wide_popover()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.weight=2");

        var cut = context.Render<DataListRangeHarness>(p => p.Add(c => c.ShowAdd, true));

        var sections = cut.FindAll("[data-slot=add-filter-popover] [data-slot=filter-section] [data-slot=filter-editor]").Select(e => e.GetAttribute("data-filter")).ToArray();
        Assert.Equal(["amount", "day", "created"], sections);
        Assert.Contains("w-80", cut.Find("[data-slot=add-filter-popover] [popover]").ClassName);
    }

    [Fact]
    public void A_range_written_as_two_fields_by_a_form_reads_back_as_the_range()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.day.from=2026-11-03&f.day.to=2026-11-05");

        var cut = context.Render<DataListRangeHarness>();

        Assert.Equal(["Day is between 11/3/2026 – 11/5/2026"], cut.FindAll("li[data-slot=filter-tag] [data-slot=filter-tag-button]").Select(Text).Select(t => t.Replace("11/03/2026", "11/3/2026").Replace("11/05/2026", "11/5/2026")));
        cut.WaitForAssertion(() => Assert.Contains("of 3", Text(cut.Find("[data-testid=count]"))));
    }

    // ---- other languages

    [Fact]
    public void The_texts_of_the_editors_follow_the_culture_of_the_user()
    {
        using var _ = Culture("fr-FR");
        var cut = RenderList("/t?f.weight.between=2..4&f.day=2026-11-05");

        Assert.Equal("De", Text(Item(cut, "weight").QuerySelector("[data-slot=filter-range] span")!));
        Assert.NotNull(Item(cut, "weight").QuerySelector("[aria-label='Poids, de']") ?? Item(cut, "weight").QuerySelector("[aria-label='Weight, de']"));
        Assert.Equal(["Aujourd’hui", "Hier", "Début du mois"], Item(cut, "day").QuerySelectorAll("button[data-slot=filter-preset]").Select(Text));
        Assert.Equal("Raccourcis pour Day", Item(cut, "day").QuerySelector("[data-slot=filter-presets]")!.GetAttribute("aria-label"));
    }
}
