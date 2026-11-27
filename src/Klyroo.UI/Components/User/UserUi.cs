using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlyUser"/> slots.</summary>
public sealed record UserUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>avatar</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? Avatar { get; init; }
    /// <summary>Classes merged into the <c>wrapper</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? Wrapper { get; init; }
    /// <summary>Classes merged into the <c>name</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? Name { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }

    /// <summary>Classes merged into the <c>stack</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? Stack { get; init; }
    /// <summary>Classes merged into the <c>badge</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? Badge { get; init; }
    /// <summary>Classes merged into the <c>contacts</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? Contacts { get; init; }
    /// <summary>Classes merged into the <c>contact</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? Contact { get; init; }
    /// <summary>Classes merged into the <c>contactBody</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? ContactBody { get; init; }
    /// <summary>Classes merged into the <c>contactIcon</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? ContactIcon { get; init; }
    /// <summary>Classes merged into the <c>contactLink</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? ContactLink { get; init; }
    /// <summary>Classes merged into the <c>contactText</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? ContactText { get; init; }
    /// <summary>Classes merged into the <c>copy</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? Copy { get; init; }
    /// <summary>Classes merged into the <c>copyIcon</c> slot of <see cref="KlyUser"/>, after the theme and the app configuration.</summary>
    public string? CopyIcon { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "avatar" => Avatar,
        "wrapper" => Wrapper,
        "name" => Name,
        "description" => Description,
        "stack" => Stack,
        "badge" => Badge,
        "contacts" => Contacts,
        "contact" => Contact,
        "contactBody" => ContactBody,
        "contactIcon" => ContactIcon,
        "contactLink" => ContactLink,
        "contactText" => ContactText,
        "copy" => Copy,
        "copyIcon" => CopyIcon,
        _ => null,
    };
}
