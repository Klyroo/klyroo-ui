using System.Globalization;
using System.Text;

namespace Blux.UI.Build.Fonts
{
    /// <summary>
    /// A small JSON reader (objects as dictionaries, arrays as lists, numbers as double): the task assembly has no
    /// System.Text.Json, which is not part of netstandard2.0 and would be one more file to ship and load.
    /// </summary>
    internal static class MiniJson
    {
        private const int MaxDepth = 64;

        /// <summary>Throws <see cref="FormatException"/>, and only that, for anything that is not valid JSON.</summary>
        public static object? Parse(string text)
        {
            try
            {
                var p = 0;
                var value = ReadValue(text, ref p, 0);
                SkipSpace(text, ref p);
                if (p != text.Length) throw new FormatException("Unexpected data after the JSON value.");
                return value;
            }
            catch (Exception e) when (e is IndexOutOfRangeException || e is ArgumentException || e is OverflowException || e is InvalidCastException)
            {
                throw new FormatException("Invalid JSON: " + e.Message, e);
            }
        }

        private static void SkipSpace(string s, ref int p)
        {
            while (p < s.Length && char.IsWhiteSpace(s[p])) p++;
        }

        private static object? ReadValue(string s, ref int p, int depth)
        {
            if (depth > MaxDepth) throw new FormatException("The JSON is nested too deeply.");
            SkipSpace(s, ref p);
            if (p >= s.Length) throw new FormatException("Unexpected end of JSON.");
            switch (s[p])
            {
                case '{':
                {
                    p++;
                    var map = new Dictionary<string, object?>();
                    SkipSpace(s, ref p);
                    if (s[p] == '}') { p++; return map; }
                    while (true)
                    {
                        SkipSpace(s, ref p);
                        var key = ReadString(s, ref p);
                        SkipSpace(s, ref p);
                        Expect(s, ref p, ':');
                        map[key] = ReadValue(s, ref p, depth + 1);
                        SkipSpace(s, ref p);
                        if (s[p] == ',') { p++; continue; }
                        Expect(s, ref p, '}');
                        return map;
                    }
                }
                case '[':
                {
                    p++;
                    var list = new List<object?>();
                    SkipSpace(s, ref p);
                    if (s[p] == ']') { p++; return list; }
                    while (true)
                    {
                        list.Add(ReadValue(s, ref p, depth + 1));
                        SkipSpace(s, ref p);
                        if (s[p] == ',') { p++; continue; }
                        Expect(s, ref p, ']');
                        return list;
                    }
                }
                case '"':
                    return ReadString(s, ref p);
                case 't': Literal(s, ref p, "true"); return true;
                case 'f': Literal(s, ref p, "false"); return false;
                case 'n': Literal(s, ref p, "null"); return null;
                default:
                {
                    var start = p;
                    while (p < s.Length && "+-0123456789.eE".IndexOf(s[p]) >= 0) p++;
                    return double.Parse(s.Substring(start, p - start), CultureInfo.InvariantCulture);
                }
            }
        }

        private static void Literal(string s, ref int p, string word)
        {
            if (string.CompareOrdinal(s, p, word, 0, word.Length) != 0) throw new FormatException($"Unexpected data at position {p}.");
            p += word.Length;
        }

        private static void Expect(string s, ref int p, char c)
        {
            if (p >= s.Length || s[p] != c) throw new FormatException($"Expected '{c}' at position {p}.");
            p++;
        }

        private static string ReadString(string s, ref int p)
        {
            Expect(s, ref p, '"');
            var sb = new StringBuilder();
            while (p < s.Length && s[p] != '"')
            {
                var c = s[p++];
                if (c != '\\') { sb.Append(c); continue; }
                var e = s[p++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u':
                        sb.Append((char)int.Parse(s.Substring(p, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        p += 4;
                        break;
                    default: sb.Append(e); break;
                }
            }
            Expect(s, ref p, '"');
            return sb.ToString();
        }

        public static Dictionary<string, object?>? Obj(object? value, string key) =>
            value is Dictionary<string, object?> d && d.TryGetValue(key, out var v) ? v as Dictionary<string, object?> : null;

        public static string? Str(Dictionary<string, object?>? obj, string key) =>
            obj != null && obj.TryGetValue(key, out var v) ? v as string : null;
    }
}
