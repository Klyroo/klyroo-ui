using Lux.UI.Theming;

namespace Lux.UI;

public sealed record InputDateUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Base { get; init; }
    public string? Trigger { get; init; }
    public string? TriggerButton { get; init; }
    public string? TriggerIcon { get; init; }
    public string? Content { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "base" => Base,
        "trigger" => Trigger,
        "triggerButton" => TriggerButton,
        "triggerIcon" => TriggerIcon,
        "content" => Content,
        _ => null,
    };
}
