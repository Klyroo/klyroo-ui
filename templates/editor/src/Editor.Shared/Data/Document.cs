using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Editor.Shared.Data;

/// <summary>A document: a title and the HTML the editor wrote. The HTML a document holds has always been through <see cref="DocumentHtml.Clean"/>.</summary>
public sealed class Document
{
    public const string UntitledTitle = "Untitled";

    public string Id { get; set; } = NewId();

    public string Title { get; set; } = UntitledTitle;

    /// <summary>True until the user renames the document: the title then follows the first line of the text.</summary>
    public bool AutoTitle { get; set; } = true;

    public string Html { get; set; } = "";

    /// <summary>
    /// False while only what the list needs is known (the id, the title, the dates and <see cref="HtmlLength">how long</see> the text is): <see cref="Html"/>, <see cref="Text"/> and
    /// <see cref="Words"/> are then empty and the text is read when the document is opened, or again when another tab changed it.
    /// </summary>
    [JsonIgnore]
    public bool BodyLoaded { get; internal set; } = true;

    /// <summary>The characters of the HTML, known without reading the text. It is what the bound on the total counts.</summary>
    [JsonIgnore]
    public int HtmlLength { get; internal set; }

    /// <summary>True once the store holds the document (it was read from it or written to it). A document the store refused to write exists in this visit only.</summary>
    [JsonIgnore]
    internal bool Persisted { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>The text of the document, for the search and the word count. Not stored: it is read again from <see cref="Html"/>.</summary>
    [JsonIgnore]
    public string Text { get; private set; } = "";

    [JsonIgnore]
    public int Words { get; private set; }

    /// <summary>Replaces the HTML (cleaned here) and what is derived from it. Returns the cleaned HTML.</summary>
    public string SetHtml(string? html)
    {
        Html = DocumentHtml.Clean(html);
        HtmlLength = Html.Length;
        BodyLoaded = true;
        Text = DocumentHtml.ToText(Html);
        Words = DocumentHtml.CountWords(Text);
        return Html;
    }

    /// <summary>Takes the title and the text (with what is derived from it) of <paramref name="other"/>, which has been through <see cref="SetHtml"/>.</summary>
    internal void Adopt(Document other)
    {
        Title = other.Title;
        Html = other.Html;
        Text = other.Text;
        Words = other.Words;
        HtmlLength = other.HtmlLength;
        BodyLoaded = true;
    }

    public static string NewId() => Guid.NewGuid().ToString("N");

    /// <summary>The title a document takes from the first line of its text: one line, cut at a word. Untitled when there is no text.</summary>
    public static string TitleFrom(string text)
    {
        var firstLine = text.Split('\n', 2)[0];
        var line = Regex.Replace(firstLine, @"\s+", " ").Trim();
        if (line.Length == 0)
        {
            return UntitledTitle;
        }
        if (line.Length <= DocumentLimits.MaxTitle)
        {
            return line;
        }
        var cut = line[..DocumentLimits.MaxTitle];
        var space = cut.LastIndexOf(' ');
        return (space > DocumentLimits.MaxTitle / 2 ? cut[..space] : cut).TrimEnd() + "...";
    }
}

/// <summary>The bounds of what a browser keeps: its storage is a few megabytes shared by every app of the site, and what it holds can be edited by the user.</summary>
public static class DocumentLimits
{
    /// <summary>The documents kept. Creating one beyond it deletes the least recently used.</summary>
    public const int MaxDocuments = 100;

    public const int MaxTitle = 80;

    /// <summary>The characters of the HTML of one document. A change that would pass it is not saved, and the editor says so.</summary>
    public const int MaxHtmlCharacters = 150_000;

    /// <summary>
    /// The characters of the HTML of every document together. The storage of a browser holds about five million characters for the whole site, so this leaves
    /// room for the other apps of the same site (the live demos share one). The least recently used documents go when a save would pass it.
    /// </summary>
    public const long MaxTotalHtmlCharacters = 1_000_000;

    /// <summary>The documents read from the storage at most: what is past it is not ours to keep and is removed (the storage of the site can hold anything).</summary>
    public const int MaxDocumentsRead = 200;

    /// <summary>
    /// The largest message the browser may send to the server over the circuit of the Server render mode, in bytes. Blazor Server accepts 32 KB by default, which a document
    /// of a few thousand words passes (the editor reports the whole text), so the host raises it to this (<c>Program.cs</c>) and the README gives the arithmetic. It is the
    /// bound for what travels: one change of one document (<see cref="MaxHtmlCharacters"/> characters, at most three bytes each in UTF-8, which is 450,000 bytes in
    /// the worst ordinary case: all CJK text) and the text of one document read back from the storage (<see cref="MaxTransferBytes"/>). The WebAssembly and MAUI hosts
    /// have no such limit.
    /// </summary>
    public const int MaxReceiveMessageBytes = 512 * 1024;

