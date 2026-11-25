using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Changelog.Web.ReleaseNotes;

public enum FetchStatus
{
    /// <summary>The releases were read.</summary>
    Ok,

    /// <summary>The conditional request found nothing new (304): the copy that was sent the ETag for is current.</summary>
    NotModified,

    /// <summary>GitHub's rate limit was reached (403 or 429): <see cref="FetchOutcome.RetryAt"/> says when to try again.</summary>
    RateLimited,

    /// <summary>The repository does not exist, is private, or the token cannot see it (404).</summary>
    NotFound,

    /// <summary>Anything else: no network, a timeout, an error status, an answer that is not what the API sends.</summary>
    Failed,
}

/// <param name="Status">What happened.</param>
/// <param name="Releases">The releases, newest first, for <see cref="FetchStatus.Ok"/>.</param>
/// <param name="ETag">The ETag of the first page, to send with the next request (<see cref="FetchStatus.Ok"/> only).</param>
/// <param name="RetryAt">For <see cref="FetchStatus.RateLimited"/>: when the limit resets, or when GitHub asked to come back.</param>
/// <param name="Detail">A line for the log. It never holds a token or a header.</param>
public sealed record FetchOutcome(
    FetchStatus Status,
    IReadOnlyList<ReleaseEntry>? Releases = null,
    string? ETag = null,
    DateTimeOffset? RetryAt = null,
    string? Detail = null);

/// <summary>
/// A typed <see cref="HttpClient"/> for <c>GET /repos/{owner}/{repo}/releases</c> of the GitHub REST API. It sends a generic User-Agent (GitHub requires one,
/// and nothing about you or your site is in it), the token as a bearer header only when one is configured, and <c>If-None-Match</c> with the ETag of the
/// last answer: a 304 does not count against the rate limit. It follows the <c>Link: rel="next"</c> header for more pages, and only to the host it was
/// configured for, so a header cannot send the token elsewhere. It never throws for something GitHub or the network does: the result says what happened.
/// </summary>
public sealed partial class GitHubReleaseClient(HttpClient http, ILogger<GitHubReleaseClient> logger)
{
    public const string UserAgent = "klyroo-template-changelog";

    /// <summary>The most requests one fetch makes, so a long chain of next links ends.</summary>
    private const int MaxPages = 10;

    [GeneratedRegex("<([^>]*)>\\s*;\\s*rel=\"?next\"?", RegexOptions.IgnoreCase)]
    private static partial Regex NextLink();

