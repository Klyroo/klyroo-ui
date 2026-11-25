using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyPageFeature"/> slots.</summary>
public sealed record PageFeatureUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Leading { get; init; }
    public string? LeadingIcon { get; init; }
    public string? Wrapper { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Link { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "leading" => Leading,
        "leadingIcon" => LeadingIcon,
        "wrapper" => Wrapper,
        "title" => Title,
        "description" => Description,
        "link" => Link,
        _ => null,
    };
}
