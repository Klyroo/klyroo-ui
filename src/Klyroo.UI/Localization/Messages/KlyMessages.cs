namespace Klyroo.UI;

/// <summary>
/// Every text the components render: accessible names, labels, placeholders, announcements. A <see cref="KlyLocale"/>
/// carries one set; build a variant with a <c>with</c> expression (<c>KlyLocales.En.Messages with { Close = "Dismiss" }</c>).
/// </summary>
/// <remarks>
/// Partial: each family of components declares its messages in its own file. A message with placeholders
/// ({0}, {1}) is formatted by the component with <see cref="string.Format(string, object?[])"/>; keep the same
/// placeholders when you translate it.
/// </remarks>
public sealed partial record KlyMessages
{
    /// <summary>Accessible name of the close button of alerts, banners, modals, drawers, slideovers, toasts, tags, chat attachments and the other dismissible components.</summary>
    public string Close { get; init; } = "Close";
    /// <summary>Accessible name of the list of toasts of <see cref="KlyToaster"/>.</summary>
    public string Notifications { get; init; } = "Notifications";
    /// <summary>Text shown by <see cref="KlyChart{TItem}"/> and <see cref="KlyTable{TItem}"/> when there is nothing to show.</summary>
    public string NoData { get; init; } = "No data";

    /// <summary>Tooltip of the button of a sortable column that sorts it in ascending order.</summary>
    public string SortAscending { get; init; } = "Sort ascending";

    /// <summary>Tooltip of the button of a sorted column that reverses the order.</summary>
    public string SortDescending { get; init; } = "Sort descending";

    /// <summary>
    /// Name of the key that "mod" stands for in the hint of a shortcut (<c>mod+k</c>): "Ctrl", "Strg" in German. On Apple
    /// devices the browser shows the command symbol instead.
    /// </summary>
    public string ModifierKey { get; init; } = "Ctrl";
}
