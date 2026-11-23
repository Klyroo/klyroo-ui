using System.Runtime.CompilerServices;
using System.Text;

namespace Chat.Shared.Assistant;

/// <summary>
/// Reads a Server-Sent Events stream (<c>text/event-stream</c>) and gives the data of each event. Only the <c>data</c> field is read: that is all the OpenAI API
/// and the endpoint of the web host use. Lines end with LF, CRLF or CR, a line that starts with a colon is a comment, several <c>data</c> lines of one event are
/// joined with a line feed, and an event ends at a blank line (or at the end of the stream). A line longer than <paramref name="maxLineCharacters"/> is refused: the
/// stream comes from a service that is not under our control, and a stream with no line break would otherwise fill the memory.
/// </summary>
public static class SseReader
{
    public const int DefaultMaxLineCharacters = 256 * 1024;

    public static async IAsyncEnumerable<string> ReadDataAsync(
        Stream stream, int maxLineCharacters = DefaultMaxLineCharacters, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
        var buffer = new char[4096];
        var line = new StringBuilder();
        var data = new StringBuilder();
        var hasData = false;
        var previousWasCarriageReturn = false;

        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0)
            {
                break;
            }
            for (var i = 0; i < read; i++)
            {
                var c = buffer[i];
                if (c == '\n' && previousWasCarriageReturn)
                {
                    // The second half of a CRLF: the line ended at the CR.
                    previousWasCarriageReturn = false;
                    continue;
                }
                previousWasCarriageReturn = c == '\r';
                if (c is not ('\n' or '\r'))
                {
                    if (line.Length >= maxLineCharacters)
                    {
                        throw new CompletionException("The service sent a line that is too long.");
                    }
                    line.Append(c);
                    continue;
                }
                if (line.Length == 0)
                {
                    // A blank line ends the event.
                    if (hasData)
                    {
                        yield return data.ToString();
                        data.Clear();
                        hasData = false;
                    }
                    continue;
                }
                Accept(line.ToString(), data, ref hasData);
                line.Clear();
                if (data.Length > maxLineCharacters * 4)
                {
                    throw new CompletionException("The service sent an event that is too long.");
                }
            }
        }

        if (line.Length > 0)
        {
            Accept(line.ToString(), data, ref hasData);
        }
        if (hasData)
        {
            yield return data.ToString();
        }
    }

    private static void Accept(string line, StringBuilder data, ref bool hasData)
    {
        if (line[0] == ':')
        {
            return;
        }
        var colon = line.IndexOf(':');
        var field = colon < 0 ? line : line[..colon];
        if (field != "data")
        {
            return;
        }
        var value = colon < 0 ? "" : line[(colon + 1)..];
        if (value.StartsWith(' '))
        {
            value = value[1..];
        }
        if (hasData)
        {
            data.Append('\n');
        }
        data.Append(value);
        hasData = true;
    }
}
