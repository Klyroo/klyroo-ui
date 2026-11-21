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

    private int _persistMaxBytes = 16 * 1024;
    private int _persistPageMaxBytes = 8 * 1024;

    /// <summary>
    /// The most bytes (state, loaded state and result with its facets, as JSON) one data list hands from its prerender to the interactive render (16 KB by default). A bigger result is not persisted:
    /// the interactive render loads it again once, and a warning names the list and the size. The limit exists because the client sends everything persisted to the server when a Server circuit starts,
    /// and SignalR refuses messages past 32 KB. Set it in <c>AddBluxUiTools</c>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int PersistMaxBytes
    {
        get => _persistMaxBytes;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _persistMaxBytes = value;
        }
    }

    /// <summary>The most bytes all the data lists of one prerendered page hand over together (8 KB by default: what the browser sends is about 3.5 times bigger once escaped, encrypted and encoded, next to the rest of the page state, and SignalR refuses 32 KB): lists past it, in the order they persist, are not persisted.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int PersistPageMaxBytes
    {
        get => _persistPageMaxBytes;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _persistPageMaxBytes = value;
        }
    }
}
