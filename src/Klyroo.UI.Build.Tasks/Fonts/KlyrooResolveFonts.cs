using System.Collections;
using Klyroo.UI.Build.Fonts;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Klyroo.UI.Build
{
    /// <summary>
    /// Resolves the <c>KlyrooFont</c> items of a project: asks the providers, downloads the files into a cache, and writes the
    /// self-hosted files, <c>klyroo.fonts.css</c> and the manifest into <see cref="OutputDir"/>. See the Fonts guide.
    /// </summary>
    public sealed class KlyrooResolveFonts : Microsoft.Build.Utilities.Task
    {
        /// <summary>The KlyrooFont items: identity = family, metadata = options.</summary>
        [Required] public ITaskItem[] Fonts { get; set; } = Array.Empty<ITaskItem>();

        /// <summary>Folder the files are generated into (absolute).</summary>
        [Required] public string OutputDir { get; set; } = "";

        /// <summary>Folder of the cache of provider answers and downloaded files.</summary>
        [Required] public string CacheDir { get; set; } = "";

        /// <summary>Relative paths in Src are resolved against it.</summary>
        public string ProjectDirectory { get; set; } = "";

        /// <summary>Providers to try, in order, for a font without a Provider (separated by ';').</summary>
        public string ProviderPriority { get; set; } = "google;bunny;fontsource";

        /// <summary>A font that cannot be resolved is an error (Release) instead of a warning (Debug).</summary>
        public bool FailOnError { get; set; }

        /// <summary>Never touch the network (KlyrooOffline): every font must already be in the cache.</summary>
        public bool Offline { get; set; }

        /// <summary>Route of the files in the site.</summary>
        public string UrlBase { get; set; } = "_klyroo/fonts";

        /// <summary>How long a provider's answer is reused before it is asked again.</summary>
        public int CacheDays { get; set; } = 7;

        /// <summary>C# file that receives the manifest as an assembly attribute, read by KlyFonts.</summary>
        public string ManifestSourceFile { get; set; } = "";

        /// <summary>Every generated file, with the file name inside the output folder in the metadata <c>KlyrooFile</c> (not RelativePath, which the static web assets would take as the route).</summary>
        [Output] public ITaskItem[] GeneratedFiles { get; set; } = Array.Empty<ITaskItem>();

        public override bool Execute()
        {
            var specs = new List<FontSpec>();
            foreach (var item in Fonts)
            {
                try
                {
                    var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (DictionaryEntry entry in item.CloneCustomMetadata())
                    {
                        metadata[(string)entry.Key] = (string?)entry.Value ?? "";
                    }
                    specs.Add(FontBuilder.ParseSpec(item.ItemSpec, metadata, ProjectDirectory.Length > 0 ? ProjectDirectory : Directory.GetCurrentDirectory()));
                }
                catch (FontException e)
                {
                    Log.LogError(subcategory: null, errorCode: "KLYFONT001", helpKeyword: null, file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0, message: e.Message);
                }
            }
            if (Log.HasLoggedErrors) return false;

            var options = new FontBuildOptions
            {
                Specs = specs,
                ProviderPriority = ProviderPriority.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim().ToLowerInvariant()).ToList(),
                CacheDir = CacheDir,
                OutputDir = OutputDir,
                UrlBase = UrlBase.Trim('/'),
                CacheMaxAge = TimeSpan.FromDays(Math.Max(0, CacheDays)),
                Offline = Offline,
                Info = m => Log.LogMessage(MessageImportance.Normal, "Klyroo fonts: " + m),
                Warn = m => Log.LogWarning(subcategory: null, warningCode: "KLYFONT002", helpKeyword: null, file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0, message: m),
            };

            FontBuildResult result;
            try
            {
                result = FontBuilder.Run(options, new HttpNetwork());
            }
            catch (Exception e) when (!(e is OutOfMemoryException))
            {
                // Whatever the cause, it is reported as a diagnostic of the task rather than as an MSB4018 crash.
                var detail = e is BuildToolException || e is IOException || e is UnauthorizedAccessException ? e.Message : e.GetType().Name + ": " + e.Message;
                Log.LogError(subcategory: null, errorCode: "KLYFONT003", helpKeyword: null, file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0, message: "Could not generate the fonts: " + detail);
                return false;
            }

            foreach (var error in result.Errors)
            {
                var message = error + " Fonts are self-hosted: they are downloaded at build time, so a build without network needs them in the cache (" + CacheDir + "). Set KlyrooFonts=false to turn the feature off.";
                if (FailOnError)
                {
                    Log.LogError(subcategory: null, errorCode: "KLYFONT004", helpKeyword: null, file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0, message: message);
                }
                else
                {
                    Log.LogWarning(subcategory: null, warningCode: "KLYFONT004", helpKeyword: null, file: null, lineNumber: 0, columnNumber: 0, endLineNumber: 0, endColumnNumber: 0, message: message + " (an error in a Release build)");
                }
            }

            if (ManifestSourceFile.Length > 0)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ManifestSourceFile)!);
                var source = "// <auto-generated>Klyroo.UI fonts manifest: do not edit.</auto-generated>\n[assembly: global::Klyroo.UI.KlyrooFontsManifest(" + FontBuilder.Json(result.ManifestJson) + ")]\n";
                if (!File.Exists(ManifestSourceFile) || File.ReadAllText(ManifestSourceFile) != source)
                {
                    File.WriteAllText(ManifestSourceFile, source);
                }
            }

            GeneratedFiles = result.Files.Select(name =>
            {
                var item = new TaskItem(Path.Combine(OutputDir, name));
                item.SetMetadata("KlyrooFile", name);
                return (ITaskItem)item;
            }).ToArray();
            return !Log.HasLoggedErrors;
        }
    }
}
