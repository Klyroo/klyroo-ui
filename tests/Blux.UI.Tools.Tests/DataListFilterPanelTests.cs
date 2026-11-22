using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Blux.UI.Server;
using Blux.UI.Tools.Query;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

/// <summary>
/// The filter panel: a section per filter with the editors of the tags and the add menu, the counts and the expanded sections, Live and Submit apply modes (Apply and Reset, one history entry), clear all,
/// and static rendering as one GET form (text, number and date fields in the form, operators, options and choices as links) whose submit reads back as the same rules.
/// </summary>
public partial class DataListFilterPanelTests : BluxToolsTestContext
{
    private BunitNavigationManager Navigation => (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();

    private static string Query(NavigationManager navigation) => Uri.UnescapeDataString(new Uri(navigation.Uri).Query);

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    private static string Text(IElement element) => Spaces().Replace(element.TextContent, " ").Trim();

    private IRenderedComponent<DataListPanelHarness> RenderList(string? url = null, Action<ComponentParameterCollectionBuilder<DataListPanelHarness>>? configure = null)
    {
        if (url is not null) Navigation.NavigateTo(url);
        return Render<DataListPanelHarness>(p => configure?.Invoke(p));
    }

    private static IElement Section(IRenderedComponent<DataListPanelHarness> cut, string key) => cut.Find($"bx-collapsible[data-slot=filter-panel-section][data-filter={key}]");

    private static string[] SectionKeys(IRenderedComponent<DataListPanelHarness> cut) =>
        [.. cut.FindAll("bx-collapsible[data-slot=filter-panel-section]").Select(s => s.GetAttribute("data-filter")!)];

    private static IElement[] OptionRows(IRenderedComponent<DataListPanelHarness> cut, string key) =>
        [.. Section(cut, key).QuerySelectorAll("[data-slot=filter-options] li")];

    private static Task Type(IElement input, string text) =>
        input.TriggerEventAsync("onbxchange", new BxChangeEventArgs { Value = text, Sequence = int.Parse(input.GetAttribute("data-bx-seq") ?? "0") + 1 });

    private static Task WaitForQuery(NavigationManager navigation, string expected) =>
        Task.Run(async () =>
        {
            for (var i = 0; i < 100 && Query(navigation) != expected; i++) await Task.Delay(20);
            Assert.Equal(expected, Query(navigation));
        });

    // ---- the sections

    [Fact]
    public void The_panel_lists_every_filter_of_the_schema_as_a_collapsible_section_in_the_order_of_the_schema()
    {
        var cut = RenderList();

        Assert.Equal(["title", "status", "active", "owner"], SectionKeys(cut));
        Assert.Equal("Title", Text(Section(cut, "title").QuerySelector("button[data-bx-collapsible-trigger]")!));
        var panel = cut.Find("[data-slot=filter-panel]");
        Assert.Equal("Filters", Text(cut.Find($"#{panel.GetAttribute("aria-labelledby")}")));
        Assert.Equal(["Title", "Status", "Active", "Owner"], cut.FindAll("[data-slot=filter-panel-section] [data-bx-collapsible-trigger] > span:first-child").Select(Text));
    }

    [Fact]
    public void A_section_with_a_filter_on_starts_expanded_and_the_others_closed()
    {
        var cut = RenderList("/t?f.status=open&f.active=true");

        Assert.False(Section(cut, "title").HasAttribute("open"));
        Assert.True(Section(cut, "status").HasAttribute("open"));
        Assert.True(Section(cut, "active").HasAttribute("open"));
        Assert.False(Section(cut, "owner").HasAttribute("open"));
        Assert.Equal("true", Section(cut, "status").QuerySelector("button[data-bx-collapsible-trigger]")!.GetAttribute("aria-expanded"));
        Assert.Equal("false", Section(cut, "title").QuerySelector("button[data-bx-collapsible-trigger]")!.GetAttribute("aria-expanded"));
    }

    [Fact]
    public void A_section_opens_when_a_filter_turns_on_by_other_means_and_stays_open_when_it_turns_off()
    {
        var cut = RenderList();
        Assert.False(Section(cut, "owner").HasAttribute("open"));

        Navigation.NavigateTo("/t?f.owner=ann");

        cut.WaitForAssertion(() => Assert.True(Section(cut, "owner").HasAttribute("open")));
        Navigation.NavigateTo("/t");
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-slot=filter-panel-section-count]")));
        Assert.True(Section(cut, "owner").HasAttribute("open"));
    }

