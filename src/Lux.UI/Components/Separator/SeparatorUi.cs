using Lux.UI.Theming;

namespace Lux.UI;

/// <summary>Per-instance class overrides of <see cref="LxSeparator"/> slots.</summary>
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
