using Blux.UI.Theming;

namespace Blux.UI;

// Per-instance class overrides, one property per slot of the component theme.

public sealed record BadgeUi : ISlotOverrides
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

public sealed record AvatarUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Image { get; init; }
    public string? Fallback { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "image" => Image,
        "fallback" => Fallback,
        _ => null,
    };
}

public sealed record CardUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Header { get; init; }
    public string? Body { get; init; }
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

public sealed record AlertUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Icon { get; init; }
    public string? Wrapper { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Actions { get; init; }
    public string? Close { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "icon" => Icon,
        "wrapper" => Wrapper,
        "title" => Title,
        "description" => Description,
        "actions" => Actions,
        "close" => Close,
        _ => null,
    };
}

public sealed record InputUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Base { get; init; }
    public string? Leading { get; init; }
    public string? LeadingIcon { get; init; }
    public string? Trailing { get; init; }
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

public sealed record TextareaUi : ISlotOverrides
{
    public string? Base { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "base" => Base,
        _ => null,
    };
}

public sealed record CheckboxUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Container { get; init; }
    public string? Base { get; init; }
    public string? Icon { get; init; }
    public string? IndeterminateIcon { get; init; }
    public string? Wrapper { get; init; }
    public string? Label { get; init; }
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

public sealed record RadioGroupUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Legend { get; init; }
    public string? Fieldset { get; init; }
    public string? Item { get; init; }
    public string? Container { get; init; }
    public string? Base { get; init; }
    public string? Indicator { get; init; }
    public string? Wrapper { get; init; }
    public string? Label { get; init; }
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

public sealed record SelectUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Base { get; init; }
    public string? Label { get; init; }
    public string? TrailingIcon { get; init; }
    public string? Content { get; init; }
    public string? Listbox { get; init; }
    public string? Item { get; init; }
    public string? ItemLabel { get; init; }
    public string? ItemIcon { get; init; }
    public string? ItemLeadingIcon { get; init; }
    public string? ItemCount { get; init; }
    public string? Group { get; init; }
    public string? GroupLabel { get; init; }

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
        _ => null,
    };
}

public sealed record FormFieldUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? LabelWrapper { get; init; }
    public string? Label { get; init; }
    public string? Hint { get; init; }
    public string? Description { get; init; }
    public string? Container { get; init; }
    public string? Error { get; init; }
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

public sealed record ModalUi : ISlotOverrides
{
    public string? Content { get; init; }
    public string? Header { get; init; }
    public string? Wrapper { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Close { get; init; }
    public string? Body { get; init; }
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

public sealed record TooltipUi : ISlotOverrides
{
    public string? Content { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "content" => Content,
        _ => null,
    };
}

public sealed record DropdownMenuUi : ISlotOverrides
{
    public string? Content { get; init; }
    public string? Group { get; init; }
    public string? Label { get; init; }
    public string? Separator { get; init; }
    public string? Item { get; init; }
    public string? ItemIcon { get; init; }
    public string? ItemLabel { get; init; }
    public string? ItemShortcut { get; init; }

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
        _ => null,
    };
}

public sealed record ToastUi : ISlotOverrides
{
    public string? Viewport { get; init; }
    public string? Root { get; init; }
    public string? Wrapper { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
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

public sealed record TabsUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? List { get; init; }
    public string? Trigger { get; init; }
    public string? Icon { get; init; }
    public string? Label { get; init; }
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

public sealed record AccordionUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Item { get; init; }
    public string? Header { get; init; }
    public string? Trigger { get; init; }
    public string? LeadingIcon { get; init; }
    public string? Label { get; init; }
    public string? TrailingIcon { get; init; }
    public string? Content { get; init; }
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

public sealed record TableUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Base { get; init; }
    public string? Caption { get; init; }
    public string? Thead { get; init; }
    public string? Tr { get; init; }
    public string? Th { get; init; }
    public string? SortButton { get; init; }
    public string? SortIcon { get; init; }
    public string? Tbody { get; init; }
    public string? Td { get; init; }
    public string? Empty { get; init; }

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
        _ => null,
    };
}

public enum CardVariant
{
    Outline,
    Soft,
    Subtle,
}

public enum TabsVariant
{
    Pill,
    Link,
}

public enum BxOrientation
{
    Horizontal,
    Vertical,
}