    [Fact]
    public void Each_section_counts_its_values_and_the_header_counts_the_filters_and_says_so_to_a_screen_reader()
    {
        var cut = RenderList("/t?f.status=open,paused,closed&f.title=ann");

        Assert.Equal("3", Text(Section(cut, "status").QuerySelector("[data-slot=filter-panel-section-count]")!));
        Assert.Equal("1", Text(Section(cut, "title").QuerySelector("[data-slot=filter-panel-section-count]")!));
        Assert.Empty(Section(cut, "owner").QuerySelectorAll("[data-slot=filter-panel-section-count]"));
        Assert.Equal("true", Section(cut, "status").QuerySelector("[data-slot=filter-panel-section-count]")!.GetAttribute("aria-hidden"));
        Assert.Contains("3 active", Text(Section(cut, "status").QuerySelector("button[data-bx-collapsible-trigger]")!));
        Assert.Equal("2", Text(cut.Find("[data-slot=filter-panel-count]")));
        Assert.Contains("2 active", Text(cut.Find("[data-slot=filter-panel-title]")));
    }

    [Fact]
    public void An_option_filter_that_also_matches_empty_rows_counts_that_too()
    {
        var cut = RenderList("/t?f.status=open,~");

        Assert.Equal("2", Text(Section(cut, "status").QuerySelector("[data-slot=filter-panel-section-count]")!));
    }

    [Fact]
    public void The_description_shows_only_when_given_and_a_list_without_filters_says_so()
    {
        Assert.Empty(RenderList().FindAll("[data-slot=filter-panel-description]"));
        Assert.Equal("Narrow the tickets.", Text(RenderList(null, p => p.Add(c => c.Description, "Narrow the tickets.").Add(c => c.UrlKey, "b")).Find("[data-slot=filter-panel-description]")));

        var schema = DataListSchema.For<Ticket>(s => s.RowKey(t => t.Id).Field(t => t.Id, f => f.Sortable()).DefaultSort(t => t.Id));
        var none = RenderList(null, p => p.Add(c => c.Schema, schema).Add(c => c.UrlKey, "c"));
        Assert.Equal("No filters available", Text(none.Find("[data-slot=filter-panel-empty]")));
        Assert.Empty(none.FindAll("bx-collapsible"));
    }

    [Fact]
    public void The_sections_hold_the_editors_of_the_tags_with_the_same_operators_and_no_second_title()
    {
        var cut = RenderList("/t?f.status=open&f.title=ann", p => p.Add(c => c.ShowTags, true));

        var panelOperators = Section(cut, "status").QuerySelectorAll("select[data-slot=filter-operator] option").Select(o => o.TextContent).ToArray();
        var tagOperators = cut.Find("li[data-slot=filter-tag][data-filter=status]").QuerySelectorAll("select[data-slot=filter-operator] option").Select(o => o.TextContent).ToArray();
        Assert.Equal(tagOperators, panelOperators);
        Assert.Equal(["contains", "is", "is not"], Section(cut, "title").QuerySelectorAll("select[data-slot=filter-operator] option").Select(o => o.TextContent));
        Assert.Empty(Section(cut, "title").QuerySelectorAll("[data-slot=filter-editor-title]"));
        Assert.NotNull(Section(cut, "title").QuerySelector("[data-slot=filter-editor][data-filter=title]"));
    }

    // ---- the Live apply mode

    [Fact]
    public async Task In_live_mode_typing_applies_after_a_pause_and_goes_back_to_the_first_page_with_one_history_entry()
    {
        var cut = RenderList("/t?p=2");
        var before = Navigation.History.Count;

        await Type(Section(cut, "title").QuerySelector("input[aria-label='Value for Title']")!, "ann");

        await WaitForQuery(Navigation, "?f.title=ann");
        Assert.Equal(before + 1, Navigation.History.Count);
        Assert.False(Navigation.History.First().Options.ReplaceHistoryEntry);
        cut.WaitForAssertion(() => Assert.Equal("1", Text(Section(cut, "title").QuerySelector("[data-slot=filter-panel-section-count]")!)));
    }

