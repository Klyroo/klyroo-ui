using Blux.UI.Tools.Query;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

/// <summary>Cursor paging, <see cref="BxDataListInfiniteLoader{TItem}"/>, <see cref="DataListPagingMode.None"/> and table virtualization.</summary>
public class DataListCursorTests : BluxToolsTestContext
{
    private static DataListSchema<Person> SchemaWithFacets { get; } = DataListSchema.For<Person>(s => s
        .RowKey(p => p.Id)
        .Search(p => p.Name)
        .Field(p => p.Name, f => f.Sortable())
        .Field(p => p.Age, f => f.Sortable())
        .Field(p => p.City, f => f.Filter(DataListFilter.Option().WithFacets()))
        .DefaultSort(p => p.Name)
        .DefaultPageSize(10));

    [Fact]
    public async Task Cursor_controller_appends_pages_until_the_last_one()
    {
        using var controller = new DataListController<Person>(
            DataListControllerTests.Schema,
            DataListControllerTests.Schema.InMemoryProvider(DataListControllerTests.People))
        {
            PagingMode = DataListPagingMode.Cursor,
        };

        await controller.LoadAsync(DataListControllerTests.Schema.DefaultState);

        Assert.Equal(10, controller.Rows.Count);
        Assert.True(controller.HasMore);
        Assert.NotNull(controller.NextCursor);
        Assert.Equal(45, controller.Result!.TotalCount);

        await controller.LoadMoreAsync();
        Assert.Equal(20, controller.Rows.Count);
        Assert.True(controller.HasMore);
        Assert.Equal("Person 01", controller.Rows[0].Name);
        Assert.Equal("Person 20", controller.Rows[19].Name);

        // Drain the rest (25 remaining: 10 + 10 + 5).
        while (controller.HasMore) await controller.LoadMoreAsync();

        Assert.Equal(45, controller.Rows.Count);
        Assert.False(controller.HasMore);
        Assert.Null(controller.NextCursor);
    }

    [Fact]
    public async Task Cursor_load_more_does_not_re_request_facets_and_keeps_the_initial_ones()
    {
        var requests = new List<DataListRequest>();
        DataListProvider<Person> provider = (req, ct) =>
        {
            requests.Add(req);
            return ValueTask.FromResult(SchemaWithFacets.ExecuteInMemory(DataListControllerTests.People, req));
        };

        using var controller = new DataListController<Person>(SchemaWithFacets, provider) { PagingMode = DataListPagingMode.Cursor };

        await controller.LoadAsync(SchemaWithFacets.DefaultState);
        Assert.Single(requests);
        Assert.NotEmpty(requests[0].Facets);
        var initialFacets = controller.Result!.Facets;
        Assert.NotNull(initialFacets);

        await controller.LoadMoreAsync();
        Assert.Equal(2, requests.Count);
        Assert.Empty(requests[1].Facets);
        Assert.Equal(initialFacets, controller.Result!.Facets);
        Assert.Equal(20, controller.Rows.Count);
    }

    [Fact]
    public async Task Cursor_load_more_error_keeps_loaded_rows_and_cursor_and_retry_succeeds()
    {
        var failNext = true;
        DataListProvider<Person> provider = (req, ct) =>
        {
            if (req.Paging is DataListPaging.Cursor { After: not null } && failNext)
            {
                failNext = false;
                throw new InvalidOperationException("Temporary blip");
            }
            return ValueTask.FromResult(DataListControllerTests.Schema.ExecuteInMemory(DataListControllerTests.People, req));
        };

        using var controller = new DataListController<Person>(DataListControllerTests.Schema, provider) { PagingMode = DataListPagingMode.Cursor };

        await controller.LoadAsync(DataListControllerTests.Schema.DefaultState);
        var cursorAfterFirst = controller.NextCursor;

        await controller.LoadMoreAsync();

        Assert.True(controller.HasLoadMoreError);
        Assert.False(controller.HasError);
        Assert.Equal(10, controller.Rows.Count);
        Assert.Equal(cursorAfterFirst, controller.NextCursor);

        // Retry succeeds and appends page 2.
        await controller.LoadMoreAsync();

        Assert.False(controller.HasLoadMoreError);
        Assert.Equal(20, controller.Rows.Count);
        Assert.NotEqual(cursorAfterFirst, controller.NextCursor);
    }

