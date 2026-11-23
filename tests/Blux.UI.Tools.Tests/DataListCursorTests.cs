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

    [Fact]
    public async Task Refresh_in_cursor_mode_restarts_from_the_first_page()
    {
        using var controller = new DataListController<Person>(
            DataListControllerTests.Schema,
            DataListControllerTests.Schema.InMemoryProvider(DataListControllerTests.People))
        {
            PagingMode = DataListPagingMode.Cursor,
        };

        await controller.LoadAsync(DataListControllerTests.Schema.DefaultState);
        await controller.LoadMoreAsync();
        Assert.Equal(20, controller.Rows.Count);

        await controller.RefreshAsync();
        Assert.Equal(10, controller.Rows.Count);
        Assert.True(controller.HasMore);
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
