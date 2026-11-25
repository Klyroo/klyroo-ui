using Klyroo.UI.Theming;

namespace Klyroo.UI;

public enum PinInputType
{
    Numeric,
    Alphanumeric,
}

public sealed record PinInputUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Cell { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "cell" => Cell,
        _ => null,
    };
}