    [Fact]
    public async Task A_new_load_cancels_an_in_flight_load_more_and_ignores_its_result()
    {
        var secondGate = new TaskCompletionSource<DataListResult<Person>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        DataListProvider<Person> provider = (req, ct) =>
        {
            calls++;
            if (calls == 2)
            {
                ct.Register(() => secondGate.TrySetCanceled(ct));
                return new ValueTask<DataListResult<Person>>(secondGate.Task);
            }
            return ValueTask.FromResult(DataListControllerTests.Schema.ExecuteInMemory(DataListControllerTests.People, req));
        };

        using var controller = new DataListController<Person>(DataListControllerTests.Schema, provider) { PagingMode = DataListPagingMode.Cursor };

        await controller.LoadAsync(DataListControllerTests.Schema.DefaultState);
        var loadMore = controller.LoadMoreAsync();
        Assert.True(controller.IsLoadingMore);

        // Changing sort starts a new LoadAsync, which cancels the in-flight LoadMoreAsync.
        await controller.LoadAsync(DataListControllerTests.Schema.DefaultState with { Sort = [new DataListSort("age", true)] });
        await loadMore;

        Assert.False(controller.IsLoadingMore);
        Assert.False(controller.HasLoadMoreError);
        Assert.Equal(10, controller.Rows.Count);
    }

    // A source that can lose rows between the requests, and remembers the cursor of every page it was asked for.
    private sealed class Source(IEnumerable<Person> rows)
    {
        public List<Person> Rows { get; } = [.. rows];

        public List<string?> After { get; } = [];

        public Func<DataListRequest, bool>? Fail { get; set; }

        public DataListProvider<Person> Provider => (request, token) =>
        {
            if (request.Paging is DataListPaging.Cursor cursor) After.Add(cursor.After);
            if (Fail?.Invoke(request) == true) throw new InvalidOperationException("The source is down.");
            return ValueTask.FromResult(DataListControllerTests.Schema.ExecuteInMemory(Rows, request));
        };
    }

    [Fact]
    public async Task Refresh_in_cursor_mode_loads_every_page_on_screen_again_from_the_first_cursor()
    {
        var source = new Source(DataListControllerTests.People);
        using var controller = new DataListController<Person>(DataListControllerTests.Schema, source.Provider) { PagingMode = DataListPagingMode.Cursor };
        await controller.LoadAsync(DataListControllerTests.Schema.DefaultState);
        await controller.LoadMoreAsync();
        await controller.LoadMoreAsync();
        Assert.Equal(30, controller.Rows.Count);
        Assert.Equal(3, controller.PagesLoaded);
        var cursors = source.After.ToList();
        source.After.Clear();
        // The first page lost a row: the pages on screen shift by one.
        source.Rows.RemoveAll(p => p.Id == 1);

        await controller.RefreshAsync();

        // The three pages again, one after the other, each by the cursor the previous answer gave: the rows on screen are what the source holds now.
        Assert.Equal(cursors.Count, source.After.Count);
        Assert.Null(source.After[0]);
        Assert.Equal(30, controller.Rows.Count);
        Assert.Equal("Person 02", controller.Rows[0].Name);
        Assert.Equal("Person 31", controller.Rows[29].Name);
        Assert.Equal(3, controller.PagesLoaded);
        Assert.True(controller.HasMore);
        Assert.Equal(44, controller.Result!.TotalCount);
        Assert.False(controller.HasError);
        // The list goes on from where it was.
        await controller.LoadMoreAsync();
        Assert.Equal(40, controller.Rows.Count);
    }

    [Fact]
    public async Task A_refresh_keeps_the_rows_on_screen_when_the_first_page_fails_and_reports_the_error()
    {
        var source = new Source(DataListControllerTests.People);
        using var controller = new DataListController<Person>(DataListControllerTests.Schema, source.Provider) { PagingMode = DataListPagingMode.Cursor };
        await controller.LoadAsync(DataListControllerTests.Schema.DefaultState);
        await controller.LoadMoreAsync();

        source.Fail = request => request.Paging is DataListPaging.Cursor { After: null };
        await controller.RefreshAsync();

        Assert.True(controller.HasError);
        Assert.Equal(20, controller.Rows.Count);
        Assert.Equal(2, controller.PagesLoaded);
    }

