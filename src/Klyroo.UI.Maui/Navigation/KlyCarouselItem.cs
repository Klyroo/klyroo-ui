namespace Klyroo.UI.Maui;

/// <summary>A slide of a <see cref="KlyCarousel"/>.</summary>
/// <param name="Content">Content of the slide.</param>
public sealed record KlyCarouselItem(View? Content)
{
    /// <summary>What a screen reader says for the slide. Defaults to "1 of 3".</summary>
    public string? Label { get; init; }
}
