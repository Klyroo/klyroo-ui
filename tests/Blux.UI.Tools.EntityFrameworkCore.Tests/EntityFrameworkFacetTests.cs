using Blux.UI.Tools.Query;

namespace Blux.UI.Tools.EntityFrameworkCore.Tests;

/// <summary>Facets through the Entity Framework adapter: the group-by queries run asynchronously, match the in-memory counts, and are not repeated while the user pages or sorts.</summary>
public class EntityFrameworkFacetTests
{
    private static readonly DataListSchema<Product> FacetSchema = DataListSchema.For<Product>(s => s
        .RowKey(p => p.Id)
        .Field(p => p.Name, f => f.Sortable().Filter(DataListFilter.Text()))
        .Field(p => p.Price, f => f.Sortable().Filter(DataListFilter.Option().WithFacets()))
        .Field(p => p.Active, f => f.Sortable().Filter(DataListFilter.Boolean().WithFacets().WithFacetMode(FacetMode.IncludeSelf)))
        .DefaultSort(p => p.Id)
        .DefaultPageSize(10));

    private static DataListState State(int page = 1, params FilterRule[] filters) => FacetSchema.DefaultState with { Page = page, Filters = filters };

    private static DataListController<Product> Controller(Shop shop, IDataListQueryExecutor executor) =>
        new(FacetSchema, DataListController<Product>.CreateProvider(FacetSchema, null, null, shop.Products, executor));

    [Fact]
    public async Task The_facets_run_asynchronously_with_the_page_and_the_count()
    {
        using var shop = new Shop();
        using var controller = Controller(shop, new EntityFrameworkDataListQueryExecutor());

        await controller.LoadAsync(State());

        Assert.False(controller.HasError, controller.Error?.ToString());
        Assert.Equal(0, shop.Counter.Sync);
        // The page, the count, the buckets and the empty count of the option facet, the buckets of the boolean facet.
        Assert.Equal(5, shop.Counter.Async);
        Assert.Equal(2, controller.Result!.Facets!.Count);
    }

    [Fact]
    public async Task The_counts_equal_the_in_memory_counts_with_a_filter_on_each_mode()
    {
        using var shop = new Shop();
        var rows = shop.Products.OrderBy(p => p.Id).ToList();
        using var queryable = Controller(shop, new EntityFrameworkDataListQueryExecutor());
        using var memory = new DataListController<Product>(FacetSchema, DataListController<Product>.CreateProvider(FacetSchema, rows, null));
        var state = State(1, new FilterRule("active", FilterOperator.Is, new FilterValue.Boolean(true)), new FilterRule("price", FilterOperator.IsAnyOf, new FilterValue.List(["10", "12.5"])));

        await queryable.LoadAsync(state);
        await memory.LoadAsync(state);

        Assert.False(queryable.HasError, queryable.Error?.ToString());
        Assert.Equal(memory.Result!.Facets, queryable.Result!.Facets);
        // Active keeps its own rule (include-self), price leaves its own out: other prices are counted among the active products.
        Assert.Equal(["true"], queryable.Result.Facets!.Single(f => f.Key == "active").Options.Select(o => o.Value));
        Assert.True(queryable.Result.Facets!.Single(f => f.Key == "price").Options.Count > 2);
    }

    [Fact]
    public async Task Paging_and_sorting_do_not_count_again_and_a_filter_or_a_refresh_does()
    {
        using var shop = new Shop();
        using var controller = Controller(shop, new EntityFrameworkDataListQueryExecutor());
        await controller.LoadAsync(State());
        var afterFirst = shop.Counter.Async;

        await controller.LoadAsync(State(2));
        // Only the page and the count: the facets stay those of the first load.
        Assert.Equal(afterFirst + 2, shop.Counter.Async);
        Assert.Equal(2, controller.Result!.Facets!.Count);

        await controller.LoadAsync(State(2) with { Sort = [new DataListSort("price", true)] });
        Assert.Equal(afterFirst + 4, shop.Counter.Async);

        await controller.LoadAsync(State(1, new FilterRule("name", FilterOperator.Contains, new FilterValue.Text("Product 1"))));
        Assert.Equal(afterFirst + 4 + 5, shop.Counter.Async);

        var before = shop.Counter.Async;
        await controller.RefreshAsync();
        Assert.Equal(before + 5, shop.Counter.Async);
    }
}
