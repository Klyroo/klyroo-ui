using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyColorModeSwitch"/> slots.</summary>
public sealed record ColorModeSwitchUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyColorModeSwitch"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>container</c> slot of <see cref="KlyColorModeSwitch"/>, after the theme and the app configuration.</summary>
    public string? Container { get; init; }
    /// <summary>Classes merged into the <c>input</c> slot of <see cref="KlyColorModeSwitch"/>, after the theme and the app configuration.</summary>
    public string? Input { get; init; }
    /// <summary>Classes merged into the <c>thumb</c> slot of <see cref="KlyColorModeSwitch"/>, after the theme and the app configuration.</summary>
    public string? Thumb { get; init; }
    /// <summary>Classes merged into the <c>moon</c> slot of <see cref="KlyColorModeSwitch"/>, after the theme and the app configuration.</summary>
    public string? Moon { get; init; }
    /// <summary>Classes merged into the <c>sun</c> slot of <see cref="KlyColorModeSwitch"/>, after the theme and the app configuration.</summary>
    public string? Sun { get; init; }
    /// <summary>Classes merged into the <c>wrapper</c> slot of <see cref="KlyColorModeSwitch"/>, after the theme and the app configuration.</summary>
    public string? Wrapper { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyColorModeSwitch"/>, after the theme and the app configuration.</summary>
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
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyColorModeSelect"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyColorModeSelect"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }
    /// <summary>Classes merged into the <c>trailingIcon</c> slot of <see cref="KlyColorModeSelect"/>, after the theme and the app configuration.</summary>
    public string? TrailingIcon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "base" => Base,
        "trailingIcon" => TrailingIcon,
        _ => null,
    };
}
