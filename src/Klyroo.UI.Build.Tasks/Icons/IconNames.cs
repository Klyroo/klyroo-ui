// Shared by the build task (namespace Klyroo.UI.Build) and by the library (Klyroo.UI.Internal), which compiles this file as a link,
// so both read an icon name the same way.
#if KLYROO_TASKS
namespace Klyroo.UI.Build
#else
namespace Klyroo.UI.Internal
#endif
{
    /// <summary>An icon of an Iconify collection: <c>lucide:chevron-down</c>.</summary>
    internal readonly struct IconRef : IEquatable<IconRef>
    {
        public IconRef(string prefix, string name)
        {
            Prefix = prefix;
            Name = name;
        }

        public string Prefix { get; }
        public string Name { get; }

        /// <summary>The unambiguous form, <c>prefix:name</c>.</summary>
        public string Key => Prefix + ":" + Name;

        public bool Equals(IconRef other) => Prefix == other.Prefix && Name == other.Name;
        public override bool Equals(object? obj) => obj is IconRef other && Equals(other);
        public override int GetHashCode() => (Prefix.GetHashCode() * 397) ^ Name.GetHashCode();
        public override string ToString() => Key;
    }

    /// <summary>How a name was written.</summary>
    internal enum IconNameForm
    {
        /// <summary><c>ph:house</c>: the unambiguous form.</summary>
        Colon,
        /// <summary><c>i-ph-house</c>: the prefix is found by longest match against the known collections.</summary>
        IPrefix,
        /// <summary><c>house</c>: a Lucide icon.</summary>
        Bare,
    }

    /// <summary>
    /// Icon names accepted by Klyroo: <c>i-{collection}-{name}</c> (<c>i-lucide-lightbulb</c>), <c>{collection}:{name}</c>
    /// (<c>lucide:lightbulb</c>) and, for compatibility with the names Klyroo.UI always accepted, a bare Lucide name (<c>lightbulb</c>).
    /// A collection prefix may contain dashes (<c>simple-icons</c>), so the <c>i-</c> form takes the longest known prefix that fits:
    /// <c>i-mdi-light-home</c> is <c>mdi-light:home</c>. The <c>prefix:name</c> form never needs a list.
    /// </summary>
    internal static class IconNames
    {
        public const string DefaultPrefix = "lucide";

        private static readonly HashSet<string> Known = new HashSet<string>(IconCollections.Prefixes, StringComparer.Ordinal);

        public static bool IsKnownPrefix(string prefix) => Known.Contains(prefix);

        /// <summary>A prefix or a name: lowercase letters and digits in groups separated by single dashes.</summary>
        public static bool IsValidPart(string? value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            var previousDash = true;
            foreach (var c in value!)
            {
                if (c == '-')
                {
                    if (previousDash) return false;
                    previousDash = true;
                }
                else if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    previousDash = false;
                }
                else
                {
                    return false;
                }
            }
            return !previousDash;
        }

        public static bool TryParse(string? value, out IconRef icon) => TryParse(value, null, out icon, out _);

        /// <summary>Parses a name; <paramref name="extraPrefixes"/> says which prefixes besides the Iconify collections (custom ones) the <c>i-</c> form may match.</summary>
        public static bool TryParse(string? value, Func<string, bool>? extraPrefixes, out IconRef icon, out IconNameForm form)
        {
            icon = default;
            form = IconNameForm.Bare;
            if (value == null) return false;
            value = value.Trim();
            if (value.Length == 0) return false;

            var colon = value.IndexOf(':');
            if (colon >= 0)
            {
                var prefix = value.Substring(0, colon);
                var name = value.Substring(colon + 1);
                if (!IsValidPart(prefix) || !IsValidPart(name)) return false;
                icon = new IconRef(prefix, name);
                form = IconNameForm.Colon;
                return true;
            }

            if (!IsValidPart(value)) return false;

            if (value.StartsWith("i-", StringComparison.Ordinal))
            {
                var rest = value.Substring(2);
                // Longest prefix first: every dash is a candidate end of the prefix, scanned from the right.
                for (var i = rest.LastIndexOf('-'); i > 0; i = rest.LastIndexOf('-', i - 1))
                {
                    var prefix = rest.Substring(0, i);
                    if (Known.Contains(prefix) || (extraPrefixes != null && extraPrefixes(prefix)))
                    {
                        icon = new IconRef(prefix, rest.Substring(i + 1));
                        form = IconNameForm.IPrefix;
                        return true;
                    }
                    if (i == 1) break;
                }
                // No collection starts the name: "i-cursor" is a Lucide icon whose name happens to begin with "i-".
            }

            icon = new IconRef(DefaultPrefix, value);
            form = IconNameForm.Bare;
            return true;
        }
    }
}
