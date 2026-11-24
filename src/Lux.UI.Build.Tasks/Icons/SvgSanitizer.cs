using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

// Shared by the build task (every icon body is sanitized when the bundle is generated) and by the library (LuxIconOptions.RegisterSvg),
// like IconNames.cs and SvgImport.cs.
#if LUX_TASKS
namespace Lux.UI.Build
#else
namespace Lux.UI.Internal
#endif
{
    /// <summary>
    /// Turns the markup of an icon (what goes inside the <c>&lt;svg&gt;</c> element) into markup that is safe to render raw, whoever wrote it.
    /// Icon bodies come from the Iconify API, from a cache shared by several projects, from a self-hosted server and from SVG files:
    /// all third-party data, rendered as markup by the component and by the scripts. A filter on the text cannot be made safe (an
    /// attribute without quotes, a tag rebuilt after a removal, an entity that spells <c>javascript:</c>), so the markup is parsed as XML
    /// (no DTD, no resolver) and written again from an allowlist; nothing of the input text survives except what the walk keeps.
    /// <list type="bullet">
    /// <item><b>Elements</b>: the drawing and structure elements of SVG (<c>g path circle ellipse line polyline polygon rect text tspan defs symbol
    /// use linearGradient radialGradient stop pattern clipPath mask marker filter</c> with its <c>fe*</c> primitives except <c>feImage</c>) and the
    /// animation elements (<c>animate animateTransform animateMotion mpath set</c>). Everything else, with its content, is dropped: <c>script
    /// foreignObject style image a iframe title desc metadata</c>, elements of other namespaces. An <c>a</c> keeps its content.</item>
    /// <item><b>Attributes</b>: geometry, presentation, gradient, filter and animation attributes. Every <c>on*</c>, <c>class</c>, <c>data-*</c> and
    /// unknown attribute is dropped, with the namespace declarations. <c>style</c> keeps only presentation properties.</item>
    /// <item><b>References</b>: <c>href</c> and <c>xlink:href</c> must start with <c>#</c>; every <c>url()</c> must point to a local <c>#id</c>;
    /// a value that contains <c>javascript:</c>, <c>vbscript:</c> or <c>data:</c> once entities, white space and control characters are
    /// removed is dropped; an animation whose <c>attributeName</c> could reach a link, a handler or a style is dropped.</item>
    /// </list>
    /// The output is canonical: double-quoted attributes, escaped text, no comments, no namespaces. That is what lets the component
    /// rewrite ids with simple patterns.
    /// </summary>
    internal static class SvgSanitizer
    {
        private const string SvgNs = "http://www.w3.org/2000/svg";
        private const string XlinkNs = "http://www.w3.org/1999/xlink";
        private const string XmlNsNs = "http://www.w3.org/2000/xmlns/";
        private const string XmlNs = "http://www.w3.org/XML/1998/namespace";
        private const int MaxDepth = 64;
        private const int MaxCharacters = 4000000;

        private static readonly HashSet<string> Elements = new HashSet<string>(StringComparer.Ordinal)
        {
            "g", "path", "circle", "ellipse", "line", "polyline", "polygon", "rect", "text", "tspan", "defs", "symbol", "use",
            "linearGradient", "radialGradient", "stop", "pattern", "clipPath", "mask", "marker",
            "filter", "feBlend", "feColorMatrix", "feComponentTransfer", "feComposite", "feConvolveMatrix", "feDiffuseLighting",
            "feDisplacementMap", "feDistantLight", "feDropShadow", "feFlood", "feFuncA", "feFuncB", "feFuncG", "feFuncR", "feGaussianBlur",
            "feMerge", "feMergeNode", "feMorphology", "feOffset", "fePointLight", "feSpecularLighting", "feSpotLight", "feTile", "feTurbulence",
            "animate", "animateTransform", "animateMotion", "mpath", "set",
        };

        // Elements dropped but whose content stays (a link around a drawing).
        private static readonly HashSet<string> Unwrapped = new HashSet<string>(StringComparer.Ordinal) { "a", "switch" };

        private static readonly string[] Presentation =
        {
            "fill", "fill-opacity", "fill-rule", "stroke", "stroke-width", "stroke-linecap", "stroke-linejoin", "stroke-miterlimit", "stroke-dasharray",
            "stroke-dashoffset", "stroke-opacity", "opacity", "color", "clip-path", "clip-rule", "mask", "filter", "display", "visibility", "transform",
            "transform-origin", "vector-effect", "paint-order", "shape-rendering", "text-rendering", "color-interpolation", "color-interpolation-filters",
            "stop-color", "stop-opacity", "flood-color", "flood-opacity", "lighting-color", "mix-blend-mode", "isolation", "font-family", "font-size",
            "font-weight", "font-style", "font-variant", "text-anchor", "dominant-baseline", "alignment-baseline", "baseline-shift", "letter-spacing",
            "word-spacing", "text-decoration", "direction", "writing-mode", "marker-start", "marker-mid", "marker-end", "overflow",
        };

        private static readonly string[] Geometry =
        {
            "d", "x", "y", "x1", "y1", "x2", "y2", "cx", "cy", "r", "rx", "ry", "width", "height", "points", "fx", "fy", "fr", "dx", "dy", "rotate",
            "textLength", "lengthAdjust", "pathLength", "viewBox", "preserveAspectRatio", "gradientUnits", "gradientTransform", "spreadMethod",
            "patternUnits", "patternContentUnits", "patternTransform", "clipPathUnits", "maskUnits", "maskContentUnits", "filterUnits", "primitiveUnits",
            "markerWidth", "markerHeight", "markerUnits", "refX", "refY", "orient", "offset", "xml:space",
        };

        private static readonly string[] FilterAttributes =
        {
            "in", "in2", "result", "stdDeviation", "values", "type", "mode", "operator", "k1", "k2", "k3", "k4", "tableValues", "slope", "intercept",
            "amplitude", "exponent", "order", "kernelMatrix", "divisor", "bias", "targetX", "targetY", "edgeMode", "surfaceScale", "diffuseConstant",
            "specularConstant", "specularExponent", "azimuth", "elevation", "baseFrequency", "numOctaves", "seed", "stitchTiles", "radius", "scale",
            "xChannelSelector", "yChannelSelector", "limitingConeAngle", "pointsAtX", "pointsAtY", "pointsAtZ", "z", "kernelUnitLength", "preserveAlpha",
            "stdDeviationX", "dx", "dy",
        };

        private static readonly string[] AnimationAttributes =
        {
            "attributeName", "attributeType", "begin", "end", "dur", "min", "max", "restart", "repeatCount", "repeatDur", "calcMode", "keyTimes",
            "keySplines", "from", "to", "by", "additive", "accumulate", "path", "keyPoints",
        };

        // What an animation may change: never a link, a handler, an id, the style string or the animation itself.
        private static readonly Dictionary<string, string> Animatable = Build(Presentation, Geometry, FilterAttributes);

        private static readonly Dictionary<string, string> Attributes = Build(Presentation, Geometry, FilterAttributes, AnimationAttributes, new[] { "id" });

        private static readonly HashSet<string> StyleProperties = new HashSet<string>(Presentation.Except(new[] { "transform-origin" }), StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> StyleFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "rgb", "rgba", "hsl", "hsla", "url", "translate", "translatex", "translatey", "rotate", "scale", "scalex", "scaley", "matrix", "skewx", "skewy", "calc", "var",
        };

        private static readonly Regex UrlCall = new Regex(@"url\(\s*(?:""(?<a>[^""]*)""|'(?<a>[^']*)'|(?<a>[^)'""]*))\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex LocalId = new Regex(@"^#[A-Za-z_][\w.:-]*$", RegexOptions.Compiled);
        private static readonly Regex FunctionName = new Regex(@"([A-Za-z-]+)\s*\(", RegexOptions.Compiled);
        private static readonly Regex Hidden = new Regex(@"[\u0000- \u007f-\u009f­​-‏  ﻿]", RegexOptions.Compiled);

        private static Dictionary<string, string> Build(params string[][] lists)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var list in lists)
            {
                foreach (var name in list) map[name] = name;
            }
            return map;
        }

        /// <summary>
        /// Sanitizes an icon body. <paramref name="namespaces"/> are the <c>xmlns:prefix</c> declarations of the file the body comes from
        /// (an Inkscape file uses <c>inkscape:label</c> without declaring it on the element); they only make the body parseable, none survives.
        /// </summary>
        /// <exception cref="FormatException">The body is not well-formed XML (a fragment that cannot be read is never guessed at).</exception>
        public static string Sanitize(string body, IEnumerable<KeyValuePair<string, string>>? namespaces = null)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));
            if (body.Length == 0) return "";

            var wrapper = new StringBuilder("<svg xmlns=\"" + SvgNs + "\" xmlns:xlink=\"" + XlinkNs + "\"");
            if (namespaces != null)
            {
                foreach (var pair in namespaces)
                {
                    var prefix = pair.Key.StartsWith("xmlns:", StringComparison.OrdinalIgnoreCase) ? pair.Key.Substring(6) : pair.Key;
                    if (prefix == "xlink" || prefix == "xml" || prefix == "xmlns" || !Regex.IsMatch(prefix, @"^[A-Za-z_][\w.-]*$") || pair.Value.IndexOfAny(new[] { '"', '<', '&' }) >= 0) continue;
                    wrapper.Append(" xmlns:").Append(prefix).Append("=\"").Append(pair.Value).Append('"');
                }
            }
            wrapper.Append('>').Append(body).Append("</svg>");

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                MaxCharactersInDocument = MaxCharacters,
                CheckCharacters = true,
            };

            var output = new StringBuilder(body.Length);
            var open = new Stack<string?>();
            try
            {
                using (var reader = XmlReader.Create(new System.IO.StringReader(wrapper.ToString()), settings))
                {
                    var more = reader.Read();
                    var justOpened = -1;
                    while (more)
                    {
                        if (reader.Depth > MaxDepth) throw new FormatException("The SVG is nested more than " + MaxDepth + " levels deep.");
                        var skipped = false;
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                if (reader.Depth == 0) break;
                                if (!StartElement(reader, output, open, ref justOpened))
                                {
                                    // Skip() lands on the node after the dropped element, which still has to be looked at.
                                    reader.Skip();
                                    skipped = true;
                                }
                                break;
                            case XmlNodeType.EndElement:
                                if (reader.Depth == 0) break;
                                if (open.Count > 0)
                                {
                                    var name = open.Pop();
                                    if (name == null)
                                    {
                                        // A link that was unwrapped.
                                    }
                                    else if (output.Length == justOpened)
                                    {
                                        // Nothing came between the tags: write <circle/> rather than <circle></circle>.
                                        output.Length--;
                                        output.Append("/>");
                                        justOpened = -1;
                                    }
                                    else
                                    {
                                        output.Append("</").Append(name).Append('>');
                                    }
                                }
                                break;
                            case XmlNodeType.Text:
                            case XmlNodeType.CDATA:
                            case XmlNodeType.SignificantWhitespace:
                                if (open.Count > 0 && (open.Peek() == "text" || open.Peek() == "tspan")) AppendText(output, reader.Value);
                                break;
                        }
                        if (!skipped) more = reader.Read();
                    }
                }
            }
            catch (XmlException e)
            {
                throw new FormatException("The SVG markup is not well-formed: " + e.Message);
            }
            return output.ToString();
        }

        private static bool StartElement(XmlReader reader, StringBuilder output, Stack<string?> open, ref int justOpened)
        {
            var local = reader.LocalName;
            var isEmpty = reader.IsEmptyElement;

            if (reader.NamespaceURI == SvgNs && Unwrapped.Contains(local))
            {
                if (!isEmpty) open.Push(null);
                return true;
            }
            if (reader.NamespaceURI != SvgNs || !Elements.Contains(local)) return false;

            var attributes = new List<KeyValuePair<string, string>>();
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    string? name = null;
                    if (reader.NamespaceURI.Length == 0 && reader.Prefix.Length == 0) name = reader.LocalName;
                    else if (reader.NamespaceURI == XlinkNs && reader.LocalName == "href") name = "xlink:href";
                    else if (reader.NamespaceURI == XmlNs && reader.LocalName == "space") name = "xml:space";
                    else if (reader.NamespaceURI == XmlNsNs) name = null;
                    if (name == null) continue;
                    var kept = CleanAttribute(local, name, reader.Value, out var canonical, out var value);
                    if (kept) attributes.Add(new KeyValuePair<string, string>(canonical!, value!));
                }
                reader.MoveToElement();
            }

            if (!ElementIsUsable(local, attributes)) return false;

            output.Append('<').Append(local);
            foreach (var pair in attributes)
            {
                output.Append(' ').Append(pair.Key).Append("=\"");
                AppendEscaped(output, pair.Value);
                output.Append('"');
            }
            if (isEmpty)
            {
                output.Append("/>");
            }
            else
            {
                output.Append('>');
                justOpened = output.Length;
                open.Push(local);
            }
            return true;
        }

        private static bool ElementIsUsable(string element, List<KeyValuePair<string, string>> attributes)
        {
            bool Has(string name) => attributes.Any(a => a.Key == name);
            switch (element)
            {
                case "use":
                case "mpath":
                    return Has("href") || Has("xlink:href");
                case "animate":
                case "animateTransform":
                case "set":
                    return Has("attributeName");
                default:
                    return true;
            }
        }

        private static bool CleanAttribute(string element, string name, string raw, out string? canonical, out string? value)
        {
            canonical = null;
            value = null;

            if (name == "xlink:href" || string.Equals(name, "href", StringComparison.OrdinalIgnoreCase))
            {
                var target = raw.Trim();
                if (!LocalId.IsMatch(target)) return false;
                canonical = name == "xlink:href" ? name : "href";
                value = target;
                return true;
            }
            if (name == "xml:space")
            {
                if (raw != "preserve" && raw != "default") return false;
                canonical = name;
                value = raw;
                return true;
            }
            if (!Attributes.TryGetValue(name, out var known))
            {
                if (!string.Equals(name, "style", StringComparison.OrdinalIgnoreCase)) return false;
                var style = CleanStyle(raw);
                if (style.Length == 0) return false;
                canonical = "style";
                value = style;
                return true;
            }

            if (known == "attributeName")
            {
                var target = raw.Trim();
                if (!Animatable.TryGetValue(target, out var animatable)) return false;
                canonical = known;
                value = animatable;
                return true;
            }

            if (!CleanValue(raw, out var clean)) return false;
            canonical = known;
            value = clean;
            return true;
        }

        /// <summary>No script or data link, and every <c>url()</c> points to an id of the same image (written back as <c>url(#id)</c>).</summary>
        private static bool CleanValue(string raw, out string value)
        {
            value = raw;
            var hidden = Hidden.Replace(raw, "").ToLowerInvariant();
            if (hidden.Contains("javascript:") || hidden.Contains("vbscript:") || hidden.Contains("data:")) return false;
            if (hidden.IndexOf("url(", StringComparison.Ordinal) < 0)
            {
                return hidden.IndexOf("@import", StringComparison.Ordinal) < 0 && hidden.IndexOf("expression(", StringComparison.Ordinal) < 0;
            }
            var calls = 0;
            var ok = true;
            var rewritten = UrlCall.Replace(raw, m =>
            {
                calls++;
                var target = m.Groups["a"].Value.Trim();
                if (!LocalId.IsMatch(target)) { ok = false; return m.Value; }
                return "url(" + target + ")";
            });
            var expected = Regex.Matches(hidden, @"url\(").Count;
            if (!ok || calls != expected) return false;
            value = rewritten;
            return true;
        }

        private static string CleanStyle(string raw)
        {
            if (raw.IndexOf('\\') >= 0 || raw.Contains("/*") || raw.IndexOf('<') >= 0 || raw.IndexOf('&') >= 0) return "";
            var sb = new StringBuilder();
            foreach (var declaration in SplitDeclarations(raw))
            {
                var colon = declaration.IndexOf(':');
                if (colon <= 0) continue;
                var property = declaration.Substring(0, colon).Trim();
                var text = declaration.Substring(colon + 1).Trim();
                if (!StyleProperties.Contains(property) || text.Length == 0 || text.IndexOf('"') >= 0) continue;
                if (text.EndsWith("!important", StringComparison.OrdinalIgnoreCase)) text = text.Substring(0, text.Length - 10).Trim();
                if (FunctionName.Matches(text).Cast<Match>().Any(m => !StyleFunctions.Contains(m.Groups[1].Value))) continue;
                if (!CleanValue(text, out var clean)) continue;
                if (sb.Length > 0) sb.Append(';');
                sb.Append(property.ToLowerInvariant()).Append(':').Append(clean);
            }
            return sb.ToString();
        }

        private static IEnumerable<string> SplitDeclarations(string style)
        {
            var depth = 0;
            var quote = '\0';
            var start = 0;
            for (var i = 0; i < style.Length; i++)
            {
                var c = style[i];
                if (quote != '\0') { if (c == quote) quote = '\0'; continue; }
                if (c == '\'') quote = c;
                else if (c == '(') depth++;
                else if (c == ')') depth = Math.Max(0, depth - 1);
                else if (c == ';' && depth == 0)
                {
                    yield return style.Substring(start, i - start);
                    start = i + 1;
                }
            }
            if (start < style.Length) yield return style.Substring(start);
        }

        private static void AppendText(StringBuilder sb, string text)
        {
            foreach (var c in text)
            {
                switch (c)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    default: sb.Append(c); break;
                }
            }
        }

        private static void AppendEscaped(StringBuilder sb, string text)
        {
            foreach (var c in text)
            {
                switch (c)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '"': sb.Append("&quot;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '\n': sb.Append("&#10;"); break;
                    case '\r': sb.Append("&#13;"); break;
                    case '\t': sb.Append("&#9;"); break;
                    default: sb.Append(c); break;
                }
            }
        }
    }
}
