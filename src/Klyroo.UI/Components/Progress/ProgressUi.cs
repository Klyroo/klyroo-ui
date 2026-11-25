using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyProgress"/> slots.</summary>
public sealed record ProgressUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyProgress"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>status</c> slot of <see cref="KlyProgress"/>, after the theme and the app configuration.</summary>
    public string? Status { get; init; }
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyProgress"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }
    /// <summary>Classes merged into the <c>indicator</c> slot of <see cref="KlyProgress"/>, after the theme and the app configuration.</summary>
    public string? Indicator { get; init; }
    /// <summary>Classes merged into the <c>steps</c> slot of <see cref="KlyProgress"/>, after the theme and the app configuration.</summary>
    public string? Steps { get; init; }
    /// <summary>Classes merged into the <c>step</c> slot of <see cref="KlyProgress"/>, after the theme and the app configuration.</summary>
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
