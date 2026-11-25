using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>A panel of a <see cref="KlySplitter"/>.</summary>
public sealed record KlySplitterPanel
{
    /// <summary>Content of the panel.</summary>
    public RenderFragment? Content { get; init; }

    /// <summary>Initial size in percent. Panels without one share what the others leave.</summary>
    public double? Size { get; init; }

    /// <summary>Smallest size in percent.</summary>
    public double MinSize { get; init; }

    /// <summary>Largest size in percent.</summary>
    public double MaxSize { get; init; } = 100;
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlySplitter"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record SplitterUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlySplitter"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>panel</c> slot of <see cref="KlySplitter"/>, after the theme and the app configuration.</summary>
    public string? Panel { get; init; }
    /// <summary>Classes merged into the <c>handle</c> slot of <see cref="KlySplitter"/>, after the theme and the app configuration.</summary>
    public string? Handle { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "panel" => Panel,
        "handle" => Handle,
        _ => null,
    };
}
