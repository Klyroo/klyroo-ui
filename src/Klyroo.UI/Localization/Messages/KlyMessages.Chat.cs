namespace Klyroo.UI;

public sealed partial record KlyMessages
{
    /// <summary>Accessible name of the list of messages of a conversation.</summary>
    public string ChatConversation { get; init; } = "Conversation";

    /// <summary>Text of the button that scrolls a conversation back to its latest message.</summary>
    public string ChatJumpToLatest { get; init; } = "Jump to latest";

    /// <summary>Read out while an answer is pending or arriving.</summary>
    public string ChatResponding { get; init; } = "Assistant is responding";

    /// <summary>Shown under the messages when the last request failed.</summary>
    public string ChatError { get; init; } = "Something went wrong. Try again.";

    /// <summary>Accessible names of the authors of a message.</summary>
    public string ChatUser { get; init; } = "You";
    /// <summary>Name of the author of an assistant message in <see cref="KlyChatMessage"/>, used as the accessible name of its avatar.</summary>
    public string ChatAssistant { get; init; } = "Assistant";
    /// <summary>Name of the author of a system message in <see cref="KlyChatMessage"/>, used as the accessible name of its avatar.</summary>
    public string ChatSystem { get; init; } = "System";

    /// <summary>Accessible name and placeholder of the message field of a chat prompt.</summary>
    public string ChatPromptLabel { get; init; } = "Message";
    /// <summary>Placeholder of the text area of <see cref="KlyChatPrompt"/>.</summary>
    public string ChatPromptPlaceholder { get; init; } = "Type a message...";

    /// <summary>Accessible names of the submit button of a chat prompt: send, stop the answer, retry after an error.</summary>
    public string ChatSend { get; init; } = "Send";
    /// <summary>Label of the stop button of <see cref="KlyChatPromptSubmit"/>, shown while an answer is on its way.</summary>
    public string ChatStop { get; init; } = "Stop";
    /// <summary>Label of the retry button of <see cref="KlyChatPromptSubmit"/>, shown after an error.</summary>
    public string ChatRetry { get; init; } = "Retry";

    /// <summary>Accessible name of the button that removes an attachment: {0} is the file name.</summary>
    public string ChatRemoveAttachment { get; init; } = "Remove {0}";

    /// <summary>Label of a reasoning block while it streams, and when it is done without a known duration.</summary>
    public string ChatThinking { get; init; } = "Thinking...";
    /// <summary>Title of <see cref="KlyChatReasoning"/> once the reasoning has finished.</summary>
    public string ChatThought { get; init; } = "Thought";

    /// <summary>Label of a finished reasoning block: {0} is the number of seconds.</summary>
    public string ChatThoughtFor { get; init; } = "Thought for {0} seconds";

    /// <summary>Read out after the name of a tool call that is running or failed.</summary>
    public string ChatToolRunning { get; init; } = "running";
    /// <summary>Word that <see cref="KlyChatTool"/> adds to its name when the tool call failed.</summary>
    public string ChatToolFailed { get; init; } = "failed";

    /// <summary>Read out after the name of an attachment that is uploading or failed.</summary>
    public string ChatAttachmentUploading { get; init; } = "uploading";
    /// <summary>Word that <see cref="KlyChatAttachment"/> shows when the upload of a file failed.</summary>
    public string ChatAttachmentFailed { get; init; } = "failed";
}
