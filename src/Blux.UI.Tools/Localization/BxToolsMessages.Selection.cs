namespace Blux.UI.Tools;

public sealed partial record BxToolsMessages
{
    /// <summary>Accessible name of the checkbox in the header of a data list that selects the rows of the page.</summary>
    public string SelectAllRows { get; init; } = "Select all rows on this page";

    /// <summary>Accessible name of the checkbox of a row that has a label. {0} is the label of the row (<c>RowLabel</c> of the schema).</summary>
    public string SelectRow { get; init; } = "Select {0}";

    /// <summary>Accessible name of the checkbox of a row that has no label. {0} is the position of the row on the page, from 1.</summary>
    public string SelectRowAt { get; init; } = "Select row {0}";

    /// <summary>How many rows are selected, announced when it changes. {0} is the count.</summary>
    public string SelectionCount { get; init; } = "{0} selected";

    /// <summary>Said instead of the count when the actions apply to every result. {0} is how many results match.</summary>
    public string SelectionAllResults { get; init; } = "All {0} results selected";

    /// <summary>Button of the selection bar that makes the actions apply to every result. {0} is how many results match.</summary>
    public string SelectionSelectAllResults { get; init; } = "Select all {0} results";

    /// <summary>Button of the selection bar that goes back from every result to the rows that were ticked. {0} is how many.</summary>
    public string SelectionOnlySelected { get; init; } = "Only the {0} selected";

    /// <summary>Button of the selection bar that unticks every row.</summary>
    public string SelectionClear { get; init; } = "Clear selection";

    /// <summary>Accessible name of the selection bar, the group of the actions on the selected rows.</summary>
    public string SelectionActionsLabel { get; init; } = "Actions on the selection";

    /// <summary>Label of the button that opens the menu of the toolbar actions of a data list.</summary>
    public string ActionsMenu { get; init; } = "Actions";

    /// <summary>Label of the button of the selection bar that opens the actions that did not fit.</summary>
    public string ActionsMore { get; init; } = "More actions";

    /// <summary>Confirm button of the dialog an action asks before it runs, when the action gives no label.</summary>
    public string ActionConfirm { get; init; } = "Confirm";

    /// <summary>Cancel button of the dialog an action asks before it runs.</summary>
    public string ActionCancel { get; init; } = "Cancel";

    /// <summary>Accessible name of the button that opens the actions of one row of a data list; {0} is the label of the row.</summary>
    public string RowActionsLabel { get; init; } = "Actions for {0}";

    /// <summary>Accessible name of the button that opens the actions of a row that has no label; {0} is its position on the page.</summary>
    public string RowActionsLabelAt { get; init; } = "Actions for row {0}";
}
