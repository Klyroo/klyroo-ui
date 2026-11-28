namespace Klyroo.UI.Maui;

/// <summary>A node of a <see cref="KlyTree"/>.</summary>
/// <param name="Label">Text of the node.</param>
public sealed record KlyTreeItem(string Label)
{
    /// <summary>Identifies the item in <see cref="KlyTree.Value"/>. Defaults to its label, so give one when labels repeat.</summary>
    public string? Value { get; init; }

    /// <summary>Icon shown before the label: a Lucide name such as <see cref="KlyIconNames.File"/>.</summary>
    public string? Icon { get; init; }

    /// <summary>The nodes below this one. A node with children can be expanded.</summary>
    public IReadOnlyList<KlyTreeItem>? Children { get; init; }

    /// <summary>Initial state of an item with children.</summary>
    public bool Expanded { get; init; }

    /// <summary>Disables the node.</summary>
    public bool Disabled { get; init; }
}
