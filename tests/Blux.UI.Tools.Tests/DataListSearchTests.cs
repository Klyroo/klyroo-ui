using Blux.UI.Tools.Query;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

/// <summary>The search part: the field and its attributes, what a report of the browser writes to the state and the address, the empty states and the static GET form.</summary>
public class DataListSearchTests : BluxToolsTestContext
{
    private static readonly Person[] People = DataListControllerTests.People;

    private BunitNavigationManager Navigation => (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();

    private static string Query(NavigationManager navigation) => new Uri(navigation.Uri).Query;

    private static string[] Names(IRenderedComponent<DataListSearchHarness> cut) => [.. cut.FindAll("tbody tr[data-slot=row] td:first-child").Select(td => td.TextContent)];

    // What the script of Blux.UI reports once the typing paused, on Enter or on Escape.
    private static Task Type(IRenderedComponent<DataListSearchHarness> cut, string text, int sequence = 1) =>
        cut.Find("input[type=search]").TriggerEventAsync("onbxchange", new BxChangeEventArgs { Value = text, Sequence = sequence });

    private static Task WaitForRows(IRenderedComponent<DataListSearchHarness> cut, Func<string[], bool> done) =>
        cut.WaitForAssertionAsync(() => Assert.True(done(Names(cut)), string.Join(",", Names(cut))));

    private IRenderedComponent<DataListSearchHarness> RenderList(Action<ComponentParameterCollectionBuilder<DataListSearchHarness>>? configure = null) =>
        Render<DataListSearchHarness>(p =>
        {
            p.Add(c => c.Items, People);
            configure?.Invoke(p);
        });

    private static (string Name, string? Value)[] Hidden(AngleSharp.Dom.IElement form) =>
        [.. form.QuerySelectorAll("input[type=hidden]").Select(i => (i.GetAttribute("name")!, i.GetAttribute("value")))];

    // ---- the field

    [Fact]
    public void The_field_is_a_search_box_with_a_debounce_escape_to_clear_a_localized_placeholder_and_a_name_from_the_label()
    {
        var cut = RenderList(p => p.Add(c => c.Label, "People"));

        var input = cut.Find("input[type=search]");
        Assert.Equal("300", input.GetAttribute("data-bx-debounce"));
        Assert.NotNull(input.GetAttribute("data-bx-clear-on-escape"));
        Assert.Equal("Search…", input.GetAttribute("placeholder"));
        Assert.Equal("Search People", input.GetAttribute("aria-label"));
        Assert.Equal("search", input.GetAttribute("enterkeyhint"));
        Assert.Equal("off", input.GetAttribute("autocomplete"));
        Assert.Equal("Search People", cut.Find("[role=search]").GetAttribute("aria-label"));
        // A search icon inside the field, hidden from assistive technologies.
        Assert.Equal("true", input.ParentElement!.QuerySelector("svg")!.GetAttribute("aria-hidden"));
        Assert.Equal("Clear search", cut.Find("[data-slot=search-clear]").GetAttribute("aria-label"));
        Assert.Equal("button", cut.Find("[data-slot=search-clear]").GetAttribute("type"));
    }

    [Fact]
    public void Without_a_label_the_name_is_just_Search()
    {
        var cut = RenderList();

        Assert.Equal("Search", cut.Find("input[type=search]").GetAttribute("aria-label"));
    }

    [Fact]
    public void The_debounce_comes_from_the_options_and_the_part_can_override_it()
    {
        using var context = new Customized(t => t.SearchDebounce = 450);

        Assert.Equal("450", context.Render<DataListSearchHarness>(p => p.Add(c => c.Items, People).Add(c => c.UrlSync, false)).Find("input[type=search]").GetAttribute("data-bx-debounce"));
        Assert.Equal("120", context.Render<DataListSearchHarness>(p => p.Add(c => c.Items, People).Add(c => c.Debounce, 120).Add(c => c.UrlSync, false)).Find("input[type=search]").GetAttribute("data-bx-debounce"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BluxToolsOptions().SearchDebounce = -1);
    }

    [Fact]
    public void The_texts_follow_the_culture_of_the_user()
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("fr-FR");
        try
        {
            var cut = RenderList(p => p.Add(c => c.Label, "Personnes"));

            Assert.Equal("Rechercher…", cut.Find("input[type=search]").GetAttribute("placeholder"));
            Assert.Equal("Rechercher dans Personnes", cut.Find("input[type=search]").GetAttribute("aria-label"));
            Assert.Equal("Effacer la recherche", cut.Find("[data-slot=search-clear]").GetAttribute("aria-label"));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = culture;
        }
    }

    [Fact]
    public void The_default_layout_has_the_search_when_the_schema_declares_search_fields_and_only_then()
    {
        var withSearch = Render<BxDataList<Person>>(p => p.Add(c => c.Schema, DataListSearchHarness.Schema).Add(c => c.Items, People).Add(c => c.UrlSync, false));
        var without = Render<BxDataList<Person>>(p => p.Add(c => c.Schema, DataListControllerTests.Schema).Add(c => c.Items, People).Add(c => c.UrlSync, false));

        Assert.Single(withSearch.FindAll("[data-slot=toolbar] input[type=search]"));
        Assert.Empty(without.FindAll("input[type=search]"));
    }

    // ---- typing

    [Fact]
    public async Task A_report_of_the_browser_searches_the_rows_and_replaces_the_address()
    {
        var cut = RenderList();

        await Type(cut, "Person 1");

        await WaitForRows(cut, names => names.Length == 10 && names.All(n => n.StartsWith("Person 1")));
        Assert.Equal("?q=Person%201", Query(Navigation));
        Assert.True(Navigation.History.First().Options.ReplaceHistoryEntry, "typing must not add a history entry");
        Assert.Equal("1–10 of 10", cut.Find("[data-testid=count]").TextContent);
    }

    [Fact]
    public async Task Searching_goes_back_to_the_first_page()
    {
        Navigation.NavigateTo("/people?p=3");
        var cut = RenderList();
        await WaitForRows(cut, names => names[0] == "Person 21");

        await Type(cut, "Paris");

        await WaitForRows(cut, names => names.Length == 10);
        Assert.Equal("?q=Paris", Query(Navigation));
        Assert.Equal("1–10 of 22", cut.Find("[data-testid=count]").TextContent);
    }

    [Fact]
    public async Task The_text_is_trimmed_and_the_same_search_with_other_spaces_changes_nothing()
    {
        var cut = RenderList();
        await Type(cut, "  Person 4  ", 1);
        await WaitForRows(cut, names => names.Length == 6);
        Assert.Equal("?q=Person%204", Query(Navigation));
        var navigations = 0;
        Navigation.LocationChanged += (_, _) => Interlocked.Increment(ref navigations);

        // Typing a space after the text: not a search of its own, and no write.
        await Type(cut, "Person 4 ", 2);
        await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(0, navigations);
        Assert.Equal("?q=Person%204", Query(Navigation));
    }

    [Fact]
    public async Task A_text_shorter_than_the_minimum_searches_nothing_and_clears_an_older_search()
    {
        var cut = RenderList(p => p.Add(c => c.MinLength, 3));
        await Type(cut, "Per", 1);
        await WaitForRows(cut, names => names.Length == 10);
        Assert.Equal("?q=Per", Query(Navigation));

        await Type(cut, "Pe", 2);

        await cut.WaitForAssertionAsync(() => Assert.Equal("", Query(Navigation)));
        await Type(cut, "  P ", 3);
        Assert.Equal("", Query(Navigation));
    }

    [Fact]
    public async Task Emptying_the_field_removes_the_search_from_the_address()
    {
        Navigation.NavigateTo("/people?q=Lyon");
        var cut = RenderList();
        await WaitForRows(cut, names => names.Length == 10);
        Assert.Equal("Lyon", cut.Find("input[type=search]").GetAttribute("value"));

        await Type(cut, "");

        await cut.WaitForAssertionAsync(() => Assert.Equal("", Query(Navigation)));
        await WaitForRows(cut, names => names[0] == "Person 01");
    }

    [Fact]
    public async Task The_search_keeps_the_sort_and_the_page_size()
    {
        Navigation.NavigateTo("/people?s=-name&ps=5&p=2");
        var cut = RenderList();

        await Type(cut, "PARIS");

        await cut.WaitForAssertionAsync(() =>
        {
            var query = System.Web.HttpUtility.ParseQueryString(Query(Navigation));
            Assert.Equal("PARIS", query["q"]);
            Assert.Equal("5", query["ps"]);
            Assert.Equal("-name", query["s"]);
            Assert.Null(query["p"]);
        });
        await WaitForRows(cut, names => names.Length == 5);
    }

    [Fact]
    public async Task A_search_in_the_address_fills_the_field_and_a_change_of_the_address_updates_it()
    {
        Navigation.NavigateTo("/people?q=Lyon");
        var cut = RenderList();
        await WaitForRows(cut, names => names.Length == 10);
        Assert.Equal("Lyon", cut.Find("input[type=search]").GetAttribute("value"));

        // Back, a link or a reset button: the field shows it, even text that was still waiting.
        Navigation.NavigateTo("/people?q=Paris");

        await cut.WaitForAssertionAsync(() => Assert.Equal("Paris", cut.Find("input[type=search]").GetAttribute("data-bx-set")));
        await cut.WaitForAssertionAsync(() => Assert.Equal("1–10 of 22", cut.Find("[data-testid=count]").TextContent));
    }

    [Fact]
    public async Task The_clear_button_empties_the_field_and_the_search_and_puts_the_focus_back()
    {
        Navigation.NavigateTo("/people?q=Lyon");
        var cut = RenderList();
        await WaitForRows(cut, names => names.Length == 10);

        cut.Find("[data-slot=search-clear]").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal("", Query(Navigation)));
        Assert.Equal("", cut.Find("input[type=search]").GetAttribute("data-bx-set"));
        await cut.WaitForAssertionAsync(() => Assert.Contains(JSInterop.Invocations, i => i.Identifier == "Blux.tools.focusId"));
    }

