using Lux.UI.Theming;

namespace Lux.UI;

/// <summary>Per-instance class overrides of <see cref="LxSidebar"/> slots.</summary>
public sealed record SidebarUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Host { get; init; }
    /// <summary>The wrapper of everything inside the aside but the resize handle; the panel takes its dialog role while it is open.</summary>
    public string? Panel { get; init; }
    public string? Header { get; init; }
    public string? HeaderContent { get; init; }
    public string? Toggle { get; init; }
    public string? ToggleIcon { get; init; }
    public string? Content { get; init; }
    public string? Footer { get; init; }
    public string? Handle { get; init; }
    /// <summary>The wrapper of the button that opens the panel (below lg).</summary>
    public string? Mobile { get; init; }
    /// <summary>The title row of the panel, below lg.</summary>
    public string? MobileHeader { get; init; }
    public string? MobileTitle { get; init; }
    public string? MobileClose { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "host" => Host,
        "panel" => Panel,
        "header" => Header,
        "headerContent" => HeaderContent,
        "toggle" => Toggle,
        "toggleIcon" => ToggleIcon,
        "content" => Content,
        "footer" => Footer,
        "handle" => Handle,
        "mobile" => Mobile,
        "mobileHeader" => MobileHeader,
        "mobileTitle" => MobileTitle,
        "mobileClose" => MobileClose,
        _ => null,
    };
}
