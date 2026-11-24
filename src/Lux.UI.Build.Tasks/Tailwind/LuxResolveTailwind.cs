using Microsoft.Build.Framework;

namespace Lux.UI.Build
{
    /// <summary>
    /// Finds the Tailwind standalone CLI for the machine that builds: the binary named by <c>LuxTailwindExecutable</c>, or the pinned
    /// release, downloaded once into the cache and checked against its embedded SHA-256. See the Tailwind guide.
    /// </summary>
    public sealed class LuxResolveTailwind : Microsoft.Build.Utilities.Task
    {
        /// <summary>LuxTailwindExecutable: a binary to use as it is, with no download and no check.</summary>
        public string Executable { get; set; } = "";

        /// <summary>Release of the CLI to download (LuxTailwindVersion).</summary>
        [Required] public string Version { get; set; } = "";

        /// <summary>Folder of the cache of downloaded binaries (LuxTailwindCacheDir); empty means the user-level default.</summary>
        public string CacheDir { get; set; } = "";

        /// <summary>Never touch the network (LuxOffline).</summary>
        public bool Offline { get; set; }

        /// <summary>The binary to run.</summary>
        [Output] public string ExecutablePath { get; set; } = "";

        public override bool Execute()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(Executable))
                {
                    if (!File.Exists(Executable))
                    {
                        throw new BuildToolException($"LuxTailwindExecutable points at \"{Executable}\", which does not exist.");
                    }
                    ExecutablePath = Executable;
                    return true;
                }
                var cacheDir = string.IsNullOrWhiteSpace(CacheDir) ? TailwindCli.DefaultCacheDir() : CacheDir;
                if (cacheDir == null)
                {
                    throw new BuildToolException("The user profile folder is unknown, so there is no default cache for the Tailwind CLI. Set LuxTailwindCacheDir to a folder, or LuxTailwindExecutable to a Tailwind binary.");
                }
                // The binary is a hundred megabytes: a slow link needs more than the usual minute.
                ExecutablePath = TailwindCli.Resolve(new HttpNetwork(TimeSpan.FromMinutes(10)), Version, cacheDir, Offline, TailwindPlatform.Current(), message => Log.LogMessage(MessageImportance.High, message));
                return true;
            }
            catch (BuildToolException e)
            {
                Log.LogError(subcategory: null, errorCode: "BLUXTW001", helpKeyword: null, file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0, message: e.Message);
                return false;
            }
            catch (Exception e) when (!(e is OutOfMemoryException || e is StackOverflowException))
            {
                Log.LogError(subcategory: null, errorCode: "BLUXTW002", helpKeyword: null, file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0, message: "Could not get the Tailwind CLI: " + e.GetType().Name + ": " + e.Message + " Set LuxTailwindExecutable to a Tailwind binary, or LuxTailwind=false to turn the feature off.");
                return false;
            }
        }
    }
}
