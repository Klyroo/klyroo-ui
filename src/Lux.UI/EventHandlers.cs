using Microsoft.AspNetCore.Components;

namespace Lux.UI;

/// <summary>Payload of the <c>lx-toggle</c> browser event raised by Lux custom elements when they open or close.</summary>
public sealed class LxToggleEventArgs : EventArgs
{
    public bool Open { get; set; }

    /// <summary>
    /// Increments with every toggle of one element. Rendered back so the element can ignore a render
    /// that was computed before .NET saw its latest toggle.
    /// </summary>
    public int Sequence { get; set; }
}

/// <summary>Payload of the <c>lx-change</c> browser event raised by Lux custom elements when their value changes.</summary>
public sealed class LxChangeEventArgs : EventArgs
{
    public string? Value { get; set; }

    /// <inheritdoc cref="LxToggleEventArgs.Sequence"/>
    public int Sequence { get; set; }
}

/// <summary>Payload of the <c>lx-search</c> browser event raised by menus whose options come from .NET, when the text typed in their filter field settles.</summary>
public sealed class LxSearchEventArgs : EventArgs
{
    /// <summary>The text of the filter field, as typed (not trimmed).</summary>
    public string? Value { get; set; }

    /// <summary>Increments with every report of one element. .NET orders answers by what it asked last, so it does not read it.</summary>
    public int Sequence { get; set; }
}

/// <summary>Payload of the <c>lx-limit</c> browser event raised by <see cref="LxEditor"/> when its text goes past <see cref="LxEditor.MaxLength"/> or comes back within it.</summary>
public sealed class LxLimitEventArgs : EventArgs
{
    /// <summary>True while the text is longer than <see cref="Max"/>.</summary>
    public bool Exceeded { get; set; }

    /// <summary>The characters of HTML the editor holds now.</summary>
    public int Length { get; set; }

    /// <summary>The limit, <see cref="LxEditor.MaxLength"/>.</summary>
    public int Max { get; set; }
}

/// <summary>Custom events, registered on the browser side by <c>Lux.UI.lib.module.js</c>.</summary>
[EventHandler("onlxtoggle", typeof(LxToggleEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("onlxchange", typeof(LxChangeEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("onlxsearch", typeof(LxSearchEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("onlxlimit", typeof(LxLimitEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
public static class EventHandlers;
