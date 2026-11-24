using Lux.UI.Theming;

namespace Lux.UI;

public sealed record BlogPostsUi : ISlotOverrides
{
    public string? Root { get; init; }

    string? ISlotOverrides.For(string slot) => slot == "root" ? Root : null;
}
