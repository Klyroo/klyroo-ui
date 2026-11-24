namespace Lux.UI;

public sealed record BxRadioItem<TValue>(TValue Value, string Label)
{
    public string? Description { get; init; }
    public bool Disabled { get; init; }
}
