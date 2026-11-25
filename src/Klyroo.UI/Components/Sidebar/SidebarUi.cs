using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>Per-instance class overrides of <see cref="KlySidebar"/> slots.</summary>
public sealed record SidebarUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyDashboardSidebar"/> and <see cref="KlySidebar"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>host</c> slot of <see cref="KlyDashboardSidebar"/> and <see cref="KlySidebar"/>, after the theme and the app configuration.</summary>
    public string? Host { get; init; }
    /// <summary>The wrapper of everything inside the aside but the resize handle; the panel takes its dialog role while it is open.</summary>
    public string? Panel { get; init; }
    /// <summary>Classes merged into the <c>header</c> slot of <see cref="KlyDashboardSidebar"/> and <see cref="KlySidebar"/>, after the theme and the app configuration.</summary>
    public string? Header { get; init; }
    /// <summary>Classes merged into the <c>headerContent</c> slot of <see cref="KlyDashboardSidebar"/> and <see cref="KlySidebar"/>, after the theme and the app configuration.</summary>
    public string? HeaderContent { get; init; }
    /// <summary>Classes merged into the <c>toggle</c> slot of <see cref="KlyDashboardSidebar"/> and <see cref="KlySidebar"/>, after the theme and the app configuration.</summary>
    public string? Toggle { get; init; }
    /// <summary>Classes merged into the <c>toggleIcon</c> slot of <see cref="KlyDashboardSidebar"/> and <see cref="KlySidebar"/>, after the theme and the app configuration.</summary>
    public string? ToggleIcon { get; init; }
    /// <summary>Classes merged into the <c>content</c> slot of <see cref="KlyDashboardSidebar"/> and <see cref="KlySidebar"/>, after the theme and the app configuration.</summary>
    public string? Content { get; init; }
    /// <summary>Classes merged into the <c>footer</c> slot of <see cref="KlyDashboardSidebar"/> and <see cref="KlySidebar"/>, after the theme and the app configuration.</summary>
    public string? Footer { get; init; }
    /// <summary>Classes merged into the <c>handle</c> slot of <see cref="KlyDashboardSidebar"/> and <see cref="KlySidebar"/>, after the theme and the app configuration.</summary>
    public string? Handle { get; init; }
    /// <summary>The wrapper of the button that opens the panel (below lg).</summary>
    public string? Mobile { get; init; }
    /// <summary>The title row of the panel, below lg.</summary>
    public string? MobileHeader { get; init; }
    /// <summary>Classes merged into the <c>mobileTitle</c> slot of <see cref="KlyDashboardSidebar"/> and <see cref="KlySidebar"/>, after the theme and the app configuration.</summary>
    public string? MobileTitle { get; init; }
    /// <summary>Classes merged into the <c>mobileClose</c> slot of <see cref="KlyDashboardSidebar"/> and <see cref="KlySidebar"/>, after the theme and the app configuration.</summary>
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
