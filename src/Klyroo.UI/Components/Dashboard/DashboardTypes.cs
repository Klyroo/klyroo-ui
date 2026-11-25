using Klyroo.UI.Theming;

namespace Klyroo.UI;

/// <summary>The slots the sidebars of one <see cref="KlyDashboardGroup"/> occupy, shared by every <see cref="DashboardContext"/> the group hands out.</summary>
internal sealed class SidebarSlots
{
    private readonly HashSet<int> _taken = [];

    /// <summary>Takes the lowest free slot (1-based). A sidebar that goes away releases its slot, so a new one reuses it.</summary>
    public int Take()
    {
        var slot = 1;
        while (!_taken.Add(slot)) slot++;
        return slot;
    }

    public void Release(int slot) => _taken.Remove(slot);
}

/// <summary>
/// What a <see cref="KlyDashboardGroup"/> tells the components inside it. The group hands out a new instance when its key
/// changes, so the sidebars and the controls that read it render again with the new key.
/// </summary>
internal sealed class DashboardContext(string? key, SidebarSlots slots)
{
    /// <summary>The storage key of the group: the sidebar remembers its state under it, and the controls find it by it.</summary>
    public string? Key { get; } = key;

    /// <summary>Takes the lowest free sidebar slot of the group.</summary>
    public int RegisterSidebar() => slots.Take();

    public void ReleaseSidebar(int slot) => slots.Release(slot);

    /// <summary>
    /// The storage id of the sidebar in a slot: the first slot has the key of the group, the following ones <c>key-2</c>, <c>key-3</c>,
    /// so two sidebars never share a cookie and the controls without a target keep finding the first. It is computed from the
    /// current key each time, so a sidebar follows a changed key.
    /// </summary>
    public string? SidebarId(int slot) => slot == 1 || Key is null ? Key : $"{Key}-{slot}";
}

/// <summary>Per-instance class overrides of <see cref="KlyDashboardGroup"/> slots.</summary>
public sealed record DashboardGroupUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyDashboardGroup"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }

    string? ISlotOverrides.For(string slot) => slot == "root" ? Root : null;
}

/// <summary>Per-instance class overrides of <see cref="KlyDashboardPanel"/> slots.</summary>
public sealed record DashboardPanelUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyDashboardPanel"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>header</c> slot of <see cref="KlyDashboardPanel"/>, after the theme and the app configuration.</summary>
    public string? Header { get; init; }
    /// <summary>Classes merged into the <c>body</c> slot of <see cref="KlyDashboardPanel"/>, after the theme and the app configuration.</summary>
    public string? Body { get; init; }
    /// <summary>Classes merged into the <c>footer</c> slot of <see cref="KlyDashboardPanel"/>, after the theme and the app configuration.</summary>
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

/// <summary>Per-instance class overrides of <see cref="KlyDashboardResizeHandle"/> slots.</summary>
public sealed record DashboardResizeHandleUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyDashboardResizeHandle"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }

    string? ISlotOverrides.For(string slot) => slot == "root" ? Root : null;
}

/// <summary>Per-instance class overrides of <see cref="KlyDashboardNavbar"/> slots.</summary>
public sealed record DashboardNavbarUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyDashboardNavbar"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>left</c> slot of <see cref="KlyDashboardNavbar"/>, after the theme and the app configuration.</summary>
    public string? Left { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyDashboardNavbar"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>center</c> slot of <see cref="KlyDashboardNavbar"/>, after the theme and the app configuration.</summary>
    public string? Center { get; init; }
    /// <summary>Classes merged into the <c>right</c> slot of <see cref="KlyDashboardNavbar"/>, after the theme and the app configuration.</summary>
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

/// <summary>Per-instance class overrides of <see cref="KlyDashboardToolbar"/> slots.</summary>
public sealed record DashboardToolbarUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyDashboardToolbar"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>left</c> slot of <see cref="KlyDashboardToolbar"/>, after the theme and the app configuration.</summary>
    public string? Left { get; init; }
    /// <summary>Classes merged into the <c>center</c> slot of <see cref="KlyDashboardToolbar"/>, after the theme and the app configuration.</summary>
    public string? Center { get; init; }
    /// <summary>Classes merged into the <c>right</c> slot of <see cref="KlyDashboardToolbar"/>, after the theme and the app configuration.</summary>
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

/// <summary>Per-instance class overrides of <see cref="KlyDashboardSearchButton"/> slots.</summary>
public sealed record DashboardSearchButtonUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyDashboardSearchButton"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>icon</c> slot of <see cref="KlyDashboardSearchButton"/>, after the theme and the app configuration.</summary>
    public string? Icon { get; init; }
    /// <summary>Classes merged into the <c>label</c> slot of <see cref="KlyDashboardSearchButton"/>, after the theme and the app configuration.</summary>
    public string? Label { get; init; }
    /// <summary>Classes merged into the <c>kbds</c> slot of <see cref="KlyDashboardSearchButton"/>, after the theme and the app configuration.</summary>
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
