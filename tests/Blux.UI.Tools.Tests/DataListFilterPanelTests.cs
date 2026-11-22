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

    // Apply, Reset and Clear stay focusable when there is nothing to do: they say so with aria-disabled and do nothing.
    private static bool Idle(IElement button) => button.GetAttribute("aria-disabled") == "true" && !button.HasAttribute("disabled");

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
        Assert.True(Idle(cut.Find("button[data-slot=filter-panel-clear]")));

        Navigation.NavigateTo("/t?f.status=open&f.title=ann&f.active=true&s=-id&q=x");
        cut.WaitForAssertion(() => Assert.False(Idle(cut.Find("button[data-slot=filter-panel-clear]"))));
        var before = Navigation.History.Count;

        cut.Find("button[data-slot=filter-panel-clear]").Click();

        cut.WaitForAssertion(() => Assert.Equal("?s=-id&q=x", Query(Navigation)));
        Assert.Equal(before + 1, Navigation.History.Count);
        Assert.False(Navigation.History.First().Options.ReplaceHistoryEntry);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-slot=filter-panel-count]")));
        Assert.True(Idle(cut.Find("button[data-slot=filter-panel-clear]")));
    }

    // ---- the Submit apply mode

    [Fact]
    public async Task In_submit_mode_changes_wait_for_Apply_which_applies_every_section_in_one_history_entry()
    {
        var cut = RenderList("/t?f.status=open", p => p.Add(c => c.ApplyMode, DataListApplyMode.Submit));
        cut.WaitForAssertion(() => Assert.Equal(4, OptionRows(cut, "status").Length));
        var before = Navigation.History.Count;
        Assert.True(Idle(cut.Find("button[data-slot=filter-panel-apply]")));
        Assert.True(Idle(cut.Find("button[data-slot=filter-panel-reset]")));

        OptionRows(cut, "status")[1].QuerySelector("input[type=checkbox]")!.Change(true);
        await Type(Section(cut, "title").QuerySelector("input[aria-label='Value for Title']")!, "ann");
        await Section(cut, "active").QuerySelector("bx-toggle-group")!.TriggerEventAsync("onbxchange", new BxChangeEventArgs { Value = "false", Sequence = 1 });

        await Task.Delay(400, Xunit.TestContext.Current.CancellationToken);
        Assert.Equal("?f.status=open", Query(Navigation));
        Assert.Equal(before, Navigation.History.Count);
        cut.WaitForAssertion(() => Assert.False(Idle(cut.Find("button[data-slot=filter-panel-apply]"))));
        Assert.Empty(cut.FindAll("[data-slot=filter-editor] [data-slot=filter-actions]"));

        cut.Find("button[data-slot=filter-panel-apply]").Click();

        await WaitForQuery(Navigation, "?f.status=open,paused&f.title=ann&f.active=false");
        Assert.Equal(before + 1, Navigation.History.Count);
        Assert.False(Navigation.History.First().Options.ReplaceHistoryEntry);
        cut.WaitForAssertion(() => Assert.True(Idle(cut.Find("button[data-slot=filter-panel-apply]"))));
        Assert.Equal("2", Text(Section(cut, "status").QuerySelector("[data-slot=filter-panel-section-count]")!));
    }

    [Fact]
    public async Task In_submit_mode_Reset_drops_what_was_not_applied_in_every_section()
    {
        var cut = RenderList("/t?f.title=bob", p => p.Add(c => c.ApplyMode, DataListApplyMode.Submit));
        cut.WaitForAssertion(() => Assert.Equal(4, OptionRows(cut, "status").Length));
        OptionRows(cut, "status")[0].QuerySelector("input[type=checkbox]")!.Change(true);
        await Type(Section(cut, "title").QuerySelector("input[aria-label='Value for Title']")!, "zed");
        cut.WaitForAssertion(() => Assert.False(Idle(cut.Find("button[data-slot=filter-panel-reset]"))));

        cut.Find("button[data-slot=filter-panel-reset]").Click();

        cut.WaitForAssertion(() => Assert.True(Idle(cut.Find("button[data-slot=filter-panel-reset]"))));
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
        cut.WaitForAssertion(() => Assert.False(Idle(cut.Find("button[data-slot=filter-panel-apply]"))));
        OptionRows(cut, "status")[1].QuerySelector("input[type=checkbox]")!.Change(false);

        cut.WaitForAssertion(() => Assert.True(Idle(cut.Find("button[data-slot=filter-panel-apply]"))));
        await Task.CompletedTask;
    }

    [Fact]
    public async Task In_submit_mode_clear_filters_stages_the_removal_until_Apply_and_Reset_restores_it()
    {
        var cut = RenderList("/t?f.active=true&f.status=open", p => p.Add(c => c.ApplyMode, DataListApplyMode.Submit));
        cut.WaitForAssertion(() => Assert.Equal(4, OptionRows(cut, "status").Length));
        Assert.True(OptionRows(cut, "status")[0].QuerySelector("input[type=checkbox]")!.HasAttribute("checked"));
        var before = Navigation.History.Count;

        cut.Find("button[data-slot=filter-panel-clear]").Click();

        // Nothing is applied, the editors show the removal and Apply is ready.
        cut.WaitForAssertion(() => Assert.False(Idle(cut.Find("button[data-slot=filter-panel-apply]"))));
        cut.WaitForAssertion(() => Assert.False(OptionRows(cut, "status")[0].QuerySelector("input[type=checkbox]")!.HasAttribute("checked")));
        Assert.Equal("?f.active=true&f.status=open", Query(Navigation));
        Assert.Equal(before, Navigation.History.Count);

        cut.Find("button[data-slot=filter-panel-reset]").Click();
        cut.WaitForAssertion(() => Assert.True(OptionRows(cut, "status")[0].QuerySelector("input[type=checkbox]")!.HasAttribute("checked")));
        Assert.True(Idle(cut.Find("button[data-slot=filter-panel-apply]")));

        cut.Find("button[data-slot=filter-panel-clear]").Click();
        cut.WaitForAssertion(() => Assert.False(Idle(cut.Find("button[data-slot=filter-panel-apply]"))));
        cut.Find("button[data-slot=filter-panel-apply]").Click();

        await WaitForQuery(Navigation, "");
        Assert.Equal(before + 1, Navigation.History.Count);
    }

    [Fact]
    public void Apply_Reset_and_Clear_stay_buttons_that_can_take_the_focus_when_there_is_nothing_to_do()
    {
        var cut = RenderList(null, p => p.Add(c => c.ApplyMode, DataListApplyMode.Submit));

        foreach (var slot in new[] { "apply", "reset", "clear" })
        {
            var button = cut.Find($"button[data-slot=filter-panel-{slot}]");
            Assert.False(button.HasAttribute("disabled"), slot);
            Assert.Equal("true", button.GetAttribute("aria-disabled"));
        }
    }

    [Fact]
    public void Pressing_Apply_with_nothing_staged_does_nothing()
    {
        var cut = RenderList("/t?f.status=open", p => p.Add(c => c.ApplyMode, DataListApplyMode.Submit));
        var before = Navigation.History.Count;

        cut.Find("button[data-slot=filter-panel-apply]").Click();
        cut.Find("button[data-slot=filter-panel-reset]").Click();

        Assert.Equal(before, Navigation.History.Count);
        Assert.Equal("?f.status=open", Query(Navigation));
    }

    [Fact]
    public async Task A_change_that_the_list_makes_to_a_staged_filter_clears_its_pending_flag()
    {
        var cut = RenderList("/t", p => p.Add(c => c.ApplyMode, DataListApplyMode.Submit));
        cut.WaitForAssertion(() => Assert.Equal(4, OptionRows(cut, "status").Length));
        OptionRows(cut, "status")[0].QuerySelector("input[type=checkbox]")!.Change(true);
        cut.WaitForAssertion(() => Assert.False(Idle(cut.Find("button[data-slot=filter-panel-apply]"))));

        // The address now has exactly what was staged (a tag, Back, a typed address).
        Navigation.NavigateTo("/t?f.status=open");

        cut.WaitForAssertion(() => Assert.True(Idle(cut.Find("button[data-slot=filter-panel-apply]"))));
        await Task.CompletedTask;
    }

    [Fact]
    public void The_result_count_is_a_live_region_in_the_panel()
    {
        var cut = RenderList("/t?f.active=true");

        var results = cut.Find("[data-slot=filter-panel-results]");
        cut.WaitForAssertion(() => Assert.Equal("Results: 13", Text(cut.Find("[data-slot=filter-panel-results]"))));
        Assert.Equal("status", results.GetAttribute("role"));
        Assert.Equal("polite", results.GetAttribute("aria-live"));
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
        // Every filter has its own fields in the form, so no rule of the schema is a hidden field; the page goes back to the first.
        Assert.DoesNotContain("f.status", hidden.Keys);
        Assert.DoesNotContain("f.active", hidden.Keys);
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
    public void Statically_rendered_options_are_checkboxes_and_yes_no_any_are_radios_of_the_form_and_only_the_operators_are_links()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.title=ann&f.status=open,paused&f.active=true");

        var cut = context.Render<DataListPanelHarness>();

        var form = cut.Find("form[data-slot=filter-panel-form]");
        cut.WaitForAssertion(() => Assert.Equal(5, form.QuerySelectorAll("[data-filter=status] input[type=checkbox]").Length));
        var boxes = form.QuerySelectorAll("[data-filter=status] input[data-slot=filter-option-input]");
        Assert.All(boxes, b => Assert.Equal("f.status", b.GetAttribute("name")));
        Assert.Equal(["open", "paused"], boxes.Where(b => b.HasAttribute("checked")).Select(b => b.GetAttribute("value")));
        Assert.Equal("~", form.QuerySelector("[data-filter=status] input[data-slot=filter-include-empty]")!.GetAttribute("value"));
        Assert.Empty(form.QuerySelectorAll("[data-filter=status] a[data-slot=filter-option]"));
        var radios = form.QuerySelectorAll("[data-filter=active] input[type=radio]");
        Assert.Equal(["true", "false", ""], radios.Select(r => r.GetAttribute("value")));
        Assert.All(radios, r => Assert.Equal("f.active", r.GetAttribute("name")));
        Assert.Equal("true", radios.Single(r => r.HasAttribute("checked")).GetAttribute("value"));
        var operators = cut.FindAll("bx-collapsible[data-filter=title] [data-slot=filter-operators] a");
        Assert.Equal(["contains", "is", "is not"], operators.Select(Text));
        Assert.Equal("ann", QueryOf(operators[2].GetAttribute("href")!)["f.title.isNot"]);
    }

    [Fact]
    public void Statically_rendered_an_option_with_a_comma_is_escaped_the_way_the_address_writes_it_and_is_not_a_default_marker_without_a_default()
    {
        var schema = DataListSchema.For<Ticket>(s => s
            .RowKey(t => t.Id)
            .Field(t => t.Id, f => f.Sortable())
            .Field(t => t.Status, f => f.Filter(DataListFilter.Option(new DataListFilterOption("a,b", "A, B"), new DataListFilterOption("c", "C")).WithLabel("Status")))
            .DefaultSort(t => t.Id));
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t");

        var cut = context.Render<DataListPanelHarness>(p => p.Add(c => c.Schema, schema));

        var form = cut.Find("form[data-slot=filter-panel-form]");
        Assert.Equal("a%2Cb", form.QuerySelector("input[data-slot=filter-option-input]")!.GetAttribute("value"));
        Assert.Empty(form.QuerySelectorAll("input[type=hidden][name='f.status']"));
    }

    [Fact]
    public void A_filter_with_a_default_rule_sends_a_blank_marker_so_that_unchecking_everything_removes_the_default()
    {
        var schema = DataListSchema.For<Ticket>(s => s
            .RowKey(t => t.Id)
            .Field(t => t.Id, f => f.Sortable())
            .Field(t => t.Status, f => f.Filter(DataListFilter.Option(new DataListFilterOption("open", "Open"), new DataListFilterOption("closed", "Closed")).WithLabel("Status").WithDefault(new FilterValue.List(["open"]))))
            .Field(t => t.Active, f => f.Filter(DataListFilter.Boolean().WithLabel("Active").WithDefault(new FilterValue.Boolean(true))))
            .DefaultSort(t => t.Id));
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t");

        var cut = context.Render<DataListPanelHarness>(p => p.Add(c => c.Schema, schema));

        var form = cut.Find("form[data-slot=filter-panel-form]");
        Assert.Single(form.QuerySelectorAll("input[type=hidden][name='f.status'][value='']"));
        Assert.False(form.QuerySelector("input[type=radio][value='']")!.HasAttribute("data-bx-prune"));
    }

    [Fact]
    public void Statically_rendered_an_option_with_is_has_an_any_radio_that_takes_the_choice_off()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.status.is=open");

        var cut = context.Render<DataListPanelHarness>();

        var form = cut.Find("form[data-slot=filter-panel-form]");
        var none = form.QuerySelector("[data-filter=status] input[data-slot=filter-option-none]")!;
        Assert.Equal("radio", none.GetAttribute("type"));
        Assert.Equal("f.status.is", none.GetAttribute("name"));
        Assert.False(none.HasAttribute("checked"));
        Assert.True(none.HasAttribute("data-bx-prune"));
        Assert.Equal("open", form.QuerySelectorAll("[data-filter=status] input[type=radio][checked]").Single().GetAttribute("value"));

        // With the any radio picked and the form applied the filter is gone.
        Assert.DoesNotContain(Rules(TicketSchemaForRadios, "f.status.is=", null), r => r.Key == "status");
    }

    private static readonly DataListSchema<Ticket> TicketSchemaForRadios = FilterData.Schema;

    [Fact]
    public void Statically_rendered_an_option_with_any_of_has_no_any_radio()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.status=open");

        var cut = context.Render<DataListPanelHarness>();

        Assert.Empty(cut.FindAll("[data-filter=status] input[data-slot=filter-option-none]"));
    }

    [Fact]
    public void Statically_rendered_the_blank_any_radio_is_marked_so_that_it_leaves_the_address()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t");

        var cut = context.Render<DataListPanelHarness>();

        Assert.True(cut.Find("form[data-slot=filter-panel-form] [data-filter=active] input[type=radio][value='']").HasAttribute("data-bx-prune"));
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
        string.Join('&', form.QuerySelectorAll("input[name]")
            .Where(i => i.GetAttribute("type") is not ("checkbox" or "radio") || i.HasAttribute("checked"))
            .Select(i => i.GetAttribute("name") + "=" + Uri.EscapeDataString(i.GetAttribute("value") ?? "")));

    private static IReadOnlyList<FilterRule> Rules<T>(DataListSchema<T> schema, string query, string? zone)
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
        { null, "f.amount=120%2C~&f.day=2026-11-05%2C~" },
        { null, "f.amount.between=100..200%2C~&f.day.between=2026-11-02..2026-11-06%2C~" },
    };

    // Number and date filters that also allow "is empty" and "is not empty".
    private static readonly DataListSchema<Order> EmptyAwareSchema = DataListSchema.For<Order>(s => s
        .RowKey(o => o.Id)
        .Field(o => o.Id, f => f.Sortable())
        .Field(o => o.Amount, f => f.Filter(DataListFilter.Number(0, 10, 500).WithLabel("Amount").WithOperators(FilterOperator.Is, FilterOperator.Between, FilterOperator.IsEmpty, FilterOperator.IsNotEmpty)))
        .Field(o => o.Day, f => f.Filter(DataListFilter.Date().WithLabel("Day").WithOperators(FilterOperator.Is, FilterOperator.Between, FilterOperator.IsEmpty, FilterOperator.IsNotEmpty)))
        .DefaultSort(o => o.Id));

    [Theory]
    [InlineData("f.amount.isEmpty=~")]
    [InlineData("f.day.isNotEmpty=~")]
    [InlineData("f.amount.isNotEmpty=~&f.day.isEmpty=~")]
    public void Statically_rendered_a_rule_without_a_value_goes_back_as_it_came_when_the_form_is_applied(string query)
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?" + query);

        var cut = context.Render<DataListOrderPanelHarness>(p => p.Add(c => c.Schema, EmptyAwareSchema));

        var form = cut.Find("form[data-slot=filter-panel-form]");
        var rules = Rules(EmptyAwareSchema, query, null);
        Assert.NotEmpty(rules);
        Assert.Equal(rules.OrderBy(r => r.Key, StringComparer.Ordinal), Rules(EmptyAwareSchema, Submitted(form), null).OrderBy(r => r.Key, StringComparer.Ordinal));
        Assert.NotEmpty(form.QuerySelectorAll("[data-slot=filter-valueless]"));
    }

    [Fact]
    public void Statically_rendered_a_rule_without_a_value_on_a_filter_with_a_default_is_not_taken_for_a_removed_default()
    {
        var schema = DataListSchema.For<Order>(s => s
            .RowKey(o => o.Id)
            .Field(o => o.Id, f => f.Sortable())
            .Field(o => o.Amount, f => f.Filter(DataListFilter.Number(0, 10, 500).WithLabel("Amount").WithOperators(FilterOperator.Is, FilterOperator.IsEmpty).WithDefault(new FilterValue.Number(100))))
            .DefaultSort(o => o.Id));
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.amount.isEmpty=~");

        var cut = context.Render<DataListOrderPanelHarness>(p => p.Add(c => c.Schema, schema));

        var rules = Rules(schema, "f.amount.isEmpty=~", null);
        Assert.Equal(rules, Rules(schema, Submitted(cut.Find("form[data-slot=filter-panel-form]")), null));
    }

    private static readonly DataListSchema<Ticket> TicketSchema = DataListSchema.For<Ticket>(s => s
        .RowKey(t => t.Id)
        .Field(t => t.Id, f => f.Sortable())
        .Field(t => t.Title, f => f.Filter(DataListFilter.Text().WithLabel("Title").WithOperators(FilterOperator.Contains, FilterOperator.Is, FilterOperator.IsEmpty, FilterOperator.IsNotEmpty)))
        .Field(t => t.Status, f => f.Filter(DataListFilter.Option(new DataListFilterOption("open", "Open"), new DataListFilterOption("paused", "Paused"), new DataListFilterOption("a,b", "A and B")).WithLabel("Status")))
        .Field(t => t.Active, f => f.Filter(DataListFilter.Boolean().WithLabel("Active")))
        .Field(t => t.Owner, f => f.Filter(DataListFilter.Option().WithLabel("Owner")))
        .DefaultSort(t => t.Id));

    [Theory]
    [InlineData("f.title.isEmpty=~")]
    [InlineData("f.title=ann%2C~")]
    [InlineData("f.title.is=ann%2C~&f.active=true")]
    [InlineData("f.status=open,paused")]
    [InlineData("f.status=open,~&f.owner.isNot=ann,bob")]
    [InlineData("f.status=paused,a%252Cb")]
    [InlineData("f.status=~")]
    [InlineData("f.active=false&f.status.is=open")]
    public void Statically_rendered_text_and_option_rules_round_trip_through_the_form_with_their_empty_rows(string query)
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?" + query);

        var cut = context.Render<DataListPanelHarness>(p => p.Add(c => c.Schema, TicketSchema));

        var form = cut.Find("form[data-slot=filter-panel-form]");
        var rules = Rules(TicketSchema, query, null);
        Assert.NotEmpty(rules);
        Assert.Equal(rules.OrderBy(r => r.Key, StringComparer.Ordinal), Rules(TicketSchema, Submitted(form), null).OrderBy(r => r.Key, StringComparer.Ordinal));
    }

    [Fact]
    public void A_form_with_nothing_filled_in_applies_no_filter_and_a_removed_default_stays_removed()
    {
        var schema = DataListSchema.For<Ticket>(s => s
            .RowKey(t => t.Id)
            .Field(t => t.Id, f => f.Sortable())
            .Field(t => t.Status, f => f.Filter(DataListFilter.Option(new DataListFilterOption("open", "Open"), new DataListFilterOption("closed", "Closed")).WithLabel("Status").WithDefault(new FilterValue.List(["open"]))))
            .DefaultSort(t => t.Id));
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/t?f.status=");

        var cut = context.Render<DataListPanelHarness>(p => p.Add(c => c.Schema, schema));

        var form = cut.Find("form[data-slot=filter-panel-form]");
        Assert.Empty(form.QuerySelectorAll("input[type=checkbox][checked]"));
        Assert.DoesNotContain(Rules(schema, Submitted(form), null), r => r.Key == "status");
    }

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
