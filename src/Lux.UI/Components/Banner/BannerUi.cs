using Lux.UI.Theming;

namespace Lux.UI;

/// <summary>Per-instance class overrides of <see cref="LxBanner"/> slots.</summary>
public sealed record BannerUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Container { get; init; }
    public string? Icon { get; init; }
    public string? Content { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Actions { get; init; }
    public string? Close { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "container" => Container,
        "icon" => Icon,
        "content" => Content,
        "title" => Title,
        "description" => Description,
        "actions" => Actions,
        "close" => Close,
        _ => null,
    };
}
