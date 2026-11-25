using System.Globalization;
using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>Who wrote a <see cref="KlyChatMessage"/>.</summary>
public enum ChatRole
{
    /// <summary>Written by the person using the chat.</summary>
    User,
    /// <summary>Written by the model or the agent.</summary>
    Assistant,
    /// <summary>A notice from the app, such as an instruction or a status.</summary>
    System,
}

/// <summary>
/// Where a conversation is: nothing pending, the request sent and no answer yet, the answer arriving, or failed.
/// </summary>
public enum ChatStatus
{
    /// <summary>Nothing is pending.</summary>
    Idle,
    /// <summary>The request is sent and no answer has started.</summary>
    Submitted,
    /// <summary>The answer is arriving.</summary>
    Streaming,
    /// <summary>The request failed.</summary>
    Error,
}

/// <summary>Which side of the conversation a message sits on.</summary>
public enum ChatSide
{
    /// <summary>At the start of the line.</summary>
    Left,
    /// <summary>At the end of the line.</summary>
    Right,
}

/// <summary>Look of a message bubble. <see cref="Naked"/> has no surface at all.</summary>
public enum ChatMessageVariant
{
    /// <summary>A filled bubble in the semantic color.</summary>
    Solid,
    /// <summary>A border and no fill.</summary>
    Outline,
    /// <summary>A light tint of the color.</summary>
    Soft,
    /// <summary>A light tint with a border.</summary>
    Subtle,
    /// <summary>No surface.</summary>
    Naked,
}

/// <summary>Which side of a collapsible block carries the chevron.</summary>
public enum ChatChevron
{
    /// <summary>The chevron is at the end of the header.</summary>
    Trailing,
    /// <summary>The chevron is at the start of the header.</summary>
    Leading,
}

/// <summary>Look of a <see cref="KlyChatTool"/>.</summary>
public enum ChatToolVariant
{
    /// <summary>One line of text.</summary>
    Inline,
    /// <summary>A bordered block.</summary>
    Card,
}

/// <summary>Look of a <see cref="KlyChatAttachment"/>.</summary>
public enum ChatAttachmentVariant
{
    /// <summary>A chip with an icon or a small preview, the name and the size.</summary>
    Card,
    /// <summary>The picture alone.</summary>
    Thumbnail,
}

/// <summary>State of a <see cref="KlyChatAttachment"/>.</summary>
public enum ChatAttachmentStatus
{
    /// <summary>The file is ready.</summary>
    Idle,
    /// <summary>The file is being uploaded.</summary>
    Uploading,
    /// <summary>The upload failed.</summary>
    Error,
}

/// <summary>What a <see cref="KlyChatPrompt"/> tells the <see cref="KlyChatPromptSubmit"/> inside it.</summary>
internal sealed record ChatPromptContext(ChatStatus Status, bool Disabled);

internal static class ChatValue
{
    public static string Of(ChatRole role) => role switch
    {
        ChatRole.User => "user",
        ChatRole.Assistant => "assistant",
        ChatRole.System => "system",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
    };

