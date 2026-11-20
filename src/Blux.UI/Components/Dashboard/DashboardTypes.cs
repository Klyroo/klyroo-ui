using Blux.UI.Theming;

namespace Blux.UI;

/// <summary>The slots the sidebars of one <see cref="BxDashboardGroup"/> occupy, shared by every <see cref="DashboardContext"/> the group hands out.</summary>
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
/// What a <see cref="BxDashboardGroup"/> tells the components inside it. The group hands out a new instance when its key
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
