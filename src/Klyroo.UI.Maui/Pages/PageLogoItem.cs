namespace Klyroo.UI.Maui;

/// <summary>A logo of a <see cref="KlyPageLogos"/>: a built-in icon, or an image. <see cref="Alt"/> is its accessible name.</summary>
/// <param name="Alt">Accessible name of the logo.</param>
public sealed record PageLogoItem(string Alt)
{
    /// <summary>Name of a built-in icon (see <see cref="KlyIcon"/>).</summary>
    public string? Icon { get; init; }

    /// <summary>The image, used when no <see cref="Icon"/> is given.</summary>
    public ImageSource? Source { get; init; }
}
