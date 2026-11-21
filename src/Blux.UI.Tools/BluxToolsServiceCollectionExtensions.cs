using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Blux.UI.Tools;

public static class BluxToolsServiceCollectionExtensions
{
    /// <summary>
    /// Registers Blux.UI.Tools, and Blux.UI when it is not registered yet (<c>AddBluxUi</c> may run any number of times: every call configures
    /// the same options). Call it in the server <c>Program.cs</c> and in the client one with the same configuration, ideally through a shared
    /// method of the client project, like <c>AddBluxUi</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the themes and messages of Tools. Its themes are read-only from the first render, like those of Blux.UI.</param>
    public static IServiceCollection AddBluxUiTools(this IServiceCollection services, Action<BluxToolsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        // The options object of Tools lives inside the one of Blux.UI, so its themes freeze together and AllThemes lists them.
        services.AddBluxUi(options =>
        {
            var tools = options.Extension<BluxToolsOptions>();
            configure?.Invoke(tools);
        });
        // One per circuit or browser tab: it follows the NavigationManager of its scope.
        services.TryAddScoped<QueryState>();
        // How a Queryable source runs its queries: blocking by default, replaced by an adapter package (AddBluxUiToolsEntityFrameworkAdapter).
        services.TryAddSingleton<IDataListQueryExecutor>(SynchronousDataListQueryExecutor.Instance);
        return services;
    }
}