    [Fact]
    public async Task A_page_that_fails_during_a_refresh_sends_the_list_back_to_its_first_page_without_an_error()
    {
        var source = new Source(DataListControllerTests.People);
        using var controller = new DataListController<Person>(DataListControllerTests.Schema, source.Provider) { PagingMode = DataListPagingMode.Cursor };
        await controller.LoadAsync(DataListControllerTests.Schema.DefaultState);
        var firstCursor = controller.NextCursor;
        await controller.LoadMoreAsync();
        await controller.LoadMoreAsync();
        Assert.Equal(30, controller.Rows.Count);

        // Pages 1 and 2 answer, the third does not.
        var asked = 0;
        source.Fail = request => request.Paging is DataListPaging.Cursor && ++asked == 3;
        await controller.RefreshAsync();

        Assert.False(controller.HasError);
        Assert.Equal(10, controller.Rows.Count);
        Assert.Equal(1, controller.PagesLoaded);
        Assert.Equal(firstCursor, controller.NextCursor);
        // A fresh start: the next page loads as usual.
        source.Fail = null;
        await controller.LoadMoreAsync();
        Assert.Equal(20, controller.Rows.Count);
        Assert.Equal(2, controller.PagesLoaded);
    }

    [Fact]
    public async Task A_refresh_loads_at_most_MaxRefreshPages_and_the_list_goes_on_after_them()
    {
        var source = new Source(DataListControllerTests.People);
        using var controller = new DataListController<Person>(DataListControllerTests.Schema, source.Provider) { PagingMode = DataListPagingMode.Cursor, MaxRefreshPages = 2 };
        await controller.LoadAsync(DataListControllerTests.Schema.DefaultState);
        await controller.LoadMoreAsync();
        await controller.LoadMoreAsync();
        await controller.LoadMoreAsync();
        Assert.Equal(40, controller.Rows.Count);
        source.After.Clear();

        await controller.RefreshAsync();

        Assert.Equal(2, source.After.Count);
        Assert.Equal(20, controller.Rows.Count);
        Assert.Equal(2, controller.PagesLoaded);
        Assert.True(controller.HasMore);
        await controller.LoadMoreAsync();
        Assert.Equal("Person 30", controller.Rows[^1].Name);
    }

    [Fact]
    public async Task A_refresh_of_a_list_entered_at_a_cursor_starts_from_that_cursor()
    {
        var source = new Source(DataListControllerTests.People);
        using var first = new DataListController<Person>(DataListControllerTests.Schema, source.Provider) { PagingMode = DataListPagingMode.Cursor };
        await first.LoadAsync(DataListControllerTests.Schema.DefaultState);
        var middle = first.NextCursor;

        using var controller = new DataListController<Person>(DataListControllerTests.Schema, source.Provider) { PagingMode = DataListPagingMode.Cursor };
        await controller.LoadAsync(DataListControllerTests.Schema.DefaultState with { Cursor = middle });
        await controller.LoadMoreAsync();
        Assert.Equal("Person 11", controller.Rows[0].Name);
        source.After.Clear();

        await controller.RefreshAsync();

        Assert.Equal(middle, source.After[0]);
        Assert.Equal(20, controller.Rows.Count);
        Assert.Equal("Person 11", controller.Rows[0].Name);
    }

