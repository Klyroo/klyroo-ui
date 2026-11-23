using Microsoft.AspNetCore.Components;

namespace Blux.UI.Tools;

/// <summary>The 19 read-only display block kinds supported by <see cref="BxDisplay"/>.</summary>
public enum DisplayKind
{
    /// <summary>Section heading or eyebrow with optional icon, count pill, description and action.</summary>
    Heading,

    /// <summary>Formatted text block (body, lead, muted, caption, quote, legal or prose) with optional line clamping and Read more toggle.</summary>
    Text,

    /// <summary>Contextual callout or banner with tone, icon, bullet items, collapsible details and actions.</summary>
    Alert,

    /// <summary>Key/value fact list or grouped ledger with optional copy buttons, links, chips and empty value dashes.</summary>
    Facts,

    /// <summary>Metric highlight (hero, price, row or trend) with optional delta badge and SVG sparkline.</summary>
    Stat,

    /// <summary>Financial or numeric line-item breakdown (open, collapsible or two-column comparison) with subtotals and total row.</summary>
    Breakdown,

    /// <summary>Progress or capacity meter (bar, stacked segments, split, SVG ring or discrete level scale) with warning/error thresholds.</summary>
    Meter,

    /// <summary>Compact read-only visualization (bars, SVG line, SVG donut or conversion funnel).</summary>
    Chart,

    /// <summary>Lightweight read-only key/value table with sticky header/footer and progressive reveal.</summary>
    Table,

    /// <summary>Before/after field comparison (inline strike-through + arrow, or three-column table).</summary>
    Diff,

    /// <summary>Entity summary card, row, compact row or revealable stack with avatar/icon, reference, badges and trailing value.</summary>
    Record,

    /// <summary>User profile row, avatar group with overflow count, or contact card with copyable contact entries.</summary>
    Person,

    /// <summary>Structured list (bullets, numbered, icons, checks, vertical steps, multi-column, chips or downloadable files).</summary>
    List,

    /// <summary>Chronological event stream (vertical, log, horizontal pipeline or schedule with a Today marker).</summary>
    Timeline,

    /// <summary>Calendar leaf tile, date range with day count chip, or deadline countdown with urgency tone.</summary>
    Date,

    /// <summary>Image frame, multi-tile image gallery, poster video preview, document card or external link card.</summary>
    Media,

    /// <summary>Code block (with JSON token highlighting), single-line copyable value, or masked secret with reveal toggle.</summary>
    Code,

    /// <summary>Dashed or plain empty-state placeholder with icon, title, description and actions.</summary>
    Empty,

    /// <summary>Collapsible accordion sections (separated or flush, single or multiple open).</summary>
    Accordion,
}

/// <summary>Container surface treatment of a <see cref="BxDisplay"/> block.</summary>
public enum DisplaySurface
{
    /// <summary>No border, background or padding.</summary>
    Plain,

    /// <summary>Muted elevated background with padding.</summary>
    Muted,

    /// <summary>Bordered card with padding.</summary>
    Bordered,

    /// <summary>Elevated card with border and shadow.</summary>
    Elevated,
}

/// <summary>A chip/badge displayed inside a <see cref="BxDisplay"/> block.</summary>
public sealed record DisplayChip(
    string Label,
    BxColor? Color = null,
    string? Icon = null);

/// <summary>An action button or link displayed inside a <see cref="BxDisplay"/> block.</summary>
public sealed class DisplayAction
{
    public required string Label { get; init; }
    public string? Icon { get; init; }
    public string? Href { get; init; }
    public string? Target { get; init; }
    public EventCallback OnClick { get; init; }
    public BxColor Color { get; init; } = BxColor.Neutral;
    public ButtonVariant Variant { get; init; } = ButtonVariant.Outline;
}

/// <summary>One key/value entry in a <see cref="DisplayKind.Facts"/> block.</summary>
public sealed class DisplayFact
{
    public required string Label { get; init; }
    public string? Value { get; init; }
    public string? Hint { get; init; }
    public DisplayChip? Chip { get; init; }
    public BxColor? Tone { get; init; }
    public bool Copy { get; init; }
    public string? CopyText { get; init; }
    public bool Mono { get; init; }
    public bool Emphasis { get; init; }
    public string? Icon { get; init; }
    public string? Href { get; init; }
    public bool Loading { get; init; }
}

