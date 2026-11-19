using Blux.UI.Theming;

namespace Blux.UI;

/// <summary>Per-instance class overrides of <see cref="BxSidebar"/> slots.</summary>
public sealed record SidebarUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Host { get; init; }
    public string? Header { get; init; }
    public string? HeaderContent { get; init; }
    public string? Toggle { get; init; }
    public string? ToggleIcon { get; init; }
    public string? Content { get; init; }
    public string? Footer { get; init; }
    public string? Handle { get; init; }
    public string? Mobile { get; init; }
    public string? MobileBody { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "host" => Host,
        "header" => Header,
        "headerContent" => HeaderContent,
        "toggle" => Toggle,
        "toggleIcon" => ToggleIcon,
        "content" => Content,
        "footer" => Footer,
        "handle" => Handle,
        "mobile" => Mobile,
        "mobileBody" => MobileBody,
        _ => null,
    };
}
