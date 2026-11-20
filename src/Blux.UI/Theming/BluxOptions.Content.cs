using Blux.UI.Theming;

namespace Blux.UI;

public sealed partial class BluxOptions
{
    public ComponentTheme ContentNavigation { get; } = Themes.ContentNavigation();
    public ComponentTheme ContentToc { get; } = Themes.ContentToc();
    public ComponentTheme ContentSurround { get; } = Themes.ContentSurround();
    public ComponentTheme ContentSearchButton { get; } = Themes.ContentSearchButton();
}
