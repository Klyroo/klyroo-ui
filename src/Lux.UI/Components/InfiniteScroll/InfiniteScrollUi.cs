using Lux.UI.Theming;

namespace Lux.UI;

/// <summary>Per-instance class overrides of <see cref="LxInfiniteScroll"/> slots.</summary>
public sealed record InfiniteScrollUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Sentinel { get; init; }
    public string? Trigger { get; init; }
    public string? Spinner { get; init; }
    public string? Status { get; init; }
    public string? End { get; init; }
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
