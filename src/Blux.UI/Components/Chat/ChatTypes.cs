using System.Globalization;
using Blux.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Blux.UI;

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

    /// <summary>Bytes as a short text: 512 B, 1.5 KB, 2 MB.</summary>
    public static string Bytes(long bytes) => bytes switch
    {
        < 1024 => string.Create(CultureInfo.InvariantCulture, $"{bytes} B"),
        < 1024 * 1024 => string.Create(CultureInfo.InvariantCulture, $"{bytes / 1024.0:0.#} KB"),
        < 1024L * 1024 * 1024 => string.Create(CultureInfo.InvariantCulture, $"{bytes / (1024.0 * 1024):0.#} MB"),
        _ => string.Create(CultureInfo.InvariantCulture, $"{bytes / (1024.0 * 1024 * 1024):0.#} GB"),
    };
}

/// <summary>Icons that only the chat components use, so the shared icon set stays as it is.</summary>
internal static class BxChatIcons
{
    public const string Stop = "M5 5h14v14H5Z";
    public const string Retry = "M3 12a9 9 0 0 1 9-9 9.75 9.75 0 0 1 6.74 2.74L21 8M21 3v5h-5M21 12a9 9 0 0 1-9 9 9.75 9.75 0 0 1-6.74-2.74L3 16M8 16H3v5";
    public const string Send = "m5 12 7-7 7 7M12 19V5";
    public const string Wrench = "M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94z";
    public const string Alert = "M12 8v4m0 4h.01M22 12a10 10 0 1 1-20 0 10 10 0 0 1 20 0Z";
    public const string Spinner = "M21 12a9 9 0 1 1-6.219-8.56";
    public const string File = "M6 22a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.704.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2zM14 2v5a1 1 0 0 0 1 1h5";
    public const string Close = "M18 6 6 18M6 6l12 12";
    public const string ChevronDown = "m6 9 6 6 6-6";
    public const string ArrowDown = "M12 5v14m7-7-7 7-7-7";

    public static RenderFragment Draw(string path, string? @class) => builder =>
    {
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "viewBox", "0 0 24 24");
        builder.AddAttribute(2, "fill", "none");
        builder.AddAttribute(3, "stroke", "currentColor");
        builder.AddAttribute(4, "stroke-width", "2");
        builder.AddAttribute(5, "stroke-linecap", "round");
        builder.AddAttribute(6, "stroke-linejoin", "round");
        builder.AddAttribute(7, "aria-hidden", "true");
        builder.AddAttribute(8, "class", @class);
        builder.OpenElement(9, "path");
        builder.AddAttribute(10, "d", path);
        builder.CloseElement();
        builder.CloseElement();
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
