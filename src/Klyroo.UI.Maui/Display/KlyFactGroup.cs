namespace Klyroo.UI.Maui;

/// <summary>A titled group of rows of a <see cref="KlyFacts"/>.</summary>
/// <param name="Title">The group title.</param>
/// <param name="Items">The rows of the group.</param>
public sealed record KlyFactGroup(string? Title, IReadOnlyList<KlyFact> Items)
{
    /// <summary>Text shown opposite the title.</summary>
    public string? Aside { get; init; }
}
