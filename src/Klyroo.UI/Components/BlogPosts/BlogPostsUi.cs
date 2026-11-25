using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed record BlogPostsUi : ISlotOverrides
{
    public string? Root { get; init; }

    string? ISlotOverrides.For(string slot) => slot == "root" ? Root : null;
}
