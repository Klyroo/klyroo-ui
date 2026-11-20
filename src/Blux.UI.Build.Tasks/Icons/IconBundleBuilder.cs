using System.Globalization;
using System.Text;

namespace Blux.UI.Build
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

    /// <summary>A folder of SVG files, the <c>BluxIconCollection</c> item.</summary>
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

        /// <summary>The <c>BluxIcon</c> items: names in any form, always looked up.</summary>
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

        /// <summary>The C# file that registers the icons with Blux.UI at startup.</summary>
        public string Source { get; set; } = "";

        public int IconCount { get; set; }
        public int Downloaded { get; set; }
    }

    /// <summary>
    /// Turns what an app asks for (names found in its sources, <c>BluxIcon</c> items, folders of SVG files) into the icon data it bundles:
    /// the Iconify API answers the collections, a cache keeps every icon so later builds, offline ones included, need no network,
    /// and a C# file registers the result with <c>BluxIconBundle</c> from a module initializer.
    /// </summary>
    internal static class IconBundleBuilder
    {
        private const int MaxUrlLength = 500;
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
                else result.Problems.Add(new IconProblem("\"" + item + "\" in a BluxIcon item is not an icon name. Use prefix:name (ph:house) or i-prefix-name (i-ph-house); a bare name is a Lucide icon.", null, 0, true));
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

            result.IconCount = icons.Count;
            result.Source = Emit(icons.Values.Select(v => (v.Key, v.Value)));
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

        private static void Fetch(string prefix, Dictionary<string, Request> wanted, IconBuildOptions options, INetwork network,
            SortedDictionary<string, KeyValuePair<IconRef, IconBody>> icons, IconBuildResult result)
        {
            var missing = new List<string>();
            var stale = new Dictionary<string, IconBody>(StringComparer.Ordinal);
            var notFound = new HashSet<string>(StringComparer.Ordinal);
            foreach (var name in wanted.Keys.OrderBy(n => n, StringComparer.Ordinal))
            {
                var path = CachePath(options, prefix, name);
                if (File.Exists(path))
                {
                    var fresh = options.Offline || DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < options.CacheMaxAge;
                    var cached = ReadCache(path, out var body);
                    if (cached == CacheState.Icon)
                    {
                        if (fresh) { Add(prefix, name, body, icons); continue; }
                        stale[name] = body;
                    }
                    else if (cached == CacheState.Missing && fresh)
                    {
                        notFound.Add(name);
                        continue;
                    }
                }
                missing.Add(name);
            }

            string? failure = null;
            var collectionMissing = false;
            if (missing.Count > 0)
            {
                if (options.Offline)
                {
                    failure = "Offline build (BluxOffline) and the icon is not in the cache (" + options.CacheDir + ").";
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
                            foreach (var name in chunk)
                            {
                                if (set.TryResolve(name, out var body))
                                {
                                    WriteCache(CachePath(options, prefix, name), body);
                                    Add(prefix, name, body, icons);
                                    stale.Remove(name);
                                }
                                else
                                {
                                    WriteMissing(CachePath(options, prefix, name));
                                    notFound.Add(name);
                                }
                            }
                        }
                        catch (DownloadException e) when (e.StatusCode == 404)
                        {
                            collectionMissing = true;
                            foreach (var name in chunk) notFound.Add(name);
                        }
                        catch (BuildToolException e)
                        {
                            failure = e.Message + " Without network the icons must already be in the cache (" + options.CacheDir + "); set BluxOffline=true to build from the cache only.";
                        }
                        catch (Exception e) when (e is HttpRequestException || e is TaskCanceledException || e is IOException || e is System.Net.WebException)
                        {
                            failure = "Could not reach " + options.Api + ": " + e.Message + ". Without network the icons must already be in the cache (" + options.CacheDir + "); set BluxOffline=true to build from the cache only.";
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
                if (stale.TryGetValue(name, out var old))
                {
                    // The cache is older than BluxIconsCacheDays but the network failed: the cached icon is still right.
                    Add(prefix, name, old, icons);
                    if (failure != null) options.Info("Using the cached icon " + key + ": " + failure);
                    continue;
                }
                string message;
                if (failure != null) message = "The icon " + key + " could not be downloaded: " + failure;
                else if (collectionMissing) message = "The icon " + key + " was not found: \"" + prefix + "\" is not a collection of the Iconify API (" + options.Api + "). Names are written i-prefix-name or prefix:name; custom icons come from a BluxIconCollection folder.";
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

        private enum CacheState { None, Icon, Missing }

        private static CacheState ReadCache(string path, out IconBody body)
        {
            body = default;
            try
            {
                var map = Fonts.MiniJson.Parse(File.ReadAllText(path, Encoding.UTF8)) as Dictionary<string, object?>;
                if (map == null) return CacheState.None;
                if (map.TryGetValue("missing", out var m) && m is bool b && b) return CacheState.Missing;
                var text = Fonts.MiniJson.Str(map, "body");
                var viewBox = Fonts.MiniJson.Str(map, "viewBox");
                if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(viewBox)) return CacheState.None;
                body = new IconBody(text!, viewBox!);
                return CacheState.Icon;
            }
            catch (Exception e) when (e is FormatException || e is IOException || e is IndexOutOfRangeException || e is ArgumentOutOfRangeException)
            {
                return CacheState.None;
            }
        }

        private static void WriteCache(string path, IconBody body) =>
            Store(path, "{\"body\":" + Quote(body.Body, '"') + ",\"viewBox\":" + Quote(body.ViewBox, '"') + "}");

        private static void WriteMissing(string path) => Store(path, "{\"missing\":true}");

        private static void Store(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temp, text, new UTF8Encoding(false));
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }

        /// <summary>The C# file that registers the icons: a module initializer of the app's assembly, so it runs whatever the render mode.</summary>
        internal static string Emit(IEnumerable<(IconRef Icon, IconBody Body)> icons)
        {
            var sb = new StringBuilder();
            sb.Append("// <auto-generated>Blux.UI icon bundle: do not edit. Generated at build time from the icons the app uses.</auto-generated>\n");
            sb.Append("#pragma warning disable\n");
            sb.Append("namespace Blux.UI.Generated\n{\n");
            sb.Append("    internal static class BluxIconBundleInitializer\n    {\n");
            sb.Append("        [global::System.Runtime.CompilerServices.ModuleInitializer]\n");
            sb.Append("        internal static void Register()\n        {\n");
            foreach (var (icon, body) in icons)
            {
                sb.Append("            global::Blux.UI.BluxIconBundle.Add(")
                  .Append(Quote(icon.Prefix, '"')).Append(", ")
                  .Append(Quote(icon.Name, '"')).Append(", ")
                  .Append(Quote(body.Body, '"')).Append(", ")
                  .Append(Quote(body.ViewBox, '"')).Append(");\n");
            }
            sb.Append("        }\n    }\n}\n");
            return sb.ToString();
        }

        /// <summary>A string literal valid both in C# and in JSON for the characters icons contain.</summary>
        private static string Quote(string value, char quote)
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
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append(quote);
            return sb.ToString();
        }
    }
}
