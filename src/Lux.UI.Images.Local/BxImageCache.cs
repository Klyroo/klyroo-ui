using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Lux.UI.Images.Local;

/// <summary>A variant found on disk.</summary>
internal readonly record struct BxCachedVariant(string Path, string ContentType);

/// <summary>
/// The disk cache: one directory per source image holding its variants, bounded twice. A source keeps at most
/// <see cref="BxLocalImagesOptions.MaxVariantsPerSource"/> variants (the least recently used one is evicted to make room, so junk variants cannot lock
/// real ones out), and the whole cache stays under <see cref="BxLocalImagesOptions.MaxCacheBytes"/> counted in allocated blocks. Last access is the
/// modification time, refreshed when a variant is read. Files and directories can vanish at any moment: nothing here assumes they exist.
/// </summary>
internal sealed class BxImageCache
{
    internal const long BlockSize = 4096;
    internal const long EntryOverhead = 512;
    internal const string MetaName = "source.meta";
    private static readonly TimeSpan StaleTemporaryAge = TimeSpan.FromMinutes(10);

    private readonly string _root;
    private readonly long _maxBytes;
    private readonly int _maxVariants;
    private readonly TimeSpan _touchInterval;
    private readonly ILogger _logger;
    private readonly object[] _stripes = Enumerable.Range(0, 64).Select(_ => new object()).ToArray();
    private readonly ConcurrentDictionary<string, int> _activeTemporaries = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, int> _activeDirectories = new(StringComparer.Ordinal);
    private readonly Lazy<bool> _caseInsensitive;
    private long _bytes;
    private int _scanned;
    private int _pruning;
    private Task _pruneTask = Task.CompletedTask;

    public BxImageCache(BxLocalImagesOptions options, ILogger logger)
    {
        _root = options.CacheDirectory;
        _maxBytes = options.MaxCacheBytes;
        _maxVariants = Math.Max(1, options.MaxVariantsPerSource);
        _touchInterval = options.AccessTouchInterval;
        _logger = logger;
        _caseInsensitive = new Lazy<bool>(ProbeCaseInsensitivity);
    }

    /// <summary>Whether the file system under the cache folder ignores case (Windows, macOS by default): then <c>/IMG/A.png</c> and <c>/img/a.png</c> are one file.</summary>
    public bool CaseInsensitive => _caseInsensitive.Value;

    /// <summary>What the disk really holds for a file of this length: whole blocks plus the cost of the entry itself.</summary>
    public static long AllocatedSize(long length) => (length + BlockSize - 1) / BlockSize * BlockSize + EntryOverhead;

    private static long DirectoryCost => BlockSize + EntryOverhead;

    public string DirectoryOf(string sourceKey) => Path.Combine(_root, BxImageRequestParser.Hash(sourceKey)[..32]);

    private object StripeOf(string directory) => _stripes[(uint)directory.GetHashCode() % (uint)_stripes.Length];

