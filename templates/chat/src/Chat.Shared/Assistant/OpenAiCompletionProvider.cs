using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

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
public sealed class OpenAiCompletionProvider(HttpClient http, AssistantOptions options, ILogger<OpenAiCompletionProvider>? logger = null) : ICompletionProvider
{
    public Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default) =>
        options.Problems() is { Count: > 0 } problems
            ? Task.FromException<IReadOnlyList<ModelInfo>>(NotConfigured(problems))
            : Task.FromResult(options.ModelList());

    // What the visitor reads is generic: the hint for the operator (which setting to fix) goes to the log.
    private CompletionException NotConfigured(IReadOnlyList<string> problems)
    {
        logger?.LogWarning("The assistant is not configured: {Problem}", problems[0]);
        return new CompletionException("The assistant is not available right now.") { Status = 503 };
    }

    /// <summary>
    /// Streams the reply. A cancellation that the caller did not ask for (the time allowed ran out, in the middle of the stream too), a broken connection and a stream that ends
    /// before the service said it was done are failures, <see cref="CompletionException"/>, not a shorter answer: the caller must not take half an answer for a whole one.
    /// </summary>
    public async IAsyncEnumerable<CompletionChunk> StreamAsync(CompletionRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var pieces = Stream(request, cancellationToken).GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            bool more;
            try
            {
                more = await pieces.MoveNextAsync();
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger?.LogWarning("The assistant did not finish within Assistant:TimeoutSeconds ({Seconds})", options.TimeoutSeconds);
                throw new CompletionException("The assistant did not answer in time.") { Status = 504 };
            }
            catch (Exception error) when (error is HttpRequestException or IOException or JsonException)
            {
                logger?.LogWarning(error, "The stream of the assistant broke");
                throw new CompletionException("The connection to the assistant was interrupted.", error) { Status = 502 };
            }
            if (!more)
            {
                yield break;
            }
            yield return pieces.Current;
        }
    }

    private async IAsyncEnumerable<CompletionChunk> Stream(CompletionRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
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
            logger?.LogWarning(error, "The assistant could not be reached");
            throw new CompletionException("The assistant could not be reached.", error) { Status = 502 };
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
                // Bounded: a reply of MaxOutputCharacters, in JSON, with room for the rest of the envelope. A service cannot make us read more.
                var cap = (long)limit * 8 + 64 * 1024;
                using var whole = JsonDocument.Parse(await ReadBoundedAsync(stream, cap, token));
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

            var finished = false;
            await foreach (var data in SseReader.ReadDataAsync(stream, cancellationToken: token))
            {
                if (data.Trim() == "[DONE]")
                {
                    yield break;
                }
                finished |= data.Contains("\"finish_reason\":\"", StringComparison.Ordinal) || data.Contains("\"finish_reason\": \"", StringComparison.Ordinal);
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
            if (!finished)
            {
                // The stream ended with neither [DONE] nor a finish reason: the connection was cut, and what arrived is not the answer.
                throw new CompletionException("The connection to the assistant was interrupted.") { Status = 502 };
            }
        }
    }

    /// <summary>Reads the whole stream up to <paramref name="cap"/> bytes; more is refused.</summary>
    private static async Task<byte[]> ReadBoundedAsync(Stream stream, long cap, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > cap)
            {
                throw new CompletionException("The assistant sent a reply that is too long.") { Status = 502 };
            }
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
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
            if (options.MaxOutputTokens > 0)
            {
                writer.WriteNumber(options.MaxTokensField, options.MaxOutputTokens);
            }
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

    // The visitor reads a generic message; the status and the hint for the operator go to the log.
    private CompletionException Refused(int status)
    {
        var hint = status switch
        {
            401 or 403 => "the service refused the key: check Assistant:ApiKey",
            404 => "the service answered 404: check Assistant:BaseUrl and the model id",
            429 => "the service is rate limiting this key",
            400 or 422 => "the service rejected the request: check the model id and Assistant:MaxTokensField",
            >= 300 and < 400 => "the service answered with a redirect, which is not followed: check Assistant:BaseUrl",
            _ => "the service answered with an error",
        };
        logger?.LogWarning("The assistant service answered HTTP {Status}: {Hint}", status, hint);
        return status == 429
            ? new CompletionException("The assistant is receiving too many requests. Try again in a moment.") { Status = 429 }
            : new CompletionException("The assistant is not available right now.") { Status = 502 };
    }
}
