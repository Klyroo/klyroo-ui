namespace Lux.UI.Build
{
    /// <summary>A source file that may hold icon names: its path, size and last write time (what decides whether the bundle is still up to date).</summary>
    internal sealed class IconSourceStat
    {
        public IconSourceStat(string path, long length, long ticks)
        {
            Path = path;
            Length = length;
            Ticks = ticks;
        }

        public string Path { get; }
        public long Length { get; }
        public long Ticks { get; }
    }

    /// <summary>
    /// Lists the <c>.razor</c>, <c>.cshtml</c> and <c>.cs</c> files below the roots that the scan reads. It never reads a file: size and time are enough to know
    /// whether anything changed. Not listed: the folders of the build output and of packages, folders that start with a dot (<c>.git</c>, <c>.vs</c>, <c>.claude</c>,
    /// <c>.idea</c>: tools, and the metadata of nested checkouts), folders that are links (a link to a parent folder would loop, a link elsewhere is somebody else's
    /// tree), links to files, and files over <see cref="MaxBytes"/> (a generated blob named <c>.cs</c> is not an app source).
    /// </summary>
    internal static class IconSourceFinder
    {
        public const long MaxBytes = 1024 * 1024;

        private static readonly string[] Extensions = { ".razor", ".cshtml", ".cs" };
        private static readonly string[] SkippedNames = { "bin", "obj", "node_modules", "wwwroot" };

        public static List<IconSourceStat> Find(IEnumerable<string> roots, ISet<string> skip, Action<string> note)
        {
            var found = new List<IconSourceStat>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var root in roots.Distinct(StringComparer.Ordinal))
            {
                if (!Directory.Exists(root)) continue;
                foreach (var path in Enumerate(root, skip))
                {
                    if (!seen.Add(path)) continue;
                    try
                    {
                        var info = new FileInfo(path);
                        if ((info.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                        if (info.Length > MaxBytes)
                        {
                            note(path + " is larger than 1 MiB and is not scanned for icon names (list its icons with <LuxIcon Include=\"...\" />).");
                            continue;
                        }
                        found.Add(new IconSourceStat(path, info.Length, info.LastWriteTimeUtc.Ticks));
                    }
                    catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                    {
                        // Gone between the listing and now: the next build sees the tree as it is then.
                    }
                }
            }
            return found;
        }

        private static IEnumerable<string> Enumerate(string directory, ISet<string> skip)
        {
            var stack = new Stack<string>();
            stack.Push(directory);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                string[] files;
                string[] children;
                try
                {
                    files = Directory.GetFiles(current);
                    children = Directory.GetDirectories(current);
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    continue;
                }
                foreach (var file in files.OrderBy(f => f, StringComparer.Ordinal))
                {
                    var extension = Path.GetExtension(file);
                    if (Extensions.Any(x => string.Equals(x, extension, StringComparison.OrdinalIgnoreCase))) yield return file;
                }
                foreach (var child in children.OrderByDescending(c => c, StringComparer.Ordinal))
                {
                    var name = Path.GetFileName(child);
                    if (name.StartsWith(".", StringComparison.Ordinal) || SkippedNames.Contains(name, StringComparer.OrdinalIgnoreCase) || skip.Contains(Normalize(child)) || IsLink(child)) continue;
                    stack.Push(child);
                }
            }
        }

        private static bool IsLink(string directory)
        {
            try
            {
                return (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                return true;
            }
        }

        public static string Normalize(string path) =>
            Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
