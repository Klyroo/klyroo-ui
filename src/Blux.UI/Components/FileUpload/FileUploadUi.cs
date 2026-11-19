using Blux.UI.Theming;

namespace Blux.UI;

public sealed record FileUploadUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Dropzone { get; init; }
    public string? Icon { get; init; }
    public string? Label { get; init; }
    public string? Description { get; init; }
    public string? Files { get; init; }
    public string? File { get; init; }
    public string? FileName { get; init; }
    public string? FileSize { get; init; }
    public string? Remove { get; init; }
    public string? Error { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "dropzone" => Dropzone,
        "icon" => Icon,
        "label" => Label,
        "description" => Description,
        "files" => Files,
        "file" => File,
        "fileName" => FileName,
        "fileSize" => FileSize,
        "remove" => Remove,
        "error" => Error,
        _ => null,
    };
}
