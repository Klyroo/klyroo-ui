namespace Lux.UI.Internal;

/// <summary>The items of one heading, or items with none (<see cref="Label"/> is null) that are listed as they are.</summary>
internal sealed record SelectSection(string? Label, IReadOnlyList<BxSelectItem> Items);

internal static class SelectSections
{
    /// <summary>
    /// Splits items by <see cref="BxSelectItem.Group"/>: the items without a group first, in their order and with no heading,
    /// then each group in the order of its first item, with all its items. Items with no group at all are one section with no
    /// heading, so a list that does not use groups renders exactly as it always did.
    /// </summary>
    public static IReadOnlyList<SelectSection> Of(IReadOnlyList<BxSelectItem> items)
    {
        if (!items.Any(i => !string.IsNullOrEmpty(i.Group))) return [new SelectSection(null, items)];

        var loose = new List<BxSelectItem>();
        var groups = new List<SelectSection>();
        var lists = new Dictionary<string, List<BxSelectItem>>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.Group)) loose.Add(item);
            else if (lists.TryGetValue(item.Group, out var list)) list.Add(item);
            else
            {
                list = [item];
                lists[item.Group] = list;
                groups.Add(new SelectSection(item.Group, list));
            }
        }
        return loose.Count == 0 ? groups : [new SelectSection(null, loose), .. groups];
    }
}
