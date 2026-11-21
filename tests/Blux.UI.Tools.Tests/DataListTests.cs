using Blux.UI.Tools.Query;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

/// <summary>The data list in a renderer: the table, aria-sort, links in static rendering, buttons and URL writes when interactive, loading, errors and sources.</summary>
public class DataListTests : BluxToolsTestContext
{
    private static readonly Person[] People = DataListControllerTests.People;

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    private IRenderedComponent<DataListHarness> RenderList(Action<ComponentParameterCollectionBuilder<DataListHarness>>? configure = null) =>
        Render<DataListHarness>(p =>
        {
            p.Add(c => c.Items, People);
            configure?.Invoke(p);
        });

    private IRenderedComponent<DataListHarness> RenderProvider(DataListProvider<Person> provider) =>
        Render<DataListHarness>(p => p.Add(c => c.Provider, provider));

    private static string[] Names(IRenderedComponent<DataListHarness> cut) =>
        [.. cut.FindAll("tbody tr[data-slot=row]").Select(r => r.QuerySelector("td")!.TextContent)];

    private static async Task<IRenderedComponent<DataListHarness>> Settled(IRenderedComponent<DataListHarness> cut)
    {
        await cut.WaitForAssertionAsync(() => Assert.Empty(cut.FindAll("[data-slot=skeleton-row]")));
        return cut;
    }

    [Fact]
    public void Renders_a_table_with_the_first_page_sorted_by_the_default_sort()
    {
        var cut = RenderList();

        Assert.Equal(["Name", "Age", "Town"], cut.FindAll("thead th").Select(h => h.TextContent.Trim()));
        Assert.Equal(10, cut.FindAll("tbody tr[data-slot=row]").Count);
        Assert.Equal("Person 01", Names(cut)[0]);
        Assert.Equal("People", cut.Find("caption").TextContent);
        // The template, the format and the alignment of the columns.
        var first = cut.Find("tbody tr[data-slot=row]");
        Assert.Equal("Lyon", first.QuerySelector("td:nth-child(3) strong")!.TextContent);
        Assert.Contains("city-col", first.QuerySelector("td:nth-child(3)")!.ClassName);
        Assert.Equal("21.0", first.QuerySelector("td:nth-child(2)")!.TextContent);
        Assert.Contains("text-end", first.QuerySelector("td:nth-child(2)")!.ClassName);
    }

    [Fact]
    public void The_table_is_a_labelled_scrolling_region_with_column_headers()
    {
        var cut = RenderList();

        var region = cut.Find("[data-testid=table]");
        Assert.Equal("region", region.GetAttribute("role"));
        Assert.Equal("People", region.GetAttribute("aria-label"));
        Assert.Equal("0", region.GetAttribute("tabindex"));
        Assert.Contains("overflow-x-auto", region.ClassName);
        Assert.All(cut.FindAll("thead th"), th => Assert.Equal("col", th.GetAttribute("scope")));
    }

    [Fact]
    public void The_result_count_is_a_polite_live_region_with_the_rows_shown()
    {
        var cut = RenderList();

        var count = cut.Find("[data-testid=count]");
        Assert.Equal("status", count.GetAttribute("role"));
        Assert.Equal("polite", count.GetAttribute("aria-live"));
        Assert.Equal("true", count.GetAttribute("aria-atomic"));
        Assert.Equal("1–10 of 45", count.TextContent);
    }

