using Blux.UI.Theming;

namespace Blux.UI;

/// <summary>What a <see cref="BxDashboardGroup"/> tells the components inside it.</summary>
internal sealed class DashboardContext
{
    /// <summary>The storage key of the group: the sidebar remembers its state under it, and the controls find it by it.</summary>
    public string? Key { get; set; }

    private int _sidebars;

    /// <summary>
    /// Counts a sidebar of the group, in document order, and gives its storage id: the first one has the key of the group, the
    /// following ones <c>key-2</c>, <c>key-3</c>, so two sidebars never share a cookie and the controls without a target keep
    /// finding the first.
    /// </summary>
    public string? RegisterSidebar()
    {
        var index = ++_sidebars;
        return index == 1 || Key is null ? Key : $"{Key}-{index}";
    }
}

/// <summary>Per-instance class overrides of <see cref="BxDashboardGroup"/> slots.</summary>
public sealed record DashboardGroupUi : ISlotOverrides
{
    public string? Root { get; init; }

    string? ISlotOverrides.For(string slot) => slot == "root" ? Root : null;
}

/// <summary>Per-instance class overrides of <see cref="BxDashboardPanel"/> slots.</summary>
public sealed record DashboardPanelUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Header { get; init; }
    public string? Body { get; init; }
    public string? Footer { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "header" => Header,
        "body" => Body,
        "footer" => Footer,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of <see cref="BxDashboardResizeHandle"/> slots.</summary>
public sealed record DashboardResizeHandleUi : ISlotOverrides
{
    public string? Root { get; init; }

    string? ISlotOverrides.For(string slot) => slot == "root" ? Root : null;
}

/// <summary>Per-instance class overrides of <see cref="BxDashboardNavbar"/> slots.</summary>
public sealed record DashboardNavbarUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Left { get; init; }
    public string? Title { get; init; }
    public string? Center { get; init; }
    public string? Right { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "left" => Left,
        "title" => Title,
        "center" => Center,
        "right" => Right,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of <see cref="BxDashboardToolbar"/> slots.</summary>
public sealed record DashboardToolbarUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Left { get; init; }
    public string? Center { get; init; }
    public string? Right { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "left" => Left,
        "center" => Center,
        "right" => Right,
        _ => null,
    };
}

/// <summary>Per-instance class overrides of <see cref="BxDashboardSearchButton"/> slots.</summary>
public sealed record DashboardSearchButtonUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Icon { get; init; }
    public string? Label { get; init; }
    public string? Kbds { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "icon" => Icon,
        "label" => Label,
        "kbds" => Kbds,
        _ => null,
    };
}
