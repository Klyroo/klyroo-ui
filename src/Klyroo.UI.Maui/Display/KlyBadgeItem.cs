namespace Klyroo.UI.Maui;

/// <summary>A small badge shown in a read-only block, drawn as a subtle small <see cref="KlyBadge"/>.</summary>
/// <param name="Label">The badge text.</param>
public sealed record KlyBadgeItem(string Label)
{
    /// <summary>The badge color. Neutral when not set.</summary>
    public KlyColor? Color { get; init; }
}
