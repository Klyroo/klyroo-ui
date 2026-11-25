using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed partial class KlyrooOptions
{
    public ComponentTheme ContentNavigation { get; } = Themes.ContentNavigation();
    public ComponentTheme ContentToc { get; } = Themes.ContentToc();
    public ComponentTheme ContentSurround { get; } = Themes.ContentSurround();
    public ComponentTheme ContentSearchButton { get; } = Themes.ContentSearchButton();
}
