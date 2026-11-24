using Lux.UI.Theming;

namespace Lux.UI;

public sealed record MarqueeUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Group { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "group" => Group,
        _ => null,
    };
}
