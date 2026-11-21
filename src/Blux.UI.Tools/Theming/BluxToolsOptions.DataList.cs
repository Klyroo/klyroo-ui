using Blux.UI.Theming;

namespace Blux.UI.Tools;

public sealed partial class BluxToolsOptions
{
    /// <summary>The empty state of a data list.</summary>
    public ComponentTheme DataListEmpty { get; } = ToolsThemes.DataListEmpty();

    /// <summary>The data list and its parts: toolbar, result count, refresh button, table, pagination and error state. One theme for all of them, so a look is changed in one place.</summary>
    public ComponentTheme DataList { get; } = ToolsThemes.DataList();

    private int _searchDebounce = 300;

    /// <summary>
    /// Milliseconds of quiet after the last keystroke before the search field of a data list searches (300 by default), unless the field sets its own
    /// <c>Debounce</c>. Enter searches at once. Set it in <c>AddBluxUiTools</c>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int SearchDebounce
    {
        get => _searchDebounce;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _searchDebounce = value;
        }
    }
}
