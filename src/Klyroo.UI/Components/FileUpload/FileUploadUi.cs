using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyFileUpload"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record FileUploadUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyFileUpload"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>dropzone</c> slot of <see cref="KlyFileUpload"/>, after the theme and the app configuration.</summary>
    public string? Dropzone { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyFileUpload"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyFileUpload"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyFileUpload"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>files</c> slot of <see cref="KlyFileUpload"/>, after the theme and the app configuration.</summary>
    public string? Files { get; init; }
    /// <summary>Classes merged into the <c>file</c> slot of <see cref="KlyFileUpload"/>, after the theme and the app configuration.</summary>
    public string? File { get; init; }
    /// <summary>Classes merged into the <c>fileName</c> slot of <see cref="KlyFileUpload"/>, after the theme and the app configuration.</summary>
    public string? FileName { get; init; }
    /// <summary>Classes merged into the <c>fileSize</c> slot of <see cref="KlyFileUpload"/>, after the theme and the app configuration.</summary>
    public string? FileSize { get; init; }
    /// <summary>Classes merged into the <c>remove</c> slot of <see cref="KlyFileUpload"/>, after the theme and the app configuration.</summary>
    public string? Remove { get; init; }
    /// <summary>Classes merged into the <c>error</c> slot of <see cref="KlyFileUpload"/>, after the theme and the app configuration.</summary>
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