    [Fact]
    public void Numbers_of_the_count_follow_the_culture()
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        var uiCulture = System.Globalization.CultureInfo.CurrentUICulture;
        // A copy of fr-FR with a visible group separator: the test does not depend on the ICU data of the machine.
        var french = (System.Globalization.CultureInfo)new System.Globalization.CultureInfo("fr-FR").Clone();
        french.NumberFormat.NumberGroupSeparator = "_";
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.CurrentUICulture = french;
        try
        {
            var many = Enumerable.Range(1, 2500).Select(i => new Person(i, $"Person {i}", 20, "Lyon")).ToArray();
            var cut = Render<DataListHarness>(p => p.Add(c => c.Items, many));

            Assert.Equal("1–10 sur 2_500", cut.Find("[data-testid=count]").TextContent);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = culture;
            System.Globalization.CultureInfo.CurrentUICulture = uiCulture;
        }
    }

    [Fact]
    public void Sortable_headers_carry_aria_sort_and_the_others_do_not()
    {
        var cut = RenderList();

        var headers = cut.FindAll("thead th");
        Assert.Equal("ascending", headers[0].GetAttribute("aria-sort"));
        Assert.Equal("none", headers[1].GetAttribute("aria-sort"));
        // city is not sortable in the schema, whatever the column says.
        Assert.Null(headers[2].GetAttribute("aria-sort"));
        Assert.Empty(headers[2].QuerySelectorAll("button, a"));
    }

    [Fact]
    public void Sort_buttons_are_named_by_what_a_click_does()
    {
        var cut = RenderList();

        Assert.Equal("Sort by Name, descending", cut.Find("th[data-key=name] button").GetAttribute("aria-label"));
        Assert.Equal("Sort by Age, ascending", cut.Find("th[data-key=age] button").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task A_click_on_a_sort_button_sorts_pushes_the_url_and_resets_the_page()
    {
        Navigation.NavigateTo("/people?p=2");
        var cut = RenderList();

        cut.Find("th[data-key=age] button").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal("ascending", cut.Find("th[data-key=age]").GetAttribute("aria-sort")));
        await cut.WaitForAssertionAsync(() => Assert.Equal("/people?s=age", new Uri(Navigation.Uri).PathAndQuery));
        Assert.Equal("none", cut.Find("th[data-key=name]").GetAttribute("aria-sort"));
        Assert.Equal("Sort by Age, descending", cut.Find("th[data-key=age] button").GetAttribute("aria-label"));
        Assert.Equal(10, cut.FindAll("tbody tr[data-slot=row]").Count);
        Assert.Equal("1–10 of 45", cut.Find("[data-testid=count]").TextContent);
    }

    [Fact]
    public async Task A_second_click_sorts_descending_and_a_third_goes_back_to_the_default()
    {
        var cut = RenderList();

        cut.Find("th[data-key=age] button").Click();
        await cut.WaitForAssertionAsync(() => Assert.Equal("ascending", cut.Find("th[data-key=age]").GetAttribute("aria-sort")));
        cut.Find("th[data-key=age] button").Click();
        await cut.WaitForAssertionAsync(() => Assert.Equal("descending", cut.Find("th[data-key=age]").GetAttribute("aria-sort")));
        Assert.Equal("Clear the sort by Age", cut.Find("th[data-key=age] button").GetAttribute("aria-label"));
        await cut.WaitForAssertionAsync(() => Assert.Equal("?s=-age", new Uri(Navigation.Uri).Query));

        cut.Find("th[data-key=age] button").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal("ascending", cut.Find("th[data-key=name]").GetAttribute("aria-sort")));
        await cut.WaitForAssertionAsync(() => Assert.Equal("", new Uri(Navigation.Uri).Query));
    }

    [Fact]
    public async Task A_click_on_a_page_loads_it_and_pushes_the_url()
    {
        var cut = RenderList();

        cut.Find("nav[aria-label=Pagination] button[aria-label='Page 2']").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal("Person 11", Names(cut)[0]));
        await cut.WaitForAssertionAsync(() => Assert.Equal("?p=2", new Uri(Navigation.Uri).Query));
        Assert.Equal("11–20 of 45", cut.Find("[data-testid=count]").TextContent);
        Assert.Equal("page", cut.Find("nav button[aria-label='Page 2']").GetAttribute("aria-current"));
    }

    [Fact]
    public async Task A_new_page_size_goes_back_to_the_first_page()
    {
        Navigation.NavigateTo("/people?p=3");
        var cut = RenderList();

        cut.Find("[data-slot=page-size] select").Change("25");

        await cut.WaitForAssertionAsync(() => Assert.Equal(25, cut.FindAll("tbody tr[data-slot=row]").Count));
        await cut.WaitForAssertionAsync(() => Assert.Equal("?ps=25", new Uri(Navigation.Uri).Query));
        Assert.Equal("1–25 of 45", cut.Find("[data-testid=count]").TextContent);
        Assert.Equal("Rows per page", cut.Find("[data-slot=page-size] label").TextContent);
    }

    [Fact]
    public void A_typed_url_loads_its_state()
    {
        Navigation.NavigateTo("/people?p=2&s=-age&ps=5&utm_source=mail");

        var cut = RenderList();

        Assert.Equal(5, cut.FindAll("tbody tr[data-slot=row]").Count);
        Assert.Equal("descending", cut.Find("th[data-key=age]").GetAttribute("aria-sort"));
        Assert.Equal("6–10 of 45", cut.Find("[data-testid=count]").TextContent);
        Assert.Equal("5", ((AngleSharp.Html.Dom.IHtmlSelectElement)cut.Find("[data-slot=page-size] select")).Value);
    }

    [Fact]
    public void A_bad_url_value_falls_back_to_the_default()
    {
        Navigation.NavigateTo("/people?p=abc&s=nope&ps=abc");

        var cut = RenderList();

        Assert.Equal("Person 01", Names(cut)[0]);
        Assert.Equal("1–10 of 45", cut.Find("[data-testid=count]").TextContent);
    }

    [Fact]
    public void A_page_past_the_last_one_shows_the_last_page_even_when_the_url_cannot_change()
    {
        Navigation.NavigateTo("/people?p=99");

        var cut = RenderList();

        Assert.Equal(["Person 41", "Person 42", "Person 43", "Person 44", "Person 45"], Names(cut));
        Assert.Equal("41–45 of 45", cut.Find("[data-testid=count]").TextContent);
    }

    [Fact]
    public async Task A_page_past_the_last_one_is_replaced_in_the_url_by_an_interactive_list()
    {
        Navigation.NavigateTo("/people?p=99&utm_source=mail");

        var cut = RenderList();

        Assert.Equal("41–45 of 45", cut.Find("[data-testid=count]").TextContent);
        await cut.WaitForAssertionAsync(() => Assert.Equal("?p=5&utm_source=mail", new Uri(Navigation.Uri).Query));
    }

    [Fact]
    public async Task Back_and_forward_follow_the_url()
    {
        var cut = RenderList();

        Navigation.NavigateTo("/?p=3");

        await cut.WaitForAssertionAsync(() => Assert.Equal("Person 21", Names(cut)[0]));
        Navigation.NavigateTo("/");
        await cut.WaitForAssertionAsync(() => Assert.Equal("Person 01", Names(cut)[0]));
    }

    [Fact]
    public async Task A_url_key_prefixes_the_keys_and_leaves_other_parameters_alone()
    {
        Navigation.NavigateTo("/people?utm_source=mail&p=4");
        var cut = RenderList(p => p.Add(c => c.UrlKey, "users"));

        // p=4 is not the key of this list: the first page shows.
        Assert.Equal("Person 01", Names(cut)[0]);
        cut.Find("th[data-key=age] button").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal("?utm_source=mail&p=4&users.s=age", new Uri(Navigation.Uri).Query));
    }

    [Fact]
    public async Task Two_lists_with_different_prefixes_do_not_touch_each_other()
    {
        Navigation.NavigateTo("/people?a.p=2&b.p=3");
        var cut = Render<TwoLists>(p => p.Add(c => c.Items, People));

        await cut.WaitForAssertionAsync(() => Assert.Equal("Person 11", cut.Find("[data-testid=a] tbody td").TextContent));
        Assert.Equal("Person 21", cut.Find("[data-testid=b] tbody td").TextContent);
        cut.Find("[data-testid=a] th[data-key=age] button").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal("?b.p=3&a.s=age", new Uri(Navigation.Uri).Query));
        Assert.Equal("Person 21", cut.Find("[data-testid=b] tbody td").TextContent);
    }

    [Fact]
    public void Two_lists_that_share_the_url_keys_say_so()
    {
        var error = Assert.ThrowsAny<Exception>(() => Render<TwoLists>(p => p.Add(c => c.Items, People).Add(c => c.SamePrefix, true)));

        Assert.Contains("claimed", error.ToString());
    }

    [Fact]
    public void Static_rendering_sorts_with_links_that_carry_the_state()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/people?p=2&utm_source=mail");

        var cut = context.Render<DataListHarness>(p => p.Add(c => c.Items, People));

        Assert.Empty(cut.FindAll("thead button"));
        Assert.Equal("/people?utm_source=mail&s=age", cut.Find("th[data-key=age] a").GetAttribute("href"));
        Assert.Equal("Sort by Age, ascending", cut.Find("th[data-key=age] a").GetAttribute("aria-label"));
        Assert.Equal("/people?utm_source=mail&s=-name", cut.Find("th[data-key=name] a").GetAttribute("href"));
        Assert.Equal("ascending", cut.Find("th[data-key=name]").GetAttribute("aria-sort"));
    }

    [Fact]
    public void Static_rendering_pages_with_links()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/people?s=-age&utm_source=mail");

        var cut = context.Render<DataListHarness>(p => p.Add(c => c.Items, People));

        var links = cut.FindAll("nav[aria-label=Pagination] a");
        Assert.Empty(cut.FindAll("nav[aria-label=Pagination] button"));
        Assert.Equal("/people?s=-age&utm_source=mail&p=2", cut.Find("a[aria-label='Page 2']").GetAttribute("href"));
        Assert.Equal("/people?s=-age&utm_source=mail&p=5", cut.Find("a[aria-label='Page 5']").GetAttribute("href"));
        Assert.Null(cut.Find("a[aria-label='Previous page']").GetAttribute("href"));
        Assert.Equal("/people?s=-age&utm_source=mail&p=2", cut.Find("a[aria-label='Next page']").GetAttribute("href"));
        Assert.Equal("page", cut.Find("a[aria-label='Page 1']").GetAttribute("aria-current"));
        Assert.NotEmpty(links);
    }

    [Fact]
    public void Static_rendering_changes_the_page_size_with_a_get_form_that_keeps_the_rest()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/people?p=3&s=-age&utm_source=mail");

        var cut = context.Render<DataListHarness>(p => p.Add(c => c.Items, People));

        var form = cut.Find("[data-slot=page-size] form");
        Assert.Equal("get", form.GetAttribute("method"));
        Assert.Equal("/people", form.GetAttribute("action"));
        var hidden = form.QuerySelectorAll("input[type=hidden]").ToDictionary(i => i.GetAttribute("name")!, i => i.GetAttribute("value"));
        Assert.Equal("mail", hidden["utm_source"]);
        Assert.Equal("-age", hidden["s"]);
        // The page goes back to the first and the select supplies the size.
        Assert.DoesNotContain("p", hidden.Keys);
        Assert.DoesNotContain("ps", hidden.Keys);
        Assert.Equal("ps", form.QuerySelector("select")!.GetAttribute("name"));
        Assert.Equal("Apply", form.QuerySelector("button[type=submit]")!.TextContent.Trim());
        Assert.Equal("Rows per page", form.QuerySelector("label")!.TextContent);
    }

    [Fact]
    public void Static_rendering_prefixes_the_form_fields_and_the_links_with_the_url_key()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/people?users.s=-age&users.p=2");

        var cut = context.Render<DataListHarness>(p => p.Add(c => c.Items, People).Add(c => c.UrlKey, "users"));

        Assert.Equal("users.ps", cut.Find("[data-slot=page-size] select").GetAttribute("name"));
        Assert.Equal("/people?users.s=-age&users.p=3", cut.Find("a[aria-label='Page 3']").GetAttribute("href"));
        Assert.Equal("-age", cut.Find("[data-slot=page-size] input[name='users.s']").GetAttribute("value"));
    }

    [Fact]
    public void Static_rendering_without_url_sync_has_inert_controls_and_no_links()
    {
        using var context = new StaticContext();

        var cut = context.Render<DataListHarness>(p => p.Add(c => c.Items, People).Add(c => c.UrlSync, false));

        Assert.Empty(cut.FindAll("thead a, thead button"));
        Assert.All(cut.FindAll("nav[aria-label=Pagination] button"), b => Assert.True(b.HasAttribute("disabled")));
        Assert.True(cut.Find("[data-slot=page-size] select").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("[data-slot=refresh]"));
    }

    [Fact]
    public void Static_rendering_refreshes_with_a_link_to_the_same_page()
    {
        using var context = new StaticContext();
        context.Navigation.NavigateTo("/people?p=2");

        var cut = context.Render<DataListHarness>(p => p.Add(c => c.Items, People));

        var refresh = cut.Find("[data-testid=refresh]");
        Assert.Equal("A", refresh.TagName);
        Assert.Equal("/people?p=2", refresh.GetAttribute("href"));
        Assert.Equal("Refresh", refresh.TextContent.Trim());
    }

    [Fact]
    public async Task Shows_placeholder_rows_while_the_first_rows_load()
    {
        var gate = new TaskCompletionSource<DataListResult<Person>>();
        var cut = RenderProvider((_, _) => new ValueTask<DataListResult<Person>>(gate.Task));

        Assert.Equal(10, cut.FindAll("[data-slot=skeleton-row]").Count);
        Assert.Equal("true", cut.Find("tbody").GetAttribute("aria-busy"));
        Assert.Equal("Loading…", cut.Find("[data-testid=count]").TextContent);
        Assert.Empty(cut.FindAll("[data-testid=pagination] nav"));

        gate.SetResult(new DataListResult<Person>([.. People.Take(2)], 2));

        await cut.WaitForAssertionAsync(() => Assert.Empty(cut.FindAll("[data-slot=skeleton-row]")));
        Assert.Equal(2, cut.FindAll("tbody tr[data-slot=row]").Count);
        Assert.Null(cut.Find("tbody").GetAttribute("aria-busy"));
        Assert.Equal("1–2 of 2", cut.Find("[data-testid=count]").TextContent);
    }

    [Fact]
    public async Task Keeps_the_rows_on_screen_and_marks_the_table_busy_while_another_page_loads()
    {
        var pending = new TaskCompletionSource<DataListResult<Person>>();
        var provider = DataListControllerTests.Schema.InMemoryProvider(People);
        var calls = 0;
        var cut = RenderProvider((request, token) => ++calls == 1 ? provider(request, token) : new ValueTask<DataListResult<Person>>(pending.Task));
        await Settled(cut);

        cut.Find("button[aria-label='Page 2']").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal("true", cut.Find("tbody").GetAttribute("aria-busy")));
        Assert.Equal("Person 01", Names(cut)[0]);
        Assert.Equal("1–10 of 45", cut.Find("[data-testid=count]").TextContent);
        Assert.Empty(cut.FindAll("[data-slot=skeleton-row]"));

        pending.SetResult(new DataListResult<Person>([People[10]], 45));
        await cut.WaitForAssertionAsync(() => Assert.Equal("Person 11", Names(cut)[0]));
        Assert.Null(cut.Find("tbody").GetAttribute("aria-busy"));
    }

    [Fact]
    public async Task An_error_shows_the_alert_and_retry_loads_again()
    {
        var provider = DataListControllerTests.Schema.InMemoryProvider(People);
        var calls = 0;
        var cut = RenderProvider((request, token) =>
            ++calls == 1 ? throw new InvalidOperationException("boom") : provider(request, token));

        await cut.WaitForAssertionAsync(() => Assert.NotEmpty(cut.FindAll("[data-testid=error]")));
        var alert = cut.Find("[data-testid=error]");
        Assert.Equal("alert", alert.GetAttribute("role"));
        Assert.Contains("Could not load the data", alert.TextContent);
        Assert.Empty(cut.FindAll("[data-slot=skeleton-row]"));
        Assert.Empty(cut.FindAll("[data-slot=empty-row]"));

        cut.Find("[data-testid=error] [data-slot=retry]").Click();

        await cut.WaitForAssertionAsync(() => Assert.Empty(cut.FindAll("[data-testid=error]")));
        Assert.Equal(10, cut.FindAll("tbody tr[data-slot=row]").Count);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task The_refresh_button_loads_the_rows_again()
    {
        var provider = DataListControllerTests.Schema.InMemoryProvider(People);
        var calls = 0;
        var cut = RenderProvider((request, token) =>
        {
            calls++;
            return provider(request, token);
        });
        await Settled(cut);

        cut.Find("[data-testid=refresh]").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal(2, calls));
        Assert.Equal(10, cut.FindAll("tbody tr[data-slot=row]").Count);
    }

    [Fact]
    public void An_empty_list_shows_the_empty_state_and_the_hook_gets_the_list()
    {
        var cut = Render<DataListHarness>(p => p.Add(c => c.Items, Array.Empty<Person>()));

        Assert.Equal("No results", cut.Find("[data-slot=empty-row] [data-slot=title]").TextContent);
        Assert.Equal("3", cut.Find("[data-slot=empty-row] td").GetAttribute("colspan"));
        Assert.Equal("No results", cut.Find("[data-testid=count]").TextContent);
        Assert.Empty(cut.FindAll("[data-testid=pagination] nav"));
    }

    [Fact]
    public void The_empty_state_can_be_replaced_and_tells_filtered_from_not()
    {
        var cut = Render<DataListHarness>(p => p
            .Add(c => c.Items, Array.Empty<Person>())
            .Add(c => c.EmptyContent, context => builder => builder.AddContent(0, context.IsFiltered ? "nothing matches" : "nothing exists")));

        Assert.Equal("nothing exists", cut.Find("[data-slot=empty-row] td").TextContent.Trim());
    }

    [Fact]
    public async Task Without_url_sync_the_list_keeps_its_state_and_reports_it()
    {
        Navigation.NavigateTo("/people?p=3");
        DataListState? reported = null;
        var cut = RenderList(p => p
            .Add(c => c.UrlSync, false)
            .Add(c => c.StateChanged, EventCallback.Factory.Create<DataListState>(this, state => reported = state)));

        // The URL is not read.
        Assert.Equal("Person 01", Names(cut)[0]);
        cut.Find("button[aria-label='Page 2']").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal("Person 11", Names(cut)[0]));
        Assert.Equal(2, reported!.Page);
        Assert.Equal("?p=3", new Uri(Navigation.Uri).Query);
    }

    [Fact]
    public async Task Without_url_sync_a_new_state_parameter_is_followed()
    {
        var cut = RenderList(p => p.Add(c => c.UrlSync, false).Add(c => c.State, DataListControllerTests.Schema.DefaultState with { Page = 2 }));
        Assert.Equal("Person 11", Names(cut)[0]);

        cut.Render(p => p.Add(c => c.State, DataListControllerTests.Schema.DefaultState with { Page = 4 }));

        await cut.WaitForAssertionAsync(() => Assert.Equal("Person 31", Names(cut)[0]));
    }

    [Fact]
    public async Task A_list_without_a_schema_is_described_by_its_columns()
    {
        var cut = Render<DataListImplicitHarness>(p => p.Add(c => c.Items, People));

        await cut.WaitForAssertionAsync(() => Assert.Equal(45, cut.FindAll("tbody tr[data-slot=row]").Count));
        var headers = cut.FindAll("thead th");
        Assert.Equal(["Full name", "Age", "City"], headers.Select(h => h.TextContent.Trim()));
        Assert.Equal("none", headers[0].GetAttribute("aria-sort"));
        Assert.Equal("none", headers[1].GetAttribute("aria-sort"));
        // Sortable="false" on the column.
        Assert.Null(headers[2].GetAttribute("aria-sort"));
        Assert.Equal("Person 01", cut.Find("tbody td").TextContent);

        cut.Find("th button[aria-label='Sort by Full name, ascending']").Click();
        cut.Find("th button[aria-label='Sort by Full name, descending']").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal("Person 45", cut.Find("tbody td").TextContent));
        Assert.Equal("1–45 of 45", cut.Find("[data-testid=count]").TextContent);
    }

    [Fact]
    public void A_list_takes_exactly_one_source()
    {
        Assert.Throws<InvalidOperationException>(() => Render<DataListHarness>(p => p.Add(c => c.Items, null)));
        Assert.Throws<InvalidOperationException>(() => Render<DataListHarness>(p => p
            .Add(c => c.Items, People).Add(c => c.Provider, DataListControllerTests.Schema.InMemoryProvider(People))));
    }

    [Fact]
    public void A_provider_needs_a_schema()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Render<DataListHarness>(p => p
            .Add(c => c.Schema, null).Add(c => c.Provider, DataListControllerTests.Schema.InMemoryProvider(People))));

        Assert.Contains("Schema", error.Message);
    }

    [Fact]
    public void A_table_without_columns_shows_the_fields_of_the_schema()
    {
        var cut = Render<BxDataList<Person>>(p => p.Add(c => c.Items, People).Add(c => c.Schema, DataListControllerTests.Schema).Add(c => c.UrlSync, false));

        Assert.Equal(["Name", "Age", "City"], cut.FindAll("thead th").Select(h => h.TextContent.Trim()));
        Assert.Equal("Person 01", cut.Find("tbody td").TextContent);
        // The default layout: count, refresh, table, pagination.
        Assert.NotNull(cut.Find("[data-slot=result-count]"));
        Assert.NotNull(cut.Find("[data-slot=pagination]"));
    }

    [Fact]
    public void Density_changes_the_padding_of_the_cells()
    {
        var compact = RenderList(p => p.Add(c => c.Density, DataListDensity.Compact).Add(c => c.UrlSync, false));
        var comfortable = RenderList(p => p.Add(c => c.Density, DataListDensity.Comfortable).Add(c => c.UrlSync, false));

        Assert.Contains("py-1.5", compact.Find("tbody td").ClassName);
        Assert.Contains("py-5", comfortable.Find("tbody td").ClassName);
    }

    [Fact]
    public void Texts_follow_the_culture()
    {
        var uiCulture = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("fr-FR");
        try
        {
            var cut = RenderList();

            Assert.Equal("Trier par Age, croissant", cut.Find("th[data-key=age] button").GetAttribute("aria-label"));
            Assert.Equal("Lignes par page", cut.Find("[data-slot=page-size] label").TextContent);
            Assert.Equal("Actualiser", cut.Find("[data-testid=refresh]").TextContent.Trim());
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = uiCulture;
        }
    }

    [Fact]
    public void The_slots_come_from_the_theme_of_the_package()
    {
        using var context = new Customized();
        var cut = context.Render<DataListHarness>(p => p.Add(c => c.Items, People));

        Assert.Contains("rounded-none", cut.Find("[data-testid=table]").ClassName);
        Assert.Contains("custom-root", cut.Find("[data-testid=list]").ClassName);
    }

    [Fact]
    public async Task The_time_zone_of_the_browser_goes_out_with_the_request_of_an_interactive_list()
    {
        JSInterop.Setup<string?>("Blux.tools.timeZone").SetResult("Pacific/Auckland");
        DataListRequest? seen = null;
        var provider = DataListControllerTests.Schema.InMemoryProvider(People);

        var cut = RenderProvider((request, token) =>
        {
            seen = request;
            return provider(request, token);
        });
        await Settled(cut);

        Assert.Equal("Pacific/Auckland", seen!.TimeZone);
    }

    [Fact]
    public void The_time_zone_stays_empty_in_static_rendering()
    {
        using var context = new StaticContext();
        DataListRequest? seen = null;
        var provider = DataListControllerTests.Schema.InMemoryProvider(People);

        context.Render<DataListHarness>(p => p.Add(c => c.Provider, (request, token) =>
        {
            seen = request;
            return provider(request, token);
        }));

        Assert.Null(seen!.TimeZone);
    }

    [Fact]
    public async Task A_page_change_keeps_focus_in_the_pagination()
    {
        var cut = RenderList();

        cut.Find("button[aria-label='Page 2']").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal("Person 11", Names(cut)[0]));
        await cut.WaitForAssertionAsync(() => Assert.Contains(JSInterop.Invocations, i => i.Identifier == "Blux.tools.focusPage"));
    }

    private sealed class StaticContext : BluxToolsTestContext
    {
        public StaticContext() : base(interactive: false) { }

        public NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();
    }

    private sealed class Customized : BluxToolsTestContext
    {
        public Customized() : base(t => t.DataList.Slot("tableRoot", "rounded-none").Slot("root", "custom-root")) { }
    }
}
