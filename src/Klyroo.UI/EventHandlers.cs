using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>Payload of the <c>kly-toggle</c> browser event raised by Klyroo custom elements when they open or close.</summary>
public sealed class KlyToggleEventArgs : EventArgs
{
    /// <summary>True when the element is open after the toggle.</summary>
    public bool Open { get; set; }

    /// <summary>
    /// Increments with every toggle of one element. Rendered back so the element can ignore a render
    /// that was computed before .NET saw its latest toggle.
    /// </summary>
    public int Sequence { get; set; }
}

/// <summary>Payload of the <c>kly-change</c> browser event raised by Klyroo custom elements when their value changes.</summary>
public sealed class KlyChangeEventArgs : EventArgs
{
    /// <summary>The new value of the element, as text.</summary>
    public string? Value { get; set; }

    /// <inheritdoc cref="KlyToggleEventArgs.Sequence"/>
    public int Sequence { get; set; }
}

/// <summary>Payload of the <c>kly-search</c> browser event raised by menus whose options come from .NET, when the text typed in their filter field settles.</summary>
public sealed class KlySearchEventArgs : EventArgs
{
    /// <summary>The text of the filter field, as typed (not trimmed).</summary>
    public string? Value { get; set; }

    /// <summary>Increments with every report of one element. .NET orders answers by what it asked last, so it does not read it.</summary>
    public int Sequence { get; set; }
}

/// <summary>Payload of the <c>kly-pick</c> browser event raised by <see cref="KlyChart{TItem}"/> when a bar, point or slice is clicked or chosen with Enter or Space.</summary>
public sealed class KlyPickEventArgs : EventArgs
{
    /// <summary>The position of the row in the data of the chart.</summary>
    public int Row { get; set; }

    /// <summary>The position of the series the pick landed on, or -1 when the browser could not tell (the chart then takes the first series that has a value in the row).</summary>
    public int Series { get; set; }
}

/// <summary>Payload of the <c>kly-limit</c> browser event raised by <see cref="KlyEditor"/> when its text goes past <see cref="KlyEditor.MaxLength"/> or comes back within it.</summary>
public sealed class KlyLimitEventArgs : EventArgs
{
    /// <summary>True while the text is longer than <see cref="Max"/>.</summary>
    public bool Exceeded { get; set; }

    /// <summary>The characters of HTML the editor holds now.</summary>
    public int Length { get; set; }

    /// <summary>The limit, <see cref="KlyEditor.MaxLength"/>.</summary>
    public int Max { get; set; }
}

/// <summary>Custom events, registered on the browser side by <c>Klyroo.UI.lib.module.js</c>.</summary>
[EventHandler("onklytoggle", typeof(KlyToggleEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("onklychange", typeof(KlyChangeEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("onklysearch", typeof(KlySearchEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("onklypick", typeof(KlyPickEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("onklylimit", typeof(KlyLimitEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
public static class EventHandlers;
