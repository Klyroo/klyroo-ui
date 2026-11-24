using Microsoft.AspNetCore.Components;

namespace Lux.UI;

/// <summary>Payload of the <c>bx-toggle</c> browser event raised by Blux custom elements when they open or close.</summary>
public sealed class BxToggleEventArgs : EventArgs
{
    public bool Open { get; set; }

    /// <summary>
    /// Increments with every toggle of one element. Rendered back so the element can ignore a render
    /// that was computed before .NET saw its latest toggle.
    /// </summary>
    public int Sequence { get; set; }
}

/// <summary>Payload of the <c>bx-change</c> browser event raised by Blux custom elements when their value changes.</summary>
public sealed class BxChangeEventArgs : EventArgs
{
    public string? Value { get; set; }

    /// <inheritdoc cref="BxToggleEventArgs.Sequence"/>
    public int Sequence { get; set; }
}

/// <summary>Payload of the <c>bx-search</c> browser event raised by menus whose options come from .NET, when the text typed in their filter field settles.</summary>
public sealed class BxSearchEventArgs : EventArgs
{
    /// <summary>The text of the filter field, as typed (not trimmed).</summary>
    public string? Value { get; set; }

    /// <summary>Increments with every report of one element. .NET orders answers by what it asked last, so it does not read it.</summary>
    public int Sequence { get; set; }
}

/// <summary>Payload of the <c>bx-limit</c> browser event raised by <see cref="BxEditor"/> when its text goes past <see cref="BxEditor.MaxLength"/> or comes back within it.</summary>
public sealed class BxLimitEventArgs : EventArgs
{
    /// <summary>True while the text is longer than <see cref="Max"/>.</summary>
    public bool Exceeded { get; set; }

    /// <summary>The characters of HTML the editor holds now.</summary>
    public int Length { get; set; }

    /// <summary>The limit, <see cref="BxEditor.MaxLength"/>.</summary>
    public int Max { get; set; }
}

/// <summary>Custom events, registered on the browser side by <c>Lux.UI.lib.module.js</c>.</summary>
[EventHandler("onbxtoggle", typeof(BxToggleEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("onbxchange", typeof(BxChangeEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("onbxsearch", typeof(BxSearchEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("onbxlimit", typeof(BxLimitEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
public static class EventHandlers;
