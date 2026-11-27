namespace Klyroo.UI;

/// <summary>Inline message with an optional icon, title, description, actions and close button, for feedback that stays in the page flow.</summary>
public partial class KlyAlert;

/// <summary>Whether a <see cref="KlyAlert"/> is a live region, and how urgently it is announced.</summary>
public enum KlyAlertLive
{
    /// <summary>No live region: the alert is read in its place, like any content.</summary>
    None,
    /// <summary>A polite announcement (<c>role="status"</c>) that waits for the user to be idle.</summary>
    Polite,
    /// <summary>An assertive announcement (<c>role="alert"</c>) that interrupts.</summary>
    Assertive,
}
