using System.Globalization;
using System.Text;

namespace Blux.UI.Tools;

/// <summary>Parsed CSV preview table returned by <see cref="FilePreviewService.ParseCsv"/>.</summary>
public sealed record ParsedCsvPreview(
    char Delimiter,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyList<bool> NumericColumns,
    bool Truncated);

/// <summary>
/// Scoped service managing active file preview overlays rendered by <see cref="BxFilePreviewProvider"/>.
/// Also provides deterministic file-kind detection, CSV parsing, and size/extension formatting utilities.
/// </summary>
public sealed class FilePreviewService
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "apng", "avif", "bmp", "gif", "heic", "heif", "ico", "jpeg", "jpg", "png", "svg", "webp"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "m4v", "mov", "mp4", "ogv", "webm"
    };

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "aac", "flac", "m4a", "mp3", "oga", "ogg", "opus", "wav", "weba"
    };

    private static readonly HashSet<string> CsvExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "csv", "tsv"
    };

    private static readonly HashSet<string> MarkdownExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "markdown", "md", "mdx"
    };

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "c", "cc", "cfg", "cmd", "conf", "cpp", "cs", "css", "dockerfile", "env", "go", "graphql", "h", "hpp",
        "htm", "html", "http", "ini", "java", "js", "json", "jsonc", "jsx", "kt", "less", "log", "lua", "makefile",
        "mjs", "php", "pl", "properties", "ps1", "py", "r", "razor", "rb", "rs", "sass", "scala", "scss", "sh",
        "sql", "srt", "svelte", "swift", "toml", "ts", "tsx", "txt", "vtt", "vue", "xml", "yaml", "yml", "zsh"
    };

    private static readonly HashSet<string> OfficeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "doc", "docx", "key", "numbers", "odp", "ods", "odt", "pages", "ppt", "pptx", "rtf", "xls", "xlsx"
    };

    private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "7z", "bz2", "gz", "rar", "tar", "tgz", "xz", "zip"
    };

    private IReadOnlyList<FilePreviewFile> _files = [];
    private FilePreviewOptions _options = new();

    /// <summary>Raised whenever the open state, active file list, or active index changes.</summary>
    public event Action? Changed;

    /// <summary>Whether a <see cref="BxFilePreviewProvider"/> is currently mounted in the component tree.</summary>
    public bool IsMounted { get; private set; }

    /// <summary>Whether the file preview overlay is currently open.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>Files in the active gallery.</summary>
    public IReadOnlyList<FilePreviewFile> Files => _files;

    /// <summary>Zero-based index of the currently shown file in <see cref="Files"/>.</summary>
    public int CurrentIndex { get; private set; }

    /// <summary>Current options for the active preview session.</summary>
    public FilePreviewOptions Options => _options;

    /// <summary>Currently active file, or <see langword="null"/> when closed.</summary>
    public FilePreviewFile? CurrentFile =>
        IsOpen && CurrentIndex >= 0 && CurrentIndex < _files.Count ? _files[CurrentIndex] : null;

    internal void AttachProvider()
    {
        IsMounted = true;
    }

    internal void DetachProvider()
    {
        IsMounted = false;
    }

    /// <summary>Opens a single file in <see cref="BxFilePreviewProvider"/>.</summary>
    public void Open(FilePreviewFile file, FilePreviewOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(file);
        OpenGallery([file], options);
    }

    /// <summary>Opens a gallery of files in <see cref="BxFilePreviewProvider"/>.</summary>
    public void OpenGallery(IReadOnlyList<FilePreviewFile> files, FilePreviewOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(files);
        if (files.Count == 0) return;
        _files = files.ToList();
        _options = options ?? new FilePreviewOptions();
        CurrentIndex = Math.Clamp(_options.Index, 0, _files.Count - 1);
        IsOpen = true;
        Changed?.Invoke();
    }

    /// <summary>Closes the active file preview overlay.</summary>
    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        Changed?.Invoke();
    }

    /// <summary>Moves to the next file in the gallery.</summary>
    public void Next()
    {
        if (!IsOpen || _files.Count <= 1) return;
        if (CurrentIndex + 1 < _files.Count)
        {
            CurrentIndex++;
            Changed?.Invoke();
        }
        else if (_options.Loop)
        {
            CurrentIndex = 0;
            Changed?.Invoke();
        }
    }

    /// <summary>Moves to the previous file in the gallery.</summary>
    public void Previous()
    {
        if (!IsOpen || _files.Count <= 1) return;
        if (CurrentIndex - 1 >= 0)
        {
            CurrentIndex--;
            Changed?.Invoke();
        }
        else if (_options.Loop)
        {
            CurrentIndex = _files.Count - 1;
            Changed?.Invoke();
        }
    }

    /// <summary>Jumps to a specific zero-based index in the gallery.</summary>
    public void GoTo(int index)
    {
        if (!IsOpen || _files.Count == 0) return;
        var next = _options.Loop
            ? ((index % _files.Count) + _files.Count) % _files.Count
            : Math.Clamp(index, 0, _files.Count - 1);
        if (next == CurrentIndex) return;
        CurrentIndex = next;
        Changed?.Invoke();
    }

    /// <summary>Resolves the largest container across all files in the current gallery unless overridden in <see cref="Options"/>.</summary>
    public FilePreviewContainer ResolveContainer()
    {
        if (_options.Mode is { } explicitMode) return explicitMode;
        if (_files.Count == 0) return FilePreviewContainer.Modal;
        var max = FilePreviewContainer.Modal;
        foreach (var f in _files)
        {
            var effective = f.Rendition ?? f;
            var kind = DetectKind(effective);
            var suggested = DefaultContainerForKind(kind);
            if (suggested == FilePreviewContainer.Fullscreen) return FilePreviewContainer.Fullscreen;
            if (suggested == FilePreviewContainer.Drawer) max = FilePreviewContainer.Drawer;
        }
        return max;
    }

    /// <summary>Returns the default container mode for a <see cref="FilePreviewKind"/>.</summary>
    public static FilePreviewContainer DefaultContainerForKind(FilePreviewKind kind) => kind switch
    {
        FilePreviewKind.Pdf or FilePreviewKind.Csv or FilePreviewKind.Markdown or FilePreviewKind.Text => FilePreviewContainer.Drawer,
        _ => FilePreviewContainer.Modal,
    };

    /// <summary>Detects the <see cref="FilePreviewKind"/> of a <see cref="FilePreviewFile"/> (checking explicit kind, MIME type, then file extension).</summary>
    public static FilePreviewKind DetectKind(FilePreviewFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (file.Kind is { } explicitKind) return explicitKind;
        return DetectKind(file.Name ?? FileNameFromUrl(file.Src), file.Mime);
    }

    /// <summary>Detects the <see cref="FilePreviewKind"/> from a file name and optional MIME type.</summary>
    public static FilePreviewKind DetectKind(string? fileNameOrUrl, string? mime = null)
    {
        if (!string.IsNullOrWhiteSpace(mime))
        {
            var m = mime.Split(';')[0].Trim().ToLowerInvariant();
            if (m == "application/pdf") return FilePreviewKind.Pdf;
            if (m.StartsWith("image/", StringComparison.Ordinal)) return FilePreviewKind.Image;
            if (m.StartsWith("video/", StringComparison.Ordinal)) return FilePreviewKind.Video;
            if (m.StartsWith("audio/", StringComparison.Ordinal)) return FilePreviewKind.Audio;
            if (m is "text/csv" or "text/tab-separated-values" or "application/csv") return FilePreviewKind.Csv;
            if (m is "text/markdown" or "text/x-markdown") return FilePreviewKind.Markdown;
            if (m.StartsWith("text/", StringComparison.Ordinal)
                || m is "application/json" or "application/ld+json" or "application/xml" or "application/javascript"
                    or "application/x-yaml" or "application/sql")
            {
                return FilePreviewKind.Text;
            }
            if (m.Contains("officedocument", StringComparison.Ordinal)
                || m.Contains("msword", StringComparison.Ordinal)
                || m.Contains("ms-excel", StringComparison.Ordinal)
                || m.Contains("ms-powerpoint", StringComparison.Ordinal)
                || m.Contains("opendocument", StringComparison.Ordinal)
                || m == "application/rtf")
            {
                return FilePreviewKind.Office;
            }
            if (m is "application/zip" or "application/x-tar" or "application/gzip" or "application/x-7z-compressed"
                or "application/x-rar-compressed" or "application/x-bzip2")
            {
                return FilePreviewKind.Archive;
            }
        }

        var ext = ExtractExtension(fileNameOrUrl);
        if (!string.IsNullOrEmpty(ext))
        {
            if (ext.Equals("pdf", StringComparison.OrdinalIgnoreCase)) return FilePreviewKind.Pdf;
            if (ImageExtensions.Contains(ext)) return FilePreviewKind.Image;
            if (VideoExtensions.Contains(ext)) return FilePreviewKind.Video;
            if (AudioExtensions.Contains(ext)) return FilePreviewKind.Audio;
            if (CsvExtensions.Contains(ext)) return FilePreviewKind.Csv;
            if (MarkdownExtensions.Contains(ext)) return FilePreviewKind.Markdown;
            if (TextExtensions.Contains(ext)) return FilePreviewKind.Text;
            if (OfficeExtensions.Contains(ext)) return FilePreviewKind.Office;
            if (ArchiveExtensions.Contains(ext)) return FilePreviewKind.Archive;
        }

        return FilePreviewKind.Other;
    }

    /// <summary>Extracts the lowercase extension (without leading dot) from a file name or URL.</summary>
    public static string? ExtractExtension(string? fileNameOrUrl)
    {
        if (string.IsNullOrWhiteSpace(fileNameOrUrl)) return null;
        var clean = fileNameOrUrl.Split('?', '#')[0];
        var slash = clean.LastIndexOf('/');
        var name = slash >= 0 ? clean[(slash + 1)..] : clean;
        var dot = name.LastIndexOf('.');
        if (dot <= 0 || dot == name.Length - 1) return null;
        return name[(dot + 1)..].ToLowerInvariant();
    }

    /// <summary>Extracts a display file name from a URL or returns <c>"Untitled"</c>.</summary>
    public static string FileNameFromUrl(string? src)
    {
        if (string.IsNullOrWhiteSpace(src)
            || src.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || src.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
        {
            return "Untitled";
        }
        var clean = src.Split('?', '#')[0].TrimEnd('/');
        var slash = clean.LastIndexOf('/');
        var segment = slash >= 0 ? clean[(slash + 1)..] : clean;
        return string.IsNullOrWhiteSpace(segment) ? "Untitled" : Uri.UnescapeDataString(segment);
    }

    /// <summary>Returns a short uppercase badge label for a file's extension or MIME subtype.</summary>
    public static string ExtensionLabel(string? fileName, string? mime = null)
    {
        var ext = ExtractExtension(fileName);
        if (!string.IsNullOrEmpty(ext)) return ext.ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(mime))
        {
            var slash = mime.IndexOf('/');
            if (slash >= 0 && slash + 1 < mime.Length)
            {
                var sub = mime[(slash + 1)..].Split(';', '+')[0].Trim();
                if (!string.IsNullOrEmpty(sub)) return sub.ToUpperInvariant();
            }
        }
        return "—";
    }

    /// <summary>Formats a byte count into a human-readable size string (<c>B</c>, <c>KB</c>, <c>MB</c>, <c>GB</c>).</summary>
    public static string FormatFileSize(long bytes, CultureInfo? culture = null)
    {
        var c = culture ?? CultureInfo.InvariantCulture;
        if (bytes < 1024) return string.Create(c, $"{bytes} B");
        var kb = bytes / 1024.0;
        if (kb < 1024) return kb < 10 ? kb.ToString("0.0", c) + " KB" : Math.Round(kb).ToString("0", c) + " KB";
        var mb = kb / 1024.0;
        if (mb < 1024) return mb < 10 ? mb.ToString("0.0", c) + " MB" : Math.Round(mb).ToString("0", c) + " MB";
        var gb = mb / 1024.0;
        return gb < 10 ? gb.ToString("0.0", c) + " GB" : Math.Round(gb).ToString("0", c) + " GB";
    }

    /// <summary>
    /// Parses CSV or TSV content according to RFC 4180 (quoted fields, escaped quotes, embedded newlines)
    /// and auto-detects the delimiter among <c>,</c>, <c>;</c>, <c>\t</c>, and <c>|</c>.
    /// </summary>
    public static ParsedCsvPreview ParseCsv(string? content, string? fileName = null, int maxRows = 1000)
    {
        if (string.IsNullOrEmpty(content))
        {
            return new ParsedCsvPreview(',', [], [], [], false);
        }

        var text = content[0] == '\uFEFF' ? content[1..] : content;
        var ext = ExtractExtension(fileName);
        var delimiter = string.Equals(ext, "tsv", StringComparison.OrdinalIgnoreCase)
            ? '\t'
            : DetectCsvDelimiter(text);

        var allRows = new List<List<string>>();
        var currentRow = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var truncated = false;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(ch);
                }
            }
            else if (ch == '"')
            {
                inQuotes = true;
            }
            else if (ch == delimiter)
            {
                currentRow.Add(field.ToString());
                field.Clear();
            }
            else if (ch is '\r' or '\n')
            {
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                currentRow.Add(field.ToString());
                field.Clear();
                if (currentRow.Count > 1 || (currentRow.Count == 1 && currentRow[0].Length > 0))
                {
                    allRows.Add(currentRow);
                    if (allRows.Count > maxRows + 1)
                    {
                        truncated = true;
                        break;
                    }
                }
                currentRow = [];
            }
            else
            {
                field.Append(ch);
            }
        }

        if (!truncated && (field.Length > 0 || currentRow.Count > 0))
        {
            currentRow.Add(field.ToString());
            if (currentRow.Count > 1 || currentRow[0].Length > 0)
            {
                allRows.Add(currentRow);
                if (allRows.Count > maxRows + 1) truncated = true;
            }
        }

        if (allRows.Count == 0)
        {
            return new ParsedCsvPreview(delimiter, [], [], [], false);
        }

        var headers = allRows[0];
        var dataRows = allRows.Skip(1).Take(maxRows).Select(r => (IReadOnlyList<string>)r).ToList();
        var numericCols = new bool[headers.Count];
        for (var col = 0; col < headers.Count; col++)
        {
            var hasNonEmpty = false;
            var allNumeric = true;
            foreach (var row in dataRows)
            {
                if (col >= row.Count || string.IsNullOrWhiteSpace(row[col])) continue;
                hasNonEmpty = true;
                var val = row[col].Trim().TrimEnd('%').Replace(" ", "", StringComparison.Ordinal).Replace("\u00A0", "", StringComparison.Ordinal);
                if (!double.TryParse(val.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                {
                    allNumeric = false;
                    break;
                }
            }
            numericCols[col] = hasNonEmpty && allNumeric;
        }

        return new ParsedCsvPreview(delimiter, headers, dataRows, numericCols, truncated);
    }

    private static char DetectCsvDelimiter(string text)
    {
        ReadOnlySpan<char> candidates = [',', ';', '\t', '|'];
        Span<int> counts = stackalloc int[4];
        var inQuotes = false;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (!inQuotes)
            {
                if (ch is '\r' or '\n') break;
                for (var c = 0; c < candidates.Length; c++)
                {
                    if (ch == candidates[c]) counts[c]++;
                }
            }
        }

        var best = ',';
        var bestCount = 0;
        for (var c = 0; c < candidates.Length; c++)
        {
            if (counts[c] > bestCount)
            {
                bestCount = counts[c];
                best = candidates[c];
            }
        }
        return best;
    }
}
