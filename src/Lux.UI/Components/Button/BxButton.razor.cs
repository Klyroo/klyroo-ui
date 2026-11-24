using Lux.UI.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Lux.UI;

public enum ButtonVariant
{
    Solid,
    Outline,
    Soft,
    Subtle,
    Ghost,
    Link,
}

/// <summary>Per-instance class overrides of <see cref="BxButton"/> slots.</summary>
public sealed record ButtonUi : ISlotOverrides
{
    public string? Base { get; init; }
    public string? Label { get; init; }
    public string? LeadingIcon { get; init; }
    public string? TrailingIcon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "base" => Base,
        "label" => Label,
        "leadingIcon" => LeadingIcon,
        "trailingIcon" => TrailingIcon,
        _ => null,
    };
}

/// <summary>
/// Button, rendered as a <c>&lt;button&gt;</c>, or as a link when <see cref="Href"/> is set.
/// Works in static server rendering: a submit button or a link needs no interactivity.
/// </summary>
public partial class BxButton
{
    [Parameter] public string? Label { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public BxColor Color { get => _colorParameter ?? DefaultColor(BxColor.Primary); set => _colorParameter = value; }
    private BxColor? _colorParameter;
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Solid;
    [Parameter] public BxSize Size { get => _sizeParameter ?? DefaultSize(BxSize.Md); set => _sizeParameter = value; }
    private BxSize? _sizeParameter;
    [Parameter] public RenderFragment? LeadingIcon { get; set; }
    [Parameter] public RenderFragment? TrailingIcon { get; set; }

    /// <summary>Shows a spinner in place of the leading icon and disables the button.</summary>
    [Parameter] public bool Loading { get; set; }
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Full width.</summary>
    [Parameter] public bool Block { get; set; }

    /// <summary>Equal padding on every side, for icon-only buttons (give them an <c>aria-label</c>).</summary>
    [Parameter] public bool Square { get; set; }

    /// <summary>The <c>type</c> attribute of the button: button, submit or reset.</summary>
    [Parameter] public string Type { get; set; } = "button";

    [Parameter] public string? Href { get; set; }
    [Parameter] public string? Target { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    [Parameter] public ButtonUi? Ui { get; set; }

    // A click handler needs an interactive renderer. During prerendering or static rendering the button would look
    // usable but do nothing, so it is disabled until interactivity starts. Submit buttons and links work without it.
    private bool IsInert => OnClick.HasDelegate && Href is null && Type == "button" && !RendererInfo.IsInteractive;

    private bool IsDisabled => Disabled || Loading || IsInert;

    // No delegate means no handler is attached, so a plain button costs no round trip in Server mode.
    private EventCallback<MouseEventArgs> ClickHandler => IsDisabled ? default : OnClick;

    protected override void OnParametersSet()
    {
        _slots = ResolveSlots(Options.Button, new Dictionary<string, string?>
        {
            ["variant"] = Variant switch
            {
                ButtonVariant.Solid => "solid",
                ButtonVariant.Outline => "outline",
                ButtonVariant.Soft => "soft",
                ButtonVariant.Subtle => "subtle",
                ButtonVariant.Ghost => "ghost",
                ButtonVariant.Link => "link",
                _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null),
            },
            ["size"] = VariantValue.Of(Size),
            ["block"] = VariantValue.Of(Block),
            ["square"] = VariantValue.Of(Square),
            ["loading"] = VariantValue.Of(Loading),
        }, Ui, rootSlot: "base");
    }
}
