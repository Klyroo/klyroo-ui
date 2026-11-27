namespace Klyroo.UI.Maui;

/// <summary>A notification shown by <see cref="KlyToaster"/>.</summary>
public sealed record KlyToast
{
    /// <summary>Title of the toast.</summary>
    public string? Title { get; init; }

    /// <summary>Text under the title.</summary>
    public string? Description { get; init; }

    /// <summary>Semantic color of the toast, drawn as the strip on its edge. Defaults to neutral.</summary>
    public KlyColor Color { get; init; } = KlyColor.Neutral;

    /// <summary>Time before the toast closes itself; a pointer over it, or a finger on it, pauses the countdown. Zero keeps it open. Null uses <see cref="KlyToaster.Duration"/>.</summary>
    public TimeSpan? Duration { get; init; }
}
