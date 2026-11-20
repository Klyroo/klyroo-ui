using System.Globalization;
using System.Text.RegularExpressions;

namespace Blux.UI.Build.Fonts
{
    internal interface IFontProvider
    {
        string Name { get; }
        IReadOnlyList<FontFace> Resolve(FontSpec spec, CachedDownloader downloader);
    }

    /// <summary>One @font-face block of a Google-style stylesheet.</summary>
    internal sealed class CssFaceBlock
    {
        public string? Subset { get; set; }
        public string Family { get; set; } = "";
        public string Style { get; set; } = "normal";
        public WeightSpec Weight { get; set; }
        public string? UnicodeRange { get; set; }
        public string Url { get; set; } = "";
        public string Extension { get; set; } = "woff2";
    }

    internal static class GoogleCss
    {
        private static readonly Regex Block = new Regex(@"(?:/\*\s*(?<c>[^*]*?)\s*\*/\s*)?@font-face\s*\{(?<b>[^}]*)\}", RegexOptions.Compiled);
        private static readonly Regex Declaration = new Regex(@"(?<n>[a-zA-Z-]+)\s*:\s*(?<v>[^;]+);?", RegexOptions.Compiled);
        private static readonly Regex Source = new Regex(@"url\(\s*['""]?(?<u>[^'"")\s]+)['""]?\s*\)\s*(?:format\(\s*['""]?(?<f>[^'"")\s]+)['""]?\s*\))?", RegexOptions.Compiled);

        /// <summary>
        /// Parses the stylesheet of the Google Fonts API (and of Bunny Fonts, which copies it). The comment before each block
        /// names the subset (<c>/* latin-ext */</c>). When a block lists several formats, woff2 wins.
        /// </summary>
        public static List<CssFaceBlock> Parse(string css)
        {
            var result = new List<CssFaceBlock>();
            foreach (Match m in Block.Matches(css))
            {
                var block = new CssFaceBlock { Subset = m.Groups["c"].Success && m.Groups["c"].Value.Length > 0 ? m.Groups["c"].Value : null };
                foreach (Match d in Declaration.Matches(m.Groups["b"].Value))
                {
                    var value = d.Groups["v"].Value.Trim();
                    switch (d.Groups["n"].Value.ToLowerInvariant())
                    {
                        case "font-family":
                            block.Family = value.Trim('\'', '"');
                            break;
                        case "font-style":
                            block.Style = value;
                            break;
                        case "font-weight":
                            block.Weight = WeightSpec.Parse(value, block.Family);
                            break;
                        case "unicode-range":
                            block.UnicodeRange = value;
                            break;
                        case "src":
                            var best = (Url: "", Format: "");
                            foreach (Match s in Source.Matches(value))
                            {
                                var format = s.Groups["f"].Success ? s.Groups["f"].Value.ToLowerInvariant() : Path.GetExtension(s.Groups["u"].Value).TrimStart('.').ToLowerInvariant();
                                if (best.Url.Length == 0 || (format == "woff2" && best.Format != "woff2"))
                                {
                                    best = (s.Groups["u"].Value, format);
                                }
                            }
                            block.Url = best.Url;
                            block.Extension = best.Format == "woff2" ? "woff2" : best.Format == "woff" ? "woff" : best.Format == "opentype" ? "otf" : "ttf";
                            break;
                    }
                }
                if (block.Url.Length > 0 && block.Family.Length > 0)
                {
                    result.Add(block);
                }
            }
            return result;
        }
    }

    internal static class ProviderChecks
    {
        public static readonly int[] Steps = { 100, 200, 300, 400, 500, 600, 700, 800, 900 };

        /// <summary>The static weights of a request: explicit values as they are, a range as the multiples of 100 inside it.</summary>
        public static List<(WeightSpec Weight, bool FromRange)> ExpandStatic(IReadOnlyList<WeightSpec> weights)
        {
            var result = new List<(WeightSpec, bool)>();
            foreach (var w in weights)
            {
                if (!w.IsRange)
                {
                    result.Add((w, false));
                    continue;
                }
                var any = false;
                foreach (var step in Steps)
                {
                    if (step >= w.Min && step <= w.Max)
                    {
                        result.Add((new WeightSpec(step, step), true));
                        any = true;
                    }
                }
                if (!any) result.Add((new WeightSpec(w.Min, w.Min), false));
            }
            return result.GroupBy(r => r.Item1).Select(g => g.First()).OrderBy(r => r.Item1.Min).ToList();
        }

