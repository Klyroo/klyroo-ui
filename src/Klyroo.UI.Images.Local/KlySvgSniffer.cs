using System.Text;

namespace Klyroo.UI.Images.Local;

/// <summary>
/// Decides whether bytes are an SVG document by parsing past the XML declaration, comments, processing instructions and the DOCTYPE (internal
/// subset included) to the name of the root element, within the first 64 KiB. Looking for the text "&lt;svg" anywhere near the start would
/// take a PNG with a text chunk for an SVG, and miss an SVG with a long prolog.
/// </summary>
internal static class KlySvgSniffer
{
    internal const int Limit = 64 * 1024;

    public static bool IsSvg(ReadOnlySpan<byte> bytes)
    {
        var head = bytes[..Math.Min(bytes.Length, Limit)];
        var text = Decode(head);
        var i = 0;
        if (text.Length > 0 && text[0] == '﻿') i++;
        while (true)
        {
            while (i < text.Length && IsSpace(text[i])) i++;
            if (i >= text.Length || text[i] != '<') return false;

            if (At(text, i, "<?"))
            {
                var end = text.IndexOf("?>", i + 2, StringComparison.Ordinal);
                if (end < 0) return false;
                i = end + 2;
            }
            else if (At(text, i, "<!--"))
            {
                var end = text.IndexOf("-->", i + 4, StringComparison.Ordinal);
                if (end < 0) return false;
                i = end + 3;
            }
            else if (At(text, i, "<!DOCTYPE"))
            {
                i = SkipDoctype(text, i + 9);
                if (i < 0) return false;
            }
            else
            {
                var start = i + 1;
                var end = start;
                while (end < text.Length && !IsSpace(text[end]) && text[end] is not ('>' or '/')) end++;
                if (end >= text.Length) return false;
                var name = text.AsSpan(start, end - start);
                var colon = name.LastIndexOf(':');
                if (colon >= 0) name = name[(colon + 1)..];
                return name.SequenceEqual("svg");
            }
        }
    }

    private static bool IsSpace(char c) => c is ' ' or '\t' or '\r' or '\n';

    private static bool At(string text, int index, string value) => string.CompareOrdinal(text, index, value, 0, value.Length) == 0;

    // Returns the index after the closing '>', or -1 when the declaration does not end within the text.
    private static int SkipDoctype(string text, int i)
    {
        var quote = '\0';
        var subset = false;
        for (; i < text.Length; i++)
        {
            var c = text[i];
            if (quote != '\0')
            {
                if (c == quote) quote = '\0';
            }
            else if (subset && At(text, i, "<!--"))
            {
                var end = text.IndexOf("-->", i + 4, StringComparison.Ordinal);
                if (end < 0) return -1;
                i = end + 2;
            }
            else if (c is '"' or '\'')
            {
                quote = c;
            }
            else if (c == '[')
            {
                subset = true;
            }
            else if (c == ']')
            {
                subset = false;
            }
            else if (c == '>' && !subset)
            {
                return i + 1;
            }
        }
        return -1;
    }

    private static string Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) return Encoding.Unicode.GetString(bytes[2..]);
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) return Encoding.BigEndianUnicode.GetString(bytes[2..]);
        // UTF-16 without a byte order mark: a '<' next to a zero byte.
        if (bytes.Length >= 4 && bytes[0] == 0x3C && bytes[1] == 0x00) return Encoding.Unicode.GetString(bytes);
        if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x3C) return Encoding.BigEndianUnicode.GetString(bytes);
        return Encoding.UTF8.GetString(bytes);
    }
}
