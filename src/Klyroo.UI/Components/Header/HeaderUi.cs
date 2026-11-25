using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyHeader"/> slots.</summary>
public sealed record HeaderUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Container { get; init; }
    public string? Left { get; init; }
    public string? Title { get; init; }
    public string? Center { get; init; }
    public string? Right { get; init; }
    public string? Menu { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "container" => Container,
        "left" => Left,
        "title" => Title,
        "center" => Center,
        "right" => Right,
        "menu" => Menu,
        _ => null,
    };
}
