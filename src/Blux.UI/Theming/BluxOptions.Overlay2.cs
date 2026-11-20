using Blux.UI.Theming;

namespace Blux.UI;

public sealed partial class BluxOptions
{
    public ComponentTheme Drawer { get; } = Themes.Drawer();
    public ComponentTheme Slideover { get; } = Themes.Slideover();
    public ComponentTheme ContextMenu { get; } = Themes.ContextMenu();
    public ComponentTheme CommandPalette { get; } = Themes.CommandPalette();
    public ComponentTheme ColorModeButton { get; } = Themes.ColorModeButton();
    public ComponentTheme ColorModeSwitch { get; } = Themes.ColorModeSwitch();
    public ComponentTheme ColorModeSelect { get; } = Themes.ColorModeSelect();
    public ComponentTheme ColorModeAvatar { get; } = Themes.ColorModeAvatar();
    public ComponentTheme ColorModeImage { get; } = Themes.ColorModeImage();
}