    [Fact]
    public async Task A_row_repeated_on_a_later_page_is_kept_where_it_first_appeared_and_not_listed_twice()
    {
        var calls = 0;
        // The second page repeats the last row of the first one (the cursor overlaps) and the third repeats a row of the first.
        DataListProvider<Person> provider = (request, token) =>
        {
            var page = ++calls;
            return ValueTask.FromResult(page switch
            {
                1 => new DataListResult<Person>([.. DataListControllerTests.People.Take(3)], 9, "p2"),
                2 => new DataListResult<Person>([DataListControllerTests.People[2], DataListControllerTests.People[3], DataListControllerTests.People[4]], null, "p3"),
                _ => new DataListResult<Person>([DataListControllerTests.People[0], DataListControllerTests.People[5]], null, null),
            });
        };
        using var controller = new DataListController<Person>(DataListControllerTests.Schema, provider) { PagingMode = DataListPagingMode.Cursor };

        await controller.LoadAsync(DataListControllerTests.Schema.DefaultState);
        await controller.LoadMoreAsync();
        await controller.LoadMoreAsync();

        Assert.Equal([1, 2, 3, 4, 5, 6], controller.Rows.Select(p => p.Id));
        Assert.False(controller.HasMore);
        Assert.Equal(9, controller.Result!.TotalCount);
    }

    [Fact]
    public async Task Repeated_rows_are_dropped_by_a_refresh_of_several_pages_too()
    {
        // Every page, whatever the cursor, ends with the row the next one starts with.
        DataListProvider<Person> provider = (request, token) =>
        {
            var after = request.Paging is DataListPaging.Cursor { After: { } a } ? int.Parse(a) : 0;
            var rows = DataListControllerTests.People.Skip(after).Take(4).ToList();
            return ValueTask.FromResult(new DataListResult<Person>(rows, 45, after + 3 < 12 ? (after + 3).ToString() : null));
        };
        using var controller = new DataListController<Person>(DataListControllerTests.Schema, provider) { PagingMode = DataListPagingMode.Cursor };
        await controller.LoadAsync(DataListControllerTests.Schema.DefaultState);
        await controller.LoadMoreAsync();
        await controller.LoadMoreAsync();
        Assert.Equal(Enumerable.Range(1, 10), controller.Rows.Select(p => p.Id));

        await controller.RefreshAsync();

        Assert.Equal(Enumerable.Range(1, 10), controller.Rows.Select(p => p.Id));
        Assert.Equal(3, controller.PagesLoaded);
    }

    [Theory]
    [InlineData(DataListPagingMode.Offset)]
    [InlineData(DataListPagingMode.None)]
    public void A_cursor_means_nothing_unless_the_list_pages_by_cursor(DataListPagingMode mode)
    {
        using var controller = new DataListController<Person>(DataListControllerTests.Schema, DataListControllerTests.Schema.InMemoryProvider(DataListControllerTests.People)) { PagingMode = mode };
        var stray = DataListControllerTests.Schema.DefaultState with { Cursor = "abc", Page = 3 };

        var normalized = controller.Normalize(stray);

        Assert.Null(normalized.Cursor);
        Assert.Equal(3, normalized.Page);
        controller.PagingMode = DataListPagingMode.Cursor;
        Assert.Equal("abc", controller.Normalize(stray).Cursor);
    }

    [Fact]
    public async Task None_paging_mode_requests_all_matching_rows_without_pagination()
    {
        DataListRequest? seen = null;
        DataListProvider<Person> provider = (req, ct) =>
        {
            seen = req;
            return ValueTask.FromResult(DataListControllerTests.Schema.ExecuteInMemory(DataListControllerTests.People, req));
        };

        using var controller = new DataListController<Person>(DataListControllerTests.Schema, provider) { PagingMode = DataListPagingMode.None };

        await controller.LoadAsync(DataListControllerTests.Schema.DefaultState);

        Assert.Equal(DataListPaging.None.Instance, seen!.Paging);
        Assert.Equal(45, controller.Rows.Count);
        Assert.False(controller.HasMore);
    }

    [Fact]
    public void Default_layout_uses_infinite_loader_for_cursor_pagination_for_offset_and_neither_for_none()
    {
        var cursorCut = Render<BxDataList<Person>>(p => p
            .Add(x => x.Schema, DataListControllerTests.Schema)
            .Add(x => x.Items, DataListControllerTests.People)
            .Add(x => x.PagingMode, DataListPagingMode.Cursor)
            .Add(x => x.UrlSync, false));
        Assert.Single(cursorCut.FindAll("[data-slot=infinite-loader]"));
        Assert.Empty(cursorCut.FindAll("[data-slot=pagination]"));

        var noneCut = Render<BxDataList<Person>>(p => p
            .Add(x => x.Schema, DataListControllerTests.Schema)
            .Add(x => x.Items, DataListControllerTests.People)
            .Add(x => x.PagingMode, DataListPagingMode.None)
            .Add(x => x.UrlSync, false));
        Assert.Empty(noneCut.FindAll("[data-slot=infinite-loader]"));
        Assert.Empty(noneCut.FindAll("[data-slot=pagination]"));
        Assert.Equal(45, noneCut.FindAll("tbody tr[data-slot=row]").Count);
    }

