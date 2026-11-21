using System.Data.Common;
using Blux.UI.Tools.Query;
using Microsoft.AspNetCore.Components;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.EntityFrameworkCore.Tests;

public sealed class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public double Price { get; set; }

    public bool Active { get; set; }

    public DateOnly Created { get; set; }
}

/// <summary>Counts the commands EF runs, told apart by the way it runs them.</summary>
public sealed class CommandCounter : DbCommandInterceptor
{
    public int Sync;
    public int Async;

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        Interlocked.Increment(ref Sync);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref Async);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        Interlocked.Increment(ref Sync);
        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref Async);
        return ValueTask.FromResult(result);
    }
}

public sealed class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
}

/// <summary>A database in memory (SQLite) with 45 products, kept open for the life of the test.</summary>
public sealed class Shop : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public Shop()
    {
        _connection.Open();
        Counter = new CommandCounter();
        Db = new ShopContext(new DbContextOptionsBuilder<ShopContext>().UseSqlite(_connection).AddInterceptors(Counter).Options);
        Db.Database.EnsureCreated();
        Db.Products.AddRange(Enumerable.Range(1, 45).Select(i => new Product
        {
            Id = i,
            Name = $"Product {i:00}",
            Price = 10 + i % 7 * 2.5,
            Active = i % 3 != 0,
            Created = new DateOnly(2026, 1, 1).AddDays(i),
        }));
        Db.SaveChanges();
        Counter.Sync = 0;
        Counter.Async = 0;
    }

    public ShopContext Db { get; }

    public CommandCounter Counter { get; }

    public IQueryable<Product> Products => Db.Products.AsNoTracking();

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }

    public static DataListSchema<Product> Schema { get; } = DataListSchema.For<Product>(s => s
        .RowKey(p => p.Id)
        .Field(p => p.Name, f => f.Sortable())
        .Field(p => p.Price, f => f.Sortable())
        .Field(p => p.Active, f => f.Sortable())
        .Field(p => p.Created, f => f.Sortable())
        .DefaultSort(p => p.Id)
        .DefaultPageSize(10));
}

public class EntityFrameworkAdapterTests
{
    private static DataListState State(int page = 1, int size = 10, params DataListSort[] sort) =>
        Shop.Schema.DefaultState with { Page = page, PageSize = size, Sort = sort.Length == 0 ? Shop.Schema.DefaultState.Sort : sort };

    [Fact]
    public async Task The_adapter_runs_the_queries_asynchronously_and_the_default_executor_blocks()
    {
        using var shop = new Shop();
        using var async = new DataListController<Product>(Shop.Schema, DataListController<Product>.CreateProvider(Shop.Schema, null, null, shop.Products, new EntityFrameworkDataListQueryExecutor()));

        await async.LoadAsync(State());

        Assert.False(async.HasError, async.Error?.ToString());
        Assert.Equal(0, shop.Counter.Sync);
        // The page and the count.
        Assert.Equal(2, shop.Counter.Async);

        using var blocking = new DataListController<Product>(Shop.Schema, DataListController<Product>.CreateProvider(Shop.Schema, null, null, shop.Products, SynchronousDataListQueryExecutor.Instance));
        await blocking.LoadAsync(State());

        Assert.False(blocking.HasError, blocking.Error?.ToString());
        Assert.Equal(2, shop.Counter.Sync);
        Assert.Equal(2, shop.Counter.Async);
    }

    [Theory]
    [InlineData(1, 10, "id", false)]
    [InlineData(3, 10, "id", false)]
    [InlineData(2, 25, "price", true)]
    [InlineData(1, 7, "name", true)]
    [InlineData(5, 10, "created", false)]
    [InlineData(99, 10, "id", false)]
    public async Task A_queryable_source_answers_like_an_items_source(int page, int size, string sortKey, bool descending)
    {
        using var shop = new Shop();
        var rows = shop.Products.OrderBy(p => p.Id).ToList();
        using var queryable = new DataListController<Product>(Shop.Schema, DataListController<Product>.CreateProvider(Shop.Schema, null, null, shop.Products, new EntityFrameworkDataListQueryExecutor()));
        using var memory = new DataListController<Product>(Shop.Schema, DataListController<Product>.CreateProvider(Shop.Schema, rows, null));
        var state = State(page, size, new DataListSort(sortKey, descending));

        await queryable.LoadAsync(state);
        await memory.LoadAsync(state);

        Assert.False(queryable.HasError, queryable.Error?.ToString());
        Assert.Equal(memory.Rows.Select(p => p.Id), queryable.Rows.Select(p => p.Id));
        Assert.Equal(memory.Result!.TotalCount, queryable.Result!.TotalCount);
        Assert.Equal(memory.LoadedState!.Page, queryable.LoadedState!.Page);
    }

    [Fact]
    public async Task A_cancelled_request_cancels_the_query()
    {
        using var shop = new Shop();
        var provider = DataListController<Product>.CreateProvider(Shop.Schema, null, null, shop.Products, new EntityFrameworkDataListQueryExecutor());
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await provider(Shop.Schema.DefaultState.ToRequest(), cancelled.Token));
    }

    [Fact]
    public async Task The_executor_rejects_a_query_that_is_not_an_ef_query()
    {
        var executor = new EntityFrameworkDataListQueryExecutor();

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await executor.CountAsync(new[] { 1, 2 }.AsQueryable(), CancellationToken.None));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void The_adapter_replaces_the_default_executor_whatever_the_order_of_the_calls(bool adapterFirst)
    {
        var services = new ServiceCollection();
        if (adapterFirst)
        {
            services.AddBluxUiToolsEntityFrameworkAdapter();
            services.AddBluxUiTools();
        }
        else
        {
            services.AddBluxUiTools();
            services.AddBluxUiToolsEntityFrameworkAdapter();
        }

        using var provider = services.BuildServiceProvider();

        Assert.IsType<EntityFrameworkDataListQueryExecutor>(provider.GetRequiredService<IDataListQueryExecutor>());
    }
}

/// <summary>A list in a renderer, with the adapter registered.</summary>
public class EntityFrameworkListTests : BunitContext
{
    public EntityFrameworkListTests()
    {
        Services.AddBluxUiToolsEntityFrameworkAdapter();
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("Server", true));
    }

    [Fact]
    public async Task A_list_with_a_queryable_loads_through_the_async_executor_and_sorts()
    {
        using var shop = new Shop();

        var cut = Render<BxDataList<Product>>(p => p
            .Add(c => c.Queryable, shop.Products)
            .Add(c => c.Schema, Shop.Schema)
            .Add(c => c.UrlSync, false));

        await cut.WaitForAssertionAsync(() => Assert.Equal(10, cut.FindAll("tbody tr[data-slot=row]").Count));
        Assert.Equal("1–10 of 45", cut.Find("[data-slot=result-count]").TextContent);
        Assert.Contains("Product 01", cut.Find("tbody tr").TextContent);
        Assert.Equal(0, shop.Counter.Sync);
        var before = shop.Counter.Async;

        cut.Find("th[data-key=price] button").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal("ascending", cut.Find("th[data-key=price]").GetAttribute("aria-sort")));
        await cut.WaitForAssertionAsync(() => Assert.True(shop.Counter.Async > before));
        Assert.Equal(0, shop.Counter.Sync);
    }
}
