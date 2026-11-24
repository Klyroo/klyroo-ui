using Lux.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Lux.UI;

public sealed record SwitchUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Container { get; init; }
    public string? Input { get; init; }
    public string? Thumb { get; init; }
    public string? Wrapper { get; init; }
    public string? Label { get; init; }
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
public partial class BxSwitch
{
    private SlotSet _slots = default!;

    [Parameter] public string? Label { get; set; }
    [Parameter] public string? Description { get; set; }
    [Parameter] public SwitchUi? Ui { get; set; }

    private string? DescriptionId => Description is null ? null : ControlId + "-description";

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
