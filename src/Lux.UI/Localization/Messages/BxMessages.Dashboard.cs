namespace Lux.UI;

public sealed partial record BxMessages
{
    /// <summary>Accessible name of the dashboard search dialog and its search field.</summary>
    public string DashboardSearch { get; init; } = "Search";

    /// <summary>Placeholder of the dashboard search field.</summary>
    public string DashboardSearchPlaceholder { get; init; } = "Type a command or search...";

    /// <summary>Text of the button that opens the dashboard search.</summary>
    public string DashboardSearchButton { get; init; } = "Search";
}
