using Blux.UI;

namespace Blux.Playground.Client.Docs;

public static partial class Catalog
{
    private static readonly ComponentDoc[] Chat =
    [
        new("chat-message", "ChatMessage", "Chat", "One message of a conversation, with an avatar, a surface, a header, attachments and actions. It renders the markup or text you give it: markdown is the app's job (render it with Markdig and pass the HTML as content).",
            [new("Roles and variants", typeof(Examples.ChatMessageExample), "A user message sits on the right with a soft surface, the others on the left with none. A message that is still arriving is marked busy, so it is announced once it is complete."),
             new("Files and actions", typeof(Examples.ChatMessageActionsExample), "Header and Files above the text, Actions below it.")],
            [typeof(BxChatMessage)]),
        new("chat-messages", "ChatMessages", "Chat", "The log of a conversation. It follows new messages while you are at the bottom, stops when you scroll up and offers a button to jump back, shows a typing indicator while an answer is pending, and announces a finished answer once instead of token by token.",
            [new("Status and scrolling", typeof(Examples.ChatMessagesExample), "Add messages and change the status. Scroll up in the list, add a message, then use Jump to latest."),
             new("Streaming a reply", typeof(Examples.ChatPaletteExample), "A chat panel streaming an IAsyncEnumerable of tokens, with a stop button.")],
            [typeof(BxChatMessages)]),
        new("chat-prompt", "ChatPrompt", "Chat", "A message form with a textarea that grows with its text. Enter sends, Shift+Enter adds a line, an IME composition never sends. It works in static server rendering as a form post.",
            [new("Send a message", typeof(Examples.ChatPromptExample), "The same code runs in every mode: interactive modes report the text through the browser, static rendering posts the form under FormName."),
             new("With attachments", typeof(Examples.ChatPromptAttachmentsExample), "Attachments above the field; a message may be sent with no text while some are attached.")],
            [typeof(BxChatPrompt)]),
        new("chat-prompt-submit", "ChatPromptSubmit", "Chat", "The prompt's button: send, stop while the answer is on its way, retry after an error.",
            [new("By status", typeof(Examples.ChatPromptSubmitExample), "Send is a submit button and works in static rendering; stop and retry need interactivity.")],
            [typeof(BxChatPromptSubmit)]),
        new("chat-palette", "ChatPalette", "Chat", "A full chat panel: messages that take the room that is left, and a prompt pinned under them. Put it in a modal, a slideover or any box with a height.",
            [new("Streaming chat panel", typeof(Examples.ChatPaletteExample), "Interactive: tokens arrive one by one and Stop cancels. Static rendering: the prompt posts and the page renders the whole answer.")],
            [typeof(BxChatPalette)]),
        new("chat-attachment", "ChatAttachment", "Chat", "A file as a chip: an icon or an image preview, its name and size, and a remove button.",
            [new("Cards and thumbnails", typeof(Examples.ChatAttachmentExample), "Idle, uploading and failed states; removing needs interactivity.")],
            [typeof(BxChatAttachment)]),
        new("chat-reasoning", "ChatReasoning", "Chat", "The model's thinking as a collapsible block. It opens while streaming, closes shortly after, and tells how long it took.",
            [new("Thinking, live and done", typeof(Examples.ChatReasoningExample), "Opening and closing run in the browser; toggling it yourself during the stream wins over the automatic close.")],
            [typeof(BxChatReasoning)]),
        new("chat-shimmer", "ChatShimmer", "Chat", "Text with a sweeping highlight, for loading states. With reduced motion it is static, muted text.",
            [new("Shimmering text", typeof(Examples.ChatShimmerExample))],
            [typeof(BxChatShimmer)]),
        new("chat-tool", "ChatTool", "Chat", "A tool call of the assistant: its name, whether it runs or failed, and optionally its input and output in a collapsible part.",
            [new("States and approval", typeof(Examples.ChatToolExample), "Running, failed and done calls, and a card with allow and deny actions.")],
            [typeof(BxChatTool)]),
    ];
}
