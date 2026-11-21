using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Blux.UI.Tools;

/// <summary>
/// Runs the queries of a data list with Entity Framework Core: <c>CountAsync</c> and <c>ToListAsync</c>, with the cancellation token of the request, so a newer
/// request cancels the query of the stale one in the database and no thread waits for an answer. The queries come from the <c>Queryable</c> of the list, which must be
/// an EF Core query (a <c>DbSet</c> or a query on one); anything else makes EF throw <see cref="InvalidOperationException"/>.
/// </summary>
public sealed class EntityFrameworkDataListQueryExecutor : IDataListQueryExecutor
{
    /// <inheritdoc />
    public async ValueTask<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await query.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}

public static class EntityFrameworkDataListServiceCollectionExtensions
{
    /// <summary>
    /// Makes the data lists with a <c>Queryable</c> source run their queries with Entity Framework Core (<see cref="EntityFrameworkDataListQueryExecutor"/>) instead of
    /// the blocking default. It registers Blux.UI.Tools too, and replaces the executor whether it is called before or after <c>AddBluxUiTools</c>. Call it in the server
    /// <c>Program.cs</c>; a list in WebAssembly cannot reach a database, so the client needs no EF executor.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static IServiceCollection AddBluxUiToolsEntityFrameworkAdapter(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddBluxUiTools();
        services.Replace(ServiceDescriptor.Singleton<IDataListQueryExecutor, EntityFrameworkDataListQueryExecutor>());
        return services;
    }
}
