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

public sealed partial class BluxStrings
{
    public string SearchCommands { get; set; } = "Search commands";
    public string NoCommandResults { get; set; } = "No results found";

    /// <summary>Announced to screen readers after filtering; <c>{0}</c> is the number of results.</summary>
    public string CommandResults { get; set; } = "{0} results available";
    public string ContextMenu { get; set; } = "Context menu";
    public string ToggleColorMode { get; set; } = "Toggle dark mode";
    public string ColorMode { get; set; } = "Color mode";
    public string ColorModeLight { get; set; } = "Light";
    public string ColorModeDark { get; set; } = "Dark";
    public string ColorModeSystem { get; set; } = "System";
}