    [Fact]
    public async Task In_live_mode_options_and_the_boolean_apply_at_once()
    {
        var cut = RenderList();
        cut.WaitForAssertion(() => Assert.Equal(4, OptionRows(cut, "status").Length));

        OptionRows(cut, "status")[0].QuerySelector("input[type=checkbox]")!.Change(true);
        await WaitForQuery(Navigation, "?f.status=open");

        await Section(cut, "active").QuerySelector("bx-toggle-group")!.TriggerEventAsync("onbxchange", new BxChangeEventArgs { Value = "true", Sequence = 2 });
        await WaitForQuery(Navigation, "?f.status=open&f.active=true");
        cut.WaitForAssertion(() => Assert.Equal("2", Text(cut.Find("[data-slot=filter-panel-count]"))));
    }

    [Fact]
    public void Live_mode_has_no_apply_or_reset_button()
    {
        var cut = RenderList("/t?f.status=open");

        Assert.Empty(cut.FindAll("[data-slot=filter-panel-footer]"));
        Assert.Empty(cut.FindAll("[data-slot=filter-apply]"));
    }

    // ---- clear all

    [Fact]
    public void Clear_filters_is_disabled_without_a_filter_and_takes_them_all_off_with_one_history_entry()
    {
        var cut = RenderList();
        Assert.True(cut.Find("button[data-slot=filter-panel-clear]").HasAttribute("disabled"));

        Navigation.NavigateTo("/t?f.status=open&f.title=ann&f.active=true&s=-id&q=x");
        cut.WaitForAssertion(() => Assert.False(cut.Find("button[data-slot=filter-panel-clear]").HasAttribute("disabled")));
        var before = Navigation.History.Count;

        cut.Find("button[data-slot=filter-panel-clear]").Click();

        cut.WaitForAssertion(() => Assert.Equal("?s=-id&q=x", Query(Navigation)));
        Assert.Equal(before + 1, Navigation.History.Count);
        Assert.False(Navigation.History.First().Options.ReplaceHistoryEntry);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-slot=filter-panel-count]")));
        Assert.True(cut.Find("button[data-slot=filter-panel-clear]").HasAttribute("disabled"));
    }

    // ---- the Submit apply mode

    [Fact]
    public async Task In_submit_mode_changes_wait_for_Apply_which_applies_every_section_in_one_history_entry()
    {
        var cut = RenderList("/t?f.status=open", p => p.Add(c => c.ApplyMode, DataListApplyMode.Submit));
        cut.WaitForAssertion(() => Assert.Equal(4, OptionRows(cut, "status").Length));
        var before = Navigation.History.Count;
        Assert.True(cut.Find("button[data-slot=filter-panel-apply]").HasAttribute("disabled"));
        Assert.True(cut.Find("button[data-slot=filter-panel-reset]").HasAttribute("disabled"));

        OptionRows(cut, "status")[1].QuerySelector("input[type=checkbox]")!.Change(true);
        await Type(Section(cut, "title").QuerySelector("input[aria-label='Value for Title']")!, "ann");
        await Section(cut, "active").QuerySelector("bx-toggle-group")!.TriggerEventAsync("onbxchange", new BxChangeEventArgs { Value = "false", Sequence = 1 });

        await Task.Delay(400, Xunit.TestContext.Current.CancellationToken);
        Assert.Equal("?f.status=open", Query(Navigation));
        Assert.Equal(before, Navigation.History.Count);
        cut.WaitForAssertion(() => Assert.False(cut.Find("button[data-slot=filter-panel-apply]").HasAttribute("disabled")));
        Assert.Empty(cut.FindAll("[data-slot=filter-editor] [data-slot=filter-actions]"));

        cut.Find("button[data-slot=filter-panel-apply]").Click();

        await WaitForQuery(Navigation, "?f.status=open,paused&f.title=ann&f.active=false");
        Assert.Equal(before + 1, Navigation.History.Count);
        Assert.False(Navigation.History.First().Options.ReplaceHistoryEntry);
        cut.WaitForAssertion(() => Assert.True(cut.Find("button[data-slot=filter-panel-apply]").HasAttribute("disabled")));
        Assert.Equal("2", Text(Section(cut, "status").QuerySelector("[data-slot=filter-panel-section-count]")!));
    }

    [Fact]
    public async Task In_submit_mode_Reset_drops_what_was_not_applied_in_every_section()
    {
        var cut = RenderList("/t?f.title=bob", p => p.Add(c => c.ApplyMode, DataListApplyMode.Submit));
        cut.WaitForAssertion(() => Assert.Equal(4, OptionRows(cut, "status").Length));
        OptionRows(cut, "status")[0].QuerySelector("input[type=checkbox]")!.Change(true);
        await Type(Section(cut, "title").QuerySelector("input[aria-label='Value for Title']")!, "zed");
        cut.WaitForAssertion(() => Assert.False(cut.Find("button[data-slot=filter-panel-reset]").HasAttribute("disabled")));

        cut.Find("button[data-slot=filter-panel-reset]").Click();

        cut.WaitForAssertion(() => Assert.True(cut.Find("button[data-slot=filter-panel-reset]").HasAttribute("disabled")));
        Assert.False(OptionRows(cut, "status")[0].QuerySelector("input[type=checkbox]")!.HasAttribute("checked"));
        Assert.Equal("?f.title=bob", Query(Navigation));

        // The editor is back to the rule: applying nothing changes nothing.
        cut.WaitForAssertion(() => Assert.Equal("bob", Section(cut, "title").QuerySelector("input[aria-label='Value for Title']")!.GetAttribute("value")));
    }

    [Fact]
    public async Task In_submit_mode_a_draft_that_is_back_to_what_the_list_has_is_not_pending()
    {
        var cut = RenderList("/t?f.status=open", p => p.Add(c => c.ApplyMode, DataListApplyMode.Submit));
        cut.WaitForAssertion(() => Assert.Equal(4, OptionRows(cut, "status").Length));

        OptionRows(cut, "status")[1].QuerySelector("input[type=checkbox]")!.Change(true);
        cut.WaitForAssertion(() => Assert.False(cut.Find("button[data-slot=filter-panel-apply]").HasAttribute("disabled")));
        OptionRows(cut, "status")[1].QuerySelector("input[type=checkbox]")!.Change(false);

        cut.WaitForAssertion(() => Assert.True(cut.Find("button[data-slot=filter-panel-apply]").HasAttribute("disabled")));
        await Task.CompletedTask;
    }

    [Fact]
    public void In_submit_mode_clear_filters_applies_at_once_and_drops_what_was_staged()
    {
        var cut = RenderList("/t?f.active=true", p => p.Add(c => c.ApplyMode, DataListApplyMode.Submit));
        cut.WaitForAssertion(() => Assert.Equal(4, OptionRows(cut, "status").Length));
        OptionRows(cut, "status")[0].QuerySelector("input[type=checkbox]")!.Change(true);
        cut.WaitForAssertion(() => Assert.False(cut.Find("button[data-slot=filter-panel-apply]").HasAttribute("disabled")));

        cut.Find("button[data-slot=filter-panel-clear]").Click();

        cut.WaitForAssertion(() => Assert.Equal("", Query(Navigation)));
        cut.WaitForAssertion(() => Assert.True(cut.Find("button[data-slot=filter-panel-apply]").HasAttribute("disabled")));
        Assert.False(OptionRows(cut, "status")[0].QuerySelector("input[type=checkbox]")!.HasAttribute("checked"));
    }

    [Fact]
    public void CloseOnApply_marks_the_apply_button_so_that_a_slideover_around_the_panel_closes()
    {
        var closing = RenderList(null, p => p.Add(c => c.ApplyMode, DataListApplyMode.Submit).Add(c => c.CloseOnApply, true));
        Assert.True(closing.Find("button[data-slot=filter-panel-apply]").HasAttribute("data-bx-close"));

        var staying = RenderList(null, p => p.Add(c => c.ApplyMode, DataListApplyMode.Submit).Add(c => c.UrlKey, "other"));
        Assert.False(staying.Find("button[data-slot=filter-panel-apply]").HasAttribute("data-bx-close"));
    }

    // ---- other languages

    [Fact]
    public void The_texts_follow_the_culture_of_the_user()
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("fr-FR");
        try
        {
            var cut = RenderList("/t?f.status=open,paused", p => p.Add(c => c.ApplyMode, DataListApplyMode.Submit));

            Assert.Contains("Filtres", Text(cut.Find("[data-slot=filter-panel-title]")));
            Assert.Contains("1 actif(s)", Text(cut.Find("[data-slot=filter-panel-title]")));
            Assert.Contains("2 actif(s)", Text(Section(cut, "status").QuerySelector("button[data-bx-collapsible-trigger]")!));
            Assert.Equal("Effacer les filtres", Text(cut.Find("[data-slot=filter-panel-clear]")));
            Assert.Equal("Réinitialiser", Text(cut.Find("[data-slot=filter-panel-reset]")));
            Assert.Equal("Appliquer", Text(cut.Find("[data-slot=filter-panel-apply]")));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = culture;
        }
    }

    // ---- static rendering

    private sealed class StaticContext : BluxToolsTestContext
    {
        public StaticContext(string? zone = null) : base(interactive: false, configureServices: s => s.AddBluxServer().AddSingleton<TimeProvider>(new FixedClock(new DateTimeOffset(2026, 11, 21, 12, 0, 0, TimeSpan.Zero))))
        {
            var http = new DefaultHttpContext();
            if (zone is not null) http.Request.Headers.Cookie = "blux-timezone=" + zone;
            Services.GetRequiredService<IHttpContextAccessor>().HttpContext = http;
        }

        public NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();
    }

    private static (string Name, string? Value)[] Hidden(IElement form) =>
        [.. form.QuerySelectorAll("input[type=hidden]").Select(i => (i.GetAttribute("name")!, i.GetAttribute("value")))];

    private static System.Collections.Specialized.NameValueCollection QueryOf(string href) => System.Web.HttpUtility.ParseQueryString(new Uri("http://x" + href).Query);

    [Fact]
    public void Statically_rendered_the_panel_is_one_get_form_with_the_text_field_a_submit_button_and_the_other_parameters_hidden()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.title.isNot=ann&f.status=open&f.active=true&p=2&s=-id&utm_source=mail");

        var cut = context.Render<DataListPanelHarness>();

        Assert.Single(cut.FindAll("form"));
        var form = cut.Find("form[data-slot=filter-panel-form]");
        Assert.Equal("get", form.GetAttribute("method"));
        Assert.Equal("/t", form.GetAttribute("action"));
        Assert.NotNull(form.GetAttribute("data-enhance"));
        var input = form.QuerySelector("input[type=text]")!;
        Assert.Equal("f.title.isNot", input.GetAttribute("name"));
        Assert.Equal("ann", input.GetAttribute("value"));
        var hidden = Hidden(form).ToDictionary(h => h.Name, h => h.Value);
        // The rules the form does not carry (options and yes / no are links) stay as hidden fields; the text rule is the field itself, and the page goes back to the first.
        Assert.Equal("open", hidden["f.status"]);
        Assert.Equal("true", hidden["f.active"]);
        Assert.Equal("-id", hidden["s"]);
        Assert.Equal("mail", hidden["utm_source"]);
        Assert.DoesNotContain("f.title.isNot", hidden.Keys);
        Assert.DoesNotContain("f.title", hidden.Keys);
        Assert.DoesNotContain("p", hidden.Keys);
        Assert.Single(form.QuerySelectorAll("button[type=submit]"));
        Assert.Equal("Apply", Text(form.QuerySelector("button[type=submit]")!));
        Assert.Empty(cut.FindAll("[data-slot=filter-form]"));
    }

    [Fact]
    public void Statically_rendered_the_options_the_boolean_and_the_operators_are_links_inside_the_sections()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.title=ann&f.status=open&f.active=true");

        var cut = context.Render<DataListPanelHarness>();

        cut.WaitForAssertion(() => Assert.Equal(4, cut.FindAll("bx-collapsible[data-filter=status] a[data-slot=filter-option]").Count));
        var options = cut.FindAll("bx-collapsible[data-filter=status] a[data-slot=filter-option]");
        Assert.Equal("open,paused", QueryOf(options[1].GetAttribute("href")!)["f.status"]);
        var yesNo = cut.FindAll("bx-collapsible[data-filter=active] [data-slot=filter-boolean] a");
        Assert.Equal(["Yes", "No", "Any"], yesNo.Select(Text));
        var operators = cut.FindAll("bx-collapsible[data-filter=title] [data-slot=filter-operators] a");
        Assert.Equal(["contains", "is", "is not"], operators.Select(Text));
        Assert.Equal("ann", QueryOf(operators[2].GetAttribute("href")!)["f.title.isNot"]);
    }

    [Fact]
    public void Statically_rendered_the_sections_with_a_filter_on_are_open_and_count_their_values()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.status=open,paused");

        var cut = context.Render<DataListPanelHarness>();

        Assert.True(Section(cut, "status").HasAttribute("open"));
        Assert.False(Section(cut, "title").HasAttribute("open"));
        Assert.Equal("2", Text(Section(cut, "status").QuerySelector("[data-slot=filter-panel-section-count]")!));
        Assert.Empty(cut.FindAll("button[data-slot=filter-panel-reset]"));
    }

    [Fact]
    public void Statically_rendered_clear_filters_is_a_link_that_keeps_the_rest_and_shows_only_with_a_filter_on()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.title=ann&q=x");
        var cut = context.Render<DataListPanelHarness>();

        var clear = cut.Find("a[data-slot=filter-panel-clear]");
        var query = QueryOf(clear.GetAttribute("href")!);
        Assert.Null(query["f.title"]);
        Assert.Equal("x", query["q"]);

        using var plain = new StaticContext();
        plain.Navigation.NavigateTo("/t?q=x");
        Assert.Empty(plain.Render<DataListPanelHarness>().FindAll("[data-slot=filter-panel-clear]"));
    }

    [Fact]
    public void Statically_rendered_the_url_key_names_the_fields_and_leaves_other_lists_alone()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?tk.f.title=ann&other.f.title=zzz&other.p=2");

        var cut = context.Render<DataListPanelHarness>(p => p.Add(c => c.UrlKey, "tk"));

        var form = cut.Find("form[data-slot=filter-panel-form]");
        Assert.Equal("tk.f.title", form.QuerySelector("input[type=text]")!.GetAttribute("name"));
        var hidden = Hidden(form).ToDictionary(h => h.Name, h => h.Value);
        Assert.Equal("zzz", hidden["other.f.title"]);
        Assert.Equal("2", hidden["other.p"]);
    }

    [Fact]
    public void Statically_rendered_without_url_sync_there_is_no_panel()
    {
        using var context = new StaticContext();

        var cut = context.Render<DataListPanelHarness>(p => p.Add(c => c.UrlSync, false));

        Assert.Empty(cut.FindAll("[data-slot=filter-panel]"));
        Assert.Empty(cut.FindAll("form"));
    }

    [Fact]
    public void A_filter_exact_to_the_second_has_no_section_because_it_has_no_editor()
    {
        var schema = DataListSchema.For<Order>(s => s
            .RowKey(o => o.Id)
            .Field(o => o.Id, f => f.Sortable())
            .Field(o => o.Name, f => f.Filter(DataListFilter.Text().WithLabel("Name")))
            .Field(o => o.Created, f => f.Filter(DataListFilter.Date(DateGranularity.Second).WithLabel("Created")))
            .DefaultSort(o => o.Id));

        var cut = Render<DataListOrderPanelHarness>(p => p.Add(c => c.Schema, schema));

        Assert.Equal(["name"], cut.FindAll("bx-collapsible[data-slot=filter-panel-section]").Select(s => s.GetAttribute("data-filter")!));
    }

    // What the browser sends when the form is submitted: every named field of the form, the hidden ones and the typed ones.
    private static string Submitted(IElement form) =>
        string.Join('&', form.QuerySelectorAll("input[name]").Select(i => i.GetAttribute("name") + "=" + Uri.EscapeDataString(i.GetAttribute("value") ?? "")));

    private static IReadOnlyList<FilterRule> Rules(DataListSchema<Order> schema, string query, string? zone)
    {
        var parsed = System.Web.HttpUtility.ParseQueryString(query);
        return DataListStateQuery.FromQuery(parsed.AllKeys.Select(k => KeyValuePair.Create(k!, (string?)parsed[k])), schema, null, zone).Filters;
    }

    public static TheoryData<string?, string> RoundTrips() => new()
    {
        { null, "f.amount=120&f.weight.gte=2.5&f.day=2026-11-05" },
        { null, "f.amount.between=100..200&f.day.between=2026-11-02..2026-11-06" },
        { "Europe/Paris", "f.amount.lt=300&f.day.before=2026-11-12&f.created.after=2026-11-03T14%3A30" },
        { "Europe/Paris", "f.created.between=2026-11-02T09%3A00..2026-11-04T17%3A30&f.day.after=2026-11-01" },
    };

    [Theory]
    [MemberData(nameof(RoundTrips))]
    public void Statically_rendered_number_and_date_fields_are_in_the_form_and_submitting_it_gives_back_the_same_rules(string? zone, string query)
    {
        using var context = new StaticContext(zone);
        context.Navigation.NavigateTo("/t?" + query);

        var cut = context.Render<DataListOrderPanelHarness>();

        var form = cut.Find("form[data-slot=filter-panel-form]");
        Assert.Single(cut.FindAll("form"));
        var rules = Rules(RangeData.Schema, query, zone);
        Assert.NotEmpty(rules);
        Assert.Equal(rules.OrderBy(r => r.Key, StringComparer.Ordinal), Rules(RangeData.Schema, Submitted(form), zone).OrderBy(r => r.Key, StringComparer.Ordinal));
    }

    [Fact]
    public void Statically_rendered_the_fields_of_a_filter_without_a_default_are_marked_so_that_blank_ones_leave_the_address()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t");

        var cut = context.Render<DataListOrderPanelHarness>();

        var fields = cut.FindAll("form[data-slot=filter-panel-form] input[data-slot=filter-input]");
        Assert.NotEmpty(fields);
        Assert.All(fields, f => Assert.True(f.HasAttribute("data-bx-prune")));
    }

    [Fact]
    public void A_filter_with_a_default_rule_keeps_blank_parameters_because_an_empty_one_is_how_its_default_is_removed()
    {
        var schema = DataListSchema.For<Order>(s => s
            .RowKey(o => o.Id)
            .Field(o => o.Id, f => f.Sortable())
            .Field(o => o.Name, f => f.Filter(DataListFilter.Text().WithLabel("Name").WithDefault(new FilterValue.Text("order"))))
            .Field(o => o.Weight, f => f.Filter(DataListFilter.Number().WithLabel("Weight")))
            .DefaultSort(o => o.Id));
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t");

        var cut = context.Render<DataListOrderPanelHarness>(p => p.Add(c => c.Schema, schema));

        var form = cut.Find("form[data-slot=filter-panel-form]");
        Assert.False(form.QuerySelector("input[name='f.name']")!.HasAttribute("data-bx-prune"));
        Assert.True(form.QuerySelector("input[name='f.weight']")!.HasAttribute("data-bx-prune"));
    }

    [Fact]
    public void Statically_rendered_a_range_is_two_fields_and_a_whole_day_is_a_date_field()
    {
        using var context = new StaticContext("Europe/Paris");
        context.Navigation.NavigateTo("/t?f.amount.between=100..200&f.day.between=2026-11-02..2026-11-06&f.created=2026-11-03");

        var cut = context.Render<DataListOrderPanelHarness>();

        var form = cut.Find("form[data-slot=filter-panel-form]");
        Assert.Equal("100", form.QuerySelector("input[name='f.amount.from']")!.GetAttribute("value"));
        Assert.Equal("200", form.QuerySelector("input[name='f.amount.to']")!.GetAttribute("value"));
        Assert.Equal("2026-11-02", form.QuerySelector("input[name='f.day.from']")!.GetAttribute("value"));
        Assert.Equal("2026-11-06", form.QuerySelector("input[name='f.day.to']")!.GetAttribute("value"));
        // A timed filter holding a whole day gets a date field, so applying the form keeps it a whole day.
        var created = form.QuerySelector("input[name='f.created']")!;
        Assert.Equal("date", created.GetAttribute("type"));
        Assert.Equal("2026-11-03", created.GetAttribute("value"));
    }
}
