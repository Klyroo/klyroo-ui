using Blux.UI.Theming;

namespace Blux.UI;

public enum ToggleVariant
{
    Outline,
    Soft,
    Ghost,
}

public sealed record ToggleUi : ISlotOverrides
{
    public string? Base { get; init; }
    public string? Label { get; init; }
    public string? Icon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "base" => Base,
        "label" => Label,
        "icon" => Icon,
        _ => null,
    };
}
