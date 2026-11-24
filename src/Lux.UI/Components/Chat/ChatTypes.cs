using System.Globalization;
using Lux.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Lux.UI;

/// <summary>Who wrote a <see cref="BxChatMessage"/>.</summary>
public enum ChatRole
{
    User,
    Assistant,
    System,
}

/// <summary>
/// Where a conversation is: nothing pending, the request sent and no answer yet, the answer arriving, or failed.
/// </summary>
public enum ChatStatus
{
    Idle,
    Submitted,
    Streaming,
    Error,
}

public enum ChatSide
{
    Left,
    Right,
}

/// <summary>Look of a message bubble. <see cref="Naked"/> has no surface at all.</summary>
public enum ChatMessageVariant
{
    Solid,
    Outline,
    Soft,
    Subtle,
    Naked,
}

/// <summary>Which side of a collapsible block carries the chevron.</summary>
public enum ChatChevron
{
    Trailing,
    Leading,
}

public enum ChatToolVariant
{
    Inline,
    Card,
}

public enum ChatAttachmentVariant
{
    Card,
    Thumbnail,
}

public enum ChatAttachmentStatus
{
    Idle,
    Uploading,
    Error,
}

/// <summary>What a <see cref="BxChatPrompt"/> tells the <see cref="BxChatPromptSubmit"/> inside it.</summary>
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

/// <summary>Per-instance class overrides of <see cref="BxChatMessage"/> slots.</summary>
public sealed record ChatMessageUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Container { get; init; }
    public string? Leading { get; init; }
    public string? Wrapper { get; init; }
    public string? Header { get; init; }
    public string? Files { get; init; }
    public string? Content { get; init; }
    public string? Text { get; init; }
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

/// <summary>Per-instance class overrides of <see cref="BxChatMessages"/> slots.</summary>
public sealed record ChatMessagesUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Viewport { get; init; }
    public string? Content { get; init; }
    public string? Indicator { get; init; }
    public string? Dot { get; init; }
    public string? Error { get; init; }
    public string? Jump { get; init; }
    public string? JumpIcon { get; init; }
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

/// <summary>Per-instance class overrides of <see cref="BxChatPrompt"/> slots.</summary>
public sealed record ChatPromptUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Form { get; init; }
    public string? Attachments { get; init; }
    public string? Row { get; init; }
    public string? Textarea { get; init; }
    public string? Trailing { get; init; }
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

/// <summary>Per-instance class overrides of <see cref="BxChatPromptSubmit"/> slots.</summary>
public sealed record ChatPromptSubmitUi : ISlotOverrides
{
    public string? Base { get; init; }
    public string? Icon { get; init; }
    public string? StopIcon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "base" => Base,
        "icon" => Icon,
        "stopIcon" => StopIcon,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of <see cref="BxChatPalette"/> slots.</summary>
public sealed record ChatPaletteUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Content { get; init; }
    public string? Prompt { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "content" => Content,
        "prompt" => Prompt,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of <see cref="BxChatAttachment"/> slots.</summary>
public sealed record ChatAttachmentUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Leading { get; init; }
    public string? LeadingIcon { get; init; }
    public string? Image { get; init; }
    public string? Wrapper { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Remove { get; init; }
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

/// <summary>Per-instance class overrides of <see cref="BxChatReasoning"/> slots.</summary>
public sealed record ChatReasoningUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Trigger { get; init; }
    public string? Leading { get; init; }
    public string? LeadingIcon { get; init; }
    public string? ChevronIcon { get; init; }
    public string? Label { get; init; }
    public string? Content { get; init; }
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

/// <summary>Per-instance class overrides of <see cref="BxChatShimmer"/> slots.</summary>
public sealed record ChatShimmerUi : ISlotOverrides
{
    public string? Base { get; init; }

    string? ISlotOverrides.For(string slot) => slot == "base" ? Base : null;
}

/// <summary>Per-instance class overrides of <see cref="BxChatTool"/> slots.</summary>
public sealed record ChatToolUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Trigger { get; init; }
    public string? Leading { get; init; }
    public string? LeadingIcon { get; init; }
    public string? ChevronIcon { get; init; }
    public string? Label { get; init; }
    public string? Suffix { get; init; }
    public string? Content { get; init; }
    public string? Body { get; init; }
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
