using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Chat.Shared.Assistant;

/// <summary>
/// Streams a reply from a service that speaks the OpenAI completions API (<c>POST {BaseUrl}/chat/completions</c> with <c>stream: true</c>): OpenAI itself, or a
/// compatible one (a local server, a gateway). Plain <see cref="HttpClient"/> and a small parser of the event stream, no SDK: the part of the API the app uses is
/// one request and one stream, and an SDK would bring its own dependencies into the WebAssembly download and the MAUI app.
/// <para>
/// The address and the key come from <see cref="AssistantOptions"/> only. The HTTP client the host gives must not follow redirects (so a key is never sent
/// to another address). What the service answers with when it refuses is never shown: the status is mapped to a message that holds neither the body nor the key.
/// </para>
/// </summary>
public sealed class OpenAiCompletionProvider(HttpClient http, AssistantOptions options) : ICompletionProvider
{
    public Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default) =>
        options.Problems() is { Count: > 0 } problems
            ? Task.FromException<IReadOnlyList<ModelInfo>>(NotConfigured(problems))
            : Task.FromResult(options.ModelList());

    private static CompletionException NotConfigured(IReadOnlyList<string> problems) =>
        new("The assistant is not configured. " + problems[0]) { Status = 503 };

    public async IAsyncEnumerable<CompletionChunk> StreamAsync(CompletionRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (options.Problems() is { Count: > 0 } problems)
        {
            throw NotConfigured(problems);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
        var token = timeout.Token;

        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(options.BaseUrl.Trim().TrimEnd('/') + "/chat/completions"))
        {
            Content = new StringContent(Body(request), Encoding.UTF8, "application/json"),
        };
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json", 0.5));
        if (!string.IsNullOrEmpty(options.ApiKey))
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        }

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, token);
        }
        catch (HttpRequestException error)
        {
            throw new CompletionException("The assistant could not be reached.", error) { Status = 502 };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CompletionException("The assistant did not answer in time.") { Status = 504 };
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw Refused((int)response.StatusCode);
            }

            var emitted = 0;
            var limit = options.MaxOutputCharacters;
            await using var stream = await response.Content.ReadAsStreamAsync(token);

            if (response.Content.Headers.ContentType?.MediaType is "application/json")
            {
                // A service that ignored stream: true and answered with the whole reply at once.
                using var whole = await JsonDocument.ParseAsync(stream, cancellationToken: token);
                foreach (var chunk in Whole(whole.RootElement))
                {
                    var text = Cut(chunk.Text, ref emitted, limit);
                    if (text.Length > 0)
                    {
                        yield return chunk with { Text = text };
                    }
                }
                yield break;
            }

            await foreach (var data in SseReader.ReadDataAsync(stream, cancellationToken: token))
            {
                if (data.Trim() == "[DONE]")
                {
                    yield break;
                }
                foreach (var chunk in Parse(data))
                {
                    var text = Cut(chunk.Text, ref emitted, limit);
                    if (text.Length > 0)
                    {
                        yield return chunk with { Text = text };
                    }
                    if (emitted >= limit)
                    {
                        // Closing the stream (leaving the loop disposes the response) is how the service is told to stop.
                        yield break;
                    }
                }
            }
        }
    }

    /// <summary>The request body: the model, the history (with the system prompt in front) and <c>stream: true</c>.</summary>
    internal string Body(CompletionRequest request)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("model", request.Model);
            writer.WriteBoolean("stream", true);
            writer.WriteStartArray("messages");
            if (!string.IsNullOrWhiteSpace(options.SystemPrompt))
            {
                WriteMessage(writer, CompletionMessage.SystemRole, options.SystemPrompt);
            }
            foreach (var message in request.Messages)
            {
                WriteMessage(writer, message.Role, message.Content);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void WriteMessage(Utf8JsonWriter writer, string role, string content)
    {
        writer.WriteStartObject();
        writer.WriteString("role", role);
        writer.WriteString("content", content);
        writer.WriteEndObject();
    }

    /// <summary>The pieces of one event of the stream: <c>choices[0].delta.content</c>, and the thinking that some services send as <c>reasoning_content</c> or <c>reasoning</c>.</summary>
    internal static IEnumerable<CompletionChunk> Parse(string data)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(data);
        }
        catch (JsonException)
        {
            // A line that is not JSON (a keep-alive, a banner): not part of the reply.
            yield break;
        }
        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                yield break;
            }
            if (root.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null)
            {
                throw new CompletionException("The assistant reported an error while answering.");
            }
            if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0)
            {
                yield break;
            }
            var choice = choices[0];
            if (choice.ValueKind != JsonValueKind.Object || !choice.TryGetProperty("delta", out var delta) || delta.ValueKind != JsonValueKind.Object)
            {
                yield break;
            }
            foreach (var name in new[] { "reasoning_content", "reasoning" })
            {
                if (delta.TryGetProperty(name, out var thinking) && thinking.ValueKind == JsonValueKind.String && thinking.GetString() is { Length: > 0 } thought)
                {
                    yield return new CompletionChunk(CompletionPart.Reasoning, thought);
                    break;
                }
            }
            if (delta.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String && content.GetString() is { Length: > 0 } text)
            {
                yield return new CompletionChunk(CompletionPart.Text, text);
            }
        }
    }

    private static IEnumerable<CompletionChunk> Whole(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0)
        {
            yield break;
        }
        var choice = choices[0];
        if (choice.ValueKind == JsonValueKind.Object && choice.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.Object
            && message.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String && content.GetString() is { Length: > 0 } text)
        {
            yield return new CompletionChunk(CompletionPart.Text, text);
        }
    }

    /// <summary>The part of <paramref name="text"/> that fits in what is left of <paramref name="limit"/>.</summary>
    private static string Cut(string text, ref int emitted, int limit)
    {
        var room = limit - emitted;
        if (room <= 0)
        {
            return "";
        }
        var kept = text.Length <= room ? text : text[..room];
        emitted += kept.Length;
        return kept;
    }

    private static CompletionException Refused(int status) => status switch
    {
        401 or 403 => new CompletionException($"The assistant refused the request (HTTP {status}): check Assistant:ApiKey.") { Status = 502 },
        404 => new CompletionException("The assistant answered HTTP 404: check Assistant:BaseUrl and the model id.") { Status = 502 },
        429 => new CompletionException("The assistant is receiving too many requests (HTTP 429). Try again in a moment.") { Status = 429 },
        400 or 422 => new CompletionException($"The assistant rejected the request (HTTP {status}): check the model id.") { Status = 502 },
        >= 300 and < 400 => new CompletionException("The assistant answered with a redirect, which is not followed: check Assistant:BaseUrl.") { Status = 502 },
        _ => new CompletionException($"The assistant answered with an error (HTTP {status}).") { Status = 502 },
    };
}
