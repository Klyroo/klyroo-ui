namespace Lux.UI;

public sealed partial class LuxOptions
{
    /// <summary>
    /// How images are resized and delivered: providers, breakpoints, densities, allowed remote hosts and presets,
    /// global image provider and preset configuration. Read-only once an image was resolved.
    /// </summary>
    public LxImageSettings Images { get; } = new();
}
