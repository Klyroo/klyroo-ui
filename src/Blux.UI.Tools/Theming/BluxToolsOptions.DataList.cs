using Blux.UI.Theming;

namespace Blux.UI.Tools;

public sealed partial class BluxToolsOptions
{
    /// <summary>The empty state of a data list.</summary>
    public ComponentTheme DataListEmpty { get; } = ToolsThemes.DataListEmpty();

    /// <summary>The data list and its parts: toolbar, result count, refresh button, table, pagination and error state. One theme for all of them, so a look is changed in one place.</summary>
    public ComponentTheme DataList { get; } = ToolsThemes.DataList();
}