    [Fact]
    public void Manual_infinite_loader_appends_rows_and_shows_end_message()
    {
        var cut = Render<DataListCursorHarness>(p => p
            .Add(x => x.Items, DataListControllerTests.People[..15])
            .Add(x => x.Auto, false)
            .Add(x => x.ShowEnd, true)
            .Add(x => x.IncludePagination, true));

        // Pagination hides itself in Cursor mode.
        Assert.Empty(cut.FindAll("[data-slot=pagination]"));
        Assert.Equal(10, cut.FindAll("tbody tr[data-slot=row]").Count);
        Assert.Equal("1–10 of 15", cut.Find("[data-slot=result-count]").TextContent);

        cut.Find("[data-slot=load-more-trigger]").Click();

        Assert.Equal(15, cut.FindAll("tbody tr[data-slot=row]").Count);
        Assert.Equal("1–15 of 15", cut.Find("[data-slot=result-count]").TextContent);
        Assert.Empty(cut.FindAll("[data-slot=load-more-trigger]"));
        Assert.Equal("No more items", cut.Find("[data-slot=end]").TextContent);
    }

    [Fact]
    public void Auto_infinite_loader_renders_BxInfiniteScroll_with_320px_default_margin()
    {
        var cut = Render<DataListCursorHarness>(p => p
            .Add(x => x.Auto, true)
            .Add(x => x.ShowEnd, true));

        var scroller = cut.Find("bx-infinite-scroll");
        Assert.Equal("0px 0px 320px 0px", scroller.GetAttribute("data-root-margin"));
        Assert.True(scroller.HasAttribute("data-has-more"));
    }

