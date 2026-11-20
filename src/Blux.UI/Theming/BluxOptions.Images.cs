namespace Blux.UI;

public sealed partial class BluxOptions
{
    /// <summary>
    /// How images are resized and delivered: providers, breakpoints, densities, allowed remote hosts and presets,
    /// the counterpart of the <c>image</c> key of Nuxt Image. Read-only once an image was resolved.
    /// </summary>
    public BxImageSettings Images { get; } = new();
}
