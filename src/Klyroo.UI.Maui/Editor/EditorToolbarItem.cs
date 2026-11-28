namespace Klyroo.UI.Maui;

/// <summary>A button of <see cref="KlyEditorToolbar"/>, or a separator between groups of buttons.</summary>
public sealed record EditorToolbarItem
{
    /// <summary>Creates a button for an action.</summary>
    /// <param name="action">What the button does.</param>
    public EditorToolbarItem(EditorAction action) => Action = action;

    /// <summary>What the button does.</summary>
    public EditorAction Action { get; }

    /// <summary>Heading level for <see cref="EditorAction.Heading"/>.</summary>
    public int Level { get; init; }

    /// <summary>Accessible name and tooltip. Defaults to the English name of the action.</summary>
    public string? Label { get; init; }

    /// <summary>True for <see cref="Separator"/>, which is a line between groups and not a button.</summary>
    public bool IsSeparator { get; private init; }

    /// <summary>A thin line between two groups of buttons.</summary>
    public static EditorToolbarItem Separator { get; } = new(EditorAction.Bold) { IsSeparator = true };

    /// <summary>Creates a button for a heading of the given level, 1 to 6.</summary>
    /// <param name="level">Heading level, 1 to 6.</param>
    public static EditorToolbarItem Heading(int level) => new(EditorAction.Heading) { Level = level };

    /// <summary>Formatting buttons and lists, grouped: what a fixed toolbar shows by default.</summary>
    public static IReadOnlyList<EditorToolbarItem> Defaults { get; } =
    [
        new(EditorAction.Bold), new(EditorAction.Italic), new(EditorAction.Strike), new(EditorAction.Code),
        Separator,
        Heading(1), Heading(2), Heading(3),
        Separator,
        new(EditorAction.BulletList), new(EditorAction.OrderedList), new(EditorAction.Blockquote), new(EditorAction.CodeBlock),
        Separator,
        new(EditorAction.Link),
        Separator,
        new(EditorAction.Undo), new(EditorAction.Redo),
    ];

    /// <summary>Inline formatting: what a bubble toolbar shows by default.</summary>
    public static IReadOnlyList<EditorToolbarItem> BubbleDefaults { get; } =
    [
        new(EditorAction.Bold), new(EditorAction.Italic), new(EditorAction.Strike), new(EditorAction.Code), new(EditorAction.Link),
    ];
}
