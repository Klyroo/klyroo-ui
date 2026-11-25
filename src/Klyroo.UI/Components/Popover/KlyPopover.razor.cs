using System.Diagnostics.CodeAnalysis;
using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

public sealed record PopoverUi : ISlotOverrides
{
    public string? Content { get; init; }

    string? ISlotOverrides.For(string slot) => slot == "content" ? Content : null;
}

/// <summary>
/// Non-modal floating panel anchored to a trigger. <see cref="ChildContent"/> is the trigger: its first button
/// (or focusable element) opens and closes the panel. Opening, closing, Escape, light dismiss, positioning and focus
/// run in the browser, so they cost no round trip in Server mode and work in static server rendering once
/// <c>blazor.web.js</c> has loaded.
/// </summary>
public partial class KlyPopover
{
    private SlotSet _slots = default!;
    private string _id = default!;
    private int _sequence;

    /// <summary>The trigger.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>The panel content.</summary>
    [Parameter] public RenderFragment? Content { get; set; }

    /// <summary>Open state. Use <c>@bind-Open</c>: the browser can close the panel on its own (Escape, click outside).</summary>
    [Parameter] public bool Open { get; set; }
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Floating UI placement: top, right, bottom, left, optionally suffixed with -start or -end.</summary>
    [Parameter] public string Placement { get; set; } = "bottom";

    /// <summary>Distance between trigger and panel, in pixels.</summary>
    [Parameter] public int Offset { get; set; } = 8;

    /// <summary>Accessible name of the panel. Defaults to the trigger's text.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>
    /// Role of the panel. Defaults to <c>dialog</c>. Null renders a plain panel, a disclosure as in the WAI-ARIA
    /// navigation pattern: the trigger then only reports <c>aria-expanded</c> and <c>aria-controls</c>, and
    /// <see cref="AriaLabel"/> is not rendered because a role-less element cannot carry a name.
    /// </summary>
    [Parameter] public string? Role { get; set; } = "dialog";

    [Parameter] public PopoverUi? Ui { get; set; }

    // Only listen when someone is bound: an unbound popover never calls back into .NET.
    private EventCallback<KlyToggleEventArgs> ToggleHandler =>
        OpenChanged.HasDelegate ? EventCallback.Factory.Create<KlyToggleEventArgs>(this, OnToggle) : default;

    protected override void OnParametersSet()
    {
        _id = _id ?? NewId("popover");
        _slots = ResolveSlots(Options.Popover, new Dictionary<string, string?>(), Ui, rootSlot: "content");
    }

    // Blazor creates the event args through JSON deserialization, invisible to the trimmer.
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties, typeof(KlyToggleEventArgs))]
    private async Task OnToggle(KlyToggleEventArgs e)
    {
        _sequence = e.Sequence;
        if (e.Open == Open)
        {
            return;
        }
        Open = e.Open;
        await OpenChanged.InvokeAsync(e.Open);
    }
}
