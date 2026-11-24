using Lux.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Lux.UI;

public sealed record BxCarouselItem(RenderFragment? Content)
{
    /// <summary>Accessible name of the slide. Defaults to "1 of 3".</summary>
    public string? Label { get; init; }
}

public sealed record CarouselUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Viewport { get; init; }
    public string? Slide { get; init; }
    public string? Controls { get; init; }
    public string? Control { get; init; }
    public string? Dots { get; init; }
    public string? Dot { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "viewport" => Viewport,
        "slide" => Slide,
        "controls" => Controls,
        "control" => Control,
        "dots" => Dots,
        "dot" => Dot,
        _ => null,
    };
}