    public static string Of(ChatStatus status) => status switch
    {
        ChatStatus.Idle => "idle",
        ChatStatus.Submitted => "submitted",
        ChatStatus.Streaming => "streaming",
        ChatStatus.Error => "error",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static string Of(ChatSide side) => side == ChatSide.Right ? "right" : "left";

    public static string Of(ChatMessageVariant variant) => variant switch
    {
        ChatMessageVariant.Solid => "solid",
        ChatMessageVariant.Outline => "outline",
        ChatMessageVariant.Soft => "soft",
        ChatMessageVariant.Subtle => "subtle",
        ChatMessageVariant.Naked => "naked",
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };

    public static string Of(ChatAttachmentStatus status) => status switch
    {
        ChatAttachmentStatus.Idle => "idle",
        ChatAttachmentStatus.Uploading => "uploading",
        ChatAttachmentStatus.Error => "error",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

}

/// <summary>Per-instance class overrides of <see cref="KlyChatMessage"/> slots.</summary>
public sealed record ChatMessageUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyChatMessage"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>container</c> slot of <see cref="KlyChatMessage"/>, after the theme and the app configuration.</summary>
    public string? Container { get; init; }
    /// <summary>Classes merged into the <c>leading</c> slot of <see cref="KlyChatMessage"/>, after the theme and the app configuration.</summary>
    public string? Leading { get; init; }
    /// <summary>Classes merged into the <c>wrapper</c> slot of <see cref="KlyChatMessage"/>, after the theme and the app configuration.</summary>
    public string? Wrapper { get; init; }
    /// <summary>Classes merged into the <c>header</c> slot of <see cref="KlyChatMessage"/>, after the theme and the app configuration.</summary>
    public string? Header { get; init; }
    /// <summary>Classes merged into the <c>files</c> slot of <see cref="KlyChatMessage"/>, after the theme and the app configuration.</summary>
    public string? Files { get; init; }
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlyChatMessage"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }
    /// <summary>Classes merged into the <c>text</c> slot of <see cref="KlyChatMessage"/>, after the theme and the app configuration.</summary>
    public string? Text { get; init; }
    /// <summary>Classes merged into the <c>actions</c> slot of <see cref="KlyChatMessage"/>, after the theme and the app configuration.</summary>
    public string? Actions { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "container" => Container,
        "leading" => Leading,
        "wrapper" => Wrapper,
        "header" => Header,
        "files" => Files,
        "content" => Content,
        "text" => Text,
        "actions" => Actions,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of <see cref="KlyChatMessages"/> slots.</summary>
public sealed record ChatMessagesUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyChatMessages"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>viewport</c> slot of <see cref="KlyChatMessages"/>, after the theme and the app configuration.</summary>
    public string? Viewport { get; init; }
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlyChatMessages"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }
    /// <summary>Classes merged into the <c>indicator</c> slot of <see cref="KlyChatMessages"/>, after the theme and the app configuration.</summary>
    public string? Indicator { get; init; }
    /// <summary>Classes merged into the <c>dot</c> slot of <see cref="KlyChatMessages"/>, after the theme and the app configuration.</summary>
    public string? Dot { get; init; }
    /// <summary>Classes merged into the <c>error</c> slot of <see cref="KlyChatMessages"/>, after the theme and the app configuration.</summary>
    public string? Error { get; init; }
    /// <summary>Classes merged into the <c>jump</c> slot of <see cref="KlyChatMessages"/>, after the theme and the app configuration.</summary>
    public string? Jump { get; init; }
    /// <summary>Classes merged into the <c>jumpIcon</c> slot of <see cref="KlyChatMessages"/>, after the theme and the app configuration.</summary>
    public string? JumpIcon { get; init; }
    /// <summary>Classes merged into the <c>status</c> slot of <see cref="KlyChatMessages"/>, after the theme and the app configuration.</summary>
    public string? Status { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "viewport" => Viewport,
        "content" => Content,
        "indicator" => Indicator,
        "dot" => Dot,
        "error" => Error,
        "jump" => Jump,
        "jumpIcon" => JumpIcon,
        "status" => Status,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of <see cref="KlyChatPrompt"/> slots.</summary>
public sealed record ChatPromptUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyChatPrompt"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>form</c> slot of <see cref="KlyChatPrompt"/>, after the theme and the app configuration.</summary>
    public string? Form { get; init; }
    /// <summary>Classes merged into the <c>attachments</c> slot of <see cref="KlyChatPrompt"/>, after the theme and the app configuration.</summary>
    public string? Attachments { get; init; }
    /// <summary>Classes merged into the <c>row</c> slot of <see cref="KlyChatPrompt"/>, after the theme and the app configuration.</summary>
    public string? Row { get; init; }
    /// <summary>Classes merged into the <c>textarea</c> slot of <see cref="KlyChatPrompt"/>, after the theme and the app configuration.</summary>
    public string? Textarea { get; init; }
    /// <summary>Classes merged into the <c>trailing</c> slot of <see cref="KlyChatPrompt"/>, after the theme and the app configuration.</summary>
    public string? Trailing { get; init; }
    /// <summary>Classes merged into the <c>footer</c> slot of <see cref="KlyChatPrompt"/>, after the theme and the app configuration.</summary>
    public string? Footer { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "form" => Form,
        "attachments" => Attachments,
        "row" => Row,
        "textarea" => Textarea,
        "trailing" => Trailing,
        "footer" => Footer,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of <see cref="KlyChatPromptSubmit"/> slots.</summary>
public sealed record ChatPromptSubmitUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyChatPromptSubmit"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyChatPromptSubmit"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>stopIcon</c> slot of <see cref="KlyChatPromptSubmit"/>, after the theme and the app configuration.</summary>
    public string? StopIcon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "base" => Base,
        "icon" => Icon,
        "stopIcon" => StopIcon,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of <see cref="KlyChatPalette"/> slots.</summary>
public sealed record ChatPaletteUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyChatPalette"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlyChatPalette"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }
    /// <summary>Classes merged into the <c>prompt</c> slot of <see cref="KlyChatPalette"/>, after the theme and the app configuration.</summary>
    public string? Prompt { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "content" => Content,
        "prompt" => Prompt,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of <see cref="KlyChatAttachment"/> slots.</summary>
public sealed record ChatAttachmentUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyChatAttachment"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>leading</c> slot of <see cref="KlyChatAttachment"/>, after the theme and the app configuration.</summary>
    public string? Leading { get; init; }
    /// <summary>Classes merged into the <c>leadingIcon</c> slot of <see cref="KlyChatAttachment"/>, after the theme and the app configuration.</summary>
    public string? LeadingIcon { get; init; }
    /// <summary>Classes merged into the <c>image</c> slot of <see cref="KlyChatAttachment"/>, after the theme and the app configuration.</summary>
    public string? Image { get; init; }
    /// <summary>Classes merged into the <c>wrapper</c> slot of <see cref="KlyChatAttachment"/>, after the theme and the app configuration.</summary>
    public string? Wrapper { get; init; }
    /// <summary>Classes merged into the <c>name</c> slot of <see cref="KlyChatAttachment"/>, after the theme and the app configuration.</summary>
    public string? Name { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyChatAttachment"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>remove</c> slot of <see cref="KlyChatAttachment"/>, after the theme and the app configuration.</summary>
    public string? Remove { get; init; }
    /// <summary>Classes merged into the <c>removeIcon</c> slot of <see cref="KlyChatAttachment"/>, after the theme and the app configuration.</summary>
    public string? RemoveIcon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "leading" => Leading,
        "leadingIcon" => LeadingIcon,
        "image" => Image,
        "wrapper" => Wrapper,
        "name" => Name,
        "description" => Description,
        "remove" => Remove,
        "removeIcon" => RemoveIcon,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of <see cref="KlyChatReasoning"/> slots.</summary>
public sealed record ChatReasoningUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyChatReasoning"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>trigger</c> slot of <see cref="KlyChatReasoning"/>, after the theme and the app configuration.</summary>
    public string? Trigger { get; init; }
    /// <summary>Classes merged into the <c>leading</c> slot of <see cref="KlyChatReasoning"/>, after the theme and the app configuration.</summary>
    public string? Leading { get; init; }
    /// <summary>Classes merged into the <c>leadingIcon</c> slot of <see cref="KlyChatReasoning"/>, after the theme and the app configuration.</summary>
    public string? LeadingIcon { get; init; }
    /// <summary>Classes merged into the <c>chevronIcon</c> slot of <see cref="KlyChatReasoning"/>, after the theme and the app configuration.</summary>
    public string? ChevronIcon { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyChatReasoning"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlyChatReasoning"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }
    /// <summary>Classes merged into the <c>body</c> slot of <see cref="KlyChatReasoning"/>, after the theme and the app configuration.</summary>
    public string? Body { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "trigger" => Trigger,
        "leading" => Leading,
        "leadingIcon" => LeadingIcon,
        "chevronIcon" => ChevronIcon,
        "label" => Label,
        "content" => Content,
        "body" => Body,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of <see cref="KlyChatShimmer"/> slots.</summary>
public sealed record ChatShimmerUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>base</c> slot of <see cref="KlyChatShimmer"/>, after the theme and the app configuration.</summary>
    public string? Base { get; init; }

    string? ISlotOverrides.For(string slot) => slot == "base" ? Base : null;
}

/// <summary>Per-instance class overrides of <see cref="KlyChatTool"/> slots.</summary>
public sealed record ChatToolUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyChatTool"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>trigger</c> slot of <see cref="KlyChatTool"/>, after the theme and the app configuration.</summary>
    public string? Trigger { get; init; }
    /// <summary>Classes merged into the <c>leading</c> slot of <see cref="KlyChatTool"/>, after the theme and the app configuration.</summary>
    public string? Leading { get; init; }
    /// <summary>Classes merged into the <c>leadingIcon</c> slot of <see cref="KlyChatTool"/>, after the theme and the app configuration.</summary>
    public string? LeadingIcon { get; init; }
    /// <summary>Classes merged into the <c>chevronIcon</c> slot of <see cref="KlyChatTool"/>, after the theme and the app configuration.</summary>
    public string? ChevronIcon { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyChatTool"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>suffix</c> slot of <see cref="KlyChatTool"/>, after the theme and the app configuration.</summary>
    public string? Suffix { get; init; }
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlyChatTool"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }
    /// <summary>Classes merged into the <c>body</c> slot of <see cref="KlyChatTool"/>, after the theme and the app configuration.</summary>
    public string? Body { get; init; }
    /// <summary>Classes merged into the <c>actions</c> slot of <see cref="KlyChatTool"/>, after the theme and the app configuration.</summary>
    public string? Actions { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "trigger" => Trigger,
        "leading" => Leading,
        "leadingIcon" => LeadingIcon,
        "chevronIcon" => ChevronIcon,
        "label" => Label,
        "suffix" => Suffix,
        "content" => Content,
        "body" => Body,
        "actions" => Actions,
        _ => null,
    };
}
