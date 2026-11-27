using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed partial class KlyrooOptions
{
    /// <summary>
    /// How images are resized and delivered: providers, breakpoints, densities, allowed remote hosts and presets,
    /// global image provider and preset configuration. Read-only once an image was resolved.
    /// </summary>
    public KlyImageSettings Images { get; } = new();

    /// <summary>
    /// Theme of the <c>figure</c> that <see cref="KlyImage"/> draws around an image with a <see cref="KlyImage.Caption"/>: the default classes of its slots. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ImageFigure { get; } = Themes.ImageFigure();
}
