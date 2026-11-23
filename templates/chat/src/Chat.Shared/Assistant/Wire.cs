using System.Text;
using System.Text.Json;

namespace Chat.Shared.Assistant;

/// <summary>
/// The messages between the WebAssembly half of the app and the web host, which holds the key: a JSON request, and an event stream of JSON events in answer.
/// The browser never talks to the AI service. Written and read with <see cref="Utf8JsonWriter"/> and <see cref="JsonDocument"/>, so nothing here depends on
/// reflection (a trimmed WebAssembly app keeps working).
/// <list type="bullet">
/// <item><c>POST api/assistant/stream</c> with <c>{"model":"...","messages":[{"role":"user","content":"..."}]}</c> answers <c>text/event-stream</c>.</item>
/// <item>Each event is <c>{"p":"t","d":"text"}</c> (the answer), <c>{"p":"r","d":"text"}</c> (the thinking), <c>{"error":"message"}</c> or <c>{"done":true}</c>.</item>
/// <item>A request the host refuses is answered with a status and <c>{"error":"message"}</c>, before any event.</item>
/// <item><c>GET api/assistant/models</c> answers <c>[{"id":"...","name":"..."}]</c>: the ids and names of the picker, nothing else about the configuration.</item>
/// </list>
/// </summary>
public static class Wire
{
    public const string StreamPath = "api/assistant/stream";
    public const string ModelsPath = "api/assistant/models";

    /// <summary>The largest request body the host reads, in bytes. The limits of <see cref="AssistantOptions"/> are checked after it.</summary>
    public const int MaxRequestBytes = 512 * 1024;

    public static string Request(CompletionRequest request)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("model", request.Model);
            writer.WriteStartArray("messages");
            foreach (var message in request.Messages)
            {
                writer.WriteStartObject();
                writer.WriteString("role", message.Role);
                writer.WriteString("content", message.Content);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>Reads a request. Returns null for anything that is not the shape above (the host answers 400). No limit of the options is applied here.</summary>
    public static CompletionRequest? ReadRequest(ReadOnlySpan<byte> json)
    {
        try
        {
            using var document = JsonDocument.Parse(json.ToArray(), new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("model", out var model) || model.ValueKind != JsonValueKind.String
                || !root.TryGetProperty("messages", out var messages) || messages.ValueKind != JsonValueKind.Array)
            {
                return null;
            }
            var list = new List<CompletionMessage>();
            foreach (var message in messages.EnumerateArray())
            {
                if (message.ValueKind != JsonValueKind.Object
                    || !message.TryGetProperty("role", out var role) || role.ValueKind != JsonValueKind.String
                    || !message.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.String)
                {
                    return null;
                }
                list.Add(new CompletionMessage(role.GetString()!, content.GetString()!));
            }
            return new CompletionRequest(model.GetString()!, list);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string Event(CompletionChunk chunk) =>
        Object(("p", chunk.Part == CompletionPart.Reasoning ? "r" : "t"), ("d", chunk.Text));

    public static string Error(string message) => Object(("error", message));

    public const string Done = "{\"done\":true}";

    public static string Models(IReadOnlyList<ModelInfo> models)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartArray();
            foreach (var model in models)
            {
                writer.WriteStartObject();
                writer.WriteString("id", model.Id);
                writer.WriteString("name", model.Name);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    public static IReadOnlyList<ModelInfo>? ReadModels(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }
            var models = new List<ModelInfo>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String && id.GetString() is { Length: > 0 } text)
                {
                    var name = item.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : null;
                    models.Add(new ModelInfo(text, string.IsNullOrWhiteSpace(name) ? text : name));
                }
            }
            return models;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>What an event of the stream says: the piece of the reply, a failure (the message is safe to show), or the end.</summary>
    public readonly record struct Parsed(CompletionChunk? Chunk, string? Error, bool Done);

    public static Parsed ReadEvent(string data)
    {
        try
        {
            using var document = JsonDocument.Parse(data);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return default;
            }
            if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
            {
                return new Parsed(null, Bound(error.GetString()), false);
            }
            if (root.TryGetProperty("done", out var done) && done.ValueKind == JsonValueKind.True)
            {
                return new Parsed(null, null, true);
            }
            if (root.TryGetProperty("d", out var text) && text.ValueKind == JsonValueKind.String && text.GetString() is { Length: > 0 } piece)
            {
                var part = root.TryGetProperty("p", out var p) && p.ValueKind == JsonValueKind.String && p.GetString() == "r" ? CompletionPart.Reasoning : CompletionPart.Text;
                return new Parsed(new CompletionChunk(part, piece), null, false);
            }
        }
        catch (JsonException)
        {
        }
        return default;
    }

    /// <summary>A message from the network is shown to the user: cut, so a wrong host cannot fill the screen.</summary>
    public static string? Bound(string? message) =>
        message is null ? null : message.Length <= 300 ? message : message[..300];

    private static string Object(params (string Name, string Value)[] fields)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            foreach (var (name, value) in fields)
            {
                writer.WriteString(name, value);
            }
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
