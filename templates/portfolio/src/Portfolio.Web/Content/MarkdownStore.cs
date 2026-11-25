using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klyroo.Templates.Content;

/// <summary>
/// Holds the content of the site in memory. It is read once at startup (a content file that cannot be used stops the startup, with the file named)
/// and, with <see cref="ContentOptions.Watch"/>, read again a moment after a file changes. A reload that fails keeps the previous content and logs
/// the error, so a typo in a page being edited does not take the site down.
/// </summary>
public sealed class MarkdownStore : IDisposable
{
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(300);

    private readonly ContentOptions _options;
    private readonly MarkdownRenderer _renderer;
    private readonly ILogger<MarkdownStore>? _logger;
    private readonly FileSystemWatcher? _watcher;
    private readonly Timer? _timer;
    private volatile MarkdownSite _site;

    public MarkdownStore(ContentOptions options, ILogger<MarkdownStore>? logger = null)
    {
        _options = options;
        _logger = logger;
        _renderer = new MarkdownRenderer(options.AllowRawHtml);
        _site = Load();

        if (options.Watch)
        {
            _timer = new Timer(_ => Reload(), null, Timeout.Infinite, Timeout.Infinite);
            _watcher = new FileSystemWatcher(Path.GetFullPath(options.Root)) { IncludeSubdirectories = true, EnableRaisingEvents = true };
            _watcher.Changed += (_, e) => Touched(e.FullPath);
            _watcher.Created += (_, e) => Touched(e.FullPath);
            _watcher.Deleted += (_, e) => Touched(e.FullPath);
            _watcher.Renamed += (_, e) => Touched(e.FullPath);
        }
    }

    /// <summary>The content as it is now. Take it once per render: a reload swaps the whole thing.</summary>
    public MarkdownSite Site => _site;

    /// <summary>Raised after a reload succeeded.</summary>
    public event Action? Reloaded;

    /// <summary>Reads the content folder again now.</summary>
    public bool Reload()
    {
        try
        {
            _site = Load();
        }
        catch (Exception error)
        {
            // Whatever went wrong while reading (a file being written, a bad file, a folder that went away), the content that was good stays. A reload
            // runs on a timer thread, where an exception nobody catches would end the process.
            _logger?.LogError(error, "The content was not reloaded, the previous version stays in use: {Message}", error.Message);
            return false;
        }
        try
        {
            Reloaded?.Invoke();
        }
        catch (Exception error)
        {
            _logger?.LogError(error, "A handler of Reloaded failed: {Message}", error.Message);
        }
        return true;
    }

    private MarkdownSite Load()
    {
        var site = MarkdownSite.Load(_options, _renderer);
        foreach (var link in site.BrokenLinks)
        {
            _logger?.LogWarning("The page '{Page}' links to '{Target}', which is not a page.", link.FromSlug, link.Target);
        }
        foreach (var warning in _options.Validate?.Invoke(site) ?? [])
        {
            _logger?.LogWarning("{Warning}", warning);
        }
        foreach (var image in site.MissingImages)
        {
            _logger?.LogWarning("The page '{Page}' shows the image '{Image}', which is not in the web root. Images are served from the web root, not from the content folder: put the file there and write its path from the root, for example /img/chart.png.", image.FromSlug, image.Path);
        }
        return site;
    }

    private void Touched(string path)
    {
        var name = Path.GetFileName(path);
        if (name.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) || !name.Contains('.'))
        {
            _timer?.Change(Debounce, Timeout.InfiniteTimeSpan);
        }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _timer?.Dispose();
    }
}

public static class MarkdownContentServiceCollectionExtensions
{
    /// <summary>Registers the content of the site as one <see cref="MarkdownStore"/>. The folder is read when the store is first requested.</summary>
    public static IServiceCollection AddMarkdownContent(this IServiceCollection services, ContentOptions options) =>
        services.AddSingleton(provider => new MarkdownStore(options, provider.GetService<ILogger<MarkdownStore>>()));
}
