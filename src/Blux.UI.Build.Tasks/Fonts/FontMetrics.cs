using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Blux.UI.Build.Fonts
{
    /// <summary>The Brotli decoder is only in the runtime of .NET Core and later, not in the .NET Framework of Visual Studio's MSBuild.</summary>
    internal sealed class BrotliUnavailableException : FontException
    {
        public BrotliUnavailableException() : base("This MSBuild cannot decompress woff2 (no Brotli decoder in the .NET Framework).") { }
    }

    /// <summary>The file has a font signature but its content cannot be read (truncated, corrupt compressed data).</summary>
    internal sealed class CorruptFontException : FontException
    {
        public CorruptFontException(string message) : base(message) { }
        public CorruptFontException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Reads the few tables the metrics need from a font file: plain sfnt (ttf, otf), WOFF and WOFF2. WOFF2 is a table
    /// directory followed by one Brotli stream of every table, so the tables are cut out of the decompressed stream.
    /// </summary>
    internal static class FontTables
    {
        private static readonly string[] KnownTags =
        {
            "cmap", "head", "hhea", "hmtx", "maxp", "name", "OS/2", "post", "cvt ", "fpgm", "glyf", "loca", "prep", "CFF ", "VORG",
            "EBDT", "EBLC", "gasp", "hdmx", "kern", "LTSH", "PCLT", "VDMX", "vhea", "vmtx", "BASE", "GDEF", "GPOS", "GSUB", "EBSC",
            "JSTF", "MATH", "CBDT", "CBLC", "COLR", "CPAL", "SVG ", "sbix", "acnt", "avar", "bdat", "bloc", "bsln", "cvar", "fdsc",
            "feat", "fmtx", "fvar", "gvar", "hsty", "just", "lcar", "mort", "morx", "opbd", "prop", "trak", "Zapf", "Silf", "Glat",
            "Gloc", "Feat", "Sill",
        };

        private static readonly HashSet<string> Wanted = new HashSet<string> { "cmap", "head", "hhea", "hmtx", "OS/2" };

        public static Dictionary<string, byte[]> Read(byte[] data)
        {
            if (data.Length < 12) throw new CorruptFontException("The font file is too short to be a font.");
            var signature = Encoding.ASCII.GetString(data, 0, 4);
            return signature switch
            {
                "wOF2" => ReadWoff2(data),
                "wOFF" => ReadWoff(data),
                _ => ReadSfnt(data),
            };
        }

        private static ushort U16(byte[] d, int o) => (ushort)((d[o] << 8) | d[o + 1]);
        private static uint U32(byte[] d, int o) => (uint)((d[o] << 24) | (d[o + 1] << 16) | (d[o + 2] << 8) | d[o + 3]);

        private static Dictionary<string, byte[]> ReadSfnt(byte[] d)
        {
            var count = U16(d, 4);
            var tables = new Dictionary<string, byte[]>();
            for (var i = 0; i < count; i++)
            {
                var o = 12 + i * 16;
                var tag = Encoding.ASCII.GetString(d, o, 4);
                if (!Wanted.Contains(tag)) continue;
                tables[tag] = Slice(d, (int)U32(d, o + 8), (int)U32(d, o + 12));
            }
            return tables;
        }

        private static Dictionary<string, byte[]> ReadWoff(byte[] d)
        {
            var count = U16(d, 12);
            var tables = new Dictionary<string, byte[]>();
            for (var i = 0; i < count; i++)
            {
                var o = 44 + i * 20;
                var tag = Encoding.ASCII.GetString(d, o, 4);
                if (!Wanted.Contains(tag)) continue;
                var offset = (int)U32(d, o + 4);
                var compressed = (int)U32(d, o + 8);
                var original = (int)U32(d, o + 12);
                var raw = Slice(d, offset, compressed);
                if (compressed >= original)
                {
                    tables[tag] = raw;
                    continue;
                }
                // zlib: two header bytes, then deflate.
                using var input = new MemoryStream(raw, 2, raw.Length - 2);
                using var deflate = new DeflateStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                deflate.CopyTo(output);
                tables[tag] = output.ToArray();
            }
            return tables;
        }

        private static Dictionary<string, byte[]> ReadWoff2(byte[] d)
        {
            var count = U16(d, 12);
            var compressedSize = (int)U32(d, 20);
            var p = 48;
            var entries = new List<(string Tag, int Length)>();
            for (var i = 0; i < count; i++)
            {
                var flags = d[p++];
                var index = flags & 0x3f;
                string tag;
                if (index == 63)
                {
                    tag = Encoding.ASCII.GetString(d, p, 4);
                    p += 4;
                }
                else
                {
                    tag = KnownTags[index];
                }
                var original = Base128(d, ref p);
                var version = flags >> 6;
                // glyf and loca are transformed with version 0; every other table with a version other than 0 (hmtx: 1).
                var transformed = tag == "glyf" || tag == "loca" ? version == 0 : version != 0;
                var length = transformed ? Base128(d, ref p) : original;
                entries.Add((tag, length));
            }
            var stream = Brotli(Slice(d, p, compressedSize));
            var tables = new Dictionary<string, byte[]>();
            var offset = 0;
            foreach (var (tag, length) in entries)
            {
                if (Wanted.Contains(tag)) tables[tag] = Slice(stream, offset, length);
                offset += length;
            }
            return tables;
        }

        private static int Base128(byte[] d, ref int p)
        {
            var value = 0;
            for (var i = 0; i < 5; i++)
            {
                var b = d[p++];
                value = (value << 7) | (b & 0x7f);
                if ((b & 0x80) == 0) return value;
            }
            throw new CorruptFontException("Invalid WOFF2 table directory.");
        }

        private static byte[] Slice(byte[] d, int offset, int length)
        {
            if (offset < 0 || length < 0 || offset + length > d.Length) throw new CorruptFontException("The font file is truncated or corrupted.");
            var result = new byte[length];
            Buffer.BlockCopy(d, offset, result, 0, length);
            return result;
        }

        private static Type? BrotliType() =>
            Type.GetType("System.IO.Compression.BrotliStream, System.IO.Compression.Brotli", false)
            ?? Type.GetType("System.IO.Compression.BrotliStream, System.IO.Compression", false);

        /// <summary>Whether this MSBuild can decompress woff2 (and so compute the fallback faces).</summary>
        public static bool IsBrotliAvailable() => BrotliType() != null;

        private static byte[] Brotli(byte[] compressed)
        {
            var type = BrotliType();
            if (type == null) throw new BrotliUnavailableException();
            using var input = new MemoryStream(compressed);
            using var brotli = (Stream)Activator.CreateInstance(type, input, CompressionMode.Decompress)!;
            using var output = new MemoryStream();
            brotli.CopyTo(output);
            return output.ToArray();
        }
    }

    /// <summary>The numbers a fallback face is computed from, in font units.</summary>
    internal sealed class FontMetrics
    {
        public int UnitsPerEm { get; set; }
        public int Ascent { get; set; }
        public int Descent { get; set; }
        public int LineGap { get; set; }
        /// <summary>Average advance of a lowercase letter or a space, weighted by English letter frequency.</summary>
        public double XWidthAvg { get; set; }

        // Relative frequency of a..z in English text, and of the space (about one character in six).
        private static readonly double[] LetterWeights =
        {
            8.167, 1.492, 2.782, 4.253, 12.702, 2.228, 2.015, 6.094, 6.966, 0.153, 0.772, 4.025, 2.406,
            6.749, 7.507, 1.929, 0.095, 5.987, 6.327, 9.056, 2.758, 0.978, 2.360, 0.150, 1.974, 0.074,
        };
        private const double SpaceWeight = 20;

        public static FontMetrics Read(byte[] file)
        {
            try
            {
                return ReadCore(file);
            }
            // The Brotli decoder reports a corrupt stream as an InvalidOperationException, zlib as an InvalidDataException.
            catch (Exception e) when (e is IndexOutOfRangeException || e is ArgumentException || e is InvalidDataException || e is OverflowException
                || e is InvalidOperationException || e is FormatException || e is KeyNotFoundException || e is IOException || e is NullReferenceException)
            {
                throw new CorruptFontException("The file is not a valid font: " + e.Message, e);
            }
        }

        private static FontMetrics ReadCore(byte[] file)
        {
            var t = FontTables.Read(file);
            foreach (var needed in new[] { "cmap", "head", "hhea", "hmtx" })
            {
                if (!t.ContainsKey(needed)) throw new FontException($"The font has no '{needed}' table.");
            }
            var head = t["head"];
            var hhea = t["hhea"];
            var metrics = new FontMetrics
            {
                UnitsPerEm = U16(head, 18),
                Ascent = I16(hhea, 4),
                Descent = I16(hhea, 6),
                LineGap = I16(hhea, 8),
            };
            if (metrics.UnitsPerEm == 0) throw new FontException("The font reports 0 units per em.");

            // Browsers use the typographic metrics when the font asks for it (USE_TYPO_METRICS, bit 7 of fsSelection).
            if (t.TryGetValue("OS/2", out var os2) && os2.Length >= 74 && (U16(os2, 62) & 0x80) != 0 && (I16(os2, 68) != 0 || I16(os2, 70) != 0))
            {
                metrics.Ascent = I16(os2, 68);
                metrics.Descent = I16(os2, 70);
                metrics.LineGap = I16(os2, 72);
            }

            var cmap = new Cmap(t["cmap"]);
            var hmtx = t["hmtx"];
            var numberOfHMetrics = U16(hhea, 34);
            if (numberOfHMetrics == 0) throw new FontException("The font has no horizontal metrics.");
            int Advance(int glyph)
            {
                var index = Math.Min(glyph, numberOfHMetrics - 1);
                return U16(hmtx, index * 4);
            }

            double sum = 0, total = 0;
            for (var i = 0; i < 26; i++)
            {
                var glyph = cmap.Glyph('a' + i);
                if (glyph == 0) throw new FontException("The file has no latin letters (a subset without them cannot be measured).");
                sum += LetterWeights[i] * Advance(glyph);
                total += LetterWeights[i];
            }
            var space = cmap.Glyph(' ');
            if (space != 0)
            {
                sum += SpaceWeight * Advance(space);
                total += SpaceWeight;
            }
            metrics.XWidthAvg = sum / total;
            return metrics;
        }

        private static ushort U16(byte[] d, int o) => (ushort)((d[o] << 8) | d[o + 1]);
        private static short I16(byte[] d, int o) => (short)((d[o] << 8) | d[o + 1]);
        private static uint U32(byte[] d, int o) => (uint)((d[o] << 24) | (d[o + 1] << 16) | (d[o + 2] << 8) | d[o + 3]);

        /// <summary>Code point to glyph through the cmap (format 4 or 12 of a Unicode subtable).</summary>
        private sealed class Cmap
        {
            private readonly byte[] _d;
            private readonly int _offset;
            private readonly int _format;

            public Cmap(byte[] data)
            {
                _d = data;
                var count = U16(data, 2);
                int best = -1, bestScore = -1;
                for (var i = 0; i < count; i++)
                {
                    var o = 4 + i * 8;
                    int platform = U16(data, o), encoding = U16(data, o + 2);
                    var offset = (int)U32(data, o + 4);
                    var format = U16(data, offset);
                    var score = platform == 3 && encoding == 10 && format == 12 ? 4
                        : platform == 0 && format == 12 ? 3
                        : platform == 3 && encoding == 1 && format == 4 ? 2
                        : platform == 0 && format == 4 ? 1 : -1;
                    if (score > bestScore) { best = offset; bestScore = score; }
                }
                if (best < 0) throw new FontException("The font has no Unicode character map.");
                _offset = best;
                _format = U16(data, best);
            }

            public int Glyph(int codePoint) => _format == 12 ? Glyph12(codePoint) : Glyph4(codePoint);

            private int Glyph12(int cp)
            {
                var groups = (int)U32(_d, _offset + 12);
                for (var i = 0; i < groups; i++)
                {
                    var o = _offset + 16 + i * 12;
                    uint start = U32(_d, o), end = U32(_d, o + 4);
                    if (cp >= start && cp <= end) return (int)(U32(_d, o + 8) + (uint)cp - start);
                }
                return 0;
            }

            private int Glyph4(int cp)
            {
                if (cp > 0xFFFF) return 0;
                var segCount = U16(_d, _offset + 6) / 2;
                var endCodes = _offset + 14;
                var startCodes = endCodes + segCount * 2 + 2;
                var deltas = startCodes + segCount * 2;
                var rangeOffsets = deltas + segCount * 2;
                for (var i = 0; i < segCount; i++)
                {
                    if (cp > U16(_d, endCodes + i * 2)) continue;
                    var start = U16(_d, startCodes + i * 2);
                    if (cp < start) return 0;
                    var delta = U16(_d, deltas + i * 2);
                    var rangeOffset = U16(_d, rangeOffsets + i * 2);
                    if (rangeOffset == 0) return (cp + delta) & 0xFFFF;
                    var glyphAddress = rangeOffsets + i * 2 + rangeOffset + (cp - start) * 2;
                    if (glyphAddress + 2 > _d.Length) return 0;
                    var glyph = U16(_d, glyphAddress);
                    return glyph == 0 ? 0 : (glyph + delta) & 0xFFFF;
                }
                return 0;
            }
        }
    }

    /// <summary>A system font a fallback face stands in for, measured the same way as the web font.</summary>
    internal sealed class SystemFont
    {
        public SystemFont(string[] localNames, int unitsPerEm, double xWidthAvg)
        {
            LocalNames = localNames;
            UnitsPerEm = unitsPerEm;
            XWidthAvg = xWidthAvg;
        }

        public string[] LocalNames { get; }
        public int UnitsPerEm { get; }
        public double XWidthAvg { get; }

        // Measured with the same code on Arimo, Cousine and Tinos, which have the advance widths of Arial, Courier New and
        // Times New Roman (they are metric-compatible), so no proprietary font file is needed. Liberation fonts, also
        // metric-compatible, are what Linux has under those names.
        public static SystemFont For(string category) => category switch
        {
            "mono" => new SystemFont(new[] { "Courier New", "Liberation Mono", "Cousine" }, 2048, 1229.0),
            "serif" => new SystemFont(new[] { "Times New Roman", "Liberation Serif", "Tinos" }, 2048, 824.312),
            _ => new SystemFont(new[] { "Arial", "Liberation Sans", "Arimo" }, 2048, 909.620),
        };
    }

    internal static class FallbackFace
    {
        /// <summary>
        /// A <c>@font-face</c> for "Family Fallback": the system font, resized and re-spaced so text set in it takes the same
        /// room as in the web font, which keeps the page from jumping when the web font replaces it (what fontaine does for Nuxt).
        /// </summary>
        public static string Css(string family, FontMetrics m, SystemFont system)
        {
            var sizeAdjust = (m.XWidthAvg / m.UnitsPerEm) / (system.XWidthAvg / system.UnitsPerEm);
            string Pct(double v) => (v * 100).ToString("0.##", CultureInfo.InvariantCulture) + "%";
            var denominator = m.UnitsPerEm * sizeAdjust;
            var sb = new StringBuilder();
            sb.Append("@font-face {\n");
            sb.Append("  font-family: ").Append(FontCss.Quote(family + " Fallback")).Append(";\n");
            sb.Append("  src: ").Append(string.Join(", ", system.LocalNames.Select(n => "local(" + FontCss.Quote(n) + ")"))).Append(";\n");
            sb.Append("  size-adjust: ").Append(Pct(sizeAdjust)).Append(";\n");
            sb.Append("  ascent-override: ").Append(Pct(m.Ascent / denominator)).Append(";\n");
            sb.Append("  descent-override: ").Append(Pct(Math.Abs(m.Descent) / denominator)).Append(";\n");
            sb.Append("  line-gap-override: ").Append(Pct(m.LineGap / denominator)).Append(";\n");
            sb.Append("}\n");
            return sb.ToString();
        }
    }
}
