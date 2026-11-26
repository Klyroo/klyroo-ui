using System.Buffers;
using System.Text.Json;

namespace Klyroo.UI;

/// <summary>
/// The share of persistent component state that the Klyroo libraries of one page may hand from the prerender to the interactive render,
/// shared by all of them (Klyroo.Table lists, Klyroo.Dashboard resources). The browser sends everything a page persisted back to the server in
/// the single SignalR message that starts an Interactive Server circuit, and SignalR refuses that message past 32 KB by default, which fails the
/// circuit: each library measures what it writes with <see cref="SentBytes"/> and reserves it here, and skips (then loads again) what does not fit.
/// </summary>
/// <remarks>
/// Registered as a scoped service by <c>AddKlyrooUi</c>, so one instance covers a prerendered request, or a whole Server circuit. The limit is
/// <see cref="KlyrooOptions.PersistedStateMaxBytes"/>. Thread-safe.
/// </remarks>
public sealed class KlyPersistedStateBudget
{
    private readonly object _gate = new();
    private long _used;

    /// <summary>Creates the budget of a scope. Use the instance <c>AddKlyrooUi</c> registers rather than creating one.</summary>
    /// <param name="options">The options holding <see cref="KlyrooOptions.PersistedStateMaxBytes"/>.</param>
    public KlyPersistedStateBudget(KlyrooOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        MaxBytes = options.PersistedStateMaxBytes;
    }

    /// <summary>The bytes, as sent, the libraries of the page may persist together.</summary>
    public long MaxBytes { get; }

    /// <summary>The bytes, as sent, already reserved in this scope.</summary>
    public long UsedBytes
    {
        get
        {
            lock (_gate) return _used;
        }
    }

    /// <summary>Takes <paramref name="bytes"/> from the budget when they fit, and returns whether they did. Nothing is taken when they do not.</summary>
    /// <param name="bytes">The size as sent, from <see cref="SentBytes"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bytes"/> is negative.</exception>
    public bool TryReserve(long bytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);
        lock (_gate)
        {
            if (_used + bytes > MaxBytes) return false;
            _used += bytes;
            return true;
        }
    }

    /// <summary>
    /// What a value persisted as text under <paramref name="key"/> weighs in the message that starts a Server circuit. Blazor writes the text as a
    /// JSON string, which escapes quotes, <c>+</c>, <c>&lt;</c> and non-ASCII characters, keeps it as bytes in a dictionary that is serialized
    /// with base64, then protects and base64-encodes the whole state again: about the escaped length plus the key, times 16/9.
    /// </summary>
    /// <param name="key">The key the text is persisted under.</param>
    /// <param name="text">The text persisted with <c>PersistAsJson&lt;string&gt;</c>.</param>
    /// <returns>The estimated size in bytes.</returns>
    public static long SentBytes(string key, string text)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(text);
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStringValue(text);
        }
        // The key and the punctuation of its dictionary entry ("key":"...",) travel once, inside the second encoding only.
        return (long)buffer.WrittenCount * 16 / 9 + (key.Length + 6) * 4 / 3 + 1;
    }
}
