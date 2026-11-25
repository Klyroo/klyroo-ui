using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>A slide of a <see cref="KlyCarousel"/>.</summary>
/// <param name="Content">Content of the slide.</param>
public sealed record KlyCarouselItem(RenderFragment? Content)
{
    /// <summary>Accessible name of the slide. Defaults to "1 of 3".</summary>
    public string? Label { get; init; }
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyCarousel"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record CarouselUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyCarousel"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>viewport</c> slot of <see cref="KlyCarousel"/>, after the theme and the app configuration.</summary>
    public string? Viewport { get; init; }
    /// <summary>Classes merged into the <c>slide</c> slot of <see cref="KlyCarousel"/>, after the theme and the app configuration.</summary>
    public string? Slide { get; init; }
    /// <summary>Classes merged into the <c>controls</c> slot of <see cref="KlyCarousel"/>, after the theme and the app configuration.</summary>
    public string? Controls { get; init; }
    /// <summary>Classes merged into the <c>control</c> slot of <see cref="KlyCarousel"/>, after the theme and the app configuration.</summary>
    public string? Control { get; init; }
    /// <summary>Classes merged into the <c>dots</c> slot of <see cref="KlyCarousel"/>, after the theme and the app configuration.</summary>
    public string? Dots { get; init; }
    /// <summary>Classes merged into the <c>dot</c> slot of <see cref="KlyCarousel"/>, after the theme and the app configuration.</summary>
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
