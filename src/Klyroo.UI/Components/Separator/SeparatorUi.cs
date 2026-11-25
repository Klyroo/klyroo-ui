using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlySeparator"/> slots.</summary>
public sealed record SeparatorUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Border { get; init; }
    public string? Container { get; init; }
    public string? Label { get; init; }
    public string? Icon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "border" => Border,
        "container" => Container,
        "label" => Label,
        "icon" => Icon,
        _ => null,
    };
}
