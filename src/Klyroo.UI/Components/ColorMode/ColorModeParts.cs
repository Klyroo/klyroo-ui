using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyColorModeSwitch"/> slots.</summary>
public sealed record ColorModeSwitchUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Container { get; init; }
    public string? Input { get; init; }
    public string? Thumb { get; init; }
    public string? Moon { get; init; }
    public string? Sun { get; init; }
    public string? Wrapper { get; init; }
    public string? Label { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "container" => Container,
        "input" => Input,
        "thumb" => Thumb,
        "moon" => Moon,
        "sun" => Sun,
        "wrapper" => Wrapper,
        "label" => Label,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of <see cref="KlyColorModeSelect"/> slots.</summary>
public sealed record ColorModeSelectUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Base { get; init; }
    public string? TrailingIcon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "base" => Base,
        "trailingIcon" => TrailingIcon,
        _ => null,
    };
}
