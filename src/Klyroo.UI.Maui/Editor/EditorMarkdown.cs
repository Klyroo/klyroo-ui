using System.Text;
using System.Text.RegularExpressions;

namespace Klyroo.UI.Maui;

/// <summary>A text and a selection: the result of an edit, or a state of the editor kept for undo.</summary>
internal readonly record struct EditorEdit(string Text, int Start, int Length)
{
    public int End => Start + Length;
}

/// <summary>The formats at the selection: what pressed toolbar buttons show.</summary>
internal sealed record EditorFormats(
    bool Bold, bool Italic, bool Strike, bool Underline, bool Code, int Heading,
    bool BulletList, bool OrderedList, bool TaskList, bool Blockquote, bool CodeBlock,
    string? Link, bool CanMoveUp, bool CanMoveDown)
{
    public static EditorFormats None { get; } = new(false, false, false, false, false, 0, false, false, false, false, false, null, false, false);
}

/// <summary>A trigger character typed before the cursor, with what follows it.</summary>
internal readonly record struct EditorTrigger(int Start, int Length, string Query);

/// <summary>
/// Markdown editing as pure functions: each takes the text and the selection and returns the new text and selection.
/// Inline marks (bold, italic, strike, code) toggle around the selection or the word under the cursor; block formats
/// (headings, lists, quote) change the prefix of the lines the selection touches; a block is a run of lines up to the next blank line.
/// </summary>
internal static class EditorMarkdown
{
    private enum Marker { None, Heading, Bullet, Ordered, Task }

    private enum Mark { Bold, Italic, Strike, Underline, Code }

    /// <summary>A line with the prefix it carries: quote layers, indentation, then a heading, list or task marker.</summary>
    private readonly record struct Row(int Start, string Text, int QuoteLength, int IndentLength, int MarkerLength, Marker Kind, int Level)
    {
        public bool Blank => string.IsNullOrWhiteSpace(Text);
        public int MarkerAt => Start + QuoteLength + IndentLength;
    }

    /// <summary>Replaces <paramref name="Remove"/> characters at <paramref name="At"/>. A position at <paramref name="At"/> moves after a pure insertion when <paramref name="After"/> is set.</summary>
    private readonly record struct Replace(int At, int Remove, string Insert, bool After = true);

    /// <summary>A marked stretch of a line: from the opening mark to the end of the closing mark, with the text between.</summary>
    private readonly record struct Span(int Start, int ContentStart, int ContentEnd, int End);

