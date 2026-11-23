using Blux.UI.Tools.Query;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

/// <summary>Two-way state, schema-less lists while they load, columns, accessible names, the persisted key and how often parts render.</summary>
public class DataListBehaviorTests : BluxToolsTestContext
{
    private static readonly Person[] People = DataListControllerTests.People;

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    private static string First(IRenderedComponent<DataListBindHarness> cut) => cut.Find("tbody td").TextContent;

    // ---- @bind-State

    [Fact]
    public async Task A_bound_state_without_url_sync_loads_once_per_change_when_the_parent_feeds_it_back()
    {
        var cut = Render<DataListBindHarness>(p => p.Add(c => c.UrlSync, false));
        Assert.Equal(1, cut.Instance.Calls);

        cut.Find("button[aria-label='Page 2']").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal("Person 11", First(cut)));
        // The list reported page 2, the parent set State to it, and the list did not load it a second time.
        await cut.WaitForAssertionAsync(() => Assert.Equal("2", cut.Find("[data-testid=bound-page]").TextContent));
        await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(2, cut.Instance.Calls);
    }

    [Fact]
    public async Task A_state_set_by_the_parent_loads_without_url_sync()
    {
        var cut = Render<DataListBindHarness>(p => p.Add(c => c.UrlSync, false));

        cut.Find("[data-testid=page3]").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal("Person 21", First(cut)));
        Assert.Equal(2, cut.Instance.Calls);
        cut.Find("[data-testid=reset]").Click();
        await cut.WaitForAssertionAsync(() => Assert.Equal("Person 01", First(cut)));
        Assert.Equal(3, cut.Instance.Calls);
    }

    [Fact]
    public async Task A_state_set_by_the_parent_is_written_to_the_url_with_url_sync_so_a_reset_button_works()
    {
        var cut = Render<DataListBindHarness>(p => p.Add(c => c.UrlSync, true));

        cut.Find("[data-testid=page3]").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal("?p=3", new Uri(Navigation.Uri).Query));
        await cut.WaitForAssertionAsync(() => Assert.Equal("Person 21", First(cut)));

        cut.Find("[data-testid=reset]").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal("", new Uri(Navigation.Uri).Query));
        await cut.WaitForAssertionAsync(() => Assert.Equal("Person 01", First(cut)));
        // The URL change is the one source: each change loaded once, and feeding the state back loaded nothing more.
        await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(3, cut.Instance.Calls);
    }

    [Fact]
    public async Task A_page_clicked_in_the_list_reaches_the_parent_with_url_sync()
    {
        var cut = Render<DataListBindHarness>(p => p.Add(c => c.UrlSync, true));

        cut.Find("button[aria-label='Page 2']").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal("2", cut.Find("[data-testid=bound-page]").TextContent));
        await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(2, cut.Instance.Calls);
        Assert.Equal("?p=2", new Uri(Navigation.Uri).Query);
    }

    [Fact]
    public async Task The_state_of_the_parent_starts_a_list_whose_url_holds_nothing_and_is_written_to_it()
    {
        var cut = Render<DataListBindHarness>(p => p.Add(c => c.UrlSync, true).Add(c => c.Initial, DataListControllerTests.Schema.DefaultState with { Page = 2 }));

        Assert.Equal("Person 11", First(cut));
        await cut.WaitForAssertionAsync(() => Assert.Equal("?p=2", new Uri(Navigation.Uri).Query));
        Assert.Equal(1, cut.Instance.Calls);
    }

    [Fact]
    public void The_url_wins_over_the_state_of_the_parent_at_the_start_and_is_reported_to_it()
    {
        Navigation.NavigateTo("/people?p=4");

        var cut = Render<DataListBindHarness>(p => p.Add(c => c.UrlSync, true).Add(c => c.Initial, DataListControllerTests.Schema.DefaultState with { Page = 2 }));

        Assert.Equal("Person 31", First(cut));
        cut.WaitForAssertion(() => Assert.Equal("4", cut.Find("[data-testid=bound-page]").TextContent));
    }

    // ---- a list without a schema, while it loads

    [Fact]
    public async Task Rows_and_state_that_change_during_the_first_load_of_a_list_without_a_schema_are_applied_when_it_ends()
    {
        // The first load waits for the time zone of the browser: the parent renders again with other rows meanwhile.
        var zone = JSInterop.Setup<string?>("Blux.tools.timeZone");
        var cut = Render<DataListImplicitHarness>(p => p.Add(c => c.Items, People.Take(3).ToArray()));

        cut.Render(p => p.Add(c => c.Items, People.Skip(20).Take(2).ToArray()));
        zone.SetResult("Europe/Paris");

        await cut.WaitForAssertionAsync(() => Assert.Equal(2, cut.FindAll("tbody tr[data-slot=row]").Count));
        Assert.Equal("Person 21", cut.Find("tbody td").TextContent);
    }

    // ---- columns

    [Fact]
    public void A_column_that_an_if_adds_later_comes_last_unless_it_has_an_order()
    {
        var unordered = Render<DataListColumnsHarness>(p => p.Add(c => c.ShowAge, false));
        Assert.Equal("name,city", unordered.Find("[data-testid=columns]").TextContent);
        unordered.Render(p => p.Add(c => c.ShowAge, true));
        Assert.Equal(["Name", "City", "Age"], unordered.FindAll("thead th").Select(h => h.TextContent.Trim()));

        var ordered = Render<DataListColumnsHarness>(p => p.Add(c => c.ShowAge, false).Add(c => c.Ordered, true));
        ordered.Render(p => p.Add(c => c.ShowAge, true));
        Assert.Equal(["Name", "Age", "City"], ordered.FindAll("thead th").Select(h => h.TextContent.Trim()));
        Assert.Equal("name,age,city", ordered.Find("[data-testid=columns]").TextContent);

        ordered.Render(p => p.Add(c => c.ShowAge, false));
        Assert.Equal(["Name", "City"], ordered.FindAll("thead th").Select(h => h.TextContent.Trim()));
    }

    [Fact]
    public void Columns_render_in_the_order_of_the_markup_for_columns_that_exist_from_the_start()
    {
        var cut = Render<DataListColumnsHarness>(p => p.Add(c => c.ShowAge, true));

        Assert.Equal(["Name", "Age", "City"], cut.FindAll("thead th").Select(h => h.TextContent.Trim()));
        Assert.Equal("name,age,city", cut.Find("[data-testid=columns]").TextContent);
    }

    [Fact]
    public void A_hidden_column_is_not_shown_but_stays_in_the_registry_with_its_place()
    {
        var cut = Render<DataListColumnsHarness>(p => p.Add(c => c.ShowAge, true).Add(c => c.ShowCity, false));

        Assert.Equal(["Name", "Age"], cut.FindAll("thead th").Select(h => h.TextContent.Trim()));
        Assert.Equal(2, cut.Find("tbody tr").QuerySelectorAll("td").Length);
        Assert.Equal("name,age,city!", cut.Find("[data-testid=columns]").TextContent);

        cut.Render(p => p.Add(c => c.ShowCity, true));

        Assert.Equal(["Name", "Age", "City"], cut.FindAll("thead th").Select(h => h.TextContent.Trim()));
        Assert.Equal("name,age,city", cut.Find("[data-testid=columns]").TextContent);
    }

    // ---- accessible names

    [Fact]
    public void The_default_layout_names_its_table_and_a_labelled_list_names_both_landmarks()
    {
        var plain = Render<BxDataList<Person>>(p => p.Add(c => c.Items, People).Add(c => c.Schema, DataListControllerTests.Schema).Add(c => c.UrlSync, false));
        Assert.Equal("Results", plain.Find("[data-slot=table-root]").GetAttribute("aria-label"));
        Assert.Equal("region", plain.Find("[data-slot=table-root]").GetAttribute("role"));
        Assert.Equal("Pagination", plain.Find("nav").GetAttribute("aria-label"));

        var labelled = Render<BxDataList<Person>>(p => p.Add(c => c.Items, People).Add(c => c.Schema, DataListControllerTests.Schema).Add(c => c.UrlSync, false).Add(c => c.Label, "People"));
        Assert.Equal("People", labelled.Find("[data-slot=table-root]").GetAttribute("aria-label"));
        Assert.Equal("People", labelled.Find("table").GetAttribute("aria-label"));
        Assert.Equal("Pagination, People", labelled.Find("nav").GetAttribute("aria-label"));
    }

    [Fact]
    public void An_explicit_caption_or_aria_label_wins_over_the_label_of_the_list()
    {
        var cut = Render<DataListBindHarness>(p => p.Add(c => c.UrlSync, false));

        // The harness list is labelled "People" and its table has no caption.
        Assert.Equal("People", cut.Find("[data-slot=table-root]").GetAttribute("aria-label"));
        Assert.Equal("Pagination, People", cut.Find("nav").GetAttribute("aria-label"));
    }

    [Fact]
    public void The_pagination_name_is_localized()
    {
        var uiCulture = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("fr-FR");
        try
        {
            var cut = Render<BxDataList<Person>>(p => p.Add(c => c.Items, People).Add(c => c.Schema, DataListControllerTests.Schema).Add(c => c.UrlSync, false).Add(c => c.Label, "Personnes"));

            Assert.Equal("Navigation par page, Personnes", cut.Find("nav").GetAttribute("aria-label"));
            Assert.Equal("Personnes", cut.Find("[data-slot=table-root]").GetAttribute("aria-label"));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = uiCulture;
        }
    }

    // ---- persisted keys

    [Fact]
    public void Two_lists_of_one_row_type_under_the_same_persisted_key_throw_and_name_the_fix()
    {
        var error = Assert.ThrowsAny<Exception>(() => Render<DataListPersistHarness>());

        Assert.Contains("persist", error.ToString());
        Assert.Contains("UrlKey or PersistKey", error.ToString());
    }

    [Fact]
    public async Task Lists_with_distinct_keys_persist_and_a_key_is_free_again_after_its_list_goes()
    {
        var cut = Render<DataListPersistHarness>(p => p.Add(c => c.FirstKey, "a").Add(c => c.SecondKey, "b"));
        Assert.Equal(2, cut.FindAll("table").Count);
        await DisposeComponentsAsync();

        var again = Render<DataListPersistHarness>(p => p.Add(c => c.FirstKey, "a").Add(c => c.SecondKey, "b"));

        Assert.Equal(2, again.FindAll("table").Count);
    }

    // ---- renders

    [Fact]
    public async Task Rows_do_not_rebuild_when_only_a_flag_of_the_list_changes()
    {
        var pending = new TaskCompletionSource<DataListResult<Person>>();
        var provider = DataListControllerTests.Schema.InMemoryProvider(People);
        var calls = 0;
        var cut = Render<DataListHarness>(p => p.Add(c => c.Provider, (request, token) =>
            ++calls == 1 ? provider(request, token) : new ValueTask<DataListResult<Person>>(pending.Task)));
        await cut.WaitForAssertionAsync(() => Assert.Empty(cut.FindAll("[data-slot=skeleton-row]")));
        var rows = cut.FindComponents<DataListRowView<Person>>();
        var builds = rows.Select(r => r.Instance.BuildCount).ToArray();
        var tableRenders = cut.FindComponent<BxDataListTable<Person>>().RenderCount;

        cut.Find("[data-testid=refresh]").Click();
        await cut.WaitForAssertionAsync(() => Assert.Equal("true", cut.Find("tbody").GetAttribute("aria-busy")));

        // The table rendered for the busy state; its rows did not.
        Assert.True(cut.FindComponent<BxDataListTable<Person>>().RenderCount > tableRenders);
        Assert.Equal(builds, cut.FindComponents<DataListRowView<Person>>().Select(r => r.Instance.BuildCount).ToArray());

        pending.SetResult(new DataListResult<Person>([.. People.Take(10)], 45));
        await cut.WaitForAssertionAsync(() => Assert.Null(cut.Find("tbody").GetAttribute("aria-busy")));
        // A new result is new rows: they are built again.
        Assert.All(cut.FindComponents<DataListRowView<Person>>(), r => Assert.True(r.Instance.BuildCount >= 2));
    }

    [Fact]
    public async Task Rows_follow_their_row_key_when_the_page_changes_and_a_row_that_stays_is_the_same_element()
    {
        var cut = Render<DataListHarness>(p => p.Add(c => c.Items, People).Add(c => c.UrlSync, false));
        var before = cut.FindComponents<DataListRowView<Person>>().Select(r => r.Instance.Item.Id).ToArray();
        Assert.Equal(Enumerable.Range(1, 10), before);

        cut.Find("[data-slot=page-size] select").Change("25");

        await cut.WaitForAssertionAsync(() => Assert.Equal(25, cut.FindComponents<DataListRowView<Person>>().Count));
        // Rows 1 to 10 are still rows 1 to 10, in order, with a component each.
        Assert.Equal(Enumerable.Range(1, 25), cut.FindComponents<DataListRowView<Person>>().Select(r => r.Instance.Item.Id));
    }

    [Fact]
    public async Task Removing_a_row_keeps_the_elements_of_the_rows_after_it()
    {
        var cut = Render<DataListHarness>(p => p.Add(c => c.Items, People.ToList()).Add(c => c.UrlSync, false));
        await cut.WaitForAssertionAsync(() => Assert.Equal(10, cut.FindAll("tbody tr[data-slot=row]").Count));
        var components = cut.FindComponents<DataListRowView<Person>>().ToDictionary(r => r.Instance.Item.Id, r => r.Instance);

        // Row 2 goes away: the rows after it are the same components (a component keeps its element), and the one beyond the page takes the end.
        cut.Render(p => p.Add(c => c.Items, People.Where(x => x.Id != 2).ToList()).Add(c => c.UrlSync, false));
        await cut.WaitForAssertionAsync(() => Assert.DoesNotContain(cut.FindComponents<DataListRowView<Person>>(), r => r.Instance.Item.Id == 2));

        var after = cut.FindComponents<DataListRowView<Person>>();
        Assert.Equal(10, after.Count);
        foreach (var row in after.Where(r => components.ContainsKey(r.Instance.Item.Id)))
        {
            Assert.Same(components[row.Instance.Item.Id], row.Instance);
        }
    }

    [Fact]
    public async Task One_page_change_renders_each_part_a_small_fixed_number_of_times()
    {
        var cut = Render<DataListHarness>(p => p.Add(c => c.Items, People).Add(c => c.UrlSync, false));
        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);
        var count = cut.FindComponent<BxDataListResultCount<Person>>();
        var table = cut.FindComponent<BxDataListTable<Person>>();
        var countBefore = count.RenderCount;
        var tableBefore = table.RenderCount;

        cut.Find("button[aria-label='Page 2']").Click();
        await cut.WaitForAssertionAsync(() => Assert.Equal("11–20 of 45", cut.Find("[data-testid=count]").TextContent));
        await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);

        // The state changed, the controller announced the load and its end: notifications that arrive together make one render.
        Assert.InRange(count.RenderCount - countBefore, 1, 2);
        Assert.InRange(table.RenderCount - tableBefore, 1, 2);
    }
}