    /// <summary>
    /// What one stored document may weigh on the wire when the storage hands it to the server: the length of its JSON as the browser writes it into an interop result (a quote or a
    /// backslash takes two bytes, a control character six, a letter outside ASCII two or three). 16 KB under <see cref="MaxReceiveMessageBytes"/> is left for the envelope of the
    /// message. A save that would pass it is refused like one that is too long, so no document in the storage is one the app could not read back.
    /// </summary>
    public const int MaxTransferBytes = MaxReceiveMessageBytes - 16 * 1024;

    /// <summary>The share of a bound from which the app warns that it is getting full.</summary>
    public const double WarnFraction = 0.9;
}

/// <summary>The documents as JSON, and the way back: what comes out of the storage is untrusted and is checked field by field.</summary>
public static partial class DocumentJson
{
    [GeneratedRegex("^[a-f0-9]{32}$")]
    private static partial Regex IdPattern();

    public static bool IsValidId(string? id) => id is not null && IdPattern().IsMatch(id);

    // Storage-only JSON: it never goes into HTML, so non-ASCII text is written as it is.
    private static readonly DocumentContext Context = new(new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

    /// <summary>The most characters a stored document can be written with: the HTML and the title, at the worst six characters for each (a quote is escaped), and a little metadata.</summary>
    public const int MaxJsonCharacters = 6 * (DocumentLimits.MaxHtmlCharacters + DocumentLimits.MaxTitle) + 1024;

    public static string Write(Document document) =>
        JsonSerializer.Serialize(document, Context.Document);

    /// <summary>
    /// The bytes a text takes when the browser sends it to the server as the result of an interop call: it is written as a JSON string (<c>JSON.stringify</c>), which
    /// escapes a quote and a backslash with a second character and a control character with up to six, then as UTF-8. The same count is made in the browser
    /// (<c>host.js</c>), so a document the app writes is one it can read back.
    /// </summary>
    public static long TransferBytes(string text)
    {
        // The two quotes of the string; the envelope of the message is covered by the margin of DocumentLimits.MaxTransferBytes.
        long bytes = 2;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c is '\"' or '\\')
            {
                bytes += 2;
            }
            else if (c < 0x20)
            {
                bytes += c is '\b' or '\f' or '\n' or '\r' or '\t' ? 2 : 6;
            }
            else if (c < 0x80)
            {
                bytes += 1;
            }
            else if (c < 0x800)
            {
                bytes += 2;
            }
            else if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                // A pair is four bytes of UTF-8.
                bytes += 4;
                i++;
            }
            else if (char.IsSurrogate(c))
            {
                // A lone surrogate is written as \udXXX.
                bytes += 6;
            }
            else
            {
                bytes += 3;
            }
        }
        return bytes;
    }

    /// <summary>
    /// Reads a document. False when the text is not one, or is longer than what the app writes. The HTML is cleaned again, so a hand-edited file cannot put anything on
    /// the page that the app would not have written.
    /// </summary>
    public static bool TryRead(string? json, out Document document)
    {
        document = new Document();
        if (string.IsNullOrWhiteSpace(json) || json.Length > MaxJsonCharacters)
        {
            return false;
        }
        Document? read;
        try
        {
            read = JsonSerializer.Deserialize(json, Context.Document);
        }
        catch (JsonException)
        {
            return false;
        }
        if (read is null || !IsValidId(read.Id) || (read.Html?.Length ?? 0) > DocumentLimits.MaxHtmlCharacters)
        {
            return false;
        }
        document = new Document
        {
            Id = read.Id,
            Title = ReadTitle(read.Title),
            AutoTitle = read.AutoTitle,
            CreatedAt = Clamp(read.CreatedAt),
            UpdatedAt = Clamp(read.UpdatedAt),
        };
        document.SetHtml(read.Html);
        return true;
    }

    internal static string ReadTitle(string? title)
    {
        var line = Regex.Replace(title ?? "", @"\s+", " ").Trim();
        if (line.Length == 0)
        {
            return Document.UntitledTitle;
        }
        return line.Length <= DocumentLimits.MaxTitle ? line : line[..DocumentLimits.MaxTitle].TrimEnd();
    }

    // A date from a hand-edited file: the year 1 or 9999 would break the grouping by day.
    internal static DateTimeOffset Clamp(DateTimeOffset value) =>
        value.Year is < 2000 or > 2100 ? DateTimeOffset.UtcNow : value;
}

/// <summary>Source-generated JSON for the stored documents: no reflection, so it survives trimming in the WebAssembly app and in the MAUI app.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Document))]
internal sealed partial class DocumentContext : JsonSerializerContext;
