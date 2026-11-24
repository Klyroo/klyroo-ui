using Lux.UI.Theming;

namespace Lux.UI;

/// <summary>Per-instance class overrides of <see cref="BxProgress"/> slots.</summary>
public sealed record ProgressUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Status { get; init; }
    public string? Base { get; init; }
    public string? Indicator { get; init; }
    public string? Steps { get; init; }
    public string? Step { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "status" => Status,
        "base" => Base,
        "indicator" => Indicator,
        "steps" => Steps,
        "step" => Step,
        _ => null,
    };
}
