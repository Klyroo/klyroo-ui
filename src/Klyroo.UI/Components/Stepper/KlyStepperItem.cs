using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>A step of a <see cref="KlyStepper"/>.</summary>
/// <param name="Title">Title of the step.</param>
public sealed record KlyStepperItem(string Title)
{
    /// <summary>Text under the title.</summary>
    public string? Description { get; init; }

    /// <summary>Replaces the step number (and the check of a completed step).</summary>
    public RenderFragment? Icon { get; init; }

    /// <summary>Shown below the list while this step is the current one.</summary>
    public RenderFragment? Content { get; init; }

    /// <summary>Cannot be selected when the stepper is interactive.</summary>
    public bool Disabled { get; init; }
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyStepper"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record StepperUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyStepper"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>list</c> slot of <see cref="KlyStepper"/>, after the theme and the app configuration.</summary>
    public string? List { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyStepper"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>track</c> slot of <see cref="KlyStepper"/>, after the theme and the app configuration.</summary>
    public string? Track { get; init; }
    /// <summary>Classes merged into the <c>indicator</c> slot of <see cref="KlyStepper"/>, after the theme and the app configuration.</summary>
    public string? Indicator { get; init; }
    /// <summary>Classes merged into the <c>separator</c> slot of <see cref="KlyStepper"/>, after the theme and the app configuration.</summary>
    public string? Separator { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyStepper"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>text</c> slot of <see cref="KlyStepper"/>, after the theme and the app configuration.</summary>
    public string? Text { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyStepper"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyStepper"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>status</c> slot of <see cref="KlyStepper"/>, after the theme and the app configuration.</summary>
    public string? Status { get; init; }
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlyStepper"/>, after the theme and the app configuration.</summary>
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