/// <summary>A named group of <see cref="DisplayFact"/> items in a <see cref="DisplayKind.Facts"/> block.</summary>
public sealed record DisplayFactGroup(
    string? Title,
    IReadOnlyList<DisplayFact> Items,
    string? Aside = null);

/// <summary>One sub-metric in the <c>row</c> variant of a <see cref="DisplayKind.Stat"/> block.</summary>
public sealed record DisplayStatItem(
    string Label,
    string Value,
    string? Caption = null,
    BxColor? Tone = null);

/// <summary>One line item in a <see cref="DisplayKind.Breakdown"/> block.</summary>
public sealed record DisplayBreakdownLine(
    string Label,
    string? Value = null,
    string? CompareValue = null,
    string? Detail = null,
    bool IsSubtotal = false,
    bool IsMuted = false,
    BxColor? Tone = null,
    DisplayChip? Chip = null);

/// <summary>One segment or part in a <see cref="DisplayKind.Meter"/> block.</summary>
public sealed record DisplayMeterPart(
    string Label,
    double Value,
    string? ValueLabel = null,
    BxColor? Tone = null,
    string? Icon = null,
    double? Max = null);

/// <summary>One data point in a <see cref="DisplayKind.Chart"/> block.</summary>
public sealed record DisplayChartPoint(
    string Label,
    double Value,
    BxColor? Tone = null);

/// <summary>One column definition in a <see cref="DisplayKind.Table"/> block.</summary>
public sealed record DisplayTableColumn(
    string Key,
    string Label,
    bool Numeric = false,
    DataListAlign? Align = null,
    bool Mono = false);

/// <summary>One field difference in a <see cref="DisplayKind.Diff"/> block.</summary>
public sealed record DisplayDiffItem(
    string Label,
    string? Before,
    string? After,
    bool Changed = true,
    DisplayChip? Note = null);

/// <summary>One entity record in a <see cref="DisplayKind.Record"/> block.</summary>
public sealed class DisplayRecordItem
{
    public string? Reference { get; init; }
    public required string Title { get; init; }
    public string? Meta { get; init; }
    public string? MetaIcon { get; init; }
    public string? Description { get; init; }
    public string? Icon { get; init; }
    public string? AvatarText { get; init; }
    public string? ImageSrc { get; init; }
    public BxColor? Tone { get; init; }
    public IReadOnlyList<DisplayChip>? Badges { get; init; }
    public DisplayChip? Chip { get; init; }
    public string? Trailing { get; init; }
    public string? TrailingCaption { get; init; }
    public string? Href { get; init; }
    public EventCallback OnClick { get; init; }
}

/// <summary>One person in a <see cref="DisplayKind.Person"/> block.</summary>
public sealed record DisplayPersonItem(
    string Name,
    string? Description = null,
    string? AvatarSrc = null,
    BxColor? Tone = null,
    DisplayChip? Chip = null);

/// <summary>One contact channel in the <c>contact</c> variant of a <see cref="DisplayKind.Person"/> block.</summary>
public sealed record DisplayContactItem(
    string Value,
    string? Icon = null,
    string? Href = null,
    bool Copy = true);

/// <summary>One entry in a <see cref="DisplayKind.List"/> block.</summary>
public sealed class DisplayListItem
{
    public required string Text { get; init; }
    public string? Description { get; init; }
    public string? Icon { get; init; }
    public BxColor? Tone { get; init; }
    public bool Done { get; init; }
    public bool Current { get; init; }
    public string? Meta { get; init; }
    public string? Href { get; init; }
    public string? FileName { get; init; }
    public long? FileSize { get; init; }
    public string? MimeType { get; init; }
}

/// <summary>State of a step in a <see cref="DisplayKind.Timeline"/> block.</summary>
public enum DisplayTimelineState
{
    Done,
    Current,
    Upcoming,
}

/// <summary>One event in a <see cref="DisplayKind.Timeline"/> block.</summary>
public sealed record DisplayTimelineItem(
    string Title,
    string? Description = null,
    string? Time = null,
    DateTimeOffset? Date = null,
    string? Icon = null,
    string? ActorName = null,
    DisplayTimelineState? State = null,
    BxColor? Tone = null);

/// <summary>One collapsible section in a <see cref="DisplayKind.Accordion"/> block.</summary>
public sealed record DisplayAccordionItem(
    string Label,
    string? Content = null,
    string? Icon = null,
    bool DefaultOpen = false,
    RenderFragment? Template = null);
