using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Changelog.Web.ReleaseNotes;

/// <summary>Registers the release source, the cache and the feed endpoint.</summary>
public static partial class ChangelogServices
{
    /// <summary>
    /// Adds <see cref="ReleaseService"/>, the source chosen by <c>Changelog:Source</c> (GitHub through a typed <see cref="HttpClient"/>, or a file) and
    /// <see cref="ReleaseBody"/>. A setting that cannot work is logged at start, not thrown: the pages explain it to the visitor, so a typo in a
    /// setting never takes the site down.
    /// </summary>
    public static IServiceCollection AddChangelog(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(ChangelogOptions.Section).Get<ChangelogOptions>() ?? new ChangelogOptions();
        services.Configure<ChangelogOptions>(configuration.GetSection(ChangelogOptions.Section));
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);

        if (options.EffectiveSource == ChangelogSource.File)
        {
            services.AddSingleton<IReleaseSource, FileReleaseSource>();
        }
        else
        {
            // The handler and its connections are pooled by the factory. No retries here: a failed read is retried by the service after a pause, and a
            // retry loop in front of a rate limit only makes it last longer.
            services.AddHttpClient<GitHubReleaseClient>((provider, client) =>
                    GitHubReleaseClient.Configure(client, provider.GetRequiredService<IOptions<ChangelogOptions>>().Value))
                // The service lives as long as the site and keeps its client: the pool of connections is recycled by PooledConnectionLifetime, not by the factory.
                .SetHandlerLifetime(Timeout.InfiniteTimeSpan)
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 3,
                    AutomaticDecompression = System.Net.DecompressionMethods.All,
                });
            services.AddSingleton<IReleaseSource, GitHubReleaseSource>();
        }

        services.AddSingleton<ReleaseService>();
        services.AddSingleton(provider => new ReleaseBody(provider.GetRequiredService<IReleaseSource>().RepositoryUrl));
        return services;
    }

    /// <summary>Logs what is wrong with the settings, once, at start. The token is never part of the message.</summary>
    public static void ReportChangelogSettings(this IHost host)
    {
        var options = host.Services.GetRequiredService<IOptions<ChangelogOptions>>().Value;
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Changelog");
        if (options.Problem() is { } problem)
        {
            logger.LogWarning("{Problem}", problem);
        }
        else
        {
            logger.LogInformation("Releases from {Source}{Repository}{Sample}; GitHub token {Token}", options.EffectiveSource,
                options.Repository is { Length: > 0 } ? $" ({options.Repository})" : "", options.IsSample ? " (sample data: set Changelog:Repository)" : "", string.IsNullOrEmpty(options.GitHubToken) ? "not set" : "set");
        }
    }

    [GeneratedRegex(@"^[A-Za-z0-9.\-]+(?::[0-9]{1,5})?\z|^\[[0-9A-Fa-f:.]+\](?::[0-9]{1,5})?\z")]
    private static partial Regex HostPattern();

    /// <summary>
    /// The address of the site with a trailing slash, for the links of the feed: <c>Changelog:SiteUrl</c> when set, else the one of the request, with its path base. The host
    /// header comes from the client, so it must look like a host name before it is written into a document that a cache may keep: set <c>Changelog:SiteUrl</c> in production.
    /// </summary>
    public static string? SiteUrl(HttpRequest request, ChangelogOptions options)
    {
        if (options.SiteUrl is { Length: > 0 } configured)
        {
            return Uri.TryCreate(configured, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" && uri.UserInfo.Length == 0
                ? uri.GetLeftPart(UriPartial.Path).TrimEnd('/') + "/"
                : null;
        }
        if (!request.Host.HasValue || !HostPattern().IsMatch(request.Host.Value) || request.Scheme is not ("http" or "https"))
        {
            return null;
        }
        return $"{request.Scheme}://{request.Host.Value}{request.PathBase.Value?.TrimEnd('/')}/";
    }

    /// <summary>Maps <c>GET /feed.xml</c> (titled <paramref name="title"/>): an Atom feed of the latest releases, cached by the browser for five minutes and answered with 304 when it did not change.</summary>
    public static IEndpointRouteBuilder MapReleaseFeed(this IEndpointRouteBuilder endpoints, string title, string description)
    {
        endpoints.MapGet("/feed.xml", async (HttpContext context, ReleaseService releases, ReleaseBody body, IOptions<ChangelogOptions> options, TimeProvider time) =>
        {
            var site = SiteUrl(context.Request, options.Value);
            if (site is null)
            {
                return Results.Text("The address of the site could not be worked out.", "text/plain", Encoding.UTF8, StatusCodes.Status400BadRequest);
            }
            var list = await releases.GetAsync(context.RequestAborted);
            if (!list.Available)
            {
                context.Response.Headers.RetryAfter = "300";
                return Results.Text("The releases are not available right now.", "text/plain", Encoding.UTF8, StatusCodes.Status503ServiceUnavailable);
            }
            var xml = ReleaseFeed.Build(list.Releases, new FeedInfo(site, title, description), body, list.FetchedAt ?? time.GetUtcNow());
            var etag = "\"" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(xml)))[..32] + "\"";
            context.Response.Headers.CacheControl = "public, max-age=300";
            context.Response.Headers.ETag = etag;
            if (context.Request.Headers.IfNoneMatch.Contains(etag))
            {
                return Results.StatusCode(StatusCodes.Status304NotModified);
            }
            return Results.Text(xml, "application/atom+xml", Encoding.UTF8);
        });
        return endpoints;
    }
}
