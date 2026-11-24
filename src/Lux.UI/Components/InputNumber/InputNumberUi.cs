using Lux.UI.Theming;

namespace Lux.UI;

public sealed record InputNumberUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Base { get; init; }
    public string? Decrement { get; init; }
    public string? Increment { get; init; }
    public string? Icon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "base" => Base,
        "decrement" => Decrement,
        "increment" => Increment,
        "icon" => Icon,
        _ => null,
    };
}
