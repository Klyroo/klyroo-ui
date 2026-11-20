using System.Runtime.CompilerServices;

namespace Blux.Playground.Client.Examples;

/// <summary>Stands in for a model in the chat examples: a canned reply, and a stream of tokens with a delay between them.</summary>
public static class ChatDemo
{
    private static readonly string[] Replies =
    [
        "Blazor can stream a reply by awaiting an IAsyncEnumerable of tokens and appending each one to the message, then calling StateHasChanged. The messages list keeps the latest answer in view while you stay at the bottom.",
        "Sure. Keep the text of the answer in a field, append every token you receive and let the component render again. When the stream ends, mark the message as complete so that screen readers read it once.",
        "That works the same in every interactive render mode. In static rendering the form is posted and the whole answer comes back with the page.",
        "A longer answer, then. The prompt keeps its text while the reply streams, and pressing Enter again does nothing until the answer is complete. The button next to it turns into a stop button: pressing it cancels the token stream, and what has arrived so far stays in the conversation. Once the reply ends, the messages list announces it to screen readers in one piece instead of word by word, and the send button comes back. Scroll up while it streams and the list stops following, then offers a button that brings you back to the latest message.",
    ];

    /// <summary>The longest reply: tests that act while the answer streams use it, so a slow browser still has time.</summary>
    public static string LongestReply => Replies.MaxBy(r => r.Length)!;

    /// <summary>A reply chosen from the prompt, always the same one for the same prompt.</summary>
    public static string Reply(string prompt) => Replies[prompt.Aggregate(0, (hash, c) => (hash * 31 + c) & 0x7fffffff) % Replies.Length];

    /// <summary>The words of <paramref name="text"/> one by one, <paramref name="delayMs"/> apart. Cancelling the token stops the stream.</summary>
    public static async IAsyncEnumerable<string> Tokens(string text, int delayMs = 40, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var word in text.Split(' '))
        {
            await Task.Delay(delayMs, cancellationToken);
            yield return word + " ";
        }
    }
}
