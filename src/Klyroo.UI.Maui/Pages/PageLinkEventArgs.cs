namespace Klyroo.UI.Maui;

/// <summary>
/// What a link of the page, marketing and content controls reports when the user follows it. A native app has no address bar: the app navigates (a Shell route, a page push) in the handler
/// and sets <see cref="Handled"/>. When nobody handles it and <see cref="Href"/> is an absolute <c>http</c>, <c>https</c>, <c>mailto</c> or <c>tel</c> address, the control opens it with the system.
/// </summary>
public sealed class PageLinkEventArgs : EventArgs
{
    /// <summary>Creates the arguments of a followed link.</summary>
    /// <param name="href">The address of the link, or <c>null</c> when it has none.</param>
    /// <param name="item">The item of the control the link comes from, such as a <see cref="PageLinkItem"/>.</param>
    public PageLinkEventArgs(string? href, object? item)
    {
        Href = href;
        Item = item;
    }

    /// <summary>The address the link points to.</summary>
    public string? Href { get; }

    /// <summary>The item of the control the link comes from: a <see cref="PageLinkItem"/>, a <see cref="KlyAuthProvider"/>, a <see cref="KlyContentSurroundLink"/>... <c>null</c> for a control that is itself the link.</summary>
    public object? Item { get; }

    /// <summary>Set it to <c>true</c> when the app handled the link, so the control does not open <see cref="Href"/> with the system.</summary>
    public bool Handled { get; set; }
}
