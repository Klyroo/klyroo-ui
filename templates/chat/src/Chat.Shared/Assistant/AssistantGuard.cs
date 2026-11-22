using System.Net;
using System.Runtime.CompilerServices;

namespace Chat.Shared.Assistant;

/// <summary>
/// Who is asking, for the limits: the client address as the host sees it (grouped by /64 for IPv6, see <see cref="From"/>), taken from the request on the endpoint and from
/// the connection that opened the circuit in Server mode. Scoped.
/// </summary>
public sealed class CallerKey
{
    /// <summary>The key. Until a host sets it, a random one: a session that cannot be told apart is its own caller.</summary>
    public string Value { get; set; } = "anon-" + Guid.NewGuid().ToString("N");

    /// <summary>Set by the endpoint once the authorization policy of <see cref="AssistantOptions.AuthorizationPolicy"/> passed for this request.</summary>
    public bool PreAuthorized { get; set; }

    /// <summary>
    /// The key of an address. An IPv4-mapped IPv6 address counts as the IPv4 one, and an IPv6 address is cut to its /64 prefix: one subscriber of an ISP gets a whole /64,
    /// so counting the full address would let a single machine appear as billions of callers.
    /// </summary>
    public static string From(IPAddress? address)
    {
        if (address is null)
        {
            return "unknown";
        }
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }
        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return address.ToString();
        }
        var bytes = address.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return new IPAddress(bytes) + "/64";
    }
}

/// <summary>An authorization check for the replies that do not come through the endpoint (the Server render mode runs the provider in the circuit). The web host registers one when a policy is configured.</summary>
public interface IAssistantAuthorizer
{
    Task<bool> IsAllowedAsync();
}

/// <summary>
/// What protects the host that holds the key, or runs the demo, from a caller that asks too much. One instance for the whole process: its counters live in memory, so
/// they restart with the process and are not shared between replicas (each replica allows the full budgets). It refuses
/// a request that is too big or names a model that is not on offer, and limits, per caller and for everyone:
/// <list type="bullet">
/// <item>requests per minute (<see cref="AssistantOptions.RequestsPerMinute"/>) and replies in flight (<see cref="AssistantOptions.MaxConcurrentPerCaller"/>, <see cref="AssistantOptions.MaxConcurrentStreams"/>);</item>
/// <item>a daily budget of requests and of characters of output (<see cref="AssistantOptions.DailyRequestsPerCaller"/> and the others), per UTC day. A character of output is about a quarter of a token.</item>
/// </list>
/// At most <see cref="MaxCallers"/> callers are remembered. A caller that is not remembered yet is refused with 429 while the table is full of callers that are still within their
/// day: the callers already in it keep their limits, and an attacker with many addresses cannot push a legitimate visitor out or drain a shared bucket.
/// </summary>
public sealed class AssistantGuard(AssistantOptions options, TimeProvider? time = null)
{
    public const int MaxCallers = 10_000;

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly int _capacity = Math.Max(1, options.MaxConcurrentStreams);
    private readonly object _gate = new();
    private readonly Dictionary<string, Bucket> _callers = new(StringComparer.Ordinal);
    private int _inFlight;
    private int _globalDay;
    private long _globalRequests;
    private long _globalCharacters;

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
    /// (429 or 503) when a limit is reached; nothing is held then.
    /// </summary>
    public GuardSlot Enter(string caller)
    {
        var now = _time.GetTimestamp();
        var day = DayNumber();
        var key = caller.Length is > 0 and <= 128 ? caller : "~invalid";
        lock (_gate)
        {
            if (_globalDay != day)
            {
                (_globalDay, _globalRequests, _globalCharacters) = (day, 0, 0);
            }
            if (!_callers.TryGetValue(key, out var bucket))
            {
                if (_callers.Count >= MaxCallers)
                {
                    Purge(now, day);
                }
                if (_callers.Count >= MaxCallers)
                {
                    throw new CompletionException("The assistant is very busy: try again later.") { Status = 429 };
                }
                bucket = _callers[key] = new Bucket { Day = day };
            }
            if (bucket.Day != day)
            {
                (bucket.Day, bucket.Requests, bucket.Characters) = (day, 0, 0);
            }
            while (bucket.Hits.Count > 0 && _time.GetElapsedTime(bucket.Hits.Peek(), now) >= Window)
            {
                bucket.Hits.Dequeue();
            }
            if (bucket.Hits.Count >= options.RequestsPerMinute)
            {
                throw new CompletionException("Too many requests: wait a moment before sending another message.") { Status = 429 };
            }
            if (bucket.InFlight >= options.MaxConcurrentPerCaller)
            {
                throw new CompletionException("An answer is already arriving: wait for it to end.") { Status = 429 };
            }
            if (bucket.Requests >= options.DailyRequestsPerCaller || bucket.Characters >= options.DailyOutputCharactersPerCaller)
            {
                throw new CompletionException("The daily limit of this browser is reached: try again tomorrow.") { Status = 429 };
            }
            if (_globalRequests >= options.DailyRequestsGlobal || _globalCharacters >= options.DailyOutputCharactersGlobal)
            {
                throw new CompletionException("The assistant has reached its limit for today: try again tomorrow.") { Status = 503 };
            }
            // A counter, not a semaphore: nothing waits, a reply beyond the capacity is refused at once.
            if (_inFlight >= _capacity)
            {
                throw new CompletionException("The assistant is busy: try again in a moment.") { Status = 503 };
            }
            bucket.Hits.Enqueue(now);
            bucket.Last = now;
            bucket.Requests++;
            bucket.InFlight++;
            _globalRequests++;
            _inFlight++;
            return new GuardSlot(this, bucket);
        }
    }

