using System.Globalization;

namespace Blux.UI.Tools;

/// <summary>Built-in file preview renderer kinds supported by <see cref="BxFilePreviewProvider"/> and <see cref="FilePreviewService"/>.</summary>
public enum FilePreviewKind
{
    Pdf,
    Image,
    Video,
    Audio,
    Csv,
    Markdown,
    Text,
    Office,
    Archive,
    Other,
}

/// <summary>Overlay container mode for <see cref="BxFilePreviewProvider"/>.</summary>
public enum FilePreviewContainer
{
    Modal,
    Drawer,
    Fullscreen,
}

/// <summary>Metadata key/value detail shown in the file preview details panel.</summary>
public sealed record FilePreviewDetail(string Label, string Value);

/// <summary>Custom action shown in the file preview header.</summary>
public sealed class FilePreviewAction
{
    public required string Label { get; init; }
    public string? Icon { get; init; }
    public string? Href { get; init; }
    public Func<FilePreviewFile, Task>? OnSelect { get; init; }
}

/// <summary>Describes a file to preview in <see cref="BxFilePreviewProvider"/> via <see cref="FilePreviewService"/>.</summary>
public sealed class FilePreviewFile
{
    /// <summary>URL or data URI of the file.</summary>
    public required string Src { get; init; }

    /// <summary>Optional stable identifier in a gallery.</summary>
    public string? Id { get; init; }

    /// <summary>Display file name. Defaults to the last path segment of <see cref="Src"/>.</summary>
    public string? Name { get; init; }

    /// <summary>Optional MIME type (takes precedence over the file extension when detecting the renderer kind).</summary>
    public string? Mime { get; init; }

    /// <summary>Optional explicit renderer kind overriding automatic detection.</summary>
    public FilePreviewKind? Kind { get; init; }

    /// <summary>Optional file size in bytes.</summary>
    public long? Size { get; init; }

    /// <summary>Optional last-modified timestamp.</summary>
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>Optional poster image URL for video or audio files.</summary>
    public string? Poster { get; init; }

    /// <summary>Optional inline text content (for CSV, Markdown, JSON or code files when already loaded in memory).</summary>
    public string? Content { get; init; }

    /// <summary>Optional rendition file (for example a PDF preview of an Office document) used for the stage while keeping the original file for download.</summary>
    public FilePreviewFile? Rendition { get; init; }

    /// <summary>Optional metadata details displayed in the file details drawer.</summary>
    public IReadOnlyList<FilePreviewDetail>? Details { get; init; }

    /// <summary>Optional custom download URL (defaults to <see cref="Src"/>).</summary>
    public string? DownloadUrl { get; init; }

    /// <summary>Optional custom actions in the preview header.</summary>
    public IReadOnlyList<FilePreviewAction>? Actions { get; init; }

    /// <summary>Optional video caption tracks.</summary>
    public IReadOnlyList<VideoPlayerTrack>? Tracks { get; init; }
}

/// <summary>Options when opening a file or gallery in <see cref="FilePreviewService"/>.</summary>
public sealed class FilePreviewOptions
{
    /// <summary>Initial zero-based index in the file list.</summary>
    public int Index { get; init; }

    /// <summary>Optional container override (<see cref="FilePreviewContainer.Modal"/>, <see cref="FilePreviewContainer.Drawer"/> or <see cref="FilePreviewContainer.Fullscreen"/>).</summary>
    public FilePreviewContainer? Mode { get; init; }

    /// <summary>Wraps around at the first and last file when navigating with Previous / Next (<see langword="false"/> by default).</summary>
    public bool Loop { get; init; }

    /// <summary>Initial PDF page number (<c>#page=N</c>).</summary>
    public int? PdfPage { get; init; }
}
