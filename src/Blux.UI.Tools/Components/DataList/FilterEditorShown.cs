namespace Blux.UI.Tools;

/// <summary>Whether the part that holds a filter editor shows it now (an open popover, an expanded panel section). A cascading value with two shared instances, so a change is a new reference.</summary>
internal sealed class FilterEditorShown
{
    private FilterEditorShown(bool value) => Value = value;

    public static FilterEditorShown Yes { get; } = new(true);

    public static FilterEditorShown No { get; } = new(false);

    public bool Value { get; }

    public static FilterEditorShown Of(bool shown) => shown ? Yes : No;
}
