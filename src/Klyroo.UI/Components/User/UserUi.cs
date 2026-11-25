using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyUser"/> slots.</summary>
public sealed record UserUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Avatar { get; init; }
    public string? Wrapper { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "avatar" => Avatar,
        "wrapper" => Wrapper,
        "name" => Name,
        "description" => Description,
        _ => null,
    };
}
