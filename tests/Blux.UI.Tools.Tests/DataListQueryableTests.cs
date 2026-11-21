using System.Collections;
using System.Linq.Expressions;
using Blux.UI.Tools.Query;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

/// <summary>
/// A query that is not LINQ to Objects to the eyes of <see cref="QueryableDataList"/> (it rejects <c>AsQueryable()</c>) but that evaluates in memory: the
/// stand-in for a database provider in tests that need no database. EF Core and SQLite are proven in Blux.UI.Tools.EntityFrameworkCore.Tests.
/// </summary>
internal sealed class PassThroughQueryable<T> : IOrderedQueryable<T>
{
    private readonly IQueryable<T> _inner;

    public PassThroughQueryable(IEnumerable<T> rows)
    {
        _inner = rows.AsQueryable();
        Expression = Expression.Constant(this);
        Provider = new PassThroughProvider(_inner.Provider);
    }

    internal PassThroughQueryable(Expression expression, PassThroughProvider provider)
    {
        _inner = null!;
        Expression = expression;
        Provider = provider;
    }

    public Type ElementType => typeof(T);

    public Expression Expression { get; }

    public IQueryProvider Provider { get; }

    public IEnumerator<T> GetEnumerator() =>
        ((PassThroughProvider)Provider).Run<IEnumerable<T>>(Expression).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal IQueryable<T> Inner => _inner;
}

internal sealed class PassThroughProvider(IQueryProvider inner) : IQueryProvider
{
    public IQueryable CreateQuery(Expression expression)
    {
        var element = expression.Type.GetInterfaces().Append(expression.Type)
            .First(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IQueryable<>)).GetGenericArguments()[0];
        return (IQueryable)Activator.CreateInstance(typeof(PassThroughQueryable<>).MakeGenericType(element), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public, null, [expression, this], null)!;
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new PassThroughQueryable<TElement>(expression, this);

    public object? Execute(Expression expression) => inner.Execute(new Rewriter().Visit(expression));

    public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(new Rewriter().Visit(expression));

    internal TResult Run<TResult>(Expression expression)
    {
        var rewritten = new Rewriter().Visit(expression);
        return (TResult)inner.CreateQuery(rewritten);
    }

    // The root of the query is this wrapper; the in-memory provider wants the sequence under it.
    private sealed class Rewriter : ExpressionVisitor
    {
        protected override Expression VisitConstant(ConstantExpression node)
        {
            var type = node.Value?.GetType();
            if (type is { IsGenericType: true } && type.GetGenericTypeDefinition() == typeof(PassThroughQueryable<>))
            {
                var inner = type.GetProperty("Inner", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(node.Value);
                return Expression.Constant(inner);
            }
            return base.VisitConstant(node);
        }
    }
}

/// <summary>The Queryable source: the request becomes queries, an executor runs them, and the result is the one the in-memory source gives.</summary>
public class DataListQueryableTests() : BluxToolsTestContext(configureServices: services => services.AddSingleton<IDataListQueryExecutor>(Counting))
{
    private static readonly CountingExecutor Counting = new();

    private static readonly Person[] People = DataListControllerTests.People;

    private static DataListSchema<Person> Schema => DataListControllerTests.Schema;

    private sealed class CountingExecutor : IDataListQueryExecutor
    {
        public int Counts { get; private set; }

        public int Lists { get; private set; }

        public ValueTask<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        {
            Counts++;
            return ValueTask.FromResult(query.Count());
        }

        public ValueTask<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        {
            Lists++;
            return ValueTask.FromResult(query.ToList());
        }
    }

    [Fact]
    public async Task The_synchronous_executor_gives_the_result_of_the_in_memory_source()
    {
        foreach (var state in new[]
        {
            Schema.DefaultState,
            Schema.DefaultState with { Page = 3 },
            Schema.DefaultState with { Page = 2, PageSize = 25, Sort = [new DataListSort("age", true)] },
        })
        {
            using var queryable = new DataListController<Person>(Schema, DataListController<Person>.CreateProvider(Schema, null, null, new PassThroughQueryable<Person>(People)));
            using var memory = new DataListController<Person>(Schema, DataListController<Person>.CreateProvider(Schema, People, null));

            await queryable.LoadAsync(state);
            await memory.LoadAsync(state);

            Assert.False(queryable.HasError, queryable.Error?.ToString());
            Assert.Equal(memory.Rows.Select(p => p.Id), queryable.Rows.Select(p => p.Id));
            Assert.Equal(memory.Result!.TotalCount, queryable.Result!.TotalCount);
        }
    }

