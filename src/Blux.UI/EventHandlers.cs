using Microsoft.AspNetCore.Components;

namespace Blux.UI;

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

/// <summary>Custom events, registered on the browser side by <c>Blux.UI.lib.module.js</c>.</summary>
[EventHandler("onbxtoggle", typeof(BxToggleEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("onbxchange", typeof(BxChangeEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
public static class EventHandlers;
