using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Changelog.Web.ReleaseNotes;

/// <summary>Where <see cref="ReleaseService"/> gets the releases from: GitHub, or a file.</summary>
public interface IReleaseSource
{
    /// <summary>False when the settings are not enough to ask for releases (no repository): the service then says so and asks for nothing.</summary>
    bool IsConfigured { get; }

    /// <summary>The page of the repository on GitHub, or null when there is none to link to.</summary>
    string? RepositoryUrl { get; }

    /// <summary>Reads the releases. <paramref name="etag"/> is the one of the last successful read, for a source that can answer "not modified".</summary>
    Task<FetchOutcome> FetchAsync(string? etag, CancellationToken cancellationToken);
}

/// <summary>The releases of <c>Changelog:Repository</c>, from the GitHub API.</summary>
public sealed class GitHubReleaseSource(GitHubReleaseClient client, IOptions<ChangelogOptions> options) : IReleaseSource
{
    private readonly ChangelogOptions _options = options.Value;

    // Any setting that cannot work (no repository, an api address that is not https, a number that is not positive) means no request: the token is never sent
    // to an address nobody checked.
    public bool IsConfigured => ChangelogOptions.IsRepository(_options.Repository) && _options.Problem() is null;

    public string? RepositoryUrl => _options.RepositoryUrl;

    public Task<FetchOutcome> FetchAsync(string? etag, CancellationToken cancellationToken) =>
        client.FetchAsync(_options.Repository!, _options.GitHubToken, _options.MaxReleases, etag, cancellationToken);
}

/// <summary>
/// The releases of a JSON file in the format of the GitHub API (<c>Changelog:File</c>): no network, so the template runs and is tested offline. The file is read
/// each time the service asks, which is once per <c>Changelog:CacheDuration</c>.
/// </summary>
public sealed class FileReleaseSource(IOptions<ChangelogOptions> options, IHostEnvironment environment, ILogger<FileReleaseSource> logger) : IReleaseSource
{
    private readonly ChangelogOptions _options = options.Value;

    public bool IsConfigured => true;

    public string? RepositoryUrl => _options.RepositoryUrl;

    public async Task<FetchOutcome> FetchAsync(string? etag, CancellationToken cancellationToken)
    {
        var path = Resolve();
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);
            var releases = GitHubReleaseJson.Parse(stream, ChangelogOptions.IsRepository(_options.Repository) ? _options.Repository : null);
            return new FetchOutcome(FetchStatus.Ok, [.. releases.Take(_options.MaxReleases)]);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or ArgumentException)
        {
            logger.LogWarning("The releases file {File} could not be read: {Reason}", _options.File, exception.Message);
            return new FetchOutcome(FetchStatus.Failed, Detail: $"{_options.File} could not be read: {exception.Message}");
        }
    }

    // Relative to the folder of the site, and to the one next to the assembly when the site runs from another folder.
    private string Resolve()
    {
        var path = Path.GetFullPath(_options.File, environment.ContentRootPath);
        return File.Exists(path) || Path.IsPathRooted(_options.File) ? path : Path.GetFullPath(_options.File, AppContext.BaseDirectory);
    }
}
