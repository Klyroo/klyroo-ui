namespace Klyroo.UI.Maui;

/// <summary>Where a conversation is: nothing pending, the request sent and no answer yet, the answer arriving, or failed.</summary>
public enum ChatStatus
{
    /// <summary>Nothing is pending.</summary>
    Idle,
    /// <summary>The request is sent and no answer has started.</summary>
    Submitted,
    /// <summary>The answer is arriving.</summary>
    Streaming,
    /// <summary>The request failed.</summary>
    Error,
}
