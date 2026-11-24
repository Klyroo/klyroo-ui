using Lux.UI.Theming;

namespace Lux.UI;

/// <summary>Per-instance class overrides of <see cref="BxEmpty"/> slots.</summary>
public sealed record EmptyUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Header { get; init; }
    public string? Icon { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Actions { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "header" => Header,
        "icon" => Icon,
        "title" => Title,
        "description" => Description,
        "actions" => Actions,
        _ => null,
    };
}
