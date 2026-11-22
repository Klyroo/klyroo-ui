using Blux.UI.Tools.Query;

namespace Blux.UI.Tools;

/// <summary>Which rows a bulk or toolbar action targets.</summary>
public enum DataListActionScope
{
    /// <summary>The rows that are ticked.</summary>
    Selection,

    /// <summary>Every row the search and the filters match, on every page. A list over a provider or a query does not hold them: the action gets the <see cref="DataListActionContext{TItem}.Request"/> and applies it where the data lives.</summary>
    All,
}

/// <summary>Where the rows of a selection survive: across pages, or only on the page they were ticked on.</summary>
public enum DataListSelectionScope
{
    /// <summary>The list decides: a list of rows in memory (<c>Items</c>) keeps the selection across pages, a list over a provider or a queryable keeps it for the rows of the page.</summary>
    Auto,

    /// <summary>Rows that leave the page (a new page, a new search or filter, another sort) are no longer selected.</summary>
    Page,

    /// <summary>Selected rows stay selected on other pages while the search and the filters still match them. Only a list of rows in memory (<c>Items</c>) can do it: the others behave as <see cref="Page"/>.</summary>
    All,
}

/// <summary>When running an action unticks the rows.</summary>
public enum DataListSelectionClear
{
    /// <summary>The selection is kept (the default).</summary>
    Never,

    /// <summary>The rows are unticked as the action starts.</summary>
    Trigger,

    /// <summary>The rows are unticked once the action finished without failing; a failure keeps them so that the user can try again.</summary>
    Success,
}

/// <summary>Where the selection bar sits.</summary>
public enum DataListSelectionBarPosition
{
    /// <summary>At the bottom of the window while the list is on screen, and no lower than the end of the list (the default).</summary>
    Sticky,

    /// <summary>Fixed to the bottom of the window, centered.</summary>
    Fixed,
}

/// <summary>What an action receives when it runs: the rows it targets, the scope and the request that stands for "every result".</summary>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed class DataListActionContext<TItem>
{
    internal DataListActionContext(DataListContext<TItem> list, DataListActionScope scope, IReadOnlyList<TItem> items, IReadOnlyList<string> keys, long? matchingCount, long targetCount, DataListRequest? request, CancellationToken cancellationToken)
    {
        TargetCount = targetCount;
        List = list;
        Scope = scope;
        SelectedItems = items;
        SelectedKeys = keys;
        MatchingCount = matchingCount;
        Request = request;
        CancellationToken = cancellationToken;
    }

    /// <summary>The list, to read its state or to load it again (<see cref="DataListContext{TItem}.RefreshAsync"/>).</summary>
    public DataListContext<TItem> List { get; }

    /// <summary>Whether the action targets the ticked rows or every result.</summary>
    public DataListActionScope Scope { get; }

    /// <summary>
    /// The rows the action targets. With <see cref="DataListActionScope.Selection"/>: the ticked rows (those that are on screen or were ticked on another page of a list in memory). With
    /// <see cref="DataListActionScope.All"/> on a list of rows in memory, when the action runs: every row the search and the filters match. With <see cref="DataListActionScope.All"/> over a provider or a
    /// queryable: empty, because the list never holds those rows; apply <see cref="Request"/> instead.
    /// </summary>
    public IReadOnlyList<TItem> SelectedItems { get; }

    /// <summary>
    /// How many rows the action will act on: <see cref="MatchingCount"/> when <see cref="Scope"/> is <see cref="DataListActionScope.All"/> (and the source knows it), else the number of ticked rows.
    /// Use it, not <c>SelectedItems.Count</c>, in the text of a confirmation and in <c>Visible</c> and <c>Disabled</c>: before the action runs, <see cref="SelectedItems"/> holds only the ticked rows
    /// (a list is never loaded just to describe an action), while the action itself gets every matching row of a list in memory.
    /// </summary>
    public long TargetCount { get; }

    /// <summary>The row keys (<c>RowKey</c> of the schema) of <see cref="SelectedItems"/>; empty where those are empty too.</summary>
    public IReadOnlyList<string> SelectedKeys { get; }

    /// <summary>How many rows the search and the filters match, when the source says (<see cref="DataListResult{T}.TotalCount"/>); <see langword="null"/> when it does not.</summary>
    public long? MatchingCount { get; }

    /// <summary>
    /// The request for every row the list matches: its search, filters and sort, with no paging and no facets, as the list sent it. For <see cref="DataListActionScope.All"/> over a provider, send it to your
    /// server and apply it there (<c>schema.Apply(query, request)</c>), so a bulk action never needs the rows on the client. <see langword="null"/> before the first load.
    /// </summary>
    public DataListRequest? Request { get; }

    /// <summary>Cancelled when the list goes away.</summary>
    public CancellationToken CancellationToken { get; }
}

