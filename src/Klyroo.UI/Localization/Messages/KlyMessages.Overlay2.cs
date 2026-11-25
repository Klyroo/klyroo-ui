namespace Klyroo.UI;

public sealed partial record KlyMessages
{
    /// <summary>Placeholder and accessible name of the search field of <see cref="KlyCommandPalette"/>.</summary>
    public string SearchCommands { get; init; } = "Search commands";
    /// <summary>Text shown by <see cref="KlyCommandPalette"/> when no command matches the search.</summary>
    public string NoCommandResults { get; init; } = "No results found";

    /// <summary>Announced to screen readers after filtering; <c>{0}</c> is the number of results.</summary>
    public string CommandResults { get; init; } = "{0} results available";
    /// <summary>Accessible name of the menu of <see cref="KlyContextMenu"/>.</summary>
    public string ContextMenu { get; init; } = "Context menu";
    /// <summary>Accessible name of <see cref="KlyColorModeButton"/> and <see cref="KlyColorModeSwitch"/>.</summary>
    public string ToggleColorMode { get; init; } = "Toggle dark mode";
    /// <summary>Accessible name of <see cref="KlyColorModeSelect"/>.</summary>
    public string ColorMode { get; init; } = "Color mode";
    /// <summary>Label of the light option of <see cref="KlyColorModeSelect"/>.</summary>
    public string ColorModeLight { get; init; } = "Light";
    /// <summary>Label of the dark option of <see cref="KlyColorModeSelect"/>.</summary>
    public string ColorModeDark { get; init; } = "Dark";
    /// <summary>Label of the option of <see cref="KlyColorModeSelect"/> that follows the system setting.</summary>
    public string ColorModeSystem { get; init; } = "System";
}