    private static readonly Regex RowPattern = new(@"^(?<quote>(?:>[ ]?)*)(?<indent>[ \t]*)(?<marker>#{1,6}[ ]|[-*+][ ]\[[ xX]\][ ]|[-*+][ ]|\d{1,9}[.)][ ])?", RegexOptions.CultureInvariant);
    private static readonly Regex LinkPattern = new(@"(?<!!)\[(?<text>(?:\\.|[^\]\\\n])*)\]\((?<url>[^)\s]*)\)", RegexOptions.CultureInvariant);
    private static readonly Regex StrikePattern = new(@"~~(?=\S)(.+?)(?<=\S)~~", RegexOptions.CultureInvariant);
    private static readonly Regex UnderlinePattern = new(@"<u>(.+?)</u>", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex CodePattern = new(@"`([^`]+)`", RegexOptions.CultureInvariant);
    private static readonly Regex Marks = new(@"\*+|~~|`|</?u>", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    /// <summary>Runs an action on the text. Returns null when it does not apply (a link to remove where there is none, no block to move). Undo and redo belong to the editor history.</summary>
    public static EditorEdit? Apply(EditorAction action, int level, string? argument, string text, int start, int length)
    {
        start = Math.Clamp(start, 0, text.Length);
        length = Math.Clamp(length, 0, text.Length - start);
        return action switch
        {
            EditorAction.Bold => ToggleInline(text, start, length, Mark.Bold),
            EditorAction.Italic => ToggleInline(text, start, length, Mark.Italic),
            EditorAction.Strike => ToggleInline(text, start, length, Mark.Strike),
            EditorAction.Underline => ToggleInline(text, start, length, Mark.Underline),
            EditorAction.Code => ToggleInline(text, start, length, Mark.Code),
            EditorAction.Paragraph or EditorAction.Heading or EditorAction.BulletList or EditorAction.OrderedList or EditorAction.TaskList or EditorAction.Blockquote =>
                ToggleBlock(action, Math.Clamp(level, 1, 6), text, start, length),
            EditorAction.CodeBlock => ToggleCodeBlock(text, start, length),
            EditorAction.HorizontalRule => InsertRule(text, start, length),
            EditorAction.Link => SetLink(text, start, length, argument),
            EditorAction.Image => SetImage(text, start, length, argument),
            EditorAction.ClearFormatting => Clear(text, start, length),
            EditorAction.MoveUp => Move(text, start, length, -1),
            EditorAction.MoveDown => Move(text, start, length, 1),
            _ => null,
        };
    }

    /// <summary>The formats at the selection.</summary>
    public static EditorFormats Inspect(string text, int start, int length)
    {
        start = Math.Clamp(start, 0, text.Length);
        length = Math.Clamp(length, 0, text.Length - start);
        var rows = RowsOf(text, start, length);
        var used = Considered(rows);
        var first = used[0];
        var heading = used.All(r => r.Kind == Marker.Heading && r.Level == first.Level) ? first.Level : 0;
        var (canUp, canDown) = CanMove(text, start);
        return new EditorFormats(
            FindSpan(text, start, length, Mark.Bold) is not null,
            FindSpan(text, start, length, Mark.Italic) is not null,
            FindSpan(text, start, length, Mark.Strike) is not null,
            FindSpan(text, start, length, Mark.Underline) is not null,
            FindSpan(text, start, length, Mark.Code) is not null,
            heading,
            used.All(r => r.Kind == Marker.Bullet),
            used.All(r => r.Kind == Marker.Ordered),
            used.All(r => r.Kind == Marker.Task),
            used.All(r => r.QuoteLength > 0),
            FenceAround(text, start) is not null,
            LinkAddress(text, start, length),
            canUp,
            canDown);
    }

    /// <summary>The address of the link that holds the selection, or null.</summary>
    public static string? LinkAddress(string text, int start, int length) =>
        LinkMatch(text, start, length) is { } m ? m.Groups["url"].Value.Replace("%28", "(").Replace("%29", ")") : null;

    /// <summary>
    /// The trigger typed before the cursor: the word that ends at the cursor starts with <paramref name="trigger"/>, which then starts the text or follows
    /// a space or a line break. The query is what follows the trigger, with no whitespace in it.
    /// </summary>
    public static EditorTrigger? FindTrigger(string text, int cursor, string trigger)
    {
        if (string.IsNullOrEmpty(trigger) || cursor <= 0 || cursor > text.Length) return null;
        var start = cursor;
        while (start > 0 && !char.IsWhiteSpace(text[start - 1])) start--;
        var word = text.AsSpan(start, cursor - start);
        if (!word.StartsWith(trigger, StringComparison.Ordinal)) return null;
        var query = word[trigger.Length..].ToString();
        return query.Contains(trigger, StringComparison.Ordinal) ? null : new EditorTrigger(start, word.Length, query);
    }

    // ---- Inline marks

    private static (string Open, string Close) Markers(Mark mark) => mark switch
    {
        Mark.Bold => ("**", "**"),
        Mark.Italic => ("*", "*"),
        Mark.Strike => ("~~", "~~"),
        Mark.Underline => ("<u>", "</u>"),
        Mark.Code => ("`", "`"),
        _ => throw new ArgumentOutOfRangeException(nameof(mark), mark, null),
    };

    private static List<Span> SpansOf(Mark mark, string line)
    {
        var spans = new List<Span>();
        switch (mark)
        {
            case Mark.Bold or Mark.Italic:
                foreach (var (span, stars) in StarSpans(line))
                {
                    if (mark == Mark.Bold ? stars >= 2 : stars % 2 == 1) spans.Add(span);
                }
                break;
            default:
                var pattern = mark switch { Mark.Strike => StrikePattern, Mark.Underline => UnderlinePattern, _ => CodePattern };
                foreach (Match m in pattern.Matches(line))
                {
                    var g = m.Groups[1];
                    spans.Add(new Span(m.Index, g.Index, g.Index + g.Length, m.Index + m.Length));
                }
                break;
        }
        return spans;
    }

    /// <summary>
    /// Pairs the runs of asterisks of a line: a run followed by text opens, a run preceded by text closes the nearest open one.
    /// A pair of runs of n and m asterisks marks min(n, m, 3) of them on each side: one is italic, two bold, three both.
    /// </summary>
    private static List<(Span Span, int Stars)> StarSpans(string line)
    {
        var result = new List<(Span, int)>();
        var open = new List<(int Start, int Length)>();
        var i = 0;
        while (i < line.Length)
        {
            if (line[i] != '*' || (i > 0 && line[i - 1] == '\\'))
            {
                i++;
                continue;
            }
            var run = i;
            while (i < line.Length && line[i] == '*') i++;
            var length = i - run;
            var canOpen = i < line.Length && !char.IsWhiteSpace(line[i]);
            var canClose = run > 0 && !char.IsWhiteSpace(line[run - 1]);
            if (canClose && open.Count > 0 && run > open[^1].Start + open[^1].Length)
            {
                var o = open[^1];
                open.RemoveAt(open.Count - 1);
                var n = Math.Min(Math.Min(o.Length, length), 3);
                var contentStart = o.Start + o.Length;
                result.Add((new Span(contentStart - n, contentStart, run, run + n), n));
            }
            else if (canOpen)
            {
                open.Add((run, length));
            }
        }
        return result;
    }

    /// <summary>The smallest stretch marked with <paramref name="mark"/> that holds the selection, with the offset of its line.</summary>
    private static (Span Span, int LineStart)? FindSpan(string text, int start, int length, Mark mark)
    {
        var (ls, le) = BlockBounds(text, start, 0);
        var end = start + length;
        if (end > le) return null;
        (Span, int)? best = null;
        foreach (var s in SpansOf(mark, text[ls..le]))
        {
            if (ls + s.Start > start || end > ls + s.End) continue;
            if (best is null || s.End - s.Start < best.Value.Item1.End - best.Value.Item1.Start) best = (s, ls);
        }
        return best;
    }

    private static EditorEdit ToggleInline(string text, int start, int length, Mark mark)
    {
        var (open, close) = Markers(mark);
        var end = start + length;

        if (FindSpan(text, start, length, mark) is { } found)
        {
            // Take the mark off the stretch that holds the selection. Of three asterisks, bold takes two and italic one.
            var (span, ls) = found;
            var (a, b) = mark switch { Mark.Bold => (2, 2), Mark.Italic => (1, 1), _ => (open.Length, close.Length) };
            return Replacing(text, start, length, [new(ls + span.ContentStart - a, a, "", false), new(ls + span.ContentEnd, b, "", false)]);
        }

        // An empty pair around the cursor, such as ** | **, is taken back.
        if (length == 0 && text.AsSpan(0, start).EndsWith(open, StringComparison.Ordinal) && text.AsSpan(start).StartsWith(close, StringComparison.Ordinal) &&
            (mark is not (Mark.Bold or Mark.Italic) || (StarRun(text, start, -1) == open.Length && StarRun(text, start, 1) == close.Length)))
        {
            return Replacing(text, start, length, [new(start - open.Length, open.Length, "", false), new(start, close.Length, "", false)]);
        }

        return new EditorEdit(text[..start] + open + text.Substring(start, length) + close + text[end..], start + open.Length, length);
    }

    private static int StarRun(string text, int at, int direction)
    {
        var n = 0;
        while (true)
        {
            var i = direction < 0 ? at - n - 1 : at + n;
            if (i < 0 || i >= text.Length || text[i] != '*') return n;
            n++;
        }
    }

    // ---- Lines and blocks of lines

    private static (int Start, int End) BlockBounds(string text, int start, int length)
    {
        var ls = start == 0 ? 0 : text.LastIndexOf('\n', start - 1) + 1;
        var probe = start + length;
        // A selection that ends right after a line break does not take the next line.
        if (length > 0 && text[probe - 1] == '\n') probe--;
        var le = text.IndexOf('\n', probe);
        return (ls, le < 0 ? text.Length : le);
    }

    private static Row Parse(int start, string line)
    {
        var m = RowPattern.Match(line);
        var marker = m.Groups["marker"];
        var text = marker.Success ? marker.Value : "";
        var (kind, level) = text.Length == 0
            ? (Marker.None, 0)
            : text[0] == '#' ? (Marker.Heading, text.Length - 1)
            : char.IsDigit(text[0]) ? (Marker.Ordered, 0)
            : text.Contains('[') ? (Marker.Task, 0)
            : (Marker.Bullet, 0);
        return new Row(start, line, m.Groups["quote"].Length, m.Groups["indent"].Length, text.Length, kind, level);
    }

    private static Row[] RowsOf(string text, int start, int length)
    {
        var (ls, le) = BlockBounds(text, start, length);
        var rows = new List<Row>();
        var at = ls;
        foreach (var part in text[ls..le].Split('\n'))
        {
            rows.Add(Parse(at, part));
            at += part.Length + 1;
        }
        return [.. rows];
    }

    /// <summary>The rows a block format reads: the ones with text, or all of them when the selection is on empty lines only.</summary>
    private static Row[] Considered(Row[] rows)
    {
        var used = rows.Where(r => !r.Blank).ToArray();
        return used.Length > 0 ? used : rows;
    }

    private static EditorEdit ToggleBlock(EditorAction action, int level, string text, int start, int length)
    {
        var rows = RowsOf(text, start, length);
        var used = Considered(rows);
        var edits = new List<Replace>();

        if (action == EditorAction.Blockquote)
        {
            var quoted = used.All(r => r.QuoteLength > 0);
            var hasText = rows.Any(r => !r.Blank);
            foreach (var r in rows)
            {
                if (quoted)
                {
                    if (r.QuoteLength > 0) edits.Add(new Replace(r.Start, r.Text.Length > 1 && r.Text[1] == ' ' ? 2 : 1, ""));
                }
                else
                {
                    // An empty line between lines of text keeps the quote going, so it carries a bare mark; one alone is ready to type in.
                    edits.Add(new Replace(r.Start, 0, r.Blank && hasText ? ">" : "> "));
                }
            }
        }
        else
        {
            var same = used.All(r => action switch
            {
                EditorAction.Heading => r.Kind == Marker.Heading && r.Level == level,
                EditorAction.BulletList => r.Kind == Marker.Bullet,
                EditorAction.OrderedList => r.Kind == Marker.Ordered,
                EditorAction.TaskList => r.Kind == Marker.Task,
                _ => r.Kind == Marker.None,
            });
            var number = 0;
            foreach (var r in used)
            {
                var wanted = same || action == EditorAction.Paragraph
                    ? ""
                    : action switch
                    {
                        EditorAction.Heading => new string('#', level) + " ",
                        EditorAction.BulletList => "- ",
                        EditorAction.OrderedList => $"{++number}. ",
                        _ => "- [ ] ",
                    };
                if (r.MarkerLength > 0 || wanted.Length > 0) edits.Add(new Replace(r.MarkerAt, r.MarkerLength, wanted));
            }
        }
        return Replacing(text, start, length, edits);
    }

    // ---- Code blocks, rules, links, images

    private static bool IsFence(string text, int start, int end) => text.AsSpan(start, end - start).TrimStart().StartsWith("```", StringComparison.Ordinal);

    /// <summary>The fenced block that holds a position: the offsets of the opening and closing fence lines (the closing one is -1 when the block is not closed).</summary>
    private static (int OpenStart, int OpenEnd, int CloseStart, int CloseEnd)? FenceAround(string text, int position)
    {
        var at = 0;
        var openStart = -1;
        var openEnd = -1;
        while (at <= text.Length)
        {
            var nl = text.IndexOf('\n', at);
            var end = nl < 0 ? text.Length : nl;
            if (IsFence(text, at, end))
            {
                if (openStart < 0)
                {
                    openStart = at;
                    openEnd = end;
                }
                else
                {
                    if (position >= openStart && position <= end) return (openStart, openEnd, at, end);
                    openStart = -1;
                }
            }
            if (nl < 0) break;
            at = nl + 1;
        }
        return openStart >= 0 && position >= openStart ? (openStart, openEnd, -1, -1) : null;
    }

    private static EditorEdit ToggleCodeBlock(string text, int start, int length)
    {
        if (FenceAround(text, start) is { } fence)
        {
            var (openStart, openEnd, closeStart, closeEnd) = fence;
            var opening = Math.Min(openEnd + 1, text.Length);
            if (closeStart < 0) return Replacing(text, start, length, [new(openStart, opening - openStart, "", false)]);
            if (closeStart <= opening)
            {
                // Two fence lines with nothing between them go in one piece, with a line break.
                var (from, to) = closeEnd < text.Length ? (openStart, closeEnd + 1) : (Math.Max(openStart - 1, 0), closeEnd);
                return Replacing(text, start, length, [new(from, to - from, "", false)]);
            }
            return Replacing(text, start, length, [new(openStart, opening - openStart, "", false), new(closeStart - 1, closeEnd - closeStart + 1, "", false)]);
        }
        var (ls, le) = BlockBounds(text, start, length);
        return Replacing(text, start, length, [new(ls, 0, "```\n"), new(le, 0, "\n```", false)]);
    }

    private static EditorEdit InsertRule(string text, int start, int length)
    {
        var (ls, le) = BlockBounds(text, start + length, 0);
        var blank = text.AsSpan(ls, le - ls).IsWhiteSpace();
        // The line above a rule must be empty, or the rule turns it into a heading.
        var previousStart = ls < 2 ? 0 : text.LastIndexOf('\n', ls - 2) + 1;
        var previousBlank = ls == 0 || text.AsSpan(previousStart, ls - 1 - previousStart).IsWhiteSpace();
        var insert = (blank ? (previousBlank ? "" : "\n") : "\n\n") + "---\n";
        return new EditorEdit(text[..le] + insert + text[le..], le + insert.Length, 0);
    }

    private static Match? LinkMatch(string text, int start, int length)
    {
        var (ls, le) = BlockBounds(text, start, 0);
        foreach (Match m in LinkPattern.Matches(text, ls))
        {
            if (m.Index >= le) break;
            if (m.Index <= start && start + length <= m.Index + m.Length) return m;
        }
        return null;
    }

    private static string EscapeLabel(string label) => label.Replace("[", "\\[").Replace("]", "\\]");

    private static string Unescape(string label) => label.Replace("\\[", "[").Replace("\\]", "]");

    private static string Destination(string address) => address.Replace("(", "%28").Replace(")", "%29");

    private static EditorEdit? SetLink(string text, int start, int length, string? address)
    {
        address = address?.Trim();
        var m = LinkMatch(text, start, length);
        if (string.IsNullOrEmpty(address))
        {
            if (m is null) return null;
            var plain = Unescape(m.Groups["text"].Value);
            return new EditorEdit(text[..m.Index] + plain + text[(m.Index + m.Length)..], m.Index, plain.Length);
        }

        var destination = Destination(address);
        if (m is not null)
        {
            var url = m.Groups["url"];
            return new EditorEdit(text[..url.Index] + destination + text[(url.Index + url.Length)..], m.Index + 1, m.Groups["text"].Length);
        }

        var selected = text.Substring(start, length);
        if (selected.Contains('\n')) return null;
        var label = EscapeLabel(length > 0 ? selected : address);
        return new EditorEdit(text[..start] + $"[{label}]({destination})" + text[(start + length)..], start + 1, label.Length);
    }

    private static EditorEdit? SetImage(string text, int start, int length, string? source)
    {
        source = source?.Trim();
        if (string.IsNullOrEmpty(source)) return null;
        var alt = text.Substring(start, length).Replace('\n', ' ').Trim();
        if (alt.Length == 0) alt = AltFor(source);
        if (alt.Length > 200) alt = alt[..200];
        var image = $"![{EscapeLabel(alt)}]({Destination(source)})";
        return new EditorEdit(text[..start] + image + text[(start + length)..], start + image.Length, 0);
    }

    /// <summary>The alt text of an image from the file name of its address: <c>team-photo.jpg</c> gives <c>team photo</c>.</summary>
    internal static string AltFor(string source)
    {
        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri)) return "";
        var name = Uri.UnescapeDataString(uri.Segments.LastOrDefault() ?? "").Trim('/');
        name = Regex.Replace(name, @"\.[A-Za-z0-9]{1,5}$", "", RegexOptions.CultureInvariant);
        name = Regex.Replace(name, @"[-_+]+", " ", RegexOptions.CultureInvariant).Trim();
        return name.Length > 200 ? name[..200] : name;
    }