/// <summary>The question an action asks before it runs, in a dialog. The text can say how many rows are affected.</summary>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed class DataListActionConfirm<TItem>
{
    /// <summary>The title of the dialog.</summary>
    public required string Title { get; init; }

    /// <summary>The text under the title, built from the context so that it can count the rows (<c>c =&gt; $"{c.TargetCount} customers will be removed."</c>, which is right for the ticked rows and for every result).</summary>
    public Func<DataListActionContext<TItem>, string?>? Description { get; init; }

    /// <summary>The label of the confirm button. Without it the localized "Confirm".</summary>
    public string? ConfirmLabel { get; init; }

    /// <summary>The color of the confirm button; <see cref="BxColor.Error"/> for a destructive action. Without it the color of the action.</summary>
    public BxColor? Color { get; init; }
}

/// <summary>
/// An action of a data list: a button of the <see cref="BxDataListSelectionActions{TItem}"/> bar (it acts on the selection) or an entry of the <see cref="BxDataListActionsDropdown{TItem}"/> menu.
/// While it runs its button shows a spinner and the action cannot start twice.
/// </summary>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed class DataListAction<TItem>
{
    /// <summary>Identifies the action: tests and the menu use it.</summary>
    public required string Key { get; init; }

    /// <summary>The text of the button.</summary>
    public required string Label { get; init; }

    /// <summary>The name of an icon shown before the label (<c>trash</c>), one of the icons of Blux.UI.</summary>
    public string? Icon { get; init; }

    /// <summary>The color of the button; <see cref="BxColor.Error"/> for a destructive action.</summary>
    public BxColor Color { get; init; } = BxColor.Neutral;

    /// <summary>The variant of the button in the selection bar.</summary>
    public ButtonVariant Variant { get; init; } = ButtonVariant.Soft;

    /// <summary>Hides the action when it returns <see langword="false"/> (an action that does not apply to the selected rows). It runs on every render: keep it cheap.</summary>
    public Func<DataListActionContext<TItem>, bool>? Visible { get; init; }

    /// <summary>Disables the action when it returns <see langword="true"/>. It runs on every render: keep it cheap.</summary>
    public Func<DataListActionContext<TItem>, bool>? Disabled { get; init; }

    /// <summary>Asks before the action runs. Without it the action runs on the click.</summary>
    public DataListActionConfirm<TItem>? Confirm { get; init; }

    /// <summary>When the ticked rows are unticked: never by default.</summary>
    public DataListSelectionClear ClearSelection { get; init; }

    /// <summary>Loads the list again once the action succeeded (the rows it changed or removed). Off by default; an action can also call <see cref="DataListContext{TItem}.RefreshAsync"/> itself.</summary>
    public bool RefreshOnSuccess { get; init; }

    /// <summary>
    /// What the action does. An exception marks the run as failed: the selection stays, the list is not refreshed, <c>OnError</c> of the part is raised and nothing is thrown to the page.
    /// </summary>
    public Func<DataListActionContext<TItem>, Task>? OnExecute { get; init; }
}

/// <summary>An action that threw.</summary>
/// <param name="Action">The action.</param>
/// <param name="Exception">What it threw.</param>
/// <typeparam name="TItem">The row type.</typeparam>
public sealed record DataListActionFailure<TItem>(DataListAction<TItem> Action, Exception Exception);
