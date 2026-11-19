using Blux.UI.Theming;

namespace Blux.UI;

public sealed record InputTagsUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Tags { get; init; }
    public string? Tag { get; init; }
    public string? Remove { get; init; }
    public string? Input { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "tags" => Tags,
        "tag" => Tag,
        "remove" => Remove,
        "input" => Input,
        _ => null,
    };
}
