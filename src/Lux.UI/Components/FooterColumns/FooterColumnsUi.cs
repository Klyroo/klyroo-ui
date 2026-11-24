using Lux.UI.Theming;

namespace Lux.UI;

/// <summary>Per-instance class overrides of <see cref="LxFooterColumns"/> slots.</summary>
public sealed record FooterColumnsUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Left { get; init; }
    public string? Nav { get; init; }
    public string? Column { get; init; }
    public string? Label { get; init; }
    public string? List { get; init; }
    public string? Item { get; init; }
    public string? Link { get; init; }
    public string? Icon { get; init; }
    public string? Right { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "left" => Left,
        "nav" => Nav,
        "column" => Column,
        "label" => Label,
        "list" => List,
        "item" => Item,
        "link" => Link,
        "icon" => Icon,
        "right" => Right,
        _ => null,
    };
}
