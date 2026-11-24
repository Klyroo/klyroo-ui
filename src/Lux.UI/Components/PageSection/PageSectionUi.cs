using Lux.UI.Theming;

namespace Lux.UI;

/// <summary>Per-instance class overrides of <see cref="BxPageSection"/> slots.</summary>
public sealed record PageSectionUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Container { get; init; }
    public string? Wrapper { get; init; }
    public string? Leading { get; init; }
    public string? LeadingIcon { get; init; }
    public string? Headline { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Links { get; init; }
    public string? Features { get; init; }
    public string? Feature { get; init; }
    public string? Media { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "container" => Container,
        "wrapper" => Wrapper,
        "leading" => Leading,
        "leadingIcon" => LeadingIcon,
        "headline" => Headline,
        "title" => Title,
        "description" => Description,
        "links" => Links,
        "features" => Features,
        "feature" => Feature,
        "media" => Media,
        _ => null,
    };
}
