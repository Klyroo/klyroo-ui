using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of the slots of <see cref="KlySwitch"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record SwitchUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlySwitch"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>container</c> slot of <see cref="KlySwitch"/>, after the theme and the app configuration.</summary>
    public string? Container { get; init; }
    /// <summary>Classes merged into the <c>input</c> slot of <see cref="KlySwitch"/>, after the theme and the app configuration.</summary>
    public string? Input { get; init; }
    /// <summary>Classes merged into the <c>thumb</c> slot of <see cref="KlySwitch"/>, after the theme and the app configuration.</summary>
    public string? Thumb { get; init; }
    /// <summary>Classes merged into the <c>wrapper</c> slot of <see cref="KlySwitch"/>, after the theme and the app configuration.</summary>
    public string? Wrapper { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlySwitch"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlySwitch"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "container" => Container,
        "input" => Input,
        "thumb" => Thumb,
        "wrapper" => Wrapper,
        "label" => Label,
        "description" => Description,
        _ => null,
    };
}

/// <summary>
/// On/off switch, a native checkbox with <c>role="switch"</c>. Its look follows the native checked state,
/// so it is correct in static server rendering and after a form post.
/// </summary>
public partial class KlySwitch
{
    private SlotSet _slots = default!;

    /// <summary>Text next to the switch. A <see cref="KlyFormField"/> label also names it.</summary>
    [Parameter] public string? Label { get; set; }
    /// <summary>Text under the label of the switch.</summary>
    [Parameter] public string? Description { get; set; }
    /// <summary>Per-instance class overrides of the slots of the switch, see <see cref="SwitchUi"/>.</summary>
    [Parameter] public SwitchUi? Ui { get; set; }

    private string? DescriptionId => Description is null ? null : ControlId + "-description";

    /// <inheritdoc />
    protected override void OnParametersSet() =>
        _slots = ResolveSlots(Options.Switch, new Dictionary<string, string?>
        {
            ["size"] = VariantValue.Of(EffectiveSize),
        }, Ui, rootSlot: "root");

    private Dictionary<string, object> Attributes()
    {
        var attributes = ControlAttributes(_slots["input"], DescriptionId);
        attributes["role"] = "switch";
        return attributes;
    }
}
