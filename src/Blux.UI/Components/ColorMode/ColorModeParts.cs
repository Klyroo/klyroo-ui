using Blux.UI.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blux.UI;

/// <summary>Per-instance class overrides of <see cref="BxColorModeSwitch"/> slots.</summary>
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

/// <summary>Per-instance class overrides of <see cref="BxColorModeSelect"/> slots.</summary>
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

internal static class ColorModeIcons
{
    public static RenderFragment Moon(string? @class) => Icon(@class, "M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z");

    public static RenderFragment Sun(string? @class) =>
        Icon(@class, "M16 12a4 4 0 1 1-8 0 4 4 0 0 1 8 0ZM12 2v2m0 16v2M4.9 4.9l1.4 1.4m11.4 11.4 1.4 1.4M2 12h2m16 0h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4");

    private static RenderFragment Icon(string? @class, string path) => builder =>
    {
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "viewBox", "0 0 24 24");
        builder.AddAttribute(2, "fill", "none");
        builder.AddAttribute(3, "stroke", "currentColor");
        builder.AddAttribute(4, "stroke-width", "2");
        builder.AddAttribute(5, "stroke-linecap", "round");
        builder.AddAttribute(6, "stroke-linejoin", "round");
        builder.AddAttribute(7, "aria-hidden", "true");
        builder.AddAttribute(8, "class", @class);
        builder.OpenElement(9, "path");
        builder.AddAttribute(10, "d", path);
        builder.CloseElement();
        builder.CloseElement();
    };
}
