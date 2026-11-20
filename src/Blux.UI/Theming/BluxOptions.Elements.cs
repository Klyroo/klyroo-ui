using Blux.UI.Theming;

namespace Blux.UI;

public sealed partial class BluxOptions
{
    public ComponentTheme Kbd { get; } = Themes.Kbd();
    public ComponentTheme Separator { get; } = Themes.Separator();
    public ComponentTheme Skeleton { get; } = Themes.Skeleton();
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
    /// Icons available to <c>BxIcon</c> besides the built-in Lucide set: name to SVG path data (24x24 viewBox,
    /// drawn as a 2px round stroke in currentColor). An entry replaces a built-in icon of the same name.
    /// </summary>
    public Dictionary<string, string> Icons { get; } = new(StringComparer.Ordinal);
}
