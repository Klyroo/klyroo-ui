using Lux.UI.Theming;

namespace Lux.UI;

public sealed record CollapsibleUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Trigger { get; init; }
    public string? Label { get; init; }
    public string? Icon { get; init; }
    public string? Content { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "trigger" => Trigger,
        "label" => Label,
        "icon" => Icon,
        "content" => Content,
        _ => null,
    };
}
