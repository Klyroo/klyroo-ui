using System.Globalization;
using System.Text;

namespace Lux.UI.Build
{
    internal sealed class IconSourceFile
    {
        public IconSourceFile(string path, string text)
        {
            Path = path;
            Text = text;
        }

        public string Path { get; }
        public string Text { get; }
    }

    /// <summary>A folder of SVG files, the <c>LuxIconCollection</c> item.</summary>
    internal sealed class IconFolder
    {
        public IconFolder(string directory, string prefix)
        {
            Directory = directory;
            Prefix = prefix;
        }

        public string Directory { get; }
        public string Prefix { get; }
    }

    internal sealed class IconBuildOptions
    {
        /// <summary>Source files to scan for literal icon names (empty: no scanning).</summary>
        public List<IconSourceFile> Sources { get; set; } = new List<IconSourceFile>();

        /// <summary>The <c>LuxIcon</c> items: names in any form, always looked up.</summary>
        public List<string> Explicit { get; set; } = new List<string>();

        public List<IconFolder> Folders { get; set; } = new List<IconFolder>();

        public string CacheDir { get; set; } = "";

        /// <summary>Base address of the Iconify API.</summary>
        public string Api { get; set; } = "https://api.iconify.design";

        public TimeSpan CacheMaxAge { get; set; } = TimeSpan.FromDays(30);

        /// <summary>Never touch the network: every icon must already be in the cache.</summary>
        public bool Offline { get; set; }

        public Action<string> Info { get; set; } = _ => { };
    }

    internal sealed class IconProblem
    {
        public IconProblem(string message, string? file, int line, bool fatal)
        {
            Message = message;
            File = file;
            Line = line;
            Fatal = fatal;
        }

        public string Message { get; }
        public string? File { get; }
        public int Line { get; }

        /// <summary>A warning in Debug and an error in Release; the others are only logged.</summary>
        public bool Fatal { get; }
    }

    internal sealed class IconBuildResult
    {
        public List<IconProblem> Problems { get; } = new List<IconProblem>();

        /// <summary>The C# file that registers the icons with Lux.UI at startup.</summary>
        public string Source { get; set; } = "";

        public int IconCount { get; set; }
        public int Downloaded { get; set; }

        /// <summary>Set when the cache could not be written (read-only folder, full disk): the build goes on, the icons are in the bundle, only the next build cannot reuse them.</summary>
        public string? CacheWarning { get; set; }
    }

    /// <summary>
    /// Turns what an app asks for (names found in its sources, <c>LuxIcon</c> items, folders of SVG files) into the icon data it bundles:
    /// the Iconify API answers the collections, a cache keeps every icon so later builds, offline ones included, need no network,
    /// and a C# file registers the result with <c>LuxIconBundle</c> from a module initializer.
    /// </summary>
    internal static class IconBundleBuilder
    {
        private const int MaxUrlLength = 500;

        /// <summary>
        /// Format of the files of the cache. They hold what the API answered for an icon (resolved, never sanitized: that happens when the
        /// bundle is generated), so a fix of the sanitizer reaches cached icons at once. A file of an older format is asked again when the
        /// network is there, and used (and sanitized by the current code) when it is not.
        /// </summary>
        internal const int CacheFormat = 2;
        private static readonly HashSet<string> Builtin = new HashSet<string>(IconCollections.BuiltinLucide, StringComparer.Ordinal);

        private sealed class Request
        {
            public bool Fatal;
            public string? File;
            public int Line;
        }

        public static IconBuildResult Run(IconBuildOptions options, INetwork network)
        {
            var result = new IconBuildResult();
            var icons = new SortedDictionary<string, KeyValuePair<IconRef, IconBody>>(StringComparer.Ordinal);

            // Local collections first: their prefixes are known to the scanner (the i- form and standalone strings).
            var localPrefixes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var folder in options.Folders)
            {
                if (!IconNames.IsValidPart(folder.Prefix))
                {
                    result.Problems.Add(new IconProblem("The prefix \"" + folder.Prefix + "\" of the icon collection " + folder.Directory + " must be lowercase letters and digits separated by dashes (custom, my-brand).", null, 0, true));
                    continue;
                }
                localPrefixes.Add(folder.Prefix);
                LoadFolder(folder, icons, result);
            }
            Func<string, bool> isLocal = p => localPrefixes.Contains(p);

