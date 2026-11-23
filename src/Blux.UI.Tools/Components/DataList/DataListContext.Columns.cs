namespace Blux.UI.Tools;

public sealed partial class DataListContext<TItem>
{
    /// <summary>Every keyed column of the table in its active order, with its effective visibility, pin edge and width.</summary>
    public IReadOnlyList<DataListColumnState> ColumnStates => _owner.ColumnStates();

    /// <summary>Whether any column visibility, order, pinning or width currently differs from the columns' defaults.</summary>
    public bool HasCustomColumnPreferences => _owner.HasCustomColumnPreferences;

    /// <summary>Counts column preference changes so cached rows rebuild when columns reorder, hide, pin or resize.</summary>
    internal int ColumnsVersion => _owner.ColumnsVersion;

    /// <summary>Whether this render is a prerender of an interactive page (used by interactive-only toolbar parts to render a disabled placeholder).</summary>
    internal bool WillBeInteractive => _owner.WillBeInteractive;

    /// <summary>Shows or hides a column by key. Does nothing for a <see cref="BxDataListColumn{TItem}.Required"/> column.</summary>
    public void SetColumnVisible(string key, bool visible) => _owner.SetColumnVisible(key, visible);

    /// <summary>Shows every column (<paramref name="visible"/> true) or hides every column that is not <see cref="BxDataListColumn{TItem}.Required"/>; when none is, the first one stays so the table is never empty.</summary>
    public void SetAllColumnsVisible(bool visible) => _owner.SetAllColumnsVisible(visible);

    /// <summary>Pins a column to the start or end edge, or unpins it.</summary>
    public void SetColumnPinned(string key, DataListPin pin) => _owner.SetColumnPinned(key, pin);

    /// <summary>Moves a column earlier (<c>-1</c>) or later (<c>+1</c>) in the column order.</summary>
    public void MoveColumn(string key, int delta) => _owner.MoveColumn(key, delta);

    /// <summary>Puts a column right before or right after another one in the column order: what dropping a dragged column on another does.</summary>
    public void MoveColumnNextTo(string key, string targetKey, bool after) => _owner.MoveColumnNextTo(key, targetKey, after);

    /// <summary>Sets a custom pixel width for a resizable column, or clears it when <see langword="null"/>.</summary>
    public void SetColumnWidth(string key, int? width) => _owner.SetColumnWidth(key, width);

    /// <summary>Restores every column's visibility, order, pinning and width to its declared defaults and clears any saved cookie.</summary>
    public void ResetColumns() => _owner.ResetColumns();

    /// <summary>Whether the column may be hidden now: it is not Required and another column stays visible.</summary>
    internal bool CanHideColumn(BxDataListColumn<TItem> column) => _owner.CanHideColumn(column);

    /// <summary>The last change made in the column panel, worded for a live region.</summary>
    internal string ColumnAnnouncement => _owner.ColumnAnnouncement;

    /// <summary>The last resize, worded for a live region.</summary>
    internal string WidthAnnouncement => _owner.WidthAnnouncement;

    internal List<BxDataListColumn<TItem>> TableColumns() => _owner.TableColumns();

    internal DataListPin EffectiveColumnPin(BxDataListColumn<TItem> column) => _owner.EffectiveColumnPin(column);

    internal int? EffectiveColumnWidth(BxDataListColumn<TItem> column) => _owner.EffectiveColumnWidth(column);
}