        public static void ValidateStyles(FontSpec spec)
        {
            foreach (var style in spec.EffectiveStyles)
            {
                if (style != "normal" && style != "italic")
                {
                    throw new FontException($"Font '{spec.Family}': style '{style}' is not supported. Use normal or italic.");
                }
            }
        }

        /// <summary>
        /// Every requested weight, style and subset must come back. A weight that only came from expanding a range may be
        /// missing (the family has no 450); an explicit one may not.
        /// </summary>
        public static List<FontFace> Verify(FontSpec spec, string provider, IReadOnlyList<FontFace> faces, IEnumerable<(WeightSpec Weight, bool Tolerated)> wanted)
        {
            var subsets = spec.Subsets;
            var kept = faces.Where(f => f.Subset == null || subsets.Contains(f.Subset)).ToList();
            foreach (var subset in subsets)
            {
                if (!kept.Any(f => f.Subset == subset) && faces.Any(f => f.Subset != null))
                {
                    var available = string.Join(", ", faces.Where(f => f.Subset != null).Select(f => f.Subset!).Distinct());
                    throw new FontException($"Font '{spec.Family}' has no subset '{subset}' at {provider} (available: {available}).");
                }
            }
            foreach (var style in spec.EffectiveStyles)
            {
                foreach (var (weight, tolerated) in wanted)
                {
                    if (!kept.Any(f => f.Style == style && f.Weight.Covers(weight)) && !tolerated)
                    {
                        throw new FontException($"Font '{spec.Family}' is not available at {provider} with weight {weight.ToString().Replace(' ', '-')} and style {style}.");
                    }
                }
                if (!kept.Any(f => f.Style == style))
                {
                    throw new FontException($"Font '{spec.Family}' is not available at {provider} with style {style}.");
                }
            }
            if (kept.Count == 0)
            {
                throw new FontException($"{provider} returned no font file for '{spec.Family}'.");
            }
            return kept.Where(f => spec.EffectiveStyles.Contains(f.Style)).ToList();
        }
    }

    /// <summary>
    /// Google Fonts, through the CSS2 API (<c>/css2?family=Name:ital,wght@0,400;1,400</c>): a range of weights is answered with
    /// one variable font file per subset.
    /// </summary>
    internal sealed class GoogleProvider : IFontProvider
    {
        private readonly string _endpoint;
        public GoogleProvider(string endpoint = "https://fonts.googleapis.com/css2") { _endpoint = endpoint; }
        public string Name => "google";

        public static string BuildUrl(string endpoint, FontSpec spec)
        {
            ProviderChecks.ValidateStyles(spec);
            var weights = spec.EffectiveWeights.OrderBy(w => w.Min).ToList();
            for (var i = 1; i < weights.Count; i++)
            {
                if (weights[i].Min <= weights[i - 1].Max)
                {
                    throw new FontException($"Font '{spec.Family}': the weights {weights[i - 1].ToString().Replace(' ', '-')} and {weights[i].ToString().Replace(' ', '-')} overlap, which the Google Fonts API rejects.");
                }
            }
            string W(WeightSpec w) => w.IsRange ? $"{w.Min}..{w.Max}" : w.Min.ToString(CultureInfo.InvariantCulture);
            var italic = spec.EffectiveStyles.Contains("italic");
            string axis;
            if (italic)
            {
                var tuples = new List<string>();
                if (spec.EffectiveStyles.Contains("normal")) tuples.AddRange(weights.Select(w => "0," + W(w)));
                tuples.AddRange(weights.Select(w => "1," + W(w)));
                axis = "ital,wght@" + string.Join(";", tuples);
            }
            else
            {
                axis = "wght@" + string.Join(";", weights.Select(W));
            }
            var family = Uri.EscapeDataString(spec.Family).Replace("%20", "+");
            return $"{endpoint}?family={family}:{axis}&display={Uri.EscapeDataString(spec.Display)}";
        }

        public IReadOnlyList<FontFace> Resolve(FontSpec spec, CachedDownloader downloader)
        {
            var css = downloader.GetText(BuildUrl(_endpoint, spec), HttpNetwork.BrowserUserAgent);
            var faces = FromBlocks(spec, GoogleCss.Parse(css));
            return ProviderChecks.Verify(spec, Name, faces, spec.EffectiveWeights.Select(w => (w, false)));
        }

        internal static List<FontFace> FromBlocks(FontSpec spec, List<CssFaceBlock> blocks) =>
            blocks.Select(b => new FontFace
            {
                Family = spec.Family,
                Style = b.Style,
                Weight = b.Weight,
                Subset = b.Subset,
                UnicodeRange = b.UnicodeRange,
                Url = b.Url,
                Extension = b.Extension,
            }).ToList();
    }

