using Klyroo.UI.Theming;

namespace Klyroo.UI;

// Per-instance class overrides, one property per slot of the component theme.

/// <summary>Per-instance class overrides of the slots of <see cref="KlyBadge"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record BadgeUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyBadge"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyBadge"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>leadingIcon</c> slot of <see cref="KlyBadge"/>, after the theme and the app configuration.</summary>
    public string? LeadingIcon { get; init; }
    /// <summary>Classes merged into the <c>trailingIcon</c> slot of <see cref="KlyBadge"/>, after the theme and the app configuration.</summary>
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
/// Per-instance class overrides of the slots of <see cref="KlyAvatar"/> and <see cref="KlyColorModeAvatar"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record AvatarUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyAvatar"/> and <see cref="KlyColorModeAvatar"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>image</c> slot of <see cref="KlyAvatar"/> and <see cref="KlyColorModeAvatar"/>, after the theme and the app configuration.</summary>
    public string? Image { get; init; }
    /// <summary>Classes merged into the <c>fallback</c> slot of <see cref="KlyAvatar"/> and <see cref="KlyColorModeAvatar"/>, after the theme and the app configuration.</summary>
    public string? Fallback { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "image" => Image,
        "fallback" => Fallback,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyCard"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record CardUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyCard"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>header</c> slot of <see cref="KlyCard"/>, after the theme and the app configuration.</summary>
    public string? Header { get; init; }
    /// <summary>Classes merged into the <c>body</c> slot of <see cref="KlyCard"/>, after the theme and the app configuration.</summary>
    public string? Body { get; init; }
    /// <summary>Classes merged into the <c>footer</c> slot of <see cref="KlyCard"/>, after the theme and the app configuration.</summary>
    public string? Footer { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "header" => Header,
        "body" => Body,
        "footer" => Footer,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyAlert"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record AlertUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyAlert"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyAlert"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>wrapper</c> slot of <see cref="KlyAlert"/>, after the theme and the app configuration.</summary>
    public string? Wrapper { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyAlert"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyAlert"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>actions</c> slot of <see cref="KlyAlert"/>, after the theme and the app configuration.</summary>
    public string? Actions { get; init; }
    /// <summary>Classes merged into the <c>close</c> slot of <see cref="KlyAlert"/>, after the theme and the app configuration.</summary>
    public string? Close { get; init; }

    /// <summary>Classes merged into the <c>toggle</c> slot of <see cref="KlyAlert"/>, after the theme and the app configuration.</summary>
    public string? Toggle { get; init; }
    /// <summary>Classes merged into the <c>toggleIcon</c> slot of <see cref="KlyAlert"/>, after the theme and the app configuration.</summary>
    public string? ToggleIcon { get; init; }
    /// <summary>Classes merged into the <c>detailsPanel</c> slot of <see cref="KlyAlert"/>, after the theme and the app configuration.</summary>
    public string? DetailsPanel { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "icon" => Icon,
        "wrapper" => Wrapper,
        "title" => Title,
        "description" => Description,
        "actions" => Actions,
        "close" => Close,
        "toggle" => Toggle,
        "toggleIcon" => ToggleIcon,
        "detailsPanel" => DetailsPanel,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyInput"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record InputUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyInput"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyInput"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }
    /// <summary>Classes merged into the <c>leading</c> slot of <see cref="KlyInput"/>, after the theme and the app configuration.</summary>
    public string? Leading { get; init; }
    /// <summary>Classes merged into the <c>leadingIcon</c> slot of <see cref="KlyInput"/>, after the theme and the app configuration.</summary>
    public string? LeadingIcon { get; init; }
    /// <summary>Classes merged into the <c>trailing</c> slot of <see cref="KlyInput"/>, after the theme and the app configuration.</summary>
    public string? Trailing { get; init; }
    /// <summary>Classes merged into the <c>trailingIcon</c> slot of <see cref="KlyInput"/>, after the theme and the app configuration.</summary>
    public string? TrailingIcon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "base" => Base,
        "leading" => Leading,
        "leadingIcon" => LeadingIcon,
        "trailing" => Trailing,
        "trailingIcon" => TrailingIcon,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyTextarea"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record TextareaUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyTextarea"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "base" => Base,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyCheckbox"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record CheckboxUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyCheckbox"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>container</c> slot of <see cref="KlyCheckbox"/>, after the theme and the app configuration.</summary>
    public string? Container { get; init; }
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyCheckbox"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyCheckbox"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>indeterminateIcon</c> slot of <see cref="KlyCheckbox"/>, after the theme and the app configuration.</summary>
    public string? IndeterminateIcon { get; init; }
    /// <summary>Classes merged into the <c>wrapper</c> slot of <see cref="KlyCheckbox"/>, after the theme and the app configuration.</summary>
    public string? Wrapper { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyCheckbox"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyCheckbox"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "container" => Container,
        "base" => Base,
        "icon" => Icon,
        "indeterminateIcon" => IndeterminateIcon,
        "wrapper" => Wrapper,
        "label" => Label,
        "description" => Description,
        _ => null,
    };
}

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyRadioGroup{TValue}"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record RadioGroupUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyRadioGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>legend</c> slot of <see cref="KlyRadioGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Legend { get; init; }
    /// <summary>Classes merged into the <c>fieldset</c> slot of <see cref="KlyRadioGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Fieldset { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyRadioGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>container</c> slot of <see cref="KlyRadioGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Container { get; init; }
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyRadioGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }
    /// <summary>Classes merged into the <c>indicator</c> slot of <see cref="KlyRadioGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Indicator { get; init; }
    /// <summary>Classes merged into the <c>wrapper</c> slot of <see cref="KlyRadioGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Wrapper { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyRadioGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyRadioGroup{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "legend" => Legend,
        "fieldset" => Fieldset,
        "item" => Item,
        "container" => Container,
        "base" => Base,
        "indicator" => Indicator,
        "wrapper" => Wrapper,
        "label" => Label,
        "description" => Description,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlySelect{TValue}"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record SelectUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlySelect{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlySelect{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlySelect{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>trailingIcon</c> slot of <see cref="KlySelect{TValue}"/>, after the theme and the app configuration.</summary>
    public string? TrailingIcon { get; init; }
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlySelect{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }
    /// <summary>Classes merged into the <c>listbox</c> slot of <see cref="KlySelect{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Listbox { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlySelect{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>itemLabel</c> slot of <see cref="KlySelect{TValue}"/>, after the theme and the app configuration.</summary>
    public string? ItemLabel { get; init; }
    /// <summary>Classes merged into the <c>itemIcon</c> slot of <see cref="KlySelect{TValue}"/>, after the theme and the app configuration.</summary>
    public string? ItemIcon { get; init; }
    /// <summary>Classes merged into the <c>itemLeadingIcon</c> slot of <see cref="KlySelect{TValue}"/>, after the theme and the app configuration.</summary>
    public string? ItemLeadingIcon { get; init; }
    /// <summary>Classes merged into the <c>itemCount</c> slot of <see cref="KlySelect{TValue}"/>, after the theme and the app configuration.</summary>
    public string? ItemCount { get; init; }
    /// <summary>Classes merged into the <c>group</c> slot of <see cref="KlySelect{TValue}"/>, after the theme and the app configuration.</summary>
    public string? Group { get; init; }
    /// <summary>Classes merged into the <c>groupLabel</c> slot of <see cref="KlySelect{TValue}"/>, after the theme and the app configuration.</summary>
    public string? GroupLabel { get; init; }
    /// <summary>Classes merged into the <c>leadingIcon</c> slot of <see cref="KlySelect{TValue}"/>, after the theme and the app configuration.</summary>
    public string? LeadingIcon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "base" => Base,
        "label" => Label,
        "trailingIcon" => TrailingIcon,
        "content" => Content,
        "listbox" => Listbox,
        "item" => Item,
        "itemLabel" => ItemLabel,
        "itemIcon" => ItemIcon,
        "itemLeadingIcon" => ItemLeadingIcon,
        "itemCount" => ItemCount,
        "group" => Group,
        "groupLabel" => GroupLabel,
        "leadingIcon" => LeadingIcon,
        _ => null,
    };
}

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyFormField"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record FormFieldUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyFormField"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>labelWrapper</c> slot of <see cref="KlyFormField"/>, after the theme and the app configuration.</summary>
    public string? LabelWrapper { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyFormField"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>hint</c> slot of <see cref="KlyFormField"/>, after the theme and the app configuration.</summary>
    public string? Hint { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyFormField"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>container</c> slot of <see cref="KlyFormField"/>, after the theme and the app configuration.</summary>
    public string? Container { get; init; }
    /// <summary>Classes merged into the <c>error</c> slot of <see cref="KlyFormField"/>, after the theme and the app configuration.</summary>
    public string? Error { get; init; }
    /// <summary>Classes merged into the <c>help</c> slot of <see cref="KlyFormField"/>, after the theme and the app configuration.</summary>
    public string? Help { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "labelWrapper" => LabelWrapper,
        "label" => Label,
        "hint" => Hint,
        "description" => Description,
        "container" => Container,
        "error" => Error,
        "help" => Help,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyModal"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record ModalUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlyModal"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }
    /// <summary>Classes merged into the <c>header</c> slot of <see cref="KlyModal"/>, after the theme and the app configuration.</summary>
    public string? Header { get; init; }
    /// <summary>Classes merged into the <c>wrapper</c> slot of <see cref="KlyModal"/>, after the theme and the app configuration.</summary>
    public string? Wrapper { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyModal"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyModal"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>close</c> slot of <see cref="KlyModal"/>, after the theme and the app configuration.</summary>
    public string? Close { get; init; }
    /// <summary>Classes merged into the <c>body</c> slot of <see cref="KlyModal"/>, after the theme and the app configuration.</summary>
    public string? Body { get; init; }
    /// <summary>Classes merged into the <c>footer</c> slot of <see cref="KlyModal"/>, after the theme and the app configuration.</summary>
    public string? Footer { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "content" => Content,
        "header" => Header,
        "wrapper" => Wrapper,
        "title" => Title,
        "description" => Description,
        "close" => Close,
        "body" => Body,
        "footer" => Footer,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyTooltip"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record TooltipUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlyTooltip"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "content" => Content,
        _ => null,
    };
}

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyDropdownMenu"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record DropdownMenuUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlyDropdownMenu"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }
    /// <summary>Classes merged into the <c>group</c> slot of <see cref="KlyDropdownMenu"/>, after the theme and the app configuration.</summary>
    public string? Group { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyDropdownMenu"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>separator</c> slot of <see cref="KlyDropdownMenu"/>, after the theme and the app configuration.</summary>
    public string? Separator { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyDropdownMenu"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>itemIcon</c> slot of <see cref="KlyDropdownMenu"/>, after the theme and the app configuration.</summary>
    public string? ItemIcon { get; init; }
    /// <summary>Classes merged into the <c>itemLabel</c> slot of <see cref="KlyDropdownMenu"/>, after the theme and the app configuration.</summary>
    public string? ItemLabel { get; init; }
    /// <summary>Classes merged into the <c>itemShortcut</c> slot of <see cref="KlyDropdownMenu"/>, after the theme and the app configuration.</summary>
    public string? ItemShortcut { get; init; }

    /// <summary>The arrow at the end of an item that opens a submenu.</summary>
    public string? ItemChevron { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "content" => Content,
        "group" => Group,
        "label" => Label,
        "separator" => Separator,
        "item" => Item,
        "itemIcon" => ItemIcon,
        "itemLabel" => ItemLabel,
        "itemShortcut" => ItemShortcut,
        "itemChevron" => ItemChevron,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyToaster"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record ToastUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>viewport</c> slot of <see cref="KlyToaster"/>, after the theme and the app configuration.</summary>
    public string? Viewport { get; init; }
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyToaster"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>wrapper</c> slot of <see cref="KlyToaster"/>, after the theme and the app configuration.</summary>
    public string? Wrapper { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyToaster"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyToaster"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>close</c> slot of <see cref="KlyToaster"/>, after the theme and the app configuration.</summary>
    public string? Close { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "viewport" => Viewport,
        "root" => Root,
        "wrapper" => Wrapper,
        "title" => Title,
        "description" => Description,
        "close" => Close,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyTabs"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record TabsUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyTabs"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>list</c> slot of <see cref="KlyTabs"/>, after the theme and the app configuration.</summary>
    public string? List { get; init; }
    /// <summary>Classes merged into the <c>trigger</c> slot of <see cref="KlyTabs"/>, after the theme and the app configuration.</summary>
    public string? Trigger { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyTabs"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyTabs"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlyTabs"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "list" => List,
        "trigger" => Trigger,
        "icon" => Icon,
        "label" => Label,
        "content" => Content,
        _ => null,
    };
}

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyAccordion"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record AccordionUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyAccordion"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyAccordion"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>header</c> slot of <see cref="KlyAccordion"/>, after the theme and the app configuration.</summary>
    public string? Header { get; init; }
    /// <summary>Classes merged into the <c>trigger</c> slot of <see cref="KlyAccordion"/>, after the theme and the app configuration.</summary>
    public string? Trigger { get; init; }
    /// <summary>Classes merged into the <c>leadingIcon</c> slot of <see cref="KlyAccordion"/>, after the theme and the app configuration.</summary>
    public string? LeadingIcon { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyAccordion"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>trailingIcon</c> slot of <see cref="KlyAccordion"/>, after the theme and the app configuration.</summary>
    public string? TrailingIcon { get; init; }
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlyAccordion"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }
    /// <summary>Classes merged into the <c>body</c> slot of <see cref="KlyAccordion"/>, after the theme and the app configuration.</summary>
    public string? Body { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "item" => Item,
        "header" => Header,
        "trigger" => Trigger,
        "leadingIcon" => LeadingIcon,
        "label" => Label,
        "trailingIcon" => TrailingIcon,
        "content" => Content,
        "body" => Body,
        _ => null,
    };
}

/// <summary>
/// Per-instance class overrides of the slots of <see cref="KlyTable{TItem}"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.
/// </summary>
public sealed record TableUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyTable{TItem}"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyTable{TItem}"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }
    /// <summary>Classes merged into the <c>caption</c> slot of <see cref="KlyTable{TItem}"/>, after the theme and the app configuration.</summary>
    public string? Caption { get; init; }
    /// <summary>Classes merged into the <c>thead</c> slot of <see cref="KlyTable{TItem}"/>, after the theme and the app configuration.</summary>
    public string? Thead { get; init; }
    /// <summary>Classes merged into the <c>tr</c> slot of <see cref="KlyTable{TItem}"/>, after the theme and the app configuration.</summary>
    public string? Tr { get; init; }
    /// <summary>Classes merged into the <c>th</c> slot of <see cref="KlyTable{TItem}"/>, after the theme and the app configuration.</summary>
    public string? Th { get; init; }
    /// <summary>Classes merged into the <c>sortButton</c> slot of <see cref="KlyTable{TItem}"/>, after the theme and the app configuration.</summary>
    public string? SortButton { get; init; }
    /// <summary>Classes merged into the <c>sortIcon</c> slot of <see cref="KlyTable{TItem}"/>, after the theme and the app configuration.</summary>
    public string? SortIcon { get; init; }
    /// <summary>Classes merged into the <c>tbody</c> slot of <see cref="KlyTable{TItem}"/>, after the theme and the app configuration.</summary>
    public string? Tbody { get; init; }
    /// <summary>Classes merged into the <c>td</c> slot of <see cref="KlyTable{TItem}"/>, after the theme and the app configuration.</summary>
    public string? Td { get; init; }
    /// <summary>Classes merged into the <c>empty</c> slot of <see cref="KlyTable{TItem}"/>, after the theme and the app configuration.</summary>
    public string? Empty { get; init; }

    /// <summary>Classes merged into the <c>tfoot</c> slot of <see cref="KlyTable{TItem}"/>, after the theme and the app configuration.</summary>
    public string? Tfoot { get; init; }
    /// <summary>Classes merged into the <c>tf</c> slot of <see cref="KlyTable{TItem}"/>, after the theme and the app configuration.</summary>
    public string? Tf { get; init; }
    /// <summary>Classes merged into the <c>cellEnd</c> slot of <see cref="KlyTable{TItem}"/>, after the theme and the app configuration.</summary>
    public string? CellEnd { get; init; }
    /// <summary>Classes merged into the <c>cellNumeric</c> slot of <see cref="KlyTable{TItem}"/>, after the theme and the app configuration.</summary>
    public string? CellNumeric { get; init; }
    /// <summary>Classes merged into the <c>cellMono</c> slot of <see cref="KlyTable{TItem}"/>, after the theme and the app configuration.</summary>
    public string? CellMono { get; init; }
    /// <summary>Classes merged into the <c>more</c> slot of <see cref="KlyTable{TItem}"/>, after the theme and the app configuration.</summary>
    public string? More { get; init; }
    /// <summary>Classes merged into the <c>moreIcon</c> slot of <see cref="KlyTable{TItem}"/>, after the theme and the app configuration.</summary>
    public string? MoreIcon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "base" => Base,
        "caption" => Caption,
        "thead" => Thead,
        "tr" => Tr,
        "th" => Th,
        "sortButton" => SortButton,
        "sortIcon" => SortIcon,
        "tbody" => Tbody,
        "td" => Td,
        "empty" => Empty,
        "tfoot" => Tfoot,
        "tf" => Tf,
        "cellEnd" => CellEnd,
        "cellNumeric" => CellNumeric,
        "cellMono" => CellMono,
        "more" => More,
        "moreIcon" => MoreIcon,
        _ => null,
    };
}

/// <summary>Look of a <see cref="KlyCard"/>.</summary>
public enum CardVariant
{
    /// <summary>A border around the card.</summary>
    Outline,
    /// <summary>A tinted background.</summary>
    Soft,
    /// <summary>A tinted background with a border.</summary>
    Subtle,
}

/// <summary>Look of <see cref="KlyTabs"/>.</summary>
public enum TabsVariant
{
    /// <summary>The selected tab is a filled pill.</summary>
    Pill,
    /// <summary>The selected tab is marked by an underline.</summary>
    Link,
}

/// <summary>Direction of a layout.</summary>
public enum KlyOrientation
{
    /// <summary>Along the line of text.</summary>
    Horizontal,
    /// <summary>Top to bottom.</summary>
    Vertical,
}