            var registeredInCode = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in options.Sources)
            {
                foreach (var name in IconScanner.RegisteredNames(source.Text)) registeredInCode.Add(name);
            }

            var requests = new Dictionary<IconRef, Request>();
            void Want(IconRef icon, bool fatal, string? file, int line)
            {
                if (registeredInCode.Contains(icon.Key)) return;
                if (icon.Prefix == IconNames.DefaultPrefix && Builtin.Contains(icon.Name)) return;
                if (requests.TryGetValue(icon, out var existing))
                {
                    if (fatal && !existing.Fatal)
                    {
                        existing.Fatal = true;
                        existing.File = file;
                        existing.Line = line;
                    }
                    return;
                }
                requests[icon] = new Request { Fatal = fatal, File = file, Line = line };
            }

            foreach (var item in options.Explicit)
            {
                if (IconNames.TryParse(item, isLocal, out var icon, out _)) Want(icon, true, null, 0);
                else result.Problems.Add(new IconProblem("\"" + item + "\" in a LuxIcon item is not an icon name. Use prefix:name (ph:house) or i-prefix-name (i-ph-house); a bare name is a Lucide icon.", null, 0, true));
            }
            foreach (var source in options.Sources)
            {
                foreach (var mention in IconScanner.Scan(source.Path, source.Text, isLocal))
                {
                    var known = IconNames.IsKnownPrefix(mention.Icon.Prefix) || localPrefixes.Contains(mention.Icon.Prefix);
                    var fatal = mention.Detection == IconDetection.Context && mention.Form != IconNameForm.Bare && known;
                    Want(mention.Icon, fatal, mention.File, mention.Line);
                }
            }

            // Icons of local collections come from the folder, not from the API: a name the folder lacks is reported here.
            foreach (var pair in requests.Where(r => localPrefixes.Contains(r.Key.Prefix)).ToList())
            {
                requests.Remove(pair.Key);
                if (!icons.ContainsKey(pair.Key.Key) && pair.Value.Fatal)
                {
                    result.Problems.Add(new IconProblem("The icon " + pair.Key.Key + " is not in the folder of the collection \"" + pair.Key.Prefix + "\".", pair.Value.File, pair.Value.Line, true));
                }
            }

            foreach (var group in requests.GroupBy(r => r.Key.Prefix).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                Fetch(group.Key, group.ToDictionary(g => g.Key.Name, g => g.Value, StringComparer.Ordinal), options, network, icons, result);
            }