    private static EditorEdit Clear(string text, int start, int length)
    {
        var selected = text.Substring(start, length);
        var cleaned = LinkPattern.Replace(selected, m => Unescape(m.Groups["text"].Value));
        cleaned = Marks.Replace(cleaned, "");
        var next = text[..start] + cleaned + text[(start + length)..];
        var edits = new List<Replace>();
        foreach (var r in RowsOf(next, start, cleaned.Length))
        {
            if (r.QuoteLength > 0) edits.Add(new Replace(r.Start, r.QuoteLength, ""));
            if (r.MarkerLength > 0) edits.Add(new Replace(r.MarkerAt, r.MarkerLength, ""));
        }
        return Replacing(next, start, cleaned.Length, edits);
    }

    // ---- Moving blocks

    /// <summary>The blocks of a text: runs of lines between blank lines. Blank lines inside a fenced block do not end it.</summary>
    private static List<(int Start, int End)> BlocksOf(string text)
    {
        var blocks = new List<(int, int)>();
        var at = 0;
        var fenced = false;
        var blockStart = -1;
        var blockEnd = -1;
        while (at <= text.Length)
        {
            var nl = text.IndexOf('\n', at);
            var end = nl < 0 ? text.Length : nl;
            if (!fenced && text.AsSpan(at, end - at).IsWhiteSpace())
            {
                if (blockStart >= 0) blocks.Add((blockStart, blockEnd));
                blockStart = -1;
            }
            else
            {
                if (blockStart < 0) blockStart = at;
                blockEnd = end;
                if (IsFence(text, at, end)) fenced = !fenced;
            }
            if (nl < 0) break;
            at = nl + 1;
        }
        if (blockStart >= 0) blocks.Add((blockStart, blockEnd));
        return blocks;
    }

