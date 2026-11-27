namespace Klyroo.UI.Maui;

/// <summary>State of a <see cref="KlyChatAttachment"/>.</summary>
public enum ChatAttachmentStatus
{
    /// <summary>The file is ready.</summary>
    Idle,
    /// <summary>The file is being uploaded.</summary>
    Uploading,
    /// <summary>The upload failed.</summary>
    Error,
}
