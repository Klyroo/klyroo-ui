namespace Klyroo.UI.Maui;

/// <summary>The state of the "Show more" reveal of the list-like blocks: how many items show, and how many the next press adds.</summary>
internal sealed class DisplayReveal
{
    private int _revealed;

    /// <summary>Keeps at least <paramref name="max"/> items visible.</summary>
    public void Sync(int? max)
    {
        if (max is { } m && _revealed < m) _revealed = m;
    }

    /// <summary>The items to show and the number the next press reveals (0 when everything shows).</summary>
    public (IReadOnlyList<T> Visible, int Remaining) Apply<T>(IReadOnlyList<T>? source, int? max, int? step)
    {
        if (source is null || source.Count == 0) return ([], 0);
        if (max is not { } limitMax || limitMax <= 0) return (source, 0);
        var limit = Math.Max(limitMax, _revealed);
        if (source.Count <= limit) return (source, 0);
        var next = step is > 0 ? Math.Min(step.Value, source.Count - limit) : source.Count - limit;
        return (source.Take(limit).ToList(), next);
    }

    /// <summary>Shows the next batch.</summary>
    public void More(int total, int? max, int? step)
    {
        var current = Math.Max(max ?? 0, _revealed);
        _revealed = step is > 0 ? Math.Min(total, current + step.Value) : total;
    }
}
