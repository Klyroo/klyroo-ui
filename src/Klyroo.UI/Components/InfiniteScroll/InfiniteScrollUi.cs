using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyInfiniteScroll"/> slots.</summary>
public sealed record InfiniteScrollUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyInfiniteScroll"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>sentinel</c> slot of <see cref="KlyInfiniteScroll"/>, after the theme and the app configuration.</summary>
    public string? Sentinel { get; init; }
    /// <summary>Classes merged into the <c>trigger</c> slot of <see cref="KlyInfiniteScroll"/>, after the theme and the app configuration.</summary>
    public string? Trigger { get; init; }
    /// <summary>Classes merged into the <c>spinner</c> slot of <see cref="KlyInfiniteScroll"/>, after the theme and the app configuration.</summary>
    public string? Spinner { get; init; }
    /// <summary>Classes merged into the <c>status</c> slot of <see cref="KlyInfiniteScroll"/>, after the theme and the app configuration.</summary>
    public string? Status { get; init; }
    /// <summary>Classes merged into the <c>end</c> slot of <see cref="KlyInfiniteScroll"/>, after the theme and the app configuration.</summary>
    public string? End { get; init; }
    /// <summary>Classes merged into the <c>marker</c> slot of <see cref="KlyInfiniteScroll"/>, after the theme and the app configuration.</summary>
    public string? Marker { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "sentinel" => Sentinel,
        "trigger" => Trigger,
        "spinner" => Spinner,
        "status" => Status,
        "end" => End,
        "marker" => Marker,
        _ => null,
    };
}