    [Fact]
    public void Load_more_error_renders_inline_alert_and_retry_recovers_and_supports_custom_template()
    {
        var fail = true;
        DataListProvider<Person> provider = (req, ct) =>
        {
            if (req.Paging is DataListPaging.Cursor { After: not null } && fail)
            {
                fail = false;
                throw new InvalidOperationException("Boom");
            }
            return ValueTask.FromResult(DataListControllerTests.Schema.ExecuteInMemory(DataListControllerTests.People[..15], req));
        };

        var cut = Render<DataListCursorHarness>(p => p
            .Add(x => x.Provider, provider)
            .Add(x => x.Auto, false));

        cut.Find("[data-slot=load-more-trigger]").Click();

        var alert = cut.Find("[data-slot=load-more-error]");
        Assert.Equal("alert", alert.GetAttribute("role"));
        Assert.Contains("Could not load more results.", alert.TextContent);
        Assert.Equal(10, cut.FindAll("tbody tr[data-slot=row]").Count);

        cut.Find("[data-slot=load-more-retry]").Click();

        Assert.Empty(cut.FindAll("[data-slot=load-more-error]"));
        Assert.Equal(15, cut.FindAll("tbody tr[data-slot=row]").Count);

        // Custom ErrorTemplate
        fail = true;
        var customCut = Render<DataListCursorHarness>(p => p
            .Add(x => x.Provider, provider)
            .Add(x => x.Auto, false)
            .Add(x => x.ErrorTemplate, ctx => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "data-custom-err", "true");
                builder.AddContent(2, ctx.LoadMoreError?.Message);
                builder.CloseElement();
            }));

        customCut.Find("[data-slot=load-more-trigger]").Click();
        Assert.Equal("Boom", customCut.Find("[data-custom-err]").TextContent);
    }

    [Fact]
    public void Selection_survives_load_more_in_cursor_mode()
    {
        var cut = Render<DataListCursorHarness>(p => p
            .Add(x => x.Items, DataListControllerTests.People[..25])
            .Add(x => x.Auto, false)
            .Add(x => x.Selectable, true));

        // Select row 1 on page 1.
        cut.FindAll("input[data-select=row]")[0].Change(true);
        Assert.True(cut.FindAll("input[data-select=row]")[0].HasAttribute("checked"));

        // Append page 2: row 1 stays selected.
        cut.Find("[data-slot=load-more-trigger]").Click();
        Assert.Equal(20, cut.FindAll("tbody tr[data-slot=row]").Count);
        Assert.True(cut.FindAll("input[data-select=row]")[0].HasAttribute("checked"));
    }

    [Theory]
    [InlineData(DataListDensity.Compact, "h-9")]
    [InlineData(DataListDensity.Default, "h-11")]
    [InlineData(DataListDensity.Comfortable, "h-15")]
    public void Virtualized_table_in_interactive_mode_marks_virtualized_and_applies_density_row_heights_and_aria_rowcount(DataListDensity density, string heightClass)
    {
        var cut = Render<DataListCursorHarness>(p => p
            .Add(x => x.PagingMode, DataListPagingMode.None)
            .Add(x => x.Virtualize, true)
            .Add(x => x.Density, density)
            .Add(x => x.IncludeLoader, false));

        var root = cut.Find("[data-slot=table-root]");
        Assert.Equal("true", root.GetAttribute("data-virtualized"));
        Assert.Contains("overflow-y-auto", root.ClassName);
        Assert.Contains("sticky", cut.Find("thead").ClassName);
        Assert.Equal("46", cut.Find("table").GetAttribute("aria-rowcount"));
        Assert.Equal("1", cut.Find("thead tr").GetAttribute("aria-rowindex"));

        var firstRow = cut.Find("tbody tr[data-slot=row]");
        Assert.Equal("2", firstRow.GetAttribute("aria-rowindex"));
        Assert.Contains(heightClass, firstRow.ClassName);
    }

    [Fact]
    public void Virtualized_cursor_table_hosts_auto_infinite_loader_inside_its_scroll_box()
    {
        var cut = Render<DataListCursorHarness>(p => p
            .Add(x => x.PagingMode, DataListPagingMode.Cursor)
            .Add(x => x.Virtualize, true)
            .Add(x => x.Auto, true));

        var root = cut.Find("[data-slot=table-root]");
        var scrollId = root.GetAttribute("data-bx-scroll-root");
        Assert.False(string.IsNullOrEmpty(scrollId));

        // The infinite loader is rendered inside the table's scroll root, not outside it, and watches that scroll root.
        Assert.Single(cut.FindAll("[data-slot=infinite-loader]"));
        var loaderInTable = root.QuerySelector("[data-slot=infinite-loader]");
        Assert.NotNull(loaderInTable);
        var scroller = root.QuerySelector("bx-infinite-scroll");
        Assert.NotNull(scroller);
        Assert.Equal($"[data-bx-scroll-root='{scrollId}']", scroller.GetAttribute("data-root"));
    }

    [Fact]
    public async Task Interactive_cursor_list_entered_at_a_cursor_shows_back_to_start_button_and_resets_to_first_page()
    {
        using var initial = new DataListController<Person>(
            DataListControllerTests.Schema,
            DataListControllerTests.Schema.InMemoryProvider(DataListControllerTests.People[..25]))
        {
            PagingMode = DataListPagingMode.Cursor,
        };
        await initial.LoadAsync(DataListControllerTests.Schema.DefaultState);
        var secondCursor = initial.NextCursor;
        Assert.NotNull(secondCursor);

        var cut = Render<DataListCursorHarness>(p => p
            .Add(x => x.Items, DataListControllerTests.People[..25])
            .Add(x => x.Auto, false)
            .Add(x => x.State, DataListControllerTests.Schema.DefaultState with { Cursor = secondCursor }));

        Assert.Contains("Person 11", cut.Find("tbody").TextContent);
        Assert.Equal("10 of 25", cut.Find("[data-slot=result-count]").TextContent);

        var reset = cut.Find("button[data-slot=cursor-reset]");
        Assert.Equal("Back to start", reset.TextContent.Trim());

        reset.Click();

        Assert.Empty(cut.FindAll("[data-slot=cursor-reset]"));
        Assert.Contains("Person 01", cut.Find("tbody").TextContent);
        Assert.Equal("1–10 of 25", cut.Find("[data-slot=result-count]").TextContent);
    }

    [Fact]
    public async Task Changing_PagingMode_dynamically_resets_page_and_cursor_and_reloads_from_first_rows()
    {
        using var initial = new DataListController<Person>(
            DataListControllerTests.Schema,
            DataListControllerTests.Schema.InMemoryProvider(DataListControllerTests.People[..25]))
        {
            PagingMode = DataListPagingMode.Cursor,
        };
        await initial.LoadAsync(DataListControllerTests.Schema.DefaultState);
        var secondCursor = initial.NextCursor!;

        var cut = Render<DataListCursorHarness>(p => p
            .Add(x => x.Items, DataListControllerTests.People[..25])
            .Add(x => x.PagingMode, DataListPagingMode.Cursor)
            .Add(x => x.Auto, false)
            .Add(x => x.State, DataListControllerTests.Schema.DefaultState with { Cursor = secondCursor }));

        Assert.Contains("Person 11", cut.Find("tbody").TextContent);

        cut.Render(p => p
            .Add(x => x.Items, DataListControllerTests.People[..25])
            .Add(x => x.PagingMode, DataListPagingMode.None)
            .Add(x => x.Auto, false)
            .Add(x => x.State, DataListControllerTests.Schema.DefaultState with { Cursor = secondCursor }));

        Assert.Equal(25, cut.FindAll("tbody tr[data-slot=row]").Count);
        Assert.Contains("Person 01", cut.FindAll("tbody tr[data-slot=row]")[0].TextContent);
    }
}

