using System.Globalization;
using Lux.UI.Build.Fonts;

namespace Lux.UI.Build
{
    /// <summary>The API answered, with status 200, the number 404: its way to say that a collection does not exist.</summary>
    internal sealed class CollectionNotFoundException : BuildToolException
    {
        public CollectionNotFoundException() : base("The Iconify API does not know this collection (it answered 404).") { }
    }

    /// <summary>
    /// An icon set in the Iconify JSON format (https://iconify.design/docs/types/iconify-json.html), which is what
    /// <c>https://api.iconify.design/{prefix}.json?icons=a,b</c> answers: <c>icons</c> with a <c>body</c> and optional
    /// geometry, <c>aliases</c> that point to a parent icon and may flip or rotate it, a default <c>width</c> and <c>height</c>
    /// (16 when the set gives none) and, for the API, a <c>not_found</c> list.
    /// </summary>
    internal sealed class IconifySet
    {
        public const double DefaultSize = 16;

        private sealed class Entry
        {
            public string Body = "";
            public double? Left, Top, Width, Height;
            public int Rotate;
            public bool HFlip, VFlip;
            public string? Parent;
        }

        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        private double _width = DefaultSize;
        private double _height = DefaultSize;

        public string Prefix { get; private set; } = "";

        /// <summary>The names the API reported it does not have.</summary>
        public List<string> NotFound { get; } = new List<string>();

        public static IconifySet Parse(string json)
        {
            object? root;
            try
            {
                root = MiniJson.Parse(json);
            }
            catch (Exception e) when (e is FormatException || e is IndexOutOfRangeException || e is ArgumentOutOfRangeException)
            {
                throw new BuildToolException("The answer is not valid JSON: " + e.Message);
            }
            // For a prefix it does not have, the API answers HTTP 200 with the body 404 (not a JSON object), so that is not a network failure.
            if (root is double code && code == 404) throw new CollectionNotFoundException();
            if (!(root is Dictionary<string, object?> map)) throw new BuildToolException("The answer is not an Iconify icon set (a JSON object was expected).");

            var set = new IconifySet
            {
                Prefix = MiniJson.Str(map, "prefix") ?? "",
                _width = Number(map, "width") ?? DefaultSize,
                _height = Number(map, "height") ?? DefaultSize,
            };
            foreach (var key in new[] { "icons", "aliases" })
            {
                if (!(MiniJson.Obj(map, key) is Dictionary<string, object?> group)) continue;
                foreach (var pair in group)
                {
                    if (!(pair.Value is Dictionary<string, object?> item)) continue;
                    set._entries[pair.Key] = new Entry
                    {
                        Body = MiniJson.Str(item, "body") ?? "",
                        Parent = MiniJson.Str(item, "parent"),
                        Left = Number(item, "left"),
                        Top = Number(item, "top"),
                        Width = Number(item, "width"),
                        Height = Number(item, "height"),
                        Rotate = (int)(Number(item, "rotate") ?? 0),
                        HFlip = item.TryGetValue("hFlip", out var h) && h is bool hb && hb,
                        VFlip = item.TryGetValue("vFlip", out var v) && v is bool vb && vb,
                    };
                }
            }
            if (map.TryGetValue("not_found", out var nf) && nf is List<object?> list)
            {
                foreach (var name in list)
                {
                    if (name is string s) set.NotFound.Add(s);
                }
            }
            return set;
        }

        public IEnumerable<string> Names => _entries.Keys;

        public bool Contains(string name) => _entries.ContainsKey(name);

        /// <summary>Resolves an icon or an alias to its markup and view box, applying the flips and rotations an alias adds.</summary>
        public bool TryResolve(string name, out IconBody icon)
        {
            icon = default;
            if (!TryFlatten(name, 0, out var e) || e.Body.Length == 0) return false;

            var left = e.Left ?? 0;
            var top = e.Top ?? 0;
            var width = e.Width ?? _width;
            var height = e.Height ?? _height;
            var body = e.Body;

            var rotate = e.Rotate;
            var hFlip = e.HFlip;
            var vFlip = e.VFlip;
            var transform = new List<string>();
            // The same steps as iconToSVG of @iconify/utils, so a flipped alias looks the way the Iconify components draw it.
            if (hFlip)
            {
                if (vFlip)
                {
                    rotate += 2;
                }
                else
                {
                    transform.Add("translate(" + Num(width + left) + " " + Num(0 - top) + ")");
                    transform.Add("scale(-1 1)");
                    top = left = 0;
                }
            }
            else if (vFlip)
            {
                transform.Add("translate(" + Num(0 - left) + " " + Num(height + top) + ")");
                transform.Add("scale(1 -1)");
                top = left = 0;
            }

            switch (((rotate % 4) + 4) % 4)
            {
                case 1:
                {
                    var t = height / 2 + top;
                    transform.Insert(0, "rotate(90 " + Num(t) + " " + Num(t) + ")");
                    break;
                }
                case 2:
                    transform.Insert(0, "rotate(180 " + Num(width / 2 + left) + " " + Num(height / 2 + top) + ")");
                    break;
                case 3:
                {
                    var t = width / 2 + left;
                    transform.Insert(0, "rotate(-90 " + Num(t) + " " + Num(t) + ")");
                    break;
                }
            }
            if ((((rotate % 4) + 4) % 4) % 2 == 1)
            {
                var swap = width;
                width = height;
                height = swap;
            }
            if (transform.Count > 0)
            {
                body = "<g transform=\"" + string.Join(" ", transform) + "\">" + body + "</g>";
            }
            icon = new IconBody(body, Num(left) + " " + Num(top) + " " + Num(width) + " " + Num(height));
            return true;
        }

        private bool TryFlatten(string name, int depth, out Entry flat)
        {
            flat = default!;
            if (depth > 8 || !_entries.TryGetValue(name, out var entry)) return false;
            if (entry.Parent == null)
            {
                flat = entry;
                return true;
            }
            if (!TryFlatten(entry.Parent, depth + 1, out var parent)) return false;
            // A child overrides what the parent says; rotations add up and flips cancel out.
            flat = new Entry
            {
                Body = entry.Body.Length > 0 ? entry.Body : parent.Body,
                Left = entry.Left ?? parent.Left,
                Top = entry.Top ?? parent.Top,
                Width = entry.Width ?? parent.Width,
                Height = entry.Height ?? parent.Height,
                Rotate = parent.Rotate + entry.Rotate,
                HFlip = parent.HFlip != entry.HFlip,
                VFlip = parent.VFlip != entry.VFlip,
            };
            return true;
        }

        private static double? Number(Dictionary<string, object?> map, string key) =>
            map.TryGetValue(key, out var v) && v is double d ? d : (double?)null;

        private static string Num(double value) => Math.Round(value, 4).ToString("0.####", CultureInfo.InvariantCulture);
    }
}