    private static (bool Up, bool Down) CanMove(string text, int position)
    {
        var blocks = BlocksOf(text);
        var index = blocks.FindIndex(b => position >= b.Start && position <= b.End);
        return index < 0 ? (false, false) : (index > 0, index < blocks.Count - 1);
    }

    private static EditorEdit? Move(string text, int start, int length, int direction)
    {
        var blocks = BlocksOf(text);
        var index = blocks.FindIndex(b => start >= b.Start && start <= b.End);
        var other = index + direction;
        if (index < 0 || other < 0 || other >= blocks.Count) return null;

        var moved = blocks[index];
        var (a, b) = direction < 0 ? (blocks[other], moved) : (moved, blocks[other]);
        var gap = text[a.End..b.Start];
        var next = text[..a.Start] + text[b.Start..b.End] + gap + text[a.Start..a.End] + text[b.End..];
        var newStart = direction < 0 ? a.Start : a.Start + (b.End - b.Start) + gap.Length;
        return new EditorEdit(next, newStart + (start - moved.Start), Math.Min(length, moved.End - start));
    }

    // ---- Replacements

    private static int Map(int position, List<Replace> edits)
    {
        var shift = 0;
        foreach (var r in edits)
        {
            if (position < r.At || (position == r.At && !(r.Remove == 0 && r.After))) break;
            if (position >= r.At + r.Remove) shift += r.Insert.Length - r.Remove;
            else return r.At + shift + Math.Min(position - r.At, r.Insert.Length);
        }
        return position + shift;
    }

    /// <summary>Applies replacements in ascending order and carries the selection through them.</summary>
    private static EditorEdit Replacing(string text, int start, int length, List<Replace> edits)
    {
        var sb = new StringBuilder(text.Length + 16);
        var at = 0;
        foreach (var r in edits)
        {
            sb.Append(text, at, r.At - at).Append(r.Insert);
            at = r.At + r.Remove;
        }
        sb.Append(text, at, text.Length - at);
        var from = Map(start, edits);
        var to = Map(start + length, edits);
        return new EditorEdit(sb.ToString(), from, Math.Max(0, to - from));
    }
}
