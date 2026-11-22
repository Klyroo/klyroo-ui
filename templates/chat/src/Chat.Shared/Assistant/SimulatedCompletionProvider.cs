using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Chat.Shared.Assistant;

/// <summary>
/// An <see cref="ICompletionProvider"/> that answers from a handful of canned Markdown replies, a word at a time with a short pause between words. It makes no
/// network call and holds no state, so it is what the live demo of the template runs, and what the tests and a first run without a key use. The reply is picked
/// from the last message by keywords, the same one for the same message; the second model sends a few lines of "thinking" first, to show how a model that exposes
/// its reasoning looks. A reply is never longer than a couple of thousand characters, so the work one request can cause is bounded.
/// </summary>
public sealed partial class SimulatedCompletionProvider(AssistantOptions options) : ICompletionProvider
{
    public static IReadOnlyList<ModelInfo> DemoModels { get; } =
    [
        new("demo-quick", "Quick (simulated)"),
        new("demo-thoughtful", "Thoughtful (simulated)"),
    ];

    /// <summary>The longest text the provider ever sends, in characters, whatever the options say.</summary>
    public const int MaxReplyCharacters = 4_000;

    public Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(DemoModels);

    public async IAsyncEnumerable<CompletionChunk> StreamAsync(CompletionRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var prompt = request.Messages.LastOrDefault(m => m.Role == CompletionMessage.UserRole)?.Content ?? "";
        var delay = TimeSpan.FromMilliseconds(Math.Clamp(options.SimulatedDelayMilliseconds, 0, 200));
        var limit = Math.Min(MaxReplyCharacters, options.MaxOutputCharacters);

        // A pause before the first word, like a service that has to start.
        await Pause(delay * 6, cancellationToken);

        if (request.Model == "demo-thoughtful")
        {
            foreach (var piece in Pieces(Thought(prompt)))
            {
                await Pause(delay, cancellationToken);
                yield return new CompletionChunk(CompletionPart.Reasoning, piece);
            }
        }

        var sent = 0;
        foreach (var piece in Pieces(Reply(prompt)))
        {
            if (sent + piece.Length > limit)
            {
                yield break;
            }
            await Pause(delay, cancellationToken);
            sent += piece.Length;
            yield return new CompletionChunk(CompletionPart.Text, piece);
        }
    }

    private static Task Pause(TimeSpan delay, CancellationToken cancellationToken) =>
        delay > TimeSpan.Zero ? Task.Delay(delay, cancellationToken) : Task.CompletedTask;

    /// <summary>The words of a text, each with the space after it, so that joining the pieces gives the text back.</summary>
    internal static IEnumerable<string> Pieces(string text) =>
        Word().Matches(text).Select(m => m.Value);

    [GeneratedRegex(@"\S+\s*|\s+")]
    private static partial Regex Word();

    /// <summary>The reply for a prompt: a canned one picked by keywords, after a one line quote of the question.</summary>
    internal static string Reply(string prompt)
    {
        var body = Pick(prompt);
        return $"> {Quote(prompt)}\n\n{body}";
    }

    internal static string Thought(string prompt) =>
        $"The question is \"{Quote(prompt, 60)}\". I will answer it in two short parts: the idea first, then a small example you can run. " +
        "I keep the wording plain and check that the example is the smallest one that shows the point.";

    /// <summary>The prompt on one line, cut, with nothing that could be taken for markup left to the renderer: it is Markdown too, and is made safe like the rest.</summary>
    internal static string Quote(string prompt, int max = 110)
    {
        var line = Whitespace().Replace(prompt, " ").Trim();
        if (line.Length == 0)
        {
            return "(empty)";
        }
        return line.Length <= max ? line : line[..max].TrimEnd() + "...";
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    private static string Pick(string prompt)
    {
        var text = prompt.ToLowerInvariant();
        if (ContainsAny(text, "mutex", "semaphore", "lock", "thread"))
        {
            return Locks;
        }
        if (ContainsAny(text, "debounce", "c#", "csharp", "code", "method", "function", "write a"))
        {
            return Debounce;
        }
        if (ContainsAny(text, "email", "mail", "polite", "extension", "letter"))
        {
            return Email;
        }
        if (ContainsAny(text, "sqlite", "postgres", "database", "sql"))
        {
            return Databases;
        }
        return Overview;
    }

    private static bool ContainsAny(string text, params string[] words) => words.Any(text.Contains);

    private const string Locks = """
        ## Mutex or semaphore

        Both guard a shared resource, and the difference is how many callers may hold them at once.

        | | Mutex | Semaphore |
        | --- | --- | --- |
        | Holders at once | one | up to a count you choose |
        | Released by | the thread that took it | any thread |
        | Typical use | a critical section | a pool of connections |

        In C# a `lock` is a mutex for one process, and `SemaphoreSlim` is the semaphore that works with `await`:

        ```csharp
        var gate = new SemaphoreSlim(3);

        async Task CallAsync(HttpClient http, string url)
        {
            await gate.WaitAsync();
            try { await http.GetStringAsync(url); }
            finally { gate.Release(); }
        }
        ```

        Release in a `finally`, or one failed call keeps a slot forever.
        """;

    private const string Debounce = """
        ## A small debouncer

        A debouncer runs an action once the calls stop for a while. Each new call cancels the one waiting:

        ```csharp
        public sealed class Debouncer(TimeSpan delay)
        {
            private CancellationTokenSource? _pending;

            public async Task RunAsync(Func<Task> action)
            {
                _pending?.Cancel();
                var source = _pending = new CancellationTokenSource();
                try
                {
                    await Task.Delay(delay, source.Token);
                    await action();
                }
                catch (OperationCanceledException)
                {
                    // A newer call took over.
                }
            }
        }
        ```

        - Use it for a search box that queries a server: type, pause, one request.
        - Keep the delay short, 250 to 400 ms feels immediate.
        - Dispose the last source when the owner goes away.
        """;

    private const string Email = """
        ## A short, polite request

        Here is a draft you can adjust:

        > Subject: Request for a short extension
        >
        > Hello Sam,
        >
        > Thank you for the clear brief. To deliver the report at the quality it deserves, I would like to ask for two more working days. I can send you the outline today, so you can see where it stands.
        >
        > Would that work for you?
        >
        > Best regards

        **Why it works:** it names the reason, asks for a specific amount of time, and offers something in the meantime.
        """;

    private const string Databases = """
        ## SQLite or PostgreSQL for a small app

        Start from where the data lives and who writes to it.

        | | SQLite | PostgreSQL |
        | --- | --- | --- |
        | Runs as | a file in your app | a server |
        | Writers at once | one | many |
        | Backups | copy the file | dump or replicate |
        | Setup | none | an instance to run |

        **Pick SQLite** when one app owns the data, the load is modest, and you want nothing to operate. **Pick PostgreSQL** when several services write, or you need its extensions and roles.

        ```bash
        # SQLite: nothing to install, the database is a file
        sqlite3 app.db "select count(*) from orders;"
        ```
        """;

    private const string Overview = """
        ## Here is what I can show

        This reply is canned: the demo runs without a model, and answers from a few prepared texts. It still shows how a streamed answer renders:

        1. **Text** with *emphasis* and `inline code`.
        2. Lists, like this one.
        3. A [link](https://learn.microsoft.com/dotnet/) to the .NET documentation.

        ```json
        { "model": "demo-quick", "stream": true }
        ```

        Ask about **locks**, a **debounce** method, a polite **email** or **SQLite** to see the other replies.
        """;
}
