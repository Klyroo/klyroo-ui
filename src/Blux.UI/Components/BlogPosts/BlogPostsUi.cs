using Blux.UI.Theming;

namespace Blux.UI;

public sealed record BlogPostsUi : ISlotOverrides
{
    public string? Root { get; init; }

    string? ISlotOverrides.For(string slot) => slot == "root" ? Root : null;
}
