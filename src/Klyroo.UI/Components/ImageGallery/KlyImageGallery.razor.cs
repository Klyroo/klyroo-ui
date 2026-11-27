namespace Klyroo.UI;

/// <summary>One image of a <see cref="KlyImageGallery"/>.</summary>
/// <param name="Src">The address of the image.</param>
public sealed record KlyGalleryImage(string Src)
{
    /// <summary>The text alternative. Defaults to <see cref="KlyMessages.ImageAlt"/> with the position of the image.</summary>
    public string? Alt { get; init; }

    /// <summary>The width of the image in CSS pixels, as <see cref="KlyImage.Width"/>.</summary>
    public int? Width { get; init; }

    /// <summary>The height of the image in CSS pixels, as <see cref="KlyImage.Height"/>.</summary>
    public int? Height { get; init; }
}

/// <summary>A grid of images with a +N overflow tile.</summary>
public partial class KlyImageGallery;
