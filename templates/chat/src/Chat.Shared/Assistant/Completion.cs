namespace Chat.Shared.Assistant;

/// <summary>One message of the history sent to the model. <see cref="Role"/> is <c>system</c>, <c>user</c> or <c>assistant</c>.</summary>
public sealed record CompletionMessage(string Role, string Content)
{
    public const string SystemRole = "system";
    public const string UserRole = "user";
    public const string AssistantRole = "assistant";

    public static bool IsKnownRole(string? role) => role is SystemRole or UserRole or AssistantRole;
}

/// <summary>The history of a conversation and the model that answers it.</summary>
public sealed record CompletionRequest(string Model, IReadOnlyList<CompletionMessage> Messages);

/// <summary>What a <see cref="CompletionChunk"/> carries: the answer, or the thinking that comes before it (models that expose it).</summary>
public enum CompletionPart
{
    Text,
    Reasoning,
}

/// <summary>A piece of the reply, in the order the model wrote it.</summary>
public readonly record struct CompletionChunk(CompletionPart Part, string Text);

/// <summary>A model the user can pick: the id sent to the service and the name shown in the picker.</summary>
public sealed record ModelInfo(string Id, string Name);

/// <summary>
/// The one thing the pages know about an AI service. An implementation streams the reply to a history, in pieces. It is the seam to swap: the app ships
/// <see cref="OpenAiCompletionProvider"/> (any service that speaks the OpenAI completions API), <see cref="SimulatedCompletionProvider"/> (canned replies,
/// no network, for demos and tests) and <see cref="RemoteCompletionProvider"/> (what the WebAssembly half calls: the web host, which holds the key).
/// </summary>
public interface ICompletionProvider
{
    /// <summary>The models on offer, the first one is the default. Never empty.</summary>
    Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams the reply. Cancelling the token stops it. A problem the user can act on (not configured, refused, too many requests) is a
    /// <see cref="CompletionException"/>; its message is safe to show.
    /// </summary>
    IAsyncEnumerable<CompletionChunk> StreamAsync(CompletionRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// A failure whose <see cref="Exception.Message"/> may be shown to the user: it never holds a key, an address of the service or the body it answered with.
/// Any other exception is shown as a generic message and kept for the logs.
/// </summary>
public sealed class CompletionException : Exception
{
    public CompletionException(string message) : base(message)
    {
    }

    public CompletionException(string message, Exception inner) : base(message, inner)
    {
    }

    /// <summary>The HTTP status to answer with when a host passes the failure on (the proxy endpoint of the web host).</summary>
    public int Status { get; init; } = 502;
}
