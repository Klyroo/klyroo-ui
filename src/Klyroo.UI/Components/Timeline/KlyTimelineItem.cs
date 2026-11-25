using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>An event of a <see cref="KlyTimeline"/>.</summary>
/// <param name="Title">Title of the event.</param>
public sealed record KlyTimelineItem(string Title)
{
    /// <summary>Free text: "Mar 2026", "Yesterday".</summary>
    public string? Date { get; init; }
    /// <summary>Text under the title.</summary>
    public string? Description { get; init; }
    /// <summary>Icon of the event, as markup such as a <see cref="KlyIcon"/>.</summary>
    public RenderFragment? Icon { get; init; }
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyTimeline"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record TimelineUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>track</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Track { get; init; }
    /// <summary>Classes merged into the <c>indicator</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Indicator { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>separator</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Separator { get; init; }
    /// <summary>Classes merged into the <c>text</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Text { get; init; }
    /// <summary>Classes merged into the <c>date</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Date { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyTimeline"/>, after the theme and the app configuration.</summary>
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
