using Blux.UI.Theming;

namespace Blux.UI.Tools;

/// <summary>Per-instance class overrides of <see cref="BxDataListEmpty"/> slots.</summary>
public sealed record DataListEmptyUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Icon { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Actions { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "icon" => Icon,
        "title" => Title,
        "description" => Description,
        "actions" => Actions,
        _ => null,
    };
}
