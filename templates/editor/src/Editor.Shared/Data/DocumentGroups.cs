namespace Editor.Shared.Data;

/// <summary>The documents of one period of the list: Today, Yesterday...</summary>
public sealed record DocumentGroup(string Label, IReadOnlyList<Document> Items);

public static class DocumentGroups
{
    /// <summary>
    /// Groups documents by the day they were last changed, as the user's calendar counts it: <paramref name="offset"/> is the UTC offset of the browser,
    /// so "Today" starts at the user's midnight, not the server's. The order of <paramref name="items"/> is kept inside a group, and empty groups are left out.
    /// </summary>
    public static IReadOnlyList<DocumentGroup> Of(IEnumerable<Document> items, DateTimeOffset now, TimeSpan offset)
    {
        var today = (now.UtcDateTime + offset).Date;
        var groups = new List<(string Label, List<Document> Items)>
        {
            ("Today", []),
            ("Yesterday", []),
            ("Previous 7 days", []),
            ("Previous 30 days", []),
            ("Older", []),
        };
        foreach (var document in items)
        {
            var days = (today - (document.UpdatedAt.UtcDateTime + offset).Date).Days;
            var index = days <= 0 ? 0 : days == 1 ? 1 : days <= 7 ? 2 : days <= 30 ? 3 : 4;
            groups[index].Items.Add(document);
        }
        return [.. groups.Where(g => g.Items.Count > 0).Select(g => new DocumentGroup(g.Label, g.Items))];
    }
}