/// <summary>Static SSR behavior of cursor paging, Back to start link, result count and virtualization opt-out.</summary>
public class DataListCursorStaticTests : BluxToolsTestContext
{
    public DataListCursorStaticTests() : base(interactive: false) { }

    [Fact]
    public async Task Statically_rendered_cursor_list_links_to_next_cursor_and_shows_back_to_start_on_later_pages()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/list");

        var firstPage = Render<DataListCursorHarness>(p => p
            .Add(x => x.Items, DataListControllerTests.People[..25])
            .Add(x => x.UrlSync, true)
            .Add(x => x.Auto, true));

        var link = firstPage.Find("[data-slot=infinite-loader] a");
        var href = link.GetAttribute("href");
        Assert.NotNull(href);
        Assert.Contains("c=", href);
        Assert.Empty(firstPage.FindAll("[data-slot=cursor-reset]"));

        // Follow the cursor link: dispose the first render (new HTTP request in SSR) and render page 2.
        await DisposeComponentsAsync();
        nav.NavigateTo(new Uri(new Uri("http://localhost/"), href).ToString());
        var secondPage = Render<DataListCursorHarness>(p => p
            .Add(x => x.Items, DataListControllerTests.People[..25])
            .Add(x => x.UrlSync, true)
            .Add(x => x.Auto, true));

        Assert.Equal(10, secondPage.FindAll("tbody tr[data-slot=row]").Count);
        Assert.Contains("Person 11", secondPage.Find("tbody").TextContent);
        Assert.Equal("10 of 25", secondPage.Find("[data-slot=result-count]").TextContent);

        var reset = secondPage.Find("[data-slot=cursor-reset]");
        Assert.DoesNotContain("c=", reset.GetAttribute("href") ?? "");

        // Sort links clear the cursor parameter.
        var sortLink = secondPage.Find("thead a");
        Assert.DoesNotContain("c=", sortLink.GetAttribute("href") ?? "");
    }

    [Fact]
    public void Statically_rendered_table_never_virtualizes()
    {
        var cut = Render<DataListCursorHarness>(p => p
            .Add(x => x.PagingMode, DataListPagingMode.None)
            .Add(x => x.Virtualize, true)
            .Add(x => x.IncludeLoader, false));

        var root = cut.Find("[data-slot=table-root]");
        Assert.False(root.HasAttribute("data-virtualized"));
        Assert.False(cut.Find("table").HasAttribute("aria-rowcount"));
        Assert.Equal(45, cut.FindAll("tbody tr[data-slot=row]").Count);
    }
}