    [Fact]
    public async Task The_executor_counts_and_loads_the_queries()
    {
        var executor = new CountingExecutor();
        using var controller = new DataListController<Person>(Schema, DataListController<Person>.CreateProvider(Schema, null, null, new PassThroughQueryable<Person>(People), executor));

        await controller.LoadAsync(Schema.DefaultState);

        Assert.Equal(1, executor.Lists);
        Assert.Equal(1, executor.Counts);
        Assert.Equal(45, controller.Result!.TotalCount);
    }

    [Fact]
    public async Task A_page_past_the_end_is_clamped_through_the_queryable_too()
    {
        using var controller = new DataListController<Person>(Schema, DataListController<Person>.CreateProvider(Schema, null, null, new PassThroughQueryable<Person>(People)));

        await controller.LoadAsync(Schema.DefaultState with { Page = 99 });

        Assert.Equal(5, controller.LoadedState!.Page);
        Assert.Equal(5, controller.Rows.Count);
    }

    [Fact]
    public async Task An_in_memory_queryable_is_rejected_with_the_advice_to_use_Items()
    {
        using var controller = new DataListController<Person>(Schema, DataListController<Person>.CreateProvider(Schema, null, null, People.AsQueryable()));

        await controller.LoadAsync(Schema.DefaultState);

        Assert.True(controller.HasError);
        Assert.Contains("ExecuteInMemory", controller.Error!.Message);
    }

    [Fact]
    public void Several_sources_or_none_are_rejected()
    {
        var queryable = new PassThroughQueryable<Person>(People);

        Assert.Throws<InvalidOperationException>(() => DataListController<Person>.CreateProvider(Schema, People, null, queryable));
        Assert.Throws<InvalidOperationException>(() => DataListController<Person>.CreateProvider(Schema, null, Schema.InMemoryProvider(People), queryable));
        Assert.NotNull(DataListController<Person>.CreateProvider(Schema, null, null, queryable));
    }

    [Fact]
    public void The_container_has_the_synchronous_executor_by_default()
    {
        // The container of this class replaces it with a counting one; a plain one has the synchronous executor.
        using var plain = new PlainContext();

        Assert.Same(SynchronousDataListQueryExecutor.Instance, plain.Services.GetRequiredService<IDataListQueryExecutor>());
    }

    [Fact]
    public async Task The_synchronous_executor_checks_the_token_before_it_runs()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var queryable = new PassThroughQueryable<Person>(People);

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await SynchronousDataListQueryExecutor.Instance.CountAsync(queryable, cancelled.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await SynchronousDataListQueryExecutor.Instance.ToListAsync(queryable, cancelled.Token));
    }

    [Fact]
    public void A_list_with_a_queryable_runs_it_through_the_executor_of_the_container()
    {
        var executor = Counting;
        var before = (executor.Lists, executor.Counts);

        var cut = Render<BxDataList<Person>>(p => p
            .Add(c => c.Queryable, new PassThroughQueryable<Person>(People))
            .Add(c => c.Schema, Schema)
            .Add(c => c.UrlSync, false));

        Assert.Equal(10, cut.FindAll("tbody tr[data-slot=row]").Count);
        Assert.Equal("1–10 of 45", cut.Find("[data-slot=result-count]").TextContent);
        Assert.True(executor.Lists > before.Lists);
        Assert.True(executor.Counts > before.Counts);
    }

    [Fact]
    public void A_queryable_needs_a_schema_and_cannot_come_with_another_source()
    {
        var queryable = new PassThroughQueryable<Person>(People);

        Assert.Throws<InvalidOperationException>(() => Render<BxDataList<Person>>(p => p.Add(c => c.Queryable, queryable)));
        Assert.Throws<InvalidOperationException>(() => Render<BxDataList<Person>>(p => p
            .Add(c => c.Queryable, queryable).Add(c => c.Items, People).Add(c => c.Schema, Schema)));
    }

    [Fact]
    public async Task Sorting_and_paging_a_list_with_a_queryable_follow_the_url()
    {
        var cut = Render<BxDataList<Person>>(p => p
            .Add(c => c.Queryable, new PassThroughQueryable<Person>(People))
            .Add(c => c.Schema, Schema));

        cut.Find("th[data-key=age] button").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal("ascending", cut.Find("th[data-key=age]").GetAttribute("aria-sort")));
        Assert.Equal("1–10 of 45", cut.Find("[data-slot=result-count]").TextContent);
    }

    private sealed class PlainContext : BluxToolsTestContext;
}
