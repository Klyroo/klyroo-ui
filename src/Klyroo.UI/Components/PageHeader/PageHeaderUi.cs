using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyPageHeader"/> slots.</summary>
public sealed record PageHeaderUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Wrapper { get; init; }
    public string? Headline { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Links { get; init; }
    public string? Body { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "wrapper" => Wrapper,
        "headline" => Headline,
        "title" => Title,
        "description" => Description,
        "links" => Links,
        "body" => Body,
        _ => null,
    };
}
