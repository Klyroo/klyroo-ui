namespace Lux.UI;

public sealed record LxRadioItem<TValue>(TValue Value, string Label)
{
    public string? Description { get; init; }
    public bool Disabled { get; init; }
}
