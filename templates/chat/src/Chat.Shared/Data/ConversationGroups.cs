namespace Chat.Shared.Data;

/// <summary>The conversations of one period of the list: Today, Yesterday...</summary>
public sealed record ConversationGroup(string Label, IReadOnlyList<Conversation> Items);

public static class ConversationGroups
{
    /// <summary>
    /// Groups conversations by the day they were last used, as the user's calendar counts it: <paramref name="offset"/> is the UTC offset of the browser,
    /// so "Today" starts at the user's midnight, not the server's. The order of <paramref name="items"/> is kept inside a group, and empty groups are left out.
    /// </summary>
    public static IReadOnlyList<ConversationGroup> Of(IEnumerable<Conversation> items, DateTimeOffset now, TimeSpan offset)
    {
        var today = (now.UtcDateTime + offset).Date;
        var groups = new List<(string Label, List<Conversation> Items)>
        {
            ("Today", []),
            ("Yesterday", []),
            ("Previous 7 days", []),
            ("Previous 30 days", []),
            ("Older", []),
        };
        foreach (var conversation in items)
        {
            var days = (today - (conversation.UpdatedAt.UtcDateTime + offset).Date).Days;
            var index = days <= 0 ? 0 : days == 1 ? 1 : days <= 7 ? 2 : days <= 30 ? 3 : 4;
            groups[index].Items.Add(conversation);
        }
        return [.. groups.Where(g => g.Items.Count > 0).Select(g => new ConversationGroup(g.Label, g.Items))];
    }
}
