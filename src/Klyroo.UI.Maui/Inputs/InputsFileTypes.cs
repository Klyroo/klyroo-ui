namespace Klyroo.UI.Maui;

/// <summary>
/// The <c>accept</c> list of a file upload (<c>.png,image/*</c>), read for the three ways the platforms name a file type: extensions (Windows),
/// MIME types (Android) and type identifiers (iOS and Mac Catalyst). A token the table does not know is only used where it already is in the right form.
/// </summary>
internal static class InputsFileTypes
{
    private static readonly (string Extension, string Mime, string Uti)[] Known =
    [
        (".png", "image/png", "public.png"),
        (".jpg", "image/jpeg", "public.jpeg"),
        (".jpeg", "image/jpeg", "public.jpeg"),
        (".gif", "image/gif", "com.compuserve.gif"),
        (".webp", "image/webp", "org.webmproject.webp"),
        (".svg", "image/svg+xml", "public.svg-image"),
        (".bmp", "image/bmp", "com.microsoft.bmp"),
        (".pdf", "application/pdf", "com.adobe.pdf"),
        (".txt", "text/plain", "public.plain-text"),
        (".csv", "text/csv", "public.comma-separated-values-text"),
        (".json", "application/json", "public.json"),
        (".zip", "application/zip", "public.zip-archive"),
        (".doc", "application/msword", "com.microsoft.word.doc"),
        (".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "org.openxmlformats.wordprocessingml.document"),
        (".xls", "application/vnd.ms-excel", "com.microsoft.excel.xls"),
        (".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "org.openxmlformats.spreadsheetml.sheet"),
        (".mp3", "audio/mpeg", "public.mp3"),
        (".mp4", "video/mp4", "public.mpeg-4"),
    ];

    private static readonly Dictionary<string, (string Uti, string[] Extensions)> Families = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/*"] = ("public.image", [".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".svg"]),
        ["video/*"] = ("public.movie", [".mp4", ".mov", ".avi", ".mkv", ".webm"]),
        ["audio/*"] = ("public.audio", [".mp3", ".wav", ".m4a", ".ogg", ".flac"]),
        ["text/*"] = ("public.text", [".txt", ".csv", ".md"]),
    };

    /// <summary>The accept list as extensions, MIME types and type identifiers. Each list is empty when nothing in the accept text can be said that way.</summary>
    public static (List<string> Extensions, List<string> Mimes, List<string> Utis) Parse(string? accept)
    {
        List<string> extensions = [];
        List<string> mimes = [];
        List<string> utis = [];
        foreach (var token in (accept ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (token.StartsWith('.'))
            {
                Add(extensions, token.ToLowerInvariant());
                foreach (var known in Known.Where(k => k.Extension == token.ToLowerInvariant()))
                {
                    Add(mimes, known.Mime);
                    Add(utis, known.Uti);
                }
            }
            else if (Families.TryGetValue(token, out var family))
            {
                Add(mimes, token.ToLowerInvariant());
                Add(utis, family.Uti);
                foreach (var extension in family.Extensions) Add(extensions, extension);
            }
            else if (token.Contains('/'))
            {
                Add(mimes, token.ToLowerInvariant());
                foreach (var known in Known.Where(k => string.Equals(k.Mime, token, StringComparison.OrdinalIgnoreCase)))
                {
                    Add(extensions, known.Extension);
                    Add(utis, known.Uti);
                }
            }
        }
        return (extensions, mimes, utis);
    }

    /// <summary>The picker filter of an accept list, or <c>null</c> to show every file.</summary>
    public static FilePickerFileType? ToPickerType(string? accept)
    {
        var (extensions, mimes, utis) = Parse(accept);
        if (extensions.Count == 0 && mimes.Count == 0 && utis.Count == 0) return null;
        var types = new Dictionary<DevicePlatform, IEnumerable<string>>();
        if (mimes.Count > 0) types[DevicePlatform.Android] = mimes;
        if (utis.Count > 0)
        {
            types[DevicePlatform.iOS] = utis;
            types[DevicePlatform.MacCatalyst] = utis;
        }
        if (extensions.Count > 0) types[DevicePlatform.WinUI] = extensions;
        return new FilePickerFileType(types);
    }

    /// <summary>The MIME type of a picked file: from its extension when the table knows it, else the one the platform reports (the platform has none to report on a plain build).</summary>
    public static string? ContentTypeOf(FileResult file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        foreach (var known in Known)
        {
            if (known.Extension == extension) return known.Mime;
        }
        try
        {
            return file.ContentType;
        }
        catch (NotImplementedException)
        {
            return null;
        }
    }

    /// <summary>Whether a file is in the accept list: an extension (<c>.png</c>) matches the end of the name, a family (<c>image/*</c>) the start of the type, any other type the whole of it.</summary>
    public static bool Matches(string fileName, string? contentType, string? accept)
    {
        var tokens = (accept ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0) return true;
        var type = contentType ?? string.Empty;
        foreach (var token in tokens)
        {
            var matches = token.StartsWith('.') ? fileName.EndsWith(token, StringComparison.OrdinalIgnoreCase)
                : token.EndsWith("/*", StringComparison.Ordinal) ? type.StartsWith(token[..^1], StringComparison.OrdinalIgnoreCase)
                : string.Equals(type, token, StringComparison.OrdinalIgnoreCase);
            if (matches) return true;
        }
        return false;
    }

    private static void Add(List<string> list, string value)
    {
        if (!list.Contains(value)) list.Add(value);
    }
}
