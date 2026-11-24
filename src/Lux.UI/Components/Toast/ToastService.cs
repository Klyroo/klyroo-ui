using Microsoft.JSInterop;

namespace Lux.UI;

public sealed record BxToast
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public BxColor Color { get; init; } = BxColor.Neutral;

    /// <summary>Time before the toast closes itself; hovering or focusing it pauses the countdown. Zero keeps it open.</summary>
    public TimeSpan? Duration { get; init; }
}

/// <summary>Shows toasts in the page's <see cref="BxToaster"/>. Needs an interactive render mode.</summary>
public interface IToastService
{
    ValueTask Add(BxToast toast);
}

internal sealed class ToastService(IJSRuntime js) : IToastService
{
    // Primitive arguments only: they serialize without reflection, so trimming cannot break them.
    public ValueTask Add(BxToast toast) =>
        js.InvokeVoidAsync("Blux.toast", toast.Title, toast.Description, toast.Color.Value,
            toast.Duration is { } duration ? (int)duration.TotalMilliseconds : null);
}
