using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Klyroo.UI;

/// <summary>Look of a <see cref="KlyButton"/>.</summary>
public enum ButtonVariant
{
    /// <summary>A filled button in the semantic color.</summary>
    Solid,
    /// <summary>A border in the semantic color and no fill.</summary>
    Outline,
    /// <summary>A light tint of the semantic color.</summary>
    Soft,
    /// <summary>A light tint with a border.</summary>
    Subtle,
    /// <summary>No border and no fill until hovered.</summary>
    Ghost,
    /// <summary>Looks like a text link.</summary>
    Link,
}

/// <summary>Per-instance class overrides of <see cref="KlyButton"/> slots.</summary>
public sealed record ButtonUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyButton"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyButton"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>leadingIcon</c> slot of <see cref="KlyButton"/>, after the theme and the app configuration.</summary>
    public string? LeadingIcon { get; init; }
    /// <summary>Classes merged into the <c>trailingIcon</c> slot of <see cref="KlyButton"/>, after the theme and the app configuration.</summary>
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
public partial class KlyButton
{
    /// <summary>Text of the button. Use <c>ChildContent</c> for markup.</summary>
    [Parameter] public string? Label { get; set; }
    /// <summary>Content of the button, instead of <see cref="Label"/>.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    /// <summary>
    /// Semantic color of the button: a built-in <see cref="KlyColor"/> or the name of a color you defined. When not set, <see cref="KlyDefaults.Color"/> applies, else primary.
    /// </summary>
    [Parameter] public KlyColor Color { get => _colorParameter ?? DefaultColor(KlyColor.Primary); set => _colorParameter = value; }
    private KlyColor? _colorParameter;
    /// <summary>Look of the button: <c>Solid</c>, <c>Outline</c>, <c>Soft</c>, <c>Subtle</c>, <c>Ghost</c> or <c>Link</c>.</summary>
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Solid;
    /// <summary>Size of the button, from <c>Xs</c> to <c>Xl</c>. When not set, <see cref="KlyDefaults.Size"/> applies, else medium.</summary>
    [Parameter] public KlySize Size { get => _sizeParameter ?? DefaultSize(KlySize.Md); set => _sizeParameter = value; }
    private KlySize? _sizeParameter;
    /// <summary>Icon before the label, as markup such as a <see cref="KlyIcon"/>. Replaced by a spinner while <see cref="Loading"/>.</summary>
    [Parameter] public RenderFragment? LeadingIcon { get; set; }
    /// <summary>Icon after the label, as markup such as a <see cref="KlyIcon"/>.</summary>
    [Parameter] public RenderFragment? TrailingIcon { get; set; }

    /// <summary>Shows a spinner in place of the leading icon and disables the button.</summary>
    [Parameter] public bool Loading { get; set; }
    /// <summary>Disables the button.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Full width.</summary>
    [Parameter] public bool Block { get; set; }

    /// <summary>Equal padding on every side, for icon-only buttons (give them an <c>aria-label</c>).</summary>
    [Parameter] public bool Square { get; set; }

    /// <summary>The <c>type</c> attribute of the button: <see cref="ButtonType.Button"/> (the default), <see cref="ButtonType.Submit"/> or <see cref="ButtonType.Reset"/>.</summary>
    [Parameter] public ButtonType Type { get; set; } = ButtonType.Button;

    /// <summary>Address to link to. When set the button renders as a link.</summary>
    [Parameter] public string? Href { get; set; }
    /// <summary>The <c>target</c> attribute of the link, such as <c>_blank</c>. It applies when <see cref="Href"/> is set.</summary>
    [Parameter] public string? Target { get; set; }
    /// <summary>Raised when the button is clicked. The button stays disabled until the page is interactive when this is set.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    /// <summary>Per-instance class overrides of the slots of the button, see <see cref="ButtonUi"/>.</summary>
    [Parameter] public ButtonUi? Ui { get; set; }

    // A click handler needs an interactive renderer. During prerendering or static rendering the button would look
    // usable but do nothing, so it is disabled until interactivity starts. Submit buttons and links work without it.
    private bool IsInert => OnClick.HasDelegate && Href is null && Type == ButtonType.Button && !RendererInfo.IsInteractive;

    private bool IsDisabled => Disabled || Loading || IsInert;

    // No delegate means no handler is attached, so a plain button costs no round trip in Server mode.
    private EventCallback<MouseEventArgs> ClickHandler => IsDisabled ? default : OnClick;

    /// <inheritdoc />
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
