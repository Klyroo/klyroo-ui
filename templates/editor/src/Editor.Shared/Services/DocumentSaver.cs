using Editor.Shared.Data;

namespace Editor.Shared.Services;

/// <summary>
/// Puts the changes of the editor into <see cref="DocumentState"/> one at a time. The editor reports a change 400 ms after you stop typing and when the focus leaves it, so a
/// slow store (a full disk, a database behind your own sign-in) can still be writing the first change when the second arrives. Written side by side, the first to finish would
/// say "Saved" while the second was still running, a failed second one could be hidden behind it, and the older text could land last. Here a change that arrives while one is being
/// written waits, only the latest of the waiting ones for a document is kept (it holds all the text), and <see cref="Finished"/> speaks once, for the last change.
/// </summary>
public sealed class DocumentSaver(DocumentState state)
{
    private readonly Dictionary<Document, string?> _waiting = [];
    private Document? _writing;
    private bool _running;

    /// <summary>Raised when everything given for a document has been written (or refused), with what happened to the last change.</summary>
    public event Action<Document, SaveResult>? Finished;

    /// <summary>True while a change of the document is being written or waits to be.</summary>
    public bool IsBusy(Document document) => _waiting.ContainsKey(document) || _writing == document;

    /// <summary>Keeps <paramref name="html"/> as the text of the document, after the writes that are already running. Returns when the queue is empty.</summary>
    public async Task SubmitAsync(Document document, string? html)
    {
        _waiting[document] = html;
        if (_running)
        {
            return;
        }
        _running = true;
        try
        {
            while (_waiting.Count > 0)
            {
                var (next, text) = _waiting.First();
                _waiting.Remove(next);
                _writing = next;
                SaveResult result;
                try
                {
                    result = await state.UpdateHtmlAsync(next, text);
                }
                finally
                {
                    _writing = null;
                }
                // A change that came in meanwhile will speak for this document when it is written.
                if (!_waiting.ContainsKey(next))
                {
                    Finished?.Invoke(next, result);
                }
            }
        }
        finally
        {
            _running = false;
        }
    }
}
