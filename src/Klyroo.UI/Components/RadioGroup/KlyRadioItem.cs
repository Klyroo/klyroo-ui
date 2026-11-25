namespace Klyroo.UI;

public sealed record KlyRadioItem<TValue>(TValue Value, string Label)
{
    public string? Description { get; init; }
    public bool Disabled { get; init; }
}
