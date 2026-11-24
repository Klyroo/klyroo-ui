using Microsoft.AspNetCore.Components;

namespace Lux.UI;

/// <summary>Icons as render fragments, for the parameters that take one (<c>LeadingIcon</c>, <c>Icon</c>...). Size them with classes.</summary>
public static class BxIcons
{
    /// <summary>
    /// An icon by name, in any form <see cref="BxIcon"/> accepts, drawn in <c>currentColor</c>. It finds the Lucide icons built into
    /// the library and the icons bundled by the app's build; icons registered on <c>LuxOptions.Icons</c> are only found by
    /// <see cref="BxIcon"/>. An unknown name renders an empty icon.
    /// </summary>
    public static RenderFragment Named(string name, string? @class = null) => IconRender.Fragment(null, name, @class);
}
