namespace Blux.UI.Tools.Query;

/// <summary>Value equality for the list members of the records (records compare lists by reference otherwise).</summary>
internal static class SequenceEquality
{
    public static bool Equal<T>(IReadOnlyList<T> a, IReadOnlyList<T> b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a.Count != b.Count) return false;
        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < a.Count; i++)
            if (!comparer.Equals(a[i], b[i])) return false;
        return true;
    }

    public static int Hash<T>(IReadOnlyList<T> list)
    {
        var hash = new HashCode();
        foreach (var item in list) hash.Add(item);
        return hash.ToHashCode();
    }
}
