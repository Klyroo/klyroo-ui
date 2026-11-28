namespace Klyroo.UI.Maui;

/// <summary>Who wrote a <see cref="KlyChatMessage"/>.</summary>
public enum ChatRole
{
    /// <summary>Written by the person using the chat.</summary>
    User,
    /// <summary>Written by the model or the agent.</summary>
    Assistant,
    /// <summary>A notice from the app, such as an instruction or a status.</summary>
    System,
}
