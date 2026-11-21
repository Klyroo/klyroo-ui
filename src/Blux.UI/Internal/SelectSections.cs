namespace Blux.UI.Internal;

/// <summary>The items of one heading, or items with none (<see cref="Label"/> is null) that are listed as they are.</summary>
internal sealed record SelectSection(string? Label, IReadOnlyList<BxSelectItem> Items);

internal static class SelectSections
{
    /// <summary>
    /// Splits items by <see cref="BxSelectItem.Group"/>. A group is listed where its first item is, with all its items;
    /// items without a group stay where they are. Items with no group at all are one section with no heading, so a list
    /// that does not use groups renders exactly as it always did.
    /// </summary>
    public static IReadOnlyList<SelectSection> Of(IReadOnlyList<BxSelectItem> items)
    {
        if (!items.Any(i => !string.IsNullOrEmpty(i.Group))) return [new SelectSection(null, items)];

        var sections = new List<SelectSection>();
        var lists = new Dictionary<string, List<BxSelectItem>>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.Group))
            {
                if (sections.Count > 0 && sections[^1].Label is null) ((List<BxSelectItem>)sections[^1].Items).Add(item);
                else sections.Add(new SelectSection(null, new List<BxSelectItem> { item }));
            }
            else if (lists.TryGetValue(item.Group, out var list)) list.Add(item);
            else
            {
                list = [item];
                lists[item.Group] = list;
                sections.Add(new SelectSection(item.Group, list));
            }
        }
        return sections;
    }
}
