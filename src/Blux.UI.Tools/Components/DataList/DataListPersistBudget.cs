using System.Runtime.CompilerServices;

namespace Blux.UI.Tools;

/// <summary>The bytes the data lists of one prerendered page hand to the interactive render, so that together they stay under what a Server circuit accepts at start.</summary>
internal static class DataListPersistBudget
{
    private sealed class Total
    {
        public long Bytes;
    }

    private static readonly ConditionalWeakTable<IServiceProvider, Total> Totals = new();

    /// <summary>Takes <paramref name="bytes"/> from the budget of the scope when the list is within <paramref name="listMax"/> and the lists so far plus this one within <paramref name="pageMax"/>; <see langword="false"/> otherwise (nothing is taken).</summary>
    public static bool Reserve(IServiceProvider scope, long bytes, int listMax, int pageMax)
    {
        if (bytes > listMax) return false;
        var total = Totals.GetOrCreateValue(scope);
        lock (total)
        {
            if (total.Bytes + bytes > pageMax) return false;
            total.Bytes += bytes;
            return true;
        }
    }
}
