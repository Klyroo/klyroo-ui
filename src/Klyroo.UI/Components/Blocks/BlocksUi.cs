using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of the slots of <see cref="KlyHeading"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record HeadingUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyHeading"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>lead</c> slot of <see cref="KlyHeading"/>, after the theme and the app configuration.</summary>
    public string? Lead { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyHeading"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>text</c> slot of <see cref="KlyHeading"/>, after the theme and the app configuration.</summary>
    public string? Text { get; init; }
    /// <summary>Classes merged into the <c>titleRow</c> slot of <see cref="KlyHeading"/>, after the theme and the app configuration.</summary>
    public string? TitleRow { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyHeading"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>count</c> slot of <see cref="KlyHeading"/>, after the theme and the app configuration.</summary>
    public string? Count { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyHeading"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>skeleton</c> slot of <see cref="KlyHeading"/>, after the theme and the app configuration.</summary>
    public string? Skeleton { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "lead" => Lead,
        "icon" => Icon,
        "text" => Text,
        "titleRow" => TitleRow,
        "title" => Title,
        "count" => Count,
        "description" => Description,
        "skeleton" => Skeleton,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyText"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record TextUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyText"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyText"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>paragraph</c> slot of <see cref="KlyText"/>, after the theme and the app configuration.</summary>
    public string? Paragraph { get; init; }
    /// <summary>Classes merged into the <c>body</c> slot of <see cref="KlyText"/>, after the theme and the app configuration.</summary>
    public string? Body { get; init; }
    /// <summary>Classes merged into the <c>toggle</c> slot of <see cref="KlyText"/>, after the theme and the app configuration.</summary>
    public string? Toggle { get; init; }
    /// <summary>Classes merged into the <c>footer</c> slot of <see cref="KlyText"/>, after the theme and the app configuration.</summary>
    public string? Footer { get; init; }
    /// <summary>Classes merged into the <c>skeleton</c> slot of <see cref="KlyText"/>, after the theme and the app configuration.</summary>
    public string? Skeleton { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "title" => Title,
        "paragraph" => Paragraph,
        "body" => Body,
        "toggle" => Toggle,
        "footer" => Footer,
        "skeleton" => Skeleton,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyStat"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record StatUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyStat"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyStat"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>row</c> slot of <see cref="KlyStat"/>, after the theme and the app configuration.</summary>
    public string? Row { get; init; }
    /// <summary>Classes merged into the <c>valueGroup</c> slot of <see cref="KlyStat"/>, after the theme and the app configuration.</summary>
    public string? ValueGroup { get; init; }
    /// <summary>Classes merged into the <c>value</c> slot of <see cref="KlyStat"/>, after the theme and the app configuration.</summary>
    public string? Value { get; init; }
    /// <summary>Classes merged into the <c>unit</c> slot of <see cref="KlyStat"/>, after the theme and the app configuration.</summary>
    public string? Unit { get; init; }
    /// <summary>Classes merged into the <c>delta</c> slot of <see cref="KlyStat"/>, after the theme and the app configuration.</summary>
    public string? Delta { get; init; }
    /// <summary>Classes merged into the <c>sparkline</c> slot of <see cref="KlyStat"/>, after the theme and the app configuration.</summary>
    public string? Sparkline { get; init; }
    /// <summary>Classes merged into the <c>caption</c> slot of <see cref="KlyStat"/>, after the theme and the app configuration.</summary>
    public string? Caption { get; init; }
    /// <summary>Classes merged into the <c>skeleton</c> slot of <see cref="KlyStat"/>, after the theme and the app configuration.</summary>
    public string? Skeleton { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "label" => Label,
        "row" => Row,
        "valueGroup" => ValueGroup,
        "value" => Value,
        "unit" => Unit,
        "delta" => Delta,
        "sparkline" => Sparkline,
        "caption" => Caption,
        "skeleton" => Skeleton,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyStatGroup"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record StatGroupUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyStatGroup"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyStatGroup"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>grid</c> slot of <see cref="KlyStatGroup"/>, after the theme and the app configuration.</summary>
    public string? Grid { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyStatGroup"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyStatGroup"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>value</c> slot of <see cref="KlyStatGroup"/>, after the theme and the app configuration.</summary>
    public string? Value { get; init; }
    /// <summary>Classes merged into the <c>caption</c> slot of <see cref="KlyStatGroup"/>, after the theme and the app configuration.</summary>
    public string? Caption { get; init; }
    /// <summary>Classes merged into the <c>skeleton</c> slot of <see cref="KlyStatGroup"/>, after the theme and the app configuration.</summary>
    public string? Skeleton { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "title" => Title,
        "grid" => Grid,
        "item" => Item,
        "label" => Label,
        "value" => Value,
        "caption" => Caption,
        "skeleton" => Skeleton,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyFacts"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record FactsUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>group</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? Group { get; init; }
    /// <summary>Classes merged into the <c>groupHeader</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? GroupHeader { get; init; }
    /// <summary>Classes merged into the <c>groupTitle</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? GroupTitle { get; init; }
    /// <summary>Classes merged into the <c>groupAside</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? GroupAside { get; init; }
    /// <summary>Classes merged into the <c>list</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? List { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>labelRow</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? LabelRow { get; init; }
    /// <summary>Classes merged into the <c>labelIcon</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? LabelIcon { get; init; }
    /// <summary>Classes merged into the <c>hint</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? Hint { get; init; }
    /// <summary>Classes merged into the <c>value</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? Value { get; init; }
    /// <summary>Classes merged into the <c>valueMono</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? ValueMono { get; init; }
    /// <summary>Classes merged into the <c>valueEmphasis</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? ValueEmphasis { get; init; }
    /// <summary>Classes merged into the <c>valueSkeleton</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? ValueSkeleton { get; init; }
    /// <summary>Classes merged into the <c>empty</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? Empty { get; init; }
    /// <summary>Classes merged into the <c>link</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? Link { get; init; }
    /// <summary>Classes merged into the <c>copy</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? Copy { get; init; }
    /// <summary>Classes merged into the <c>copyIcon</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? CopyIcon { get; init; }
    /// <summary>Classes merged into the <c>more</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? More { get; init; }
    /// <summary>Classes merged into the <c>moreIcon</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? MoreIcon { get; init; }
    /// <summary>Classes merged into the <c>none</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? None { get; init; }
    /// <summary>Classes merged into the <c>skeleton</c> slot of <see cref="KlyFacts"/>, after the theme and the app configuration.</summary>
    public string? Skeleton { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "group" => Group,
        "groupHeader" => GroupHeader,
        "groupTitle" => GroupTitle,
        "groupAside" => GroupAside,
        "list" => List,
        "item" => Item,
        "label" => Label,
        "labelRow" => LabelRow,
        "labelIcon" => LabelIcon,
        "hint" => Hint,
        "value" => Value,
        "valueMono" => ValueMono,
        "valueEmphasis" => ValueEmphasis,
        "valueSkeleton" => ValueSkeleton,
        "empty" => Empty,
        "link" => Link,
        "copy" => Copy,
        "copyIcon" => CopyIcon,
        "more" => More,
        "moreIcon" => MoreIcon,
        "none" => None,
        "skeleton" => Skeleton,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyBreakdown"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record BreakdownUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyBreakdown"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyBreakdown"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>toggle</c> slot of <see cref="KlyBreakdown"/>, after the theme and the app configuration.</summary>
    public string? Toggle { get; init; }
    /// <summary>Classes merged into the <c>toggleIcon</c> slot of <see cref="KlyBreakdown"/>, after the theme and the app configuration.</summary>
    public string? ToggleIcon { get; init; }
    /// <summary>Classes merged into the <c>columns</c> slot of <see cref="KlyBreakdown"/>, after the theme and the app configuration.</summary>
    public string? Columns { get; init; }
    /// <summary>Classes merged into the <c>columnHeader</c> slot of <see cref="KlyBreakdown"/>, after the theme and the app configuration.</summary>
    public string? ColumnHeader { get; init; }
    /// <summary>Classes merged into the <c>lines</c> slot of <see cref="KlyBreakdown"/>, after the theme and the app configuration.</summary>
    public string? Lines { get; init; }
    /// <summary>Classes merged into the <c>row</c> slot of <see cref="KlyBreakdown"/>, after the theme and the app configuration.</summary>
    public string? Row { get; init; }
    /// <summary>Classes merged into the <c>subtotal</c> slot of <see cref="KlyBreakdown"/>, after the theme and the app configuration.</summary>
    public string? Subtotal { get; init; }
    /// <summary>Classes merged into the <c>muted</c> slot of <see cref="KlyBreakdown"/>, after the theme and the app configuration.</summary>
    public string? Muted { get; init; }
    /// <summary>Classes merged into the <c>total</c> slot of <see cref="KlyBreakdown"/>, after the theme and the app configuration.</summary>
    public string? Total { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyBreakdown"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>labelRow</c> slot of <see cref="KlyBreakdown"/>, after the theme and the app configuration.</summary>
    public string? LabelRow { get; init; }
    /// <summary>Classes merged into the <c>detail</c> slot of <see cref="KlyBreakdown"/>, after the theme and the app configuration.</summary>
    public string? Detail { get; init; }
    /// <summary>Classes merged into the <c>amount</c> slot of <see cref="KlyBreakdown"/>, after the theme and the app configuration.</summary>
    public string? Amount { get; init; }
    /// <summary>Classes merged into the <c>compareAmount</c> slot of <see cref="KlyBreakdown"/>, after the theme and the app configuration.</summary>
    public string? CompareAmount { get; init; }
    /// <summary>Classes merged into the <c>footnote</c> slot of <see cref="KlyBreakdown"/>, after the theme and the app configuration.</summary>
    public string? Footnote { get; init; }
    /// <summary>Classes merged into the <c>skeleton</c> slot of <see cref="KlyBreakdown"/>, after the theme and the app configuration.</summary>
    public string? Skeleton { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "title" => Title,
        "toggle" => Toggle,
        "toggleIcon" => ToggleIcon,
        "columns" => Columns,
        "columnHeader" => ColumnHeader,
        "lines" => Lines,
        "row" => Row,
        "subtotal" => Subtotal,
        "muted" => Muted,
        "total" => Total,
        "label" => Label,
        "labelRow" => LabelRow,
        "detail" => Detail,
        "amount" => Amount,
        "compareAmount" => CompareAmount,
        "footnote" => Footnote,
        "skeleton" => Skeleton,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyDiff"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record DiffUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyDiff"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyDiff"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>list</c> slot of <see cref="KlyDiff"/>, after the theme and the app configuration.</summary>
    public string? List { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyDiff"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyDiff"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>values</c> slot of <see cref="KlyDiff"/>, after the theme and the app configuration.</summary>
    public string? Values { get; init; }
    /// <summary>Classes merged into the <c>before</c> slot of <see cref="KlyDiff"/>, after the theme and the app configuration.</summary>
    public string? Before { get; init; }
    /// <summary>Classes merged into the <c>arrow</c> slot of <see cref="KlyDiff"/>, after the theme and the app configuration.</summary>
    public string? Arrow { get; init; }
    /// <summary>Classes merged into the <c>after</c> slot of <see cref="KlyDiff"/>, after the theme and the app configuration.</summary>
    public string? After { get; init; }
    /// <summary>Classes merged into the <c>unchanged</c> slot of <see cref="KlyDiff"/>, after the theme and the app configuration.</summary>
    public string? Unchanged { get; init; }
    /// <summary>Classes merged into the <c>skeleton</c> slot of <see cref="KlyDiff"/>, after the theme and the app configuration.</summary>
    public string? Skeleton { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "title" => Title,
        "list" => List,
        "item" => Item,
        "label" => Label,
        "values" => Values,
        "before" => Before,
        "arrow" => Arrow,
        "after" => After,
        "unchanged" => Unchanged,
        "skeleton" => Skeleton,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyRecordList"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record RecordListUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>header</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? Header { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>count</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? Count { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>lead</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? Lead { get; init; }
    /// <summary>Classes merged into the <c>image</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? Image { get; init; }
    /// <summary>Classes merged into the <c>avatar</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? Avatar { get; init; }
    /// <summary>Classes merged into the <c>avatarIcon</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? AvatarIcon { get; init; }
    /// <summary>Classes merged into the <c>body</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? Body { get; init; }
    /// <summary>Classes merged into the <c>reference</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? Reference { get; init; }
    /// <summary>Classes merged into the <c>name</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? Name { get; init; }
    /// <summary>Classes merged into the <c>link</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? Link { get; init; }
    /// <summary>Classes merged into the <c>meta</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? Meta { get; init; }
    /// <summary>Classes merged into the <c>metaIcon</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? MetaIcon { get; init; }
    /// <summary>Classes merged into the <c>metaText</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? MetaText { get; init; }
    /// <summary>Classes merged into the <c>trail</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? Trail { get; init; }
    /// <summary>Classes merged into the <c>trailing</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? Trailing { get; init; }
    /// <summary>Classes merged into the <c>trailingBlock</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? TrailingBlock { get; init; }
    /// <summary>Classes merged into the <c>trailingCaption</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? TrailingCaption { get; init; }
    /// <summary>Classes merged into the <c>more</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? More { get; init; }
    /// <summary>Classes merged into the <c>moreIcon</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? MoreIcon { get; init; }
    /// <summary>Classes merged into the <c>skeleton</c> slot of <see cref="KlyRecordList"/>, after the theme and the app configuration.</summary>
    public string? Skeleton { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "header" => Header,
        "title" => Title,
        "count" => Count,
        "item" => Item,
        "lead" => Lead,
        "image" => Image,
        "avatar" => Avatar,
        "avatarIcon" => AvatarIcon,
        "body" => Body,
        "reference" => Reference,
        "name" => Name,
        "link" => Link,
        "meta" => Meta,
        "metaIcon" => MetaIcon,
        "metaText" => MetaText,
        "trail" => Trail,
        "trailing" => Trailing,
        "trailingBlock" => TrailingBlock,
        "trailingCaption" => TrailingCaption,
        "more" => More,
        "moreIcon" => MoreIcon,
        "skeleton" => Skeleton,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyList"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record ListUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyList"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyList"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>list</c> slot of <see cref="KlyList"/>, after the theme and the app configuration.</summary>
    public string? List { get; init; }
    /// <summary>Classes merged into the <c>item</c> slot of <see cref="KlyList"/>, after the theme and the app configuration.</summary>
    public string? Item { get; init; }
    /// <summary>Classes merged into the <c>lead</c> slot of <see cref="KlyList"/>, after the theme and the app configuration.</summary>
    public string? Lead { get; init; }
    /// <summary>Classes merged into the <c>marker</c> slot of <see cref="KlyList"/>, after the theme and the app configuration.</summary>
    public string? Marker { get; init; }
    /// <summary>Classes merged into the <c>markerIcon</c> slot of <see cref="KlyList"/>, after the theme and the app configuration.</summary>
    public string? MarkerIcon { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyList"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>bullet</c> slot of <see cref="KlyList"/>, after the theme and the app configuration.</summary>
    public string? Bullet { get; init; }
    /// <summary>Classes merged into the <c>body</c> slot of <see cref="KlyList"/>, after the theme and the app configuration.</summary>
    public string? Body { get; init; }
    /// <summary>Classes merged into the <c>text</c> slot of <see cref="KlyList"/>, after the theme and the app configuration.</summary>
    public string? Text { get; init; }
    /// <summary>Classes merged into the <c>link</c> slot of <see cref="KlyList"/>, after the theme and the app configuration.</summary>
    public string? Link { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyList"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>meta</c> slot of <see cref="KlyList"/>, after the theme and the app configuration.</summary>
    public string? Meta { get; init; }
    /// <summary>Classes merged into the <c>more</c> slot of <see cref="KlyList"/>, after the theme and the app configuration.</summary>
    public string? More { get; init; }
    /// <summary>Classes merged into the <c>moreIcon</c> slot of <see cref="KlyList"/>, after the theme and the app configuration.</summary>
    public string? MoreIcon { get; init; }
    /// <summary>Classes merged into the <c>skeleton</c> slot of <see cref="KlyList"/>, after the theme and the app configuration.</summary>
    public string? Skeleton { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "title" => Title,
        "list" => List,
        "item" => Item,
        "lead" => Lead,
        "marker" => Marker,
        "markerIcon" => MarkerIcon,
        "icon" => Icon,
        "bullet" => Bullet,
        "body" => Body,
        "text" => Text,
        "link" => Link,
        "description" => Description,
        "meta" => Meta,
        "more" => More,
        "moreIcon" => MoreIcon,
        "skeleton" => Skeleton,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyDateTile"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record DateTileUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyDateTile"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>tile</c> slot of <see cref="KlyDateTile"/>, after the theme and the app configuration.</summary>
    public string? Tile { get; init; }
    /// <summary>Classes merged into the <c>month</c> slot of <see cref="KlyDateTile"/>, after the theme and the app configuration.</summary>
    public string? Month { get; init; }
    /// <summary>Classes merged into the <c>day</c> slot of <see cref="KlyDateTile"/>, after the theme and the app configuration.</summary>
    public string? Day { get; init; }
    /// <summary>Classes merged into the <c>text</c> slot of <see cref="KlyDateTile"/>, after the theme and the app configuration.</summary>
    public string? Text { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyDateTile"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>caption</c> slot of <see cref="KlyDateTile"/>, after the theme and the app configuration.</summary>
    public string? Caption { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyDateTile"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>skeleton</c> slot of <see cref="KlyDateTile"/>, after the theme and the app configuration.</summary>
    public string? Skeleton { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "tile" => Tile,
        "month" => Month,
        "day" => Day,
        "text" => Text,
        "title" => Title,
        "caption" => Caption,
        "description" => Description,
        "skeleton" => Skeleton,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyMeter"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record MeterUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>header</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? Header { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>value</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? Value { get; init; }
    /// <summary>Classes merged into the <c>track</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? Track { get; init; }
    /// <summary>Classes merged into the <c>bar</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? Bar { get; init; }
    /// <summary>Classes merged into the <c>part</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? Part { get; init; }
    /// <summary>Classes merged into the <c>ring</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? Ring { get; init; }
    /// <summary>Classes merged into the <c>ringSvg</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? RingSvg { get; init; }
    /// <summary>Classes merged into the <c>ringDrawing</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? RingDrawing { get; init; }
    /// <summary>Classes merged into the <c>ringTrack</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? RingTrack { get; init; }
    /// <summary>Classes merged into the <c>ringArc</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? RingArc { get; init; }
    /// <summary>Classes merged into the <c>ringValue</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? RingValue { get; init; }
    /// <summary>Classes merged into the <c>ringText</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? RingText { get; init; }
    /// <summary>Classes merged into the <c>caption</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? Caption { get; init; }
    /// <summary>Classes merged into the <c>scale</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? Scale { get; init; }
    /// <summary>Classes merged into the <c>level</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? Level { get; init; }
    /// <summary>Classes merged into the <c>legend</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? Legend { get; init; }
    /// <summary>Classes merged into the <c>legendItem</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? LegendItem { get; init; }
    /// <summary>Classes merged into the <c>legendIcon</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? LegendIcon { get; init; }
    /// <summary>Classes merged into the <c>legendDot</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? LegendDot { get; init; }
    /// <summary>Classes merged into the <c>legendLabel</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? LegendLabel { get; init; }
    /// <summary>Classes merged into the <c>legendValue</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? LegendValue { get; init; }
    /// <summary>Classes merged into the <c>skeleton</c> slot of <see cref="KlyMeter"/>, after the theme and the app configuration.</summary>
    public string? Skeleton { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "header" => Header,
        "label" => Label,
        "value" => Value,
        "track" => Track,
        "bar" => Bar,
        "part" => Part,
        "ring" => Ring,
        "ringSvg" => RingSvg,
        "ringDrawing" => RingDrawing,
        "ringTrack" => RingTrack,
        "ringArc" => RingArc,
        "ringValue" => RingValue,
        "ringText" => RingText,
        "caption" => Caption,
        "scale" => Scale,
        "level" => Level,
        "legend" => Legend,
        "legendItem" => LegendItem,
        "legendIcon" => LegendIcon,
        "legendDot" => LegendDot,
        "legendLabel" => LegendLabel,
        "legendValue" => LegendValue,
        "skeleton" => Skeleton,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyImageGallery"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record ImageGalleryUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyImageGallery"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>grid</c> slot of <see cref="KlyImageGallery"/>, after the theme and the app configuration.</summary>
    public string? Grid { get; init; }
    /// <summary>Classes merged into the <c>tile</c> slot of <see cref="KlyImageGallery"/>, after the theme and the app configuration.</summary>
    public string? Tile { get; init; }
    /// <summary>Classes merged into the <c>image</c> slot of <see cref="KlyImageGallery"/>, after the theme and the app configuration.</summary>
    public string? Image { get; init; }
    /// <summary>Classes merged into the <c>overflow</c> slot of <see cref="KlyImageGallery"/>, after the theme and the app configuration.</summary>
    public string? Overflow { get; init; }
    /// <summary>Classes merged into the <c>caption</c> slot of <see cref="KlyImageGallery"/>, after the theme and the app configuration.</summary>
    public string? Caption { get; init; }
    /// <summary>Classes merged into the <c>skeleton</c> slot of <see cref="KlyImageGallery"/>, after the theme and the app configuration.</summary>
    public string? Skeleton { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "grid" => Grid,
        "tile" => Tile,
        "image" => Image,
        "overflow" => Overflow,
        "caption" => Caption,
        "skeleton" => Skeleton,
        _ => null,
    };
}
