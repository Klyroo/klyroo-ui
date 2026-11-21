using System.Collections;

namespace Blux.UI.Tools;

/// <summary>Equality of query values: by value (arrays element by element), and a moment includes its offset, because 10:00+01:00 is not 09:00Z in a URL.</summary>
internal static class QueryEquality
{
    public static bool Equal<T>(T a, T b)
    {
        if (a is DateTimeOffset x && b is DateTimeOffset y) return x.EqualsExact(y);
        if (a is IStructuralEquatable equatable) return equatable.Equals(b, StructuralComparisons.StructuralEqualityComparer);
        return EqualityComparer<T>.Default.Equals(a, b);
    }
}
