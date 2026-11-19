using Microsoft.AspNetCore.Components;

namespace Blux.UI;

public enum BxMenuItemKind
{
    Item,
    Separator,
    Heading,
}

/// <summary>An entry of <see cref="BxDropdownMenu"/>: an action, a link (with <see cref="Href"/>), a heading or a separator.</summary>
public sealed record BxMenuItem
{
    public BxMenuItemKind Kind { get; init; } = BxMenuItemKind.Item;
    public string Label { get; init; } = "";

    /// <summary>Identifies the item in <see cref="BxDropdownMenu.OnSelect"/>. Defaults to the label.</summary>
    public string? Value { get; init; }
    public string? Href { get; init; }
    public RenderFragment? Icon { get; init; }
    public string? Shortcut { get; init; }
    public bool Disabled { get; init; }

    /// <summary>"error" for destructive actions.</summary>
    public BxColor? Color { get; init; }

    public static BxMenuItem Separator() => new() { Kind = BxMenuItemKind.Separator };
    public static BxMenuItem Heading(string label) => new() { Kind = BxMenuItemKind.Heading, Label = label };
}
