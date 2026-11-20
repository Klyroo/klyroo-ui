using Microsoft.Build.Framework;

namespace Blux.UI.Build
{
    /// <summary>
    /// Bundles the icons an app uses: scans its sources for literal icon names, adds the <c>BluxIcon</c> items and the
    /// <c>BluxIconCollection</c> folders, downloads what the cache lacks from the Iconify API and writes the C# file that
    /// registers the result with Blux.UI. See the Icons guide.
    /// </summary>
    public sealed class BluxResolveIcons : Microsoft.Build.Utilities.Task
    {
        /// <summary>The BluxIcon items: identity = icon name in any form.</summary>
        public ITaskItem[] Icons { get; set; } = Array.Empty<ITaskItem>();

        /// <summary>The BluxIconCollection items: identity = folder, metadata <c>Prefix</c>.</summary>
        public ITaskItem[] Collections { get; set; } = Array.Empty<ITaskItem>();

        /// <summary>Scan the sources of the project for icon names.</summary>
        public bool Scan { get; set; } = true;

        /// <summary>Folder of the project: the sources are searched below it and relative folders resolve against it.</summary>
        [Required] public string ProjectDirectory { get; set; } = "";

        /// <summary>More folders to scan (BluxIconScanDirectory items), for sources kept outside the project folder.</summary>
        public ITaskItem[] ScanDirectories { get; set; } = Array.Empty<ITaskItem>();

        /// <summary>Folders that are not scanned, by full path (bin, obj and the like).</summary>
        public ITaskItem[] ExcludeDirectories { get; set; } = Array.Empty<ITaskItem>();

        /// <summary>Folder of the cache of downloaded icons.</summary>
        [Required] public string CacheDir { get; set; } = "";

        /// <summary>The C# file to generate (absolute).</summary>
        [Required] public string OutputFile { get; set; } = "";

        /// <summary>Base address of the Iconify API.</summary>
        public string Api { get; set; } = "https://api.iconify.design";

        /// <summary>An icon that cannot be found is an error (Release) instead of a warning (Debug).</summary>
        public bool FailOnError { get; set; }

        /// <summary>Never touch the network (BluxOffline): every icon must already be in the cache.</summary>
        public bool Offline { get; set; }

        /// <summary>How long a cached icon is trusted before it is asked again.</summary>
        public int CacheDays { get; set; } = 30;

        public override bool Execute()
        {
            try
            {
                return Run();
            }
            catch (Exception e) when (!(e is OutOfMemoryException || e is StackOverflowException))
            {
                // Whatever goes wrong (an unreadable file, a malformed answer) is a diagnostic of ours, never an MSB4018 crash trace.
                Log.LogError(subcategory: null, errorCode: "BLUXICON003", helpKeyword: null, file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0, message: "Could not bundle the icons: " + e.GetType().Name + ": " + e.Message + " BluxIcons=false turns the feature off.");
                return false;
            }
        }

        private bool Run()
        {
            if (!Uri.TryCreate(Api, UriKind.Absolute, out var api) || api.Scheme != Uri.UriSchemeHttps)
            {
                Log.LogError(subcategory: null, errorCode: "BLUXICON002", helpKeyword: null, file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0, message: "BluxIconsApi must be an absolute https address (it is \"" + Api + "\").");
                return false;
            }
            var project = ProjectDirectory.Length > 0 ? ProjectDirectory : Directory.GetCurrentDirectory();
            var options = new IconBuildOptions
            {
                CacheDir = CacheDir,
                Api = Api,
                Offline = Offline,
                CacheMaxAge = TimeSpan.FromDays(Math.Max(0, CacheDays)),
                Info = m => Log.LogMessage(MessageImportance.Normal, "Blux icons: " + m),
            };
            foreach (var item in Icons) options.Explicit.Add(item.ItemSpec);
            foreach (var item in Collections)
            {
                var directory = Path.GetFullPath(Path.Combine(project, item.ItemSpec));
                options.Folders.Add(new IconFolder(directory, item.GetMetadata("Prefix")));
            }

            if (Scan)
            {
                var skip = new HashSet<string>(ExcludeDirectories.Select(d => Normalize(d.ItemSpec)), StringComparer.Ordinal);
                var roots = new List<string> { project };
                roots.AddRange(ScanDirectories.Select(d => Path.GetFullPath(Path.Combine(project, d.ItemSpec))));
                foreach (var root in roots.Distinct(StringComparer.Ordinal))
                {
                    if (!Directory.Exists(root)) continue;
                    foreach (var path in Enumerate(root, skip))
                    {
                        try
                        {
                            options.Sources.Add(new IconSourceFile(path, File.ReadAllText(path)));
                        }
                        catch (IOException)
                        {
                            // A file being written by another tool: it is scanned by the next build.
                        }
                    }
                }
            }

            IconBuildResult result;
            try
            {
                result = IconBundleBuilder.Run(options, new HttpNetwork());
            }
            catch (Exception e) when (e is BuildToolException || e is IOException || e is UnauthorizedAccessException)
            {
                Log.LogError(subcategory: null, errorCode: "BLUXICON003", helpKeyword: null, file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0, message: "Could not bundle the icons: " + e.Message);
                return false;
            }

            foreach (var problem in result.Problems)
            {
                if (!problem.Fatal)
                {
                    Log.LogMessage(MessageImportance.Low, "Blux icons: " + problem.Message);
                    continue;
                }
                var message = problem.Message + " Icons are bundled at build time (BluxIcons=false turns the feature off, see the Icons guide).";
                if (FailOnError)
                {
                    Log.LogError(subcategory: null, errorCode: "BLUXICON001", helpKeyword: null, file: problem.File, lineNumber: problem.Line, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0, message: message);
                }
                else
                {
                    Log.LogWarning(subcategory: null, warningCode: "BLUXICON001", helpKeyword: null, file: problem.File, lineNumber: problem.Line, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0, message: message + " (an error in a Release build)");
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(OutputFile)!);
            if (!File.Exists(OutputFile) || File.ReadAllText(OutputFile) != result.Source)
            {
                File.WriteAllText(OutputFile, result.Source);
            }
            Log.LogMessage(MessageImportance.Normal, "Blux icons: {0} icon(s) bundled, {1} downloaded.", result.IconCount, result.Downloaded);
            return !Log.HasLoggedErrors;
        }

        private static readonly string[] Extensions = { ".razor", ".cshtml", ".cs" };
        private static readonly string[] SkippedNames = { "bin", "obj", "node_modules", ".git", ".vs", "wwwroot" };

        private static IEnumerable<string> Enumerate(string directory, HashSet<string> skip)
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
                    if (SkippedNames.Contains(name, StringComparer.OrdinalIgnoreCase) || skip.Contains(Normalize(child))) continue;
                    stack.Push(child);
                }
            }
        }

        private static string Normalize(string path) =>
            Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