    /// <summary>
    /// Bunny Fonts (EU hosted, no logs). It documents the Google CSS v1 API (<c>/css?family=name:400,700,400i</c>) and ignores
    /// ranges in its css2 endpoint, so static weights are requested: a range becomes the multiples of 100 inside it.
    /// </summary>
    internal sealed class BunnyProvider : IFontProvider
    {
        private readonly string _endpoint;
        public BunnyProvider(string endpoint = "https://fonts.bunny.net/css") { _endpoint = endpoint; }
        public string Name => "bunny";

        public static string BuildUrl(string endpoint, FontSpec spec)
        {
            ProviderChecks.ValidateStyles(spec);
            var weights = ProviderChecks.ExpandStatic(spec.EffectiveWeights).Select(w => w.Weight.Min).ToList();
            var tokens = new List<string>();
            foreach (var style in spec.EffectiveStyles)
            {
                tokens.AddRange(weights.Select(w => w.ToString(CultureInfo.InvariantCulture) + (style == "italic" ? "i" : "")));
            }
            return $"{endpoint}?family={FontCss.Slug(spec.Family).ToLowerInvariant()}:{string.Join(",", tokens)}&display={Uri.EscapeDataString(spec.Display)}";
        }

        public IReadOnlyList<FontFace> Resolve(FontSpec spec, CachedDownloader downloader)
        {
            var css = downloader.GetText(BuildUrl(_endpoint, spec), HttpNetwork.BrowserUserAgent);
            var faces = GoogleProvider.FromBlocks(spec, GoogleCss.Parse(css));
            if (faces.Count == 0)
            {
                throw new FontException($"Font '{spec.Family}' was not found at bunny.");
            }
            return ProviderChecks.Verify(spec, Name, faces, ProviderChecks.ExpandStatic(spec.EffectiveWeights).Select(w => (w.Weight, w.FromRange)));
        }
    }

    /// <summary>
    /// Fontsource: <c>https://api.fontsource.org/v1/fonts/{id}</c> lists every weight, style and subset with the URL of each
    /// woff2 file on the jsDelivr CDN. Static files only: a range becomes the weights the family offers inside it.
    /// </summary>
    internal sealed class FontsourceProvider : IFontProvider
    {
        private readonly string _api;
        public FontsourceProvider(string api = "https://api.fontsource.org/v1/fonts") { _api = api; }
        public string Name => "fontsource";

        public static string BuildUrl(string api, FontSpec spec) => $"{api}/{FontCss.Slug(spec.Family).ToLowerInvariant()}";

        public IReadOnlyList<FontFace> Resolve(FontSpec spec, CachedDownloader downloader)
        {
            ProviderChecks.ValidateStyles(spec);
            string json;
            try
            {
                json = downloader.GetText(BuildUrl(_api, spec));
            }
            catch (DownloadException e) when (e.StatusCode == 404)
            {
                throw new FontException($"Font '{spec.Family}' was not found at fontsource.");
            }
            var root = MiniJson.Parse(json) as Dictionary<string, object?>
                ?? throw new FontException($"Unexpected answer from fontsource for '{spec.Family}'.");
            var ranges = MiniJson.Obj(root, "unicodeRange");
            var variants = MiniJson.Obj(root, "variants") ?? throw new FontException($"Fontsource lists no variant of '{spec.Family}'.");
            var wanted = ProviderChecks.ExpandStatic(spec.EffectiveWeights);

            var faces = new List<FontFace>();
            foreach (var (weight, _) in wanted)
            {
                var byStyle = MiniJson.Obj(variants, weight.Min.ToString(CultureInfo.InvariantCulture));
                if (byStyle == null) continue;
                foreach (var style in spec.EffectiveStyles)
                {
                    var bySubset = MiniJson.Obj(byStyle, style);
                    if (bySubset == null) continue;
                    foreach (var subset in spec.Subsets)
                    {
                        var url = MiniJson.Str(MiniJson.Obj(MiniJson.Obj(bySubset, subset), "url"), "woff2");
                        if (url == null) continue;
                        faces.Add(new FontFace
                        {
                            Family = spec.Family,
                            Style = style,
                            Weight = weight,
                            Subset = subset,
                            UnicodeRange = MiniJson.Str(ranges, subset),
                            Url = url,
                            Extension = "woff2",
                        });
                    }
                }
            }
            if (faces.Count == 0)
            {
                throw new FontException($"Fontsource has none of the requested weights, styles and subsets of '{spec.Family}'.");
            }
            return ProviderChecks.Verify(spec, Name, faces, wanted.Select(w => (w.Weight, w.FromRange)));
        }
    }

