using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Chat.Shared.Assistant;

/// <summary>
/// The provider of the WebAssembly half of the web app. It does not know the AI service, its address or the key: it asks the web host (<c>api/assistant/stream</c>,
/// see <see cref="Wire"/>), which does. The <see cref="HttpClient"/> must have the base address of the host (<c>&lt;base href&gt;</c>), so the calls go below the path base.
/// </summary>
public sealed class RemoteCompletionProvider(HttpClient http) : ICompletionProvider
{
    private IReadOnlyList<ModelInfo>? _models;

    public async Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        if (_models is not null)
        {
            return _models;
        }
        try
        {
            var json = await http.GetStringAsync(Wire.ModelsPath, cancellationToken);
            var models = Wire.ReadModels(json);
            if (models is not { Count: > 0 })
            {
                throw new CompletionException("The host offers no model.");
            }
            return _models = models;
        }
        catch (HttpRequestException error)
        {
            throw new CompletionException("The host could not be reached.", error);
        }
    }

    public async IAsyncEnumerable<CompletionChunk> StreamAsync(CompletionRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, Wire.StreamPath)
        {
            Content = new StringContent(Wire.Request(request), Encoding.UTF8, "application/json"),
        };
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException error)
        {
            throw new CompletionException("The host could not be reached.", error);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw await Refused(response, cancellationToken);
            }
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var done = false;
            await foreach (var data in SseReader.ReadDataAsync(stream, cancellationToken: cancellationToken))
            {
                var parsed = Wire.ReadEvent(data);
                if (parsed.Error is not null)
                {
                    throw new CompletionException(parsed.Error);
                }
                if (parsed.Done)
                {
                    done = true;
                    break;
                }
                if (parsed.Chunk is { } chunk)
                {
                    yield return chunk;
                }
            }
            if (!done)
            {
                // The host always ends a complete answer with a done event: without one the connection was cut, and what arrived is not the answer.
                throw new CompletionException("The connection to the assistant was interrupted.");
            }
        }
    }

    /// <summary>The host answers a refusal with <c>{"error":"..."}</c>: its message was written to be shown. Anything else gets a plain one.</summary>
    private static async Task<CompletionException> Refused(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var status = (int)response.StatusCode;
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (body.Length <= 4096)
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
                {
                    return new CompletionException(Wire.Bound(error.GetString())!) { Status = status };
                }
            }
        }
        catch (JsonException)
        {
        }
        return new CompletionException($"The host answered with an error (HTTP {status}).") { Status = status };
    }
}