    /// <summary>Sets what every request needs. Called when the client is registered.</summary>
    public static void Configure(HttpClient client, ChangelogOptions options)
    {
        // A setting that is not an https address never reaches here (the source is not configured then), but this must not throw whatever it holds.
        var api = Uri.TryCreate(options.ApiUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps ? uri : new Uri("https://api.github.com/");
        client.BaseAddress = new Uri(api.AbsoluteUri.EndsWith('/') ? api.AbsoluteUri : api.AbsoluteUri + "/");
        client.Timeout = options.RequestTimeout > TimeSpan.Zero ? options.RequestTimeout : TimeSpan.FromSeconds(15);
        client.MaxResponseContentBufferSize = 16 * 1024 * 1024;
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
    }

    /// <param name="repository"><c>owner/repo</c>, validated by <see cref="ChangelogOptions.IsRepository"/>.</param>
    /// <param name="token">The token, or null or empty to send none.</param>
    /// <param name="maxReleases">How many releases to keep, newest first.</param>
    /// <param name="etag">The ETag of the first page from the last fetch, or null.</param>
    /// <param name="cancellationToken">Cancels the fetch; this one is rethrown, every other failure is a result.</param>
    public async Task<FetchOutcome> FetchAsync(string repository, string? token, int maxReleases, string? etag, CancellationToken cancellationToken)
    {
        if (!ChangelogOptions.IsRepository(repository))
        {
            return new FetchOutcome(FetchStatus.Failed, Detail: "the repository is not owner/repo");
        }

        var perPage = Math.Clamp(maxReleases, 1, 100);
        Uri? next = new Uri($"repos/{repository}/releases?per_page={perPage}", UriKind.Relative);
        var releases = new List<ReleaseEntry>();
        string? firstEtag = null;

        for (var page = 1; page <= MaxPages && next is not null && releases.Count < maxReleases; page++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, next);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            if (page == 1 && !string.IsNullOrEmpty(etag))
            {
                request.Headers.TryAddWithoutValidation("If-None-Match", etag);
            }

            HttpResponseMessage response;
            try
            {
                response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                return new FetchOutcome(FetchStatus.Failed, Detail: "GitHub did not answer in time");
            }
            catch (HttpRequestException exception)
            {
                // The message of the exception says what failed (name resolution, connection refused, TLS); it holds no header.
                return new FetchOutcome(FetchStatus.Failed, Detail: $"the request to GitHub failed: {exception.Message}");
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.NotModified && page == 1)
                {
                    return new FetchOutcome(FetchStatus.NotModified, ETag: etag);
                }
                if (IsRateLimited(response, out var retryAt))
                {
                    return new FetchOutcome(FetchStatus.RateLimited, RetryAt: retryAt, Detail: "the GitHub rate limit was reached");
                }
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new FetchOutcome(FetchStatus.NotFound, Detail: "GitHub answered 404: the repository does not exist or cannot be seen");
                }
                if (!response.IsSuccessStatusCode)
                {
                    return new FetchOutcome(FetchStatus.Failed, Detail: $"GitHub answered {(int)response.StatusCode}");
                }

                try
                {
                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    releases.AddRange(GitHubReleaseJson.Parse(stream, repository));
                }
                catch (Exception exception) when (exception is JsonException or InvalidOperationException or ArgumentException)
                {
                    return new FetchOutcome(FetchStatus.Failed, Detail: $"GitHub sent something that is not a list of releases: {exception.Message}");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception) when (exception is HttpRequestException or IOException or OperationCanceledException)
                {
                    return new FetchOutcome(FetchStatus.Failed, Detail: $"reading the answer of GitHub failed: {exception.Message}");
                }

                if (page == 1)
                {
                    firstEtag = response.Headers.ETag?.ToString();
                }
                next = NextPage(response);
            }
        }

        if (releases.Count > maxReleases)
        {
            releases.RemoveRange(maxReleases, releases.Count - maxReleases);
        }
        logger.LogInformation("Fetched {Count} releases of {Repository}", releases.Count, repository);
        return new FetchOutcome(FetchStatus.Ok, DeduplicateSlugs(releases), firstEtag);
    }

    // Slugs were made per page: make them unique across the whole list.
    private static List<ReleaseEntry> DeduplicateSlugs(List<ReleaseEntry> releases)
    {
        var slugs = ReleaseSlug.Assign(releases.Select(r => r.Tag));
        return [.. releases.Select((r, i) => r with { Slug = slugs[i] })];
    }

    /// <summary>
    /// True for a 429, and for a 403 that GitHub sends when the primary limit is used up (<c>X-RateLimit-Remaining: 0</c>) or a secondary limit applies
    /// (<c>Retry-After</c>). A 403 without those is a refusal, not a limit. <paramref name="retryAt"/> is the reset time or the time <c>Retry-After</c> gives.
    /// </summary>
    public static bool IsRateLimited(HttpResponseMessage response, out DateTimeOffset? retryAt)
    {
        retryAt = null;
        if (response.StatusCode is not (HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests))
        {
            return false;
        }
        var remaining = Header(response, "X-RateLimit-Remaining");
        var exhausted = remaining == "0";
        var retryAfter = response.Headers.RetryAfter;
        if (response.StatusCode != HttpStatusCode.TooManyRequests && !exhausted && retryAfter is null)
        {
            return false;
        }
        if (retryAfter?.Delta is { } delta)
        {
            retryAt = DateTimeOffset.UtcNow + delta;
        }
        else if (retryAfter?.Date is { } date)
        {
            retryAt = date;
        }
        else if (long.TryParse(Header(response, "X-RateLimit-Reset"), NumberStyles.None, CultureInfo.InvariantCulture, out var reset) && reset is > 0 and < 253402300799)
        {
            retryAt = DateTimeOffset.FromUnixTimeSeconds(reset);
        }
        return true;
    }

    private static string? Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;

    // The next page, from the Link header, when it is on the host the client talks to (and over https): the token must not follow a link anywhere else.
    private Uri? NextPage(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Link", out var links))
        {
            return null;
        }
        var match = NextLink().Match(string.Join(", ", links));
        if (!match.Success || !Uri.TryCreate(match.Groups[1].Value, UriKind.Absolute, out var uri))
        {
            return null;
        }
        var origin = http.BaseAddress!;
        if (uri.Scheme != Uri.UriSchemeHttps || !string.Equals(uri.Authority, origin.Authority, StringComparison.OrdinalIgnoreCase) || uri.UserInfo.Length > 0)
        {
            logger.LogWarning("Ignored a next page link to another host");
            return null;
        }
        return uri;
    }
}
