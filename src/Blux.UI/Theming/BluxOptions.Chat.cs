using Blux.UI.Theming;

namespace Blux.UI;

public sealed partial class BluxOptions
{
    public ComponentTheme ChatMessage { get; } = Themes.ChatMessage();
    public ComponentTheme ChatMessages { get; } = Themes.ChatMessages();
    public ComponentTheme ChatPrompt { get; } = Themes.ChatPrompt();
    public ComponentTheme ChatPromptSubmit { get; } = Themes.ChatPromptSubmit();
    public ComponentTheme ChatPalette { get; } = Themes.ChatPalette();
    public ComponentTheme ChatAttachment { get; } = Themes.ChatAttachment();
    public ComponentTheme ChatReasoning { get; } = Themes.ChatReasoning();
    public ComponentTheme ChatShimmer { get; } = Themes.ChatShimmer();
    public ComponentTheme ChatTool { get; } = Themes.ChatTool();
}

public sealed partial class BluxStrings
{
    /// <summary>Accessible name of the list of messages of a conversation.</summary>
    public string ChatConversation { get; set; } = "Conversation";

    /// <summary>Text of the button that scrolls a conversation back to its latest message.</summary>
    public string ChatJumpToLatest { get; set; } = "Jump to latest";

    /// <summary>Read out while an answer is pending or arriving.</summary>
    public string ChatResponding { get; set; } = "Assistant is responding";

    /// <summary>Shown under the messages when the last request failed.</summary>
    public string ChatError { get; set; } = "Something went wrong. Try again.";

    /// <summary>Accessible names of the authors of a message.</summary>
    public string ChatUser { get; set; } = "You";
    public string ChatAssistant { get; set; } = "Assistant";
    public string ChatSystem { get; set; } = "System";

    /// <summary>Accessible name and placeholder of the message field of a chat prompt.</summary>
    public string ChatPromptLabel { get; set; } = "Message";
    public string ChatPromptPlaceholder { get; set; } = "Type a message...";

    /// <summary>Accessible names of the submit button of a chat prompt: send, stop the answer, retry after an error.</summary>
    public string ChatSend { get; set; } = "Send";
    public string ChatStop { get; set; } = "Stop";
    public string ChatRetry { get; set; } = "Retry";

    /// <summary>Accessible name of the button that removes an attachment: {0} is the file name.</summary>
    public string ChatRemoveAttachment { get; set; } = "Remove {0}";

    /// <summary>Label of a reasoning block while it streams, and when it is done without a known duration.</summary>
    public string ChatThinking { get; set; } = "Thinking...";
    public string ChatThought { get; set; } = "Thought";

    /// <summary>Label of a finished reasoning block: {0} is the number of seconds.</summary>
    public string ChatThoughtFor { get; set; } = "Thought for {0} seconds";

    /// <summary>Read out after the name of a tool call that is running or failed.</summary>
    public string ChatToolRunning { get; set; } = "running";
    public string ChatToolFailed { get; set; } = "failed";

    /// <summary>Read out after the name of an attachment that is uploading or failed.</summary>
    public string ChatAttachmentUploading { get; set; } = "uploading";
    public string ChatAttachmentFailed { get; set; } = "failed";
}
