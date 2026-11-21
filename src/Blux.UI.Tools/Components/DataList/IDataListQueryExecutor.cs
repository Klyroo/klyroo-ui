using Blux.UI.Tools.Query;

namespace Blux.UI.Tools;

/// <summary>
/// Runs the queries a data list builds for a <c>Queryable</c> source (<see cref="QueryableDataList.Apply{T}"/>): the count of the matching rows and the page. The list asks the
/// service container for one, so an adapter package replaces how the queries run (EF Core's <c>CountAsync</c> and <c>ToListAsync</c>, with the cancellation token of the
/// request) without the list referencing the provider. <see cref="SynchronousDataListQueryExecutor"/> is the default.
/// </summary>
public interface IDataListQueryExecutor
{
    /// <summary>Counts the rows of a query.</summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">Cancelled when a newer request replaces this one.</param>
    ValueTask<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken);

    /// <summary>
    /// Loads the rows of a query. A cursor needs the sort values the database computed next to each row: the list passes <see cref="DataListQuery{T}.Rows"/> (an
    /// <c>IQueryable&lt;DataListRow&lt;T&gt;&gt;</c>) here, so the projection is part of the query and needs nothing from the executor.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">Cancelled when a newer request replaces this one.</param>
    ValueTask<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken);
}

/// <summary>
/// The default executor: <c>Count()</c> and <c>ToList()</c>, on the thread that asks. <b>It blocks that thread while the database answers</b>: in Blazor Server
/// that is the thread of the circuit, in WebAssembly the only thread of the page. Use it for tests, small local databases and providers with no asynchronous API;
/// register an asynchronous executor for a real database (<c>AddBluxUiToolsEntityFrameworkAdapter()</c> of Blux.UI.Tools.EntityFrameworkCore for EF Core).
/// The cancellation token is checked before each query, not during it.
/// </summary>
public sealed class SynchronousDataListQueryExecutor : IDataListQueryExecutor
{
    /// <summary>The shared instance.</summary>
    public static SynchronousDataListQueryExecutor Instance { get; } = new();

    /// <inheritdoc />
    public ValueTask<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(query.Count());
    }

    /// <inheritdoc />
    public ValueTask<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(query.ToList());
    }
}

/// <summary>The provider of a <c>Queryable</c> source: the request becomes queries (<see cref="QueryableDataList"/>), an <see cref="IDataListQueryExecutor"/> runs them, and the result is assembled.</summary>
internal static class DataListQueryableProvider
{
    public static DataListProvider<T> Create<T>(DataListSchema<T> schema, IQueryable<T> source, IDataListQueryExecutor executor) =>
        async (request, cancellationToken) =>
        {
            // The queries run one after the other: they usually share one DbContext, which does not allow two at once.
            var query = schema.Apply(source, request);
            var rows = await executor.ToListAsync(query.Rows, cancellationToken).ConfigureAwait(false);
            int? total = query.WantsTotal ? await executor.CountAsync(query.Matching, cancellationToken).ConfigureAwait(false) : null;
            List<DataListFacetResult>? facets = null;
            foreach (var facet in schema.FacetQueries(source, request))
            {
                var buckets = await executor.ToListAsync(facet.Buckets, cancellationToken).ConfigureAwait(false);
                int? missing = facet.Missing is null ? null : await executor.CountAsync(facet.Missing, cancellationToken).ConfigureAwait(false);
                (facets ??= []).Add(facet.ToResult(buckets, missing));
            }
            return schema.ToResult(request, rows, total, facets);
        };
}