    public bool TryGet(string directory, string key, out BxCachedVariant variant)
    {
        variant = default;
        try
        {
            foreach (var path in Directory.EnumerateFiles(directory, key + ".*"))
            {
                if (path.EndsWith(".tmp", StringComparison.Ordinal)) continue;
                Touch(path);
                variant = new BxCachedVariant(path, BxImageProcessor.ContentTypeOf(Path.GetExtension(path)));
                return true;
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // The directory is gone or being pruned: it is a miss.
        }
        return false;
    }

    private void Touch(string path)
    {
        try
        {
            if (DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > _touchInterval) File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Evicted meanwhile: the next request recreates it.
        }
    }

    /// <summary>Writes a variant, evicting the least recently used ones of the same source first when the source is at its budget. Returns the final path.</summary>
    public async Task<string> StoreAsync(string directory, string key, string extension, byte[] data, CancellationToken cancellation)
    {
        var target = Path.Combine(directory, key + extension);
        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        _activeDirectories.AddOrUpdate(directory, 1, (_, n) => n + 1);
        _activeTemporaries[temporary] = 0;
        try
        {
            await WriteWithRetryAsync(directory, temporary, data, cancellation).ConfigureAwait(false);
            long delta;
            lock (StripeOf(directory))
            {
                delta = EvictForNewVariant(directory, target);
                var existed = File.Exists(target);
                File.Move(temporary, target, overwrite: true);
                delta += existed ? 0 : AllocatedSize(data.LongLength);
            }
            AddBytes(delta);
            return target;
        }
        catch
        {
            TryDelete(temporary);
            throw;
        }
        finally
        {
            _activeTemporaries.TryRemove(temporary, out _);
            if (_activeDirectories.AddOrUpdate(directory, 0, (_, n) => n - 1) <= 0) _activeDirectories.TryRemove(directory, out _);
        }
    }

    private async Task WriteWithRetryAsync(string directory, string path, byte[] data, CancellationToken cancellation)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    AddBytes(DirectoryCost);
                }
                await File.WriteAllBytesAsync(path, data, cancellation).ConfigureAwait(false);
                return;
            }
            catch (DirectoryNotFoundException) when (attempt < 3)
            {
                // A prune removed the (empty) directory between its creation and the write.
            }
        }
    }

    // Called under the lock of the source: counts the variants and evicts the oldest ones until one more fits. Returns the bytes freed (negative).
    private long EvictForNewVariant(string directory, string target)
    {
        long delta = 0;
        try
        {
            var variants = new DirectoryInfo(directory).EnumerateFiles()
                .Where(f => f.Name != MetaName && !f.Name.EndsWith(".tmp", StringComparison.Ordinal) && !string.Equals(f.FullName, target, StringComparison.Ordinal))
                .OrderBy(f => f.LastWriteTimeUtc)
                .ToList();
            for (var i = 0; variants.Count - i >= _maxVariants; i++)
            {
                var length = variants[i].Length;
                if (TryDelete(variants[i].FullName)) delta -= AllocatedSize(length);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // The directory vanished: nothing to evict.
        }
        return delta;
    }

    /// <summary>The stamp and fetch time recorded for a remote source, or null when none (or unreadable).</summary>
    public (string Stamp, DateTime FetchedUtc)? ReadMeta(string directory)
    {
        try
        {
            var path = Path.Combine(directory, MetaName);
            var lines = File.ReadAllLines(path);
            if (lines.Length >= 2 && long.TryParse(lines[1], out var ticks)) return (lines[0], new DateTime(ticks, DateTimeKind.Utc));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // No meta yet, or evicted.
        }
        return null;
    }

    public async Task WriteMetaAsync(string directory, string stamp, CancellationToken cancellation)
    {
        var path = Path.Combine(directory, MetaName);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var content = System.Text.Encoding.UTF8.GetBytes(stamp + "\n" + DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n");
        _activeDirectories.AddOrUpdate(directory, 1, (_, n) => n + 1);
        _activeTemporaries[temporary] = 0;
        try
        {
            await WriteWithRetryAsync(directory, temporary, content, cancellation).ConfigureAwait(false);
            lock (StripeOf(directory))
            {
                var existed = File.Exists(path);
                File.Move(temporary, path, overwrite: true);
                if (!existed) AddBytes(AllocatedSize(content.Length));
            }
        }
        catch
        {
            TryDelete(temporary);
            throw;
        }
        finally
        {
            _activeTemporaries.TryRemove(temporary, out _);
            if (_activeDirectories.AddOrUpdate(directory, 0, (_, n) => n - 1) <= 0) _activeDirectories.TryRemove(directory, out _);
        }
    }

    private void AddBytes(long delta)
    {
        if (Interlocked.Exchange(ref _scanned, 1) == 0)
        {
            // The first write of this process: learn what earlier runs left on disk.
            SchedulePrune();
        }
        if (Interlocked.Add(ref _bytes, delta) > _maxBytes) SchedulePrune();
    }

    /// <summary>Starts a trim in the background unless one is running. Requests never wait for it.</summary>
    public void SchedulePrune()
    {
        if (Interlocked.CompareExchange(ref _pruning, 1, 0) != 0) return;
        _pruneTask = Task.Run(() =>
        {
            try
            {
                Trim();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Trimming the image cache failed");
            }
            finally
            {
                Volatile.Write(ref _pruning, 0);
            }
        });
    }

    /// <summary>Completes when the running trim (if any) has finished. For tests.</summary>
    internal Task IdleAsync() => _pruneTask;

    internal long Bytes => Interlocked.Read(ref _bytes);

    private void Trim()
    {
        var root = new DirectoryInfo(_root);
        if (!root.Exists)
        {
            Interlocked.Exchange(ref _bytes, 0);
            return;
        }

        var variants = new List<(FileInfo File, long Size)>();
        var directories = new List<DirectoryInfo>();
        long total = 0;
        foreach (var directory in SafeEnumerate(() => root.EnumerateDirectories()))
        {
            directories.Add(directory);
            total += DirectoryCost;
            foreach (var file in SafeEnumerate(() => directory.EnumerateFiles()))
            {
                long size;
                try { size = AllocatedSize(file.Length); }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException) { continue; }

                if (file.Name.EndsWith(".tmp", StringComparison.Ordinal))
                {
                    // Never an in-flight write; a leftover of a crash goes after a while.
                    if (!_activeTemporaries.ContainsKey(file.FullName) && DateTime.UtcNow - file.LastWriteTimeUtc > StaleTemporaryAge && TryDelete(file.FullName)) continue;
                    total += size;
                }
                else if (file.Name == MetaName)
                {
                    total += size;
                }
                else
                {
                    variants.Add((file, size));
                    total += size;
                }
            }
        }
        // Files directly in the root (not ours) are not counted: only what this cache wrote is bounded.

        var target = (long)(_maxBytes * 0.9);
        if (total > _maxBytes)
        {
            foreach (var (file, size) in variants.OrderBy(v => v.File.LastWriteTimeUtc))
            {
                if (total <= target) break;
                lock (StripeOf(file.DirectoryName ?? ""))
                {
                    if (TryDelete(file.FullName)) total -= size;
                }
            }
        }

        // A directory without variants goes, with its source record, unless a writer holds it or the record was just written.
        foreach (var directory in directories)
        {
            if (_activeDirectories.ContainsKey(directory.FullName)) continue;
            try
            {
                var remaining = directory.EnumerateFiles().ToList();
                if (remaining.Any(f => f.Name != MetaName && (!f.Name.EndsWith(".tmp", StringComparison.Ordinal) || _activeTemporaries.ContainsKey(f.FullName) || DateTime.UtcNow - f.LastWriteTimeUtc <= StaleTemporaryAge))) continue;
                if (remaining.Any(f => f.Name == MetaName && DateTime.UtcNow - f.LastWriteTimeUtc < TimeSpan.FromMinutes(1))) continue;
                lock (StripeOf(directory.FullName))
                {
                    foreach (var file in remaining)
                    {
                        if (TryDelete(file.FullName)) total -= AllocatedSize(file.Length);
                    }
                    if (!_activeDirectories.ContainsKey(directory.FullName))
                    {
                        directory.Delete();
                        total -= DirectoryCost;
                    }
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Not empty or in use: left for the next trim.
            }
        }

        Interlocked.Exchange(ref _bytes, Math.Max(0, total));
    }

    private static IEnumerable<T> SafeEnumerate<T>(Func<IEnumerable<T>> source)
    {
        try { return source().ToList(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return []; }
    }

    private static bool TryDelete(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private bool ProbeCaseInsensitivity()
    {
        try
        {
            Directory.CreateDirectory(_root);
            var name = "case-probe-" + Guid.NewGuid().ToString("N");
            var lower = Path.Combine(_root, name.ToLowerInvariant());
            File.WriteAllBytes(lower, []);
            try { return File.Exists(Path.Combine(_root, name.ToUpperInvariant())); }
            finally { File.Delete(lower); }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
