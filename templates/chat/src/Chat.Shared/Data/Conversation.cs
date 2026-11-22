using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Chat.Shared.Assistant;

namespace Chat.Shared.Data;

/// <summary>One message of a conversation, as it is shown and stored: the question of the user or the answer, with the thinking that came before it when the model sent some.</summary>
public sealed class ConversationMessage
{
    public string Id { get; set; } = NewId();

    /// <summary><c>user</c> or <c>assistant</c>.</summary>
    public string Role { get; set; } = CompletionMessage.UserRole;

    public string Content { get; set; } = "";

    /// <summary>The thinking the model sent before its answer, as plain text, or null.</summary>
    public string? Reasoning { get; set; }

    /// <summary>How long the thinking took, in seconds, when it was measured.</summary>
    public int? ThinkingSeconds { get; set; }

    /// <summary>The model that wrote an answer.</summary>
    public string? Model { get; set; }

    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>The answer ended by an error (the connection broke, the service timed out) and is not complete.</summary>
    public bool Interrupted { get; set; }

    [JsonIgnore]
    public bool IsUser => Role == CompletionMessage.UserRole;

    public static string NewId() => Guid.NewGuid().ToString("N");
}

/// <summary>A conversation: a title, the model it uses and its messages, oldest first.</summary>
public sealed class Conversation
{
    public string Id { get; set; } = ConversationMessage.NewId();

    public string Title { get; set; } = "New conversation";

    public string Model { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<ConversationMessage> Messages { get; set; } = [];

    /// <summary>The characters of every message: what is compared with <see cref="ConversationLimits.MaxConversationCharacters"/>.</summary>
    [JsonIgnore]
    public long Characters => Messages.Sum(m => (long)m.Content.Length);

    /// <summary>The history to send to the model: user and assistant messages with a text, in order.</summary>
    public IReadOnlyList<CompletionMessage> History() =>
        [.. Messages.Where(m => m.Content.Length > 0).Select(m => new CompletionMessage(m.Role, m.Content))];

    /// <summary>The title a conversation takes from its first question: one line, cut at a word.</summary>
    public static string TitleFrom(string text)
    {
        var line = Regex.Replace(text, @"\s+", " ").Trim();
        if (line.Length == 0)
        {
            return "New conversation";
        }
        if (line.Length <= ConversationLimits.MaxTitle)
        {
            return line;
        }
        var cut = line[..ConversationLimits.MaxTitle];
        var space = cut.LastIndexOf(' ');
        return (space > ConversationLimits.MaxTitle / 2 ? cut[..space] : cut).TrimEnd() + "...";
    }
}

/// <summary>The bounds of what a browser keeps: its storage is a few megabytes shared by every app of the site, and what it holds can be edited by the user.</summary>
public static class ConversationLimits
{
    /// <summary>The conversations kept. Creating one beyond it deletes the least recently used.</summary>
    public const int MaxConversations = 100;

    /// <summary>The messages of one conversation.</summary>
    public const int MaxMessages = 60;

    /// <summary>The characters of one question.</summary>
    public const int MaxPromptCharacters = 8_000;

    /// <summary>
    /// The characters of all the messages of a conversation: past it, the conversation is full and a new one starts. It is also the default of <c>Assistant:MaxInputCharacters</c>
    /// (what the host accepts), so the page never builds a conversation the host refuses.
    /// </summary>
    public const int MaxConversationCharacters = 60_000;

    public const int MaxTitle = 80;

    /// <summary>The longest message read back from the storage, a safety net for what was edited by hand.</summary>
    public const int MaxStoredMessageCharacters = 40_000;

    public const int MaxStoredReasoningCharacters = 20_000;
}

/// <summary>The conversations as JSON, and the way back: what comes out of the storage is untrusted and is checked field by field.</summary>
public static partial class ConversationJson
{
    [GeneratedRegex("^[a-f0-9]{32}$")]
    private static partial Regex IdPattern();

    public static bool IsValidId(string? id) => id is not null && IdPattern().IsMatch(id);

    public static string Write(Conversation conversation) =>
        JsonSerializer.Serialize(conversation, ConversationContext.Default.Conversation);

    /// <summary>
    /// Reads a conversation. False when the text is not one. A role that is not user or assistant, a field of the wrong type or a message past the
    /// limits does not make it fail: the message is dropped or cut, so a hand-edited file cannot put anything on the page that the app would not have written.
    /// </summary>
    public static bool TryRead(string? json, out Conversation conversation)
    {
        conversation = new Conversation();
        if (string.IsNullOrWhiteSpace(json) || json.Length > 4 * ConversationLimits.MaxConversationCharacters)
        {
            return false;
        }
        Conversation? read;
        try
        {
            read = JsonSerializer.Deserialize(json, ConversationContext.Default.Conversation);
        }
        catch (JsonException)
        {
            return false;
        }
        if (read is null || !IsValidId(read.Id))
        {
            return false;
        }
        conversation = new Conversation
        {
            Id = read.Id,
            Title = Conversation.TitleFrom(read.Title ?? ""),
            Model = Truncate(read.Model ?? "", 100),
            CreatedAt = Clamp(read.CreatedAt),
            UpdatedAt = Clamp(read.UpdatedAt),
        };
        long total = 0;
        foreach (var message in (read.Messages ?? []).TakeLast(ConversationLimits.MaxMessages))
        {
            if (message is null || message.Role is not (CompletionMessage.UserRole or CompletionMessage.AssistantRole) || !IsValidId(message.Id))
            {
                continue;
            }
            var content = Truncate(message.Content ?? "", ConversationLimits.MaxStoredMessageCharacters);
            total += content.Length;
            if (total > ConversationLimits.MaxConversationCharacters * 2L)
            {
                // Over what the app ever stores: keep the most recent part.
                conversation.Messages.Clear();
                total = content.Length;
            }
            conversation.Messages.Add(new ConversationMessage
            {
                Id = message.Id,
                Role = message.Role,
                Content = content,
                Reasoning = message.Reasoning is null ? null : Truncate(message.Reasoning, ConversationLimits.MaxStoredReasoningCharacters),
                ThinkingSeconds = message.ThinkingSeconds is >= 0 and <= 36_000 ? message.ThinkingSeconds : null,
                Model = message.Model is null ? null : Truncate(message.Model, 100),
                Interrupted = message.Interrupted,
                At = Clamp(message.At),
            });
        }
        return true;
    }

    private static string Truncate(string text, int max) => text.Length <= max ? text : text[..max];

    // A date from a hand-edited file: the year 1 or 9999 would break the grouping by day.
    private static DateTimeOffset Clamp(DateTimeOffset value) =>
        value.Year is < 2000 or > 2100 ? DateTimeOffset.UtcNow : value;
}

/// <summary>Source-generated JSON for the stored conversations: no reflection, so it survives trimming in the WebAssembly app and in the MAUI app.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Conversation))]
internal sealed partial class ConversationContext : JsonSerializerContext;
