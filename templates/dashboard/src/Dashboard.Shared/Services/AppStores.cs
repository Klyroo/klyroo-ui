using Dashboard.Shared.Data;

namespace Dashboard.Shared.Services;

/// <summary>
/// The customers the app shows, one set per user session (a scoped service). The rows are replaced, never edited in place: the data list loads again when it gets a
/// new collection. Swap the body for calls to your API or your database; the pages only use <see cref="Items"/>, <see cref="Add"/> and <see cref="Remove"/>.
/// </summary>
public sealed class CustomerStore
{
    /// <summary>The customers, newest id last. A new array after every change.</summary>
    public Customer[] Items { get; private set; } = CustomerData.All;

    /// <summary>Raised after a change.</summary>
    public event Action? Changed;

    /// <summary>Adds a customer with the next free id.</summary>
    public Customer Add(string name, string email, string company, string country, string plan)
    {
        var customer = new Customer(Items.Max(c => c.Id) + 1, name, email, company, country, plan, plan == "Free" ? "Trial" : "Active", 0, 0m, DemoClock.Today, true);
        Items = [.. Items, customer];
        Changed?.Invoke();
        return customer;
    }

    /// <summary>Removes customers by id.</summary>
    public void Remove(IReadOnlyCollection<int> ids)
    {
        Items = [.. Items.Where(c => !ids.Contains(c.Id))];
        Changed?.Invoke();
    }
}

/// <summary>The messages of the inbox, and which ones were read or archived, for the session.</summary>
public sealed class MailStore
{
    private readonly HashSet<int> _read = [];
    private readonly HashSet<int> _archived = [];

    /// <summary>The messages that are not archived, newest first.</summary>
    public IReadOnlyList<Mail> Items => [.. MailData.All.Where(m => !_archived.Contains(m.Id)).Select(m => m with { Unread = m.Unread && !_read.Contains(m.Id) })];

    /// <summary>How many messages are unread.</summary>
    public int UnreadCount => Items.Count(m => m.Unread);

    /// <summary>Raised after a change.</summary>
    public event Action? Changed;

    public Mail? Find(int id) => Items.FirstOrDefault(m => m.Id == id);

    public void MarkRead(int id)
    {
        if (_read.Add(id)) Changed?.Invoke();
    }

    public void Archive(int id)
    {
        if (_archived.Add(id)) Changed?.Invoke();
    }
}

/// <summary>The notifications of the slideover and which ones were read.</summary>
public sealed class NotificationStore
{
    private readonly HashSet<int> _read = [];

    public IReadOnlyList<AppNotification> Items => [.. PeopleData.Notifications.Select(n => n with { Unread = n.Unread && !_read.Contains(n.Id) })];

    public int UnreadCount => Items.Count(n => n.Unread);

    public event Action? Changed;

    public void MarkAllRead()
    {
        foreach (var notification in PeopleData.Notifications) _read.Add(notification.Id);
        Changed?.Invoke();
    }
}

/// <summary>The members of the team: their roles can be changed and people can be invited, for the session.</summary>
public sealed class MemberStore
{
    private readonly List<Member> _members = [.. PeopleData.Members];

    public IReadOnlyList<Member> Items => _members;

    public event Action? Changed;

    public void SetRole(int id, string role)
    {
        var index = _members.FindIndex(m => m.Id == id);
        if (index < 0) return;
        _members[index] = _members[index] with { Role = role };
        Changed?.Invoke();
    }

    public void Invite(string email, string name, string role)
    {
        if (_members.Any(m => string.Equals(m.Email, email, StringComparison.OrdinalIgnoreCase))) return;
        _members.Add(new Member(_members.Max(m => m.Id) + 1, name, email, role, false));
        Changed?.Invoke();
    }

    public void Remove(int id)
    {
        if (_members.RemoveAll(m => m.Id == id && m.Role != "Owner") > 0) Changed?.Invoke();
    }
}
