using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyPageCard"/> slots.</summary>
public sealed record PageCardUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Spotlight { get; init; }
    public string? Header { get; init; }
    public string? Container { get; init; }
    public string? Wrapper { get; init; }
    public string? Body { get; init; }
    public string? Leading { get; init; }
    public string? LeadingIcon { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Footer { get; init; }
    public string? Link { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "spotlight" => Spotlight,
        "header" => Header,
        "container" => Container,
        "wrapper" => Wrapper,
        "body" => Body,
        "leading" => Leading,
        "leadingIcon" => LeadingIcon,
        "title" => Title,
        "description" => Description,
        "footer" => Footer,
        "link" => Link,
        _ => null,
    };
}
