using System.Globalization;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

namespace Blux.UI.Build.Fonts
{
    /// <summary>A failure to resolve or read a font, with a message meant to be shown to the person building.</summary>
    internal class FontException : BuildToolException
    {
        public FontException(string message) : base(message) { }
        public FontException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>A requested weight: one value (400) or a range (400..700) that a variable font covers.</summary>
    internal readonly struct WeightSpec : IEquatable<WeightSpec>
    {
        public WeightSpec(int min, int max) { Min = min; Max = max; }
        public int Min { get; }
        public int Max { get; }
        public bool IsRange => Max > Min;

        /// <summary>Accepts "400", "400..700" and "400 700" (the form Nuxt uses).</summary>
        public static WeightSpec Parse(string text, string family)
        {
            var parts = text.Split(new[] { "..", " " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length is 1 or 2
                && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var min)
                && int.TryParse(parts[parts.Length - 1], NumberStyles.None, CultureInfo.InvariantCulture, out var max)
                && min >= 1 && max <= 1000 && min <= max)
            {
                return new WeightSpec(min, max);
            }
            throw new FontException($"Font '{family}': '{text}' is not a valid weight. Use a number (400), a range (400..700) or a list separated by ';' (400;700).");
        }

        public bool Covers(WeightSpec other) => Min <= other.Min && Max >= other.Max;
        public override string ToString() => IsRange ? $"{Min} {Max}" : Min.ToString(CultureInfo.InvariantCulture);
        public bool Equals(WeightSpec other) => Min == other.Min && Max == other.Max;
        public override bool Equals(object? obj) => obj is WeightSpec w && Equals(w);
        public override int GetHashCode() => Min * 1009 + Max;
    }

    /// <summary>One <c>BluxFont</c> item of the project, parsed.</summary>
    internal sealed class FontSpec
    {
        public string Family { get; set; } = "";
        /// <summary>"sans", "mono" or "" (no token: only @font-face is generated).</summary>
        public string Role { get; set; } = "";
        /// <summary>Empty: try the providers in priority order.</summary>
        public string Provider { get; set; } = "";
        /// <summary>Null: the default (400..700 for remote providers, every file for local).</summary>
        public List<WeightSpec>? Weights { get; set; }
        /// <summary>Null: normal for remote providers, every file for local.</summary>
        public List<string>? Styles { get; set; }
        public List<string> Subsets { get; set; } = new List<string> { "latin" };
        public string Display { get; set; } = "swap";
        public bool? Preload { get; set; }
        /// <summary>Folder of the files of the local provider (absolute).</summary>
        public string Src { get; set; } = "";
        /// <summary>"sans", "serif" or "mono": the system font the fallback face imitates. Empty: from the role.</summary>
        public string Category { get; set; } = "";

        public bool PreloadEnabled => Preload ?? Role.Length > 0;
        public string EffectiveCategory => Category.Length > 0 ? Category : Role == "mono" ? "mono" : "sans";

        public IReadOnlyList<WeightSpec> EffectiveWeights => Weights ?? new List<WeightSpec> { new WeightSpec(400, 700) };
        public IReadOnlyList<string> EffectiveStyles => Styles ?? new List<string> { "normal" };

        /// <summary>Everything that changes the generated files, for the up-to-date check.</summary>
        public string Signature() =>
            string.Join("|", Family, Role, Provider, Weights == null ? "-" : string.Join(",", Weights), Styles == null ? "-" : string.Join(",", Styles),
                string.Join(",", Subsets), Display, Preload?.ToString() ?? "-", Src, Category);
    }

    /// <summary>One downloadable or local font file with the CSS descriptors of its @font-face.</summary>
    internal sealed class FontFace
    {
        public string Family { get; set; } = "";
        public string Style { get; set; } = "normal";
        public WeightSpec Weight { get; set; }
        public string? Subset { get; set; }
        public string? UnicodeRange { get; set; }
        /// <summary>woff2, woff, ttf or otf.</summary>
        public string Extension { get; set; } = "woff2";
        /// <summary>Remote URL; empty for a local file.</summary>
        public string Url { get; set; } = "";
        public string LocalPath { get; set; } = "";

        // Filled by the resolver.
        public byte[]? Bytes { get; set; }
        public string FileName { get; set; } = "";
    }

    /// <summary>What a provider answers is data from outside: it is checked before it is cached or written into the stylesheet.</summary>
    internal static class FontChecks
    {
        // One value of unicode-range: U+hex, U+hex-hex or U+hex with trailing ? wildcards (CSS Fonts 4, 6 digits at most).
        private static readonly System.Text.RegularExpressions.Regex RangeToken = new System.Text.RegularExpressions.Regex(
            @"^[Uu]\+(?:[0-9A-Fa-f]{1,6}(?:-[0-9A-Fa-f]{1,6})?|(?=[0-9A-Fa-f?]{1,6}$)[0-9A-Fa-f]*\?+)$", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>A comma separated list of unicode-range values and nothing else (nothing that could end the declaration).</summary>
        public static bool IsValidUnicodeRange(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            foreach (var token in value!.Split(','))
            {
                var t = token.Trim();
                if (!RangeToken.IsMatch(t)) return false;
            }
            return true;
        }

        /// <summary>wOF2, wOFF, TrueType (0x00010000), OpenType/CFF (OTTO) or Apple TrueType (true).</summary>
        public static bool HasFontSignature(byte[] data)
        {
            if (data.Length < 12) return false;
            var tag = System.Text.Encoding.ASCII.GetString(data, 0, 4);
            return tag == "wOF2" || tag == "wOFF" || tag == "OTTO" || tag == "true" || (data[0] == 0 && data[1] == 1 && data[2] == 0 && data[3] == 0);
        }

        public static void RequireFont(byte[] data, string what)
        {
            if (!HasFontSignature(data))
            {
                var head = BitConverter.ToString(data, 0, Math.Min(4, data.Length));
                throw new FontException($"{what} is not a font file (it starts with {head}; expected wOF2, wOFF, TrueType or OpenType). A provider or a proxy probably answered with an error page.");
            }
        }

        /// <summary>A file URL from a provider answer: absolute and https, nothing else is downloaded.</summary>
        public static void RequireHttps(string url, string family, string provider)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                throw new FontException($"{provider} answered a font URL that is not absolute for '{family}': '{url}'. Only absolute https URLs are downloaded.");
            }
            if (uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new FontException($"{provider} answered a font URL that is not https for '{family}': '{url}'. Only https URLs are downloaded.");
            }
        }
    }

    internal static class FontCss
    {
        public static string Format(string extension) => extension switch
        {
            "woff2" => "woff2",
            "woff" => "woff",
            "otf" => "opentype",
            _ => "truetype",
        };

        public static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        public static string Slug(string family)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var c in family)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
                else if (sb.Length > 0 && sb[sb.Length - 1] != '-') sb.Append('-');
            }
            return sb.ToString().Trim('-');
        }
    }
}