            var fatalKeys = new HashSet<string>(requests.Where(r => r.Value.Fatal).Select(r => r.Key.Key), StringComparer.Ordinal);
            result.Source = Emit(icons.Values.Select(v => (v.Key, v.Value)), result.Problems, fatalKeys, out var emitted);
            result.IconCount = emitted;
            return result;
        }

        private static void LoadFolder(IconFolder folder, SortedDictionary<string, KeyValuePair<IconRef, IconBody>> icons, IconBuildResult result)
        {
            if (!Directory.Exists(folder.Directory))
            {
                result.Problems.Add(new IconProblem("The folder " + folder.Directory + " of the icon collection \"" + folder.Prefix + "\" does not exist.", null, 0, true));
                return;
            }
            var root = Path.GetFullPath(folder.Directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            foreach (var path in Directory.GetFiles(root, "*.svg", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
            {
                var relative = path.Substring(root.Length + 1);
                var name = FileName(relative);
                if (!IconNames.IsValidPart(name))
                {
                    result.Problems.Add(new IconProblem("The file name " + relative + " does not make an icon name (lowercase letters, digits and dashes).", path, 0, true));
                    continue;
                }
                try
                {
                    var body = SvgImport.Parse(File.ReadAllText(path));
                    var icon = new IconRef(folder.Prefix, name);
                    icons[icon.Key] = new KeyValuePair<IconRef, IconBody>(icon, body);
                }
                catch (FormatException e)
                {
                    result.Problems.Add(new IconProblem("The icon " + relative + " cannot be used: " + e.Message, path, 0, true));
                }
            }
        }

        /// <summary>Icon names are lowercase with dashes: "Arrow Left.svg" is "arrow-left", and a file in a sub folder is prefixed by it ("brand/logo.svg" is "brand-logo").</summary>
        internal static string FileName(string relativePath)
        {
            var withoutExtension = relativePath.Substring(0, relativePath.Length - 4);
            var sb = new StringBuilder();
            foreach (var c in withoutExtension)
            {
                if (c == '/' || c == '\\' || c == ' ' || c == '_' || c == '.') sb.Append('-');
                else sb.Append(char.ToLowerInvariant(c));
            }
            var name = sb.ToString();
            while (name.Contains("--")) name = name.Replace("--", "-");
            return name.Trim('-');
        }

        private static string CachePath(IconBuildOptions options, string prefix, string name) =>
            Path.Combine(options.CacheDir, prefix, name + ".json");

        // A collection the API does not have is remembered too, in a file no icon name can collide with (names are lowercase letters, digits and dashes).
        private static string CollectionPath(IconBuildOptions options, string prefix) =>
            Path.Combine(options.CacheDir, prefix, "_collection.json");

        private static void Fetch(string prefix, Dictionary<string, Request> wanted, IconBuildOptions options, INetwork network,
            SortedDictionary<string, KeyValuePair<IconRef, IconBody>> icons, IconBuildResult result)
        {
            var missing = new List<string>();
            var stale = new Dictionary<string, IconBody>(StringComparer.Ordinal);
            var notFound = new HashSet<string>(StringComparer.Ordinal);
            var incomplete = new HashSet<string>(StringComparer.Ordinal);
            var collectionMissing = false;

            var collection = ReadCache(CollectionPath(options, prefix), out _, out var collectionVersion, out var collectionAge);
            if (collection == CacheState.CollectionMissing && collectionVersion == CacheFormat && (options.Offline || collectionAge < options.CacheMaxAge))
            {
                collectionMissing = true;
                foreach (var name in wanted.Keys) notFound.Add(name);
            }
            else
            {
                foreach (var name in wanted.Keys.OrderBy(n => n, StringComparer.Ordinal))
                {
                    var state = ReadCache(CachePath(options, prefix, name), out var body, out var version, out var age);
                    if (state == CacheState.Icon)
                    {
                        // Offline there is nothing better to do with an old file than use it (the sanitizer runs when the bundle is generated).
                        if (options.Offline || (version == CacheFormat && age < options.CacheMaxAge)) { Add(prefix, name, body, icons); continue; }
                        stale[name] = body;
                    }
                    else if (state == CacheState.Missing && version == CacheFormat && (options.Offline || age < options.CacheMaxAge))
                    {
                        notFound.Add(name);
                        continue;
                    }
                    missing.Add(name);
                }
            }

            string? failure = null;
            if (missing.Count > 0 && !collectionMissing)
            {
                if (options.Offline)
                {
                    failure = "Offline build (LuxOffline) and the icon is not in the cache (" + options.CacheDir + ").";
                }
                else
                {
                    foreach (var chunk in Chunks(prefix, missing, options.Api))
                    {
                        var url = Url(options.Api, prefix, chunk);
                        try
                        {
                            var set = IconifySet.Parse(network.GetText(url, null));
                            result.Downloaded += chunk.Count;
                            var reported = new HashSet<string>(set.NotFound, StringComparer.Ordinal);
                            foreach (var name in chunk)
                            {
                                if (set.TryResolve(name, out var body))
                                {
                                    // In the bundle first: a cache that cannot be written never costs an icon.
                                    Add(prefix, name, body, icons);
                                    stale.Remove(name);
                                    TryStore(options, CachePath(options, prefix, name), WriteIcon(body), result);
                                }
                                else if (reported.Contains(name))
                                {
                                    // Only what the API says it does not have is remembered as missing.
                                    TryStore(options, CachePath(options, prefix, name), "{\"v\":" + CacheFormat + ",\"missing\":true}", result);
                                    notFound.Add(name);
                                }
                                else
                                {
                                    // Neither an icon nor in not_found: an incomplete answer, which says nothing about the icon.
                                    incomplete.Add(name);
                                }
                            }
                        }
                        catch (CollectionNotFoundException)
                        {
                            collectionMissing = true;
                        }
                        catch (DownloadException e) when (e.StatusCode == 404)
                        {
                            collectionMissing = true;
                        }
                        catch (BuildToolException e)
                        {
                            failure = e.Message + " Without network the icons must already be in the cache (" + options.CacheDir + "); set LuxOffline=true to build from the cache only.";
                        }
                        catch (Exception e) when (e is HttpRequestException || e is TaskCanceledException || e is IOException || e is System.Net.WebException)
                        {
                            failure = "Could not reach " + options.Api + ": " + e.Message + ". Without network the icons must already be in the cache (" + options.CacheDir + "); set LuxOffline=true to build from the cache only.";
                        }
                        if (collectionMissing)
                        {
                            TryStore(options, CollectionPath(options, prefix), "{\"v\":" + CacheFormat + ",\"collectionMissing\":true}", result);
                            foreach (var name in missing) notFound.Add(name);
                            break;
                        }
                        if (failure != null) break;
                    }
                }
            }

            foreach (var name in wanted.Keys.OrderBy(n => n, StringComparer.Ordinal))
            {
                var key = prefix + ":" + name;
                if (icons.ContainsKey(key)) continue;
                var request = wanted[name];
                if (stale.TryGetValue(name, out var old) && !notFound.Contains(name))
                {
                    // The cache is older than LuxIconsCacheDays (or written by an older Lux.UI) but the network did not answer: the cached icon is still right.
                    Add(prefix, name, old, icons);
                    if (failure != null) options.Info("Using the cached icon " + key + ": " + failure);
                    else if (incomplete.Contains(name)) options.Info("Using the cached icon " + key + ": the answer of " + options.Api + " did not include it.");
                    continue;
                }
                string message;
                if (collectionMissing) message = "The icon " + key + " was not found: \"" + prefix + "\" is not a collection of the Iconify API (" + options.Api + "), so a name is probably misspelled. Names are written i-prefix-name or prefix:name (https://icones.js.org lists the collections); custom icons come from a LuxIconCollection folder.";
                else if (failure != null) message = "The icon " + key + " could not be downloaded: " + failure;
                else if (incomplete.Contains(name)) message = "The icon " + key + " could not be downloaded: the answer of " + options.Api + " did not include it and did not list it as not found (an incomplete answer). It is not remembered; the next build asks again.";
                else message = "The icon " + key + " does not exist in the \"" + prefix + "\" collection of Iconify (https://icones.js.org/collection/" + prefix + " lists its icons).";
                result.Problems.Add(new IconProblem(message, request.File, request.Line, request.Fatal));
            }
        }

        private static void Add(string prefix, string name, IconBody body, SortedDictionary<string, KeyValuePair<IconRef, IconBody>> icons)
        {
            var icon = new IconRef(prefix, name);
            icons[icon.Key] = new KeyValuePair<IconRef, IconBody>(icon, body);
        }

        private static string Url(string api, string prefix, List<string> names) =>
            api.TrimEnd('/') + "/" + prefix + ".json?icons=" + string.Join(",", names);

        /// <summary>The API documentation asks for URLs of at most 500 characters: longer lists are split.</summary>
        private static IEnumerable<List<string>> Chunks(string prefix, List<string> names, string api)
        {
            var current = new List<string>();
            var length = Url(api, prefix, current).Length;
            foreach (var name in names)
            {
                if (current.Count > 0 && length + name.Length + 1 > MaxUrlLength)
                {
                    yield return current;
                    current = new List<string>();
                    length = Url(api, prefix, current).Length;
                }
                current.Add(name);
                length += name.Length + 1;
            }
            if (current.Count > 0) yield return current;
        }

        private enum CacheState { None, Icon, Missing, CollectionMissing }

        /// <summary>Reads a cache file; a file that is gone, unreadable or not in a form we wrote is no file. <paramref name="version"/> is 1 for the files of the first format (no version field).</summary>
        private static CacheState ReadCache(string path, out IconBody body, out int version, out TimeSpan age)
        {
            body = default;
            version = 0;
            age = TimeSpan.MaxValue;
            try
            {
                var bytes = CachedDownloader.TryReadBytes(path);
                if (bytes == null) return CacheState.None;
                age = DateTime.UtcNow - File.GetLastWriteTimeUtc(path);
                if (!(Fonts.MiniJson.Parse(Encoding.UTF8.GetString(bytes)) is Dictionary<string, object?> map)) return CacheState.None;
                version = map.TryGetValue("v", out var v) && v is double d ? (int)d : 1;
                if (map.TryGetValue("collectionMissing", out var c) && c is bool cb && cb) return CacheState.CollectionMissing;
                if (map.TryGetValue("missing", out var m) && m is bool b && b) return CacheState.Missing;
                var text = Fonts.MiniJson.Str(map, "body");
                var viewBox = Fonts.MiniJson.Str(map, "viewBox");
                if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(viewBox)) return CacheState.None;
                body = new IconBody(text!, viewBox!);
                return CacheState.Icon;
            }
            catch (Exception e) when (e is FormatException || e is IOException || e is UnauthorizedAccessException || e is IndexOutOfRangeException || e is ArgumentOutOfRangeException)
            {
                return CacheState.None;
            }
        }

        private static string WriteIcon(IconBody body) =>
            "{\"v\":" + CacheFormat + ",\"body\":" + Quote(body.Body, '"') + ",\"viewBox\":" + Quote(body.ViewBox, '"') + "}";

        /// <summary>
        /// The cache is an optimization, so a write that fails (a read-only folder, a full disk, a folder another build holds) is one warning for
        /// the build and nothing else: the icon is already in the bundle. The write is atomic and survives another project writing the same file.
        /// </summary>
        private static void TryStore(IconBuildOptions options, string path, string text, IconBuildResult result)
        {
            try
            {
                CachedDownloader.Store(path, new UTF8Encoding(false).GetBytes(text), overwrite: true);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException || e is System.Security.SecurityException)
            {
                if (result.CacheWarning == null)
                {
                    result.CacheWarning = "The icon cache " + options.CacheDir + " cannot be written (" + e.Message + "): the icons are bundled, but the next build asks the API again. Set LuxIconsCacheDir to a writable folder.";
                }
            }
        }

        /// <summary>
        /// The C# file that registers the icons: a module initializer of the app's assembly, announced by an assembly attribute so Lux.UI finds and
        /// runs it whatever assembly renders first (a Blazor Web App renders a component of the client project on the server before any client code ran).
        /// Every body is sanitized here (see <see cref="SvgSanitizer"/>), whatever its source: the API, the cache, a folder of files.
        /// A body that cannot be read as SVG is left out and reported.
        /// </summary>
        internal static string Emit(IEnumerable<(IconRef Icon, IconBody Body)> icons, List<IconProblem>? problems, ISet<string>? fatalKeys, out int emitted)
        {
            emitted = 0;
            var sb = new StringBuilder();
            sb.Append("// <auto-generated>Lux.UI icon bundle: do not edit. Generated at build time from the icons the app uses.</auto-generated>\n");
            sb.Append("#pragma warning disable\n");
            sb.Append("[assembly: global::Lux.UI.LuxIconRegistrar(typeof(global::Lux.UI.Generated.LuxIconBundleInitializer))]\n");
            sb.Append("namespace Lux.UI.Generated\n{\n");
            sb.Append("    internal static class LuxIconBundleInitializer\n    {\n");
            sb.Append("        [global::System.Runtime.CompilerServices.ModuleInitializer]\n");
            sb.Append("        internal static void Register()\n        {\n");
            foreach (var (icon, body) in icons)
            {
                string clean;
                try
                {
                    clean = SvgSanitizer.Sanitize(body.Body);
                }
                catch (FormatException e)
                {
                    problems?.Add(new IconProblem("The icon " + icon.Key + " is not bundled: its markup cannot be read as SVG (" + e.Message + ").", null, 0, fatalKeys != null && fatalKeys.Contains(icon.Key)));
                    continue;
                }
                if (clean.Length == 0)
                {
                    problems?.Add(new IconProblem("The icon " + icon.Key + " is not bundled: nothing is left of it once the scripts, images, links and styles that cannot be inlined are removed.", null, 0, fatalKeys != null && fatalKeys.Contains(icon.Key)));
                    continue;
                }
                emitted++;
                sb.Append("            global::Lux.UI.LuxIconBundle.Add(")
                  .Append(Quote(icon.Prefix, '"')).Append(", ")
                  .Append(Quote(icon.Name, '"')).Append(", ")
                  .Append(Quote(clean, '"')).Append(", ")
                  .Append(Quote(body.ViewBox, '"')).Append(");\n");
            }
            sb.Append("        }\n    }\n}\n");
            return sb.ToString();
        }

        /// <summary>A string literal valid both in C# and in JSON for the characters icons contain, line terminators of every kind escaped (the C# compiler ends a line at U+0085, U+2028 and U+2029 too).</summary>
        internal static string Quote(string value, char quote)
        {
            var sb = new StringBuilder(value.Length + 2);
            sb.Append(quote);
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ' || c == '\u0085' || c == '\u2028' || c == '\u2029') sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append(quote);
            return sb.ToString();
        }
    }
}