    [Fact]
    public async Task A_list_without_url_sync_searches_through_its_state()
    {
        var cut = RenderList(p => p.Add(c => c.UrlSync, false));

        await Type(cut, "Lyon");

        await WaitForRows(cut, names => names.Length == 10);
        Assert.Equal("", Query(Navigation));
        Assert.Equal("1–10 of 23", cut.Find("[data-testid=count]").TextContent);
    }

    [Fact]
    public async Task A_report_is_one_provider_call_and_a_report_that_matches_the_state_calls_nothing()
    {
        var calls = 0;
        var provider = DataListSearchHarness.Schema.InMemoryProvider(People);
        var cut = Render<DataListSearchHarness>(p => p.Add(c => c.Provider, (request, token) =>
        {
            Interlocked.Increment(ref calls);
            return provider(request, token);
        }));
        await WaitForRows(cut, names => names.Length == 10);
        Assert.Equal(1, calls);

        await Type(cut, "Lyon", 1);
        await WaitForRows(cut, names => names.Length == 10 && calls == 2);
        // The browser reports only after a pause: the same text again (Enter after the pause) is not a change.
        await Type(cut, "Lyon", 2);
        await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(2, calls);
    }

    // ---- empty states

    [Fact]
    public async Task A_search_that_matches_nothing_says_so_with_the_text_and_offers_to_clear_it()
    {
        var cut = RenderList();
        await WaitForRows(cut, names => names.Length == 10);

        await Type(cut, "zzz");

        await cut.WaitForAssertionAsync(() => Assert.Equal("No results for “zzz”", cut.Find("tbody [data-slot=title]").TextContent));
        Assert.Equal("Check the spelling or try a different search.", cut.Find("tbody [data-slot=description]").TextContent);
        Assert.Equal("true", cut.Find("tbody [data-filtered]").GetAttribute("data-filtered"));
        // The polite region says it too.
        Assert.Equal("No results for “zzz”", cut.Find("[data-testid=count]").TextContent);
        var action = cut.Find("tbody [data-slot=reset]");
        Assert.Equal("Clear search", action.TextContent.Trim());

        action.Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal("", Query(Navigation)));
        await WaitForRows(cut, names => names.Length == 10);
        Assert.Equal("", cut.Find("input[type=search]").GetAttribute("data-bx-set"));
        await cut.WaitForAssertionAsync(() => Assert.Contains(JSInterop.Invocations, i => i.Identifier == "Blux.tools.focusId"));
    }

    [Fact]
    public async Task A_list_with_no_data_keeps_the_plain_empty_state_without_an_action()
    {
        var cut = Render<DataListSearchHarness>(p => p.Add(c => c.Items, Array.Empty<Person>()));
        await cut.WaitForAssertionAsync(() => Assert.NotEmpty(cut.FindAll("tbody [data-slot=title]")));

        Assert.Equal("No results", cut.Find("tbody [data-slot=title]").TextContent);
        Assert.Equal("Try changing your search or your filters.", cut.Find("tbody [data-slot=description]").TextContent);
        Assert.Empty(cut.FindAll("tbody [data-filtered]"));
        Assert.Empty(cut.FindAll("tbody [data-slot=reset]"));
    }

    [Fact]
    public async Task The_empty_state_of_a_typed_search_is_the_filtered_one_from_the_first_render()
    {
        Navigation.NavigateTo("/people?q=nobody");
        var cut = RenderList();

        await cut.WaitForAssertionAsync(() => Assert.Equal("No results for “nobody”", cut.Find("tbody [data-slot=title]").TextContent));
    }

    [Fact]
    public async Task The_context_knows_when_the_list_is_filtered_and_resets_to_the_defaults()
    {
        var schema = DataListSearchHarness.Schema;
        var state = schema.DefaultState with { Search = "x", Page = 4, PageSize = 25, Sort = [new DataListSort("name", true)] };
        DataListContext<Person>? context = null;
        Navigation.NavigateTo("/people?q=x&p=4");
        var cut = Render<BxDataList<Person>>(p => p.Add(c => c.Schema, schema).Add(c => c.Items, People).AddChildContent<CaptureContext<Person>>(
            c => c.Add(x => x.OnContext, captured => context = captured)));

        Assert.NotNull(context);
        Assert.True(context!.IsFiltered);
        Assert.False(context.IsFilteredFor(schema.DefaultState));
        var reset = context.ResetState(state);
        Assert.Equal("", reset.Search);
        Assert.Equal(1, reset.Page);
        // The sort, the page size and the layout stay.
        Assert.Equal(25, reset.PageSize);
        Assert.Equal("name", reset.Sort[0].Key);
        await cut.InvokeAsync(() => context.Reset());
        await cut.WaitForAssertionAsync(() => Assert.Equal("", Query(Navigation)));
        await cut.WaitForAssertionAsync(() => Assert.False(context.IsFiltered));
    }

    // ---- static rendering

    private sealed class StaticContext : BluxToolsTestContext
    {
        public StaticContext() : base(interactive: false) { }

        public NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();
    }

    [Fact]
    public void Static_rendering_has_a_get_form_that_keeps_the_other_parameters_and_drops_the_page()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/people?p=3&s=-name&ps=5&utm_source=mail&q=Lyon");

        var cut = context.Render<DataListSearchHarness>(p => p.Add(c => c.Items, People).Add(c => c.Label, "People"));

        var form = cut.Find("form[role=search]");
        Assert.Equal("get", form.GetAttribute("method"));
        Assert.Equal("/people", form.GetAttribute("action"));
        Assert.Equal("Search People", form.GetAttribute("aria-label"));
        var hidden = Hidden(form).ToDictionary(h => h.Name, h => h.Value);
        Assert.Equal("mail", hidden["utm_source"]);
        Assert.Equal("-name", hidden["s"]);
        Assert.Equal("5", hidden["ps"]);
        // The page goes back to the first and the field supplies the search.
        Assert.DoesNotContain("p", hidden.Keys);
        Assert.DoesNotContain("q", hidden.Keys);
        var input = form.QuerySelector("input[type=search]")!;
        Assert.Equal("q", input.GetAttribute("name"));
        Assert.Equal("Lyon", input.GetAttribute("value"));
        Assert.Equal("Search", form.QuerySelector("button[type=submit]")!.TextContent.Trim());
        Assert.Empty(cut.FindAll("button[data-slot=search-clear]"));
        var clear = cut.Find("a[data-slot=search-clear]");
        Assert.Equal("Clear search", clear.GetAttribute("aria-label"));
        var href = System.Web.HttpUtility.ParseQueryString(new Uri("http://x" + clear.GetAttribute("href")).Query);
        Assert.Null(href["q"]);
        Assert.Null(href["p"]);
        Assert.Equal("-name", href["s"]);
        Assert.Equal("mail", href["utm_source"]);
    }

    [Fact]
    public void Static_rendering_prefixes_the_fields_with_the_url_key_and_leaves_the_other_lists_alone()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/people?users.p=2&other.p=4&other.q=x");

        var cut = context.Render<DataListSearchHarness>(p => p.Add(c => c.Items, People).Add(c => c.UrlKey, "users"));

        var form = cut.Find("form[role=search]");
        Assert.Equal("users.q", form.QuerySelector("input[type=search]")!.GetAttribute("name"));
        var hidden = Hidden(form).ToDictionary(h => h.Name, h => h.Value);
        Assert.Equal("4", hidden["other.p"]);
        Assert.Equal("x", hidden["other.q"]);
        Assert.DoesNotContain("users.p", hidden.Keys);
        // No search yet: no clear link.
        Assert.Empty(cut.FindAll("a[data-slot=search-clear]"));
    }

    [Fact]
    public void Static_rendering_asks_the_browser_to_refuse_a_text_shorter_than_the_minimum()
    {
        using var context = new StaticContext();

        var cut = context.Render<DataListSearchHarness>(p => p.Add(c => c.Items, People).Add(c => c.MinLength, 3));

        Assert.Equal("3", cut.Find("input[type=search]").GetAttribute("minlength"));
    }

    [Fact]
    public void Static_rendering_without_url_sync_has_a_disabled_field_and_no_form()
    {
        using var context = new StaticContext();

        var cut = context.Render<DataListSearchHarness>(p => p.Add(c => c.Items, People).Add(c => c.UrlSync, false));

        Assert.Empty(cut.FindAll("form"));
        Assert.NotNull(cut.Find("input[type=search]").GetAttribute("disabled"));
    }

    [Fact]
    public void Static_rendering_of_a_search_that_matches_nothing_links_to_the_address_without_it()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/people?q=zzz&s=-name&utm_source=mail");

        var cut = context.Render<DataListSearchHarness>(p => p.Add(c => c.Items, People));

        Assert.Equal("No results for “zzz”", cut.Find("tbody [data-slot=title]").TextContent);
        var action = cut.Find("tbody a[data-slot=reset]");
        Assert.Equal("Clear search", action.TextContent.Trim());
        var href = System.Web.HttpUtility.ParseQueryString(new Uri("http://x" + action.GetAttribute("href")).Query);
        Assert.Null(href["q"]);
        Assert.Equal("-name", href["s"]);
        Assert.Equal("mail", href["utm_source"]);
    }

    [Fact]
    public void A_state_with_spaces_around_the_search_is_normalized()
    {
        var schema = DataListSearchHarness.Schema;
        var controller = new DataListController<Person>(schema, DataListController<Person>.CreateProvider(schema, People, null));

        Assert.Equal("a b", controller.Normalize(schema.DefaultState with { Search = "  a b " }).Search);
        Assert.Same(schema.DefaultState, controller.Normalize(schema.DefaultState));
    }

    private sealed class Customized : BluxToolsTestContext
    {
        public Customized(Action<BluxToolsOptions> configure) : base(configure) { }
    }
}