    private int DayNumber() => (int)(_time.GetUtcNow().UtcDateTime.Date - DateTime.UnixEpoch).TotalDays;

    // Forgets the callers that are idle and whose day is over: they have nothing left to remember.
    private void Purge(long now, int day)
    {
        foreach (var (key, bucket) in _callers.ToArray())
        {
            if (bucket.InFlight == 0 && bucket.Day != day && _time.GetElapsedTime(bucket.Last, now) >= Window)
            {
                _callers.Remove(key);
            }
        }
    }

    internal void Add(Bucket bucket, int characters)
    {
        lock (_gate)
        {
            bucket.Characters += characters;
            _globalCharacters += characters;
        }
    }

    internal void Release(Bucket bucket)
    {
        lock (_gate)
        {
            bucket.InFlight--;
            _inFlight--;
        }
    }

    /// <summary>The callers in memory, for the tests.</summary>
    internal int Callers
    {
        get
        {
            lock (_gate)
            {
                return _callers.Count;
            }
        }
    }

    /// <summary>The replies in flight, for the tests.</summary>
    internal int InFlight
    {
        get
        {
            lock (_gate)
            {
                return _inFlight;
            }
        }
    }

    internal sealed class Bucket
    {
        public Queue<long> Hits { get; } = new();

        public long Last { get; set; }

        public int Day { get; set; }

        public long Requests { get; set; }

        public long Characters { get; set; }

        public int InFlight { get; set; }
    }

    /// <summary>A reply in flight. Add the characters of what is sent, dispose when it ends.</summary>
    public sealed class GuardSlot : IDisposable
    {
        private readonly AssistantGuard _guard;
        private readonly Bucket _bucket;
        private int _released;

        internal GuardSlot(AssistantGuard guard, Bucket bucket) => (_guard, _bucket) = (guard, bucket);

        public void AddOutput(int characters) => _guard.Add(_bucket, characters);

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                _guard.Release(_bucket);
            }
        }
    }
}

/// <summary>
/// The provider the pages and the endpoint of the web host use: the real one (or the simulated one) behind <see cref="AssistantGuard"/>. The request is checked
/// before anything is sent, and the slot is held while the reply streams.
/// </summary>
public sealed class GuardedCompletionProvider(ICompletionProvider inner, AssistantGuard guard, CallerKey caller, IAssistantAuthorizer? authorizer = null) : ICompletionProvider
{
    public Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default) => inner.GetModelsAsync(cancellationToken);

    public async IAsyncEnumerable<CompletionChunk> StreamAsync(CompletionRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (authorizer is not null && !caller.PreAuthorized && !await authorizer.IsAllowedAsync())
        {
            throw new CompletionException("Sign in to use the assistant.") { Status = 401 };
        }
        guard.Validate(request, await inner.GetModelsAsync(cancellationToken));
        using var slot = guard.Enter(caller.Value);
        await foreach (var chunk in inner.StreamAsync(request, cancellationToken))
        {
            slot.AddOutput(chunk.Text.Length);
            yield return chunk;
        }
    }
}
