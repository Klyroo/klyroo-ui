using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

// Shared by the build task (custom icon folders) and by the library (LuxIconOptions.RegisterSvg), like IconNames.cs.
#if LUX_TASKS
namespace Lux.UI.Build
#else
namespace Lux.UI.Internal
#endif
{
    /// <summary>The markup and the view box of an icon, in the form Iconify uses.</summary>
    internal readonly struct IconBody
    {
        public IconBody(string body, string viewBox)
        {
            Body = body;
            ViewBox = viewBox;
        }

        /// <summary>What goes inside the <c>&lt;svg&gt;</c> element.</summary>
        public string Body { get; }

        /// <summary>The <c>viewBox</c> attribute: <c>left top width height</c>.</summary>
        public string ViewBox { get; }
    }

    /// <summary>Reads the markup of an <c>.svg</c> file into the form Iconify uses: the content of the root element and a view box.</summary>
    internal static class SvgImport
    {
        // Presentation attributes of the root element move to a group, so a file such as <svg fill="none" stroke="currentColor">
        // keeps its look once its content is inlined into the svg element of the component.
        private static readonly string[] RootPresentation =
        {
            "fill", "stroke", "stroke-width", "stroke-linecap", "stroke-linejoin", "stroke-miterlimit", "stroke-dasharray",
            "stroke-dashoffset", "stroke-opacity", "fill-opacity", "fill-rule", "clip-rule", "opacity", "color",
        };

        private static readonly Regex Comments = new Regex(@"<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex Prolog = new Regex(@"<\?.*?\?>|<!DOCTYPE[^>]*>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex Root = new Regex(@"<svg\b(?<attrs>(?:[^>""']|""[^""]*""|'[^']*')*)>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex StyleElement = new Regex(@"<style\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex Attribute = new Regex(@"(?<name>[A-Za-z_:][\w:.-]*)\s*=\s*(?:""(?<v>[^""]*)""|'(?<v>[^']*)')", RegexOptions.Compiled);

        /// <exception cref="FormatException">The text is not an SVG image the library can inline.</exception>
        public static IconBody Parse(string svg)
        {
            if (svg == null) throw new ArgumentNullException(nameof(svg));
            var text = Prolog.Replace(Comments.Replace(svg, ""), "");
            var root = Root.Match(text);
            if (!root.Success) throw new FormatException("There is no <svg> element.");
            if (root.Value.EndsWith("/>", StringComparison.Ordinal)) throw new FormatException("The <svg> element is empty.");
            var close = text.LastIndexOf("</svg", StringComparison.OrdinalIgnoreCase);
            if (close < root.Index + root.Length) throw new FormatException("The <svg> element is not closed.");

            var inner = text.Substring(root.Index + root.Length, close - root.Index - root.Length);
            if (StyleElement.IsMatch(inner))
            {
                throw new FormatException("It contains a <style> element, whose rules would apply to the whole page. Use presentation attributes (fill, stroke...) instead.");
            }
            var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in Attribute.Matches(root.Groups["attrs"].Value))
            {
                attributes[m.Groups["name"].Value] = m.Groups["v"].Value;
            }

            // The presentation attributes of the root go on a group, then everything is read as XML and written again from an allowlist
            // (see SvgSanitizer): a filter over the text could not be made safe.
            var group = new StringBuilder();
            foreach (var name in RootPresentation)
            {
                if (attributes.TryGetValue(name, out var value) && !value.Contains("\""))
                {
                    group.Append(' ').Append(name).Append("=\"").Append(value).Append('"');
                }
            }
            var namespaces = attributes.Where(a => a.Key.StartsWith("xmlns:", StringComparison.OrdinalIgnoreCase)).ToList();
            var body = SvgSanitizer.Sanitize(inner, namespaces).Trim();
            if (body.Length == 0) throw new FormatException("The <svg> element is empty (or holds only content that cannot be inlined: scripts, images, links to other files).");
            if (group.Length > 0)
            {
                var wrapper = SvgSanitizer.Sanitize("<g" + group + "/>", namespaces);
                body = (wrapper.EndsWith("/>", StringComparison.Ordinal) ? wrapper.Substring(0, wrapper.Length - 2) + ">" : "<g>") + body + "</g>";
            }

            return new IconBody(body, ViewBoxOf(attributes));
        }

        private static string ViewBoxOf(Dictionary<string, string> attributes)
        {
            if (attributes.TryGetValue("viewBox", out var raw))
            {
                var parts = raw.Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 4 && parts.All(p => double.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out _)))
                {
                    var width = double.Parse(parts[2], CultureInfo.InvariantCulture);
                    var height = double.Parse(parts[3], CultureInfo.InvariantCulture);
                    if (width > 0 && height > 0) return string.Join(" ", parts);
                }
                throw new FormatException("The viewBox \"" + raw + "\" is not four numbers with a positive width and height.");
            }
            if (attributes.TryGetValue("width", out var w) && attributes.TryGetValue("height", out var h) && Dimension(w, out var wn) && Dimension(h, out var hn))
            {
                return "0 0 " + wn.ToString(CultureInfo.InvariantCulture) + " " + hn.ToString(CultureInfo.InvariantCulture);
            }
            return "0 0 24 24";
        }

        private static bool Dimension(string value, out double number)
        {
            value = value.Trim();
            if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase)) value = value.Substring(0, value.Length - 2);
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) && number > 0;
        }
    }
}
