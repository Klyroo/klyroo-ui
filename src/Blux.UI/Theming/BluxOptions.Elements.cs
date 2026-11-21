using Blux.UI.Theming;

namespace Blux.UI;

public sealed partial class BluxOptions
{
    public ComponentTheme Kbd { get; } = Themes.Kbd();
    public ComponentTheme Separator { get; } = Themes.Separator();
    public ComponentTheme Skeleton { get; } = Themes.Skeleton();
    public ComponentTheme InfiniteScroll { get; } = Themes.InfiniteScroll();
    public ComponentTheme Chip { get; } = Themes.Chip();
    public ComponentTheme Link { get; } = Themes.Link();
    public ComponentTheme Icon { get; } = Themes.Icon();
    public ComponentTheme Container { get; } = Themes.Container();
    public ComponentTheme Progress { get; } = Themes.Progress();
    public ComponentTheme Banner { get; } = Themes.Banner();
    public ComponentTheme Empty { get; } = Themes.Empty();
    public ComponentTheme User { get; } = Themes.User();
    public ComponentTheme AvatarGroup { get; } = Themes.AvatarGroup();
    public ComponentTheme FieldGroup { get; } = Themes.FieldGroup();

    /// <summary>
    /// The icons components draw (<c>Close</c>, <c>Check</c>, <c>ChevronDown</c>...), replaceable by name, and <c>Register</c> to add icons in code.
    /// Any Iconify icon works by name, see <see cref="BxIcon"/>.
    /// </summary>
    public BluxIconOptions Icons { get; } = new();
}
