using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyPageCta"/> slots.</summary>
public sealed record PageCtaUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Container { get; init; }
    public string? Wrapper { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Links { get; init; }
    public string? Body { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "container" => Container,
        "wrapper" => Wrapper,
        "title" => Title,
        "description" => Description,
        "links" => Links,
        "body" => Body,
        _ => null,
    };
}
