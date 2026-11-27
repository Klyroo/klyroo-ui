namespace Klyroo.UI.Maui;

/// <summary>One image of a <see cref="KlyImageGallery"/>.</summary>
/// <param name="Src">The picture. A string or a <see cref="Uri"/> converts to an <see cref="ImageSource"/>.</param>
public sealed record KlyGalleryImage(ImageSource Src)
{
    /// <summary>The text alternative. Defaults to <see cref="KlyImageGallery.ImageAltFormat"/> with the position of the image.</summary>
    public string? Alt { get; init; }
}
