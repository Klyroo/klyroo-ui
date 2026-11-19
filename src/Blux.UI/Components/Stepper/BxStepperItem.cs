using Blux.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Blux.UI;

public sealed record BxStepperItem(string Title)
{
    public string? Description { get; init; }

    /// <summary>Replaces the step number (and the check of a completed step).</summary>
    public RenderFragment? Icon { get; init; }

    /// <summary>Shown below the list while this step is the current one.</summary>
    public RenderFragment? Content { get; init; }

    /// <summary>Cannot be selected when the stepper is interactive.</summary>
    public bool Disabled { get; init; }
}

public sealed record StepperUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? List { get; init; }
    public string? Item { get; init; }
    public string? Track { get; init; }
    public string? Indicator { get; init; }
    public string? Separator { get; init; }
    public string? Icon { get; init; }
    public string? Text { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Status { get; init; }
    public string? Content { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "list" => List,
        "item" => Item,
        "track" => Track,
        "indicator" => Indicator,
        "separator" => Separator,
        "icon" => Icon,
        "text" => Text,
        "title" => Title,
        "description" => Description,
        "status" => Status,
        "content" => Content,
        _ => null,
    };
}
