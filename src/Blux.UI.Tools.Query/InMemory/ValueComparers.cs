using System.Globalization;

namespace Blux.UI.Tools.Query;

/// <summary>
/// Compares two field values for sorting. Strings use a natural order that ignores case and accents and reads digit runs as numbers
/// (<c>item2</c> before <c>item10</c>), like <c>localeCompare(..., { numeric: true, sensitivity: 'base' })</c> in the Nuxt UI table; every culture
/// rule is the invariant one. Numbers and dates compare by value, <see langword="false"/> before <see langword="true"/>.
/// </summary>
internal sealed class SortValueComparer(bool descending) : IComparer<object?>
{
    /// <summary>Nulls sort last whatever the direction; the direction flips only the order of the values.</summary>
    public int Compare(object? x, object? y)
    {
        if (x is null) return y is null ? 0 : 1;
        if (y is null) return -1;
        var c = CompareValues(x, y);
        return descending ? -c : c;
    }

    public static int CompareValues(object x, object y)
    {
        if (x is string a && y is string b) return NaturalCompare(a, b);
        if (x is IComparable comparable && x.GetType() == y.GetType()) return Math.Sign(comparable.CompareTo(y));
        return string.CompareOrdinal(x.ToString(), y.ToString());
    }

    internal static int NaturalCompare(string a, string b)
    {
        var i = 0;
        var j = 0;
        while (i < a.Length && j < b.Length)
        {
            var digitA = char.IsAsciiDigit(a[i]);
            var digitB = char.IsAsciiDigit(b[j]);
            var startA = i;
            var startB = j;
            while (i < a.Length && char.IsAsciiDigit(a[i]) == digitA) i++;
            while (j < b.Length && char.IsAsciiDigit(b[j]) == digitB) j++;
            var runA = a.AsSpan(startA, i - startA);
            var runB = b.AsSpan(startB, j - startB);
            int c;
            if (digitA && digitB)
            {
                var na = runA.TrimStart('0');
                var nb = runB.TrimStart('0');
                c = na.Length != nb.Length ? na.Length.CompareTo(nb.Length) : na.CompareTo(nb, StringComparison.Ordinal);
            }
            else
            {
                c = CultureInfo.InvariantCulture.CompareInfo.Compare(runA, runB, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);
            }
            if (c != 0) return Math.Sign(c);
        }
        return (a.Length - i).CompareTo(b.Length - j);
    }
}

/// <summary>Compares row keys by their own type; strings are ordinal. The same tie-break the IQueryable applier puts after the sort.</summary>
internal sealed class RowKeyComparer : IComparer<object?>
{
    public static readonly RowKeyComparer Instance = new();

    public int Compare(object? x, object? y)
    {
        if (x is null) return y is null ? 0 : 1;
        if (y is null) return -1;
        if (x is string a && y is string b) return Math.Sign(string.CompareOrdinal(a, b));
        if (x is IComparable comparable && x.GetType() == y.GetType()) return Math.Sign(comparable.CompareTo(y));
        return Math.Sign(string.CompareOrdinal(x.ToString(), y.ToString()));
    }
}
