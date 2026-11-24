using Lux.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Lux.UI;

public sealed record BxTimelineItem(string Title)
{
    /// <summary>Free text: "Mar 2026", "Yesterday".</summary>
    public string? Date { get; init; }
    public string? Description { get; init; }
    public RenderFragment? Icon { get; init; }
}

public sealed record TimelineUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Item { get; init; }
    public string? Track { get; init; }
    public string? Indicator { get; init; }
    public string? Icon { get; init; }
    public string? Separator { get; init; }
    public string? Text { get; init; }
    public string? Date { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "item" => Item,
        "track" => Track,
        "indicator" => Indicator,
        "icon" => Icon,
        "separator" => Separator,
        "text" => Text,
        "date" => Date,
        "title" => Title,
        "description" => Description,
        _ => null,
    };
}
