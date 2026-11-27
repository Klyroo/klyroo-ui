namespace Klyroo.UI.Maui;

/// <summary>What <see cref="KlyChatPrompt.Submitted"/> reports.</summary>
public sealed class KlyChatPromptSubmittedEventArgs : EventArgs
{
    /// <summary>Creates the arguments.</summary>
    /// <param name="text">The text of the message.</param>
    public KlyChatPromptSubmittedEventArgs(string text) => Text = text;

    /// <summary>The text of the message, as typed. It can be empty when the message has attachments.</summary>
    public string Text { get; }
}
