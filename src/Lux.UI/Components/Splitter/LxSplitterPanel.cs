using Lux.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Lux.UI;

public sealed record LxSplitterPanel
{
    public RenderFragment? Content { get; init; }

    /// <summary>Initial size in percent. Panels without one share what the others leave.</summary>
    public double? Size { get; init; }

    /// <summary>Smallest size in percent.</summary>
    public double MinSize { get; init; }

    /// <summary>Largest size in percent.</summary>
    public double MaxSize { get; init; } = 100;
}

public sealed record SplitterUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Panel { get; init; }
    public string? Handle { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "panel" => Panel,
        "handle" => Handle,
        _ => null,
    };
}
