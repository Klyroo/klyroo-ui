using Lux.UI.Theming;

namespace Lux.UI;

public sealed partial class LuxOptions
{
    public ComponentTheme Chart { get; } = Themes.Chart();
}
