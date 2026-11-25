using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed record InputRatingUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Input { get; init; }
    public string? Star { get; init; }
    public string? Icon { get; init; }
    public string? Fill { get; init; }
    public string? FilledIcon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "input" => Input,
        "star" => Star,
        "icon" => Icon,
        "fill" => Fill,
        "filledIcon" => FilledIcon,
        _ => null,
    };
}
