namespace Klyroo.UI.Maui;

/// <summary>
/// Where a floating panel (<see cref="KlyTooltip"/>, <see cref="KlyPopover"/>, <see cref="KlyDropdownMenu"/>) sits against its trigger:
/// a side, optionally aligned to the start or the end of that side. The host flips it to the opposite side when there is no room.
/// </summary>
public enum KlyPlacement
{
    /// <summary>Above the trigger, centered.</summary>
    Top,
    /// <summary>Above the trigger, aligned to its start edge.</summary>
    TopStart,
    /// <summary>Above the trigger, aligned to its end edge.</summary>
    TopEnd,
    /// <summary>On the right of the trigger, centered.</summary>
    Right,
    /// <summary>On the right of the trigger, aligned to its top.</summary>
    RightStart,
    /// <summary>On the right of the trigger, aligned to its bottom.</summary>
    RightEnd,
    /// <summary>Below the trigger, centered.</summary>
    Bottom,
    /// <summary>Below the trigger, aligned to its start edge.</summary>
    BottomStart,
    /// <summary>Below the trigger, aligned to its end edge.</summary>
    BottomEnd,
    /// <summary>On the left of the trigger, centered.</summary>
    Left,
    /// <summary>On the left of the trigger, aligned to its top.</summary>
    LeftStart,
    /// <summary>On the left of the trigger, aligned to its bottom.</summary>
    LeftEnd,
}
