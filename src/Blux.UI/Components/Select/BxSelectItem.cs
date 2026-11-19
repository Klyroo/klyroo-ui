namespace Blux.UI;

public sealed record BxSelectItem(string Value, string Label)
{
    public bool Disabled { get; init; }
}
