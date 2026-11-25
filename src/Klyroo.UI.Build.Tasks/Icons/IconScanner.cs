using System.Text.RegularExpressions;

namespace Klyroo.UI.Build
{
    /// <summary>How sure the scanner is that a string is an icon.</summary>
    internal enum IconDetection
    {
        /// <summary>The string is the value of an icon-typed attribute or property: an icon that cannot be found is reported.</summary>
        Context,
        /// <summary>The string is, on its own, an <c>i-prefix-name</c> name with a known collection (<c>"i-ph-house"</c>): an icon that cannot be found is ignored.</summary>
        Loose,
    }

    internal sealed class IconMention
    {
        public IconMention(IconRef icon, IconNameForm form, IconDetection detection, string file, int line)
        {
            Icon = icon;
            Form = form;
            Detection = detection;
            File = file;
            Line = line;
        }

        public IconRef Icon { get; }
        public IconNameForm Form { get; }
        public IconDetection Detection { get; }
        public string File { get; }
        public int Line { get; }
    }

    /// <summary>
    /// Finds the icon names an app writes as literals in its <c>.razor</c>, <c>.cshtml</c> and <c>.cs</c> files. The rules are
    /// deliberately narrow, because a wrong guess costs a download and, in a Release build, a failure:
    /// <list type="number">
    /// <item><b>Icon-typed attributes and properties</b> (strict): <c>Icon="..."</c>, <c>LeadingIcon="..."</c>, <c>Icon = "..."</c> in an object initializer
    /// and every other name that ends in <c>Icon</c>, when the value is a plain string literal that reads as an icon name.</item>
    /// <item><c>Name="..."</c> on <c>&lt;KlyIcon&gt;</c>.</item>
    /// <item><c>KlyIcons.Named("...")</c>.</item>
    /// <item><c>Icons.Close = "..."</c> (and every other key of <c>KlyrooOptions.Icons</c>), as in <c>o.Icons.Close = "ph:x"</c>.</item>
    /// <item><b>A standalone string</b> that is exactly <c>i-prefix-name</c> with the prefix of a known Iconify collection or
    /// of a <c>KlyrooIconCollection</c> (loose): bundled when found, ignored when not. The <c>prefix:name</c> form is never taken on its own:
    /// many of the collection prefixes (<c>token</c>, <c>memory</c>, <c>map</c>, <c>geo</c>) are also words that appear before a colon in
    /// ordinary strings, so a <c>"memory:cache"</c> in the code would be downloaded. In an icon context the colon form is read, as everywhere.</item>
    /// </list>
    /// A bare name (<c>"book-open"</c>) is only taken from the first four contexts, as a Lucide icon. Not detected: names built at runtime
    /// (<c>$"i-lucide-{x}"</c>), values behind an expression (<c>Icon="@(a ? "x" : "y")"</c>, <c>cond ? "ph:a" : "ph:b"</c> in strict contexts) and
    /// comments are not interpreted (a commented-out name is still bundled). List those names with <c>&lt;KlyrooIcon Include="..." /&gt;</c>.
    /// Names registered in code with <c>Icons.Register("name", ...)</c> are never downloaded.
    /// </summary>
    internal static class IconScanner
    {
        private const string Literal = "\"([^\"\\r\\n\\\\]*)\"";

        // Foo Icon="x" / LeadingIcon="x" / Icon = "x" (not ==, not data-icon).
        private static readonly Regex IconProperty = new Regex(@"(?<![\w-])[A-Z]?\w*Icon\s*=(?!=)\s*" + Literal, RegexOptions.Compiled);
        // <KlyIcon Name="x" ...>, also across lines.
        private static readonly Regex IconName = new Regex(@"<(?:Kly)?Icon\b[^>]*?\sName\s*=\s*" + Literal, RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex Named = new Regex(@"\bKlyIcons\s*\.\s*Named\s*\(\s*(?:name\s*:\s*)?" + Literal, RegexOptions.Compiled);
        private static readonly Regex AppIcon = new Regex(@"\bIcons\s*\.\s*[A-Z]\w*\s*=(?!=)\s*" + Literal, RegexOptions.Compiled);
        private static readonly Regex Registered = new Regex(@"\bIcons\s*\.\s*Register(?:Svg)?\s*\(\s*" + Literal, RegexOptions.Compiled);
        private static readonly Regex Standalone = new Regex("\"(?<v>i-[a-z0-9]+(?:-[a-z0-9]+)+)\"", RegexOptions.Compiled);

        public static List<IconMention> Scan(string file, string text, Func<string, bool>? extraPrefix = null)
        {
            var found = new List<IconMention>();
            var seen = new HashSet<int>();
            var lines = new LineIndex(text);

            void Add(Match m, IconDetection detection, string group = "1")
            {
                var g = m.Groups[group];
                if (!g.Success || !seen.Add(g.Index)) return;
                if (!IconNames.TryParse(g.Value, extraPrefix, out var icon, out var form)) return;
                if (form == IconNameForm.Bare && IsNoise(icon.Name)) return;
                found.Add(new IconMention(icon, form, detection, file, lines.LineOf(g.Index)));
            }

            foreach (Match m in IconProperty.Matches(text)) Add(m, IconDetection.Context);
            foreach (Match m in IconName.Matches(text)) Add(m, IconDetection.Context);
            foreach (Match m in Named.Matches(text)) Add(m, IconDetection.Context);
            foreach (Match m in AppIcon.Matches(text)) Add(m, IconDetection.Context);
            foreach (Match m in Standalone.Matches(text))
            {
                var g = m.Groups["v"];
                if (seen.Contains(g.Index)) continue;
                // Loose: only the i-prefix-name form, and only when its collection is known.
                if (IconNames.TryParse(g.Value, extraPrefix, out var icon, out var form) && form == IconNameForm.IPrefix
                    && (IconNames.IsKnownPrefix(icon.Prefix) || (extraPrefix != null && extraPrefix(icon.Prefix))))
                {
                    seen.Add(g.Index);
                    found.Add(new IconMention(icon, form, IconDetection.Loose, file, lines.LineOf(g.Index)));
                }
            }
            return found;
        }

        /// <summary>Names registered in code with <c>Icons.Register("custom:logo", ...)</c>, which must not be looked up.</summary>
        public static IEnumerable<string> RegisteredNames(string text)
        {
            foreach (Match m in Registered.Matches(text))
            {
                if (IconNames.TryParse(m.Groups[1].Value, out var icon)) yield return icon.Key;
            }
        }

        // "Icon" properties that hold a flag or a number rather than a name.
        private static bool IsNoise(string name) => name == "true" || name == "false" || name == "null" || name == "none" || name.All(char.IsDigit);

        private sealed class LineIndex
        {
            private readonly List<int> _starts = new List<int> { 0 };

            public LineIndex(string text)
            {
                for (var i = 0; i < text.Length; i++)
                {
                    if (text[i] == '\n') _starts.Add(i + 1);
                }
            }

            public int LineOf(int index)
            {
                var i = _starts.BinarySearch(index);
                return (i >= 0 ? i : ~i - 1) + 1;
            }
        }
    }
}
