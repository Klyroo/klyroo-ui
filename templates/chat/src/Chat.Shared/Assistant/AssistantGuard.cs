using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Chat.Shared.Assistant;

/// <summary>Who is asking, for the rate limit: the address of the request on the endpoint of the web host, a random id per session of a circuit. Scoped.</summary>
public sealed class CallerKey
{
    public string Value { get; set; } = Guid.NewGuid().ToString("N");
}

/// <summary>
/// What protects the host that holds the key, or runs the demo, from a caller that asks too much: it refuses a request that is too big or names a model that is
/// not on offer, a caller that starts more than <see cref="AssistantOptions.RequestsPerMinute"/> replies in a minute, and any request beyond
/// <see cref="AssistantOptions.MaxConcurrentStreams"/> replies in flight. One instance for the whole host (the counters are shared by every visitor); it keeps at most
/// <see cref="MaxCallers"/> callers in memory.
/// </summary>
public sealed class AssistantGuard(AssistantOptions options, TimeProvider? time = null)
{
    /// <summary>The callers the guard remembers. When a new one arrives beyond it, the old entries are dropped; if none is old, the newcomer shares one bucket with the others.</summary>
    public const int MaxCallers = 10_000;

    private const string Overflow = "~overflow";
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly int _capacity = Math.Max(1, options.MaxConcurrentStreams);
    private int _inFlight;
    private readonly ConcurrentDictionary<string, Bucket> _callers = new(StringComparer.Ordinal);

    /// <summary>Refuses a request that is not one the app would send. The history ends with a user message, holds only user and assistant messages, and fits the limits.</summary>
    public void Validate(CompletionRequest request, IReadOnlyList<ModelInfo> models)
    {
        if (request.Messages.Count == 0 || request.Messages.Count > options.MaxMessages)
        {
            throw new CompletionException($"A conversation sent to the assistant holds between 1 and {options.MaxMessages} messages.") { Status = 400 };
        }
        if (request.Messages[^1].Role != CompletionMessage.UserRole)
        {
            throw new CompletionException("The last message must be the user's.") { Status = 400 };
        }
        long characters = 0;
        foreach (var message in request.Messages)
        {
            // The system message is the operator's (Assistant:SystemPrompt), never the caller's.
            if (message.Role is not (CompletionMessage.UserRole or CompletionMessage.AssistantRole) || string.IsNullOrWhiteSpace(message.Content))
            {
                throw new CompletionException("A message has no text or a role the assistant does not take.") { Status = 400 };
            }
            characters += message.Content.Length;
        }
        if (characters > options.MaxInputCharacters)
        {
            throw new CompletionException($"The conversation is too long to send ({options.MaxInputCharacters} characters at most). Start a new one.") { Status = 413 };
        }
        if (!models.Any(m => m.Id == request.Model))
        {
            throw new CompletionException("That model is not on offer.") { Status = 400 };
        }
    }

    /// <summary>
    /// Counts a reply for <paramref name="caller"/> and takes a slot for it. Dispose the result when the reply ends. Throws a <see cref="CompletionException"/>
    /// (429 or 503) when the caller asked too often or every slot is taken; nothing is held then.
    /// </summary>
    public IDisposable Enter(string caller)
    {
        var now = _time.GetTimestamp();
        if (!Allowed(caller, now))
        {
            throw new CompletionException("Too many requests: wait a moment before sending another message.") { Status = 429 };
        }
        // A counter, not a semaphore: nothing waits, a reply beyond the capacity is refused at once.
        if (Interlocked.Increment(ref _inFlight) > _capacity)
        {
            Interlocked.Decrement(ref _inFlight);
            throw new CompletionException("The assistant is busy: try again in a moment.") { Status = 503 };
        }
        return new Slot(this);
    }

    private bool Allowed(string caller, long now)
    {
        var key = caller.Length is > 0 and <= 128 ? caller : Overflow;
        if (!_callers.ContainsKey(key) && _callers.Count >= MaxCallers)
        {
            Purge(now);
            if (_callers.Count >= MaxCallers)
            {
                key = Overflow;
            }
        }
        var bucket = _callers.GetOrAdd(key, _ => new Bucket());
        lock (bucket)
        {
            while (bucket.Hits.Count > 0 && _time.GetElapsedTime(bucket.Hits.Peek(), now) >= Window)
            {
                bucket.Hits.Dequeue();
            }
            // The overflow bucket is shared by many callers: it is allowed more.
            var limit = key == Overflow ? options.RequestsPerMinute * 10 : options.RequestsPerMinute;
            if (bucket.Hits.Count >= limit)
            {
                return false;
            }
            bucket.Hits.Enqueue(now);
            bucket.Last = now;
            return true;
        }
    }

    private void Purge(long now)
    {
        foreach (var (key, bucket) in _callers)
        {
            lock (bucket)
            {
                if (_time.GetElapsedTime(bucket.Last, now) >= Window)
                {
                    _callers.TryRemove(key, out _);
                }
            }
        }
    }

    private sealed class Bucket
    {
        public Queue<long> Hits { get; } = new();

        public long Last { get; set; }
    }

    /// <summary>The callers in memory, for the tests.</summary>
    internal int Callers => _callers.Count;

    /// <summary>The replies in flight, for the tests.</summary>
    internal int InFlight => Volatile.Read(ref _inFlight);

    private sealed class Slot(AssistantGuard guard) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                Interlocked.Decrement(ref guard._inFlight);
            }
        }
    }
}

/// <summary>
/// The provider the pages and the endpoint of the web host use: the real one (or the simulated one) behind <see cref="AssistantGuard"/>. The request is checked
/// before anything is sent, and the slot is held while the reply streams.
/// </summary>
public sealed class GuardedCompletionProvider(ICompletionProvider inner, AssistantGuard guard, CallerKey caller) : ICompletionProvider
{
    public Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default) => inner.GetModelsAsync(cancellationToken);

    public async IAsyncEnumerable<CompletionChunk> StreamAsync(CompletionRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        guard.Validate(request, await inner.GetModelsAsync(cancellationToken));
        using var slot = guard.Enter(caller.Value);
        await foreach (var chunk in inner.StreamAsync(request, cancellationToken))
        {
            yield return chunk;
        }
    }
}
