namespace Lux.UI;

public sealed partial record BxMessages
{
    public string SearchCommands { get; init; } = "Search commands";
    public string NoCommandResults { get; init; } = "No results found";

    /// <summary>Announced to screen readers after filtering; <c>{0}</c> is the number of results.</summary>
    public string CommandResults { get; init; } = "{0} results available";
    public string ContextMenu { get; init; } = "Context menu";
    public string ToggleColorMode { get; init; } = "Toggle dark mode";
    public string ColorMode { get; init; } = "Color mode";
    public string ColorModeLight { get; init; } = "Light";
    public string ColorModeDark { get; init; } = "Dark";
    public string ColorModeSystem { get; init; } = "System";
}