    /// <summary>
    /// Files of the project. The folder (<c>Src</c>) holds files named after the family with the weight and the style in the
    /// name: <c>Inter-400.woff2</c>, <c>Inter-700-italic.woff2</c>, <c>Inter-BoldItalic.woff2</c>, <c>InterVariable.woff2</c>.
    /// </summary>
    internal sealed class LocalProvider : IFontProvider
    {
        public string Name => "local";

        private static readonly string[] Extensions = { "woff2", "woff", "ttf", "otf" };

        private static readonly (string Name, int Weight)[] Names =
        {
            ("extralight", 200), ("ultralight", 200), ("extrabold", 800), ("ultrabold", 800), ("semibold", 600), ("demibold", 600),
            ("thin", 100), ("light", 300), ("regular", 400), ("normal", 400), ("book", 400), ("medium", 500), ("bold", 700),
            ("heavy", 800), ("black", 900),
        };

        private static string Normalize(string text) => new string(text.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        /// <summary>Weight and style of a file name once the family is removed; null when something else is left over.</summary>
        public static (WeightSpec Weight, string Style)? ParseFileName(string stem, string family)
        {
            var name = Normalize(stem);
            var prefix = Normalize(family);
            if (!name.StartsWith(prefix, StringComparison.Ordinal)) return null;
            var rest = name.Substring(prefix.Length);

            var style = "normal";
            foreach (var word in new[] { "italic", "oblique" })
            {
                if (rest.Contains(word)) { style = "italic"; rest = rest.Replace(word, ""); }
            }
            foreach (var noise in new[] { "static", "webfont", "latin" })
            {
                rest = rest.Replace(noise, "");
            }

            if (rest.Contains("variable"))
            {
                rest = rest.Replace("variable", "");
                return rest.Length == 0 ? (new WeightSpec(100, 900), style) : ((WeightSpec, string)?)null;
            }
            var digits = Regex.Match(rest, @"[1-9]00");
            if (digits.Success)
            {
                var weight = int.Parse(digits.Value, CultureInfo.InvariantCulture);
                rest = rest.Remove(digits.Index, digits.Length);
                return rest.Length == 0 ? (new WeightSpec(weight, weight), style) : ((WeightSpec, string)?)null;
            }
            foreach (var (word, weight) in Names)
            {
                if (rest.Contains(word))
                {
                    rest = rest.Replace(word, "");
                    return rest.Length == 0 ? (new WeightSpec(weight, weight), style) : ((WeightSpec, string)?)null;
                }
            }
            return rest.Length == 0 ? (new WeightSpec(400, 400), style) : ((WeightSpec, string)?)null;
        }

        public IReadOnlyList<FontFace> Resolve(FontSpec spec, CachedDownloader downloader)
        {
            if (spec.Src.Length == 0 || !Directory.Exists(spec.Src))
            {
                throw new FontException($"Font '{spec.Family}': the folder '{spec.Src}' of the local provider does not exist. Set Src to the folder holding the font files.");
            }
            var found = new Dictionary<(string, WeightSpec), FontFace>();
            foreach (var path in Directory.EnumerateFiles(spec.Src, "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
            {
                var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
                if (Array.IndexOf(Extensions, ext) < 0) continue;
                var parsed = ParseFileName(Path.GetFileNameWithoutExtension(path), spec.Family);
                if (parsed == null) continue;
                var (weight, style) = parsed.Value;
                if (spec.Styles != null && !spec.Styles.Contains(style)) continue;
                if (spec.Weights != null && !spec.Weights.Any(w => w.Covers(weight) || weight.Covers(w))) continue;
                var key = (style, weight);
                if (found.TryGetValue(key, out var existing) && Array.IndexOf(Extensions, existing.Extension) <= Array.IndexOf(Extensions, ext)) continue;
                found[key] = new FontFace { Family = spec.Family, Style = style, Weight = weight, Extension = ext, LocalPath = path };
            }
            if (found.Count == 0)
            {
                throw new FontException($"No file of '{spec.Family}' found in '{spec.Src}'. Name the files after the family with the weight and style, for example {FontCss.Slug(spec.Family)}-400.woff2 or {FontCss.Slug(spec.Family)}-700-italic.woff2.");
            }
            return found.Values.OrderBy(f => f.Style, StringComparer.Ordinal).ThenBy(f => f.Weight.Min).ToList();
        }
    }
}
