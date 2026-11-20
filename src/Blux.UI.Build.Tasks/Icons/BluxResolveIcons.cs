using System.Security.Cryptography;
using System.Text;
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

        /// <summary>False when no icon is bundled: the file is then removed, and the targets leave it out of the compilation.</summary>
        [Output] public bool HasIcons { get; set; }


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

            var sources = new List<IconSourceStat>();
            if (Scan)
            {
                var skip = new HashSet<string>(ExcludeDirectories.Select(d => IconSourceFinder.Normalize(d.ItemSpec)), StringComparer.Ordinal);
                var roots = new List<string> { project };
                roots.AddRange(ScanDirectories.Select(d => Path.GetFullPath(Path.Combine(project, d.ItemSpec))));
                sources = IconSourceFinder.Find(roots, skip, m => Log.LogMessage(MessageImportance.Low, "Blux icons: " + m));
            }

            // Nothing that decides the bundle changed since the last build that came out clean: the files are not even read.
            var stamp = OutputFile + ".inputs";
            var key = InputsKey(project, sources);
            if (ReadStamp(stamp, out var stampKey, out var stampHasIcons) && stampKey == key && stampHasIcons == File.Exists(OutputFile))
            {
                HasIcons = stampHasIcons;
                Log.LogMessage(MessageImportance.Low, "Blux icons: nothing changed since the last build, the bundle is up to date.");
                return true;
            }

            foreach (var source in sources)
            {
                try
                {
                    options.Sources.Add(new IconSourceFile(source.Path, File.ReadAllText(source.Path)));
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    // A file being written by another tool, or one we may not read: it is scanned by the next build.
                    Log.LogMessage(MessageImportance.Low, "Blux icons: {0} could not be read ({1}) and is not scanned.", source.Path, e.Message);
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

            if (result.CacheWarning != null)
            {
                Log.LogWarning(subcategory: null, warningCode: "BLUXICON004", helpKeyword: null, file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0, message: result.CacheWarning);
            }

            HasIcons = result.IconCount > 0;
            Directory.CreateDirectory(Path.GetDirectoryName(OutputFile)!);
            if (HasIcons)
            {
                if (!File.Exists(OutputFile) || File.ReadAllText(OutputFile) != result.Source)
                {
                    File.WriteAllText(OutputFile, result.Source);
                }
            }
            else if (File.Exists(OutputFile))
            {
                // No icon left: the file goes (a leftover would keep registering icons nobody asks for), and the targets drop it from the compilation.
                File.Delete(OutputFile);
            }
            Log.LogMessage(MessageImportance.Normal, "Blux icons: {0} icon(s) bundled, {1} downloaded.", result.IconCount, result.Downloaded);

            // Only a clean run is remembered: a problem is reported again by the next build until it is fixed.
            if (!Log.HasLoggedErrors && result.CacheWarning == null && !result.Problems.Any(p => p.Fatal))
            {
                WriteStamp(stamp, key, HasIcons);
            }
            else
            {
                TryDelete(stamp);
            }
            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// What decides the bundle: the paths, sizes and times of the scanned files and of the files of the collections, the items, the options,
        /// the day of the cache window (so a cache that expires is refreshed) and this task's assembly (a new Blux.UI may read or write differently).
        /// </summary>
        private string InputsKey(string project, List<IconSourceStat> sources)
        {
            var sb = new StringBuilder();
            sb.Append("task=").Append(typeof(BluxResolveIcons).Assembly.ManifestModule.ModuleVersionId).Append('\n');
            sb.Append("scan=").Append(Scan).Append(";api=").Append(Api).Append(";fail=").Append(FailOnError).Append(";offline=").Append(Offline)
              .Append(";cache=").Append(CacheDir).Append(";output=").Append(OutputFile).Append('\n');
            var days = Math.Max(1, CacheDays);
            sb.Append("window=").Append(Offline ? 0 : (long)(DateTime.UtcNow - new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays / days).Append('\n');
            foreach (var item in Icons.Select(i => i.ItemSpec).OrderBy(i => i, StringComparer.Ordinal)) sb.Append("icon=").Append(item).Append('\n');
            foreach (var item in Collections)
            {
                var directory = Path.GetFullPath(Path.Combine(project, item.ItemSpec));
                sb.Append("collection=").Append(directory).Append(';').Append(item.GetMetadata("Prefix")).Append('\n');
                try
                {
                    foreach (var file in Directory.GetFiles(directory, "*.svg", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
                    {
                        var info = new FileInfo(file);
                        sb.Append(file).Append('|').Append(info.Length).Append('|').Append(info.LastWriteTimeUtc.Ticks).Append('\n');
                    }
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    sb.Append("unreadable\n");
                }
            }
            foreach (var source in sources) sb.Append(source.Path).Append('|').Append(source.Length).Append('|').Append(source.Ticks).Append('\n');
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()))).Replace("-", "");
            }
        }

        private static bool ReadStamp(string stamp, out string key, out bool hasIcons)
        {
            key = "";
            hasIcons = false;
            try
            {
                var lines = File.ReadAllLines(stamp);
                if (lines.Length < 2) return false;
                key = lines[0];
                hasIcons = lines[1] == "1";
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static void WriteStamp(string stamp, string key, bool hasIcons)
        {
            try
            {
                File.WriteAllText(stamp, key + "\n" + (hasIcons ? "1" : "0") + "\n");
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                // Without a stamp the next build does the work again.
            }
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); } catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { }
        }
    }
}
